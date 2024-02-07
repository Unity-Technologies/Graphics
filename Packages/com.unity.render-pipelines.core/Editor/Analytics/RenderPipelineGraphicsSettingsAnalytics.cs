using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.Analytics;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class RenderPipelineGraphicsSettingsAnalytics : IPostprocessBuildWithReport
    {
        const int k_MaxEventsPerHour = 1000;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = "unity.srp";
        const string k_EventName = "uRenderPipelineGraphicsSettingsUsage";

        [AnalyticInfo(eventName: k_EventName, vendorKey: k_VendorKey, maxEventsPerHour: k_MaxEventsPerHour, maxNumberOfElements: k_MaxNumberOfElements)]
        public class Analytic : IAnalytic
        {
            [Serializable]
            public struct AnalyticsData : IAnalytic.IData
            {
                public string settings;
                public string[] usage;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = GatherDataToBeSent();
                error = null;
                return true;
            }

            public static IAnalytic.DataList<AnalyticsData> GatherDataToBeSent()
            {
                using (ListPool<AnalyticsData>.Get(out var tmp))
                {
                    GraphicsSettings.ForEach(settings =>
                    {
                        var settingsType = settings.GetType();
                        var usage = settings.ToNestedColumn(Activator.CreateInstance(settingsType));
                        if (usage.Length != 0)
                            tmp.Add(new AnalyticsData() { settings = settingsType.FullName, usage = usage  });
                    });

                    return new IAnalytic.DataList<AnalyticsData>(tmp.ToArray());
                }
            }
        }

        static void SendUniversalEvent()
        {
            //The event shouldn't be able to report if this is disabled but if we know we're not going to report
            //Lets early out and not waste time gathering all the data
            if (!EditorAnalytics.enabled)
                return;

            Analytic analytic = new Analytic();
            EditorAnalytics.SendAnalytic(analytic);
        }

        public int callbackOrder { get; }
        public void OnPostprocessBuild(BuildReport report)
        {
            SendUniversalEvent();
        }
    }
}
