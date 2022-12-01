using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Analytics
{
    internal class BuildTargetAnalytic : IPostprocessBuildWithReport
    {
        public int callbackOrder => int.MaxValue;

        const int k_MaxEventsPerHour = 10;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = "unity.srp";

        [System.Diagnostics.DebuggerDisplay("{render_pipeline_asset_type} - {quality_levels}/{total_quality_levels_on_project}")]
        internal struct BuildTargetAnalyticData
        {
            internal const string k_EventName = "uBuildTargetAnalytic";

            // Naming convention for analytics data
            public string build_target;
            public string render_pipeline_asset_type;
            public int quality_levels;
            public int total_quality_levels_on_project;
        };

        void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport _)
        {
            if (!EditorAnalytics.enabled || EditorAnalytics.RegisterEventWithLimit(BuildTargetAnalyticData.k_EventName, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey) != AnalyticsResult.Ok)
                return;

            if (!TryGatherData(out var data, out var warning))
                Debug.Log(warning);

            EditorAnalytics.SendEventWithLimit(BuildTargetAnalyticData.k_EventName, data);
        }

        [MustUseReturnValue]
        static bool TryGatherData([NotNullWhen(true)] out BuildTargetAnalyticData data, [NotNullWhen(false)] out string warning)
        {
            var activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            var activeBuildTargetGroup = BuildPipeline.GetBuildTargetGroup(activeBuildTarget);
            var activeBuildTargetGroupName = activeBuildTargetGroup.ToString();

            warning = string.Empty;

            var assetType = GraphicsSettings.currentRenderPipeline == null ? "Built-In Render Pipeline" : GraphicsSettings.currentRenderPipeline.GetType().ToString();

            data = new BuildTargetAnalyticData()
            {
                build_target = activeBuildTarget.ToString(),
                quality_levels = QualitySettings.GetActiveQualityLevelsForPlatformCount(activeBuildTargetGroupName),
                render_pipeline_asset_type = assetType,
                total_quality_levels_on_project = QualitySettings.count
            };

            return true;
        }

        [MenuItem("internal:Edit/Rendering/Analytics/Send BuildTargetAnalytic ", priority = 0)]
        static void SendAnalyitic()
        {
            if (!TryGatherData(out var data, out var warning))
                Debug.Log(warning);
        }
    }

}
