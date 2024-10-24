using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Base implementation for drawing Default Volume Profile UI in Graphics Settings.
    /// </summary>
    public abstract class DefaultVolumeProfileSettingsPropertyDrawer : PropertyDrawer
    {
        // UUM-77758: Due to how PropertyDrawers are created and cached, there is no way to retrieve them reliably
        // later. We know that only one DefaultVolumeProfile exists at any given time, so we can access it through
        // static variables.
        static SerializedProperty s_DefaultVolumeProfileSerializedProperty;
        static DefaultVolumeProfileEditor s_DefaultVolumeProfileEditor;

        VisualElement m_Root;

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
        /// Implementation of the Default Volume Profile asset field.
        /// </summary>
        /// <returns>VisualElement containing the UI</returns>
        protected abstract VisualElement CreateAssetFieldUI();

        /// <summary>
        /// Context menu implementation for Default Volume Profile.
        /// </summary>
        /// <typeparam name="TSetting">Default Volume Profile Settings type</typeparam>
        /// <typeparam name="TRenderPipeline">Render Pipeline type</typeparam>
        public abstract class DefaultVolumeProfileSettingsContextMenu<TSetting, TRenderPipeline> : IRenderPipelineGraphicsSettingsContextMenu<TSetting>
            where TSetting : class, IDefaultVolumeProfileSettings
            where TRenderPipeline : RenderPipeline
        {
            /// <summary>
            /// Path where new Default Volume Profile will be created.
            /// </summary>
            protected abstract string defaultVolumeProfilePath { get; }

            void IRenderPipelineGraphicsSettingsContextMenu<TSetting>.PopulateContextMenu(TSetting setting, PropertyDrawer _, ref GenericMenu menu)
            {
                bool canCreateNewAsset = RenderPipelineManager.currentPipeline is TRenderPipeline;
                VolumeProfileUtils.AddVolumeProfileContextMenuItems(ref menu,
                    setting.volumeProfile,
                    s_DefaultVolumeProfileEditor.allEditors,
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
                    onComponentEditorsExpandedCollapsed: s_DefaultVolumeProfileEditor.RebuildListViews,
                    canCreateNewAsset);
            }
        }
    }
}
