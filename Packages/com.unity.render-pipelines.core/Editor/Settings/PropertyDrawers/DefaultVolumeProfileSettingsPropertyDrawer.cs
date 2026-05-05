using System;
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
        // UUM-77758: Due to how PropertyDrawers are created and cached, there is no way to retrieve them reliably
        // later. We know that only one DefaultVolumeProfile exists at any given time, so we can access it through
        // static variables.
        static SerializedProperty s_DefaultVolumeProfileSerializedProperty;
        static DefaultVolumeProfileEditor s_DefaultVolumeProfileEditor;

        VisualElement m_Root;
        ObjectField m_ObjectField;

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
            if (!RenderPipelineManager.pipelineSwitchCompleted)
                // Defer creation of the UI until the render pipeline is created and VolumeManager is initialized
                RenderPipelineManager.activeRenderPipelineCreated += CreateDefaultVolumeProfileEditor;
            else
                CreateDefaultVolumeProfileEditor();

            m_Root.Add(CreateAssetFieldUI());
            m_Root.Add(m_EditorContainer);

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
            RenderPipelineManager.activeRenderPipelineCreated -= CreateDefaultVolumeProfileEditor;

            VolumeProfile profile = m_VolumeProfileSerializedProperty.objectReferenceValue as VolumeProfile;
            if (profile == null)
                return;

            if (profile == VolumeManager.instance.globalDefaultProfile)
                VolumeProfileUtils.EnsureAllOverridesForDefaultProfile(profile);

            if (s_DefaultVolumeProfileSerializedProperty != m_VolumeProfileSerializedProperty)
            {
                s_DefaultVolumeProfileSerializedProperty = m_VolumeProfileSerializedProperty;
                s_DefaultVolumeProfileEditor = new DefaultVolumeProfileEditor(profile, m_SettingsSerializedObject);
            }
            m_EditorContainer.Add(s_DefaultVolumeProfileEditor.Create());
            m_EditorContainer.Q<HelpBox>("volume-override-info-box").text = volumeInfoBoxLabel.text;

            if (m_DefaultVolumeProfileFoldoutExpanded.value)
                m_EditorContainer.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Destroys the Default Volume Profile editor.
        /// </summary>
        protected void DestroyDefaultVolumeProfileEditor()
        {
            m_EditorContainer.style.display = DisplayStyle.None;
            m_EditorContainer?.Clear();

            if (s_DefaultVolumeProfileEditor != null)
                s_DefaultVolumeProfileEditor.Destroy();
            s_DefaultVolumeProfileEditor = null;
            s_DefaultVolumeProfileSerializedProperty = null;
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
            Object previousValue, IDefaultVolumeProfileSettings defaultVolumeProfileSettings = null)
            where TRenderPipeline : RenderPipeline
        {
            bool confirmed = VolumeProfileUtils.UpdateGlobalDefaultVolumeProfileWithConfirmation<TRenderPipeline>(newValue as VolumeProfile, defaultVolumeProfileSettings?.volumeProfile);
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
            DestroyDefaultVolumeProfileEditor();
            CreateDefaultVolumeProfileEditor();
        }

        /// <summary>
        /// Draw ObjectField for Default Volume.
        /// </summary>
        /// <param name="defaultVolumeProfileSettings">Default value source if available</param>
        /// <typeparam name="TRenderPipeline">Render Pipeline type for Default Volume</typeparam>
        /// <typeparam name="TDefaultVolumeSettings">Default Volume settings container type</typeparam>
        /// <returns>New Object Field</returns>
        protected VisualElement DrawDefaultVolumeObjectField<TRenderPipeline, TDefaultVolumeSettings>(TDefaultVolumeSettings defaultVolumeProfileSettings = null)
            where TRenderPipeline: RenderPipeline
            where TDefaultVolumeSettings : class, IDefaultVolumeProfileSettings
        {
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

                if (RenderPipelineManager.currentPipeline is not TRenderPipeline)
                {
                    m_ObjectField.SetValueWithoutNotify(evt.previousValue);
                    Debug.Log($"Cannot change Default Volume Profile when {typeof(TRenderPipeline).Name} is not active. Rolling back to previous value.");
                    return;
                }

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
                    EditorApplication.delayCall += () => ShowGlobalDefaultVolumeDialog<TRenderPipeline>(m_ObjectField, newValue, oldValue, defaultVolumeProfileSettings);
                    return;
                }

                VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile<TRenderPipeline>(evt.newValue as VolumeProfile, defaultVolumeProfileSettings?.volumeProfile);
                UpdateDefaultVolumeSerializedPropertyAndRecreate(m_ObjectField, evt.newValue);
            });

            m_ObjectField.RegisterCallback<AttachToPanelEvent>(evt =>
            {
                if (GraphicsSettings.currentRenderPipeline == null || RenderPipelineManager.pipelineSwitchCompleted)
                    HandleRenderPipelineChange<TRenderPipeline>();
                RenderPipelineManager.activeRenderPipelineTypeChanged += HandleRenderPipelineChange<TRenderPipeline>;
            });
            m_ObjectField.RegisterCallback<DetachFromPanelEvent>(evt => RenderPipelineManager.activeRenderPipelineTypeChanged -= HandleRenderPipelineChange<TRenderPipeline>);

            profileLine.Add(toggle);
            profileLine.Add(m_ObjectField);

            return profileLine;
        }

        void HandleRenderPipelineChange<TRenderPipeline>()
            where TRenderPipeline: RenderPipeline
        {
            m_ObjectField.enabledSelf = RenderPipelineManager.currentPipeline is TRenderPipeline;
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
                bool canCreateNewAsset = RenderPipelineManager.currentPipeline is TRenderPipeline;
                VolumeProfileUtils.AddVolumeProfileContextMenuItems(ref menu,
                    setting.volumeProfile,
                    s_DefaultVolumeProfileEditor == null ? null : s_DefaultVolumeProfileEditor.allEditors,
                    overrideStateOnReset: true,
                    defaultVolumeProfilePath: defaultVolumeProfilePath,
                    onNewVolumeProfileCreated: createdProfile =>
                    {
                        s_DefaultVolumeProfileSerializedProperty.objectReferenceValue = createdProfile;
                        s_DefaultVolumeProfileSerializedProperty.serializedObject.ApplyModifiedProperties();

                        VolumeProfile initialAsset = null;

                        var initialAssetSettings = EditorGraphicsSettings.GetRenderPipelineSettingsFromInterface<IDefaultVolumeProfileAsset>();
                        if (initialAssetSettings.Length > 0)
                        {
                            if (initialAssetSettings.Length > 1)
                                throw new InvalidOperationException("Found multiple settings implementing IDefaultVolumeProfileAsset, expected only one");
                            initialAsset = initialAssetSettings[0].defaultVolumeProfile;
                        }
                        VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile<TRenderPipeline>(createdProfile, initialAsset);
                    },
                    onComponentEditorsExpandedCollapsed: s_DefaultVolumeProfileEditor == null ? null : s_DefaultVolumeProfileEditor.RebuildListViews,
                    canCreateNewAsset);
            }
        }
    }
}
