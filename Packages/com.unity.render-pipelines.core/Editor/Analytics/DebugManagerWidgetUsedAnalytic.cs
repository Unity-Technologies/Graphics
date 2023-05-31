using System.Diagnostics;
using UnityEngine.Analytics;

namespace UnityEditor.Rendering.Analytics
{
    // schema = com.unity3d.data.schemas.editor.analytics.uDebugManagerWidgetUsedAnalytic_v1
    // taxonomy = editor.analytics.uDebugManagerWidgetUsedAnalytic.v1
    internal class DebugManagerWidgetUsedAnalytic
    {
        const int k_MaxEventsPerHour = 1000;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = "unity.srp";

        [DebuggerDisplay("{query_path} - {value}")]
        class Data
        {
            internal const string k_EventName = "uDebugManagerWidgetUsedAnalytic";

            // Naming convention for analytics data
            public string query_path;
            public string value;
        }

        public static void Send(string path, object value)
        {
            if (EditorAnalytics.enabled &&
                EditorAnalytics.RegisterEventWithLimit(Data.k_EventName, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey) == AnalyticsResult.Ok)
            {
                using (UnityEngine.Pool.GenericPool<Data>.Get(out var data))
                {
                    data.query_path = path;
                    data.value = value.ToString();
                    EditorAnalytics.SendEventWithLimit(Data.k_EventName, data);
                }
            }
        }
    }
}
