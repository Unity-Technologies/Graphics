using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.Analytics;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDAnalytics : IPostprocessBuildWithReport
    {
        const int k_MaxEventsPerHour = 10;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = "unity.hdrp";

        public int callbackOrder => int.MaxValue;

        struct UsageEventData
        {
            internal const string k_EventName = "uHDRPUsage";
            internal const int k_CurrentVersion = 2;

            // Naming convention for analytics data
            public string build_target;
            public string asset_guid;
            public string[] changed_settings;
        }

        static void SendUsage()
        {
            if (!EditorAnalytics.enabled || EditorAnalytics.RegisterEventWithLimit(UsageEventData.k_EventName, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey, UsageEventData.k_CurrentVersion) != AnalyticsResult.Ok)
                return;

            var activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;

            using (ListPool<HDRenderPipelineAsset>.Get(out var tmp))
            {
                if (!EditorUserBuildSettings.activeBuildTarget.TryGetRenderPipelineAssets<HDRenderPipelineAsset>(tmp) || tmp.Count == 0)
                    return;

                RenderPipelineSettings defaults = RenderPipelineSettings.NewDefault();

                foreach (var hdrpAsset in tmp)
                {
                    var data = new UsageEventData()
                    {
                        build_target = activeBuildTarget.ToString(),
                        asset_guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(hdrpAsset.GetInstanceID())),
                        changed_settings = hdrpAsset.currentPlatformRenderPipelineSettings.ToNestedColumnWithDefault(defaults, true)
                    };
                    EditorAnalytics.SendEventWithLimit(UsageEventData.k_EventName, data, UsageEventData.k_CurrentVersion);
                }
            }
        }

        void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport _)
        {
            SendUsage();
        }

        [MenuItem("internal:Edit/Rendering/Analytics/Send HDRP usage analytics", priority = 0)]
        static void SendAnalytic()
        {
            SendUsage();
        }
    }
}
