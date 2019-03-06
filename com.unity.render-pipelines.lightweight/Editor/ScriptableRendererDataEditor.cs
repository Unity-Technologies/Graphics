using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering.LWRP;

namespace UnityEditor.Rendering.LWRP
{
    [CustomEditor(typeof(ScriptableRendererData), true)]
    public class ScriptableRendererDataEditor : Editor
    {
        class Styles
        {
            public static readonly GUIContent RenderFeatures =
                new GUIContent("Renderer Features", 
                "Features to include in this renderer.\nTo add or remove features, use the plus and minus at the bottom of this box.");

            public static readonly GUIContent RenderFeatureHeader =
                new GUIContent("Empty Pass", "This pass does not exist.");
        }

        SavedBool[] m_Foldouts;
        SerializedProperty m_RenderPasses;
        
        ReorderableList m_PassesList;
        List<SerializedObject> m_ElementSOs = new List<SerializedObject>();
        
        SerializedObject GetElementSO(int index)
        {
            if (m_ElementSOs.Count != m_RenderPasses.arraySize || m_ElementSOs[index] == null)
            {
                m_ElementSOs.Clear();
                for (int i = 0; i < m_RenderPasses.arraySize; i++)
                {
                    var obj = m_RenderPasses.GetArrayElementAtIndex(i)?.objectReferenceValue;
                    m_ElementSOs.Add(obj != null ? new SerializedObject(obj) : null);
                }
            }
       
            m_ElementSOs[index].Update();
            return m_ElementSOs[index];
        }
        
        private void OnValidate()
        {
            m_RenderPasses = serializedObject.FindProperty("m_RendererFeatures");
            CreateFoldoutBools();

            m_PassesList = new ReorderableList(m_RenderPasses.serializedObject,
                                                m_RenderPasses,
                                                true,
                                                true,
                                                true,
                                                true);

            m_PassesList.drawElementCallback =
            (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                EditorGUI.BeginChangeCheck();
                var element = m_PassesList.serializedProperty.GetArrayElementAtIndex(index);
                var propRect = new Rect(rect.x, 
                                        rect.y + EditorGUIUtility.standardVerticalSpacing,
                                        rect.width,
                                        EditorGUIUtility.singleLineHeight);
                var headerRect = new Rect(rect.x + EditorUtils.Styles.defaultIndentWidth,
                                            rect.y + EditorGUIUtility.standardVerticalSpacing,
                                            rect.width - EditorUtils.Styles.defaultIndentWidth, 
                                            EditorGUIUtility.singleLineHeight);

                if (element.objectReferenceValue != null)
                {
                    Styles.RenderFeatureHeader.text = element.objectReferenceValue.name;
                    Styles.RenderFeatureHeader.tooltip = element.objectReferenceValue.GetType().Name;
                    m_Foldouts[index].value =
                        EditorGUI.Foldout(headerRect,
                            m_Foldouts[index].value,
                            Styles.RenderFeatureHeader,
                            true);
                    if (m_Foldouts[index].value)
                    {
                        propRect.y += EditorUtils.Styles.defaultLineSpace;
                        EditorGUI.BeginChangeCheck();
                        element.objectReferenceValue.name =
                            EditorGUI.DelayedTextField(propRect, "Pass Name", element.objectReferenceValue.name);
                        if (EditorGUI.EndChangeCheck())
                        {
                            AssetDatabase.SaveAssets();
                        }

                        var elementSO = GetElementSO(index);
                        SerializedProperty settings = elementSO.FindProperty("settings");

                        EditorGUI.BeginChangeCheck();
                        if (settings != null)
                        {
                            propRect.y += EditorUtils.Styles.defaultLineSpace;
                            EditorGUI.PropertyField(propRect, settings, true);
                        }

                        if (EditorGUI.EndChangeCheck())
                            elementSO.ApplyModifiedProperties();
                    }
                }
                else
                {
                    EditorGUI.ObjectField(propRect, element, GUIContent.none);
                }
                
                if (EditorGUI.EndChangeCheck())
                {
                    element.serializedObject.ApplyModifiedProperties();
                }
            };

            m_PassesList.elementHeightCallback = (index) =>
            {
                var height = EditorUtils.Styles.defaultLineSpace + (EditorGUIUtility.standardVerticalSpacing * 2);
                
                var element = m_PassesList.serializedProperty.GetArrayElementAtIndex(index);
                if (element.objectReferenceValue == null)
                    return height;

                if (m_Foldouts[index].value)
                {
                    height += EditorUtils.Styles.defaultLineSpace;
                    var serializedObject = GetElementSO(index);
                    var settingsProp = serializedObject.FindProperty("settings");
                    if (settingsProp != null)
                    {
                        return height + EditorGUI.GetPropertyHeight(settingsProp) +
                               EditorGUIUtility.standardVerticalSpacing;
                    }
                }
                return height;
            };

            m_PassesList.onAddCallback += AddPass;
            m_PassesList.onRemoveCallback = RemovePass;
            m_PassesList.onReorderCallbackWithDetails += ReorderPass;
		    
            m_PassesList.drawHeaderCallback = (Rect testHeaderRect) => {
                EditorGUI.LabelField(testHeaderRect, Styles.RenderFeatures);
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            if(m_PassesList == null)
                OnValidate();
            
            m_PassesList.DoLayoutList();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void CreateFoldoutBools()
        {
            m_Foldouts = new SavedBool[m_RenderPasses.arraySize];
            for (var i = 0; i < m_RenderPasses.arraySize; i++)
            {
                var name = m_RenderPasses.serializedObject.targetObject.name;
                m_Foldouts[i] =
                    new SavedBool($"{name}.ELEMENT{i.ToString()}.PassFoldout", false);
            }
        }
        
        private void AddPass(ReorderableList list)
        {
            var menu = new GenericMenu();

            foreach (Type type in 
                AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes())
                    .Where(myType => myType.IsClass &&
                                     !myType.IsAbstract &&
                                     myType.IsSubclassOf(typeof(ScriptableRendererFeature))))
            {
                var path = type.Name;
                if (type.Namespace != null)
                {
                    var nameSpace = type.Namespace;
                    if (nameSpace == typeof(ScriptableRendererFeature).Namespace)
                        nameSpace = nameSpace.Split('.').Last();
                    nameSpace = nameSpace.Replace('.', '/');
                    path = string.Format($"{nameSpace}/{path}");
                }
                menu.AddItem(new GUIContent(path), false, AddPassHandler, type.Name);
            }
            menu.ShowAsContext();
        }

        private void RemovePass(ReorderableList list)
        {
            var obj = m_RenderPasses.GetArrayElementAtIndex(list.index).objectReferenceValue;
            if (EditorUtility.DisplayDialog("Removing Render Pass Feature",
                $"Are you sure you want to remove the pass {obj.name}, this operation cannot be undone",
                "Remove",
                "Cancel"))
            {
                DestroyImmediate(obj, true);
                AssetDatabase.SaveAssets();
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
                m_RenderPasses.DeleteArrayElementAtIndex(list.index);
                m_RenderPasses.serializedObject.ApplyModifiedProperties();
                m_ElementSOs.Clear();
            }
        }
        
        private void ReorderPass(ReorderableList list, int oldIndex, int newIndex)
        {
            var item = m_ElementSOs[oldIndex];
            m_ElementSOs.RemoveAt(oldIndex);
            m_ElementSOs.Insert(newIndex, item);

            var oldHeaderState = m_Foldouts[oldIndex].value;
            var newHeaderState = m_Foldouts[newIndex].value;
            m_Foldouts[oldIndex].value = newHeaderState;
            m_Foldouts[newIndex].value = oldHeaderState;
        }
        
        private void AddPassHandler(object pass)
        {
            m_RenderPasses.serializedObject.ApplyModifiedProperties();
            
            if (m_PassesList.serializedProperty != null)
            {
                var asset = AssetDatabase.GetAssetOrScenePath(m_RenderPasses.serializedObject.targetObject);
                var obj = CreateInstance((string)pass);
                obj.name = $"New{obj.GetType().Name}";
                AssetDatabase.AddObjectToAsset(obj, asset);
                
                ++m_PassesList.serializedProperty.arraySize;
                m_PassesList.index = m_PassesList.serializedProperty.arraySize - 1;
                m_PassesList.serializedProperty.serializedObject.ApplyModifiedProperties();
                m_PassesList.serializedProperty.GetArrayElementAtIndex(m_PassesList.index).objectReferenceValue = obj;
                m_PassesList.serializedProperty.serializedObject.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
            GetElementSO(m_PassesList.index);
            CreateFoldoutBools();
            EditorUtility.SetDirty(m_RenderPasses.serializedObject.targetObject);
        }
    }
}
