using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    [CustomEditor(typeof(ForwardRendererData))]
    public class ForwardRendererDataEditor : Editor
    {
        private class Styles
        {
            public static GUIContent renderPasses = new GUIContent("Render Passes", "List of render passes");
            
            public static GUIContent shaderFoldout = new GUIContent("Shaders", "List of required shaders");
            public static GUIContent blitShader = new GUIContent("Blit", "Shader used for all default blits");
            public static GUIContent copyDepthShader = new GUIContent("Copy Depth", "Shader used when copying depth buffers");
            public static GUIContent screenSpaceShadowShader = new GUIContent("Screen Space Shadows", "Shader for calculating screen space shadow map");
            public static GUIContent samplingShader = new GUIContent("Sampling", "Shader used for various sampling features");
        }
        
        SavedBool m_ShadersFoldout;

        private SerializedProperty m_renderPasses;
        private SerializedProperty m_blitShader;
        private SerializedProperty m_copyDepthShader;
        private SerializedProperty m_screenSpaceShadowShader;
        private SerializedProperty m_samplingShader;
        
        private ReorderableList m_passesList;

        private void OnEnable()
        {
            m_ShadersFoldout = new SavedBool($"{target.GetType()}.ShadersFoldout", false);
            
            m_renderPasses = serializedObject.FindProperty("m_RenderPassFeatures");
            m_blitShader = serializedObject.FindProperty("m_BlitShader");
            m_copyDepthShader = serializedObject.FindProperty("m_CopyDepthShader");
            m_screenSpaceShadowShader = serializedObject.FindProperty("m_ScreenSpaceShadowShader");
            m_samplingShader = serializedObject.FindProperty("m_SamplingShader");
		    
            m_passesList = new ReorderableList(serializedObject, m_renderPasses, true, true, true, true);

            m_passesList.drawElementCallback =
            (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = m_passesList.serializedProperty.GetArrayElementAtIndex(index);
                var newRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
                var labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 0.1f;
                EditorGUI.ObjectField(newRect, element);
                EditorGUIUtility.labelWidth = labelWidth;
            };

            m_passesList.onAddCallback += AddPass;
		    
            m_passesList.drawHeaderCallback = (Rect testHeaderRect) => {
                EditorGUI.LabelField(testHeaderRect, "Render Pass Features");
            };
        }

        private void OnDisable()
        {
            m_passesList.onAddCallback -= AddPass;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            // TODO:remove later
            EditorGUILayout.Space(); 
            EditorGUILayout.HelpBox("Testing Inspector Below", MessageType.Warning);
            EditorGUILayout.Space();
            // End TODO
         
            serializedObject.Update();
            
            m_passesList.DoLayoutList();

            EditorGUILayout.Space();

            m_ShadersFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShadersFoldout.value, Styles.shaderFoldout);
            if (m_ShadersFoldout.value)
            {
                EditorGUILayout.PropertyField(m_blitShader, Styles.blitShader);
                EditorGUILayout.PropertyField(m_copyDepthShader, Styles.copyDepthShader);
                EditorGUILayout.PropertyField(m_screenSpaceShadowShader, Styles.screenSpaceShadowShader);
                EditorGUILayout.PropertyField(m_samplingShader, Styles.samplingShader);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();
        }

        private void AddPass(ReorderableList list)
        {
            if (list.serializedProperty != null)
            {
                ++list.serializedProperty.arraySize;
                list.index = list.serializedProperty.arraySize - 1;
            }
 
            EditorUtility.SetDirty(target);
        }
    }
}
