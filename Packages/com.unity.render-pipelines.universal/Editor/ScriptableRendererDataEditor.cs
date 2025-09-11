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
        bool? m_IsIntermediateTextureForbidden = null;
        [SerializeField] private bool falseBool = false;
        List<Editor> m_Editors = new List<Editor>();

        // Computed on first access on this editor frame, and cleaned at the end of OnInspectorGUI
        /// <summary>
        /// Compute if this ScriptableRenderer is contained by an URPAsset that has IntermediateTextureMode == Never.
        /// </summary>
        protected bool isIntermediateTextureForbidden => m_IsIntermediateTextureForbidden ??= DetermineIfIntermediateTexturesAreForbidden();

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

            // clean cache to force check again on next frame
            m_IsIntermediateTextureForbidden = null;
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
            
        bool DetermineIfIntermediateTexturesAreForbidden()
        {
            var thisRenderer = target as ScriptableRendererData;
            if (thisRenderer == null)
                return false;
            
            // Check current render pipeline asset.
            var currentAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (currentAsset != null)
            {
                foreach (var renderer in currentAsset.rendererDataList)
                {
                    if (renderer != thisRenderer) 
                        continue;
                    
                    if (currentAsset.intermediateTextureMode == IntermediateTextureMode.Never)
                        return true;
                }
            }

            // Determine if IntermediateTexture is forbidden by any Quality Levels
            for (int i = 0; i < QualitySettings.count; ++i)
            {
                var qualityAsset = QualitySettings.GetRenderPipelineAssetAt(i) as UniversalRenderPipelineAsset;
                if (qualityAsset == null)
                    continue;
                
                foreach (var renderer in qualityAsset.rendererDataList)
                {
                    if (renderer != thisRenderer) 
                        continue;

                    if (qualityAsset.intermediateTextureMode == IntermediateTextureMode.Never)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Draws a warning when IntermediateTextureMode is set to Never.
        /// Should be called at the top of the Inspector.
        /// </summary>
        protected void DisplayIntermediateTextureWarnings()
        {
            if (isIntermediateTextureForbidden)
            {
                EditorUtils.QualitySettingsHelpBox(
                    "The active URP Asset is set to never use Intermediate Texture for maximum performance, and contains this Renderer. This prevents the renderer from using the full-screen pass required by some features.\nPost Processing and some RendererFeature are deactivated.",
                    MessageType.Info, 
                    UniversalRenderPipelineAssetUI.Expandable.Quality, 
                    "m_IntermediateTextureMode"
                );
                EditorGUILayout.Space();
            }
        }

        private void DrawRendererFeature(int index, ref SerializedProperty renderFeatureProperty)
        {
            if ((renderFeatureProperty.objectReferenceValue as ScriptableRendererFeature) == null)
            {
                DrawMissingRendererFeature(index, renderFeatureProperty);
                return;
            }
            DrawRendererFeatureUnchecked(index, renderFeatureProperty);
        }
        
        private void DrawRendererFeatureUnchecked(int index, SerializedProperty renderFeatureProperty)
        {
            // Get the serialized object for the editor script & update it
            Editor rendererFeatureEditor = m_Editors[index];
            SerializedObject serializedRendererFeaturesSerializedObject = rendererFeatureEditor.serializedObject;
            serializedRendererFeaturesSerializedObject.Update();

            ScriptableRendererFeature rendererFeature = serializedRendererFeaturesSerializedObject.targetObject as ScriptableRendererFeature;
            bool disabled = isIntermediateTextureForbidden
                && (rendererFeature.useIntermediateTexturesInternal == ScriptableRendererFeature.IntermediateTextureUsage.Required
                    || rendererFeature.deactivatedAfterIntermediateNotAllowed);

            // Foldout header
            EditorGUI.BeginChangeCheck();
            bool displayContent = DrawHeader(index, renderFeatureProperty, serializedRendererFeaturesSerializedObject, disabled, out bool hasCustomTitle);
            bool hasChangedProperties = EditorGUI.EndChangeCheck();

            // ObjectEditor
            if (displayContent)
            {
                if (rendererFeature.useIntermediateTexturesInternal == ScriptableRendererFeature.IntermediateTextureUsage.Unknown)
                    EditorGUILayout.HelpBox("This ScriptableRendererFeature has useIntermediateTexturesInternal set to unknown. It may be deactivated on activation if IntermediateTextureMode is set to Never and require IntermediateTexture.", MessageType.Warning);

                if (!hasCustomTitle)
                {
                    EditorGUI.BeginChangeCheck();
                    SerializedProperty nameProperty = serializedRendererFeaturesSerializedObject.FindProperty("m_Name");
                    nameProperty.stringValue = ValidateName(EditorGUILayout.DelayedTextField(Styles.PassNameField, nameProperty.stringValue));
                    if (EditorGUI.EndChangeCheck())
                    {
                        hasChangedProperties = true;

                        // We need to update sub-asset name
                        serializedRendererFeaturesSerializedObject.targetObject.name = nameProperty.stringValue;
                        AssetDatabase.SaveAssets();

                        // Triggers update for sub-asset name change
                        ProjectWindowUtil.ShowCreatedAsset(target);
                    }
                }

                EditorGUI.BeginChangeCheck();
                rendererFeatureEditor.OnInspectorGUI();
                hasChangedProperties |= EditorGUI.EndChangeCheck();

                EditorGUILayout.Space();
            }

            // Apply changes and save if the user has modified any settings
            if (hasChangedProperties)
            {
                serializedRendererFeaturesSerializedObject.ApplyModifiedProperties();
                serializedObject.ApplyModifiedProperties();
                ForceSave();
            }
        }

        bool DrawHeader(int index, SerializedProperty renderFeatureProperty, SerializedObject serializedRendererFeaturesEditor, bool disabled, out bool hasCustomTitle)
        {
            var rendererFeature = serializedRendererFeaturesEditor.targetObject;
            Type type = rendererFeature.GetType();

            hasCustomTitle = GetCustomTitle(type, out string titleLabel);
            if (!hasCustomTitle)
                titleLabel = ObjectNames.GetInspectorTitle(rendererFeature);
            GetTooltip(type, out string tooltip);
            GUIContent title = EditorGUIUtility.TrTextContent(titleLabel, tooltip);

            DocumentationUtils.TryGetHelpURL(type, out string documentationURL);

            if (!disabled)
            {
                SerializedProperty activeProperty = serializedRendererFeaturesEditor.FindProperty("m_Active");
                return CoreEditorUtils.DrawHeaderToggle(title, renderFeatureProperty, activeProperty, pos => OnContextClick(rendererFeature, pos, index), null, null, documentationURL);
            }

            // ====
            // Manually redraw the header as we cannot just disable only the toggle with ImGUI.
            // Disabling fully the header in a EditorGUI.DisabledScope make it non expendable/collapsable. And buttons become unresponsives.
            // This is temporary while waiting to find time to convert to UITK
            // ====

            // Update GUIContents
            title = UniversalRenderPipelineAssetUI.Styles.GetNoIntermediateTextureVariant(title);
            var documentationIcon = new GUIContent(CoreEditorStyles.iconHelp, $"Open Reference for {titleLabel}.");

            // Compute all rects as in CoreEditorUtils.DrawHeaderToggle
            Rect backgroundRect = EditorGUI.IndentedRect(GUILayoutUtility.GetRect(1f, 17f));

            Rect labelRect = backgroundRect;
            labelRect.xMin += 32f;
            labelRect.xMax -= 20f + 16 + 5;

            Rect foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;

            Rect toggleRect = backgroundRect;
            toggleRect.x += 16f;
            toggleRect.y += 2f;
            toggleRect.width = 13f;
            toggleRect.height = 13f;

            Rect contextMenuRect = new Rect(labelRect.xMax + 3f + 16 + 5, labelRect.y + 1f, 16, 16);


            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            // Draw background
            float backgroundTint = (EditorGUIUtility.isProSkin ? 0.1f : 1f);
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            // Draw Label and Checkbox
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);
                GUI.Toggle(toggleRect, false, GUIContent.none, CoreEditorStyles.smallTickbox);
            }

            // Draw collapsing toggle
            renderFeatureProperty.serializedObject.Update();
            bool expanded = GUI.Toggle(foldoutRect, renderFeatureProperty.isExpanded, GUIContent.none, EditorStyles.foldout);

            // Draw Context menu
            if (GUI.Button(contextMenuRect, CoreEditorStyles.contextMenuIcon, CoreEditorStyles.contextMenuStyle))
                OnContextClick(rendererFeature, new Vector2(contextMenuRect.x, contextMenuRect.yMax), index);

            // Draw Documentation button
            if (!string.IsNullOrEmpty(documentationURL))
            {
                Rect documentationRect = contextMenuRect;
                documentationRect.x -= 16 + 2;
                
                if (GUI.Button(documentationRect, documentationIcon, CoreEditorStyles.iconHelpStyle))
                    Help.BrowseURL(documentationURL);
            }

            // Handle label clicks
            var e = Event.current;
            if (e.type == EventType.MouseDown)
            {
                if (backgroundRect.Contains(e.mousePosition))
                {
                    // Left click: Expand/Collapse
                    if (e.button == 0)
                        expanded ^= true;
                    // Right click: Context menu
                    else 
                        OnContextClick(rendererFeature, e.mousePosition, index);

                    e.Use();
                }
            }

            // Register changes
            renderFeatureProperty.isExpanded = expanded;
            renderFeatureProperty.serializedObject.ApplyModifiedProperties();

            return expanded;
        }

        void DrawMissingRendererFeature(int index, SerializedProperty renderFeatureProperty)
        {
            CoreEditorUtils.DrawHeaderToggle(Styles.MissingFeature, renderFeatureProperty, m_FalseBool, pos => OnContextClick(null, pos, index));
            m_FalseBool.boolValue = false; // always make sure false bool is false
            EditorGUILayout.HelpBox(Styles.MissingFeature.tooltip, MessageType.Error);
            if (GUILayout.Button("Attempt Fix", EditorStyles.miniButton))
            {
                ScriptableRendererData data = target as ScriptableRendererData;
                if (!data.ValidateRendererFeatures())
                {
                    if (EditorUtility.DisplayDialog("Remove Missing Renderer Feature",
                        "This renderer feature script is missing (likely deleted or failed to compile). Do you want to remove it from the list and delete the associated sub-asset?",
                        "Yes", "No"))
                    {
                        data.RemoveMissingRendererFeatures();
                    }
                }
            }
        }

        private void OnContextClick(Object rendererFeatureObject, Vector2 position, int id)
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

            if(rendererFeatureObject?.GetType() == typeof(FullScreenPassRendererFeature))
                menu.AddAdvancedPropertiesBoolMenuItem();

            menu.AddSeparator(string.Empty);
            menu.AddItem(EditorGUIUtility.TrTextContent("Remove"), false, () => RemoveComponent(id));

            menu.DropDown(new Rect(position, Vector2.zero));
        }

        internal void AddComponent(Type type)
        {
            serializedObject.Update();

            ScriptableObject component = CreateInstance(type);
            component.name = $"{type.Name}";
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
