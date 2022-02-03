using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Bridge
{
    static class GraphViewTestHelpers
    {
        public class TimerEventSchedulerWrapper : IDisposable
        {
            readonly VisualElement m_GraphView;

            internal TimerEventSchedulerWrapper(VisualElement graphView)
            {
                m_GraphView = graphView;
                Panel.TimeSinceStartup = () => TimeSinceStartup;
            }

            public long TimeSinceStartup { get; set; }

            public void Dispose()
            {
                Panel.TimeSinceStartup = null;
            }

            public void UpdateScheduledEvents()
            {
                TimerEventScheduler s = (TimerEventScheduler)m_GraphView.elementPanel.scheduler;
                s.UpdateScheduledEvents();
            }
        }

        public static TimerEventSchedulerWrapper CreateTimerEventSchedulerWrapper(this VisualElement graphView)
        {
            return new TimerEventSchedulerWrapper(graphView);
        }
    }
}
