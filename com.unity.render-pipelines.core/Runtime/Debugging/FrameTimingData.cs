using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

//#define RTPROFILER_DEBUG

public class FrameTimingData
{
    /// <summary>
    /// Size of the Bottleneck History Window in number of samples.
    /// </summary>
    public int BottleneckHistorySize { get; set; } = 60;

    /// <summary>
    /// Size of the Sample History Window in number of samples.
    /// </summary>
    public int SampleHistorySize { get; set; } = 30;

    /// <summary>
    /// Update timing data from profiling counters.
    /// </summary>
    public void UpdateFrameTiming()
    {
        FrameTiming[] timing = new FrameTiming[1];
        FrameTimingManager.CaptureFrameTimings();
        FrameTimingManager.GetLatestTimings(1, timing);

        m_History.DiscardOldSamples(SampleHistorySize);

        FrameTimeSample sample = new FrameTimeSample();

        if (timing.Length > 0)
        {
            sample.FullFrameTime = (float)timing.First().cpuFrameTime;
            sample.FramesPerSecond = 1000f / sample.FullFrameTime;
            sample.MainThreadCPUFrameTime = (float)timing.First().cpuMainThreadFrameTime;
            sample.MainThreadCPUPresentWaitTime = (float)timing.First().cpuMainThreadPresentWaitTime;
            sample.RenderThreadCPUFrameTime = (float)timing.First().cpuRenderThreadFrameTime;
            sample.GPUFrameTime = (float)timing.First().gpuFrameTime;
        }

        m_History.Add(sample);
        m_History.ComputeAggregateValues();

        var bottleneck = DetermineBottleneck(m_History.SampleAverage);

        while (m_BottleneckHistory.Count > BottleneckHistorySize)
        {
            m_BottleneckHistory.RemoveAt(0);
        }

        m_BottleneckHistory.Add(bottleneck);
        m_BottleneckData = ComputeBottleneckStats();
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
                    displayName = "Frame Rate, fps",
                    refreshRate = 1f / 5f,
                    values = new[]
                    {
                        new DebugUI.Value { formatString = "F2", getter = () => m_History.SampleAverage.FramesPerSecond },
                        new DebugUI.Value { formatString = "F2", getter = () => m_History.SampleMin.FramesPerSecond },
                        new DebugUI.Value { formatString = "F2", getter = () => m_History.SampleMax.FramesPerSecond },
                    }
                },
                new DebugUI.ValueTuple
                {
                    displayName = "Frame Time, ms",
                    refreshRate = 1f / 5f,
                    values = new[]
                    {
                        new DebugUI.Value { formatString = "F2", getter = () => m_History.SampleAverage.FullFrameTime },
                        new DebugUI.Value { formatString = "F2", getter = () => m_History.SampleMin.FullFrameTime },
                        new DebugUI.Value { formatString = "F2", getter = () => m_History.SampleMax.FullFrameTime },
                    }
                },
                new DebugUI.ValueTuple
                {
                    displayName = "CPU Main Thread Frame, ms",
                    refreshRate = 1f / 5f,
                    values = new[]
                    {
                        new DebugUI.Value { formatString = "F2", getter = () => m_History.SampleAverage.MainThreadCPUFrameTime },
                        new DebugUI.Value { formatString = "F2", getter = () => m_History.SampleMin.MainThreadCPUFrameTime },
                        new DebugUI.Value { formatString = "F2", getter = () => m_History.SampleMax.MainThreadCPUFrameTime },
                    }
                },
                new DebugUI.ValueTuple
                {
                    displayName = "CPU Render Thread Frame, ms",
                    refreshRate = 1f / 5f,
                    values = new[]
                    {
                        new DebugUI.Value { formatString = "F2", getter = () => m_History.SampleAverage.RenderThreadCPUFrameTime },
                        new DebugUI.Value { formatString = "F2", getter = () => m_History.SampleMin.RenderThreadCPUFrameTime },
                        new DebugUI.Value { formatString = "F2", getter = () => m_History.SampleMax.RenderThreadCPUFrameTime },
                    }
                },
                new DebugUI.ValueTuple
                {
                    displayName = "CPU Present Wait, ms",
                    refreshRate = 1f / 5f,
                    values = new[]
                    {
                        new DebugUI.Value { formatString = "F2", getter = () => m_History.SampleAverage.MainThreadCPUPresentWaitTime },
                        new DebugUI.Value { formatString = "F2", getter = () => m_History.SampleMin.MainThreadCPUPresentWaitTime },
                        new DebugUI.Value { formatString = "F2", getter = () => m_History.SampleMax.MainThreadCPUPresentWaitTime },
                    }
                },
                new DebugUI.ValueTuple
                {
                    displayName = "GPU Frame, ms",
                    refreshRate = 1f / 5f,
                    values = new[]
                    {
                        new DebugUI.Value { formatString = "F2", getter = () => m_History.SampleAverage.GPUFrameTime },
                        new DebugUI.Value { formatString = "F2", getter = () => m_History.SampleMin.GPUFrameTime },
                        new DebugUI.Value { formatString = "F2", getter = () => m_History.SampleMax.GPUFrameTime },
                    }
                }
            }
        });

        list.Add(new DebugUI.Foldout
        {
            displayName = "Bottlenecks",
            children =
            {
#if false//UNITY_EDITOR
                new DebugUI.Container { displayName = "Not supported in Editor" }
#else
                new DebugUI.ProgressBarValue { displayName = "CPU", getter = () => m_BottleneckData.CPU },
                new DebugUI.ProgressBarValue { displayName = "GPU", getter = () => m_BottleneckData.GPU },
                new DebugUI.ProgressBarValue { displayName = "Present limited", getter = () => m_BottleneckData.PresentLimited },
                new DebugUI.ProgressBarValue { displayName = "Balanced", getter = () => m_BottleneckData.Balanced },
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
                    getter = () => SampleHistorySize,
                    setter = (value) => { SampleHistorySize = value; },
                    min = () => 1,
                    max = () => 100
                },
                new DebugUI.IntField
                {
                    displayName = "Bottleneck History Size",
                    getter = () => BottleneckHistorySize,
                    setter = (value) => { BottleneckHistorySize = value; },
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

    /// <summary>
    /// Represents timing data captured from a single frame.
    /// </summary>
    struct FrameTimeSample
    {
        internal float FramesPerSecond;
        internal float FullFrameTime;
        internal float MainThreadCPUFrameTime;
        internal float MainThreadCPUPresentWaitTime;
        internal float RenderThreadCPUFrameTime;
        internal float GPUFrameTime;

        internal FrameTimeSample(float initValue)
        {
            FramesPerSecond = initValue;
            FullFrameTime = initValue;
            MainThreadCPUFrameTime = initValue;
            MainThreadCPUPresentWaitTime = initValue;
            RenderThreadCPUFrameTime = initValue;
            GPUFrameTime = initValue;
        }

        internal void Add(FrameTimeSample other)
        {
            FramesPerSecond += other.FramesPerSecond;
            FullFrameTime += other.FullFrameTime;
            MainThreadCPUFrameTime += other.MainThreadCPUFrameTime;
            MainThreadCPUPresentWaitTime += other.MainThreadCPUPresentWaitTime;
            RenderThreadCPUFrameTime += other.RenderThreadCPUFrameTime;
            GPUFrameTime += other.GPUFrameTime;
        }

        internal void Divide(float denominator)
        {
            FramesPerSecond /= denominator;
            FullFrameTime /= denominator;
            MainThreadCPUFrameTime /= denominator;
            MainThreadCPUPresentWaitTime /= denominator;
            RenderThreadCPUFrameTime /= denominator;
            GPUFrameTime /= denominator;
        }

        internal void Min(FrameTimeSample other)
        {
            FramesPerSecond = Mathf.Min(FramesPerSecond, other.FramesPerSecond);
            FullFrameTime = Mathf.Min(FullFrameTime, other.FullFrameTime);
            MainThreadCPUFrameTime = Mathf.Min(MainThreadCPUFrameTime, other.MainThreadCPUFrameTime);
            MainThreadCPUPresentWaitTime = Mathf.Min(MainThreadCPUPresentWaitTime, other.MainThreadCPUPresentWaitTime);
            RenderThreadCPUFrameTime = Mathf.Min(RenderThreadCPUFrameTime, other.RenderThreadCPUFrameTime);
            GPUFrameTime = Mathf.Min(GPUFrameTime, other.GPUFrameTime);
        }

        internal void Max(FrameTimeSample other)
        {
            FramesPerSecond = Mathf.Max(FramesPerSecond, other.FramesPerSecond);
            FullFrameTime = Mathf.Max(FullFrameTime, other.FullFrameTime);
            MainThreadCPUFrameTime = Mathf.Max(MainThreadCPUFrameTime, other.MainThreadCPUFrameTime);
            MainThreadCPUPresentWaitTime = Mathf.Max(MainThreadCPUPresentWaitTime, other.MainThreadCPUPresentWaitTime);
            RenderThreadCPUFrameTime = Mathf.Max(RenderThreadCPUFrameTime, other.RenderThreadCPUFrameTime);
            GPUFrameTime = Mathf.Max(GPUFrameTime, other.GPUFrameTime);
        }
    };

    /// <summary>
    /// Proportional percentages between different bottleneck categories, representing the portion of
    /// samples classified into each bottleneck category over the Bottleneck History Window.
    /// </summary>
    struct BottleneckData
    {
        public float PresentLimited;
        public float CPU;
        public float GPU;
        public float Balanced;
    };

    BottleneckData m_BottleneckData;

    /// <summary>
    /// Container class for sample history with helpers to calculate min, max and average in one pass.
    /// </summary>
    class FrameTimeSampleHistory
    {
        List<FrameTimeSample> m_Samples = new();

        internal FrameTimeSample SampleAverage;
        internal FrameTimeSample SampleMin;
        internal FrameTimeSample SampleMax;

        internal void Add(FrameTimeSample sample)
        {
            m_Samples.Add(sample);
        }

        internal void ComputeAggregateValues()
        {
            FrameTimeSample average = new();
            FrameTimeSample min = new(float.MaxValue);
            FrameTimeSample max = new(float.MinValue);
            for (int i = 0; i < m_Samples.Count; i++)
            {
                var s = m_Samples[i];
                average.Add(s);
                min.Min(s);
                max.Max(s);
            }

            average.Divide(m_Samples.Count);

            SampleAverage = average;
            SampleMin = min;
            SampleMax = max;
        }

        internal void DiscardOldSamples(int sampleHistorySize)
        {
            while (m_Samples.Count >= sampleHistorySize)
                m_Samples.RemoveAt(0);
        }

        internal void Clear()
        {
            m_Samples.Clear();
        }
    }

    FrameTimeSampleHistory m_History = new();

    enum PerformanceBottleneck
    {
        Indeterminate,      // Cannot be determined
        PresentLimited,     // Limited by presentation (vsync or framerate cap)
        CPU,                // Limited by CPU (main and/or render thread)
        GPU,                // Limited by GPU
        Balanced,           // Limited by both CPU and GPU, i.e. well balanced
    }

    List<PerformanceBottleneck> m_BottleneckHistory = new();

    BottleneckData ComputeBottleneckStats()
    {
        var stats = new BottleneckData();
        for (int i = 0; i < m_BottleneckHistory.Count; i++)
        {
            switch (m_BottleneckHistory[i])
            {
                case PerformanceBottleneck.Balanced:
                    stats.Balanced++;
                    break;
                case PerformanceBottleneck.CPU:
                    stats.CPU++;
                    break;
                case PerformanceBottleneck.GPU:
                    stats.GPU++;
                    break;
                case PerformanceBottleneck.PresentLimited:
                    stats.PresentLimited++;
                    break;
            }
        }

        stats.Balanced /= m_BottleneckHistory.Count;
        stats.CPU /= m_BottleneckHistory.Count;
        stats.GPU /= m_BottleneckHistory.Count;
        stats.PresentLimited /= m_BottleneckHistory.Count;

        return stats;
    }

    static PerformanceBottleneck DetermineBottleneck(FrameTimeSample s)
    {
        const float kNearFullFrameTimeThresholdPercent = 0.2f;
        const float kNonZeroPresentWaitTimeMs = 0.5f;

        if (s.GPUFrameTime == 0 || s.MainThreadCPUFrameTime == 0) // In direct mode, render thread doesn't exist
            return PerformanceBottleneck.Indeterminate; // Missing data
        float fullFrameTimeWithMargin = (1f - kNearFullFrameTimeThresholdPercent) * s.FullFrameTime;

        // GPU time is close to frame time, CPU times are not
        if (s.GPUFrameTime              > fullFrameTimeWithMargin &&
            s.MainThreadCPUFrameTime    < fullFrameTimeWithMargin &&
            s.RenderThreadCPUFrameTime  < fullFrameTimeWithMargin)
            return PerformanceBottleneck.GPU;

        // One of the CPU times is close to frame time, GPU is not
        if (s.GPUFrameTime              < fullFrameTimeWithMargin &&
            (s.MainThreadCPUFrameTime   > fullFrameTimeWithMargin ||
             s.RenderThreadCPUFrameTime  > fullFrameTimeWithMargin))
            return PerformanceBottleneck.CPU;

        // Main thread waited due to Vsync or target frame rate
        if (s.MainThreadCPUPresentWaitTime > kNonZeroPresentWaitTimeMs)
        {
            // None of the times are close to frame time
            if (s.GPUFrameTime              < fullFrameTimeWithMargin &&
                s.MainThreadCPUFrameTime    < fullFrameTimeWithMargin &&
                s.RenderThreadCPUFrameTime  < fullFrameTimeWithMargin)
                return PerformanceBottleneck.PresentLimited;
        }

        return PerformanceBottleneck.Balanced;
    }
}
