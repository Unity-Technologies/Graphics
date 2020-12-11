using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering.Universal;
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

            public static readonly GUIContent MissingFeature = new GUIContent("Missing RendererFeature",
                "Missing reference, due to compilation issues or missing files. you can attempt auto fix or choose to remove the feature.");

            public static GUIStyle BoldLabelSimple;

            static Styles()
            {
                BoldLabelSimple = new GUIStyle(EditorStyles.label);
                BoldLabelSimple.fontStyle = FontStyle.Bold;
            }
        }

        private SerializedProperty m_RendererFeatures;
        private SerializedProperty m_RendererFeaturesMap;
        private SerializedProperty m_FalseBool;
        [SerializeField] private bool falseBool = false;
        List<Editor> m_Editors = new List<Editor>();

        private void OnEnable()
        {
            m_RendererFeatures = serializedObject.FindProperty(nameof(ScriptableRendererData.m_RendererFeatures));
            m_RendererFeaturesMap = serializedObject.FindProperty(nameof(ScriptableRendererData.m_RendererFeatureMap));
            var editorObj = new SerializedObject(this);
            m_FalseBool = editorObj.FindProperty(nameof(falseBool));
            UpdateEditorList();
            Sort();
        }

        private void OnDisable()
        {
            ClearEditorsList();
        }

        public override void OnInspectorGUI()
        {
            if (m_RendererFeatures == null)
                OnEnable();
            else if (m_RendererFeatures.arraySize != m_Editors.Count)
                UpdateEditorList();

            serializedObject.Update();
            DrawRendererFeatureList();
        }

        private void DrawRendererFeatureList()
        {
            EditorGUILayout.LabelField(Styles.RenderFeatures, EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (m_RendererFeatures.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No Renderer Features added", MessageType.Info);
            }
            else
            {
                int firstOrderedFeature = TryFindFirstOrderedFeature();

                if (firstOrderedFeature != -1)
                {
                    CoreEditorUtils.DrawSplitter();

                    if (firstOrderedFeature != 0)
                    {
                        EditorGUILayout.LabelField("Default", EditorStyles.miniLabel);

                        //Draw List
                        CoreEditorUtils.DrawSplitter();
                        for (int i = 0; i < firstOrderedFeature; i++)
                        {
                            SerializedProperty renderFeaturesProperty = m_RendererFeatures.GetArrayElementAtIndex(i);
                            DrawRendererFeature(i, ref renderFeaturesProperty);
                            CoreEditorUtils.DrawSplitter();
                        }
                    }

                    EditorGUILayout.LabelField("Ordered", EditorStyles.miniLabel);

                    //Draw List
                    CoreEditorUtils.DrawSplitter();
                    for (int i = firstOrderedFeature; i < m_RendererFeatures.arraySize; i++)
                    {
                        SerializedProperty renderFeaturesProperty = m_RendererFeatures.GetArrayElementAtIndex(i);
                        DrawRendererFeature(i, ref renderFeaturesProperty);
                        CoreEditorUtils.DrawSplitter();
                    }
                }
                else
                {
                    //Draw List
                    CoreEditorUtils.DrawSplitter();
                    for (int i = 0; i < m_RendererFeatures.arraySize; i++)
                    {
                        SerializedProperty renderFeaturesProperty = m_RendererFeatures.GetArrayElementAtIndex(i);
                        DrawRendererFeature(i, ref renderFeaturesProperty);
                        CoreEditorUtils.DrawSplitter();
                    }
                }
            }
            EditorGUILayout.Space();

            //Add renderer
            if (GUILayout.Button("Add Renderer Feature", EditorStyles.miniButton))
            {
                AddPassMenu();
            }
        }

        private int TryFindFirstOrderedFeature()
        {
            for (int i = 0; i < m_RendererFeatures.arraySize; i++)
            {
                SerializedProperty renderFeaturesProperty = m_RendererFeatures.GetArrayElementAtIndex(i);
                var rendererFeature = renderFeaturesProperty.objectReferenceValue as ScriptableRendererFeature;
                if (rendererFeature.queueMode != RendererFeatureQueueMode.UsePass)
                {
                    return i;
                }
            }
            return -1;
        }

        private bool HideRendererFeatureNameCheck(Type type)
        {
            var hideFeatureName = type.GetCustomAttribute<DisallowMultipleRendererFeature>();
            return hideFeatureName != null && !hideFeatureName.displayName;
        }

        private void DrawRendererFeature(int index, ref SerializedProperty renderFeatureProperty)
        {
            Object rendererFeatureObjRef = renderFeatureProperty.objectReferenceValue;
            if (rendererFeatureObjRef != null)
            {
                bool hasChangedProperties = false;
                string title = ObjectNames.GetInspectorTitle(rendererFeatureObjRef);

                // Get the serialized object for the editor script & update it
                Editor rendererFeatureEditor = m_Editors[index];
                SerializedObject serializedRendererFeaturesEditor = rendererFeatureEditor.serializedObject;
                serializedRendererFeaturesEditor.Update();

                bool displayName = !HideRendererFeatureNameCheck(rendererFeatureObjRef.GetType());
                if (!displayName)
                    title = rendererFeatureObjRef.GetType().Name;

                SerializedProperty invalidDependencyProperty = serializedRendererFeaturesEditor.FindProperty("m_ValidDependencies");
                GUI.enabled = invalidDependencyProperty.boolValue;

                // Foldout header
                bool displayContent = false;
                EditorGUI.BeginChangeCheck();
                SerializedProperty activeProperty = serializedRendererFeaturesEditor.FindProperty("m_Active");
                displayContent = CoreEditorUtils.DrawHeaderToggle(title, renderFeatureProperty, activeProperty, pos => OnContextClick(pos, index));
                hasChangedProperties |= EditorGUI.EndChangeCheck();

                GUI.enabled = true;

                // ObjectEditor
                if (displayContent)
                {
                    if (displayName)
                    {
                        EditorGUI.BeginChangeCheck();
                        SerializedProperty nameProperty = serializedRendererFeaturesEditor.FindProperty("m_Name");
                        nameProperty.stringValue = ValidateName(EditorGUILayout.DelayedTextField(Styles.PassNameField, nameProperty.stringValue));
                        if (EditorGUI.EndChangeCheck())
                        {
                            hasChangedProperties = true;

                            // We need to update sub-asset name
                            rendererFeatureObjRef.name = nameProperty.stringValue;
                            AssetDatabase.SaveAssets();

                            // Triggers update for sub-asset name change
                            ProjectWindowUtil.ShowCreatedAsset(target);
                        }
                    }

                    EditorGUI.BeginChangeCheck();
                    rendererFeatureEditor.OnInspectorGUI();
                    hasChangedProperties |= EditorGUI.EndChangeCheck();

                    EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
                }

                // Apply changes and save if the user has modified any settings
                if (hasChangedProperties)
                {
                    serializedRendererFeaturesEditor.ApplyModifiedProperties();
                    serializedObject.ApplyModifiedProperties();
                    ForceSave();
                }
            }
            else
            {
                CoreEditorUtils.DrawHeaderToggle(Styles.MissingFeature, renderFeatureProperty, m_FalseBool, pos => OnContextClick(pos, index));
                m_FalseBool.boolValue = false; // always make sure false bool is false
                EditorGUILayout.HelpBox(Styles.MissingFeature.tooltip, MessageType.Error);
                if (GUILayout.Button("Attempt Fix", EditorStyles.miniButton))
                {
                    ScriptableRendererData data = target as ScriptableRendererData;
                    data.ValidateRendererFeatures();
                }
            }
        }

        private void OnContextClick(Vector2 position, int id)
        {
            var menu = new GenericMenu();

            if (id == 0)
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Move Up"));
            else
                menu.AddItem(EditorGUIUtility.TrTextContent("Move Up"), false, () => MoveComponent(id, -1));

            if (id == m_RendererFeatures.arraySize - 1)
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Move Down"));
            else
                menu.AddItem(EditorGUIUtility.TrTextContent("Move Down"), false, () => MoveComponent(id, 1));

            menu.AddSeparator(string.Empty);
            menu.AddItem(EditorGUIUtility.TrTextContent("Remove"), false, () => RemoveComponent(id));

            menu.DropDown(new Rect(position, Vector2.zero));
        }

        private void AddPassMenu()
        {
            GenericMenu menu = new GenericMenu();
            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<ScriptableRendererFeature>();
            foreach (Type type in types)
            {
                var data = target as ScriptableRendererData;
                if (data.DuplicateFeatureCheck(type))
                {
                    continue;
                }

                string path = GetMenuNameFromType(type);
                menu.AddItem(new GUIContent(path), false, AddComponent, type.Name);
            }
            menu.ShowAsContext();
        }

        private void AddComponent(object type)
        {
            serializedObject.Update();

            ScriptableObject component = CreateInstance((string)type);
            component.name = $"New{(string)type}";
            Undo.RegisterCreatedObjectUndo(component, "Add Renderer Feature");

            // Store this new effect as a sub-asset so we can reference it safely afterwards
            // Only when we're not dealing with an instantiated asset
            if (EditorUtility.IsPersistent(target))
            {
                AssetDatabase.AddObjectToAsset(component, target);
            }
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(component, out var guid, out long localId);

            // Grow the list first, then add - that's how serialized lists work in Unity
            m_RendererFeatures.arraySize++;
            SerializedProperty componentProp = m_RendererFeatures.GetArrayElementAtIndex(m_RendererFeatures.arraySize - 1);
            componentProp.objectReferenceValue = component;

            // Update GUID Map
            m_RendererFeaturesMap.arraySize++;
            SerializedProperty guidProp = m_RendererFeaturesMap.GetArrayElementAtIndex(m_RendererFeaturesMap.arraySize - 1);
            guidProp.longValue = localId;
            UpdateEditorList();
            serializedObject.ApplyModifiedProperties();

            Sort();

            // Force save / refresh
            if (EditorUtility.IsPersistent(target))
            {
                ForceSave();
            }
            serializedObject.ApplyModifiedProperties();
        }

        private class SortNode
        {
            public List<SortNode> dependencies;
            public SortItem value;
            public Type type;
            public bool usingOrdering;

            public bool IsAllDependenciesPresent(List<SortNode> list)
            {
                foreach (var dependency in dependencies)
                {
                    var foundNode = list.Find(n => n == dependency);
                    if (foundNode == null)
                        return false;
                }
                return true;
            }
        }

        private struct SortItem
        {
            public Object rendererFeature;
            public long guid;
        }

        private SortNode FindNode(List<SortNode> nodes, List<SortNode> list)
        {
            foreach (var node in nodes)
            {
                if (node.IsAllDependenciesPresent(list))
                {
                    return node;
                }
            }
            return null;
        }

        private void Sort()
        {
            List<SortNode> nodes = new List<SortNode>();

            for (int i = 0; i < m_RendererFeatures.arraySize; i++)
            {
                var element = m_RendererFeatures.GetArrayElementAtIndex(i);
                var rendererFeatureObj = element.objectReferenceValue;
                if (rendererFeatureObj == null)
                    continue;

                var element2 = m_RendererFeaturesMap.GetArrayElementAtIndex(i);
                var guid = element2.longValue;
                var rendererFeatureType = rendererFeatureObj.GetType();

                var d = rendererFeatureObj as ScriptableRendererFeature;
                var usingOrdering = d.queueMode != RendererFeatureQueueMode.UsePass;

                nodes.Add(new SortNode()
                {
                    value = new SortItem()
                    {
                        rendererFeature = rendererFeatureObj,
                        guid = guid,
                    },
                    type = rendererFeatureType,
                    dependencies = new List<SortNode>(),
                    usingOrdering = usingOrdering,
                });
            }

            for (int i = 0; i < m_RendererFeatures.arraySize; i++)
            {
                var node = nodes[i];
                var validDependencies = true;

                if (node.usingOrdering)
                {
                    var executeBeforeRendererFeature = node.type.GetCustomAttributes<ExecuteBeforeRendererFeature>();
                    foreach (var attribute in executeBeforeRendererFeature)
                    {
                        var foundNode = nodes.Find(n => attribute.rendererFeatureType.IsAssignableFrom(n.type));
                        if (foundNode == null || !foundNode.usingOrdering)
                        {
                            if (attribute.isRequired)
                                validDependencies = false;
                            continue;
                        }

                        foundNode.dependencies.Add(node);
                    }

                    var executeAfterRendererFeature = node.type.GetCustomAttributes<ExecuteAfterRendererFeature>();
                    foreach (var attribute in executeAfterRendererFeature)
                    {
                        var foundNode = nodes.Find(n => attribute.rendererFeatureType.IsAssignableFrom(n.type));
                        if (foundNode == null || !foundNode.usingOrdering)
                        {
                            if (attribute.isRequired)
                                validDependencies = false;
                            continue;
                        }

                        node.dependencies.Add(foundNode);
                    }

                    if (node.usingOrdering)
                    {
                        foreach (var node2 in nodes)
                        {
                            if (!node2.usingOrdering)
                                node.dependencies.Add(node2);
                        }
                    }
                }

                var editor = m_Editors[i];
                var property = editor.serializedObject.FindProperty("m_ValidDependencies");
                if (validDependencies != property.boolValue)
                {
                    property.boolValue = validDependencies;
                    editor.serializedObject.ApplyModifiedProperties();
                }
            }

            List<SortNode> list = new List<SortNode>();

            while (nodes.Count != 0)
            {
                var node = FindNode(nodes, list);
                if (node == null)
                {
                    Debug.LogError("Bad execution order");
                    break;
                }

                nodes.Remove(node);
                list.Add(node);
            }

            if (IsDirty(list))
            {
                for (int i = 0; i < m_RendererFeatures.arraySize; i++)
                {
                    var element = m_RendererFeatures.GetArrayElementAtIndex(i);
                    element.objectReferenceValue = list[i].value.rendererFeature;

                    var elemtns2 = m_RendererFeaturesMap.GetArrayElementAtIndex(i);
                    elemtns2.longValue = list[i].value.guid;
                }
                UpdateEditorList();
                serializedObject.ApplyModifiedProperties();
            }
        }

        private bool IsDirty(List<SortNode> list)
        {
            for (int i = 0; i < m_RendererFeatures.arraySize; i++)
            {
                var element = m_RendererFeatures.GetArrayElementAtIndex(i);
                var rendererFeatureObjRef = element.objectReferenceValue;
                if (rendererFeatureObjRef == null)
                    continue;
                if (rendererFeatureObjRef != list[i].value.rendererFeature)
                    return true;
            }
            return false;
        }

        private void RemoveComponent(int id)
        {
            SerializedProperty property = m_RendererFeatures.GetArrayElementAtIndex(id);
            Object component = property.objectReferenceValue;
            property.objectReferenceValue = null;

            Undo.SetCurrentGroupName(component == null ? "Remove Renderer Feature" : $"Remove {component.name}");

            // remove the array index itself from the list
            m_RendererFeatures.DeleteArrayElementAtIndex(id);
            m_RendererFeaturesMap.DeleteArrayElementAtIndex(id);
            UpdateEditorList();
            serializedObject.ApplyModifiedProperties();

            // Destroy the setting object after ApplyModifiedProperties(). If we do it before, redo
            // actions will be in the wrong order and the reference to the setting object in the
            // list will be lost.
            if (component != null)
            {
                Undo.DestroyObjectImmediate(component);
            }

            Sort();

            // Force save / refresh
            ForceSave();
        }

        private void MoveComponent(int id, int offset)
        {
            Undo.SetCurrentGroupName("Move Render Feature");
            serializedObject.Update();
            m_RendererFeatures.MoveArrayElement(id, id + offset);
            m_RendererFeaturesMap.MoveArrayElement(id, id + offset);
            UpdateEditorList();
            serializedObject.ApplyModifiedProperties();

            Sort();

            // Force save / refresh
            ForceSave();
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

        private string ValidateName(string name)
        {
            name = Regex.Replace(name, @"[^a-zA-Z0-9 ]", "");
            return name;
        }

        private void UpdateEditorList()
        {
            ClearEditorsList();
            for (int i = 0; i < m_RendererFeatures.arraySize; i++)
            {
                m_Editors.Add(CreateEditor(m_RendererFeatures.GetArrayElementAtIndex(i).objectReferenceValue));
            }
        }

        //To avoid leaking memory we destroy editors when we clear editors list
        private void ClearEditorsList()
        {
            for (int i = m_Editors.Count - 1; i >= 0; --i)
            {
                DestroyImmediate(m_Editors[i]);
            }
            m_Editors.Clear();
        }

        private void ForceSave()
        {
            EditorUtility.SetDirty(target);
        }
    }
}
