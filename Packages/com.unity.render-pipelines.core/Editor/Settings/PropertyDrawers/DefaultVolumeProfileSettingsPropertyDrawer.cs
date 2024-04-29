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
        VisualElement m_Root;
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

            m_Editor = new DefaultVolumeProfileEditor(profile, m_SettingsSerializedObject);
            m_EditorContainer.Add(m_Editor.Create());
            m_EditorContainer.Q<HelpBox>("volume-override-info-box").text = volumeInfoBoxLabel.text;
        }

        /// <summary>
        /// Destroys the Default Volume Profile editor.
        /// </summary>
        protected void DestroyDefaultVolumeProfileEditor()
        {
            if (m_Editor != null)
                m_Editor.Destroy();
            m_Editor = null;
            m_EditorContainer?.Clear();
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

            void IRenderPipelineGraphicsSettingsContextMenu<TSetting>.PopulateContextMenu(TSetting setting, PropertyDrawer drawer, ref GenericMenu menu)
            {
                menu.AddSeparator("");

                var volumeDrawer = drawer as DefaultVolumeProfileSettingsPropertyDrawer;
                bool canCreateNewAsset = RenderPipelineManager.currentPipeline is TRenderPipeline;
                VolumeProfileUtils.AddVolumeProfileContextMenuItems(ref menu,
                    setting.volumeProfile,
                    volumeDrawer.m_Editor.allEditors,
                    overrideStateOnReset: true,
                    defaultVolumeProfilePath: defaultVolumeProfilePath,
                    onNewVolumeProfileCreated: createdProfile =>
                    {
                        volumeDrawer.m_VolumeProfileSerializedProperty.objectReferenceValue = createdProfile;
                        volumeDrawer.m_VolumeProfileSerializedProperty.serializedObject.ApplyModifiedProperties();

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
                    onComponentEditorsExpandedCollapsed: volumeDrawer.m_Editor.RebuildListViews,
                    canCreateNewAsset);
            }
        }
    }
}
