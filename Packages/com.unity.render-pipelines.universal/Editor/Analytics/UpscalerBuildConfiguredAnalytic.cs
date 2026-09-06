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
            // Use renderScale as default
            UpscalerQualityInfo qualityInfo = UpscalerQualityInfo.FromFloat(urpAsset.renderScale);

#if ENABLE_UPSCALER_FRAMEWORK
            // No upscaler selected — null is handled as an empty list.
            if (urpAsset.upscalerPriority.Count == 0)
                return null;

            // Any listed upscaler can end up being the one that runs, so the report carries all of them, in order.
            var upscalerInfoPairs = new List<(string, UpscalerQualityInfo)>(urpAsset.upscalerPriority.Count);
            foreach (var upscalerPriorityEntry in urpAsset.upscalerPriority)
            {
                UpscalerOptions options = urpAsset.GetUpscalerOptions(upscalerPriorityEntry.upscalerId);
                if (!Rendering.Analytics.UpscalerBuildConfiguredAnalytic.TryGetQualityInfo(options, out var info))
                    info = qualityInfo;

                upscalerInfoPairs.Add((upscalerPriorityEntry.upscalerId, info));
            }

            return upscalerInfoPairs;
#else
            // Legacy upscaler
            string upscalerName = urpAsset.upscalingFilter.ToString();

            return new List<(string, UpscalerQualityInfo)> { (upscalerName, qualityInfo) };
#endif
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
