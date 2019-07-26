using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
using System;

namespace UnityEditor.Rendering.HighDefinition
{
    [InitializeOnLoad]
    partial class HDWizard : EditorWindow
    {
        static class Style
        {
            public static readonly GUIContent title = EditorGUIUtility.TrTextContent("Render Pipeline Wizard");

            public const string hdrpProjectSettingsPathLabel = "Default Resources Folder";
            public const string hdrpProjectSettingsPathTooltip = "Resources Folder will be the one where to get project elements related to HDRP as default scene and default settings.";
            public const string firstTimeInitLabel = "Populate / Reset";
            public const string firstTimeInitTooltip = "Populate or override Default Resources Folder content with required assets.";
            public const string newSceneLabel = "Default Scene Prefab";
            public const string newSceneTooltip = "This prefab contains scene elements that are used when creating a new scene in HDRP.";
            public const string hdrpConfigLabel = "HDRP";
            public const string hdrpConfigTooltip = "This tab contains configuration check for High Definition Render Pipeline.";
            public const string hdrpVRConfigLabel = "HDRP + VR";
            public const string hdrpVRConfigTooltip = "This tab contains configuration check for High Definition Render Pipeline along with Virtual Reality configuration.";
            public const string hdrpDXRConfigLabel = "HDRP + DXR";
            public const string hdrpDXRConfigTooltip = "This tab contains configuration check for High Definition Render Pipeline along with DirectX Raytracing configuration.";
            public const string showOnStartUp = "Show on start";

            public const string defaultSettingsTitle = "Default Path Settings";
            public const string configurationTitle = "Configuration Checking";
            public const string migrationTitle = "Project Migration Quick-links";

            public const string migrateAllButton = "Upgrade Project Materials to High Definition Materials";
            public const string migrateSelectedButton = "Upgrade Selected Materials to High Definition Materials";
            public const string migrateLights = "Upgrade Unity Builtin Scene Light Intensity for High Definition";

            //configuration debugger
            public const string resolve = "Fix";
            public const string resolveAll = "Fix All";
            public const string resolveAllQuality = "Fix All Qualities";
            public const string resolveAllBuildTarget = "Fix All Platforms";
            public static readonly GUIContent hdrpConfigurationLabel = EditorGUIUtility.TrTextContent("HDRP configuration");
            public static readonly GUIContent vrConfigurationLabel = EditorGUIUtility.TrTextContent("VR additional configuration");
            public static readonly GUIContent dxrConfigurationLabel = EditorGUIUtility.TrTextContent("DXR additional configuration");
            public const string allConfigurationError = "There is issue in your configuration. (See below for detail)";
            public static readonly GUIContent colorSpaceLabel = EditorGUIUtility.TrTextContent("Color space");
            public const string colorSpaceError = "Only linear color space supported!";
            public static readonly GUIContent lightmapLabel = EditorGUIUtility.TrTextContent("Lightmap encoding");
            public const string lightmapError = "Only high quality lightmap supported!";
            public static readonly GUIContent shadowLabel = EditorGUIUtility.TrTextContent("Shadows");
            public const string shadowError = "Shadow must be set to activated! (either on hard or soft)";
            public static readonly GUIContent shadowMaskLabel = EditorGUIUtility.TrTextContent("Shadowmask mode");
            public const string shadowMaskError = "Only distance shadowmask supported at the project level! (You can still change this per light.)";
            public static readonly GUIContent scriptingRuntimeVersionLabel = EditorGUIUtility.TrTextContent("Script runtime version");
            public const string scriptingRuntimeVersionError = "Script runtime version must be .Net 4.x or earlier!";
            public static readonly GUIContent hdrpAssetLabel = EditorGUIUtility.TrTextContent("Asset configuration");
            public const string hdrpAssetError = "There are issues in the HDRP asset configuration. (see below)";
            public static readonly GUIContent hdrpAssetUsedLabel = EditorGUIUtility.TrTextContent("Assigned");
            public const string hdrpAssetUsedError = "There is no HDRP asset assigned to the render pipeline!";
            public static readonly GUIContent hdrpAssetRuntimeResourcesLabel = EditorGUIUtility.TrTextContent("Runtime resources");
            public const string hdrpAssetRuntimeResourcesError = "There is an issue with the runtime resources!";
            public static readonly GUIContent hdrpAssetEditorResourcesLabel = EditorGUIUtility.TrTextContent("Editor resources");
            public const string hdrpAssetEditorResourcesError = "There is an issue with the editor resources!";
            public static readonly GUIContent hdrpAssetDiffusionProfileLabel = EditorGUIUtility.TrTextContent("Diffusion profile");
            public const string hdrpAssetDiffusionProfileError = "There is no diffusion profile assigned in the HDRP asset!";
            public static readonly GUIContent defaultVolumeProfileLabel = EditorGUIUtility.TrTextContent("Default scene prefab");
            public const string defaultVolumeProfileError = "Default scene prefab must be set to create HD templated scene!";
            public static readonly GUIContent vrSupportedLabel = EditorGUIUtility.TrTextContent("VR activated");
            public const string vrSupportedError = "VR need to be enabled in Player Settings!";
            public static readonly GUIContent dxrAutoGraphicsAPILabel = EditorGUIUtility.TrTextContent("Auto graphics API");
            public const string dxrAutoGraphicsAPIError = "Auto Graphics API is not supported!";
            public static readonly GUIContent dxrDirect3D12Label = EditorGUIUtility.TrTextContent("Direct3D 12");
            public const string dxrDirect3D12Error = "Direct3D 12 is needed!";
            public static readonly GUIContent dxrSymbolLabel = EditorGUIUtility.TrTextContent("Scripting symbols");
            public const string dxrSymbolError = "REALTIME_RAYTRACING_SUPPORT must be defined!";
            public static readonly GUIContent dxrResourcesLabel = EditorGUIUtility.TrTextContent("DXR resources");
            public const string dxrResourcesError = "There is an issue with the DXR resources!";
            public static readonly GUIContent dxrActivatedLabel = EditorGUIUtility.TrTextContent("DXR activated");
            public const string dxrActivatedError = "DXR is not activated!";

            public const string hdrpAssetDisplayDialogTitle = "Create or Load HDRenderPipelineAsset";
            public const string hdrpAssetDisplayDialogContent = "Do you want to create a fresh HDRenderPipelineAsset in the default resource folder and automatically assign it?";
            public const string diffusionProfileSettingsDisplayDialogTitle = "Create or Load DiffusionProfileSettings";
            public const string diffusionProfileSettingsDisplayDialogContent = "Do you want to create a fresh DiffusionProfileSettings in the default resource folder and automatically assign it?";
            public const string scenePrefabTitle = "Create or Load HD default scene";
            public const string scenePrefabContent = "Do you want to create a fresh HD default scene in the default resource folder and automatically assign it?";
            public const string displayDialogCreate = "Create One";
            public const string displayDialogLoad = "Load One";
        }

        enum Configuration
        {
            HDRP,
            HDRP_VR,
            HDRP_DXR
        };

        Configuration m_Configuration;
        VisualElement m_BaseUpdatable;

        [MenuItem("Window/Render Pipeline/HD Render Pipeline Wizard", priority = 10000)]
        static void OpenWindow()
            => GetWindow<HDWizard>("Render Pipeline Wizard");

        void OnGUI()
        {
            foreach (VisualElementUpdatable updatable in m_BaseUpdatable.Children().Where(c => c is VisualElementUpdatable))
                updatable.CheckUpdate();
        }
        
        #region SCRIPT_RELOADING

        static int frameToWait;

        static void OpenWindowDelayed()
        {
            if (frameToWait > 0)
                --frameToWait;
            else
            {
                EditorApplication.update -= OpenWindowDelayed;

                //Application.isPlaying cannot be called in constructor. Do it here
                if (Application.isPlaying)
                    return;

                OpenWindow();
            }
        }

        [Callbacks.DidReloadScripts]
        static void ResetDelayed()
        {
            //remove it from domain reload but keep it in editor opening
            frameToWait = 0;
            EditorApplication.update -= OpenWindowDelayed;
        }

        #endregion

        #region DRAWERS

        private void OnEnable()
        {
            titleContent = Style.title;
            
            HDEditorUtils.AddStyleSheets(rootVisualElement, HDEditorUtils.FormatingPath); //.h1
            HDEditorUtils.AddStyleSheets(rootVisualElement, HDEditorUtils.WizardSheetPath);

            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            rootVisualElement.Add(scrollView);
            var container = scrollView.contentContainer;

            container.Add(CreateTitle(Style.defaultSettingsTitle));
            container.Add(CreateFolderData());
            container.Add(CreateDefaultScene());
            
            container.Add(CreateTitle(Style.configurationTitle));
            container.Add(CreateTabbedBox(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? new[] {
                        (Style.hdrpConfigLabel, Style.hdrpConfigTooltip),
                        (Style.hdrpVRConfigLabel, Style.hdrpVRConfigTooltip),
                        (Style.hdrpDXRConfigLabel, Style.hdrpDXRConfigTooltip),
                    }
                    : new[] {
                        (Style.hdrpConfigLabel, Style.hdrpConfigTooltip),
                        (Style.hdrpVRConfigLabel, Style.hdrpVRConfigTooltip),
                        //DXR only supported on window
                    },
                out m_BaseUpdatable));

            AddHDRPConfigInfo(m_BaseUpdatable);

            var vrScope = new HiddableUpdatableContainer(()
                => m_Configuration == Configuration.HDRP_VR);
            AddVRConfigInfo(vrScope);
            vrScope.Init();
            m_BaseUpdatable.Add(vrScope);
            
            var dxrScope = new HiddableUpdatableContainer(()
                => m_Configuration == Configuration.HDRP_DXR);
            AddDXRConfigInfo(dxrScope);
            dxrScope.Init();
            m_BaseUpdatable.Add(dxrScope);

            m_BaseUpdatable.Add(new FixAllButton(
                Style.resolveAll,
                () =>
                {
                    bool isCorrect = IsHDRPAllCorrect();
                    switch (m_Configuration)
                    {
                        case Configuration.HDRP_VR:
                            isCorrect &= IsVRAllCorrect();
                            break;
                        case Configuration.HDRP_DXR:
                            isCorrect &= IsDXRAllCorrect();
                            break;
                    }
                    return isCorrect;
                },
                () =>
                {
                    FixHDRPAll();
                    switch (m_Configuration)
                    {
                        case Configuration.HDRP_VR:
                            FixVRAll();
                            break;
                        case Configuration.HDRP_DXR:
                            FixDXRAll();
                            break;
                    }
                }));


            container.Add(CreateTitle(Style.migrationTitle));
            container.Add(CreateMigrationButton(Style.migrateAllButton, UpgradeStandardShaderMaterials.UpgradeMaterialsProject));
            container.Add(CreateMigrationButton(Style.migrateSelectedButton, UpgradeStandardShaderMaterials.UpgradeMaterialsSelection));
            container.Add(CreateMigrationButton(Style.migrateLights, UpgradeStandardShaderMaterials.UpgradeLights));

            container.Add(CreateWizardBehaviour());
        }

        VisualElement CreateFolderData()
        {
            var defaultResourceFolder = new TextField(Style.hdrpProjectSettingsPathLabel)
            {
                tooltip = Style.hdrpProjectSettingsPathTooltip,
                name = "DefaultResourceFolder",
                value = HDProjectSettings.projectSettingsFolderPath
            };
            defaultResourceFolder.Q<Label>().AddToClassList("normal");
            defaultResourceFolder.RegisterValueChangedCallback(evt
                => HDProjectSettings.projectSettingsFolderPath = evt.newValue);

            var repopulate = new Button(Repopulate)
            {
                text = Style.firstTimeInitLabel,
                tooltip = Style.firstTimeInitTooltip,
                name = "Repopulate"
            };

            var row = new VisualElement() { name = "ResourceRow" };
            row.Add(defaultResourceFolder);
            row.Add(repopulate);

            return row;
        }

        VisualElement CreateDefaultScene()
        {
            var newScene = new ObjectField(Style.newSceneLabel)
            {
                tooltip = Style.newSceneTooltip,
                name = "NewScene",
                objectType = typeof(GameObject),
                value = HDProjectSettings.defaultScenePrefab
            };
            newScene.Q<Label>().AddToClassList("normal");
            newScene.RegisterValueChangedCallback(evt
                => HDProjectSettings.defaultScenePrefab = evt.newValue as GameObject);

            return newScene;
        }

        VisualElement CreateTabbedBox((string label, string tooltip)[] tabs, out VisualElement innerBox)
        {
            var toolbar = new ToolbarRadio();
            toolbar.AddRadios(tabs);
            toolbar.RegisterValueChangedCallback(evt =>
                m_Configuration = (Configuration)evt.newValue);

            var outerBox = new VisualElement() { name = "OuterBox" };
            innerBox = new VisualElement { name = "InnerBox" };

            outerBox.Add(toolbar);
            outerBox.Add(innerBox);

            return outerBox;
        }

        VisualElement CreateWizardBehaviour()
        {
            var toggle = new Toggle(Style.showOnStartUp)
            {
                value = HDProjectSettings.hasStartPopup,
                name = "WizardCheckbox"
            };
            toggle.RegisterValueChangedCallback(evt
                => HDProjectSettings.hasStartPopup = evt.newValue);
            return toggle;
        }

        VisualElement CreateMigrationButton(string title, Action action)
            => new Button(action)
            {
                text = title,
                name = "MigrationButton"
            };

        void AddHDRPConfigInfo(VisualElement container)
        {
            container.Add(new ConfigInfoLine(Style.colorSpaceLabel, Style.colorSpaceError, Style.resolve, IsColorSpaceCorrect, FixColorSpace));
            container.Add(new ConfigInfoLine(Style.lightmapLabel, Style.lightmapError, Style.resolveAllBuildTarget, IsLightmapCorrect, FixLightmap));
            container.Add(new ConfigInfoLine(Style.shadowMaskLabel, Style.shadowMaskError,Style.resolveAllQuality, IsShadowmaskCorrect, FixShadowmask));
            container.Add(new ConfigInfoLine(Style.hdrpAssetLabel, Style.hdrpAssetError, Style.resolveAll, IsHdrpAssetCorrect, FixHdrpAsset));
            container.Add(new ConfigInfoLine(Style.hdrpAssetUsedLabel, Style.hdrpAssetUsedError, Style.resolve, IsHdrpAssetUsedCorrect, () => FixHdrpAssetUsed(async: false), indent: 1));
            container.Add(new ConfigInfoLine(Style.hdrpAssetRuntimeResourcesLabel, Style.hdrpAssetRuntimeResourcesError, Style.resolve, IsHdrpAssetRuntimeResourcesCorrect, FixHdrpAssetRuntimeResources, indent: 1));
            container.Add(new ConfigInfoLine(Style.hdrpAssetEditorResourcesLabel, Style.hdrpAssetEditorResourcesError, Style.resolve, IsHdrpAssetEditorResourcesCorrect, FixHdrpAssetEditorResources, indent: 1));
            container.Add(new ConfigInfoLine(Style.hdrpAssetDiffusionProfileLabel, Style.hdrpAssetDiffusionProfileError, Style.resolve, IsHdrpAssetDiffusionProfileCorrect, FixHdrpAssetDiffusionProfile, indent: 1));
            container.Add(new ConfigInfoLine(Style.defaultVolumeProfileLabel, Style.defaultVolumeProfileError, Style.resolve, IsDefaultSceneCorrect, () => FixDefaultScene(async: false)));
        }

        void AddVRConfigInfo(VisualElement container)
            =>container.Add(new ConfigInfoLine(Style.vrSupportedLabel, Style.vrSupportedError, Style.resolve, IsVRSupportedForCurrentBuildTargetGroupCorrect, FixVRSupportedForCurrentBuildTargetGroup));

        void AddDXRConfigInfo(VisualElement container)
        {
            container.Add(new ConfigInfoLine(Style.dxrAutoGraphicsAPILabel, Style.dxrAutoGraphicsAPIError, Style.resolve, IsDXRAutoGraphicsAPICorrect, FixDXRAutoGraphicsAPI));
            container.Add(new ConfigInfoLine(Style.dxrDirect3D12Label, Style.dxrDirect3D12Error, Style.resolve, IsDXRDirect3D12Correct, () => FixDXRDirect3D12(fromAsync: false)));
            container.Add(new ConfigInfoLine(Style.dxrSymbolLabel, Style.dxrSymbolError, Style.resolve, IsDXRCSharpKeyWordCorrect, FixDXRCSharpKeyWord));
            container.Add(new ConfigInfoLine(Style.dxrActivatedLabel, Style.dxrActivatedError, Style.resolve, IsDXRActivationCorrect, FixDXRActivation));
            container.Add(new ConfigInfoLine(Style.dxrResourcesLabel, Style.dxrResourcesError, Style.resolve, IsDXRAssetCorrect, FixDXRAsset));
        }

        Label CreateTitle(string title)
        {
            var label = new Label(title);
            label.AddToClassList("h1");
            return label;
        }

        #endregion
    }
}

