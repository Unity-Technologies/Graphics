using JetBrains.Annotations;
using System;
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

        [AnalyticInfo(eventName: "uBuildTargetAnalytic", vendorKey: "unity.srp", maxEventsPerHour: 10, maxNumberOfElements: 1000)]
        internal class Analytic : IAnalytic
        {

            [MustUseReturnValue]
            public bool TryGatherData([NotNullWhen(true)] out IAnalytic.IData data, [NotNullWhen(false)] out Exception error)
            {
                var activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
                var activeBuildTargetGroup = BuildPipeline.GetBuildTargetGroup(activeBuildTarget);
                var activeBuildTargetGroupName = activeBuildTargetGroup.ToString();

                error = null;

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

        };

        [System.Diagnostics.DebuggerDisplay("{render_pipeline_asset_type} - {quality_levels}/{total_quality_levels_on_project}")]
        [Serializable]
        internal struct BuildTargetAnalyticData : IAnalytic.IData
        {
            // Naming convention for analytics data
            public string build_target;
            public string render_pipeline_asset_type;
            public int quality_levels;
            public int total_quality_levels_on_project;
        };

        void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport _)
        {
            SendAnalytic();
        }

        [MenuItem("internal:Edit/Rendering/Analytics/Send BuildTargetAnalytic ", priority = 0)]
        static void SendAnalytic()
        {
            Analytic analytic = new Analytic();
            EditorAnalytics.SendAnalytic(analytic);
        }
    }

}
