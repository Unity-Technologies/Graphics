using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Editor for the Default Volume Profile
    /// </summary>
    public sealed partial class DefaultVolumeProfileEditor
    {
        const string k_TemplatePath = "Packages/com.unity.render-pipelines.core/Editor/UXML/DefaultVolumeProfileEditor.uxml";

        const int k_ContainerMarginLeft = 27;
        const int k_ImguiContainerPaddingLeft = 18;

        static Lazy<GUIStyle> s_ImguiContainerScopeStyle = new(() => new GUIStyle
        {
            padding = new RectOffset(k_ImguiContainerPaddingLeft, 0, 0, 0)
        });

        static Lazy<VisualTreeAsset> s_Template = new(() => AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TemplatePath));

        readonly Dictionary<VolumeComponentEditor, string> m_VolumeComponentHelpUrls = new();
        readonly VolumeProfile m_Profile;
        readonly SerializedObject m_TargetSerializedObject;
        readonly Editor m_BaseEditor;

        DefaultVolumeProfileCategories m_Categories;
        VisualElement m_Root;
        ToolbarSearchField m_SearchField;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="profile">VolumeProfile to display</param>
        /// <param name="targetSerializedObject">Target serialized object to update when the default volume profile changes</param>
        public DefaultVolumeProfileEditor(VolumeProfile profile, SerializedObject targetSerializedObject)
        {
            m_Profile = profile;
            m_TargetSerializedObject = targetSerializedObject;
        }

        /// <summary>
        /// List of all VolumeComponentEditors
        /// </summary>
        public List<VolumeComponentEditor> allEditors
        {
            get
            {
                var editors = new List<VolumeComponentEditor>();
                foreach (var (_, categoryEditors) in m_Categories.categories)
                {
                    editors.AddRange(categoryEditors);
                }
                return editors;
            }
        }

        /// <summary>
        /// Create the visual hierarchy
        /// </summary>
        /// <returns>Root element of the visual hierarchy</returns>
        public VisualElement Create()
        {
            m_Root = s_Template.Value.Instantiate();

            m_SearchField = m_Root.Q<ToolbarSearchField>();
            m_SearchField.RegisterValueChangedCallback(_ => CreateComponentLists());

            m_Categories = new DefaultVolumeProfileCategories(m_Profile);

            CreateComponentLists();

            Undo.undoRedoPerformed += OnUndoRedoPerformed;

            return m_Root;
        }

        void CreateComponentLists()
        {
            var componentListElement = m_Root.Q("component-list");
            componentListElement.Clear();

            var searchString = m_SearchField.value;

            bool MatchesSearchString(string title)
            {
                return searchString.Length == 0 || title.Contains(searchString, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var (categoryName, categoryEditors) in m_Categories.categories)
            {
                List<VolumeComponentEditor> filteredCategoryEditors = new();
                foreach (var category in categoryEditors)
                {
                    if (MatchesSearchString(category.GetDisplayTitle().text))
                        filteredCategoryEditors.Add(category);
                }

                if (filteredCategoryEditors.Count == 0)
                    continue;

                VisualElement categoryTitleLabel = new Label(categoryName);
                categoryTitleLabel.AddToClassList("category-header");
                componentListElement.Add(categoryTitleLabel);

                Func<VisualElement> makeItem = () =>
                {
                    var container = new IMGUIContainer();
                    container.cullingEnabled = true;
                    return container;
                };
                Action<VisualElement, int> bindItem = (e, i) =>
                {
                    (e as IMGUIContainer).onGUIHandler = () =>
                    {
                        using var indentScope = new SettingsProviderGUIScope();
                        /* values adapted to the ProjectSettings > Graphics */
                        /* they also works in the DefaultVolume inspector */
                        var minWidth = 120;
                        var indent = 66;
                        var ratio = 0.45f;
                        EditorGUIUtility.labelWidth = Mathf.Max(minWidth, (int)((e.worldBound.width - indent) * ratio));
                        VolumeComponentEditorOnGUI(filteredCategoryEditors[i]);
                    };
                };

                ListView listView = new ListView(filteredCategoryEditors, -1, makeItem, bindItem);
                listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
                componentListElement.Add(listView);

                foreach (var editor in filteredCategoryEditors)
                {
                    DocumentationUtils.TryGetHelpURL(editor.volumeComponent.GetType(), out string helpUrl);
                    helpUrl ??= string.Empty;
                    m_VolumeComponentHelpUrls[editor] = helpUrl;
                }
            }
        }

        void OnUndoRedoPerformed()
        {
            VolumeManager.instance.OnVolumeProfileChanged(m_Profile);
        }

        /// <summary>
        /// Destroy all Editors owned by this class
        /// </summary>
        public void Destroy()
        {
            m_Categories.Destroy();

            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        /// <summary>
        /// Rebuild all ListViews to force them to update their expanded state.
        /// </summary>
        public void RebuildListViews()
        {
            // Rebuild is necessary to avoid gradual "animation" effect when collapsing/expanding many foldouts at once.
            // This happens because the items have cullingEnabled=true and are not updated until they come to view.
            m_Root.Query<ListView>().ForEach(l => l.Rebuild());
        }

        void VolumeComponentEditorOnGUI(VolumeComponentEditor editor)
        {
            using (new EditorGUILayout.VerticalScope(s_ImguiContainerScopeStyle.Value))
            {
                using var changedScope = new EditorGUI.ChangeCheckScope();

                CoreEditorUtils.DrawSplitter();

                bool displayContent = CoreEditorUtils.DrawHeaderToggleFoldout(
                    new GUIContent(editor.GetDisplayTitle()),
                    editor.expanded,
                    null,
                    pos => OnVolumeComponentContextClick(pos, editor),
                    editor.hasAdditionalProperties ? () => editor.showAdditionalProperties : null,
                    () => editor.showAdditionalProperties ^= true,
                    m_VolumeComponentHelpUrls[editor]
                );
                if (displayContent ^ editor.expanded)
                    editor.expanded = displayContent;

                if (editor.expanded)
                {
                    using var scope = new EditorGUILayout.VerticalScope();

                    // This rect is drawn to suppress mouse hover highlight in order to match the old imgui
                    // implementation. Not doing this causes visual bugs with AdditionalProperties animations.
                    var highlightSuppressRect = scope.rect;
                    highlightSuppressRect.xMin -= k_ImguiContainerPaddingLeft+k_ContainerMarginLeft;
                    EditorGUI.DrawRect(highlightSuppressRect, CoreEditorStyles.backgroundColor);

                    editor.OnInternalInspectorGUI();
                }

                if (changedScope.changed)
                {
                    m_TargetSerializedObject.ApplyModifiedProperties();
                    VolumeManager.instance.OnVolumeProfileChanged(m_Profile);
                }
            }
        }

        void OnVolumeComponentContextClick(Vector2 position, VolumeComponentEditor targetEditor)
        {
            var targetComponent = targetEditor.volumeComponent;
            var menu = new GenericMenu();

            menu.AddItem(VolumeProfileUtils.Styles.collapseAll, false, () =>
            {
                VolumeProfileUtils.SetComponentEditorsExpanded(allEditors, false);
                RebuildListViews();
            });
            menu.AddItem(VolumeProfileUtils.Styles.expandAll, false, () =>
            {
                VolumeProfileUtils.SetComponentEditorsExpanded(allEditors, true);
                RebuildListViews();
            });
            menu.AddSeparator(string.Empty);

            menu.AddItem(VolumeProfileUtils.Styles.reset, false, () =>
            {
                VolumeProfileUtils.ResetComponentsInternal(targetEditor.serializedObject, m_Profile, new[] { targetComponent }, true);
            });

            menu.AddSeparator(string.Empty);

            if (targetEditor.hasAdditionalProperties)
                menu.AddAdvancedPropertiesBoolMenuItem(() => targetEditor.showAdditionalProperties, () => targetEditor.showAdditionalProperties ^= true);

            menu.AddSeparator(string.Empty);
            menu.AddItem(VolumeProfileUtils.Styles.openInRenderingDebugger, false, DebugDisplaySettingsVolume.OpenInRenderingDebugger);

            menu.AddSeparator(string.Empty);
            menu.AddItem(VolumeProfileUtils.Styles.copySettings, false, () => VolumeComponentCopyPaste.CopySettings(targetComponent));

            if (VolumeComponentCopyPaste.CanPaste(targetComponent))
                menu.AddItem(VolumeProfileUtils.Styles.pasteSettings, false, () => VolumeComponentCopyPaste.PasteSettings(targetComponent, m_Profile));
            else
                menu.AddDisabledItem(VolumeProfileUtils.Styles.pasteSettings);

            menu.DropDown(new Rect(position, Vector2.zero));
        }
    }
}
