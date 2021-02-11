#define RTPROFILER_DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

// TODO: Prototype stuff, should not be a MonoBehaviour?
public class RealtimeProfilerModel : MonoBehaviour
{
    #if RTPROFILER_DEBUG
    ProfilerCounterValue<float> m_FullFrameTimeCounter              = new ProfilerCounterValue<float>(ProfilerCategory.Render, "Full Frame Time", ProfilerMarkerDataUnit.TimeNanoseconds);
    ProfilerCounterValue<float> m_MainThreadCPUFrameTimeCounter     = new ProfilerCounterValue<float>(ProfilerCategory.Render, "Main Thread CPU Frame Time", ProfilerMarkerDataUnit.TimeNanoseconds);
    ProfilerCounterValue<float> m_RenderThreadCPUFrameTimeCounter   = new ProfilerCounterValue<float>(ProfilerCategory.Render, "Render Thread CPU Frame Time", ProfilerMarkerDataUnit.TimeNanoseconds);
    ProfilerCounterValue<float> m_GPUFrameTimeCounter               = new ProfilerCounterValue<float>(ProfilerCategory.Render, "GPU Frame Time", ProfilerMarkerDataUnit.TimeNanoseconds);
    #endif

    public struct FrameTimeSample
    {
        public float FullFrameTime;
        public float MainThreadCPUFrameTime;
        public float RenderThreadCPUFrameTime;
        public float GPUFrameTime;
    };

    public FrameTimeSample FrameTime { get; private set; }

    FrameTiming[] m_Timing = new FrameTiming[1];

    static float GetRecorderFrameAverage(ProfilerRecorder recorder)
    {
        var samplesCount = recorder.Count;
        if (samplesCount == 0)
            return 0;

        float r = 0;
        {
            var samples = new List<ProfilerRecorderSample>(samplesCount);
            recorder.CopyTo(samples);
            for (var i = 0; i < samplesCount; ++i)
                r += samples[i].Value;
            r /= samplesCount;
        }

        return r;
    }

    void OnEnable()
    {
        FrameTime = new FrameTimeSample();
    }

    void Update()
    {
        FrameTimingManager.CaptureFrameTimings();
        FrameTimingManager.GetLatestTimings(1, m_Timing);

        FrameTimeSample frameTime = FrameTime;

        frameTime.FullFrameTime                 = (float)m_Timing.First().cpuFrameTime;
        frameTime.MainThreadCPUFrameTime        = (float)m_Timing.First().mainThreadCpuFrameTime;
        frameTime.RenderThreadCPUFrameTime      = (float)m_Timing.First().renderThreadCpuFrameTime;
        frameTime.GPUFrameTime                  = (float)m_Timing.First().gpuFrameTime;

        FrameTime = frameTime;

        #if RTPROFILER_DEBUG
        const float msToNs = 1e6f;
        m_FullFrameTimeCounter.Value            = frameTime.FullFrameTime * msToNs;
        m_MainThreadCPUFrameTimeCounter.Value   = frameTime.MainThreadCPUFrameTime * msToNs;
        m_RenderThreadCPUFrameTimeCounter.Value = frameTime.RenderThreadCPUFrameTime * msToNs;
        m_GPUFrameTimeCounter.Value             = frameTime.GPUFrameTime * msToNs;
#endif
    }
}
