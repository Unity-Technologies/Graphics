using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine.Analytics;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Analytics
{
    // schema = com.unity3d.data.schemas.editor.analytics.uDebugManagerWindowLifetimeAnalytic_v1
    // taxonomy = editor.analytics.uDebugManagerWindowLifetimeAnalytic.v1
    internal class DebugManagerWindowLifetimeAnalytic
    {
        const int k_MaxEventsPerHour = 10;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = "unity.srp";

        private static DateTime?[] timeStamps = new DateTime?[2] { null, null};

        [InitializeOnLoadMethod]
        static void SubscribeToDebugManagerOpenCloseWindows()
        {
            DebugManager.windowStateChanged += OnWindowStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private static void OnBeforeAssemblyReload()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            DebugManager.windowStateChanged -= OnWindowStateChanged;
        }

        private static void OnWindowStateChanged(DebugManager.UIMode windowMode, bool open)
        {
            try
            {
                if (!timeStamps[(int)windowMode].HasValue)
                {
                    timeStamps[(int)windowMode] = DateTime.Now;
                }
                else
                {
                    Send(windowMode, timeStamps[(int)windowMode].Value);
                    timeStamps[(int)windowMode] = null;
                }
            }
            catch
            {
                // ignored, do not let analytics throw an error
            }
        }

        [DebuggerDisplay("{window_mode} - {seconds_opened}")]
        class Data
        {
            internal const string k_EventName = "uDebugManagerWindowLifetimeAnalytic";

            // Naming convention for analytics data
            public string window_mode;
            public int seconds_opened;
        }

        static void Send(DebugManager.UIMode windowMode, DateTime start)
        {
            var elapsed = DateTime.Now - start;

            if (EditorAnalytics.enabled &&
                EditorAnalytics.RegisterEventWithLimit(Data.k_EventName, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey) == AnalyticsResult.Ok)
            {
                using (UnityEngine.Pool.GenericPool<Data>.Get(out var data))
                {
                    data.window_mode = windowMode.ToString();
                    data.seconds_opened = elapsed.Seconds;
                    EditorAnalytics.SendEventWithLimit(Data.k_EventName, data);
                }
            }
        }
    }
}
