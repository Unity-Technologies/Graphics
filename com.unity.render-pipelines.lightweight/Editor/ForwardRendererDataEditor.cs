using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Experimental.Rendering;

namespace UnityEngine.Rendering.LWRP
{
    [CustomEditor(typeof(ForwardRendererData))]
    public class ForwardRendererDataEditor : Editor
    {
        private class Styles
        {
            public static GUIContent renderPasses = new GUIContent("Render Passes", "List of render passes");
        }
        
        private SerializedProperty m_renderPasses;
        
        private ReorderableList m_passesList;

        private void OnEnable()
        {
            m_renderPasses = serializedObject.FindProperty("m_RenderPassFeatures");
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
                if ((RenderPassFeature)element.objectReferenceValue)
                {
                    SerializedObject serializedObject = new SerializedObject(element.objectReferenceValue as RenderPassFeature);
                    SerializedProperty propSP = serializedObject.FindProperty("settings");
                    //Rect propRect = propSP.rectValue;
                    if (propSP != null)
                    {
                        rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                        rect.xMin += EditorStyles.inspectorDefaultMargins.padding.left;
                        EditorGUI.PropertyField(rect, propSP);
                    }
                }
               
            };
            
            m_passesList.elementHeightCallback = (index) =>
            {
                var element = m_passesList.serializedProperty.GetArrayElementAtIndex(index);
                SerializedObject serializedObject = new SerializedObject(element.objectReferenceValue as RenderPassFeature);
                var settingsProp = serializedObject.FindProperty("settings");
                if (settingsProp != null)
                {
                    return EditorGUI.GetPropertyHeight(settingsProp, true);
                }
                else
                {
                    return 200f; //EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                }
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
            serializedObject.Update();
            
            EditorGUILayout.Space();
            
            m_passesList.DoLayoutList();

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