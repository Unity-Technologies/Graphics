using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(HDRenderPipelineGlobalSettings))]
    [CanEditMultipleObjects]
    sealed class HDRenderPipelineGlobalSettingsEditor : Editor
    {
        [MenuItem("Assets/Create/Rendering/HDRP Global Settings Asset", priority = CoreUtils.Sections.section4 + 2)]
        internal static void CreateAsset()
        {
            RenderPipelineGlobalSettingsEndNameEditAction.CreateNew<HDRenderPipeline, HDRenderPipelineGlobalSettings>();
        }

        SerializedHDRenderPipelineGlobalSettings m_SerializedHDRenderPipelineGlobalSettings;

        Editor m_LookDevVolumeProfileEditor;

        DefaultVolumeProfileEditor m_DefaultVolumeProfileEditor;
        VisualElement m_DefaultVolumeProfileEditorRoot;
        EditorPrefBool m_DefaultVolumeProfileFoldoutExpanded;

        internal Editor GetLookDevDefaultVolumeProfileEditor(VolumeProfile lookDevAsset)
        {
            CreateCachedEditor(lookDevAsset, typeof(VolumeProfileEditor), ref m_LookDevVolumeProfileEditor);
            return m_LookDevVolumeProfileEditor;
        }

        void OnEnable()
        {
            m_SerializedHDRenderPipelineGlobalSettings = new SerializedHDRenderPipelineGlobalSettings(serializedObject);

            Undo.undoRedoPerformed += OnUndoRedoPerformed;

            m_DefaultVolumeProfileFoldoutExpanded = new EditorPrefBool($"{GetType()}.DefaultVolumeProfileFoldoutExpanded", true);
        }

        private void OnDisable()
        {
            m_SerializedHDRenderPipelineGlobalSettings = null;
            DestroyDefaultVolumeProfileEditor();
            CoreUtils.Destroy(m_LookDevVolumeProfileEditor);

            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        void OnUndoRedoPerformed()
        {
            if (target is HDRenderPipelineGlobalSettings settings)
            {
                if (settings.volumeProfile != VolumeManager.instance.globalDefaultProfile)
                {
                    var globalSettings = HDRenderPipelineGlobalSettings.instance;
                    var defaultValuesAsset = globalSettings != null ? globalSettings.renderPipelineEditorResources.defaultSettingsVolumeProfile : null;
                    VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile<HDRenderPipeline>(settings.volumeProfile, defaultValuesAsset);
                    DestroyDefaultVolumeProfileEditor();
                }
                else
                {
                    VolumeManager.instance.OnVolumeProfileChanged(settings.volumeProfile);
                }
            }
        }

        void DestroyDefaultVolumeProfileEditor()
        {
            if (m_DefaultVolumeProfileEditor != null)
                m_DefaultVolumeProfileEditor.Destroy();
            m_DefaultVolumeProfileEditor = null;
            m_DefaultVolumeProfileEditorRoot?.Clear();
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            root.Add(new IMGUIContainer(() => m_SerializedHDRenderPipelineGlobalSettings.serializedObject.Update()));
            root.Add(CreateVolumeProfileSection());
            root.Add(HDRenderPipelineGlobalSettingsUI.CreateImguiSections(m_SerializedHDRenderPipelineGlobalSettings, this));

            return root;
        }

        #region Default Volume Profile
        void CreateDefaultVolumeProfileEditor()
        {
            Debug.Assert(VolumeManager.instance.isInitialized);
            Debug.Assert(m_DefaultVolumeProfileEditorRoot.childCount == 0);

            var volumeProfile = m_SerializedHDRenderPipelineGlobalSettings.defaultVolumeProfile.objectReferenceValue as VolumeProfile;
            if (volumeProfile == null)
                return;

            if (volumeProfile == VolumeManager.instance.globalDefaultProfile)
                VolumeProfileUtils.EnsureAllOverridesForDefaultProfile(volumeProfile);

            m_DefaultVolumeProfileEditor = new DefaultVolumeProfileEditor(this, volumeProfile);
            m_DefaultVolumeProfileEditorRoot.Add(m_DefaultVolumeProfileEditor.Create());

            m_DefaultVolumeProfileEditorRoot.Q<HelpBox>("volume-override-info-box").text = EditorGUIUtility.TrTextContent(
                "The values in the Default Volume can be overridden by a Volume Profile assigned to HDRP asset and Volumes inside scenes.").text;
        }

        public VisualElement CreateVolumeProfileSection()
        {
            var section = new VisualElement();
            m_DefaultVolumeProfileEditorRoot = new VisualElement();

            if (VolumeManager.instance.isInitialized)
                CreateDefaultVolumeProfileEditor();

            section.AddToClassList("volume-profile-section");
            section.Add(new IMGUIContainer(() =>
            {
                using var changedScope = new EditorGUI.ChangeCheckScope();

                CoreEditorUtils.DrawSectionHeader(
                    HDRenderPipelineGlobalSettingsUI.Styles.defaultVolumeProfileSectionLabel,
                    Documentation.GetPageLink(HDRenderPipelineGlobalSettingsUI.DocumentationUrls.k_Volumes),
                    pos => OnVolumeProfileSectionContextClick(pos, m_SerializedHDRenderPipelineGlobalSettings, m_DefaultVolumeProfileEditor));
                EditorGUILayout.Space();
                DrawVolumeSection(m_SerializedHDRenderPipelineGlobalSettings);

                // Propagate foldout expander state from IMGUI to UITK
                m_DefaultVolumeProfileEditorRoot.style.display =
                    m_DefaultVolumeProfileFoldoutExpanded.value ? DisplayStyle.Flex : DisplayStyle.None;

                if (changedScope.changed)
                    m_SerializedHDRenderPipelineGlobalSettings.serializedObject.ApplyModifiedProperties();

                // HACK: Due to RP initialization, it's possible VolumeManager was not ready when the UI was created.
                // Since there is no OnRPCreated event, we create the UI lazily here if needed.
                if (m_DefaultVolumeProfileEditor == null &&
                    VolumeManager.instance.isInitialized &&
                    Event.current.type != EventType.Layout) // Cannot change visual hierarchy during layout event
                    CreateDefaultVolumeProfileEditor();
            }));

            section.Add(m_DefaultVolumeProfileEditorRoot);

            Undo.undoRedoPerformed += OnUndoRedoPerformed;

            return section;
        }

         void DrawVolumeSection(SerializedHDRenderPipelineGlobalSettings serialized)
         {
             using var indentLevelScope = new EditorGUI.IndentLevelScope();
             using var changeScope = new EditorGUI.ChangeCheckScope();

             var oldWidth = EditorGUIUtility.labelWidth;
             EditorGUIUtility.labelWidth = HDRenderPipelineGlobalSettingsUI.Styles.defaultVolumeLabelWidth;

             var globalSettings = serialized.serializedObject.targetObject as HDRenderPipelineGlobalSettings;

             bool expanded = m_DefaultVolumeProfileFoldoutExpanded.value;
             var previousDefaultVolumeProfileAsset = serialized.defaultVolumeProfile.objectReferenceValue;
             VolumeProfile defaultVolumeProfileAsset = RenderPipelineGlobalSettingsUI.DrawVolumeProfileAssetField(
                 serialized.defaultVolumeProfile,
                 HDRenderPipelineGlobalSettingsUI.Styles.defaultVolumeProfileAssetLabel,
                 getOrCreateVolumeProfile: () => globalSettings.GetOrCreateDefaultVolumeProfile(),
                 ref expanded
             );
             m_DefaultVolumeProfileFoldoutExpanded.value = expanded;

             EditorGUIUtility.labelWidth = oldWidth;

             if (changeScope.changed && defaultVolumeProfileAsset != previousDefaultVolumeProfileAsset)
             {
                 var defaultValuesAsset = globalSettings.renderPipelineEditorResources.defaultSettingsVolumeProfile;
                 if (previousDefaultVolumeProfileAsset == null)
                 {
                     VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile<HDRenderPipeline>(defaultVolumeProfileAsset,
                         defaultValuesAsset);
                 }
                 else
                 {
                     bool confirmed = VolumeProfileUtils.UpdateGlobalDefaultVolumeProfileWithConfirmation<HDRenderPipeline>(defaultVolumeProfileAsset, defaultValuesAsset);
                     if (!confirmed)
                         serialized.defaultVolumeProfile.objectReferenceValue = previousDefaultVolumeProfileAsset;
                 }

                 DestroyDefaultVolumeProfileEditor();
                 CreateDefaultVolumeProfileEditor();
             }
        }

        static void OnVolumeProfileSectionContextClick(
            Vector2 position,
            SerializedHDRenderPipelineGlobalSettings serialized,
            DefaultVolumeProfileEditor defaultVolumeProfileEditor)
        {
            var globalSettings = serialized.serializedObject.targetObject as HDRenderPipelineGlobalSettings;

            VolumeProfileUtils.OnVolumeProfileContextClick(position, globalSettings.volumeProfile, defaultVolumeProfileEditor.allEditors,
                overrideStateOnReset: true,
                defaultVolumeProfilePath: $"Assets/{HDProjectSettings.projectSettingsFolderPath}/VolumeProfile_Default.asset",
                onNewVolumeProfileCreated: volumeProfile =>
                {
                    Undo.RecordObject(globalSettings, "Set Global Settings Volume Profile");
                    globalSettings.volumeProfile = volumeProfile;
                    var defaultValuesAsset = globalSettings.renderPipelineEditorResources.defaultSettingsVolumeProfile;
                    VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile<HDRenderPipeline>(volumeProfile, defaultValuesAsset);
                    EditorUtility.SetDirty(globalSettings);
                },
                onComponentEditorsExpandedCollapsed: defaultVolumeProfileEditor.RebuildListViews);
        }

        #endregion
    }
}
