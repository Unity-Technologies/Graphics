using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Universal
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UniversalRenderPipelineGlobalSettings))]
    sealed class UniversalGlobalSettingsEditor : Editor
    {
        [MenuItem("Assets/Create/Rendering/URP Global Settings Asset", priority = CoreUtils.Sections.section4 + 2)]
        internal static void CreateAsset()
        {
            RenderPipelineGlobalSettingsEndNameEditAction.CreateNew<UniversalRenderPipeline, UniversalRenderPipelineGlobalSettings>();
        }

        SerializedUniversalRenderPipelineGlobalSettings m_SerializedGlobalSettings;

        DefaultVolumeProfileEditor m_DefaultVolumeProfileEditor;
        VisualElement m_DefaultVolumeProfileEditorRoot;
        EditorPrefBool m_DefaultVolumeProfileFoldoutExpanded;

        void OnEnable()
        {
            m_SerializedGlobalSettings = new SerializedUniversalRenderPipelineGlobalSettings(serializedObject);

            Undo.undoRedoPerformed += OnUndoRedoPerformed;

            m_DefaultVolumeProfileFoldoutExpanded = new EditorPrefBool($"{GetType()}.DefaultVolumeProfileFoldoutExpanded", true);
        }

        void OnDisable()
        {
            m_SerializedGlobalSettings = null;
            DestroyDefaultVolumeProfileEditor();

            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        void OnUndoRedoPerformed()
        {
            if (target is UniversalRenderPipelineGlobalSettings settings)
            {
                if (settings.volumeProfile != VolumeManager.instance.globalDefaultProfile)
                {
                    VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile<UniversalRenderPipeline>(settings.volumeProfile);
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

            root.Add(new IMGUIContainer(() => m_SerializedGlobalSettings.serializedObject.Update()));

            root.Add(CreateVolumeProfileSection());
            root.Add(UniversalRenderPipelineGlobalSettingsUI.CreateImguiSections(m_SerializedGlobalSettings, this));

            return root;
        }

        #region Default Volume Profile
        void CreateDefaultVolumeProfileEditor()
        {
            Debug.Assert(VolumeManager.instance.isInitialized);
            Debug.Assert(m_DefaultVolumeProfileEditorRoot.childCount == 0);

            var volumeProfile = m_SerializedGlobalSettings.defaultVolumeProfile.objectReferenceValue as VolumeProfile;
            if (volumeProfile == null)
                return;

            if (volumeProfile == VolumeManager.instance.globalDefaultProfile)
                VolumeProfileUtils.EnsureAllOverridesForDefaultProfile(volumeProfile);

            m_DefaultVolumeProfileEditor = new DefaultVolumeProfileEditor(this, volumeProfile);
            m_DefaultVolumeProfileEditorRoot.Add(m_DefaultVolumeProfileEditor.Create());

            m_DefaultVolumeProfileEditorRoot.Q<HelpBox>("volume-override-info-box").text = EditorGUIUtility.TrTextContent(
                "The values in the Default Volume can be overridden by a Volume Profile assigned to URP asset and Volumes inside scenes.").text;
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
                    UniversalRenderPipelineGlobalSettingsUI.Styles.defaultVolumeProfileHeaderLabel,
                    Documentation.GetPageLink(UniversalRenderPipelineGlobalSettingsUI.DocumentationUrls.k_Volumes),
                    pos => OnVolumeProfileSectionContextClick(pos, m_SerializedGlobalSettings, m_DefaultVolumeProfileEditor));
                EditorGUILayout.Space();
                DrawVolumeSection(m_SerializedGlobalSettings);

                // Propagate foldout expander state from IMGUI to UITK
                m_DefaultVolumeProfileEditorRoot.style.display =
                    m_DefaultVolumeProfileFoldoutExpanded.value ? DisplayStyle.Flex : DisplayStyle.None;

                if (changedScope.changed)
                    m_SerializedGlobalSettings.serializedObject.ApplyModifiedProperties();

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

        void DrawVolumeSection(SerializedUniversalRenderPipelineGlobalSettings serialized)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                using var changeScope = new EditorGUI.ChangeCheckScope();

                var oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = UniversalRenderPipelineGlobalSettingsUI.Styles.defaultVolumeLabelWidth;

                var globalSettings = serialized.serializedObject.targetObject as UniversalRenderPipelineGlobalSettings;

                bool expanded = m_DefaultVolumeProfileFoldoutExpanded.value;
                var previousDefaultVolumeProfileAsset = serialized.defaultVolumeProfile.objectReferenceValue;
                VolumeProfile defaultVolumeProfileAsset = RenderPipelineGlobalSettingsUI.DrawVolumeProfileAssetField(
                    serialized.defaultVolumeProfile,
                    UniversalRenderPipelineGlobalSettingsUI.Styles.defaultVolumeProfileLabel,
                    getOrCreateVolumeProfile: () => globalSettings.GetOrCreateDefaultVolumeProfile(),
                    ref expanded
                );
                m_DefaultVolumeProfileFoldoutExpanded.value = expanded;

                EditorGUIUtility.labelWidth = oldWidth;

                if (changeScope.changed && defaultVolumeProfileAsset != previousDefaultVolumeProfileAsset)
                {
                    if (previousDefaultVolumeProfileAsset == null)
                    {
                        VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile<UniversalRenderPipeline>(defaultVolumeProfileAsset);
                    }
                    else
                    {
                        bool confirmed = VolumeProfileUtils.UpdateGlobalDefaultVolumeProfileWithConfirmation<UniversalRenderPipeline>(defaultVolumeProfileAsset);
                        if (!confirmed)
                            serialized.defaultVolumeProfile.objectReferenceValue = previousDefaultVolumeProfileAsset;
                    }

                    DestroyDefaultVolumeProfileEditor();
                    CreateDefaultVolumeProfileEditor();
                }
            }
        }

        static void OnVolumeProfileSectionContextClick(
            Vector2 position,
            SerializedUniversalRenderPipelineGlobalSettings serialized,
            DefaultVolumeProfileEditor defaultVolumeProfileEditor)
        {
            var globalSettings = serialized.serializedObject.targetObject as UniversalRenderPipelineGlobalSettings;

            VolumeProfileUtils.OnVolumeProfileContextClick(position, globalSettings.volumeProfile, defaultVolumeProfileEditor.allEditors,
                overrideStateOnReset: true,
                defaultVolumeProfilePath: "Assets/VolumeProfile_Default.asset",
                onNewVolumeProfileCreated: volumeProfile =>
                {
                    Undo.RecordObject(globalSettings, "Set Global Settings Volume Profile");
                    globalSettings.volumeProfile = volumeProfile;
                    VolumeProfileUtils.UpdateGlobalDefaultVolumeProfile<UniversalRenderPipeline>(volumeProfile);
                    EditorUtility.SetDirty(globalSettings);
                },
                onComponentEditorsExpandedCollapsed: defaultVolumeProfileEditor.RebuildListViews);
        }

        #endregion
    }
}
