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
            public const string firstTimeInitTooltip = "Populate or override Default Resources Folder content with required assets and assign it in GraphicSettings.";
            public const string newSceneLabel = "Default Scene Prefab";
            public const string newSceneTooltip = "This prefab contains scene elements that are used when creating a new scene in HDRP.";
            public const string newDXRSceneLabel = "Default DXR Scene Prefab";
            public const string newDXRSceneTooltip = "This prefab contains scene elements that are used when creating a new scene in HDRP when ray-tracing is activated in the HDRenderPipelineAsset.";
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
            public const string allConfigurationError = "There is issue in your configuration. (See below for detail)";
            public const string colorSpaceLabel = "Color space";
            public const string colorSpaceError = "Only linear color space supported!";
            public const string lightmapLabel = "Lightmap encoding";
            public const string lightmapError = "Only high quality lightmap supported!";
            public const string shadowLabel = "Shadows";
            public const string shadowError = "Shadow must be set to activated! (either on hard or soft)";
            public const string shadowMaskLabel = "Shadowmask mode";
            public const string shadowMaskError = "Only distance shadowmask supported at the project level! (You can still change this per light.)";
            public const string scriptingRuntimeVersionLabel = "Script runtime version";
            public const string scriptingRuntimeVersionError = "Script runtime version must be .Net 4.x or earlier!";
            public const string hdrpAssetLabel = "Asset configuration";
            public const string hdrpAssetError = "There are issues in the HDRP asset configuration. (see below)";
            public const string hdrpAssetUsedLabel = "Assigned";
            public const string hdrpAssetUsedError = "There is no HDRP asset assigned to the render pipeline!";
            public const string hdrpAssetRuntimeResourcesLabel = "Runtime resources";
            public const string hdrpAssetRuntimeResourcesError = "There is an issue with the runtime resources!";
            public const string hdrpAssetEditorResourcesLabel = "Editor resources";
            public const string hdrpAssetEditorResourcesError = "There is an issue with the editor resources!";
            public const string hdrpAssetDiffusionProfileLabel = "Diffusion profile";
            public const string hdrpAssetDiffusionProfileError = "There is no diffusion profile assigned in the HDRP asset!";
            public const string hdrpSRPBatcherLabel = "SRP Batcher";
            public const string hdrpSRPBatcherError = "SRP Batcher must be enabled!";
            public const string defaultSceneLabel = "Default scene prefab";
            public const string defaultSceneError = "Default scene prefab must be set to create HD templated scene!";
            public const string defaultVolumeProfileLabel = "Default volume profile";
            public const string defaultVolumeProfileError = "Default volume profile must be assigned in the HDRP asset!";
            public const string vrSupportedLabel = "VR activated";
            public const string vrSupportedError = "VR need to be enabled in Player Settings!";
            public const string dxrAutoGraphicsAPILabel = "Auto graphics API";
            public const string dxrAutoGraphicsAPIError = "Auto Graphics API is not supported!";
            public const string dxrDirect3D12Label = "Direct3D 12";
            public const string dxrDirect3D12Error = "Direct3D 12 is needed!";
            public const string dxrScreenSpaceShadowLabel = "Screen Space Shadow";
            public const string dxrScreenSpaceShadowError = "Screen Space Shadow is required!";
            public const string dxrStaticBatchingLabel = "Static Batching";
            public const string dxrStaticBatchingError = "Static Batching is not supported!";
            public const string dxrSymbolLabel = "Scripting symbols";
            public const string dxrSymbolError = "REALTIME_RAYTRACING_SUPPORT must be defined!";
            public const string dxrResourcesLabel = "DXR resources";
            public const string dxrResourcesError = "There is an issue with the DXR resources!";
            public const string dxrActivatedLabel = "DXR activated";
            public const string dxrActivatedError = "DXR is not activated!";
            public const string defaultDXRSceneLabel = "Default DXR scene prefab";
            public const string defaultDXRSceneError = "Default DXR scene prefab must be set to create HD templated scene!";

            public const string hdrpAssetDisplayDialogTitle = "Create or Load HDRenderPipelineAsset";
            public const string hdrpAssetDisplayDialogContent = "Do you want to create a fresh HDRenderPipelineAsset in the default resource folder and automatically assign it?";
            public const string diffusionProfileSettingsDisplayDialogTitle = "Create or Load DiffusionProfileSettings";
            public const string diffusionProfileSettingsDisplayDialogContent = "Do you want to create a fresh DiffusionProfileSettings in the default resource folder and automatically assign it?";
            public const string scenePrefabTitle = "Create or Load HD default scene";
            public const string scenePrefabContent = "Do you want to create a fresh HD default scene in the default resource folder and automatically assign it?";
            public const string dxrScenePrefabTitle = "Create or Load DXR HD default scene";
            public const string dxrScenePrefabContent = "Do you want to create a fresh DXR HD default scene in the default resource folder and automatically assign it?";
            public const string displayDialogCreate = "Create One";
            public const string displayDialogLoad = "Load One";
            public const string displayDialogCancel = "Cancel";
        }

        enum Configuration
        {
            HDRP,
            HDRP_VR,
            HDRP_DXR
        };

        Configuration m_Configuration;
        VisualElement m_BaseUpdatable;
        ObjectField m_DefaultScene;
        ObjectField m_DefaultDXRScene;

        [MenuItem("Window/Render Pipeline/HD Render Pipeline Wizard", priority = 10000)]
        static void OpenWindow()
        {
            var window = GetWindow<HDWizard>("HD Render Pipeline Wizard");
            window.minSize = new Vector2(420, 450);
        }

        void OnGUI()
        {
            foreach (VisualElementUpdatable updatable in m_BaseUpdatable.Children().Where(c => c is VisualElementUpdatable))
                updatable.CheckUpdate();
        }

        static HDWizard()
        {
            LoadReflectionMethods();
            WizardBehaviour();
        }

        #region SCRIPT_RELOADING

        static int frameToWait;
        
        static void WizardBehaviourDelayed()
        {
            if (frameToWait > 0)
                --frameToWait;
            else if (HDProjectSettings.wizardIsStartPopup)
            {
                EditorApplication.update -= WizardBehaviourDelayed;

                //Application.isPlaying cannot be called in constructor. Do it here
                if (Application.isPlaying)
                    return;

                OpenWindow();
            }
        }
        
        static void WizardBehaviour()
        {
            //We need to wait at least one frame or the popup will not show up
            frameToWait = 10;
            EditorApplication.update += WizardBehaviourDelayed;
        }
        
        [Callbacks.DidReloadScripts]
        static void ResetDelayed()
        {
            //remove it from domain reload but keep it in editor opening
            frameToWait = 0;
            EditorApplication.update -= WizardBehaviourDelayed;
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
            container.Add(m_DefaultScene = CreateDefaultScene());
            container.Add(m_DefaultDXRScene = CreateDXRDefaultScene());
            
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

        ObjectField CreateDefaultScene()
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

        ObjectField CreateDXRDefaultScene()
        {
            var newDXRScene = new ObjectField(Style.newDXRSceneLabel)
            {
                tooltip = Style.newSceneTooltip,
                name = "NewDXRScene",
                objectType = typeof(GameObject),
                value = HDProjectSettings.defaultDXRScenePrefab
            };
            newDXRScene.Q<Label>().AddToClassList("normal");
            newDXRScene.RegisterValueChangedCallback(evt
                => HDProjectSettings.defaultDXRScenePrefab = evt.newValue as GameObject);

            return newDXRScene;
        }

        VisualElement CreateTabbedBox((string label, string tooltip)[] tabs, out VisualElement innerBox)
        {
            var toolbar = new ToolbarRadio();
            toolbar.AddRadios(tabs);
            toolbar.SetValueWithoutNotify(HDProjectSettings.wizardActiveTab);
            m_Configuration = (Configuration)HDProjectSettings.wizardActiveTab;
            toolbar.RegisterValueChangedCallback(evt =>
            {
                int index = evt.newValue;
                m_Configuration = (Configuration)index;
                HDProjectSettings.wizardActiveTab = index;
            });

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
                value = HDProjectSettings.wizardIsStartPopup,
                name = "WizardCheckbox"
            };
            toggle.RegisterValueChangedCallback(evt
                => HDProjectSettings.wizardIsStartPopup = evt.newValue);
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
            container.Add(new ConfigInfoLine(Style.hdrpAssetUsedLabel, Style.hdrpAssetUsedError, Style.resolve, IsHdrpAssetUsedCorrect, () => FixHdrpAssetUsed(fromAsync: false), indent: 1));
            container.Add(new ConfigInfoLine(Style.hdrpAssetRuntimeResourcesLabel, Style.hdrpAssetRuntimeResourcesError, Style.resolve, IsHdrpAssetRuntimeResourcesCorrect, FixHdrpAssetRuntimeResources, indent: 1));
            container.Add(new ConfigInfoLine(Style.hdrpAssetEditorResourcesLabel, Style.hdrpAssetEditorResourcesError, Style.resolve, IsHdrpAssetEditorResourcesCorrect, FixHdrpAssetEditorResources, indent: 1));
            container.Add(new ConfigInfoLine(Style.hdrpSRPBatcherLabel, Style.hdrpSRPBatcherError, Style.resolve, IsSRPBatcherCorrect, FixSRPBatcher, indent: 1));
            container.Add(new ConfigInfoLine(Style.hdrpAssetDiffusionProfileLabel, Style.hdrpAssetDiffusionProfileError, Style.resolve, IsHdrpAssetDiffusionProfileCorrect, FixHdrpAssetDiffusionProfile, indent: 1));
            container.Add(new ConfigInfoLine(Style.defaultSceneLabel, Style.defaultSceneError, Style.resolve, IsDefaultSceneCorrect, () => FixDefaultScene(fromAsync: false)));
            container.Add(new ConfigInfoLine(Style.defaultVolumeProfileLabel, Style.defaultVolumeProfileError, Style.resolve, IsDefaultVolumeProfileAssigned, FixDefaultVolumeProfileAssigned));
        }

        void AddVRConfigInfo(VisualElement container)
            =>container.Add(new ConfigInfoLine(Style.vrSupportedLabel, Style.vrSupportedError, Style.resolve, IsVRSupportedForCurrentBuildTargetGroupCorrect, FixVRSupportedForCurrentBuildTargetGroup));

        void AddDXRConfigInfo(VisualElement container)
        {
            container.Add(new ConfigInfoLine(Style.dxrAutoGraphicsAPILabel, Style.dxrAutoGraphicsAPIError, Style.resolve, IsDXRAutoGraphicsAPICorrect, FixDXRAutoGraphicsAPI));
            container.Add(new ConfigInfoLine(Style.dxrDirect3D12Label, Style.dxrDirect3D12Error, Style.resolve, IsDXRDirect3D12Correct, () => FixDXRDirect3D12(fromAsync: false)));
            container.Add(new ConfigInfoLine(Style.dxrStaticBatchingLabel, Style.dxrStaticBatchingError, Style.resolve, IsDXRStaticBatchingCorrect, FixDXRStaticBatching));
            container.Add(new ConfigInfoLine(Style.dxrScreenSpaceShadowLabel, Style.dxrScreenSpaceShadowError, Style.resolve, IsDXRScreenSpaceShadowCorrect, FixDXRScreenSpaceShadow));
            container.Add(new ConfigInfoLine(Style.dxrActivatedLabel, Style.dxrActivatedError, Style.resolve, IsDXRActivationCorrect, FixDXRActivation));
            container.Add(new ConfigInfoLine(Style.dxrResourcesLabel, Style.dxrResourcesError, Style.resolve, IsDXRAssetCorrect, FixDXRAsset));
            container.Add(new ConfigInfoLine(Style.defaultDXRSceneLabel, Style.defaultDXRSceneError, Style.resolve, IsDXRDefaultSceneCorrect, () => FixDXRDefaultScene(fromAsync: false)));
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

