//#define RTPROFILER_DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

public class FrameTimingData
{
#if RTPROFILER_DEBUG
    ProfilerCounterValue<float> m_FullFrameTimeCounter              = new ProfilerCounterValue<float>(ProfilerCategory.Render, "Full Frame Time", ProfilerMarkerDataUnit.TimeNanoseconds);
    ProfilerCounterValue<float> m_MainThreadCPUFrameTimeCounter     = new ProfilerCounterValue<float>(ProfilerCategory.Render, "Main Thread CPU Frame Time", ProfilerMarkerDataUnit.TimeNanoseconds);
    ProfilerCounterValue<float> m_RenderThreadCPUFrameTimeCounter   = new ProfilerCounterValue<float>(ProfilerCategory.Render, "Render Thread CPU Frame Time", ProfilerMarkerDataUnit.TimeNanoseconds);
    ProfilerCounterValue<float> m_GPUFrameTimeCounter               = new ProfilerCounterValue<float>(ProfilerCategory.Render, "GPU Frame Time", ProfilerMarkerDataUnit.TimeNanoseconds);

    ProfilerCounterValue<float> m_AvgFullFrameTimeCounter              = new ProfilerCounterValue<float>(ProfilerCategory.Render, "Avg Full Frame Time", ProfilerMarkerDataUnit.TimeNanoseconds);
    ProfilerCounterValue<float> m_AvgMainThreadCPUFrameTimeCounter     = new ProfilerCounterValue<float>(ProfilerCategory.Render, "Avg Main Thread CPU Frame Time", ProfilerMarkerDataUnit.TimeNanoseconds);
    ProfilerCounterValue<float> m_AvgRenderThreadCPUFrameTimeCounter   = new ProfilerCounterValue<float>(ProfilerCategory.Render, "Avg Render Thread CPU Frame Time", ProfilerMarkerDataUnit.TimeNanoseconds);
    ProfilerCounterValue<float> m_AvgGPUFrameTimeCounter               = new ProfilerCounterValue<float>(ProfilerCategory.Render, "Avg GPU Frame Time", ProfilerMarkerDataUnit.TimeNanoseconds);

    ProfilerCounterValue<float> m_CPUBoundCounter           = new ProfilerCounterValue<float>(ProfilerCategory.Render, "CPU Bound", ProfilerMarkerDataUnit.Percent);
    ProfilerCounterValue<float> m_GPUBoundCounter           = new ProfilerCounterValue<float>(ProfilerCategory.Render, "GPU Bound", ProfilerMarkerDataUnit.Percent);
    ProfilerCounterValue<float> m_PresentLimitedCounter     = new ProfilerCounterValue<float>(ProfilerCategory.Render, "Present bound", ProfilerMarkerDataUnit.Percent);
    ProfilerCounterValue<float> m_BalancedCounter           = new ProfilerCounterValue<float>(ProfilerCategory.Render, "Balanced", ProfilerMarkerDataUnit.Percent);
#endif

    /// <summary>
    /// Represents timing data captured from a single frame.
    /// </summary>
    public struct FrameTimeSample
    {
        public float FramesPerSecond;
        public float FullFrameTime;
        public float MainThreadCPUFrameTime;
        public float MainThreadCPUPresentWaitTime;
        public float RenderThreadCPUFrameTime;
        public float GPUFrameTime;
    };

    /// <summary>
    /// Frame timing data representing averaged values over the Frame Time History Window.
    /// </summary>
    public FrameTimeSample SampleAverage;

    /// <summary>
    /// Frame timing data representing minimum values over the Frame Time History Window.
    /// </summary>
    public FrameTimeSample SampleMin;

    /// <summary>
    /// Frame timing data representing maximum values over the Frame Time History Window.
    /// </summary>
    public FrameTimeSample SampleMax;

    /// <summary>
    /// Size of the Frame Time History Window.
    /// </summary>
    public int HistorySize { get; set; } = 30;

    /// <summary>
    /// Proportional percentages between different bottleneck categories, representing the portion of
    /// samples classified into each bottleneck category over the Bottleneck History Window.
    /// </summary>
    public struct Bottlenecks
    {
        public float PresentLimited;
        public float CPU;
        public float GPU;
        public float Balanced;
    };

    /// <summary>
    /// See <see cref="Bottlenecks"/>
    /// </summary>
    public Bottlenecks BottleneckStats;

    /// <summary>
    /// Size of the Bottleneck History Window in number of samples.
    /// </summary>
    public int BottleneckHistorySize { get; set; } = 60;

    List<FrameTimeSample> m_Samples = new List<FrameTimeSample>();

    enum PerformanceBottleneck
    {
        Indeterminate,      // Cannot be determined
        PresentLimited,     // Limited by presentation (vsync or framerate cap)
        CPU,                // Limited by CPU (main and/or render thread)
        GPU,                // Limited by GPU
        Balanced,           // Limited by both CPU and GPU, i.e. well balanced
    }

    List<PerformanceBottleneck> m_BottleneckHistory = new List<PerformanceBottleneck>();

    /// <summary>
    /// Reset gathered data.
    /// </summary>
    public void Reset()
    {
        m_Samples.Clear();
        m_BottleneckHistory.Clear();
        SampleAverage = new FrameTimeSample();
        BottleneckStats = new Bottlenecks();
    }

    /// <summary>
    /// Update timing data from profiling counters.
    /// </summary>
    public void UpdateFrameTiming()
    {
        FrameTiming[] timing = new FrameTiming[1];
        FrameTimingManager.CaptureFrameTimings();
        FrameTimingManager.GetLatestTimings(1, timing);

        while (m_Samples.Count >= HistorySize)
            m_Samples.RemoveAt(0);

        FrameTimeSample frameTime = new FrameTimeSample();

        if (timing.Length > 0)
        {
            frameTime.FullFrameTime = (float)timing.First().cpuFrameTime;
            frameTime.FramesPerSecond = 1000f / frameTime.FullFrameTime;
            frameTime.MainThreadCPUFrameTime = (float)timing.First().cpuMainThreadFrameTime;
            frameTime.MainThreadCPUPresentWaitTime = (float)timing.First().cpuMainThreadPresentWaitTime;
            frameTime.RenderThreadCPUFrameTime = (float)timing.First().cpuRenderThreadFrameTime;
            frameTime.GPUFrameTime = (float)timing.First().gpuFrameTime;
        }

        m_Samples.Add(frameTime);

        ComputeAverages();
        var bottleneck = DetermineBottleneck(SampleAverage);

        while (m_BottleneckHistory.Count > BottleneckHistorySize)
        {
            m_BottleneckHistory.RemoveAt(0);
        }
        m_BottleneckHistory.Add(bottleneck);
        BottleneckStats = ComputeBottleneckStats();

        #if RTPROFILER_DEBUG
        const float msToNs = 1e6f;
        m_FullFrameTimeCounter.Value            = frameTime.FullFrameTime * msToNs;
        m_MainThreadCPUFrameTimeCounter.Value   = frameTime.MainThreadCPUFrameTime * msToNs;
        m_RenderThreadCPUFrameTimeCounter.Value = frameTime.RenderThreadCPUFrameTime * msToNs;
        m_GPUFrameTimeCounter.Value             = frameTime.GPUFrameTime * msToNs;

        m_AvgFullFrameTimeCounter.Value            = AverageSample.FullFrameTime * msToNs;
        m_AvgMainThreadCPUFrameTimeCounter.Value   = AverageSample.MainThreadCPUFrameTime * msToNs;
        m_AvgRenderThreadCPUFrameTimeCounter.Value = AverageSample.RenderThreadCPUFrameTime * msToNs;
        m_AvgGPUFrameTimeCounter.Value             = AverageSample.GPUFrameTime * msToNs;

        m_CPUBoundCounter.Value         = bottleneck == PerformanceBottleneck.CPU ? 100f : 0f;
        m_GPUBoundCounter.Value         = bottleneck == PerformanceBottleneck.GPU ? 100f : 0f;
        m_PresentLimitedCounter.Value   = bottleneck == PerformanceBottleneck.PresentLimited ? 100f : 0f;
        m_BalancedCounter.Value         = bottleneck == PerformanceBottleneck.Balanced ? 100f : 0f;
        #endif
    }

    void ComputeAverages()
    {
        // TODO optimize

        SampleAverage.FramesPerSecond              = m_Samples.Average(s => s.FramesPerSecond);
        SampleAverage.FullFrameTime                = m_Samples.Average(s => s.FullFrameTime);
        SampleAverage.MainThreadCPUFrameTime       = m_Samples.Average(s => s.MainThreadCPUFrameTime);
        SampleAverage.MainThreadCPUPresentWaitTime = m_Samples.Average(s => s.MainThreadCPUPresentWaitTime);
        SampleAverage.RenderThreadCPUFrameTime     = m_Samples.Average(s => s.RenderThreadCPUFrameTime);
        SampleAverage.GPUFrameTime                 = m_Samples.Average(s => s.GPUFrameTime);

        SampleMin.FramesPerSecond              = m_Samples.Min(s => s.FramesPerSecond);
        SampleMin.FullFrameTime                = m_Samples.Min(s => s.FullFrameTime);
        SampleMin.MainThreadCPUFrameTime       = m_Samples.Min(s => s.MainThreadCPUFrameTime);
        SampleMin.MainThreadCPUPresentWaitTime = m_Samples.Min(s => s.MainThreadCPUPresentWaitTime);
        SampleMin.RenderThreadCPUFrameTime     = m_Samples.Min(s => s.RenderThreadCPUFrameTime);
        SampleMin.GPUFrameTime                 = m_Samples.Min(s => s.GPUFrameTime);

        SampleMax.FramesPerSecond              = m_Samples.Max(s => s.FramesPerSecond);
        SampleMax.FullFrameTime                = m_Samples.Max(s => s.FullFrameTime);
        SampleMax.MainThreadCPUFrameTime       = m_Samples.Max(s => s.MainThreadCPUFrameTime);
        SampleMax.MainThreadCPUPresentWaitTime = m_Samples.Max(s => s.MainThreadCPUPresentWaitTime);
        SampleMax.RenderThreadCPUFrameTime     = m_Samples.Max(s => s.RenderThreadCPUFrameTime);
        SampleMax.GPUFrameTime                 = m_Samples.Max(s => s.GPUFrameTime);
    }

    Bottlenecks ComputeBottleneckStats()
    {
        var stats = new Bottlenecks();
        m_BottleneckHistory.ForEach((PerformanceBottleneck bottleneck) =>
        {
            switch (bottleneck)
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
        });

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
