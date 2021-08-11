using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

public class FrameTimingData
{
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
    /// Frame timing data representing averaged values over the Frame Time History Window.
    /// </summary>
    public FrameTimeSample SampleAverage => m_History.SampleAverage;

    /// <summary>
    /// Frame timing data representing minimum values over the Frame Time History Window.
    /// </summary>
    public FrameTimeSample SampleMin => m_History.SampleMin;

    /// <summary>
    /// Frame timing data representing maximum values over the Frame Time History Window.
    /// </summary>
    public FrameTimeSample SampleMax => m_History.SampleMax;

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

    class FrameTimeSampleHistory
    {
        int numFrames = 30;
        List<FrameTimeSample> samples = new List<FrameTimeSample>();

        internal FrameTimeSample SampleAverage;
        internal FrameTimeSample SampleMin;
        internal FrameTimeSample SampleMax;

        internal void Add(FrameTimeSample sample)
        {
            samples.Add(sample);
        }

        internal void ComputeAggregateValues()
        {
            FrameTimeSample average = new();
            FrameTimeSample min = new(float.MaxValue);
            FrameTimeSample max = new(float.MinValue);
            for (int i = 0; i < samples.Count; i++)
            {
                var s = samples[i];
                average.Add(s);
                min.Min(s);
                max.Max(s);
            }

            average.Divide(samples.Count);

            SampleAverage = average;
            SampleMin = min;
            SampleMax = max;
        }

        internal void DiscardOldSamples()
        {
            while (samples.Count >= numFrames)
                samples.RemoveAt(0);
        }

        internal void Clear()
        {
            samples.Clear();
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

    List<PerformanceBottleneck> m_BottleneckHistory = new List<PerformanceBottleneck>();

    /// <summary>
    /// Reset gathered data.
    /// </summary>
    public void Reset()
    {
        m_History.Clear();
        m_BottleneckHistory.Clear();
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

        m_History.DiscardOldSamples();

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

        var bottleneck = DetermineBottleneck(SampleAverage);

        while (m_BottleneckHistory.Count > BottleneckHistorySize)
        {
            m_BottleneckHistory.RemoveAt(0);
        }

        m_BottleneckHistory.Add(bottleneck);
        BottleneckStats = ComputeBottleneckStats();
    }

    Bottlenecks ComputeBottleneckStats()
    {
        var stats = new Bottlenecks();
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
