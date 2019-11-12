using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Text.RegularExpressions;
using UnityEngine.Scripting.APIUpdating;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(ScriptableRendererData), true)]
    [MovedFrom("UnityEditor.Rendering.LWRP")] public class ScriptableRendererDataEditor : Editor
    {
        class Styles
        {
            public static readonly GUIContent RenderFeatures =
                new GUIContent("Renderer Features",
                "Features to include in this renderer.\nTo add or remove features, use the plus and minus at the bottom of this box.");

            public static readonly GUIContent PassNameField =
                new GUIContent("Name", "Render pass name. This name is the name displayed in Frame Debugger.");

            public static GUIStyle BoldLabelSimple;

            static Styles()
            {
                BoldLabelSimple = new GUIStyle(EditorStyles.label);
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

            m_PassesList.drawElementCallback += DrawElementCallback;

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

            m_PassesList.onAddCallback = AddPass;
            m_PassesList.onRemoveCallback = RemovePass;
            m_PassesList.onReorderCallbackWithDetails = ReorderPass;

            m_PassesList.drawHeaderCallback = (Rect testHeaderRect) => {
                GUI.Label(testHeaderRect, Styles.RenderFeatures);
            };
        }

        void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            if(index % 2 != 0)
                    EditorGUI.DrawRect(new Rect(rect.x - 19f, rect.y, rect.width + 23f, rect.height), new Color(0, 0, 0, 0.1f));

            EditorGUI.BeginChangeCheck();
            var element = m_PassesList.serializedProperty.GetArrayElementAtIndex(index);
            var propRect = new Rect(rect.x,
                                    rect.y + EditorGUIUtility.standardVerticalSpacing,
                                    rect.width,
                                    EditorGUIUtility.singleLineHeight);
            var headerRect = new Rect(rect.x,
                                        rect.y + EditorGUIUtility.standardVerticalSpacing,
                                        rect.width,
                                        EditorGUIUtility.singleLineHeight);

            if (element.objectReferenceValue != null)
            {
                // Get the type and append that to the name
                name = $"{element.objectReferenceValue.name} ({element.objectReferenceValue.GetType().Name})";

                GUIContent header = new GUIContent(name,
                    element.objectReferenceValue.GetType().Name);
                m_Foldouts[index].value =
                    EditorGUI.Foldout(headerRect,
                        m_Foldouts[index].value,
                        GUIContent.none,
                        true,
                        Styles.BoldLabelSimple);
                GUI.Label(headerRect, header, Styles.BoldLabelSimple);
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
                        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(target));
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
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if(m_PassesList == null)
                OnValidate();
            if(m_RenderPasses.arraySize != m_Foldouts.Length)
                CreateFoldoutBools();

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
            if (obj != null)
            {
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName($"Delete {obj.name}");
                var groupIndex = Undo.GetCurrentGroup();
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(target));
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
                m_RenderPasses.DeleteArrayElementAtIndex(list.index);
                m_RenderPasses.serializedObject.ApplyModifiedProperties();
                m_ElementSOs.Clear();

                Undo.DestroyObjectImmediate(obj);
                Undo.CollapseUndoOperations(groupIndex);
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
                Undo.SetCurrentGroupName($"Adding {(string)pass}");
                var groupIndex = Undo.GetCurrentGroup();

                var asset = AssetDatabase.GetAssetPath(target);
                var obj = CreateInstance((string)pass);
                obj.name = $"New{obj.GetType().Name}";
                AssetDatabase.AddObjectToAsset(obj, asset);
                Undo.RegisterCreatedObjectUndo(obj, obj.name);

                ++m_PassesList.serializedProperty.arraySize;
                m_PassesList.index = m_PassesList.serializedProperty.arraySize - 1;
                m_PassesList.serializedProperty.serializedObject.ApplyModifiedProperties();
                m_PassesList.serializedProperty.GetArrayElementAtIndex(m_PassesList.index).objectReferenceValue = obj;
                m_PassesList.serializedProperty.serializedObject.ApplyModifiedProperties();
                AssetDatabase.ImportAsset(asset);

                Undo.CollapseUndoOperations(groupIndex);
            }
            m_ElementSOs.Clear();
            GetElementSO(m_PassesList.index);
            CreateFoldoutBools();
            EditorUtility.SetDirty(m_RenderPasses.serializedObject.targetObject);
        }
    }
}
