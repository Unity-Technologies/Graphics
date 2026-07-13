using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering.Analytics;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    class UpscalerBuildConfiguredAnalytic : IPostprocessBuildWithReport
    {
        static Rendering.Analytics.UpscalerBuildConfiguredAnalytic.UpscalerConfiguredData CreateAnalyticsData(string buildGuid, UniversalRenderPipelineAsset urpAsset)
        {
            if (urpAsset == null)
                return default;

            var upscalerPairs = GetUpscalerInfoPairs(urpAsset);

            return new Rendering.Analytics.UpscalerBuildConfiguredAnalytic.UpscalerConfiguredData()
            {
                build_target = EditorUserBuildSettings.activeBuildTarget.ToString(),
                renderPipeline = urpAsset.pipelineType.Name,
                upscalerPriorityList = Rendering.Analytics.UpscalerBuildConfiguredAnalytic.FormatUpscalerPriorityList(upscalerPairs),
                build_guid = buildGuid,
            };
        }

        static List<(string name, UpscalerQualityInfo qualityInfo)> GetUpscalerInfoPairs(UniversalRenderPipelineAsset urpAsset)
        {
            string upscalerName = urpAsset.upscalerName;

            // Use renderScale as default
            UpscalerQualityInfo qualityInfo = UpscalerQualityInfo.FromFloat(urpAsset.renderScale);

#if ENABLE_UPSCALER_FRAMEWORK
            // No upscaler selected — null is handled as an empty list.
            if (string.IsNullOrEmpty(upscalerName))
                return null;

            UpscalerOptions options = urpAsset.GetUpscalerOptions(upscalerName);
            if (Rendering.Analytics.UpscalerBuildConfiguredAnalytic.TryGetQualityInfo(options, out var info))
                qualityInfo = info;
#else
            // Legacy upscaler
            upscalerName = urpAsset.upscalingFilter.ToString();
#endif

            return new List<(string, UpscalerQualityInfo)> { (upscalerName, qualityInfo) };
        }

        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            Rendering.Analytics.UpscalerBuildConfiguredAnalytic.SendEvent(
                report.summary.guid.ToString(),
                URPBuildData.instance.renderPipelineAssets,
                CreateAnalyticsData);
        }
    }
}
