using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Base implementation for drawing Default Volume Profile UI in Graphics Settings.
    /// </summary>
    public abstract partial class DefaultVolumeProfileSettingsPropertyDrawer : PropertyDrawer
    {
        // UUM-77758: PropertyDrawers are cached internally by the editor and the reference is unreliable
        // after domain reload. The context menu (a separate static class) needs to find the drawer for a given
        // pipeline to push assignments back into its SerializedProperty/ObjectField; we register each drawer here
        // by its pipeline type so the lookup doesn't rely on holding a stale drawer reference.
        static readonly Dictionary<Type, DefaultVolumeProfileSettingsPropertyDrawer> k_DrawersByPipelineType = new();

        VisualElement m_Root;
        ObjectField m_ObjectField;
        Type m_PipelineAssetType;
        Type m_PipelineType;
        DefaultVolumeProfileEditor m_Editor;

        /// <summary>SerializedObject representing the settings object</summary>
        protected SerializedObject m_SettingsSerializedObject;
        /// <summary>SerializedProperty representing the Default Volume Profile</summary>
        protected SerializedProperty m_VolumeProfileSerializedProperty;
        /// <summary>Foldout state</summary>
        protected EditorPrefBool m_DefaultVolumeProfileFoldoutExpanded;
        /// <summary>VisualElement containing the DefaultVolumeProfileEditor</summary>
        protected VisualElement m_EditorContainer;
        /// <summary>Default Volume Profile label width</summary>
        protected const int k_DefaultVolumeLabelWidth = 260;
        /// <summary>Info box message</summary>
        protected abstract GUIContent volumeInfoBoxLabel { get; }

        /// <summary>Label and tooltip used for the Default Volume Profile asset field.</summary>
        protected abstract GUIContent defaultVolumeProfileAssetLabel { get;  }

        /// <summary>
        /// CreatePropertyGUI implementation.
        /// </summary>
        /// <param name="property">Property to create UI for</param>
        /// <returns>VisualElement containing the created UI</returns>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            m_Root = new VisualElement();

            var header = CreateHeader();
            if (header != null)
                m_Root.Add(header);

            m_SettingsSerializedObject = property.serializedObject;
            m_VolumeProfileSerializedProperty = property.FindPropertyRelative("m_VolumeProfile");
            m_DefaultVolumeProfileFoldoutExpanded = new EditorPrefBool($"{GetType()}.DefaultVolumeProfileFoldoutExpanded", true);

            m_EditorContainer = new VisualElement();

            m_Root.Add(CreateAssetFieldUI());
            m_Root.Add(m_EditorContainer);

            // All RP/undo subscriptions are scoped to attach/detach: detach fires before domain reload, giving us a
            // chance to DestroyImmediate the child Editor instances before Unity tries to revive them post-reload
            // (where their VolumeComponent targets would be null).
            m_Root.RegisterCallback<AttachToPanelEvent>(_ => OnAttach());
            m_Root.RegisterCallback<DetachFromPanelEvent>(_ => OnDetach());

            return m_Root;
        }

        /// <summary>
        /// Creates the header for the Volume Profile editor.
        /// </summary>
        /// <returns>VisualElement containing the header. Null for no header.</returns>
        protected virtual VisualElement CreateHeader() => null;

        /// <summary>
        /// Creates the Default Volume Profile editor.
        /// </summary>
        protected void CreateDefaultVolumeProfileEditor()
        {
            // Tear down any previous editor before rebuilding — otherwise pipeline-switch / re-entry paths leak
            // DefaultVolumeProfileEditor instances (the UI is cleared but the Editor stays referenced by m_Editor).
            DestroyDefaultVolumeProfileEditor();

            // VolumeManager isn't ready yet (or the active RP isn't ours): show the warning and let the
            // attach-scoped activeRenderPipelineCreated subscription re-invoke us when it becomes ready.
            if (!VolumeManager.instance.isInitialized ||
                (m_PipelineAssetType != null && GraphicsSettings.currentRenderPipelineAssetType != m_PipelineAssetType))
            {
                ShowNoActivePipelineWarning();
                return;
            }

            VolumeProfile profile = m_VolumeProfileSerializedProperty.objectReferenceValue as VolumeProfile;
            if (profile == null)
                return;

            if (profile == VolumeManager.instance.globalDefaultProfile)
                VolumeProfileUtils.EnsureAllOverridesForDefaultProfile(profile, m_PipelineAssetType);

            m_Editor = new DefaultVolumeProfileEditor(profile, m_SettingsSerializedObject);
            m_EditorContainer.Add(m_Editor.Create());
            m_EditorContainer.Q<HelpBox>("volume-override-info-box").text = volumeInfoBoxLabel.text;

            ShowEditorContainerIfExpanded();
        }

        /// <summary>
        /// Destroys the Default Volume Profile editor.
        /// </summary>
        protected void DestroyDefaultVolumeProfileEditor()
        {
            if (m_EditorContainer != null)
            {
                m_EditorContainer.style.display = DisplayStyle.None;
                m_EditorContainer.Clear();
            }

            m_Editor?.Destroy();
            m_Editor = null;
        }

        void OnAttach()
        {
            if (m_PipelineType != null)
                k_DrawersByPipelineType[m_PipelineType] = this;

            RenderPipelineManager.activeRenderPipelineCreated += CreateDefaultVolumeProfileEditor;
            RenderPipelineManager.activeRenderPipelineTypeChanged += CreateDefaultVolumeProfileEditor;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;

            CreateDefaultVolumeProfileEditor();
        }

        void OnDetach()
        {
            RenderPipelineManager.activeRenderPipelineCreated -= CreateDefaultVolumeProfileEditor;
            RenderPipelineManager.activeRenderPipelineTypeChanged -= CreateDefaultVolumeProfileEditor;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;

            DestroyDefaultVolumeProfileEditor();

            // Only clear the slot if we still own it — a re-attach of another drawer for the same pipeline
            // type may already have overwritten us.
            if (m_PipelineType != null && k_DrawersByPipelineType.TryGetValue(m_PipelineType, out var registered) && registered == this)
                k_DrawersByPipelineType.Remove(m_PipelineType);
        }

        void OnUndoRedoPerformed()
        {
            // The ObjectField and embedded editor aren't bound to the SerializedProperty, so undo/redo of the
            // profile assignment leaves the UI stale until the Graphics page is reopened. Update() re-pulls
            // the underlying object so we observe the post-undo value instead of a cached one.
            m_VolumeProfileSerializedProperty.serializedObject.Update();
            var newProfile = m_VolumeProfileSerializedProperty.objectReferenceValue as VolumeProfile;
            if (m_ObjectField.value as VolumeProfile == newProfile)
                return;

            m_ObjectField.SetValueWithoutNotify(newProfile);
            CreateDefaultVolumeProfileEditor();
        }

        void ShowNoActivePipelineWarning()
        {
            m_EditorContainer.Add(new HelpBox(L10n.Tr("Default Volume Profile requires an active Scriptable Render Pipeline."), HelpBoxMessageType.Warning));
            ShowEditorContainerIfExpanded();
        }

        void ShowEditorContainerIfExpanded()
        {
            if (m_DefaultVolumeProfileFoldoutExpanded.value)
                m_EditorContainer.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Show modal dialog to confirm update of selected volume if needed and apply new volume profile.
        /// </summary>
        /// <param name="field">Object Field used to display Default Volume profile</param>
        /// <param name="newValue">New Volume profile</param>
        /// <param name="previousValue">Previous volume profile</param>
        /// <param name="defaultVolumeProfileSettings">Optionally provided default volume profile to extract default values</param>
        /// <typeparam name="TRenderPipeline">Render Pipeline type</typeparam>
        void ShowGlobalDefaultVolumeDialog<TRenderPipeline>(ObjectField field, Object newValue,
            Object previousValue, Type pipelineAssetType, IDefaultVolumeProfileSettings defaultVolumeProfileSettings = null)
            where TRenderPipeline : RenderPipeline
        {
            bool confirmed = VolumeProfileUtils.UpdateGlobalDefaultVolumeProfileWithConfirmation<TRenderPipeline>(newValue as VolumeProfile, pipelineAssetType, defaultVolumeProfileSettings?.volumeProfile);
            if (confirmed)
            {
                UpdateDefaultVolumeSerializedPropertyAndRecreate(field, newValue);
            }
            else
            {
                m_VolumeProfileSerializedProperty.objectReferenceValue = previousValue;
                m_VolumeProfileSerializedProperty.serializedObject.ApplyModifiedProperties();
                field.SetValueWithoutNotify(previousValue);
                // Update the ObjectSelector's visual selection if it's still open
                if (previousValue != null && ObjectSelector.isVisible)
                    ObjectSelector.SetVisualSelection(previousValue.GetEntityId());
            }
        }

        /// <summary>
        /// Update serialized property for Default Volume profile and recreate related Editors
        /// </summary>
        /// <param name="field">Object Field used to display Default Volume profile</param>
        /// <param name="newValue">New Volume profile</param>
        void UpdateDefaultVolumeSerializedPropertyAndRecreate(ObjectField field, Object newValue)
        {
            m_VolumeProfileSerializedProperty.objectReferenceValue = newValue;
            m_VolumeProfileSerializedProperty.serializedObject.ApplyModifiedProperties();
            field.SetValueWithoutNotify(newValue);
            CreateDefaultVolumeProfileEditor();
        }

        // Bridge for the static context-menu class, which can't otherwise reach instance state on the drawer.
        internal void AssignProfileFromExternal(VolumeProfile newProfile)
        {
            UpdateDefaultVolumeSerializedPropertyAndRecreate(m_ObjectField, newProfile);
        }

        /// <summary>
        /// Draw ObjectField for Default Volume.
        /// </summary>
        /// <param name="defaultVolumeProfileSettings">Default value source if available</param>
        /// <typeparam name="TRenderPipeline">Render Pipeline type for Default Volume</typeparam>
        /// <typeparam name="TDefaultVolumeSettings">Default Volume settings container type</typeparam>
        /// <returns>New Object Field</returns>
        [Obsolete("Use the overload that takes TRenderPipelineAsset instead, so Default Volume Profile changes work when the render pipeline is assigned but not yet active. #from(6000.6)")]
        protected VisualElement DrawDefaultVolumeObjectField<TRenderPipeline, TDefaultVolumeSettings>(TDefaultVolumeSettings defaultVolumeProfileSettings = null)
            where TRenderPipeline : RenderPipeline
            where TDefaultVolumeSettings : class, IDefaultVolumeProfileSettings
            => DrawDefaultVolumeObjectField<TRenderPipeline, TDefaultVolumeSettings>(GraphicsSettings.currentRenderPipelineAssetType, defaultVolumeProfileSettings);

        /// <summary>
        /// Draw ObjectField for Default Volume, scoped to the given pipeline asset type so the missing-types
        /// check uses the correct pipeline regardless of which RP is currently active.
        /// </summary>
        /// <param name="defaultVolumeProfileSettings">Default value source if available</param>
        /// <typeparam name="TRenderPipeline">Render Pipeline type for Default Volume</typeparam>
        /// <typeparam name="TRenderPipelineAsset">Render Pipeline Asset type — used to resolve compatible
        /// VolumeComponents independently of the currently-active pipeline</typeparam>
        /// <typeparam name="TDefaultVolumeSettings">Default Volume settings container type</typeparam>
        /// <returns>New Object Field</returns>
        protected VisualElement DrawDefaultVolumeObjectField<TRenderPipeline, TRenderPipelineAsset, TDefaultVolumeSettings>(TDefaultVolumeSettings defaultVolumeProfileSettings = null)
            where TRenderPipeline : RenderPipeline
            where TRenderPipelineAsset : RenderPipelineAsset
            where TDefaultVolumeSettings : class, IDefaultVolumeProfileSettings
            => DrawDefaultVolumeObjectField<TRenderPipeline, TDefaultVolumeSettings>(typeof(TRenderPipelineAsset), defaultVolumeProfileSettings);

        VisualElement DrawDefaultVolumeObjectField<TRenderPipeline, TDefaultVolumeSettings>(Type pipelineAssetType, TDefaultVolumeSettings defaultVolumeProfileSettings = null)
            where TRenderPipeline: RenderPipeline
            where TDefaultVolumeSettings : class, IDefaultVolumeProfileSettings
        {
            m_PipelineAssetType = pipelineAssetType;
            m_PipelineType = typeof(TRenderPipeline);

            VisualElement profileLine = new();
            var toggle = new Toggle();
            toggle.AddToClassList(Foldout.toggleUssClassName);
            var checkmark = toggle.Q(className: Toggle.checkmarkUssClassName);
            checkmark.AddToClassList(Foldout.checkmarkUssClassName);
            m_ObjectField = new ObjectField(defaultVolumeProfileAssetLabel.text)
            {
                tooltip = defaultVolumeProfileAssetLabel.tooltip,
                objectType = typeof(VolumeProfile),
                value = m_VolumeProfileSerializedProperty.objectReferenceValue as VolumeProfile,
                style =
                {
                    flexShrink = 1,
                }
            };
            m_ObjectField.AddToClassList("unity-base-field__aligned"); //Align with other BaseField<T>
            m_ObjectField.Q<Label>().RegisterCallback<ClickEvent>(evt => toggle.value ^= true);

            toggle.RegisterValueChangedCallback(evt =>
            {
                m_EditorContainer.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                m_DefaultVolumeProfileFoldoutExpanded.value = evt.newValue;
            });
            toggle.SetValueWithoutNotify(m_DefaultVolumeProfileFoldoutExpanded.value);
            m_EditorContainer.style.display = m_DefaultVolumeProfileFoldoutExpanded.value ? DisplayStyle.Flex : DisplayStyle.None;

            profileLine.style.flexDirection = FlexDirection.Row;
            m_ObjectField.style.flexGrow = 1;

            m_ObjectField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == evt.previousValue)
                    return;

                if (evt.newValue == null)
                {
                    m_ObjectField.SetValueWithoutNotify(evt.previousValue);
                    Debug.Log("This Volume Profile Asset cannot be null. Rolling back to previous value.");
                    return;
                }

                if (evt.previousValue != null)
                {
                    var newValue = evt.newValue;
                    var oldValue = evt.previousValue;
                    EditorApplication.delayCall += () => ShowGlobalDefaultVolumeDialog<TRenderPipeline>(m_ObjectField, newValue, oldValue, pipelineAssetType, defaultVolumeProfileSettings);
                    return;
                }

                VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile<TRenderPipeline>(evt.newValue as VolumeProfile, pipelineAssetType, defaultVolumeProfileSettings?.volumeProfile);
                UpdateDefaultVolumeSerializedPropertyAndRecreate(m_ObjectField, evt.newValue);
            });

            profileLine.Add(toggle);
            profileLine.Add(m_ObjectField);

            return profileLine;
        }

        /// <summary>
        /// Implementation of the Default Volume Profile asset field.
        /// </summary>
        /// <returns>VisualElement containing the UI</returns>
        protected abstract VisualElement CreateAssetFieldUI();

        /// <summary>
        /// Context menu implementation for Default Volume Profile.
        /// </summary>
        /// <typeparam name="TSetting">Default Volume Profile Settings type</typeparam>
        /// <typeparam name="TRenderPipeline">Render Pipeline type</typeparam>
        public abstract class DefaultVolumeProfileSettingsContextMenu2<TSetting, TRenderPipeline> : IRenderPipelineGraphicsSettingsContextMenu2<TSetting>
            where TSetting : class, IDefaultVolumeProfileSettings
            where TRenderPipeline : RenderPipeline
        {
            /// <summary>
            /// Path where new Default Volume Profile will be created.
            /// </summary>
            protected abstract string defaultVolumeProfilePath { get; }

            void IRenderPipelineGraphicsSettingsContextMenu2<TSetting>.PopulateContextMenu(TSetting setting, SerializedProperty _, ref GenericMenu menu)
            {
                k_DrawersByPipelineType.TryGetValue(typeof(TRenderPipeline), out var drawer);
                var editor = drawer?.m_Editor;
                // New / Clone are enabled even when TRenderPipeline isn't active: the asset write goes through the
                // explicit-pipeline overload below, and the drawer registry routes the assignment back to the right
                // drawer regardless of which pipeline owns the live VolumeManager. m_PipelineAssetType must also be
                // set (it's used by UpdateGlobalDefaultVolumeProfile to scope the missing-components check).
                bool canCreateNewAsset = drawer != null && drawer.m_PipelineAssetType != null;
                VolumeProfileUtils.AddVolumeProfileContextMenuItems(ref menu,
                    setting.volumeProfile,
                    editor?.allEditors,
                    overrideStateOnReset: true,
                    defaultVolumeProfilePath: defaultVolumeProfilePath,
                    onNewVolumeProfileCreated: createdProfile =>
                    {
                        VolumeProfile initialAsset = null;

                        var initialAssetSettings = EditorGraphicsSettings.GetRenderPipelineSettingsFromInterface<IDefaultVolumeProfileAsset>();
                        if (initialAssetSettings.Length > 0)
                        {
                            if (initialAssetSettings.Length > 1)
                                throw new InvalidOperationException("Found multiple settings implementing IDefaultVolumeProfileAsset, expected only one");
                            initialAsset = initialAssetSettings[0].defaultVolumeProfile;
                        }
                        VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile<TRenderPipeline>(createdProfile, drawer.m_PipelineAssetType, initialAsset);
                        drawer.AssignProfileFromExternal(createdProfile);
                    },
                    onComponentEditorsExpandedCollapsed: editor == null ? null : editor.RebuildListViews,
                    canCreateNewAsset);
            }
        }
    }
}
