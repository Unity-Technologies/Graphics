using System;
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

        [Serializable]
        internal struct UsageEventData : IAnalytic.IData
        {
            // Naming convention for analytics data
            public string build_target;
            public string asset_guid;
            public string[] changed_settings;
        }

        [AnalyticInfo(eventName: "uHDRPUsage", vendorKey: "unity.vfxgraph", maxEventsPerHour: 10, maxNumberOfElements: 1000, version: 2)]
        internal class Analytic : IAnalytic
        {
            public Analytic(UsageEventData data) { m_Data = data; }
            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = m_Data;
                error = null;
                return true;
            }

            UsageEventData m_Data;
        }

        static void SendUsage()
        {
            if (!EditorAnalytics.enabled)
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
                    Analytic analytic = new Analytic(data);
                    EditorAnalytics.SendAnalytic(analytic);
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
