using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting.APIUpdating;

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

        private SerializedProperty m_RenderPasses;
        private SerializedProperty m_RenderPassMap;
        private SerializedProperty m_FalseBool;
        private bool m_SaveAsset;
        [SerializeField] private bool falseBool = false;

        private void OnEnable()
        {
            m_RenderPasses = serializedObject.FindProperty(nameof(ScriptableRendererData.m_RendererFeatures));
            m_RenderPassMap = serializedObject.FindProperty(nameof(ScriptableRendererData.m_RendererFeatureMap));
            var editorObj = new SerializedObject(this);
            m_FalseBool =  editorObj.FindProperty(nameof(falseBool));
        }

        public override void OnInspectorGUI()
        {
            if(m_RenderPasses == null)
                OnEnable();
            serializedObject.Update();
            DrawRendererFeatureList();

            if(serializedObject.hasModifiedProperties)
                serializedObject.ApplyModifiedProperties();

            if (m_SaveAsset)
            {
                m_SaveAsset = false;
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
            }
        }

        private void DrawRendererFeatureList()
        {
            EditorGUILayout.LabelField(Styles.RenderFeatures, EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (m_RenderPasses.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No Renderer Features added", MessageType.Info);
            }
            else
            {
                //Draw List
                CoreEditorUtils.DrawSplitter();
                for (int i = 0; i < m_RenderPasses.arraySize; i++)
                {
                    var prop = m_RenderPasses.GetArrayElementAtIndex(i);
                    DrawRendererFeature(i, ref prop);
                    CoreEditorUtils.DrawSplitter();
                }
            }
            EditorGUILayout.Space();

            //Add renderer
            if (GUILayout.Button("Add Renderer Feature", EditorStyles.miniButton))
            {
                AddPassMenu();
            }
        }

        private void DrawRendererFeature(int index, ref SerializedProperty prop)
        {
            var obj = prop.objectReferenceValue;
            var title = ObjectNames.GetInspectorTitle(obj);

            if (obj != null)
            {
                var editor = CreateEditor(obj);
                var serializedFeature = new SerializedObject(obj);
                // Foldout header
                EditorGUI.BeginChangeCheck();
                var displayContent = CoreEditorUtils.DrawHeaderToggle(
                    title,
                    prop,
                    serializedFeature.FindProperty("m_Active"),
                    pos => OnContextClick(pos, index)
                );
                if (EditorGUI.EndChangeCheck())
                    m_SaveAsset = true;

                // ObjectEditor
                if (displayContent)
                {
                    EditorGUI.BeginChangeCheck();
                    var propertyName = serializedFeature.FindProperty("m_Name");
                    propertyName.stringValue = ValidateName(EditorGUILayout.DelayedTextField(Styles.PassNameField, propertyName.stringValue));
                    if (EditorGUI.EndChangeCheck())
                        m_SaveAsset = true;
                    editor.DrawDefaultInspector();
                }

                //Save the changed data
                if (!serializedFeature.hasModifiedProperties) return;
                serializedFeature.ApplyModifiedProperties();
                m_SaveAsset = true;
            }
            else
            {
                CoreEditorUtils.DrawHeaderToggle(
                    Styles.MissingFeature,
                    prop,
                    m_FalseBool,
                    pos => OnContextClick(pos, index)
                );
                m_FalseBool.boolValue = false; // always make sure false bool is false
                EditorGUILayout.HelpBox(Styles.MissingFeature.tooltip, MessageType.Error);
                if (GUILayout.Button("Attempt Fix", EditorStyles.miniButton))
                {
                    var data = target as ScriptableRendererData;
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

            if (id == m_RenderPasses.arraySize - 1)
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Move Down"));
            else
                menu.AddItem(EditorGUIUtility.TrTextContent("Move Down"), false, () => MoveComponent(id, 1));

            menu.AddSeparator(string.Empty);
            menu.AddItem(EditorGUIUtility.TrTextContent("Remove"), false, () => RemoveComponent(id));

            menu.DropDown(new Rect(position, Vector2.zero));
        }

        private void AddPassMenu()
        {
            var menu = new GenericMenu();
            var types = TypeCache.GetTypesDerivedFrom<ScriptableRendererFeature>();
            foreach (Type type in types)
            {
                string path = GetMenuNameFromType(type);
                menu.AddItem(new GUIContent(path), false, AddComponent, type.Name);
            }
            menu.ShowAsContext();
        }

        private void AddComponent(object type)
        {
            serializedObject.Update();

            var component = CreateInstance((string)type);
            component.name = $"New{(string)type}";
            Undo.RegisterCreatedObjectUndo(component, "Add Renderer Feature");

            // Store this new effect as a sub-asset so we can reference it safely afterwards
            // Only when we're not dealing with an instantiated asset
            if (EditorUtility.IsPersistent(target))
                AssetDatabase.AddObjectToAsset(component, target);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(component, out var guid, out long localId);

            // Grow the list first, then add - that's how serialized lists work in Unity
            m_RenderPasses.arraySize++;
            var componentProp = m_RenderPasses.GetArrayElementAtIndex(m_RenderPasses.arraySize - 1);
            componentProp.objectReferenceValue = component;

            // Update GUID Map
            m_RenderPassMap.arraySize++;
            var guidProp = m_RenderPassMap.GetArrayElementAtIndex(m_RenderPassMap.arraySize - 1);
            guidProp.longValue = localId;

            serializedObject.ApplyModifiedProperties();

            // Force save / refresh
            if (EditorUtility.IsPersistent(target))
            {
                m_SaveAsset = true;
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void RemoveComponent(int id)
        {
            var property = m_RenderPasses.GetArrayElementAtIndex(id);
            var component = property.objectReferenceValue;
            property.objectReferenceValue = null;

            Undo.SetCurrentGroupName(component == null ? "Remove Renderer Feature" : $"Remove {component.name}");

            // remove the array index itself from the list
            m_RenderPasses.DeleteArrayElementAtIndex(id);
            m_RenderPassMap.DeleteArrayElementAtIndex(id);
            serializedObject.ApplyModifiedProperties();

            // Destroy the setting object after ApplyModifiedProperties(). If we do it before, redo
            // actions will be in the wrong order and the reference to the setting object in the
            // list will be lost.
            if (component != null) { Undo.DestroyObjectImmediate(component); }

            // Force save / refresh
            m_SaveAsset = true;
        }

        private void MoveComponent(int id, int offset)
        {
            Undo.SetCurrentGroupName("Move Render Feature");
            serializedObject.Update();
            m_RenderPasses.MoveArrayElement(id, id + offset);
            m_RenderPassMap.MoveArrayElement(id, id + offset);
            serializedObject.ApplyModifiedProperties();
            // Force save / refresh
            m_SaveAsset = true;
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
    }
}
