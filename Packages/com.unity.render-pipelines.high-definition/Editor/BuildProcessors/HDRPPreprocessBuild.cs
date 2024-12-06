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

                GatherShaderFeatures();
            }
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
