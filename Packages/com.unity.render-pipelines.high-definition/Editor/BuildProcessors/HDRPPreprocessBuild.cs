using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

using ShaderPrefilteringData = UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset.ShaderPrefilteringData;

namespace UnityEditor.Rendering.HighDefinition
{
    // Shader features that can be used to configure shader prefiltering.
    // Prefiltering can apply complex rules that cannot be properly defined during scriptable stripping.
    [Flags]
    enum ShaderFeatures : long
    {
        None = 0,
        UseLegacyLightmaps = (1L << 0),
    }

    class HDRPPreprocessBuild : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => int.MinValue + 100;

        private static HDRPBuildData m_BuildData = null;
        private static List<ShaderFeatures> s_SupportedFeaturesList = new();

        public void OnPreprocessBuild(BuildReport report)
        {
            m_BuildData?.Dispose();
            bool useDiagnosticChecks = PlayerSettings.GetManagedCodeVariant(GetNamedBuildTarget(report)) <= ManagedCodeVariant.Checked;
            m_BuildData = new HDRPBuildData(report.summary.platform, useDiagnosticChecks);

            if (m_BuildData.buildingPlayerForHDRenderPipeline)
            {
                // Now that we know that we are on HDRP we need to make sure everything is correct, otherwise we break the build.
                if (!HDRPBuildDataValidator.IsProjectValidForBuilding(report, out var message))
                    throw new BuildFailedException(message);

                ConfigureMinimumMaxLoDValueForAllQualitySettings();

                if (HDRenderPipelineGlobalSettings.instance.TryInitializeDefaultVolumeProfile(out var defaultVolumeProfileSettings))
                {
                    Debug.Log("Default Volume Profile has been created or Diffusion Profiles have been updated to ensure all components are present. This is required to avoid missing overrides at runtime which can lead to unexpected rendering issues. Please save these changes to avoid this message in the future.");
                }

                if (defaultVolumeProfileSettings == null)
                {
                    throw new BuildFailedException("Failed to initialize the Default Volume Profile. A Default Volume Profile is required for HDRP to function properly.");
                }

                if (VolumeProfileUtils.TryEnsureAllOverridesForDefaultProfile(defaultVolumeProfileSettings.volumeProfile))
                {
                    Debug.Log("Default Volume Profile has been modified to ensure all components are present. This is required to avoid missing overrides at runtime which can lead to unexpected rendering issues. Please save these changes to avoid this message in the future.");
                }

                LogIncludedAssets(m_BuildData.renderPipelineAssets);

                if (!IsConfigurationValid(report.summary.platform))
                {
                    if(!ProceedWithBuild())
                        throw new BuildFailedException("Build canceled by user due to HDRP configuration issues.");
                }


                GatherShaderFeatures(report.summary.platform);
            }
        }

        static NamedBuildTarget GetNamedBuildTarget(BuildReport report)
        {
            var platformGroup = report.summary.platformGroup;
            if (platformGroup == BuildTargetGroup.Standalone &&
                report.summary.GetSubtarget<StandaloneBuildSubtarget>() == StandaloneBuildSubtarget.Server)
            {
                return NamedBuildTarget.Server;
            }
            return NamedBuildTarget.FromBuildTargetGroup(platformGroup);
        }

        private static bool IsConfigurationValid(BuildTarget buildTarget)
        {
            // Validate the configuration of the HDRP assets for the current build target. We want to make sure that users are aware of potential performance issues or unsupported features before building.
            // We still want to build the player even if the configuration is not optimal, but we log warnings to inform users about potential issues.
            // Note that we validate the configuration of all HDRP assets included in the build, not just the one assigned in Graphics Settings.
            // This is because users can have multiple HDRP assets in their project and switch between them at runtime, so we want to make sure that all of them are correctly configured for the target platform.

            // We must log all the warnings, and avoid doing validConfiguration &= ValidationXXX, that will avoid logging all the warnings, and only log the first one that fails.
            // This way users will have a complete overview of all the potential issues with their configuration, and can fix them all at once, instead of having to go through multiple build iterations to fix each issue one by one.
            // So be carefull when you edit this code, and make sure to log all the warnings, even if one of the validation fails.
            bool validConfiguration = true;

            {
                bool config = ValidateRayTracingConfiguration(buildTarget, m_BuildData.renderPipelineAssets);
                validConfiguration &= config;
            }

            {
                bool config = ValidateSubsurfaceScatteringConfiguration(m_BuildData.renderPipelineAssets);
                validConfiguration &= config;
            }

            {
                bool config = ValidateFilmGrainConfiguration(m_BuildData.renderPipelineAssets);
                validConfiguration &= config;
            }

            {
                bool config = ValidateVolumetricFogConfiguration(m_BuildData.renderPipelineAssets);
                validConfiguration &= config;
            }

            {
                bool config = ValidateVolumetricCloudsConfiguration(m_BuildData.renderPipelineAssets);
                validConfiguration &= config;
            }

            {
                bool config = ValidateHighQualityLineRenderingConfiguration(m_BuildData.renderPipelineAssets);
                validConfiguration &= config;
            }
            
            {
                bool config = ValidateGraphicsCompositorConfiguration(m_BuildData.renderPipelineAssets);
                validConfiguration &= config;
            }

            return validConfiguration;
        }

        internal static string k_DialogKey = $"{nameof(UnityEditor)}.{nameof(Rendering)}.{nameof(HighDefinition)}.{nameof(HDRPPreprocessBuild)}.{nameof(ProceedWithBuild)}";

        private bool ProceedWithBuild()
        {
            if(HDEditorUtils.IsInTestSuiteOrBatchMode())
                return true;

            var title = "Build Configuration Issues Detected";
            var body = new StringBuilder();

            body.AppendLine("HDRP identified settings that may impact performance or enable unsupported features for the current build target.");
            body.AppendLine("Review the Console for details (look for messages tagged 'HDRP Build Validation').");
            body.AppendLine();
            body.Append("Do you want to continue building?");

            return EditorUtility.DisplayDialog(
                title,
                body.ToString(),
                "Proceed",
                "Cancel",
                DialogOptOutDecisionType.ForThisMachine,
                k_DialogKey
            );
        }

        internal static void LogIncludedAssets(List<HDRenderPipelineAsset> assetsList)
        {
            using (GenericPool<StringBuilder>.Get(out var assetsIncluded))
            {
                assetsIncluded.Clear();

                assetsIncluded.Append($"{assetsList.Count} HDRP assets included in build");

                foreach (var hdrpAsset in assetsList)
                {
                    assetsIncluded.AppendLine($"- {hdrpAsset.name} - {AssetDatabase.GetAssetPath(hdrpAsset)}");
                }

                Debug.Log(assetsIncluded);
            }
        }

        private static bool ValidateRayTracingConfiguration(BuildTarget currentBuildTarget, List<HDRenderPipelineAsset> assetsList)
        {
            // Check if any asset has ray tracing enabled
            bool anyAssetHasRayTracingEnabled = false;
            foreach (var hdrpAsset in assetsList)
            {
                if (hdrpAsset != null && hdrpAsset.currentPlatformRenderPipelineSettings.supportRayTracing)
                {
                    anyAssetHasRayTracingEnabled = true;
                    break;
                }
            }

            if (!anyAssetHasRayTracingEnabled)
                return true; // No ray tracing enabled, skip validation

            if (HDRenderPipeline.CheckPlatformRaytracingCompatability(currentBuildTarget, out var warning))
            {
                Debug.LogWarning($"HDRP Build Validation - Ray Tracing:{warning}");
                return false;
            }

            return true;
        }

        internal static bool ValidateSubsurfaceScatteringConfiguration(List<HDRenderPipelineAsset> assetsList)
        {
            if (!EditorGraphicsSettings.ShouldValidateGraphicsForActiveBuildTarget())
                return true;

            var validationSettings = HDProjectSettings.validationSettings;

            bool anyAssetHasSSSEnabled = false;
            HDRenderPipelineAsset foundAsset = null;
            foreach (var hdrpAsset in assetsList)
            {
                if (hdrpAsset != null)
                {
                    if (hdrpAsset.currentPlatformRenderPipelineSettings.supportSubsurfaceScattering && !validationSettings.k_SubsurfaceScattering_Recommended)
                    {
                        anyAssetHasSSSEnabled = true;
                        foundAsset = hdrpAsset;

                        break;
                    }
                }
            }

            if (!anyAssetHasSSSEnabled)
                return true; // No SSS enabled, skip validation

            Debug.LogWarning($"HDRP Build Validation [{(foundAsset != null ? foundAsset.name : "")}] - {HDRenderPipelineUI.Styles.supportedSSSContent.text}: Enabled for the active platform. {HDRenderPipelineUI.Styles.featureNotRecommendedWarning}\nAsset: {AssetDatabase.GetAssetPath(foundAsset)}", foundAsset);
            return false;
        }

        internal static bool ValidateFilmGrainConfiguration(List<HDRenderPipelineAsset> assetsList)
        {
            static bool CheckVolumeProfileValid(VolumeProfile volumeProfile, HDRenderPipelineAsset hdAsset = null)
            {
                if (volumeProfile.TryGet<FilmGrain>(out var filmGrain) && filmGrain.IsActive())
                {
                    var validationSettings = HDProjectSettings.validationSettings;
                    var defaultFilmGrain = HDEditorUtils.GetVolumeComponentDefaultState<FilmGrain>();

                    float effectiveIntensity = HDEditorUtils.GetEffectiveParameterValue(
                        filmGrain.intensity, defaultFilmGrain?.intensity, 0.0f);

                    if (effectiveIntensity > 0.0f && !validationSettings.k_FilmGrain_Recommended)
                    {
                        Debug.LogWarning($"HDRP Build Validation [{volumeProfile.name}] - Film Grain: Enabled for the active platform. {HDRenderPipelineUI.Styles.featureNotRecommendedWarning}\nAsset: {AssetDatabase.GetAssetPath(volumeProfile)}", volumeProfile);
                        return false;
                    }
                }

                return true;
            }

            if (!EditorGraphicsSettings.ShouldValidateGraphicsForActiveBuildTarget())
                return true;

            // Check default volume profile from HDRP Global Settings
            bool isValidConfiguration = true;
            var defaultVolumeProfileSettings = GraphicsSettings.GetRenderPipelineSettings<HDRPDefaultVolumeProfileSettings>();
            if (defaultVolumeProfileSettings?.volumeProfile != null)
            {
                isValidConfiguration &= CheckVolumeProfileValid(defaultVolumeProfileSettings.volumeProfile);
            }

            foreach (var hdrpAsset in assetsList)
            {
                if (hdrpAsset != null && hdrpAsset.volumeProfile != null)
                {
                    isValidConfiguration &= CheckVolumeProfileValid(hdrpAsset.volumeProfile, hdrpAsset);
                }
            }

            return isValidConfiguration;
        }

        internal static bool ValidateVolumetricFogConfiguration(List<HDRenderPipelineAsset> assetsList)
        {
            static bool CheckVolumeProfileValid(VolumeProfile volumeProfile, HDRenderPipelineAsset hdAsset = null)
            {
                if (volumeProfile.TryGet<Fog>(out var fog) && fog.active)
                {
                    var validationSettings = HDProjectSettings.validationSettings;
                    Fog defaultFog = HDEditorUtils.GetVolumeComponentDefaultState<Fog>();

                    // Check if fog is enabled (required for volumetric fog)
                    bool effectiveEnabled = HDEditorUtils.GetEffectiveParameterValue(
                        fog.enabled, defaultFog?.enabled, false);

                    // Check if volumetric fog specifically is enabled
                    bool effectiveVolumetricFogEnabled = HDEditorUtils.GetEffectiveParameterValue(
                        fog.enableVolumetricFog, defaultFog?.enableVolumetricFog, false);

                    // Only validate volumetric fog settings if both are enabled
                    if (!effectiveEnabled || !effectiveVolumetricFogEnabled)
                    {
                        return true;
                    }

                    int effectiveQuality = HDEditorUtils.GetEffectiveParameterValue(
                        fog.quality, defaultFog?.quality, 0);

                    float effectiveDensityCutoff = HDEditorUtils.GetEffectiveParameterValue(
                        fog.volumetricLightingDensityCutoff, defaultFog?.volumetricLightingDensityCutoff, 0.0f);

                    float effectiveFogBudget = fog.volumetricFogBudget;
                    const int k_CustomQuality = ScalableSettingLevelParameter.LevelCount;
                    if (effectiveQuality == k_CustomQuality) // Custom quality tier
                    {
                        bool useDefaultFogBudget = !fog.volumetricFogBudgetOverrideState;
                        effectiveFogBudget = useDefaultFogBudget
                            ? (defaultFog?.volumetricFogBudget ?? 0.0f)
                            : fog.volumetricFogBudget;
                    }
                    else if (hdAsset != null)
                    {
                        effectiveQuality = Math.Clamp(effectiveQuality, 0, hdAsset.currentPlatformRenderPipelineSettings.lightingQualitySettings.Fog_Budget.Length - 1);
                        effectiveFogBudget = hdAsset.currentPlatformRenderPipelineSettings.lightingQualitySettings.Fog_Budget[effectiveQuality];
                    }

                    bool warningsFound = false;
                    if (effectiveFogBudget >= validationSettings.k_Fog_MaximumFogBudget && fog.fogControlMode == FogControl.Balance)
                    {
                        string tierName = $"{(effectiveQuality == k_CustomQuality ? "Custom" : ((ScalableSettingLevelParameter.Level)effectiveQuality).ToString())} (Budget: {effectiveFogBudget})";
                        string warningMessage = string.Format(HDRenderPipelineUI.Styles.maxFogBudgetWarning, validationSettings.k_Fog_MaximumFogBudget);
                        Debug.LogWarning($"HDRP Build Validation [{volumeProfile.name}] - {HDRenderPipelineUI.Styles.FogSettingsSubTitle.text}: {HDEditorUtils.CreateParameterWarningMessage("Tier", tierName, null, warningMessage)}\nAsset: {AssetDatabase.GetAssetPath(volumeProfile)}", volumeProfile);
                        warningsFound = true;
                    }

                    if (effectiveDensityCutoff <= 0.0f && effectiveFogBudget >= validationSettings.k_Fog_MinimumFogBudgetForCutoff && fog.fogControlMode == FogControl.Balance)
                    {
                        string warningMessage = string.Format(HDRenderPipelineUI.Styles.minFogBudgetForDensityCutoffWarning, validationSettings.k_Fog_MinimumFogBudgetForCutoff);
                        Debug.LogWarning($"HDRP Build Validation [{volumeProfile.name}] - {HDRenderPipelineUI.Styles.FogSettingsSubTitle.text}: {HDEditorUtils.CreateParameterWarningMessage("Density Cutoff", "0", null, warningMessage)}\nAsset: {AssetDatabase.GetAssetPath(volumeProfile)}", volumeProfile);
                        warningsFound = true;
                    }

                    return !warningsFound;
                }

                return true;
            }

            if (!EditorGraphicsSettings.ShouldValidateGraphicsForActiveBuildTarget())
                return true;

            // Check default volume profile from HDRP Global Settings
            bool isValidConfiguration = true;
            var defaultVolumeProfileSettings = GraphicsSettings.GetRenderPipelineSettings<HDRPDefaultVolumeProfileSettings>();
            if (defaultVolumeProfileSettings?.volumeProfile != null)
            {
                isValidConfiguration &= CheckVolumeProfileValid(defaultVolumeProfileSettings.volumeProfile);
            }

            // Check volume profiles in each HDRP asset
            foreach (var hdrpAsset in assetsList)
            {
                if (hdrpAsset != null && hdrpAsset.volumeProfile != null && hdrpAsset.currentPlatformRenderPipelineSettings.supportVolumetrics)
                {
                    isValidConfiguration &= CheckVolumeProfileValid(hdrpAsset.volumeProfile, hdrpAsset);
                }
            }

            return isValidConfiguration;
        }

        internal static bool ValidateVolumetricCloudsConfiguration(List<HDRenderPipelineAsset> assetsList)
        {
            static bool CheckVolumeProfileValid(VolumeProfile volumeProfile, HDRenderPipelineAsset hdAsset = null)
            {
                if (volumeProfile.TryGet<VolumetricClouds>(out var clouds) && clouds.active)
                {
                    var validationSettings = HDProjectSettings.validationSettings;
                    VolumetricClouds defaultClouds = HDEditorUtils.GetVolumeComponentDefaultState<VolumetricClouds>();

                    bool effectiveEnabled = HDEditorUtils.GetEffectiveParameterValue(
                        clouds.enable, defaultClouds?.enable, false);

                    if (effectiveEnabled && !validationSettings.k_VolumetricClouds_Recommended)
                    {
                        Debug.LogWarning($"HDRP Build Validation [{volumeProfile.name}] - {HDRenderPipelineUI.Styles.volumetricCloudsSubTitle.text}: Enabled for the active platform. {HDRenderPipelineUI.Styles.featureNotRecommendedWarning}\nAsset: {AssetDatabase.GetAssetPath(volumeProfile)}", volumeProfile);
                        return false;
                    }
                }

                return true;
            }

            if (!EditorGraphicsSettings.ShouldValidateGraphicsForActiveBuildTarget())
                return true;

            // Check default volume profile from HDRP Global Settings
            bool isValidConfiguration = true;
            var defaultVolumeProfileSettings = GraphicsSettings.GetRenderPipelineSettings<HDRPDefaultVolumeProfileSettings>();
            if (defaultVolumeProfileSettings?.volumeProfile != null)
            {
                isValidConfiguration &= CheckVolumeProfileValid(defaultVolumeProfileSettings.volumeProfile);
            }

            // Check volume profiles in each HDRP asset
            foreach (var hdrpAsset in assetsList)
            {
                if (hdrpAsset != null && hdrpAsset.volumeProfile != null && hdrpAsset.currentPlatformRenderPipelineSettings.supportVolumetricClouds)
                {
                    isValidConfiguration &= CheckVolumeProfileValid(hdrpAsset.volumeProfile, hdrpAsset);
                }
            }

            return isValidConfiguration;
        }

        internal static bool ValidateHighQualityLineRenderingConfiguration(List<HDRenderPipelineAsset> assetsList)
        {
            static bool CheckVolumeProfileValid(VolumeProfile volumeProfile, HDRenderPipelineAsset hdAsset = null)
            {
                if (volumeProfile.TryGet<HighQualityLineRenderingVolumeComponent>(out var hqLines) && hqLines.active)
                {
                    var validationSettings = HDProjectSettings.validationSettings;
                    var defaultHQLines = HDEditorUtils.GetVolumeComponentDefaultState<HighQualityLineRenderingVolumeComponent>();

                    bool effectiveEnable = HDEditorUtils.GetEffectiveParameterValue(
                        hqLines.enable, defaultHQLines?.enable, false);

                    bool warningsFound = false;
                    if (effectiveEnable && !validationSettings.k_HighQualityLineRendering_Recommended)
                    {
                        Debug.LogWarning($"HDRP Build Validation [{volumeProfile.name}] - {HDRenderPipelineUI.Styles.highQualityLineRenderingSubTitle.text}: Enabled for the active platform. {HDRenderPipelineUI.Styles.featureNotRecommendedWarning}\nAsset: {AssetDatabase.GetAssetPath(volumeProfile)}", volumeProfile);
                        warningsFound = true;
                    }

                    return !warningsFound;
                }

                return true;
            }

            if (!EditorGraphicsSettings.ShouldValidateGraphicsForActiveBuildTarget())
                return true;

            // Check default volume profile from HDRP Global Settings
            bool isValidConfiguration = true;
            var defaultVolumeProfileSettings = GraphicsSettings.GetRenderPipelineSettings<HDRPDefaultVolumeProfileSettings>();
            if (GraphicsSettings.defaultRenderPipeline is HDRenderPipelineAsset hdAsset && defaultVolumeProfileSettings?.volumeProfile != null)
            {
                isValidConfiguration &= CheckVolumeProfileValid(defaultVolumeProfileSettings.volumeProfile, hdAsset);
            }

            // Check volume profiles in each HDRP asset
            foreach (var hdrpAsset in assetsList)
            {
                if (hdrpAsset != null && hdrpAsset.volumeProfile != null && hdrpAsset.currentPlatformRenderPipelineSettings.supportHighQualityLineRendering)
                {
                    isValidConfiguration &= CheckVolumeProfileValid(hdrpAsset.volumeProfile, hdrpAsset);
                }
            }

            return isValidConfiguration;
        }

        internal static bool ValidateGraphicsCompositorConfiguration(List<HDRenderPipelineAsset> assetsList)
        {
            static bool CheckHDAssetValid(HDRenderPipelineAsset hdAsset)
            {
                var validationSettings = HDProjectSettings.validationSettings;
                if (!validationSettings.k_GraphicsCompositor_Recommended && hdAsset.compositorCustomVolumeComponentsList.Count > 0)
                {
                    Debug.LogWarning($"HDRP Build Validation - Graphics Compositor: Enabled for the active platform. {HDRenderPipelineUI.Styles.featureNotRecommendedWarning}\nMake sure no compositor components are remaining in the build scenes. Go to Window -> Rendering -> Graphics Compositor to disable & delete remaining components.");
                    return false;
                }

                return true;
            }

            if (!EditorGraphicsSettings.ShouldValidateGraphicsForActiveBuildTarget())
                return true;

            // Check default volume profile from HDRP Global Settings
            bool isValidConfiguration = true;
            foreach (var hdrpAsset in assetsList)
            {
                if (hdrpAsset != null)
                {
                    isValidConfiguration &= CheckHDAssetValid(hdrpAsset);
                }
            }

            return isValidConfiguration;
        }

        internal static void ConfigureMinimumMaxLoDValueForAllQualitySettings()
        {
            int GetMinimumMaxLoDValue(HDRenderPipelineAsset asset)
            {
                int minimumMaxLoD = int.MaxValue;

                if (asset != null)
                {
                    var maxLoDs = asset.currentPlatformRenderPipelineSettings.maximumLODLevel;
                    var schema = ScalableSettingSchema.GetSchemaOrNull(maxLoDs.schemaId);
                    for (int lod = 0; lod < schema.levelCount; ++lod)
                    {
                        if (maxLoDs.TryGet(lod, out int maxLoD))
                            minimumMaxLoD = Mathf.Min(minimumMaxLoD, maxLoD);
                    }
                }

                return minimumMaxLoD != int.MaxValue ? minimumMaxLoD : 0;
            }

            var defaultRenderPipeline = GraphicsSettings.defaultRenderPipeline as HDRenderPipelineAsset;

            // Update all quality levels with the right max lod so that meshes can be stripped.
            // We don't take lod bias into account because it can be overridden per camera.
            QualitySettings.ForEach((tier, name) =>
            {
                if (QualitySettings.renderPipeline is not HDRenderPipelineAsset renderPipeline)
                    renderPipeline = defaultRenderPipeline;

                QualitySettings.maximumLODLevel = GetMinimumMaxLoDValue(renderPipeline);
            });
        }

        private static void GatherShaderFeatures(BuildTarget buildTarget)
        {
            s_SupportedFeaturesList.Clear();
            using (ListPool<HDRenderPipelineAsset>.Get(out List<HDRenderPipelineAsset> hdrpAssets))
            {
                bool buildingForHDRP = buildTarget.TryGetRenderPipelineAssets(hdrpAssets);
                if (buildingForHDRP)
                {
                    // Get Supported features & update data used for Shader Prefiltering and Scriptable Stripping
                    GetSupportedShaderFeaturesFromAssets(ref hdrpAssets, ref s_SupportedFeaturesList);
                }
            }
        }

        private static void GetSupportedShaderFeaturesFromAssets(ref List<HDRenderPipelineAsset> hdrpAssets, ref List<ShaderFeatures> rendererFeaturesList)
        {
            bool useBicubicLightmapSampling = false;
            if (GraphicsSettings.TryGetRenderPipelineSettings<LightmapSamplingSettings>(out var lightmapSamplingSettings))
                useBicubicLightmapSampling = lightmapSamplingSettings.useBicubicLightmapSampling;

            for (int hdrpAssetIndex = 0; hdrpAssetIndex < hdrpAssets.Count; hdrpAssetIndex++)
            {
                // Get the asset and check if it's valid
                HDRenderPipelineAsset hdrpAsset = hdrpAssets[hdrpAssetIndex];
                if (hdrpAsset == null)
                    continue;

                // Check the asset for supported features
                ShaderFeatures hdrpAssetShaderFeatures = GetSupportedShaderFeaturesFromAsset(ref hdrpAsset);

                // Creates a struct containing all the prefiltering settings for this asset
                ShaderPrefilteringData spd = CreatePrefilteringSettings(ref hdrpAssetShaderFeatures, useBicubicLightmapSampling);

                // Update the Prefiltering settings for this URP asset
                hdrpAsset.UpdateShaderKeywordPrefiltering(ref spd);

                // Mark the asset dirty so it can be serialized once the build is finished
                EditorUtility.SetDirty(hdrpAsset);
            }
        }

        private static ShaderFeatures GetSupportedShaderFeaturesFromAsset(ref HDRenderPipelineAsset hdrpAsset)
        {
            ShaderFeatures hdrpAssetShaderFeatures = ShaderFeatures.None;

            if (hdrpAsset.gpuResidentDrawerMode != GPUResidentDrawerMode.Disabled)
                hdrpAssetShaderFeatures |= ShaderFeatures.UseLegacyLightmaps;

            return hdrpAssetShaderFeatures;
        }

        private static ShaderPrefilteringData CreatePrefilteringSettings(ref ShaderFeatures shaderFeatures, bool useBicubicLightmapSampling)
        {
            ShaderPrefilteringData spd = new();

            spd.useLegacyLightmaps = IsFeatureEnabled(shaderFeatures, ShaderFeatures.UseLegacyLightmaps);
            spd.useBicubicLightmapSampling = useBicubicLightmapSampling;

            return spd;
        }

        // Checks whether a ShaderFeature is enabled or not
        private static bool IsFeatureEnabled(ShaderFeatures featureMask, ShaderFeatures feature)
        {
            return (featureMask & feature) != 0;
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            // Clean up the build data once we have finishing building
            m_BuildData?.Dispose();
            m_BuildData = null;
        }
    }
}
