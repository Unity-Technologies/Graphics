using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering.Analytics;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class UpscalerBuildConfiguredAnalytic : IPostprocessBuildWithReport
    {
        static Rendering.Analytics.UpscalerBuildConfiguredAnalytic.UpscalerConfiguredData CreateAnalyticsData(string buildGuid, HDRenderPipelineAsset hdrpAsset)
        {
            if (hdrpAsset == null)
                return default;

            var dynResSettings = hdrpAsset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings;

            var upscalerPairs = dynResSettings.enabled ? GetUpscalerInfoPairs(dynResSettings) : null;

            return new Rendering.Analytics.UpscalerBuildConfiguredAnalytic.UpscalerConfiguredData()
            {
                build_target = EditorUserBuildSettings.activeBuildTarget.ToString(),
                renderPipeline = hdrpAsset.pipelineType.Name,
                upscalerPriorityList = Rendering.Analytics.UpscalerBuildConfiguredAnalytic.FormatUpscalerPriorityList(upscalerPairs),
                build_guid = buildGuid,
            };
        }

        static List<(string name, UpscalerQualityInfo qualityInfo)> GetUpscalerInfoPairs(GlobalDynamicResolutionSettings dynResSettings)
        {
            var upscalerNames = dynResSettings.advancedUpscalerNames;
            var pairs = new List<(string, UpscalerQualityInfo)>();

            if (upscalerNames != null)
            {
                foreach (var upscalerName in upscalerNames)
                {
                    pairs.Add((upscalerName, GetQualityInfoForUpscaler(upscalerName, dynResSettings)));
                }
            }

            // Add upsampleFilter as the final fallback upscaler.
            // It has no quality mode, so it only depends on forceResolution vs auto DRS.
            string upsampleFilterName = dynResSettings.upsampleFilter.ToString();
            pairs.Add((upsampleFilterName, GetQualityInfoForUpscaler(null, dynResSettings)));

            return pairs;
        }

        static UpscalerQualityInfo GetQualityInfoForUpscaler(string upscalerName, GlobalDynamicResolutionSettings dynResSettings)
        {
            // forceResolution takes precedence — the forced percentage is used regardless of quality mode.
            if (dynResSettings.forceResolution)
                return UpscalerQualityInfo.FromFloat(dynResSettings.forcedPercentage / 100f);

#if ENABLE_AMD && ENABLE_AMD_MODULE
            if (upscalerName == "FSR2" && dynResSettings.FSR2UseOptimalSettings)
                return UpscalerQualityInfo.FromString(
                    ((UnityEngine.AMD.FSR2Quality)dynResSettings.FSR2QualitySetting).ToString());
#endif

#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
            if (upscalerName == "DLSS" && dynResSettings.DLSSUseOptimalSettings)
                return UpscalerQualityInfo.FromString(
                    ((UnityEngine.NVIDIA.DLSSQuality)dynResSettings.DLSSPerfQualitySetting).ToString());
#endif

#if ENABLE_UPSCALER_FRAMEWORK
            if (!string.IsNullOrEmpty(upscalerName) && dynResSettings.upscalerOptions != null)
            {
                foreach (var options in dynResSettings.upscalerOptions)
                {
                    if (options != null && options.upscalerName == upscalerName)
                    {
                        if (Rendering.Analytics.UpscalerBuildConfiguredAnalytic.TryGetQualityInfo(options, out var info))
                            return info;
                        break;
                    }
                }
            }
#endif

            // auto DRS min/max
            return UpscalerQualityInfo.FromString("-1");
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            Rendering.Analytics.UpscalerBuildConfiguredAnalytic.SendEvent(
                report.summary.guid.ToString(),
                HDRPBuildData.instance.renderPipelineAssets,
                CreateAnalyticsData);
        }

        public int callbackOrder => 0;
    }
}
