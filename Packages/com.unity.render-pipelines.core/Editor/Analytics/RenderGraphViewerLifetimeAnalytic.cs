using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Analytics;

namespace UnityEditor.Rendering.Analytics
{
    // schema = com.unity3d.data.schemas.editor.analytics.uRenderGraphViewerLifetimeAnalytic_v1
    // taxonomy = editor.analytics.uRenderGraphViewerLifetimeAnalytic.v1
    internal class RenderGraphViewerLifetimeAnalytic
    {
        static bool IsInternalAssembly(Type type)
        {
            var assemblyName = type.Assembly.FullName;
            if (assemblyName.StartsWith("UnityEditor.", StringComparison.InvariantCultureIgnoreCase) ||
                assemblyName.StartsWith("Unity.", StringComparison.InvariantCultureIgnoreCase))
                return true;
            return false;
        }

        static List<string> GatherCurrentlyOpenWindowNames()
        {
            var openWindows = Resources.FindObjectsOfTypeAll(typeof(EditorWindow));
            var openWindowNames = new List<string>(openWindows.Length);
            foreach (var w in openWindows)
            {
                if (IsInternalAssembly(w.GetType()) && w is not RenderGraphViewer)
                {
                    openWindowNames.Add((w as EditorWindow).titleContent.text);
                }
            }
            return openWindowNames;
        }

        static string[] UnionWithoutLinq(List<string> a, List<string> b)
        {
            HashSet<string> aAndB = new HashSet<string>(a);
            aAndB.UnionWith(b);
            String[] aAndBArray = new String[aAndB.Count];
            aAndB.CopyTo(aAndBArray);
            return aAndBArray;
        }

        [AnalyticInfo(eventName: "uRenderGraphViewerLifetimeAnalytic", vendorKey: "unity.srp", maxEventsPerHour: 100, maxNumberOfElements: 1000)]
        internal class Analytic : IAnalytic
        {
            public Analytic(WindowOpenedMetadata windowOpenedMetadata)
            {
                List<string> currentlyOpenEditorWindows = GatherCurrentlyOpenWindowNames();
                var elapsed = DateTime.Now - windowOpenedMetadata.openedTime;
                using (UnityEngine.Pool.GenericPool<Data>.Get(out var data))
                {
                    data.seconds_opened = elapsed.Seconds;
                    data.other_open_windows = UnionWithoutLinq(currentlyOpenEditorWindows, windowOpenedMetadata.openEditorWindows);

                    m_Data = data;
                }
            }

            [DebuggerDisplay("{seconds_opened} - {other_open_windows.Length}")]
            [Serializable]
            class Data : IAnalytic.IData
            {
                // Naming convention for analytics data
                public int seconds_opened;
                public string[] other_open_windows;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = m_Data;
                error = null;
                return true;
            }

            Data m_Data;
        }

        internal struct WindowOpenedMetadata
        {
            public List<string> openEditorWindows;
            public DateTime openedTime;
        }

        static WindowOpenedMetadata? s_WindowOpenedMetadata = null;

        public static void WindowOpened()
        {
            s_WindowOpenedMetadata = new WindowOpenedMetadata
            {
                openEditorWindows = GatherCurrentlyOpenWindowNames(),
                openedTime = DateTime.Now
            };
        }

        public static void WindowClosed()
        {
            if (s_WindowOpenedMetadata.HasValue)
            {
                Analytic analytic = new Analytic(s_WindowOpenedMetadata.Value);
                EditorAnalytics.SendAnalytic(analytic);
                s_WindowOpenedMetadata = null;
            }
        }
    }
}
