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

    FrameTiming[] m_Timing = new FrameTiming[1];

    public int HistorySize { get; set; } = 1;

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

#endif
    }

    void ComputeAverages()
    {
        AverageSample.FullFrameTime               = Samples.Average(s => s.FullFrameTime);
        AverageSample.MainThreadCPUFrameTime      = Samples.Average(s => s.MainThreadCPUFrameTime);
        AverageSample.RenderThreadCPUFrameTime    = Samples.Average(s => s.RenderThreadCPUFrameTime);
        AverageSample.GPUFrameTime                = Samples.Average(s => s.GPUFrameTime);
    }
}
