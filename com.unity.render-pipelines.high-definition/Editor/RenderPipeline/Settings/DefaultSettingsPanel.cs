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
                keywords = SettingsProvider.GetSearchKeywordsFromGUIContentProperties<HDRenderPipelineUI.Styles>()
                    .Concat(SettingsProvider.GetSearchKeywordsFromGUIContentProperties<DefaultSettingsPanelIMGUI.Styles>())
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
            internal static readonly GUIContent defaultSettingsAssetLabel = EditorGUIUtility.TrTextContent("Default Settings Asset");
            internal static readonly GUIContent defaultVolumeProfileLabel = EditorGUIUtility.TrTextContent("Default Volume Profile Asset");
            internal static readonly GUIContent lookDevVolumeProfileLabel = EditorGUIUtility.TrTextContent("LookDev Volume Profile Asset");
            internal static readonly GUIContent assetSelectionLabel = EditorGUIUtility.TrTextContent("Set Default Settings Profile for HDRP");
            internal static readonly GUIContent assetSelectionIntroLabel = EditorGUIUtility.TrTextContent("The HDRP Settings Profile is a unique asset allowing you to configure default settings and behaviors for any HDRP scene in your project.");
            internal static readonly GUIContent resourceLabel = EditorGUIUtility.TrTextContent("Resources");
            internal static readonly GUIContent resourceIntroLabel = EditorGUIUtility.TrTextContent("Resources assets list the Shaders, Materials, Textures, and other Assets needed to operate the Render Pipeline.");
            internal static readonly GUIContent frameSettingsLabel = EditorGUIUtility.TrTextContent("Frame Settings");
            internal static readonly GUIContent frameSettingsIntroLabel = EditorGUIUtility.TrTextContent("Frame Settings are settings HDRP uses to render Cameras, real-time, baked, and custom reflections. You can set the default Frame Settings for each of these three individually here:", "You can override the default Frame Settings on a per component basis. Enable the 'Custom Frame Settings' checkbox to set specific Frame Settings for individual Cameras and Reflection Probes.");
            internal static readonly GUIContent volumeComponentsLabel = EditorGUIUtility.TrTextContent("Volume Profiles");
            internal static readonly GUIContent volumeComponentsIntroLabel = EditorGUIUtility.TrTextContent("A Volume Profile is a Scriptable Object which contains properties that Volumes use to determine how to render the Scene environment for Cameras they affect. You can define Volume Overrides default values for your project here:", "You can use Volume Overrides on Volume Profiles in your Scenes to override these values and customize the environment settings.");
            internal static readonly GUIContent customPostProcessOrderLabel = EditorGUIUtility.TrTextContent("Custom Post Process Orders");
            internal static readonly GUIContent defaultFrameSettingsContent = EditorGUIUtility.TrTextContent("Applied to");
            internal static readonly GUIContent customPostProcessIntroLabel = EditorGUIUtility.TrTextContent("The High Definition Render Pipeline (HDRP) allows you to write your own post-processing effects that automatically integrate into Volume. HDRP allows you to customize the order of your custom post-processing effect at each injection point.");

            internal static readonly GUIContent renderPipelineResourcesContent = EditorGUIUtility.TrTextContent("Player Resources", "Set of resources that need to be loaded when creating stand alone");
            internal static readonly GUIContent renderPipelineRayTracingResourcesContent = EditorGUIUtility.TrTextContent("Ray Tracing Resources", "Set of resources that need to be loaded when using ray tracing");
            internal static readonly GUIContent renderPipelineEditorResourcesContent = EditorGUIUtility.TrTextContent("Editor Resources", "Set of resources that need to be loaded for working in editor");

            internal static readonly GUIContent generalSettingsLabel = EditorGUIUtility.TrTextContent("Miscellaneous");

            internal static readonly GUIContent layerNamesLabel = EditorGUIUtility.TrTextContent("Layers Names", "Light and Decal Layers are specific LayerMasks used to to make LightsDecals only affect Meshes that are on corresponding Light Layers, and to apply Decals only matching Mesh Renderer or Terrain. By default, Mesh Renderers, or Terrain, Decal Layers are named **Decal Layer 1-7**. To more easily differentiate between them, you can give each Decal Layer a specific name in this section.");
            internal static readonly GUIContent layerNamesIntro = EditorGUIUtility.TrTextContent("By default, Mesh Renderers, or Terrain, Decal Layers are named **Decal Layer 1-7**. To more easily differentiate between them, you can give each Decal Layer a specific name in this section.");

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

            internal static readonly GUIContent shaderVariantLogLevel = EditorGUIUtility.TrTextContent("Shader Variant Log Level", "Controls the level logging in of shader variants information is outputted when a build is performed. Information appears in the Unity Console when the build finishes..");

            internal static readonly GUIContent lensAttenuationModeContent = EditorGUIUtility.TrTextContent("Lens Attenuation Mode", "Set the attenuation mode of the lens that is used to compute exposure. With imperfect lens some energy is lost when converting from EV100 to the exposure multiplier.");

            internal static readonly GUIContent diffusionProfileSettingsIntro = EditorGUIUtility.TrTextContent("The High Definition Render Pipeline(HDRP) allows you to use up to 15 custom Diffusion Profiles in view at the same time.To use more than 15 custom Diffusion Profiles in a Scene,you can use the Diffusion Profile Override inside a Volume.This allows you to specify which Diffusion Profiles to use in a certain area(or in the Scene if the Volume is global");
            internal static readonly GUIContent diffusionProfileSettingsLabel = EditorGUIUtility.TrTextContent("Diffusion Profile Assets");

            internal static GUIStyle sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel) { richText = true };
            internal static GUIStyle introStyle = new GUIStyle(EditorStyles.largeLabel) { wordWrap = true };
        }

        Vector2 m_ScrollViewPosition = Vector2.zero;
        public static readonly CED.IDrawer Inspector;

        static bool m_verboseMode = true;

        static DefaultSettingsPanelIMGUI()
        {
            Inspector = CED.Group(
                ResourcesSection,
                CED.Group((serialized, owner) => EditorGUILayout.Space()),
                FrameSettingsSection,
                CED.Group((serialized, owner) => EditorGUILayout.Space()),
                VolumeSection,
                CED.Group((serialized, owner) => EditorGUILayout.Space()),
                CustomPostProcessesSection,
                CED.Group((serialized, owner) => EditorGUILayout.Space()),
                DiffusionProfileSettingsSection,
                CED.Group((serialized, owner) => EditorGUILayout.Space()),
                LayerNamesSection
            );
            // fix init of selection along what is serialized
            if (k_ExpandedState[Expandable.BakedOrCustomProbeFrameSettings])
                selectedFrameSettings = SelectedFrameSettings.BakedOrCustomReflection;
            else if (k_ExpandedState[Expandable.RealtimeProbeFrameSettings])
                selectedFrameSettings = SelectedFrameSettings.RealtimeReflection;
            else //default value: camera
                selectedFrameSettings = SelectedFrameSettings.Camera;
        }

        SerializedHDRenderPipelineGlobalSettings serializedSettings;
        HDRenderPipelineGlobalSettings settingsSerialized;
        public void DoGUI(string searchContext)
        {
            if (HDRenderPipeline.currentPipeline == null)
            {
                EditorGUILayout.HelpBox("No HDRP pipeline currently active (see Quality Settings active level).", MessageType.Warning);
            }

            if ((serializedSettings == null) || (settingsSerialized != HDRenderPipelineGlobalSettings.instance))
            {
                settingsSerialized = HDRenderPipelineGlobalSettings.Ensure();
                var serializedObject = new SerializedObject(settingsSerialized);
                serializedSettings = new SerializedHDRenderPipelineGlobalSettings(serializedObject);
            }
            else
            {
                serializedSettings.serializedObject.Update();
            }
            Draw_AssetSelection(ref serializedSettings, null);

            if (settingsSerialized != null && serializedSettings != null)
            {
                EditorGUILayout.Space();
                Inspector.Draw(serializedSettings, null);
            }

            serializedSettings.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Executed when activate is called from the settings provider.
        /// </summary>
        /// <param name="searchContext"></param>
        /// <param name="rootElement"></param>
        public void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_ScrollViewPosition = Vector2.zero;
        }

        #region Global HDRenderPipelineGlobalSettings asset selection
        void Draw_AssetSelection(ref SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;

            if (m_verboseMode)
                EditorGUILayout.LabelField(Styles.assetSelectionIntroLabel, Styles.introStyle);

            if (settingsSerialized == null)
            {
                EditorGUILayout.HelpBox("No active settings for HDRP. Rendering may be broken until a new one is assigned.", MessageType.Warning);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                var newAsset = (HDRenderPipelineGlobalSettings)EditorGUILayout.ObjectField(settingsSerialized , typeof(HDRenderPipelineGlobalSettings), false);
                if (EditorGUI.EndChangeCheck())
                {
                    HDRenderPipelineGlobalSettings.UpdateGraphicsSettings(newAsset);
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
        }

        #endregion // Global HDRenderPipelineGlobalSettings asset selection

        #region Resources

        static readonly CED.IDrawer ResourcesSection = CED.Group(
            CED.Group((serialized, owner) => EditorGUILayout.LabelField(Styles.resourceLabel, Styles.sectionHeaderStyle)),
            CED.Group(Drawer_ResourcesSection)
        );
        static void Drawer_ResourcesSection(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            if (m_verboseMode)
                EditorGUILayout.LabelField(Styles.resourceIntroLabel, Styles.introStyle);

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

        public enum SelectedFrameSettings
        {
            Camera,
            BakedOrCustomReflection,
            RealtimeReflection
        }

        internal static SelectedFrameSettings selectedFrameSettings;
        static readonly CED.IDrawer FrameSettingsSection = CED.Group(
            CED.Group(Drawer_TitleDefaultFrameSettings),
            CED.Group((serialized, owner) => EditorGUI.indentLevel++),
            CED.Conditional(
                (serialized, owner) => k_ExpandedState[Expandable.CameraFrameSettings],
                CED.Select(
                    (serialized, owner) => serialized.defaultFrameSettings,
                    FrameSettingsUI.InspectorInnerbox(withOverride: false)
                )
                ),
            CED.Conditional(
                (serialized, owner) => k_ExpandedState[Expandable.BakedOrCustomProbeFrameSettings],
                CED.Select(
                    (serialized, owner) => serialized.defaultBakedOrCustomReflectionFrameSettings,
                    FrameSettingsUI.InspectorInnerbox(withOverride: false)
                )
                ),
            CED.Conditional(
                (serialized, owner) => k_ExpandedState[Expandable.RealtimeProbeFrameSettings],
                CED.Select(
                    (serialized, owner) => serialized.defaultRealtimeReflectionFrameSettings,
                    FrameSettingsUI.InspectorInnerbox(withOverride: false)
                )
                ),
            CED.Group((serialized, owner) => EditorGUI.indentLevel--)
        );


        static void Drawer_TitleDefaultFrameSettings(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            EditorGUILayout.LabelField(Styles.frameSettingsLabel, Styles.sectionHeaderStyle);

            if (m_verboseMode)
                EditorGUILayout.LabelField(Styles.frameSettingsIntroLabel, Styles.introStyle);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(Styles.defaultFrameSettingsContent);
                EditorGUI.BeginChangeCheck();
                selectedFrameSettings = (SelectedFrameSettings)EditorGUILayout.EnumPopup(selectedFrameSettings);
                if (EditorGUI.EndChangeCheck())
                    ApplyChangedDisplayedFrameSettings(serialized, owner);
            }
        }

        enum Expandable
        {
            CameraFrameSettings = 1 << 0, //obsolete
            BakedOrCustomProbeFrameSettings = 1 << 1, //obsolete
            RealtimeProbeFrameSettings = 1 << 2, //obsolete
        }

        static readonly ExpandedState<Expandable, HDRenderPipelineGlobalSettings> k_ExpandedState = new ExpandedState<Expandable, HDRenderPipelineGlobalSettings>(Expandable.CameraFrameSettings, "HDRP");

        static public void ApplyChangedDisplayedFrameSettings(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            k_ExpandedState.SetExpandedAreas(Expandable.CameraFrameSettings | Expandable.BakedOrCustomProbeFrameSettings | Expandable.RealtimeProbeFrameSettings, false);
            switch (selectedFrameSettings)
            {
                case SelectedFrameSettings.Camera:
                    k_ExpandedState.SetExpandedAreas(Expandable.CameraFrameSettings, true);
                    break;
                case SelectedFrameSettings.BakedOrCustomReflection:
                    k_ExpandedState.SetExpandedAreas(Expandable.BakedOrCustomProbeFrameSettings, true);
                    break;
                case SelectedFrameSettings.RealtimeReflection:
                    k_ExpandedState.SetExpandedAreas(Expandable.RealtimeProbeFrameSettings, true);
                    break;
            }
        }

        #endregion // Frame Settings

        #region Custom Post Processes

        static readonly CED.IDrawer CustomPostProcessesSection = CED.Group(
            CED.Group((serialized, owner) => EditorGUILayout.LabelField(Styles.customPostProcessOrderLabel, Styles.sectionHeaderStyle)),
            CED.Group(Drawer_CustomPostProcess)
        );
        static void Drawer_CustomPostProcess(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            if (m_verboseMode)
                EditorGUILayout.LabelField(Styles.customPostProcessIntroLabel, Styles.introStyle);
            using (new EditorGUI.IndentLevelScope())
            {
                serialized.uiBeforeTransparentCustomPostProcesses.DoLayoutList();
                serialized.uiBeforeTAACustomPostProcesses.DoLayoutList();
                serialized.uiBeforePostProcessCustomPostProcesses.DoLayoutList();
                serialized.uiAfterPostProcessCustomPostProcesses.DoLayoutList();
            }
        }

        #endregion // Custom Post Processes

        #region Diffusion Profile Settings List

        static readonly CED.IDrawer DiffusionProfileSettingsSection = CED.Group(
            CED.Group((serialized, owner) => EditorGUILayout.LabelField(Styles.diffusionProfileSettingsLabel, Styles.sectionHeaderStyle)),
            CED.Group(Drawer_DiffusionProfileSettings)
        );
        static void Drawer_DiffusionProfileSettings(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            if (m_verboseMode)
                EditorGUILayout.LabelField(Styles.diffusionProfileSettingsIntro, Styles.introStyle);
            using (new EditorGUI.IndentLevelScope())
            {
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
            CED.Group(Drawer_VolumeSection)
        );

        static void Drawer_VolumeSection(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            if (m_verboseMode)
                EditorGUILayout.LabelField(Styles.volumeComponentsIntroLabel, Styles.introStyle);
            using (new EditorGUI.IndentLevelScope())
            {
                var oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = Styles.labelWidth;

                HDRenderPipelineGlobalSettings globalSettings = serialized.serializedObject.targetObject as HDRenderPipelineGlobalSettings;
                VolumeProfile asset = null;
                using (new EditorGUILayout.HorizontalScope())
                {
                    var oldAssetValue = serialized.volumeProfileDefault.objectReferenceValue;
                    EditorGUILayout.PropertyField(serialized.volumeProfileDefault, Styles.defaultVolumeProfileLabel);
                    asset = serialized.volumeProfileDefault.objectReferenceValue as VolumeProfile;
                    if (asset == null && oldAssetValue != null)
                    {
                        Debug.Log("Default Volume Profile Asset cannot be null. Rolling back to previous value.");
                        serialized.volumeProfileDefault.objectReferenceValue = oldAssetValue;
                    }

                    if (GUILayout.Button(EditorGUIUtility.TrTextContent("New", "Create a new Volume Profile for default in your default resource folder (defined in Wizard)"), GUILayout.Width(38), GUILayout.Height(18)))
                    {
                        VolumeProfileCreator.CreateAndAssign(VolumeProfileCreator.Kind.Default, globalSettings);
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
                    EditorGUIUtility.labelWidth -= 18;
                    bool oldEnabled = GUI.enabled;
                    GUI.enabled = AssetDatabase.IsOpenForEdit(asset);
                    m_CachedDefaultVolumeProfileEditor.OnInspectorGUI();
                    GUI.enabled = oldEnabled;
                    EditorGUIUtility.labelWidth = oldWidth;
                }

                EditorGUILayout.Space();

                VolumeProfile lookDevAsset = null;
                using (new EditorGUILayout.HorizontalScope())
                {
                    var oldAssetValue = serialized.volumeProfileLookDev.objectReferenceValue;
                    EditorGUILayout.PropertyField(serialized.volumeProfileLookDev, Styles.lookDevVolumeProfileLabel);
                    lookDevAsset = serialized.volumeProfileLookDev.objectReferenceValue as VolumeProfile;
                    if (lookDevAsset == null && oldAssetValue != null)
                    {
                        Debug.Log("LookDev Volume Profile Asset cannot be null. Rolling back to previous value.");
                        serialized.volumeProfileLookDev.objectReferenceValue = oldAssetValue;
                    }

                    if (GUILayout.Button(EditorGUIUtility.TrTextContent("New", "Create a new Volume Profile for default in your default resource folder (defined in Wizard)"), GUILayout.Width(38), GUILayout.Height(18)))
                    {
                        VolumeProfileCreator.CreateAndAssign(VolumeProfileCreator.Kind.LookDev, globalSettings);
                    }
                }
                if (lookDevAsset != null)
                {
                    Editor.CreateCachedEditor(lookDevAsset, Type.GetType("UnityEditor.Rendering.VolumeProfileEditor"), ref m_CachedLookDevVolumeProfileEditor);
                    EditorGUIUtility.labelWidth -= 18;
                    bool oldEnabled = GUI.enabled;
                    GUI.enabled = AssetDatabase.IsOpenForEdit(lookDevAsset);
                    m_CachedLookDevVolumeProfileEditor.OnInspectorGUI();
                    GUI.enabled = oldEnabled;
                    EditorGUIUtility.labelWidth = oldWidth;

                    if (lookDevAsset.Has<VisualEnvironment>())
                        EditorGUILayout.HelpBox("VisualEnvironment is not modifiable and will be overridden by the LookDev", MessageType.Warning);
                    if (lookDevAsset.Has<HDRISky>())
                        EditorGUILayout.HelpBox("HDRISky is not modifiable and will be overridden by the LookDev", MessageType.Warning);
                }
            }
        }

        #endregion // Volume Profiles

        #region General Settings (log level, layer names)

        static readonly CED.IDrawer LayerNamesSection = CED.Group(
            CED.Group((serialized, owner) => EditorGUILayout.LabelField(Styles.generalSettingsLabel, Styles.sectionHeaderStyle)),
            CED.Group((serialized, owner) => EditorGUILayout.PropertyField(serialized.shaderVariantLogLevel, Styles.shaderVariantLogLevel)),
            CED.Group((serialized, owner) => EditorGUILayout.PropertyField(serialized.lensAttenuation, Styles.lensAttenuationModeContent)),
            CED.Group(Drawer_LightLayerNames),
            CED.Group(Drawer_DecalLayerNames)
        );

        static private bool m_ShowLightLayerNames = false;
        static private bool m_ShowDecalLayerNames = false;
        static void Drawer_LightLayerNames(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            m_ShowLightLayerNames = EditorGUILayout.Foldout(m_ShowLightLayerNames, Styles.lightLayersLabel, true);
            if (m_ShowLightLayerNames)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName0, serialized.lightLayerName0);
                    HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName1, serialized.lightLayerName1);
                    HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName2, serialized.lightLayerName2);
                    HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName3, serialized.lightLayerName3);
                    HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName4, serialized.lightLayerName4);
                    HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName5, serialized.lightLayerName5);
                    HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName6, serialized.lightLayerName6);
                    HDEditorUtils.DrawDelayedTextField(Styles.lightLayerName7, serialized.lightLayerName7);
                }
            }
        }

        static void Drawer_DecalLayerNames(SerializedHDRenderPipelineGlobalSettings serialized, Editor owner)
        {
            m_ShowDecalLayerNames = EditorGUILayout.Foldout(m_ShowDecalLayerNames, Styles.decalLayersLabel, true);
            if (m_ShowDecalLayerNames)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName0, serialized.decalLayerName0);
                    HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName1, serialized.decalLayerName1);
                    HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName2, serialized.decalLayerName2);
                    HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName3, serialized.decalLayerName3);
                    HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName4, serialized.decalLayerName4);
                    HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName5, serialized.decalLayerName5);
                    HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName6, serialized.decalLayerName6);
                    HDEditorUtils.DrawDelayedTextField(Styles.decalLayerName7, serialized.decalLayerName7);
                }
            }
        }

        #endregion
    }

    class VolumeProfileCreator : ProjectWindowCallback.EndNameEditAction
    {
        public enum Kind { Default, LookDev }
        Kind m_Kind;

        void SetKind(Kind kind) => m_Kind = kind;

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var profile = VolumeProfileFactory.CreateVolumeProfileAtPath(pathName);
            ProjectWindowUtil.ShowCreatedAsset(profile);
            Assign(profile);
        }

        void Assign(VolumeProfile profile)
        {
            switch (m_Kind)
            {
                case Kind.Default:
                    settings.volumeProfile = profile;
                    break;
                case Kind.LookDev:
                    settings.volumeProfileLookDev = profile;
                    break;
            }
            EditorUtility.SetDirty(settings);
        }

        static string GetDefaultName(Kind kind)
        {
            string defaultName;
            switch (kind)
            {
                case Kind.Default:
                    defaultName = "VolumeProfile_Default";
                    break;
                case Kind.LookDev:
                    defaultName = "LookDevProfile_Default";
                    break;
                default:
                    defaultName = "N/A";
                    break;
            }
            return defaultName;
        }

        static HDRenderPipelineGlobalSettings settings;
        public static void CreateAndAssign(Kind kind, HDRenderPipelineGlobalSettings globalSettings)
        {
            settings = globalSettings;

            if (settings == null)
            {
                Debug.LogError("Trying to create a Volume Profile for a null HDRP Global Settings. Operation aborted.");
                return;
            }
            var assetCreator = ScriptableObject.CreateInstance<VolumeProfileCreator>();
            assetCreator.SetKind(kind);
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(assetCreator.GetInstanceID(), assetCreator, $"Assets/{HDProjectSettings.projectSettingsFolderPath}/{globalSettings.name}_{GetDefaultName(kind)}.asset", null, null);
        }
    }
}
