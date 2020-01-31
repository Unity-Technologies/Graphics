using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering.LWRP;
using System.Text.RegularExpressions;

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

            public static readonly GUIContent PassNameField =
                new GUIContent("Name", "This is the name for the current pass.");

            public static GUIStyle BoldLabelSimple = new GUIStyle(EditorStyles.label);

            static Styles()
            {
                BoldLabelSimple.fontStyle = FontStyle.Bold;
            }
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
                if(index % 2 != 0)
                    EditorGUI.DrawRect(new Rect(rect.x - 19f, rect.y, rect.width + 23f, rect.height), new Color(0, 0, 0, 0.1f));
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
                    var name = element.objectReferenceValue.name;
                    var elementNamespace = element.objectReferenceValue.GetType().Namespace;
                    if (elementNamespace != null && elementNamespace.Contains("Experimental"))
                        name += " (Experimental)";
                    GUIContent header = new GUIContent(name,
                        element.objectReferenceValue.GetType().Name);
                    m_Foldouts[index].value =
                        EditorGUI.Foldout(headerRect,
                            m_Foldouts[index].value,
                            header,
                            true,
                            Styles.BoldLabelSimple);
                    if (m_Foldouts[index].value)
                    {
                        EditorGUI.indentLevel++;
                        propRect.y += EditorUtils.Styles.defaultLineSpace;
                        EditorGUI.BeginChangeCheck();
                        var objName = EditorGUI.DelayedTextField(propRect, Styles.PassNameField,
                            element.objectReferenceValue.name);
                        if (EditorGUI.EndChangeCheck())
                        {
                            objName = ValidatePassName(objName);
                            element.objectReferenceValue.name = objName;
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
                        EditorGUI.indentLevel--;
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

#if UNITY_2019_2_OR_NEWER
            var types = TypeCache.GetTypesDerivedFrom<ScriptableRendererFeature>();
            foreach (Type type in types)
            {
                string path = GetMenuNameFromType(type);
                menu.AddItem(new GUIContent(path), false, AddPassHandler, type.Name);
            }
#else
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }
                foreach (Type type in types.Where(t => t != null))
                {
                    if (type.IsSubclassOf(typeof(ScriptableRendererFeature)))
                    {
                        string path = GetMenuNameFromType(type);
                        menu.AddItem(new GUIContent(path), false, AddPassHandler, type.Name);
                    }
                }
            }
#endif
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

        private string GetMenuNameFromType(Type type)
        {
            var path = type.Name;
            if (type.Namespace != null)
            {
                if (type.Namespace.Contains("Experimental"))
                    path += " (Experimental)";
            }

            // Inserts blank space in between camel case strings
            return Regex.Replace(Regex.Replace(path, "([a-z])([A-Z])", "$1 $2", RegexOptions.Compiled),
                "([A-Z])([A-Z][a-z])", "$1 $2", RegexOptions.Compiled);
        }

        private string ValidatePassName(string name)
        {
            name = Regex.Replace(name, @"[^a-zA-Z0-9 ]", "");
            return name;
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
            m_ElementSOs.Clear();
            GetElementSO(m_PassesList.index);
            CreateFoldoutBools();
            EditorUtility.SetDirty(m_RenderPasses.serializedObject.targetObject);
        }
    }
}
