using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine.Analytics;
using UnityEngine.Rendering;
using static UnityEngine.Analytics.IAnalytic;

namespace UnityEditor.Rendering.Analytics
{
    // schema = com.unity3d.data.schemas.editor.analytics.uDebugManagerWindowLifetimeAnalytic_v1
    // taxonomy = editor.analytics.uDebugManagerWindowLifetimeAnalytic.v1
    internal class DebugManagerWindowLifetimeAnalytic
    {

        private static DateTime?[] timeStamps = new DateTime?[2] { null, null };

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

        [AnalyticInfo(eventName: "uDebugManagerWindowLifetimeAnalytic", vendorKey: "unity.srp", maxEventsPerHour: 10, maxNumberOfElements: 1000)]
        internal class Analytic : IAnalytic
        {
            public Analytic(DebugManager.UIMode windowMode, DateTime start)
            {
                var elapsed = DateTime.Now - start;
                using (UnityEngine.Pool.GenericPool<Data>.Get(out var data))
                {
                    data.window_mode = windowMode.ToString();
                    data.seconds_opened = elapsed.Seconds;
                    m_Data = data;
                }
            }

            [DebuggerDisplay("{window_mode} - {seconds_opened}")]
            [Serializable]
            class Data : IAnalytic.IData
            {
                // Naming convention for analytics data
                public string window_mode;
                public int seconds_opened;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = m_Data;
                error = null;
                return true;
            }
            Data m_Data;

        };

        static void Send(DebugManager.UIMode windowMode, DateTime start)
        {
            Analytic analytic = new Analytic(windowMode, start);
            EditorAnalytics.SendAnalytic(analytic);
        }
    };
}
