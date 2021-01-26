using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

// Prototype stuff, should not be a MonoBehaviour
public class ProfilerDataModel : MonoBehaviour
{
    public float CpuFrameTime { get; private set; }
    public float GpuFrameTime { get; private set; }

    // FrameTiming API does not work ATM
    //FrameTiming[] m_Timing = new FrameTiming[1];
    
    ProfilerRecorder m_FakeCpuTimeRecorder;
    ProfilerRecorder m_FakeGpuTimeRecorder;
    
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
        m_FakeCpuTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
        m_FakeGpuTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Render Thread", 15);
    }

    void OnDisable()
    {
        m_FakeCpuTimeRecorder.Dispose();
    }

    void Update()
    {
        // Example numbers for UI visualization - not actual data we want to display  
        CpuFrameTime = GetRecorderFrameAverage(m_FakeCpuTimeRecorder) * 1e-6f;
        GpuFrameTime = GetRecorderFrameAverage(m_FakeGpuTimeRecorder) * 1e-6f;

        // FrameTiming API does not work ATM
        //FrameTimingManager.CaptureFrameTimings();
        //FrameTimingManager.GetLatestTimings(1, m_Timing);
        //CpuFrameTime = m_Timing.First().cpuFrameTime;
        //GpuFrameTime = m_Timing.First().gpuFrameTime;
    }
}
