using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Editor script for a <c>ScriptableRendererData</c> class.
    /// </summary>
    [CustomEditor(typeof(ScriptableRendererData), true)]
    public class ScriptableRendererDataEditor : Editor
    {
        class Styles
        {
            public static readonly GUIContent RenderFeatures =
                new GUIContent("Renderer Features",
                    "A Renderer Feature is an asset that lets you add extra Render passes to a URP Renderer and configure their behavior.");

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
        }

        private void OnDisable()
        {
            ClearEditorsList();
        }

        /// <inheritdoc/>
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
                //Draw List
                CoreEditorUtils.DrawSplitter();
                for (int i = 0; i < m_RendererFeatures.arraySize; i++)
                {
                    SerializedProperty renderFeaturesProperty = m_RendererFeatures.GetArrayElementAtIndex(i);
                    DrawRendererFeature(i, ref renderFeaturesProperty);
                    CoreEditorUtils.DrawSplitter();
                }
            }
            EditorGUILayout.Space();

            //Add renderer
            using (var hscope = new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Renderer Feature", EditorStyles.miniButton))
                {
                    var r = hscope.rect;
                    var pos = new Vector2(r.x + r.width / 2f, r.yMax + 18f);
                    FilterWindow.Show(pos, new ScriptableRendererFeatureProvider(this));
                }
            }
        }

        internal bool GetCustomTitle(Type type, out string title)
        {
            var isSingleFeature = type.GetCustomAttribute<DisallowMultipleRendererFeature>();
            if (isSingleFeature != null)
            {
                title = isSingleFeature.customTitle;
                return title != null;
            }
            title = null;
            return false;
        }

        private bool GetTooltip(Type type, out string tooltip)
        {
            var attribute = type.GetCustomAttribute<TooltipAttribute>();
            if (attribute != null)
            {
                tooltip = attribute.tooltip;
                return true;
            }
            tooltip = string.Empty;
            return false;
        }

        private void DrawRendererFeature(int index, ref SerializedProperty renderFeatureProperty)
        {
            Object rendererFeatureObjRef = renderFeatureProperty.objectReferenceValue;
            if (rendererFeatureObjRef != null)
            {
                bool hasChangedProperties = false;
                string title;

                bool hasCustomTitle = GetCustomTitle(rendererFeatureObjRef.GetType(), out title);

                if (!hasCustomTitle)
                {
                    title = ObjectNames.GetInspectorTitle(rendererFeatureObjRef);
                }

                string tooltip;
                GetTooltip(rendererFeatureObjRef.GetType(), out tooltip);

                string helpURL;
                DocumentationUtils.TryGetHelpURL(rendererFeatureObjRef.GetType(), out helpURL);

                // Get the serialized object for the editor script & update it
                Editor rendererFeatureEditor = m_Editors[index];
                SerializedObject serializedRendererFeaturesEditor = rendererFeatureEditor.serializedObject;
                serializedRendererFeaturesEditor.Update();

                // Foldout header
                EditorGUI.BeginChangeCheck();
                SerializedProperty activeProperty = serializedRendererFeaturesEditor.FindProperty("m_Active");
                bool displayContent = CoreEditorUtils.DrawHeaderToggle(EditorGUIUtility.TrTextContent(title, tooltip), renderFeatureProperty, activeProperty, pos => OnContextClick(pos, index), null, null, helpURL);
                hasChangedProperties |= EditorGUI.EndChangeCheck();

                // ObjectEditor
                if (displayContent)
                {
                    if (!hasCustomTitle)
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

            AddShowAdditionalPropertiesMenuItem(ref menu, id);

            menu.AddSeparator(string.Empty);
            menu.AddItem(EditorGUIUtility.TrTextContent("Remove"), false, () => RemoveComponent(id));

            menu.DropDown(new Rect(position, Vector2.zero));
        }

        private void AddShowAdditionalPropertiesMenuItem(ref GenericMenu menu, int id)
        {
            if (m_Editors[id] != null && m_Editors[id].GetType() == typeof(FullScreenPassRendererFeatureEditor))
            {
                var featureReference = m_Editors[id] as FullScreenPassRendererFeatureEditor;
                bool additionalPropertiesAreCurrentlyOn = featureReference.showAdditionalProperties;
                menu.AddItem(EditorGUIUtility.TrTextContent("Show Additional Properties"), additionalPropertiesAreCurrentlyOn, () => featureReference.showAdditionalProperties = !additionalPropertiesAreCurrentlyOn);
            }
        }

        internal void AddComponent(string type)
        {
            serializedObject.Update();

            ScriptableObject component = CreateInstance((string)type);
            component.name = $"{(string)type}";
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

            // Force save / refresh
            if (EditorUtility.IsPersistent(target))
            {
                ForceSave();
            }
            serializedObject.ApplyModifiedProperties();
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

                ScriptableRendererFeature feature = component as ScriptableRendererFeature;
                feature?.Dispose();
            }

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

            // Force save / refresh
            ForceSave();
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
