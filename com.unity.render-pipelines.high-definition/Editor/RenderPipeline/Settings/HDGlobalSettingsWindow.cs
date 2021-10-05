using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditorInternal;
using System.Linq;
using System.Reflection;
using UnityEditor.VFX.HDRP;
using UnityEditor.VFX.UI;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDRenderPipelineGlobalSettings>;

    class HDGlobalSettingsPanelProvider
    {
        static HDGlobalSettingsPanelIMGUI s_IMGUIImpl = new HDGlobalSettingsPanelIMGUI();

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var keywords = SettingsProvider.GetSearchKeywordsFromGUIContentProperties<HDGlobalSettingsPanelIMGUI.Styles>()
                .Concat(OverridableFrameSettingsArea.frameSettingsKeywords);

            keywords = RenderPipelineSettingsUtilities.RemoveDLSSKeywords(keywords);

            return new SettingsProvider("Project/Graphics/HDRP Global Settings", SettingsScope.Project)
            {
                activateHandler = s_IMGUIImpl.OnActivate,
                keywords = keywords.ToArray(),
                guiHandler = s_IMGUIImpl.DoGUI,
                titleBarGuiHandler = s_IMGUIImpl.OnTitleBarGUI
            };
        }
    }

    internal partial class HDGlobalSettingsPanelIMGUI
    {
        public static readonly CED.IDrawer Inspector;

        public class DocumentationUrls
        {
            public static readonly string k_Volumes = "Volume-Profile";
            public static readonly string k_DiffusionProfiles = "Override-Diffusion-Profile";
            public static readonly string k_FrameSettings = "Frame-Settings";
            public static readonly string k_LightLayers = "Light-Layers";
            public static readonly string k_DecalLayers = "Decal";
            public static readonly string k_CustomPostProcesses = "Custom-Post-Process";
        }

        static HDGlobalSettingsPanelIMGUI()
        {
            Inspector = CED.Group(
                VolumeSection,
                CED.Group((serialized, owner) => EditorGUILayout.Space()),
                DiffusionProfileSettingsSection,
                CED.Group((serialized, owner) => EditorGUILayout.Space()),
                FrameSettingsSection,
                CED.Group((serialized, owner) => EditorGUILayout.Space()),
                LayerNamesSection,
                CED.Group((serialized, owner) => EditorGUILayout.Space()),
                CustomPostProcessesSection,
                CED.Group((serialized, owner) => EditorGUILayout.Space()),
                MiscSection,
                CED.Group((serialized, owner) => EditorGUILayout.Space()),
                ResourcesSection
            );
        }

        SerializedHDRenderPipelineGlobalSettings serializedSettings;
        HDRenderPipelineGlobalSettings settingsSerialized;

        public void OnTitleBarGUI()
        {
            if (GUILayout.Button(CoreEditorStyles.iconHelp, CoreEditorStyles.iconHelpStyle))
                Help.BrowseURL(Documentation.GetPageLink("Default-Settings-Window"));
        }

        internal static bool needRefreshVfxErrors = false;

        public void DoGUI(string searchContext)
        {
            // When the asset being serialized has been deleted before its reconstruction
            if (serializedSettings != null && serializedSettings.serializedObject.targetObject == null)
            {
                serializedSettings = null;
                settingsSerialized = null;
            }

            if (serializedSettings == null || settingsSerialized != HDRenderPipelineGlobalSettings.instance)
            {
                if (HDRenderPipelineGlobalSettings.instance != null)
                {
                    settingsSerialized = HDRenderPipelineGlobalSettings.Ensure();
                    var serializedObject = new SerializedObject(settingsSerialized);
                    serializedSettings = new SerializedHDRenderPipelineGlobalSettings(serializedObject);
                }
                else
                {
                    serializedSettings = null;
                    settingsSerialized = null;
                }
            }
            else if (settingsSerialized != null && serializedSettings != null)
            {
                serializedSettings.serializedObject.Update();
            }

            DrawAssetSelection(ref serializedSettings, null);
            DrawWarnings(ref serializedSettings, null);
            if (settingsSerialized != null && serializedSettings != null)
            {
                EditorGUILayout.Space();
                Inspector.Draw(serializedSettings, null);
                serializedSettings.serializedObject?.ApplyModifiedProperties();
                VFXHDRPSettingsUtility.RefreshVfxErrorsIfNeeded(ref needRefreshVfxErrors);
            }
        }

        /// <summary>
        /// Executed when activate is called from the settings provider.
        /// </summary>
        /// <param name="searchContext"></param>
        /// <param name="rootElement"></param>
        public void OnActivate(string searchContext, VisualElement rootElement)
        {
        }

        void DrawWarnings(ref SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            bool isHDRPinUse = HDRenderPipeline.currentAsset != null;
            if (isHDRPinUse && serialized != null)
                return;

            if (isHDRPinUse)
            {
                ShowMessageWithFixButton(Styles.warningGlobalSettingsMissing, MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(Styles.warningHdrpNotActive, MessageType.Warning);
                if (serialized == null)
                {
                    ShowMessageWithFixButton(Styles.infoGlobalSettingsMissing, MessageType.Info);
                }
            }
        }

        void ShowMessageWithFixButton(string helpBoxLabel, MessageType type)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.HelpBox(helpBoxLabel, type);
                if (GUILayout.Button(Styles.fixAssetButtonLabel, GUILayout.Width(45)))
                {
                    HDRenderPipelineGlobalSettings.Ensure();
                }
            }
        }

        #region Global HDRenderPipelineGlobalSettings asset selection
        void DrawAssetSelection(ref SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                var newAsset = (HDRenderPipelineGlobalSettings)EditorGUILayout.ObjectField(settingsSerialized, typeof(HDRenderPipelineGlobalSettings), false);
                if (EditorGUI.EndChangeCheck())
                {
                    HDRenderPipelineGlobalSettings.UpdateGraphicsSettings(newAsset);
                    Debug.Assert(newAsset == HDRenderPipelineGlobalSettings.instance);
                    if (settingsSerialized != null && !settingsSerialized.Equals(null))
                        EditorUtility.SetDirty(settingsSerialized);
                }

                if (GUILayout.Button(Styles.newAssetButtonLabel, GUILayout.Width(45), GUILayout.Height(18)))
                {
                    HDAssetFactory.HDRenderPipelineGlobalSettingsCreator.Create(useProjectSettingsFolder: true, assignToActiveAsset: true);
                }

                bool guiEnabled = GUI.enabled;
                GUI.enabled = guiEnabled && (settingsSerialized != null);
                if (GUILayout.Button(Styles.cloneAssetButtonLabel, GUILayout.Width(45), GUILayout.Height(18)))
                {
                    HDAssetFactory.HDRenderPipelineGlobalSettingsCreator.Clone(settingsSerialized, assignToActiveAsset: true);
                }
                GUI.enabled = guiEnabled;
            }
            EditorGUIUtility.labelWidth = oldWidth;
            EditorGUILayout.Space();
        }

        #endregion // Global HDRenderPipelineGlobalSettings asset selection

        #region Resources

        static readonly CED.IDrawer ResourcesSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.resourceLabel)),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawResourcesSection),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );
        static void DrawResourcesSection(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                var oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = Styles.labelWidth;

                EditorGUILayout.PropertyField(serialized.renderPipelineResources, Styles.renderPipelineResourcesContent);
                bool oldGuiEnabled = GUI.enabled;
                GUI.enabled = false;

                EditorGUILayout.PropertyField(serialized.renderPipelineRayTracingResources, Styles.renderPipelineRayTracingResourcesContent);

                // Not serialized as editor only datas... Retrieve them in data
                EditorGUI.showMixedValue = serialized.editorResourceHasMultipleDifferentValues;
                var editorResources = EditorGUILayout.ObjectField(Styles.renderPipelineEditorResourcesContent, serialized.firstEditorResources, typeof(HDRenderPipelineEditorResources), allowSceneObjects: false) as HDRenderPipelineEditorResources;

                EditorGUI.showMixedValue = false;

                GUI.enabled = oldGuiEnabled;
                EditorGUIUtility.labelWidth = oldWidth;
            }
        }

        #endregion // Resources

        #region Frame Settings

        static readonly CED.IDrawer FrameSettingsSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.frameSettingsLabel, Documentation.GetPageLink(DocumentationUrls.k_FrameSettings))),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawFrameSettings),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );

        static void DrawFrameSettings(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField(Styles.frameSettingsLabel_Camera, CoreEditorStyles.subSectionHeaderStyle);
                DrawFrameSettingsSubsection(0, serialized.defaultCameraFrameSettings, owner);
                EditorGUILayout.Space();

                EditorGUILayout.LabelField(Styles.frameSettingsLabel_RTProbe, CoreEditorStyles.subSectionHeaderStyle);
                DrawFrameSettingsSubsection(1, serialized.defaultRealtimeReflectionFrameSettings, owner);
                EditorGUILayout.Space();

                EditorGUILayout.LabelField(Styles.frameSettingsLabel_BakedProbe, CoreEditorStyles.subSectionHeaderStyle);
                DrawFrameSettingsSubsection(2, serialized.defaultBakedOrCustomReflectionFrameSettings, owner);
                EditorGUILayout.Space();
            }
        }

        static private bool[] m_ShowFrameSettings_Rendering = { false, false, false };
        static private bool[] m_ShowFrameSettings_Lighting = { false, false, false };
        static private bool[] m_ShowFrameSettings_AsyncCompute = { false, false, false };
        static private bool[] m_ShowFrameSettings_LightLoopDebug = { false, false, false };

        static void DrawFrameSettingsSubsection(int index, SerializedFrameSettings serialized, Editor owner)
        {
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;

            CoreEditorUtils.DrawSplitter();
            m_ShowFrameSettings_Rendering[index] = CoreEditorUtils.DrawHeaderFoldout(Styles.renderingSettingsHeaderContent, m_ShowFrameSettings_Rendering[index]);
            if (m_ShowFrameSettings_Rendering[index])
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    FrameSettingsUI.Drawer_SectionRenderingSettings(serialized, owner, withOverride: false);
                }
            }

            CoreEditorUtils.DrawSplitter();
            m_ShowFrameSettings_Lighting[index] = CoreEditorUtils.DrawHeaderFoldout(Styles.lightSettingsHeaderContent, m_ShowFrameSettings_Lighting[index]);
            if (m_ShowFrameSettings_Lighting[index])
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    FrameSettingsUI.Drawer_SectionLightingSettings(serialized, owner, withOverride: false);
                }
            }

            CoreEditorUtils.DrawSplitter();
            m_ShowFrameSettings_AsyncCompute[index] = CoreEditorUtils.DrawHeaderFoldout(Styles.asyncComputeSettingsHeaderContent, m_ShowFrameSettings_AsyncCompute[index]);
            if (m_ShowFrameSettings_AsyncCompute[index])
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    FrameSettingsUI.Drawer_SectionAsyncComputeSettings(serialized, owner, withOverride: false);
                }
            }

            CoreEditorUtils.DrawSplitter();
            m_ShowFrameSettings_LightLoopDebug[index] = CoreEditorUtils.DrawHeaderFoldout(Styles.lightLoopSettingsHeaderContent, m_ShowFrameSettings_LightLoopDebug[index]);
            if (m_ShowFrameSettings_LightLoopDebug[index])
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    FrameSettingsUI.Drawer_SectionLightLoopSettings(serialized, owner, withOverride: false);
                }
            }
            CoreEditorUtils.DrawSplitter();
            EditorGUIUtility.labelWidth = oldWidth;
        }

        #endregion // Frame Settings

        #region Custom Post Processes

        static readonly CED.IDrawer CustomPostProcessesSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.customPostProcessOrderLabel, Documentation.GetPageLink(DocumentationUrls.k_CustomPostProcesses))),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawCustomPostProcess)
        );
        static void DrawCustomPostProcess(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(5);
                serialized.uiBeforeTransparentCustomPostProcesses.DoLayoutList();
            }
            GUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(5);
                serialized.uiBeforeTAACustomPostProcesses.DoLayoutList();
            }
            GUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(5);
                serialized.uiBeforePostProcessCustomPostProcesses.DoLayoutList();
            }
            GUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(5);
                serialized.uiAfterPostProcessBlursCustomPostProcesses.DoLayoutList();
            }
            GUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(5);
                serialized.uiAfterPostProcessCustomPostProcesses.DoLayoutList();
            }
        }

        #endregion // Custom Post Processes

        #region Diffusion Profile Settings List

        static readonly CED.IDrawer DiffusionProfileSettingsSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.diffusionProfileSettingsLabel, Documentation.GetPageLink(DocumentationUrls.k_DiffusionProfiles))),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawDiffusionProfileSettings)
        );
        static void DrawDiffusionProfileSettings(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(5);
                serialized.m_DiffusionProfileUI.OnGUI(serialized.diffusionProfileSettingsList);
            }
        }

        #endregion //Diffusion Profile Settings List

        #region Volume Profiles
        static Editor m_CachedDefaultVolumeProfileEditor;
        static Editor m_CachedLookDevVolumeProfileEditor;
        static int m_CurrentVolumeProfileInstanceID;

        static readonly CED.IDrawer VolumeSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.volumeComponentsLabel, Documentation.GetPageLink(DocumentationUrls.k_Volumes))),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawVolumeSection),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );

        static void DrawVolumeSection(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                var oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = Styles.labelWidth;

                HDRenderPipelineGlobalSettings globalSettings = serialized.serializedObject.targetObject as HDRenderPipelineGlobalSettings;
                VolumeProfile asset = null;
                using (new EditorGUILayout.HorizontalScope())
                {
                    var oldAssetValue = serialized.defaultVolumeProfile.objectReferenceValue;
                    EditorGUILayout.PropertyField(serialized.defaultVolumeProfile, Styles.defaultVolumeProfileLabel);
                    asset = serialized.defaultVolumeProfile.objectReferenceValue as VolumeProfile;
                    if (asset == null && oldAssetValue != null)
                    {
                        Debug.Log("Default Volume Profile Asset cannot be null. Rolling back to previous value.");
                        serialized.defaultVolumeProfile.objectReferenceValue = oldAssetValue;
                    }

                    if (GUILayout.Button(Styles.newVolumeProfileLabel, GUILayout.Width(38), GUILayout.Height(18)))
                    {
                        HDAssetFactory.VolumeProfileCreator.CreateAndAssign(HDAssetFactory.VolumeProfileCreator.Kind.Default, globalSettings);
                    }
                }
                if (asset != null)
                {
                    // The state of the profile can change without the asset reference changing so in this case we need to reset the editor.
                    if (m_CurrentVolumeProfileInstanceID != asset.GetInstanceID() && m_CachedDefaultVolumeProfileEditor != null)
                    {
                        m_CurrentVolumeProfileInstanceID = asset.GetInstanceID();
                        m_CachedDefaultVolumeProfileEditor = null;
                    }

                    Editor.CreateCachedEditor(asset, Type.GetType("UnityEditor.Rendering.VolumeProfileEditor"), ref m_CachedDefaultVolumeProfileEditor);
                    bool oldEnabled = GUI.enabled;
                    GUI.enabled = AssetDatabase.IsOpenForEdit(asset);
                    m_CachedDefaultVolumeProfileEditor.OnInspectorGUI();
                    GUI.enabled = oldEnabled;
                }

                EditorGUILayout.Space();

                VolumeProfile lookDevAsset = null;
                using (new EditorGUILayout.HorizontalScope())
                {
                    var oldAssetValue = serialized.lookDevVolumeProfile.objectReferenceValue;
                    EditorGUILayout.PropertyField(serialized.lookDevVolumeProfile, Styles.lookDevVolumeProfileLabel);
                    lookDevAsset = serialized.lookDevVolumeProfile.objectReferenceValue as VolumeProfile;
                    if (lookDevAsset == null && oldAssetValue != null)
                    {
                        Debug.Log("LookDev Volume Profile Asset cannot be null. Rolling back to previous value.");
                        serialized.lookDevVolumeProfile.objectReferenceValue = oldAssetValue;
                    }

                    if (GUILayout.Button(Styles.newVolumeProfileLabel, GUILayout.Width(38), GUILayout.Height(18)))
                    {
                        HDAssetFactory.VolumeProfileCreator.CreateAndAssign(HDAssetFactory.VolumeProfileCreator.Kind.LookDev, globalSettings);
                    }
                }
                if (lookDevAsset != null)
                {
                    Editor.CreateCachedEditor(lookDevAsset, Type.GetType("UnityEditor.Rendering.VolumeProfileEditor"), ref m_CachedLookDevVolumeProfileEditor);
                    bool oldEnabled = GUI.enabled;
                    GUI.enabled = AssetDatabase.IsOpenForEdit(lookDevAsset);
                    m_CachedLookDevVolumeProfileEditor.OnInspectorGUI();
                    GUI.enabled = oldEnabled;

                    if (lookDevAsset.Has<VisualEnvironment>())
                        EditorGUILayout.HelpBox("VisualEnvironment is not modifiable and will be overridden by the LookDev", MessageType.Warning);
                    if (lookDevAsset.Has<HDRISky>())
                        EditorGUILayout.HelpBox("HDRISky is not modifiable and will be overridden by the LookDev", MessageType.Warning);
                }
                EditorGUIUtility.labelWidth = oldWidth;
            }
        }

        #endregion // Volume Profiles

        #region Misc Settings

        static MethodInfo s_CleanupRenderPipelineMethod = typeof(RenderPipelineManager).GetMethod("CleanupRenderPipeline", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        static readonly CED.IDrawer MiscSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.generalSettingsLabel)),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawMiscSettings),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );
        static void DrawMiscSettings(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(serialized.shaderVariantLogLevel, Styles.shaderVariantLogLevelLabel);
                EditorGUILayout.PropertyField(serialized.lensAttenuation, Styles.lensAttenuationModeContentLabel);
                EditorGUILayout.PropertyField(serialized.rendererListCulling, Styles.rendererListCulling);

#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
                EditorGUILayout.PropertyField(serialized.useDLSSCustomProjectId, Styles.useDLSSCustomProjectIdLabel);
                if (serialized.useDLSSCustomProjectId.boolValue)
                    EditorGUILayout.PropertyField(serialized.DLSSProjectId, Styles.DLSSProjectIdLabel);
#endif

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serialized.supportProbeVolumes, Styles.probeVolumeSupportContentLabel);
                if (EditorGUI.EndChangeCheck())
                {
                    // If we are running HDRP, we need to make sure the RP is reinitialized
                    if (HDRenderPipeline.currentPipeline != null)
                        s_CleanupRenderPipelineMethod?.Invoke(null, null);
                }

                EditorGUILayout.PropertyField(serialized.supportRuntimeDebugDisplay, Styles.supportRuntimeDebugDisplayContentLabel);
            }
            EditorGUIUtility.labelWidth = oldWidth;
        }

        #endregion // Misc Settings

        #region Rendering Layer Names

        static readonly CED.IDrawer LayerNamesSection = CED.Group(
            CED.Group((serialized, owner) => CoreEditorUtils.DrawSectionHeader(Styles.layerNamesLabel, contextAction: pos => OnContextClickRenderingLayerNames(pos, serialized), documentationURL: Documentation.GetPageLink(DocumentationUrls.k_LightLayers))),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawLayerNamesSettings),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );

        static void DrawLayerNamesSettings(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;

            CoreEditorUtils.DrawSplitter();
            DrawLightLayerNames(serialized, owner);
            CoreEditorUtils.DrawSplitter();
            DrawDecalLayerNames(serialized, owner);
            CoreEditorUtils.DrawSplitter();
            EditorGUILayout.Space();

            EditorGUIUtility.labelWidth = oldWidth;
        }

        static private bool m_ShowLightLayerNames = false;
        static private bool m_ShowDecalLayerNames = false;
        static void DrawLightLayerNames(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            m_ShowLightLayerNames = CoreEditorUtils.DrawHeaderFoldout(Styles.lightLayersLabel,
                m_ShowLightLayerNames,
                documentationURL: Documentation.GetPageLink(DocumentationUrls.k_LightLayers),
                contextAction: pos => OnContextClickRenderingLayerNames(pos, serialized, section: 1)
            );
            if (m_ShowLightLayerNames)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    using (var changed = new EditorGUI.ChangeCheckScope())
                    {
                        EditorGUILayout.DelayedTextField(serialized.lightLayerName0, Styles.lightLayerName0);
                        EditorGUILayout.DelayedTextField(serialized.lightLayerName1, Styles.lightLayerName1);
                        EditorGUILayout.DelayedTextField(serialized.lightLayerName2, Styles.lightLayerName2);
                        EditorGUILayout.DelayedTextField(serialized.lightLayerName3, Styles.lightLayerName3);
                        EditorGUILayout.DelayedTextField(serialized.lightLayerName4, Styles.lightLayerName4);
                        EditorGUILayout.DelayedTextField(serialized.lightLayerName5, Styles.lightLayerName5);
                        EditorGUILayout.DelayedTextField(serialized.lightLayerName6, Styles.lightLayerName6);
                        EditorGUILayout.DelayedTextField(serialized.lightLayerName7, Styles.lightLayerName7);
                        if (changed.changed)
                        {
                            serialized.serializedObject?.ApplyModifiedProperties();
                            (serialized.serializedObject.targetObject as HDRenderPipelineGlobalSettings).UpdateRenderingLayerNames();
                        }
                    }
                }
            }
        }

        static void DrawDecalLayerNames(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            m_ShowDecalLayerNames = CoreEditorUtils.DrawHeaderFoldout(Styles.decalLayersLabel, m_ShowDecalLayerNames,
                documentationURL: Documentation.GetPageLink(DocumentationUrls.k_DecalLayers),
                contextAction: pos => OnContextClickRenderingLayerNames(pos, serialized, section: 2));
            if (m_ShowDecalLayerNames)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    using (var changed = new EditorGUI.ChangeCheckScope())
                    {
                        EditorGUILayout.DelayedTextField(serialized.decalLayerName0, Styles.decalLayerName0);
                        EditorGUILayout.DelayedTextField(serialized.decalLayerName1, Styles.decalLayerName1);
                        EditorGUILayout.DelayedTextField(serialized.decalLayerName2, Styles.decalLayerName2);
                        EditorGUILayout.DelayedTextField(serialized.decalLayerName3, Styles.decalLayerName3);
                        EditorGUILayout.DelayedTextField(serialized.decalLayerName4, Styles.decalLayerName4);
                        EditorGUILayout.DelayedTextField(serialized.decalLayerName5, Styles.decalLayerName5);
                        EditorGUILayout.DelayedTextField(serialized.decalLayerName6, Styles.decalLayerName6);
                        EditorGUILayout.DelayedTextField(serialized.decalLayerName7, Styles.decalLayerName7);
                        if (changed.changed)
                        {
                            serialized.serializedObject?.ApplyModifiedProperties();
                            (serialized.serializedObject.targetObject as HDRenderPipelineGlobalSettings).UpdateRenderingLayerNames();
                        }
                    }
                }
            }
        }

        static void OnContextClickRenderingLayerNames(Vector2 position, SerializedHDRenderPipelineGlobalSettings serialized, int section = 0)
        {
            var menu = new GenericMenu();
            menu.AddItem(section == 0 ? CoreEditorStyles.resetAllButtonLabel : CoreEditorStyles.resetButtonLabel, false, () =>
            {
                var globalSettings = (serialized.serializedObject.targetObject as HDRenderPipelineGlobalSettings);
                globalSettings.ResetRenderingLayerNames(lightLayers: section < 2, decalLayers: section != 1);
            });
            menu.DropDown(new Rect(position, Vector2.zero));
        }

        #endregion
    }
}
