using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditorInternal;
using System.Linq;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDRenderPipelineGlobalSettings>;

    class DefaultSettingsPanelProvider
    {
        static DefaultSettingsPanelIMGUI s_IMGUIImpl = new DefaultSettingsPanelIMGUI();

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/Graphics/HDRP Settings", SettingsScope.Project)
            {
                activateHandler = s_IMGUIImpl.OnActivate,
                keywords = SettingsProvider.GetSearchKeywordsFromGUIContentProperties<DefaultSettingsPanelIMGUI.Styles>()
                    .Concat(OverridableFrameSettingsArea.frameSettingsKeywords)
                    .ToArray(),
                guiHandler = s_IMGUIImpl.DoGUI
            };
        }
    }
    internal class DefaultSettingsPanelIMGUI
    {
        public class Styles
        {
            public const int labelWidth = 220;
            internal static GUIStyle sectionHeaderStyle = new GUIStyle(EditorStyles.largeLabel) { richText = true, fontSize = 18, fixedHeight = 42 };
            internal static GUIStyle subSectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel);

            internal static readonly GUIContent defaultVolumeProfileLabel = EditorGUIUtility.TrTextContent("Default Volume Profile Asset");
            internal static readonly GUIContent lookDevVolumeProfileLabel = EditorGUIUtility.TrTextContent("LookDev Volume Profile Asset");

            internal static readonly GUIContent frameSettingsLabel = EditorGUIUtility.TrTextContent("Frame Settings (Default Values)");
            internal static readonly GUIContent frameSettingsLabel_Camera = EditorGUIUtility.TrTextContent("Camera");
            internal static readonly GUIContent frameSettingsLabel_RTProbe = EditorGUIUtility.TrTextContent("Realtime Reflection");
            internal static readonly GUIContent frameSettingsLabel_BakedProbe = EditorGUIUtility.TrTextContent("Baked or Custom Reflection");
            internal static readonly GUIContent renderingSettingsHeaderContent = EditorGUIUtility.TrTextContent("Rendering");
            internal static readonly GUIContent lightSettingsHeaderContent = EditorGUIUtility.TrTextContent("Lighting");
            internal static readonly GUIContent asyncComputeSettingsHeaderContent = EditorGUIUtility.TrTextContent("Asynchronous Compute Shaders");
            internal static readonly GUIContent lightLoopSettingsHeaderContent = EditorGUIUtility.TrTextContent("Light Loop Debug");

            internal static readonly GUIContent volumeComponentsLabel = EditorGUIUtility.TrTextContent("Volume Profiles");
            internal static readonly GUIContent customPostProcessOrderLabel = EditorGUIUtility.TrTextContent("Custom Post Process Orders");

            internal static readonly GUIContent resourceLabel = EditorGUIUtility.TrTextContent("Resources");
            internal static readonly GUIContent renderPipelineResourcesContent = EditorGUIUtility.TrTextContent("Player Resources", "Set of resources that need to be loaded when creating stand alone");
            internal static readonly GUIContent renderPipelineRayTracingResourcesContent = EditorGUIUtility.TrTextContent("Ray Tracing Resources", "Set of resources that need to be loaded when using ray tracing");
            internal static readonly GUIContent renderPipelineEditorResourcesContent = EditorGUIUtility.TrTextContent("Editor Resources", "Set of resources that need to be loaded for working in editor");

            internal static readonly GUIContent generalSettingsLabel = EditorGUIUtility.TrTextContent("Miscellaneous");

            internal static readonly GUIContent layerNamesLabel = EditorGUIUtility.TrTextContent("Layers Names");
            internal static readonly GUIContent lightLayersLabel = EditorGUIUtility.TrTextContent("Light Layers Names", "When enabled, HDRP allocates memory for processing Light Layers. For deferred rendering, this allocation includes an extra render target in memory and extra cost. See the Quality Settings window to enable Decal Layers on your Render pipeline asset.");
            internal static readonly GUIContent lightLayerName0 = EditorGUIUtility.TrTextContent("Light Layer 0", "The display name for Light Layer 0. This is purely cosmetic, and can be used to articulate intended use of Light Layer 0");
            internal static readonly GUIContent lightLayerName1 = EditorGUIUtility.TrTextContent("Light Layer 1", "The display name for Light Layer 1. This is purely cosmetic, and can be used to articulate intended use of Light Layer 1");
            internal static readonly GUIContent lightLayerName2 = EditorGUIUtility.TrTextContent("Light Layer 2", "The display name for Light Layer 2. This is purely cosmetic, and can be used to articulate intended use of Light Layer 2");
            internal static readonly GUIContent lightLayerName3 = EditorGUIUtility.TrTextContent("Light Layer 3", "The display name for Light Layer 3. This is purely cosmetic, and can be used to articulate intended use of Light Layer 3");
            internal static readonly GUIContent lightLayerName4 = EditorGUIUtility.TrTextContent("Light Layer 4", "The display name for Light Layer 4. This is purely cosmetic, and can be used to articulate intended use of Light Layer 4");
            internal static readonly GUIContent lightLayerName5 = EditorGUIUtility.TrTextContent("Light Layer 5", "The display name for Light Layer 5. This is purely cosmetic, and can be used to articulate intended use of Light Layer 5");
            internal static readonly GUIContent lightLayerName6 = EditorGUIUtility.TrTextContent("Light Layer 6", "The display name for Light Layer 6. This is purely cosmetic, and can be used to articulate intended use of Light Layer 6");
            internal static readonly GUIContent lightLayerName7 = EditorGUIUtility.TrTextContent("Light Layer 7", "The display name for Light Layer 7. This is purely cosmetic, and can be used to articulate intended use of Light Layer 7");

            internal static readonly GUIContent decalLayersLabel = EditorGUIUtility.TrTextContent("Decal Layers Names", "When enabled, HDRP allocates Shader variants and memory to the decals buffer and cluster decal. See the Quality Settings window to enable Decal Layers on your Render pipeline asset.");
            internal static readonly GUIContent decalLayerName0 = EditorGUIUtility.TrTextContent("Decal Layer 0", "The display name for Decal Layer 0. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 0");
            internal static readonly GUIContent decalLayerName1 = EditorGUIUtility.TrTextContent("Decal Layer 1", "The display name for Decal Layer 1. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 1");
            internal static readonly GUIContent decalLayerName2 = EditorGUIUtility.TrTextContent("Decal Layer 2", "The display name for Decal Layer 2. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 2");
            internal static readonly GUIContent decalLayerName3 = EditorGUIUtility.TrTextContent("Decal Layer 3", "The display name for Decal Layer 3. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 3");
            internal static readonly GUIContent decalLayerName4 = EditorGUIUtility.TrTextContent("Decal Layer 4", "The display name for Decal Layer 4. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 4");
            internal static readonly GUIContent decalLayerName5 = EditorGUIUtility.TrTextContent("Decal Layer 5", "The display name for Decal Layer 5. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 5");
            internal static readonly GUIContent decalLayerName6 = EditorGUIUtility.TrTextContent("Decal Layer 6", "The display name for Decal Layer 6. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 6");
            internal static readonly GUIContent decalLayerName7 = EditorGUIUtility.TrTextContent("Decal Layer 7", "The display name for Decal Layer 7. This is purely cosmetic, and can be used to articulate intended use of Decal Layer 7");

            internal static readonly GUIContent shaderVariantLogLevelLabel = EditorGUIUtility.TrTextContent("Shader Variant Log Level", "Controls the level logging in of shader variants information is outputted when a build is performed. Information appears in the Unity Console when the build finishes..");

            internal static readonly GUIContent lensAttenuationModeContentLabel = EditorGUIUtility.TrTextContent("Lens Attenuation Mode", "Set the attenuation mode of the lens that is used to compute exposure. With imperfect lens some energy is lost when converting from EV100 to the exposure multiplier.");

            internal static readonly GUIContent diffusionProfileSettingsLabel = EditorGUIUtility.TrTextContent("Diffusion Profile Assets");
            internal static readonly string warningHdrpNotActive = "No HD Render Pipeline currently active. Verify your Graphics Settings and active Quality Level.";
            internal static readonly string warningGlobalSettingsMissing = "No active settings for HDRP. Rendering may be broken until a new one is assigned.";
            internal static readonly string infoGlobalSettingsMissing = "No active Global Settings for HDRP. You may assign one below.";
        }

        public static readonly CED.IDrawer Inspector;

        static DefaultSettingsPanelIMGUI()
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
                if (HDRenderPipeline.currentAsset != null || HDRenderPipelineGlobalSettings.instance != null)
                {
                    settingsSerialized = HDRenderPipelineGlobalSettings.Ensure(folderPath: HDProjectSettings.projectSettingsFolderPath);
                    var serializedObject = new SerializedObject(settingsSerialized);
                    serializedSettings = new SerializedHDRenderPipelineGlobalSettings(serializedObject);
                }
            }
            else if (settingsSerialized != null && serializedSettings != null)
            {
                serializedSettings.serializedObject.Update();
            }

            DrawWarnings(ref serializedSettings, null);
            DrawAssetSelection(ref serializedSettings, null);
            if (settingsSerialized != null && serializedSettings != null)
            {
                EditorGUILayout.Space();
                Inspector.Draw(serializedSettings, null);
                serializedSettings.serializedObject?.ApplyModifiedProperties();
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
                EditorGUILayout.HelpBox(Styles.warningGlobalSettingsMissing, MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(Styles.warningHdrpNotActive, MessageType.Warning);
                if (serialized == null)
                    EditorGUILayout.HelpBox(Styles.infoGlobalSettingsMissing, MessageType.Info);
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
                var newAsset = (HDRenderPipelineGlobalSettings)EditorGUILayout.ObjectField(settingsSerialized , typeof(HDRenderPipelineGlobalSettings), false);
                if (EditorGUI.EndChangeCheck())
                {
                    HDRenderPipelineGlobalSettings.UpdateGraphicsSettings(newAsset);
                    if (settingsSerialized != null && !settingsSerialized.Equals(null))
                        EditorUtility.SetDirty(settingsSerialized);
                }

                if (GUILayout.Button(EditorGUIUtility.TrTextContent("New", "Create a HD Global Settings Asset in your default resource folder (defined in Wizard)"), GUILayout.Width(45), GUILayout.Height(18)))
                {
                    HDAssetFactory.HDRenderPipelineGlobalSettingsCreator.Create(useProjectSettingsFolder: true, activateAsset: true);
                }

                bool guiEnabled = GUI.enabled;
                GUI.enabled = guiEnabled && (settingsSerialized != null);
                if (GUILayout.Button(EditorGUIUtility.TrTextContent("Clone", "Clone a HD Global Settings Asset in your default resource folder (defined in Wizard)"), GUILayout.Width(45), GUILayout.Height(18)))
                {
                    HDAssetFactory.HDRenderPipelineGlobalSettingsCreator.Clone(settingsSerialized, activateAsset: true);
                }
                GUI.enabled = guiEnabled;
            }
            EditorGUIUtility.labelWidth = oldWidth;
            EditorGUILayout.Space();
        }

        #endregion // Global HDRenderPipelineGlobalSettings asset selection

        #region Resources

        static readonly CED.IDrawer ResourcesSection = CED.Group(
            CED.Group((serialized, owner) => EditorGUILayout.LabelField(Styles.resourceLabel, Styles.sectionHeaderStyle)),
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
            CED.Group(DrawFrameSettings),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );

        static void DrawFrameSettings(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            EditorGUILayout.LabelField(Styles.frameSettingsLabel, Styles.sectionHeaderStyle);
            EditorGUILayout.Space();

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField(Styles.frameSettingsLabel_Camera, Styles.subSectionHeaderStyle);
                DrawFrameSettingsSubsection(0, serialized.defaultCameraFrameSettings, owner);
                EditorGUILayout.Space();

                EditorGUILayout.LabelField(Styles.frameSettingsLabel_RTProbe, Styles.subSectionHeaderStyle);
                DrawFrameSettingsSubsection(1, serialized.defaultRealtimeReflectionFrameSettings, owner);
                EditorGUILayout.Space();

                EditorGUILayout.LabelField(Styles.frameSettingsLabel_BakedProbe, Styles.subSectionHeaderStyle);
                DrawFrameSettingsSubsection(2, serialized.defaultBakedOrCustomReflectionFrameSettings, owner);
                EditorGUILayout.Space();
            }
        }

        static private bool[] m_ShowFrameSettings_Rendering      = { false, false, false };
        static private bool[] m_ShowFrameSettings_Lighting       = { false, false, false };
        static private bool[] m_ShowFrameSettings_AsyncCompute   = { false, false, false };
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
            CED.Group((serialized, owner) => EditorGUILayout.LabelField(Styles.customPostProcessOrderLabel, Styles.sectionHeaderStyle)),
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
                serialized.uiAfterPostProcessCustomPostProcesses.DoLayoutList();
            }
        }

        #endregion // Custom Post Processes

        #region Diffusion Profile Settings List

        static readonly CED.IDrawer DiffusionProfileSettingsSection = CED.Group(
            CED.Group((serialized, owner) => EditorGUILayout.LabelField(Styles.diffusionProfileSettingsLabel, Styles.sectionHeaderStyle)),
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
            CED.Group((serialized, owner) => EditorGUILayout.LabelField(Styles.volumeComponentsLabel, Styles.sectionHeaderStyle)),
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

                    if (GUILayout.Button(EditorGUIUtility.TrTextContent("New", "Create a new Volume Profile for default in your default resource folder (defined in Wizard)"), GUILayout.Width(38), GUILayout.Height(18)))
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

                    if (GUILayout.Button(EditorGUIUtility.TrTextContent("New", "Create a new Volume Profile for default in your default resource folder (defined in Wizard)"), GUILayout.Width(38), GUILayout.Height(18)))
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

        static readonly CED.IDrawer MiscSection = CED.Group(
            CED.Group((serialized, owner) => EditorGUILayout.LabelField(Styles.generalSettingsLabel, Styles.sectionHeaderStyle)),
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
            }
            EditorGUIUtility.labelWidth = oldWidth;
        }

        #endregion // Misc Settings

        #region Rendering Layer Names

        static readonly CED.IDrawer LayerNamesSection = CED.Group(
            CED.Group((serialized, owner) => EditorGUILayout.LabelField(Styles.layerNamesLabel, Styles.sectionHeaderStyle)),
            CED.Group((serialized, owner) => EditorGUILayout.Space()),
            CED.Group(DrawLayerNamesSettings),
            CED.Group((serialized, owner) => EditorGUILayout.Space())
        );

        static void DrawLayerNamesSettings(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;

            using (new EditorGUI.IndentLevelScope())
            {
                DrawLightLayerNames(serialized, owner);
                EditorGUILayout.Space();
                DrawDecalLayerNames(serialized, owner);
                EditorGUILayout.Space();
            }
            EditorGUIUtility.labelWidth = oldWidth;
        }

        static private bool m_ShowLightLayerNames = false;
        static private bool m_ShowDecalLayerNames = false;
        static void DrawLightLayerNames(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            m_ShowLightLayerNames = EditorGUILayout.Foldout(m_ShowLightLayerNames, Styles.lightLayersLabel, true);
            if (m_ShowLightLayerNames)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName0, serialized.lightLayerName0);
                    GUILayout.Space(2);
                    HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName1, serialized.lightLayerName1);
                    GUILayout.Space(2);
                    HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName2, serialized.lightLayerName2);
                    GUILayout.Space(2);
                    HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName3, serialized.lightLayerName3);
                    GUILayout.Space(2);
                    HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName4, serialized.lightLayerName4);
                    GUILayout.Space(2);
                    HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName5, serialized.lightLayerName5);
                    GUILayout.Space(2);
                    HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName6, serialized.lightLayerName6);
                    GUILayout.Space(2);
                    HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName7, serialized.lightLayerName7);
                }
            }
        }

        static void DrawDecalLayerNames(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            m_ShowDecalLayerNames = EditorGUILayout.Foldout(m_ShowDecalLayerNames, Styles.decalLayersLabel, true);
            if (m_ShowDecalLayerNames)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName0, serialized.decalLayerName0);
                    GUILayout.Space(2);
                    HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName1, serialized.decalLayerName1);
                    GUILayout.Space(2);
                    HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName2, serialized.decalLayerName2);
                    GUILayout.Space(2);
                    HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName3, serialized.decalLayerName3);
                    GUILayout.Space(2);
                    HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName4, serialized.decalLayerName4);
                    GUILayout.Space(2);
                    HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName5, serialized.decalLayerName5);
                    GUILayout.Space(2);
                    HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName6, serialized.decalLayerName6);
                    GUILayout.Space(2);
                    HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName7, serialized.decalLayerName7);
                }
            }
        }

        #endregion
    }
}
