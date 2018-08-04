using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CustomEditor(typeof(DecalProjectorComponent))]
    public class DecalProjectorComponentEditor : Editor
    {
        private MaterialEditor m_MaterialEditor = null;
        private DecalProjectorComponent m_DecalProjectorComponent = null;
        private SerializedProperty m_MaterialProperty;
        private SerializedProperty m_DrawDistanceProperty;
        private SerializedProperty m_FadeScaleProperty;
        private SerializedProperty m_UVScaleProperty;
        private SerializedProperty m_UVBiasProperty;
        private SerializedProperty m_AffectsTransparencyProperty;
        private SerializedProperty m_Center;
        private SerializedProperty m_Size;
       
        private BoxBoundsHandle m_Handle = new BoxBoundsHandle();

        private void OnEnable()
        {
            // Create an instance of the MaterialEditor
            m_DecalProjectorComponent = (DecalProjectorComponent)target;
            m_MaterialEditor = (MaterialEditor)CreateEditor(m_DecalProjectorComponent.Mat);
            m_DecalProjectorComponent.OnMaterialChange += OnMaterialChange;
            m_MaterialProperty = serializedObject.FindProperty("m_Material");
            m_DrawDistanceProperty = serializedObject.FindProperty("m_DrawDistance");
            m_FadeScaleProperty = serializedObject.FindProperty("m_FadeScale");
            m_UVScaleProperty = serializedObject.FindProperty("m_UVScale");
            m_UVBiasProperty = serializedObject.FindProperty("m_UVBias");
            m_AffectsTransparencyProperty = serializedObject.FindProperty("m_AffectsTransparency");
            m_Center = serializedObject.FindProperty("m_Offset");
            m_Size = serializedObject.FindProperty("m_Size");
        }

        private void OnDisable()
        {
            m_DecalProjectorComponent.OnMaterialChange -= OnMaterialChange;
        }

        private void OnDestroy()
        {
            DestroyImmediate(m_MaterialEditor);
        }

        public void OnMaterialChange()
        {
            // Update material editor with the new material
            m_MaterialEditor = (MaterialEditor)CreateEditor(m_DecalProjectorComponent.Mat);
        }

        void OnSceneGUI()
        {            
            var mat = Handles.matrix;
            var col = Handles.color;

            Handles.color = Color.white;
            Handles.matrix = m_DecalProjectorComponent.transform.localToWorldMatrix;   
            m_Handle.center = m_DecalProjectorComponent.m_Offset;
            m_Handle.size = m_DecalProjectorComponent.m_Size;
            EditorGUI.BeginChangeCheck();
            m_Handle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                // adjust decal transform if handle changed
                m_DecalProjectorComponent.m_Offset = m_Handle.center;
                m_DecalProjectorComponent.m_Size = m_Handle.size;
                EditorUtility.SetDirty(m_DecalProjectorComponent);
            }
            Handles.matrix = mat;
            Handles.color = col;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_Center);
            EditorGUILayout.PropertyField(m_Size);
            EditorGUILayout.PropertyField(m_MaterialProperty);
            EditorGUILayout.PropertyField(m_DrawDistanceProperty);
            EditorGUILayout.Slider(m_FadeScaleProperty, 0.0f, 1.0f, new GUIContent("Fade scale"));
            EditorGUILayout.PropertyField(m_UVScaleProperty);
            EditorGUILayout.PropertyField(m_UVBiasProperty);
            EditorGUILayout.PropertyField(m_AffectsTransparencyProperty);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            if (m_MaterialEditor != null)
            {
                // Draw the material's foldout and the material shader field
                // Required to call m_MaterialEditor.OnInspectorGUI ();
                m_MaterialEditor.DrawHeader();

                // We need to prevent the user to edit default decal materials
                bool isDefaultMaterial = false;
                var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
                if (hdrp != null)
                {
                    isDefaultMaterial = m_DecalProjectorComponent.Mat == hdrp.GetDefaultDecalMaterial();
                }
                using (new EditorGUI.DisabledGroupScope(isDefaultMaterial))
                {
                    // Draw the material properties
                    // Works only if the foldout of m_MaterialEditor.DrawHeader () is open
                    m_MaterialEditor.OnInspectorGUI();
                }
            }
        }
    }
}
