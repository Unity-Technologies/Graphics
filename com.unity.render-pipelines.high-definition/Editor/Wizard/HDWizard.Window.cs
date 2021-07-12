using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
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

            public const string installConfigPackageLabel = "Install Configuration Editable Package";
            public const string installConfigPackageInfoInCheck = "Checking if the local config package is installed in your project's LocalPackage folder.";
            public const string installConfigPackageInfoInProgress = "The local config package is being installed in your project's LocalPackage folder.";
            public const string installConfigPackageInfoFinished = "The local config package is already installed in your project's LocalPackage folder.";

            public const string migrateAllButton = "Upgrade Project Materials to High Definition Materials";
            public const string migrateSelectedButton = "Upgrade Selected Materials to High Definition Materials";
            public const string migrateMaterials = "Upgrade HDRP Materials to Latest Version";

            public const string HDRPVersion = "Current HDRP version: ";
            public const string HDRPVersionUpdateButton = "Check update";


            //configuration debugger
            public const string resolve = "Fix";
            public const string resolveAll = "Fix All";
            public const string resolveAllQuality = "Fix All Qualities";
            public const string resolveAllBuildTarget = "Fix All Platforms";
            public const string fixAllOnNonHDRP = "The active Quality Level is not using a High Definition Render Pipeline asset. If you attempt a Fix All, the Quality Level will be changed to use it.";

            public struct ConfigStyle
            {
                public readonly string label;
                public readonly string error;
                public readonly string button;
                public readonly MessageType messageType;
                public ConfigStyle(string label, string error, string button = resolve, MessageType messageType = MessageType.Error)
                {
                    this.label = label;
                    this.error = error;
                    this.button = button;
                    this.messageType = messageType;
                }
            }

            public static readonly ConfigStyle hdrpColorSpace = new ConfigStyle(
                label: "Color space",
                error: "Only linear color space supported!");
            public static readonly ConfigStyle hdrpLightmapEncoding = new ConfigStyle(
                label: "Lightmap encoding",
                error: "Only high quality lightmap supported!",
                button: resolveAllBuildTarget);
            public static readonly ConfigStyle hdrpShadow = new ConfigStyle(
                label: "Shadows",
                error: "Shadow must be set to activated! (both hard and soft)");
            public static readonly ConfigStyle hdrpShadowmask = new ConfigStyle(
                label: "Shadowmask mode",
                error: "Only distance shadowmask supported at the project level! (You can still change this per light.)",
                button: resolveAllQuality);
            public static readonly ConfigStyle hdrpAssetGraphicsAssigned = new ConfigStyle(
                label: "Assigned - Graphics",
                error: "There is no HDRP asset assigned to the Graphic Settings!");
            public static readonly ConfigStyle hdrpAssetQualityAssigned = new ConfigStyle(
                label: "Assigned - Quality",
                error: "The RenderPipelineAsset assigned in the current Quality must be null or a HDRenderPipelineAsset. If it is null, the asset for the current Quality will be the one in Graphics Settings. (The Fix or Fix All button will nullify it)");
            public static readonly ConfigStyle hdrpAssetRuntimeResources = new ConfigStyle(
                label: "Runtime resources",
                error: "There is an issue with the runtime resources!");
            public static readonly ConfigStyle hdrpAssetEditorResources = new ConfigStyle(
                label: "Editor resources",
                error: "There is an issue with the editor resources!");
            public static readonly ConfigStyle hdrpBatcher = new ConfigStyle(
                label: "SRP Batcher",
                error: "SRP Batcher must be enabled!");
            public static readonly ConfigStyle hdrpAssetDiffusionProfile = new ConfigStyle(
                label: "Diffusion profile",
                error: "There is no diffusion profile assigned in the HDRP asset!");
            public static readonly ConfigStyle hdrpVolumeProfile = new ConfigStyle(
                label: "Default volume profile",
                error: "Default volume profile must be assigned in the HDRP asset! Also, for it to be editable, it should be outside of package.");
            public static readonly ConfigStyle hdrpLookDevVolumeProfile = new ConfigStyle(
                label: "Default Look Dev volume profile",
                error: "Default Look Dev volume profile must be assigned in the HDRP asset! Also, for it to be editable, it should be outside of package.");

            public static readonly ConfigStyle vrLegacyVRSystem = new ConfigStyle(
                label: "Legacy VR System",
                error: "Legacy VR System need to be disabled in Player Settings!");
            public static readonly ConfigStyle vrXRManagementPackage = new ConfigStyle(
                label: "XR Management Package",
                error: "XR Management Package is required to run in VR!");
            public static readonly ConfigStyle vrXRManagementPackageInstalled = new ConfigStyle(
                label: "Package Installed",
                error: "Last version of XR Management Package must be added in your project!");
            public static readonly ConfigStyle vrOculusPlugin = new ConfigStyle(
                label: "Oculus Plugin",
                error: "Oculus Plugin must installed manually.\nGo in Edit > Project Settings > XR Plugin Manager and add Oculus XR Plugin.\n(This can't be verified by the Wizard)",
                messageType: MessageType.Info);
            public static readonly ConfigStyle vrSinglePassInstancing = new ConfigStyle(
                label: "Single-Pass Instancing",
                error: "Single-Pass Instancing must be enabled in Oculus Plugin.\nGo in Edit > Project Settings > XR Plugin Manager > Oculus and change Stereo Rendering Mode to Single Pass Instanced.\n(This can't be verified by the Wizard)",
                messageType: MessageType.Info);
            public static readonly ConfigStyle vrLegacyHelpersPackage = new ConfigStyle(
                label: "XR Legacy Helpers Package",
                error: "XR Legacy Helpers Package will help you to handle inputs.");

            public static readonly ConfigStyle dxrAutoGraphicsAPI = new ConfigStyle(
                label: "Auto graphics API",
                error: "Auto Graphics API is not supported!");
            public static readonly ConfigStyle dxrD3D12 = new ConfigStyle(
                label: "Direct3D 12",
                error: "Direct3D 12 needs to be the active device! (Editor restart is required). If an API different than D3D12 is forced via command line argument, clicking Fix won't change it, so please consider removing it if wanting to run DXR.");
            public static readonly ConfigStyle dxrScreenSpaceShadow = new ConfigStyle(
                label: "Screen Space Shadows (Asset)",
                error: "Screen Space Shadows are disabled in the current HDRP Asset which means you cannot enable ray-traced shadows for lights in your scene. To enable this feature, open your HDRP Asset, go to Lighting > Shadows, and enable Screen Space Shadows.", messageType: MessageType.Warning);
            public static readonly ConfigStyle dxrScreenSpaceShadowFS = new ConfigStyle(
                label: "Screen Space Shadows (HDRP Default Settings)",
                error: $"Screen Space Shadows are disabled in the default Camera Frame Settings. This means Cameras that use these Frame Settings do not render ray-traced shadows. To enable this feature, go to Project Settings > HDRP Default Settings > Frame Settings > Default Frame Settings For Camera > Lighting and enable Screen Space Shadows. This configuration depends on {dxrScreenSpaceShadow.label}. This means, before you fix this, you must fix {dxrScreenSpaceShadow.label} first.", messageType: MessageType.Info);
            public static readonly ConfigStyle dxrReflections = new ConfigStyle(
                label: "Screan Space Reflection (Asset)",
                error: "Screen Space Reflection is disabled in the current HDRP Asset which means you cannot enable ray-traced reflections in Volume components. To enable this feature, open your HDRP Asset, go to Lighting > Reflections, and enable Screen Space Reflections.", messageType: MessageType.Warning);
            public static readonly ConfigStyle dxrReflectionsFS = new ConfigStyle(
                label: "Screan Space Reflection (HDRP Default Settings)",
                error: $"Screen Space Reflection is disabled in the default Camera Frame Settings. This means Cameras that use these Frame Settings do not render ray-traced reflections. To enable this feature, go to Project Settings > HDRP Default Settings > Frame Settings > Default Frame Settings For Camera > Lighting and enable Screen Space Reflections. This configuration depends on {dxrReflections.label}. This means, before you fix this, you must fix {dxrReflections.label} first.", messageType: MessageType.Info);
            public static readonly ConfigStyle dxrTransparentReflections = new ConfigStyle(
                label: "Screen Space Reflection - Transparent (Asset)",
                error: "Screen Space Reflection - Transparent is disabled in the current HDRP Asset which means you cannot enable ray-traced reflections on transparent GameObjects from Volume components. To enable this feature, open your HDRP Asset, go to Lighting > Reflections, and enable Transparents receive SSR.", messageType: MessageType.Warning);
            public static readonly ConfigStyle dxrTransparentReflectionsFS = new ConfigStyle(
                label: "Screen Space Reflection - Transparent (HDRP Default Settings)",
                error: $"Screen Space Reflection - Transparent is disabled in the default Camera Frame Settings. This means Cameras that use these Frame Settings do not render ray-traced reflections on transparent GameObjects. To enable this feature, go to Project Settings > HDRP Default Settings > Frame Settings > Default Frame Settings For Camera > Lighting and enable Transparents. This configuration depends on {dxrTransparentReflections.label}. This means, before you fix this, you must fix {dxrTransparentReflections.label} first.", messageType: MessageType.Info);
            public static readonly ConfigStyle dxrGI = new ConfigStyle(
                label: "Screen Space Global Illumination (Asset)",
                error: "Screen Space Global Illumination is disabled in the current HDRP asset which means you cannot enable ray-traced global illumination in Volume components. To enable this feature, open your HDRP Asset, go to Lighting and enable Screen Space Global Illumination.", messageType: MessageType.Warning);
            public static readonly ConfigStyle dxrGIFS = new ConfigStyle(
                label: "Screen Space Global Illumination (HDRP Default Settings)",
                error: $"Screen Space Global Illumination is disabled in the default Camera Frame Settings. This means Cameras that use these Frame Settings do not render ray-traced global illumination. To enable this feature, go to Project Settings > HDRP Default Settings > Frame Settings > Default Frame Settings For Camera > Lighting and enable Screen Space Global Illumination. This configuration depends on {dxrGI.label}. This means, before you fix this, you must fix {dxrGI.label} first.", messageType: MessageType.Info);
            public static readonly ConfigStyle dxr64bits = new ConfigStyle(
                label: "Architecture 64 bits",
                error: "To build your Project to a Unity Player, ray tracing requires that the build uses 64 bit architecture.");
            public static readonly ConfigStyle dxrStaticBatching = new ConfigStyle(
                label: "Static Batching",
                error: "Static Batching is not supported!");
            public static readonly ConfigStyle dxrActivated = new ConfigStyle(
                label: "DXR activated",
                error: "DXR is not activated!");
            public static readonly ConfigStyle dxrResources = new ConfigStyle(
                label: "DXR resources",
                error: "There is an issue with the DXR resources! Alternatively, Direct3D is not set as API (can be fixed with option above) or your hardware and/or OS cannot be used for DXR! (unfixable)");

            public const string hdrpAssetDisplayDialogTitle = "Create or Load HDRenderPipelineAsset";
            public const string hdrpAssetDisplayDialogContent = "Do you want to create a fresh HDRenderPipelineAsset in the default resource folder and automatically assign it?";
            public const string displayDialogCreate = "Create One";
            public const string displayDialogLoad = "Load One";
            public const string displayDialogCancel = "Cancel";
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
        VisualElement m_BaseUpdatable;
        VisualElement m_InstallConfigPackageHelpbox = null;
        VisualElement m_InstallConfigPackageButton = null;
        Label m_InstallConfigPackageHelpboxLabel;

        [MenuItem("Window/Render Pipeline/HD Render Pipeline Wizard", priority = 10000)]
        static void OpenWindow()
        {
            var window = GetWindow<HDWizard>("HD Render Pipeline Wizard");
            window.minSize = new Vector2(500, 450);
            HDProjectSettings.wizardPopupAlreadyShownOnce = true;
        }

        void OnGUI()
        {
            if (m_BaseUpdatable == null)
                return;

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
            else
            {
                EditorApplication.update -= WizardBehaviourDelayed;

                if (HDProjectSettings.wizardIsStartPopup && !HDProjectSettings.wizardPopupAlreadyShownOnce)
                {
                    //Application.isPlaying cannot be called in constructor. Do it here
                    if (Application.isPlaying)
                        return;

                    OpenWindow();
                }

                EditorApplication.quitting += () => HDProjectSettings.wizardPopupAlreadyShownOnce = false;
            }
        }

        [Callbacks.DidReloadScripts]
        static void CheckPersistencyPopupAlreadyOpened()
        {
            EditorApplication.delayCall += () =>
            {
                if (HDProjectSettings.wizardPopupAlreadyShownOnce)
                    EditorApplication.quitting += () => HDProjectSettings.wizardPopupAlreadyShownOnce = false;
            };
        }

        [Callbacks.DidReloadScripts]
        static void WizardBehaviour()
        {
            //We need to wait at least one frame or the popup will not show up
            frameToWait = 10;
            EditorApplication.update += WizardBehaviourDelayed;
        }

        #endregion

        #region DRAWERS

        private void CreateGUI()
        {
            titleContent = Style.title;

            HDEditorUtils.AddStyleSheets(rootVisualElement, HDEditorUtils.FormatingPath); //.h1
            HDEditorUtils.AddStyleSheets(rootVisualElement, HDEditorUtils.WizardSheetPath);

            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            rootVisualElement.Add(scrollView);
            var container = scrollView.contentContainer;

            container.Add(CreateHdrpVersionChecker());

            container.Add(CreateInstallConfigPackageArea());

            container.Add(CreateTitle(Style.defaultSettingsTitle));
            container.Add(CreateFolderData());

            container.Add(CreateTitle(Style.configurationTitle));
            container.Add(CreateTabbedBox(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new[]
                {
                    (Style.hdrpConfigLabel, Style.hdrpConfigTooltip),
                    (Style.hdrpVRConfigLabel, Style.hdrpVRConfigTooltip),
                    (Style.hdrpDXRConfigLabel, Style.hdrpDXRConfigTooltip),
                }
                : new[]
                {
                    (Style.hdrpConfigLabel, Style.hdrpConfigTooltip),
                    //VR only supported on window
                    //DXR only supported on window
                },
                out m_BaseUpdatable));


            var fixAllWarning = new HiddableUpdatableContainer(() => !IsHdrpAssetQualityUsedCorrect());
            fixAllWarning.Add(new HelpBox(HelpBox.Kind.Error, Style.fixAllOnNonHDRP) { name = "FixAllWarning" });
            fixAllWarning.Init();
            m_BaseUpdatable.Add(fixAllWarning);

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

            ScopeBox globalScope = new ScopeBox("Global");
            ScopeBox currentQualityScope = new ScopeBox("Current Quality");

            m_BaseUpdatable.Add(globalScope);
            m_BaseUpdatable.Add(currentQualityScope);

            AddHDRPConfigInfo(globalScope, QualityScope.Global);

            var vrScopeGlobal = new HiddableUpdatableContainer(()
                => m_Configuration == Configuration.HDRP_VR);
            AddVRConfigInfo(vrScopeGlobal, QualityScope.Global);
            vrScopeGlobal.Init();
            globalScope.Add(vrScopeGlobal);

            var dxrScopeGlobal = new HiddableUpdatableContainer(()
                => m_Configuration == Configuration.HDRP_DXR);
            AddDXRConfigInfo(dxrScopeGlobal, QualityScope.Global);
            dxrScopeGlobal.Init();
            globalScope.Add(dxrScopeGlobal);

            AddHDRPConfigInfo(currentQualityScope, QualityScope.CurrentQuality);

            var vrScopeCurrentQuality = new HiddableUpdatableContainer(()
                => m_Configuration == Configuration.HDRP_VR);
            AddVRConfigInfo(vrScopeCurrentQuality, QualityScope.CurrentQuality);
            vrScopeCurrentQuality.Init();
            currentQualityScope.Add(vrScopeCurrentQuality);

            var dxrScopeCurrentQuality = new HiddableUpdatableContainer(()
                => m_Configuration == Configuration.HDRP_DXR);
            AddDXRConfigInfo(dxrScopeCurrentQuality, QualityScope.CurrentQuality);
            dxrScopeCurrentQuality.Init();
            currentQualityScope.Add(dxrScopeCurrentQuality);

            container.Add(CreateTitle(Style.migrationTitle));
            container.Add(CreateLargeButton(Style.migrateAllButton, UpgradeStandardShaderMaterials.UpgradeMaterialsProject));
            container.Add(CreateLargeButton(Style.migrateSelectedButton, UpgradeStandardShaderMaterials.UpgradeMaterialsSelection));
            container.Add(CreateLargeButton(Style.migrateMaterials, HDRenderPipelineMenuItems.UpgradeMaterials));

            container.Add(CreateWizardBehaviour());

            CheckPersistantNeedReboot();
            CheckPersistentFixAll();
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

            return defaultResourceFolder;
        }

        VisualElement CreateTabbedBox((string label, string tooltip)[] tabs, out VisualElement innerBox)
        {
            var toolbar = new ToolbarRadio();
            toolbar.AddRadios(tabs);
            //make sure when we open the same project on different platforms the saved active tab is not out of range
            int tabIndex = toolbar.radioLength > HDProjectSettings.wizardActiveTab ? HDProjectSettings.wizardActiveTab : 0;
            toolbar.SetValueWithoutNotify(tabIndex);
            m_Configuration = (Configuration)tabIndex;
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

        VisualElement CreateLargeButton(string title, Action action)
        {
            Button button = new Button(action) { text = title };
            button.AddToClassList("LargeButton");
            return button;
        }

        VisualElement CreateInstallConfigPackageArea()
        {
            VisualElement area = new VisualElement()
            {
                name = "InstallConfigPackageArea"
            };
            m_InstallConfigPackageButton = CreateLargeButton(Style.installConfigPackageLabel, () =>
            {
                UpdateDisplayOfConfigPackageArea(ConfigPackageState.BeingFixed);
                InstallLocalConfigurationPackage(() =>
                    UpdateDisplayOfConfigPackageArea(ConfigPackageState.Present));
            });
            m_InstallConfigPackageHelpbox = new HelpBox(HelpBox.Kind.Info, Style.installConfigPackageInfoInCheck);
            m_InstallConfigPackageHelpboxLabel = m_InstallConfigPackageHelpbox.Q<Label>();
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

        void GroupEntriesForDisplay(VisualElement container, InclusiveMode filter, QualityScope scope)
        {
            foreach (var entry in entries.Where(e => e.scope == scope && filter.Contains(e.inclusiveScope)))
            {
                string error = entry.configStyle.error;

                // If it is necessary, append tht name of the current asset.
                var hdrpAsset = HDRenderPipeline.currentAsset;
                if (entry.displayAssetName && hdrpAsset != null)
                {
                    error += " (" + hdrpAsset.name + ").";
                }

                container.Add(new ConfigInfoLine(
                    entry.configStyle.label,
                    error,
                    entry.configStyle.messageType,
                    entry.configStyle.button,
                    () => entry.check(),
                    entry.fix == null ? (Action)null : () => entry.fix(fromAsync: false),
                    entry.indent,
                    entry.configStyle.messageType == MessageType.Error || entry.forceDisplayCheck,
                    entry.skipErrorIcon));
            }
        }

        void AddHDRPConfigInfo(VisualElement container, QualityScope quality)
            => GroupEntriesForDisplay(container, InclusiveMode.HDRP, quality);
        void AddVRConfigInfo(VisualElement container, QualityScope quality)
            => GroupEntriesForDisplay(container, InclusiveMode.VR, quality);
        void AddDXRConfigInfo(VisualElement container, QualityScope quality)
            => GroupEntriesForDisplay(container, InclusiveMode.DXROptional, quality);

        Label CreateTitle(string title)
        {
            var label = new Label(title);
            label.AddToClassList("h1");
            return label;
        }

        VisualElement CreateHdrpVersionChecker()
        {
            VisualElement container = new VisualElement() { name = "HDRPVersionContainer" };

            TextElement label = new TextElement() { text = Style.HDRPVersion + "checking..." };
            label.AddToClassList("normal");
            container.Add(label);

            Button button = new Button(() =>
                UnityEditor.PackageManager.UI.Window.Open("com.unity.render-pipelines.high-definition"))
            { text = Style.HDRPVersionUpdateButton };
            button.AddToClassList("RightAnchoredButton");
            container.Add(button);

            m_UsedPackageRetriever.ProcessAsync(k_HdrpPackageName, (installed, packageInfo)
                => label.text = Style.HDRPVersion + packageInfo.version + (packageInfo.source == PackageManager.PackageSource.Local ? " (local)" : ""));

            return container;
        }

        #endregion
    }
}
