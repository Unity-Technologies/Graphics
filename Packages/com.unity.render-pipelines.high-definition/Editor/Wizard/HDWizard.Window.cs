using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEditor.Rendering.Analytics;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.HighDefinition
{
    [InitializeOnLoad]
    [HDRPHelpURL("Render-Pipeline-Wizard")]
    partial class HDWizard : EditorWindowWithHelpButton
    {
        static class Style
        {
            public static readonly GUIContent title = EditorGUIUtility.TrTextContent("HDRP Wizard");

            public static readonly string hdrpProjectSettingsPathLabel = L10n.Tr("Default Resources Folder");
            public static readonly string hdrpProjectSettingsPathTooltip = L10n.Tr("Resources Folder will be the one where to get project elements related to HDRP as default scene and default settings.");
            public const string hdrpConfigLabel = "HDRP";
            public static readonly string hdrpConfigTooltip = L10n.Tr("This tab contains configuration check for High Definition Render Pipeline.");
            public const string hdrpVRConfigLabel = "VR";
            public static readonly string hdrpVRConfigTooltip = L10n.Tr("This tab contains configuration check for Virtual Reality configuration.");
            public const string hdrpDXRConfigLabel = "DXR";
            public static readonly string hdrpDXRConfigTooltip = L10n.Tr("This tab contains configuration check for DirectX Raytracing configuration.");
            public static readonly string showOnStartUp = L10n.Tr("Show on start");

            public static readonly string defaultSettingsTitle = L10n.Tr("General Settings");
            public static readonly string configurationTitle = L10n.Tr("Configuration Checking");
            public static readonly string migrationTitle = L10n.Tr("Project Migration Quick-links");

            public static readonly string installConfigPackageLabel = L10n.Tr("Embed Configuration Editable Package");
            public static readonly string installConfigPackageInfoInCheck = L10n.Tr("Checking if the config package is embedded in your project.");
            public static readonly string installConfigPackageInfoInProgress = L10n.Tr("The config package is being embedded in your project.");
            public static readonly string installConfigPackageInfoFinished = L10n.Tr("The config package is already embedded in your project.");

            public static readonly string migrateAllButton = L10n.Tr("Convert All Built-in Materials to HDRP");
            public static readonly string migrateSelectedButton = L10n.Tr("Convert Selected Built-in Materials to HDRP");
            public static readonly string migrateMaterials = L10n.Tr("Upgrade HDRP Materials to Latest Version");

            public static readonly string OpenPackageManager = L10n.Tr("Package Manager");
            public static readonly string checking = L10n.Tr("checking ...");
            public static readonly string local = L10n.Tr(" (local)");

            //configuration debugger
            public static readonly string global = L10n.Tr("Global");
            public static readonly string currentQuality = L10n.Tr("Current Quality");

            public static readonly string resolve = L10n.Tr("Fix");
            public static readonly string resolveAll = L10n.Tr("Fix All");
            public static readonly string resolveAllQuality = L10n.Tr("Fix All Qualities");
            public static readonly string resolveAllBuildTarget = L10n.Tr("Fix All Platforms");
            public static readonly string fixAllOnNonHDRP = L10n.Tr("The active Quality Level is not using a High Definition Render Pipeline asset. If you attempt a Fix All, the Quality Level will be changed to use it.");
            public static readonly string nonBuiltinMaterialWarning = L10n.Tr("The project contains materials that are not using built-in shaders. These will be skipped in the automated migration process.");

            public struct ConfigStyle
            {
                public readonly string label;
                public readonly string error;
                public readonly string button;
                public readonly MessageType messageType;
                public ConfigStyle(string label, string error, string button = null, MessageType messageType = MessageType.Error)
                {
                    if (button == null)
                        button = resolve;
                    this.label = label;
                    this.error = error;
                    this.button = button;
                    this.messageType = messageType;
                }
            }

            public static readonly ConfigStyle hdrpColorSpace = new ConfigStyle(
                label: L10n.Tr("Color space"),
                error: L10n.Tr("Only linear color space supported!"));
            public static readonly ConfigStyle hdrpLightmapEncoding = new ConfigStyle(
                label: L10n.Tr("Lightmap encoding"),
                error: L10n.Tr("Only high quality lightmap supported!"),
                button: resolveAllBuildTarget);
            public static readonly ConfigStyle hdrpShadow = new ConfigStyle(
                label: L10n.Tr("Shadows"),
                error: L10n.Tr("Shadow must be set to activated! (both hard and soft)"));
            public static readonly ConfigStyle hdrpShadowmask = new ConfigStyle(
                label: L10n.Tr("Shadowmask mode"),
                error: L10n.Tr("Only distance shadowmask supported at the project level! (You can still change this per light.)"),
                button: resolveAllQuality);
            public static readonly ConfigStyle hdrpAssetGraphicsAssigned = new ConfigStyle(
                label: L10n.Tr("Assigned - Default Render Pipeline in Graphics Settings"),
                error: L10n.Tr("There is no HDRP asset assigned to the Graphic Settings!"));
            public static readonly ConfigStyle hdrpGraphicsSettingsExists = new ConfigStyle(
                label: L10n.Tr("Settings and Resources"),
                error: L10n.Tr("Some IRenderPipelineGraphicsSettings are missing on the global settings!"));
            public static readonly ConfigStyle hdrpGlobalSettingsAssigned = new ConfigStyle(
                label: L10n.Tr("Global Settings Asset"),
                error: L10n.Tr("The Global Settings Asset is missing or out of date."));
            public static readonly ConfigStyle hdrpAssetQualityAssigned = new ConfigStyle(
                label: L10n.Tr("Assigned - Render Pipeline Asset in Quality Settings"),
                error: L10n.Tr("The RenderPipelineAsset assigned in the current Quality must be null or a HDRenderPipelineAsset. If it is null, the asset for the current Quality will be the one in Graphics Settings. (The Fix or Fix All button will nullify it)"));
            public static readonly ConfigStyle hdrpBatcher = new ConfigStyle(
                label: L10n.Tr("SRP Batcher"),
                error: L10n.Tr("SRP Batcher must be enabled!"));
            public static readonly ConfigStyle hdrpDiffusionProfile = new ConfigStyle(
                label: L10n.Tr("Diffusion profile"),
                error: L10n.Tr("There is no diffusion profile assigned in the Project Settings > Graphics > HDRP"));
            public static readonly ConfigStyle hdrpVolumeProfile = new ConfigStyle(
                label: L10n.Tr("Default volume profile"),
                error: L10n.Tr("Default volume profile must be assigned in the Project Settings > Graphics > HDRP! Also, for it to be editable, it should be outside of package."));
            public static readonly ConfigStyle hdrpLookDevVolumeProfile = new ConfigStyle(
                label: L10n.Tr("Default Look Dev volume profile"),
                error: L10n.Tr("Default Look Dev volume profile must be assigned in the Project Settings > Graphics > HDRP! Also, for it to be editable, it should be outside of package."));

            public static readonly ConfigStyle hdrpMigratableAssets = new ConfigStyle(
                label: L10n.Tr("Assets Migration"),
                error: L10n.Tr("At least one of the HDRP assets used in quality or the current HDRenderPipelineGlobalSettings have not been migrated to last version."));

            public static readonly ConfigStyle vrLegacyVRSystem = new ConfigStyle(
                label: L10n.Tr("Legacy VR System"),
                error: L10n.Tr("Legacy VR System need to be disabled in Player Settings!"));
            public static readonly ConfigStyle vrXRManagementPackage = new ConfigStyle(
                label: L10n.Tr("XR Management Package"),
                error: L10n.Tr("XR Management Package is required to run in VR!"));
            public static readonly ConfigStyle vrXRManagementPackageInstalled = new ConfigStyle(
                label: L10n.Tr("Package Installed"),
                error: L10n.Tr("Last version of XR Management Package must be added in your project!"));
            public static readonly ConfigStyle vrOculusPlugin = new ConfigStyle(
                label: L10n.Tr("Oculus Plugin"),
                error: L10n.Tr("Oculus Plugin must installed manually.\nGo in Edit > Project Settings > XR Plugin Manager and add Oculus XR Plugin.\n(This can't be verified by the Wizard)"),
                messageType: MessageType.Info);
            public static readonly ConfigStyle vrSinglePassInstancing = new ConfigStyle(
                label: L10n.Tr("Single-Pass Instancing"),
                error: L10n.Tr("Single-Pass Instancing must be enabled in Oculus Plugin.\nGo in Edit > Project Settings > XR Plugin Manager > Oculus and change Stereo Rendering Mode to Single Pass Instanced.\n(This can't be verified by the Wizard)"),
                messageType: MessageType.Info);
            public static readonly ConfigStyle vrLegacyHelpersPackage = new ConfigStyle(
                label: L10n.Tr("XR Legacy Helpers Package"),
                error: L10n.Tr("XR Legacy Helpers Package will help you to handle inputs."));

            public static readonly ConfigStyle dxrAutoGraphicsAPI = new ConfigStyle(
                label: L10n.Tr("Auto graphics API"),
                error: L10n.Tr("Auto Graphics API is not supported!"));

            public static readonly ConfigStyle dxrAutoGraphicsAPIWarning_WindowsOnly = new ConfigStyle(
                label: L10n.Tr("Auto graphics API"),
                error: L10n.Tr("Auto Graphics API is not supported on Windows!"),
                messageType: MessageType.Warning);

            public static readonly ConfigStyle dxrD3D12 = new ConfigStyle(
                label: L10n.Tr("Direct3D 12"),
                error: L10n.Tr("Direct3D 12 needs to be the active device! (Editor restart is required). If an API different than D3D12 is forced via command line argument, clicking Fix won't change it, so please consider removing it if wanting to run DXR."));

            public static readonly ConfigStyle dxrD3D12Warning_WindowsOnly = new ConfigStyle(
                label: L10n.Tr("Direct3D 12"),
                error: L10n.Tr("Direct3D 12 needs to be the active device on windows! (Editor restart is required). If an API different than D3D12 is forced via command line argument, clicking Fix won't change it, so please consider removing it if wanting to run DXR."),
                messageType: MessageType.Warning);

            public static readonly ConfigStyle dxrScreenSpaceShadow = new ConfigStyle(
                label: L10n.Tr("Screen Space Shadows (Asset)"),
                error: L10n.Tr("Screen Space Shadows are disabled in the current HDRP Asset which means you cannot enable ray-traced shadows for lights in your scene. To enable this feature, open your HDRP Asset, go to Lighting > Shadows, and enable Screen Space Shadows."),
                messageType: MessageType.Warning);
            public static readonly ConfigStyle dxrScreenSpaceShadowFS = new ConfigStyle(
                label: L10n.Tr("Screen Space Shadows (HDRP Graphics Settings)"),
                error: L10n.Tr($"Screen Space Shadows are disabled in the default Camera Frame Settings. This means Cameras that use these Frame Settings do not render ray-traced shadows. To enable this feature, go to Project Settings > Graphics > HDRP > Frame Settings (Default Values) > Camera > Lighting and enable Screen Space Shadows. This configuration depends on {dxrScreenSpaceShadow.label}. This means, before you fix this, you must fix {dxrScreenSpaceShadow.label} first."),
                messageType: MessageType.Info);
            public static readonly ConfigStyle dxrReflections = new ConfigStyle(
                label: L10n.Tr("Screen Space Reflection (Asset)"),
                error: L10n.Tr("Screen Space Reflection is disabled in the current HDRP Asset which means you cannot enable ray-traced reflections in Volume components. To enable this feature, open your HDRP Asset, go to Lighting > Reflections, and enable Screen Space Reflections."),
                messageType: MessageType.Warning);
            public static readonly ConfigStyle dxrReflectionsFS = new ConfigStyle(
                label: L10n.Tr("Screen Space Reflection (HDRP Graphics Settings)"),
                error: L10n.Tr($"Screen Space Reflection is disabled in the default Camera Frame Settings. This means Cameras that use these Frame Settings do not render ray-traced reflections. To enable this feature, go to Project Settings > Graphics > HDRP > Frame Settings (Default Values) > Camera > Lighting and enable Screen Space Reflections. This configuration depends on {dxrReflections.label}. This means, before you fix this, you must fix {dxrReflections.label} first."),
                messageType: MessageType.Info);
            public static readonly ConfigStyle dxrTransparentReflections = new ConfigStyle(
                label: L10n.Tr("Screen Space Reflection - Transparent (Asset)"),
                error: L10n.Tr("Screen Space Reflection - Transparent is disabled in the current HDRP Asset which means you cannot enable ray-traced reflections on transparent GameObjects from Volume components. To enable this feature, open your HDRP Asset, go to Lighting > Reflections, and enable Transparents receive SSR."),
                messageType: MessageType.Warning);
            public static readonly ConfigStyle dxrTransparentReflectionsFS = new ConfigStyle(
                label: L10n.Tr("Screen Space Reflection - Transparent (HDRP Graphics Settings)"),
                error: L10n.Tr($"Screen Space Reflection - Transparent is disabled in the default Camera Frame Settings. This means Cameras that use these Frame Settings do not render ray-traced reflections on transparent GameObjects. To enable this feature, go to Project Settings > Graphics > HDRP > Frame Settings (Default Values) > Camera > Lighting and enable Transparents. This configuration depends on {dxrTransparentReflections.label}. This means, before you fix this, you must fix {dxrTransparentReflections.label} first."),
                messageType: MessageType.Info);
            public static readonly ConfigStyle dxrGI = new ConfigStyle(
                label: L10n.Tr("Screen Space Global Illumination (Asset)"),
                error: L10n.Tr($"Screen Space Global Illumination is disabled in the current HDRP asset which means you cannot enable ray-traced global illumination in Volume components.{Environment.NewLine} To enable this feature, open your HDRP Asset, go to Lighting and enable Screen Space Global Illumination."),
                messageType: MessageType.Warning);
            public static readonly ConfigStyle dxrGIFS = new ConfigStyle(
                label: L10n.Tr("Screen Space Global Illumination (HDRP Graphics Settings)"),
                error: L10n.Tr($"Screen Space Global Illumination is disabled in the default Camera Frame Settings. This means Cameras that use these Frame Settings do not render ray-traced global illumination.{Environment.NewLine}To enable this feature, go to Project Settings > Graphics > HDRP > Frame Settings (Default Values) > Camera > Lighting and enable Screen Space Global Illumination. This configuration depends on {dxrGI.label}. This means, before you fix this, you must fix {dxrGI.label} first."),
                messageType: MessageType.Info);
            public static readonly ConfigStyle dxrBuildTarget = new ConfigStyle(
                label: L10n.Tr("Build Target"),
                error: L10n.Tr("To build your Project as a Unity Player your build target must be StandaloneWindows64, Playstation5 or Xbox series X"));
            public static readonly ConfigStyle dxrActivated = new ConfigStyle(
                label: L10n.Tr("DXR activated"),
                error: L10n.Tr("DXR is not activated!"));
            public static readonly ConfigStyle dxrResources = new ConfigStyle(
                label: L10n.Tr("DXR resources"),
                error: L10n.Tr("There is an issue with the DXR resources! Alternatively, Direct3D is not set as API (can be fixed with option above) or your hardware and/or OS cannot be used for DXR! (unfixable)"));

            public static readonly ConfigStyle dxrVfx = new ConfigStyle(
                label: L10n.Tr("Ray-traced Visual Effects (Asset)"),
                error: L10n.Tr($"Visual Effects Ray Tracing are disabled in the current HDRP asset which means you cannot enable Ray Tracing for Visual Effects. To enable this feature, open your HDRP Asset, go to Rendering, and enable Visual Effects Ray Tracing. This configuration depends on {dxrActivated.label}. This means, before you fix this, you must fix {dxrActivated.label} first."),
                messageType: MessageType.Warning);
            public static readonly ConfigStyle dxrVfxFS = new ConfigStyle(
                label: L10n.Tr("Ray-traced Visual Effects (HDRP Graphics Settings)"),
                error: L10n.Tr($"Visual Effects Ray Tracing are disabled in the default Camera Frame Settings. This means Cameras that use these Frame Settings do not render visual effects. To enable this feature, go to Project Settings > Graphics > HDRP > Frame Settings (Default Values) > Camera > Lighting and enable Ray Tracing VFX. This configuration depends on {dxrVfx.label}. This means, before you fix this, you must fix {dxrVfx.label} first."),
                messageType: MessageType.Info);

            public static readonly string hdrpAssetDisplayDialogTitle = L10n.Tr("Create or Load HDRenderPipelineAsset");
            public static readonly string hdrpAssetDisplayDialogContent = L10n.Tr("Do you want to create a fresh HDRenderPipelineAsset in the default resource folder and automatically assign it?");
            public static readonly string displayDialogCreate = L10n.Tr("Create One");
            public static readonly string displayDialogLoad = L10n.Tr("Load One");
            public static readonly string displayDialogCancel = L10n.Tr("Cancel");
        }

        enum Configuration
        {
            HDRP,
            HDRP_VR,
            HDRP_DXR
        }

        enum ConfigPackageState
        {
            BeingChecked,
            Missing,
            Present,
            BeingFixed
        }

        Configuration m_Configuration;
        private List<VisualElementUpdatable> m_UpdatableElements = new();
        VisualElement m_BaseUpdatable;
        VisualElement m_InstallConfigPackageHelpbox = null;
        VisualElement m_InstallConfigPackageButton = null;
        UnityEngine.UIElements.Label m_InstallConfigPackageHelpboxLabel;

        [MenuItem("Window/Rendering/HDRP Wizard", priority = 10000)]
        static internal void OpenWindow()
        {
            var window = GetWindow<HDWizard>(Style.title.text);
            window.minSize = new Vector2(500, 450);
            HDUserSettings.wizardPopupAlreadyShownOnce = true;
        }

        [MenuItem("Window/Rendering/HDRP Wizard", priority = 10000, validate = true)]
        static bool CanShowWizard()
        {
            // If the user has more than one SRP installed, only show the Wizard if the pipeline is HDRP
            return HDRenderPipeline.isReady || RenderPipelineManager.currentPipeline == null;
        }

        float lastUpdateTime = 0f;
        const float updateInterval = 0.016f; // 16 milliseconds

        void Update()
        {
            if (EditorApplication.timeSinceStartup - lastUpdateTime >= updateInterval)
            {
                for (int i = 0; i < m_UpdatableElements.Count; ++i)
                    m_UpdatableElements[i].CheckUpdate();

                if (m_BaseUpdatable != null)
                {
                    foreach (VisualElementUpdatable updatable in m_BaseUpdatable
                                 .Children()
                                 .Where(c => c is VisualElementUpdatable))
                        updatable.CheckUpdate();
                }

                // Update the time of the last update
                lastUpdateTime = (float)EditorApplication.timeSinceStartup;

            }
        }

        static HDWizard()
        {
            LoadReflectionMethods();
            WizardBehaviour();
        }

        void OnEnable()
        {
            GraphicsToolLifetimeAnalytic.WindowOpened<HDWizard>();
            
            CheckPackages(null);
            PackageManager.Events.registeredPackages += CheckPackages;
        }

        private void OnDisable()
        {
            PackageManager.Events.registeredPackages -= CheckPackages;

            GraphicsToolLifetimeAnalytic.WindowClosed<HDWizard>();
        }

        #region SCRIPT_RELOADING

        static int frameToWait;

        static void WizardBehaviourDelayed()
        {
            if (frameToWait > 0)
            {
                --frameToWait;
                return;
            }

            // No need to update this method, unsubscribe from the application update
            EditorApplication.update -= WizardBehaviourDelayed;

            // If the wizard does not need to be shown at start up, do nothing.
            if (!HDUserSettings.wizardIsStartPopup)
                return;

            //Application.isPlaying cannot be called in constructor. Do it here
            if (Application.isPlaying)
                return;

            EditorApplication.quitting += () => HDUserSettings.wizardPopupAlreadyShownOnce = false;

            ShowWizardFirstTime();
        }

        static void ShowWizardFirstTime()
        {
            // Unsubscribe from possible events
            // If the event has not been registered the unsubscribe will do nothing
            RenderPipelineManager.activeRenderPipelineTypeChanged -= ShowWizardFirstTime;

            if (!HDRenderPipeline.isReady)
            {
                // Delay the show of the wizard for the first time that the user is using HDRP
                RenderPipelineManager.activeRenderPipelineTypeChanged += ShowWizardFirstTime;
                return;
            }

            // If we reach this point can be because
            // - That the user started Unity with HDRP in use
            // - That the SRP has changed to HDRP for the first time in the session
            if (!HDUserSettings.wizardPopupAlreadyShownOnce)
                OpenWindow();
        }

        [Callbacks.DidReloadScripts]
        static void CheckPersistencyPopupAlreadyOpened()
        {
            EditorApplication.delayCall += () =>
            {
                if (HDUserSettings.wizardPopupAlreadyShownOnce)
                    EditorApplication.quitting += () => HDUserSettings.wizardPopupAlreadyShownOnce = false;
            };
        }

        [Callbacks.DidReloadScripts]
        static void WizardBehaviour()
        {
            // We should call HDProjectSettings.wizardIsStartPopup to check here.
            // But if the Wizard is opened while a domain reload occurs, we end up calling
            // LoadSerializedFileAndForget at a time Unity associate with Constructor. This is not allowed.
            // As we should wait some frame for everything to be correctly loaded anyway, we do that in WizardBehaviourDelayed.

            //We need to wait at least one frame or the popup will not show up
            frameToWait = 10;
            EditorApplication.update += WizardBehaviourDelayed;
        }

        #endregion

        #region DRAWERS

        Toolbar CreateToolbar()
        {
            var toolbar = new Toolbar()
            {
                name = "MainToolbar"
            };

            ToolbarButton button = new ToolbarButton(() => PackageManager.UI.Window.Open(k_HdrpPackageName))
            {
                text = Style.OpenPackageManager
            };

            toolbar.Add(CreateWizardBehaviour());
            toolbar.Add(button);

            return toolbar;
        }

        private void UpdateWindowTitle()
        {
            titleContent = new GUIContent($"{Style.title} ({Style.checking})");

            m_UsedPackageRetriever.ProcessAsync(k_HdrpPackageName, (installed, packageInfo)
                =>
                {
                    var packageInfoStr =
                        $"{packageInfo.version}{(packageInfo.source == PackageManager.PackageSource.Local ? Style.local : "")}";

                    titleContent = new GUIContent($"{Style.title} ({packageInfoStr})");
                });
        }

        private void CreateGUI()
        {
            UpdateWindowTitle();

            rootVisualElement.Add(CreateToolbar());

            var scrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                horizontalScroller =
                {
                    visible = false
                }
            };
            rootVisualElement.Add(scrollView);
            var container = scrollView.contentContainer;

            container.Add(CreateTitle(Style.defaultSettingsTitle));
            container.Add(CreateFolderData());

            container.Add(CreateTitle(Style.configurationTitle));
            container.Add(CreateInstallConfigPackageArea());

            var hdrpConfig = new InclusiveModeElement(InclusiveMode.HDRP, Style.hdrpConfigLabel, Style.hdrpConfigTooltip, this);
            container.Add(hdrpConfig);
            m_UpdatableElements.Add(hdrpConfig);

            var vrConfig = new InclusiveModeElement(InclusiveMode.VR, Style.hdrpVRConfigLabel, Style.hdrpVRConfigTooltip, this);
            m_UpdatableElements.Add(vrConfig);
            container.Add(vrConfig);

            var dxrConfig = new InclusiveModeElement(InclusiveMode.DXROptional, Style.hdrpDXRConfigLabel, Style.hdrpDXRConfigTooltip, this);
            m_UpdatableElements.Add(dxrConfig);
            container.Add(dxrConfig);

            foreach (var entry in entries)
            {
                var configLine = new ConfigInfoLine(entry);
                hdrpConfig.Add(entry, configLine);
                vrConfig.Add(entry, configLine);
                dxrConfig.Add(entry, configLine);
            }

            container.Add(CreateTitle(Style.migrationTitle));

            if (MaterialUpgrader.ProjectFolderContainsNonBuiltinMaterials(
                    UpgradeStandardShaderMaterials.GetHDUpgraders()))
            {
                var nonBuiltinMaterialHelpBox = new HelpBox(Style.nonBuiltinMaterialWarning, HelpBoxMessageType.Warning);
                nonBuiltinMaterialHelpBox.AddToClassList("NonBuiltinMaterialWarning");
                container.Add(nonBuiltinMaterialHelpBox);
            }

            container.Add(CreateLargeButton(Style.migrateAllButton, UpgradeStandardShaderMaterials.UpgradeMaterialsProject));
            container.Add(CreateLargeButton(Style.migrateSelectedButton, UpgradeStandardShaderMaterials.UpgradeMaterialsSelection));
            container.Add(CreateLargeButton(Style.migrateMaterials, HDRenderPipelineMenuItems.UpgradeMaterials));

            CheckPersistantNeedReboot();
            CheckPersistentFixAll();

            HDEditorUtils.AddStyleSheets(rootVisualElement, HDWizardConfig.FormattingPath); //.h1
            HDEditorUtils.AddStyleSheets(rootVisualElement, HDWizardConfig.StyleSheetPath);

            foreach (var element in m_UpdatableElements)
                element.Init();
        }

        VisualElement CreateFolderData()
        {
            var defaultResourceFolder = new TextField(Style.hdrpProjectSettingsPathLabel)
            {
                tooltip = Style.hdrpProjectSettingsPathTooltip,
                name = "DefaultResourceFolder",
                value = HDProjectSettings.projectSettingsFolderPath
            };
            defaultResourceFolder.Q<UnityEngine.UIElements.Label>().AddToClassList("normal");
            defaultResourceFolder.RegisterValueChangedCallback(evt
                => HDProjectSettings.projectSettingsFolderPath = evt.newValue);

            return defaultResourceFolder;
        }

        Toggle CreateWizardBehaviour()
        {
            var toggle = new Toggle()
            {
                text = Style.showOnStartUp,
                value = HDUserSettings.wizardIsStartPopup,
                name = "WizardCheckbox"
            };
            toggle.RegisterValueChangedCallback(evt
                => HDUserSettings.wizardIsStartPopup = evt.newValue);
            return toggle;
        }

        VisualElement CreateLargeButton(string title, Action action)
        {
            Button button = new Button(action) { text = title };
            button.AddToClassList("LargeButton");
            button.clicked += () =>
            {
                string context = "{" + $"\"id\" : \"{title}\"" + "}";
                GraphicsToolUsageAnalytic.ActionPerformed<HDWizard>("Button Pressed", new string[] { context });
            };
            return button;
        }

        VisualElement CreateInstallConfigPackageArea()
        {
            VisualElement area = new VisualElement()
            {
                name = "InstallConfigPackageArea",
                style =
                {
                    marginBottom = 3
                }
            };
            m_InstallConfigPackageButton = CreateLargeButton(Style.installConfigPackageLabel, () =>
            {
                UpdateDisplayOfConfigPackageArea(ConfigPackageState.BeingFixed);
                InstallLocalConfigurationPackage(() =>
                    UpdateDisplayOfConfigPackageArea(ConfigPackageState.Present));
            });
            m_InstallConfigPackageHelpbox = new HelpBox(Style.installConfigPackageInfoInCheck, HelpBoxMessageType.Info);
            m_InstallConfigPackageHelpbox.AddToClassList("InstallConfigPackageMessage");
            m_InstallConfigPackageHelpboxLabel = m_InstallConfigPackageHelpbox.Q<UnityEngine.UIElements.Label>();
            area.Add(m_InstallConfigPackageButton);
            area.Add(m_InstallConfigPackageHelpbox);

            UpdateDisplayOfConfigPackageArea(ConfigPackageState.BeingChecked);

            RefreshDisplayOfConfigPackageArea();
            return area;
        }

        void UpdateDisplayOfConfigPackageArea(ConfigPackageState state)
        {
            switch (state)
            {
                case ConfigPackageState.Present:
                    m_InstallConfigPackageButton.SetEnabled(false);
                    m_InstallConfigPackageButton.focusable = false;
                    m_InstallConfigPackageHelpbox.style.display = DisplayStyle.Flex;
                    m_InstallConfigPackageHelpboxLabel.text = Style.installConfigPackageInfoFinished;
                    break;

                case ConfigPackageState.Missing:
                    m_InstallConfigPackageButton.SetEnabled(true);
                    m_InstallConfigPackageButton.focusable = true;
                    m_InstallConfigPackageHelpbox.style.display = DisplayStyle.None;
                    break;

                case ConfigPackageState.BeingChecked:
                    m_InstallConfigPackageButton.SetEnabled(false);
                    m_InstallConfigPackageButton.focusable = false;
                    m_InstallConfigPackageHelpbox.style.display = DisplayStyle.Flex;
                    m_InstallConfigPackageHelpboxLabel.text = Style.installConfigPackageInfoInCheck;
                    break;

                case ConfigPackageState.BeingFixed:
                    m_InstallConfigPackageButton.SetEnabled(false);
                    m_InstallConfigPackageButton.focusable = false;
                    m_InstallConfigPackageHelpbox.style.display = DisplayStyle.Flex;
                    m_InstallConfigPackageHelpboxLabel.text = Style.installConfigPackageInfoInProgress;
                    break;
            }
        }

        UnityEngine.UIElements.Label CreateTitle(string title)
        {
            var label = new UnityEngine.UIElements.Label(title);
            label.AddToClassList("h1");
            return label;
        }

        #endregion
    }
}
