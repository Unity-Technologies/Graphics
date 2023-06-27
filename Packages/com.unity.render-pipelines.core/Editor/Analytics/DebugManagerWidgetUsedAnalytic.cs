using System;
using System.Diagnostics;
using System.IO;
using UnityEngine.Analytics;
using UnityEngine.Rendering;
using static UnityEditor.Rendering.Analytics.DebugManagerWidgetUsedAnalytic.Analytic;
using static UnityEngine.Rendering.DebugUI;

namespace UnityEditor.Rendering.Analytics
{
    // schema = com.unity3d.data.schemas.editor.analytics.uDebugManagerWidgetUsedAnalytic_v1
    // taxonomy = editor.analytics.uDebugManagerWidgetUsedAnalytic.v1
    internal class DebugManagerWidgetUsedAnalytic
    {
        [AnalyticInfo(eventName: "uDebugManagerWidgetUsedAnalytic", vendorKey: "unity.srp", maxEventsPerHour: 1000, maxNumberOfElements: 1000)]
        internal class Analytic : IAnalytic
        {
            public Analytic(string path, object value)
            {
                using (UnityEngine.Pool.GenericPool<Data>.Get(out var data))
                {
                    data.query_path = path;
                    data.value = value.ToString();
                    m_Data = data;

                }
            }

            [DebuggerDisplay("{query_path} - {value}")]
            [Serializable]
            class Data : IAnalytic.IData
            {
                // Naming convention for analytics data
                public string query_path;
                public string value;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = m_Data;
                error = null;
                return true;
            }

            Data m_Data;
        };

        public static void Send(string path, object value)
        {
            Analytic analytic = new Analytic(path, value);
            EditorAnalytics.SendAnalytic(analytic);
        }
    }
}
