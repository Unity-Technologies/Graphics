#define RTPROFILER_DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

// TODO: Prototype stuff, should not be a MonoBehaviour?
public class RealtimeProfilerModel : MonoBehaviour
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

    public struct FrameTimeSample
    {
        public float FullFrameTime;
        public float MainThreadCPUFrameTime;
        public float RenderThreadCPUFrameTime;
        public float GPUFrameTime;
    };

    List<FrameTimeSample> Samples = new List<FrameTimeSample>();

    public FrameTimeSample AverageSample = new FrameTimeSample();

    // Contains proportional values [0, 1] that describe the amount of samples classified to each
    // category over the size of the bottleneck history. Indeterminate values are discarded.
    public struct BottleneckStat
    {
        public float PresentLimited;
        public float CPU;
        public float GPU;
        public float Balanced;
    };

    public BottleneckStat BottleneckStats;

    FrameTiming[] m_Timing = new FrameTiming[1];

    public int HistorySize { get; set; } = 1;

    enum PerformanceBottleneck
    {
        Indeterminate,      // Cannot be determined
        PresentLimited,     // Limited by presentation (vsync or framerate cap)
        CPU,                // Limited by CPU (main and/or render thread)
        GPU,                // Limited by GPU
        Balanced,           // Limited by both CPU and GPU, i.e. well balanced
    }

    List<PerformanceBottleneck> m_BottleneckHistory = new List<PerformanceBottleneck>();

    public int BottleneckHistorySize { get; set; } = 100;

    void Update()
    {
        FrameTimingManager.CaptureFrameTimings();
        FrameTimingManager.GetLatestTimings(1, m_Timing);

        while (Samples.Count >= HistorySize)
            Samples.RemoveAt(0);

        FrameTimeSample frameTime = new FrameTimeSample();

        frameTime.FullFrameTime                 = (float)m_Timing.First().cpuFrameTime;
        frameTime.MainThreadCPUFrameTime        = (float)m_Timing.First().mainThreadCpuFrameTime;
        frameTime.RenderThreadCPUFrameTime      = (float)m_Timing.First().renderThreadCpuFrameTime;
        frameTime.GPUFrameTime                  = (float)m_Timing.First().gpuFrameTime;

        Samples.Add(frameTime);

        ComputeAverages();
        var bottleneck = DetermineBottleneck(AverageSample);

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
        AverageSample.FullFrameTime               = Samples.Average(s => s.FullFrameTime);
        AverageSample.MainThreadCPUFrameTime      = Samples.Average(s => s.MainThreadCPUFrameTime);
        AverageSample.RenderThreadCPUFrameTime    = Samples.Average(s => s.RenderThreadCPUFrameTime);
        AverageSample.GPUFrameTime                = Samples.Average(s => s.GPUFrameTime);
    }

    BottleneckStat ComputeBottleneckStats()
    {
        var stats = new BottleneckStat();
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
        if (s.GPUFrameTime == 0 || s.MainThreadCPUFrameTime == 0 || s.RenderThreadCPUFrameTime == 0)
            return PerformanceBottleneck.Indeterminate; // Missing data

        const float balancedThreshold = 0.1f;
        float fullFrameTimeWithMargin = (1f - balancedThreshold) * s.FullFrameTime;

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

        // None of the times are close to frame time
        if (s.GPUFrameTime              < fullFrameTimeWithMargin &&
            s.MainThreadCPUFrameTime    < fullFrameTimeWithMargin &&
            s.RenderThreadCPUFrameTime  < fullFrameTimeWithMargin)
            return PerformanceBottleneck.PresentLimited;

        return PerformanceBottleneck.Balanced;
    }
}
