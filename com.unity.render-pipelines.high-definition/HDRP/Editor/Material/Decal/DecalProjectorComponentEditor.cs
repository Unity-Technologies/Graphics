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

        public class DecalBoundsHandle :  BoxBoundsHandle
        {
            protected override Bounds OnHandleChanged(HandleDirection handle, Bounds boundsOnClick, Bounds newBounds)
            {
                // special case for Y axis because decal mesh is centered at 0, -0.5, 0
                if (handle == HandleDirection.NegativeY)
                {
                    m_Translation = Vector3.zero;
                    m_Scale = newBounds.size;
                }
                else if (handle == HandleDirection.PositiveY)
                {
                    m_Translation = (newBounds.center + newBounds.extents - (m_Center + 0.5f * m_Size));
                    m_Scale = (m_Size + m_Translation);
                }
                else
                {
                    m_Translation = newBounds.center - m_Center;
                    m_Scale = newBounds.size;
                }
                return newBounds;
            }

            public void SetSizeAndCenter(Vector3 inSize, Vector3 inCenter)
            {
                // boundsOnClick implies that it gets refreshed only if the handle is clicked on again, but we need actual center and scale which we set before handle is drawn every frame
                m_Center = inCenter;
                m_Size = inSize;
                center = inCenter;
                size = inSize;
            }

            private Vector3 m_Center;
            private Vector3 m_Size;

            public Vector3 m_Translation;
            public Vector3 m_Scale;
        }
        
        private DecalBoundsHandle m_Handle = new DecalBoundsHandle();

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
        }

		private void OnDisable()
		{
			m_DecalProjectorComponent.OnMaterialChange -= OnMaterialChange;
		}

		public void OnMaterialChange()
		{
			// Update material editor with the new material
			m_MaterialEditor = (MaterialEditor)CreateEditor(m_DecalProjectorComponent.Mat);
		}

        void OnSceneGUI()
        {
            EditorGUI.BeginChangeCheck();
            var mat = Handles.matrix;
            var col = Handles.color;

            Handles.color = Color.white;
            // decal mesh is centered at (0, -0.5, 0)
            // zero out the local scale in the matrix so that handle code gives us back the actual scale
            Handles.matrix = Matrix4x4.TRS(m_DecalProjectorComponent.transform.position, m_DecalProjectorComponent.transform.rotation, Vector3.one) * Matrix4x4.Translate(new Vector3(0.0f, -0.5f* m_DecalProjectorComponent.transform.localScale.y, 0.0f));
            // pass in the scale 
            m_Handle.SetSizeAndCenter(m_DecalProjectorComponent.transform.localScale, Vector3.zero);
            m_Handle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                // adjust decal transform if handle changed
                m_DecalProjectorComponent.transform.Translate(m_Handle.m_Translation);                
                m_DecalProjectorComponent.transform.localScale = m_Handle.m_Scale;
                Repaint();
            }

            Handles.matrix = mat;
            Handles.color = col;
        } 

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_MaterialProperty);
            EditorGUILayout.PropertyField(m_DrawDistanceProperty);
            EditorGUILayout.Slider(m_FadeScaleProperty, 0.0f, 1.0f, new GUIContent("Fade scale"));
            EditorGUILayout.PropertyField(m_UVScaleProperty);
            EditorGUILayout.PropertyField(m_UVBiasProperty);
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
