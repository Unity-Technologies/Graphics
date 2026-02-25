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
            bool isDevelopmentBuild = (report.summary.options & BuildOptions.Development) != 0;
            m_BuildData = new HDRPBuildData(EditorUserBuildSettings.activeBuildTarget, isDevelopmentBuild);

            if (m_BuildData.buildingPlayerForHDRenderPipeline)
            {
                // Now that we know that we are on HDRP we need to make sure everything is correct, otherwise we break the build.
                if (!HDRPBuildDataValidator.IsProjectValidForBuilding(report, out var message))
                    throw new BuildFailedException(message);

                ConfigureMinimumMaxLoDValueForAllQualitySettings();

                LogIncludedAssets(m_BuildData.renderPipelineAssets);

                if (!IsConfigurationValid())
                {
                    if(!ProceedWithBuild())
                        throw new BuildFailedException("Build canceled by user due to HDRP configuration issues.");
                }
                    

                GatherShaderFeatures();
            }
        }

        private static bool IsConfigurationValid()
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
                bool config = ValidateRayTracingConfiguration(m_BuildData.renderPipelineAssets);
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

        internal static bool ValidateRayTracingConfiguration(List<HDRenderPipelineAsset> assetsList)
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

            var currentBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            if (HDRenderPipeline.PlatformHasRaytracingIssues(currentBuildTarget, out var warning))
            {
                Debug.LogWarning($"HDRP Build Validation - Ray Tracing:{warning}");
                return false;
            }

            return true;
        }

        internal static bool ValidateSubsurfaceScatteringConfiguration(List<HDRenderPipelineAsset> assetsList)
        {
            var currentBuildTarget = EditorUserBuildSettings.activeBuildTarget;

            // Only validate for Switch 2
            if (currentBuildTarget != BuildTarget.Switch2)
                return true;

            // Check if any asset has Subsurface Scattering enabled
            bool anyAssetHasSSSEnabled = false;
            foreach (var hdrpAsset in assetsList)
            {
                if (hdrpAsset != null && hdrpAsset.currentPlatformRenderPipelineSettings.supportSubsurfaceScattering)
                {
                    anyAssetHasSSSEnabled = true;
                    break;
                }
            }

            if (!anyAssetHasSSSEnabled)
                return true; // No SSS enabled, skip validation

            var activeBuildTargetGroup = BuildPipeline.GetBuildTargetGroup(BuildTarget.Switch2);
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(activeBuildTargetGroup);
            Debug.LogWarning($"HDRP Build Validation - Subsurface Scattering: Subsurface Scattering is enabled for {namedBuildTarget.TargetName}. For optimal performance, set the Downsample Level to the maximum value (2) for this platform.");
            return false;
        }

        internal static bool ValidateFilmGrainConfiguration(List<HDRenderPipelineAsset> assetsList)
        {
            var currentBuildTarget = EditorUserBuildSettings.activeBuildTarget;

            // Only validate for Switch 2
            if (currentBuildTarget != BuildTarget.Switch2)
                return true;

            // Check default volume profile from HDRP Global Settings
            bool foundFilmGrain = false;
            var defaultVolumeProfileSettings = GraphicsSettings.GetRenderPipelineSettings<HDRPDefaultVolumeProfileSettings>();
            if (defaultVolumeProfileSettings?.volumeProfile != null)
            {
                if (defaultVolumeProfileSettings.volumeProfile.TryGet<FilmGrain>(out var filmGrain) && filmGrain.intensity.value > 0)
                {
                    foundFilmGrain = true;
                }
            }

            // Check volume profiles in each HDRP asset
            if (!foundFilmGrain)
            {
                foreach (var hdrpAsset in assetsList)
                {
                    if (hdrpAsset != null && hdrpAsset.volumeProfile != null)
                    {
                        if (hdrpAsset.volumeProfile.TryGet<FilmGrain>(out var filmGrain) && filmGrain.intensity.value > 0)
                        {
                            foundFilmGrain = true;
                            break;
                        }
                    }
                }
            }

            if (!foundFilmGrain)
                return true; // No Film Grain with intensity > 0, skip validation

            var activeBuildTargetGroup = BuildPipeline.GetBuildTargetGroup(BuildTarget.Switch2);
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(activeBuildTargetGroup);
            Debug.LogWarning($"HDRP Build Validation - Film Grain: Film Grain is enabled for {namedBuildTarget.TargetName}. This may significantly impact performance and should be disabled for this platform.");
            return false;
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

        private static void GatherShaderFeatures()
        {
            s_SupportedFeaturesList.Clear();
            using (ListPool<HDRenderPipelineAsset>.Get(out List<HDRenderPipelineAsset> hdrpAssets))
            {
                bool buildingForHDRP = EditorUserBuildSettings.activeBuildTarget.TryGetRenderPipelineAssets(hdrpAssets);
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
