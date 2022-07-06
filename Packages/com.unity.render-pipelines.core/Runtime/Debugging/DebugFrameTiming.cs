//#define RTPROFILER_DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Timing for debugging a frame
    /// </summary>
    public class DebugFrameTiming
    {
        const string k_FpsFormatString = "{0:F1}";
        const string k_MsFormatString = "{0:F2}ms";
        const float k_RefreshRate = 1f / 5f;

        internal FrameTimeSampleHistory m_FrameHistory;
        internal BottleneckHistory m_BottleneckHistory;

        /// <summary>
        /// Size of the Bottleneck History Window in number of samples.
        /// </summary>
        public int bottleneckHistorySize { get; set; } = 60;

        /// <summary>
        /// Size of the Sample History Window in number of samples.
        /// </summary>
        public int sampleHistorySize { get; set; } = 30;

        FrameTiming[] m_Timing = new FrameTiming[1];
        FrameTimeSample m_Sample = new FrameTimeSample();

        /// <summary>
        /// Default constructor
        /// </summary>
        public DebugFrameTiming()
        {
            m_FrameHistory = new FrameTimeSampleHistory(sampleHistorySize);
            m_BottleneckHistory = new BottleneckHistory(bottleneckHistorySize);
        }

        /// <summary>
        /// Update timing data from profiling counters.
        /// </summary>
        public void UpdateFrameTiming()
        {
            m_Timing[0] = default;
            m_Sample = default;
            FrameTimingManager.CaptureFrameTimings();
            FrameTimingManager.GetLatestTimings(1, m_Timing);

            if (m_Timing.Length > 0)
            {
                m_Sample.FullFrameTime = (float)m_Timing.First().cpuFrameTime;
                m_Sample.FramesPerSecond = m_Sample.FullFrameTime > 0f ? 1000f / m_Sample.FullFrameTime : 0f;
                m_Sample.MainThreadCPUFrameTime = (float)m_Timing.First().cpuMainThreadFrameTime;
                m_Sample.MainThreadCPUPresentWaitTime = (float)m_Timing.First().cpuMainThreadPresentWaitTime;
                m_Sample.RenderThreadCPUFrameTime = (float)m_Timing.First().cpuRenderThreadFrameTime;
                m_Sample.GPUFrameTime = (float)m_Timing.First().gpuFrameTime;
            }

            m_FrameHistory.DiscardOldSamples(sampleHistorySize);
            m_FrameHistory.Add(m_Sample);
            m_FrameHistory.ComputeAggregateValues();

            m_BottleneckHistory.DiscardOldSamples(bottleneckHistorySize);
            m_BottleneckHistory.AddBottleneckFromAveragedSample(m_FrameHistory.SampleAverage);
            m_BottleneckHistory.ComputeHistogram();
        }

        /// <summary>
        /// Add frame timing data widgets to debug UI.
        /// </summary>
        /// <param name="list">List of widgets to add the stats.</param>
        public void RegisterDebugUI(List<DebugUI.Widget> list)
        {
            list.Add(new DebugUI.Foldout()
            {
                displayName = "Frame Stats",
                opened = true,
                columnLabels = new string[] { "Avg", "Min", "Max" },
                children =
                {
                    new DebugUI.ValueTuple
                    {
                        displayName = "Frame Rate (FPS)",
                        values = new[]
                        {
                            new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_FpsFormatString, getter = () => m_FrameHistory.SampleAverage.FramesPerSecond },
                            new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_FpsFormatString, getter = () => m_FrameHistory.SampleMin.FramesPerSecond },
                            new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_FpsFormatString, getter = () => m_FrameHistory.SampleMax.FramesPerSecond },
                        }
                    },
                    new DebugUI.ValueTuple
                    {
                        displayName = "Frame Time",
                        values = new[]
                        {
                            new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_MsFormatString, getter = () => m_FrameHistory.SampleAverage.FullFrameTime },
                            new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_MsFormatString, getter = () => m_FrameHistory.SampleMin.FullFrameTime },
                            new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_MsFormatString, getter = () => m_FrameHistory.SampleMax.FullFrameTime },
                        }
                    },
                    new DebugUI.ValueTuple
                    {
                        displayName = "CPU Main Thread Frame",
                        values = new[]
                        {
                            new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_MsFormatString, getter = () => m_FrameHistory.SampleAverage.MainThreadCPUFrameTime },
                            new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_MsFormatString, getter = () => m_FrameHistory.SampleMin.MainThreadCPUFrameTime },
                            new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_MsFormatString, getter = () => m_FrameHistory.SampleMax.MainThreadCPUFrameTime },
                        }
                    },
                    new DebugUI.ValueTuple
                    {
                        displayName = "CPU Render Thread Frame",
                        values = new[]
                        {
                            new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_MsFormatString, getter = () => m_FrameHistory.SampleAverage.RenderThreadCPUFrameTime },
                            new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_MsFormatString, getter = () => m_FrameHistory.SampleMin.RenderThreadCPUFrameTime },
                            new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_MsFormatString, getter = () => m_FrameHistory.SampleMax.RenderThreadCPUFrameTime },
                        }
                    },
                    new DebugUI.ValueTuple
                    {
                        displayName = "CPU Present Wait",
                        values = new[]
                        {
                            new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_MsFormatString, getter = () => m_FrameHistory.SampleAverage.MainThreadCPUPresentWaitTime },
                            new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_MsFormatString, getter = () => m_FrameHistory.SampleMin.MainThreadCPUPresentWaitTime },
                            new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_MsFormatString, getter = () => m_FrameHistory.SampleMax.MainThreadCPUPresentWaitTime },
                        }
                    },
                    new DebugUI.ValueTuple
                    {
                        displayName = "GPU Frame",
                        values = new[]
                        {
                            new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_MsFormatString, getter = () => m_FrameHistory.SampleAverage.GPUFrameTime },
                            new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_MsFormatString, getter = () => m_FrameHistory.SampleMin.GPUFrameTime },
                            new DebugUI.Value { refreshRate = k_RefreshRate, formatString = k_MsFormatString, getter = () => m_FrameHistory.SampleMax.GPUFrameTime },
                        }
                    }
                }
            });

            list.Add(new DebugUI.Foldout
            {
                displayName = "Bottlenecks",
                children =
                {
#if UNITY_EDITOR
                    new DebugUI.Container { displayName = "Not supported in Editor" }
#else
                    new DebugUI.ProgressBarValue { displayName = "CPU", getter = () => m_BottleneckHistory.Histogram.CPU },
                    new DebugUI.ProgressBarValue { displayName = "GPU", getter = () => m_BottleneckHistory.Histogram.GPU },
                    new DebugUI.ProgressBarValue { displayName = "Present limited", getter = () => m_BottleneckHistory.Histogram.PresentLimited },
                    new DebugUI.ProgressBarValue { displayName = "Balanced", getter = () => m_BottleneckHistory.Histogram.Balanced },
#endif
                }
            });
#if RTPROFILER_DEBUG
            list.Add(new DebugUI.Foldout
            {
                displayName = "Realtime Profiler Debug",
                children =
                {
                    new DebugUI.IntField
                    {
                        displayName = "Frame Time Sample History Size",
                        getter = () => sampleHistorySize,
                        setter = (value) => { sampleHistorySize = value; },
                        min = () => 1,
                        max = () => 100
                    },
                    new DebugUI.IntField
                    {
                        displayName = "Bottleneck History Size",
                        getter = () => bottleneckHistorySize,
                        setter = (value) => { bottleneckHistorySize = value; },
                        min = () => 1,
                        max = () => 100
                    },
                    new DebugUI.IntField
                    {
                        displayName = "Force VSyncCount",
                        min = () => - 1,
                        max = () => 4,
                        getter = () => QualitySettings.vSyncCount,
                        setter = (value) => { QualitySettings.vSyncCount = value; }
                    },
                    new DebugUI.IntField
                    {
                        displayName = "Force TargetFrameRate",
                        min = () => - 1,
                        max = () => 1000,
                        getter = () => Application.targetFrameRate,
                        setter = (value) => { Application.targetFrameRate = value; }
                    },
                }
            });
#endif
        }

        internal void Reset()
        {
            m_BottleneckHistory.Clear();
            m_FrameHistory.Clear();
        }
    }
}
