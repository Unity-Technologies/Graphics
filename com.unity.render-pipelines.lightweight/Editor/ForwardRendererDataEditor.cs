using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            public static float defaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            public static GUIContent renderPasses = new GUIContent("Render Passes", "List of render passes");
            public static GUIContent header = new GUIContent("Null Pass", "Null.");
        }
        SavedBool[] m_Foldout;
        
        private SerializedProperty m_renderPasses;
        
        private ReorderableList m_passesList;
        List<SerializedObject> m_ElementSOs = new List<SerializedObject>();

        SerializedObject GetElementSO(int index)
        {
            if (m_ElementSOs.Count != m_renderPasses.arraySize)
                m_ElementSOs = Enumerable.Range(0, m_renderPasses.arraySize)
                    .Select(i => m_renderPasses.GetArrayElementAtIndex(i))
                    .Select(sp => sp.objectReferenceValue == null ? null : new SerializedObject(sp.objectReferenceValue))
                    .ToList();
       
            
            m_ElementSOs[index].Update();
            return m_ElementSOs[index];
        }
        
        private SerializedObject obj;
        private void OnEnable()
        {
            m_renderPasses = serializedObject.FindProperty("m_RenderPassFeatures");
            CreateFoldoutBools();
            
            m_passesList = new ReorderableList(serializedObject, m_renderPasses, true, true, true, true);

            m_passesList.drawElementCallback =
            (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                EditorGUI.BeginChangeCheck();
                var element = m_passesList.serializedProperty.GetArrayElementAtIndex(index);
                var newRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);

                if (element.objectReferenceValue != null)
                {
                    Styles.header.text = element.objectReferenceValue.name;

                    m_Foldout[index].value =
                        EditorGUI.BeginFoldoutHeaderGroup(newRect, m_Foldout[index].value, Styles.header);
                    if (m_Foldout[index].value)
                    {
                        newRect.y += Styles.defaultLineSpace;
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.ObjectField(newRect, element, GUIContent.none);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_ElementSOs[index] = element.objectReferenceValue == null
                                ? null
                                : new SerializedObject(element.objectReferenceValue);
                        }

                        var elementSO = GetElementSO(index);
                        if (elementSO != null)
                        {
                            SerializedProperty propSP = elementSO.FindProperty("settings");

                            EditorGUI.BeginChangeCheck();
                            if (propSP != null)
                            {
                                newRect.y += Styles.defaultLineSpace;
                                rect.y = newRect.y;
                                rect.xMin += EditorStyles.inspectorDefaultMargins.padding.left;
                                EditorGUI.PropertyField(rect, propSP);
                            }

                            if (EditorGUI.EndChangeCheck())
                                elementSO.ApplyModifiedProperties();
                        }
                    }

                    EditorGUI.EndFoldoutHeaderGroup();
                }

                if (EditorGUI.EndChangeCheck())
                {
                    element.serializedObject.ApplyModifiedProperties();
                }
            };

            m_passesList.elementHeightCallback = (index) =>
            {
                var element = m_passesList.serializedProperty.GetArrayElementAtIndex(index);
                if (element.objectReferenceValue == null)
                    return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + (m_Foldout[index].value ? EditorGUIUtility.singleLineHeight : 0f);
                var serializedObject = GetElementSO(index);                
                var settingsProp = serializedObject.FindProperty("settings");
                if (settingsProp != null)
                {
                    return m_Foldout[index].value ? EditorGUI.GetPropertyHeight(settingsProp, true) : Styles.defaultLineSpace;
                }
                else
                {
                    return Styles.defaultLineSpace;
                }
            };

            m_passesList.onAddCallback += AddPass;
            m_passesList.onRemoveCallback += list =>
            {
                m_renderPasses.DeleteArrayElementAtIndex(list.index);
                m_renderPasses.serializedObject.ApplyModifiedProperties();
                m_ElementSOs.Clear();
            };
            m_passesList.onReorderCallbackWithDetails += ReorderPass;
		    
            m_passesList.drawHeaderCallback = (Rect testHeaderRect) => {
                EditorGUI.LabelField(testHeaderRect, "Render Pass Features");
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space();

            m_passesList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void CreateFoldoutBools()
        {
            m_Foldout = new SavedBool[m_renderPasses.arraySize];
            for (var i = 0; i < m_renderPasses.arraySize; i++)
            {
                m_Foldout[i] = new SavedBool($"{serializedObject.targetObject.name}.ELEMENT{i}.PassFoldout", true);
            }
        }

        private void AddPass(ReorderableList list)
        {
            if (list.serializedProperty != null)
            {
                ++list.serializedProperty.arraySize;
                list.index = list.serializedProperty.arraySize - 1;
                list.serializedProperty.serializedObject.ApplyModifiedProperties();
            }
            m_ElementSOs.Clear();
            CreateFoldoutBools();
            EditorUtility.SetDirty(target);
        }
        
        private void ReorderPass(ReorderableList list, int oldIndex, int newIndex)
        {
            var item = m_ElementSOs[oldIndex];
            m_ElementSOs.RemoveAt(oldIndex);
            m_ElementSOs.Insert(newIndex, item);
        }
    }
}
