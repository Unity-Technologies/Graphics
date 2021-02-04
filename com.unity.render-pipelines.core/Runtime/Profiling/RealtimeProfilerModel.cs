using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

// TODO: Prototype stuff, should not be a MonoBehaviour?
public class RealtimeProfilerModel : MonoBehaviour
{
    static GameObject s_Updater;
    internal static RealtimeProfilerModel GetOrCreateRuntimeInstance()
    {
        if (s_Updater == null)
        {
            s_Updater = new GameObject("RealtimeProfilerModelUpdater")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            s_Updater.AddComponent<RealtimeProfilerModel>();
        }

        return s_Updater.GetComponent<RealtimeProfilerModel>();
    }

    internal static void DestroyInstance()
    {
        if (s_Updater != null)
        {
            DestroyImmediate(s_Updater);
            s_Updater = null;
        }
    }

    public struct FrameTimeSample
    {
        public float FullFrameTime;
        public float LogicCPUFrameTime;
        public float CombinedCPUFrameTime;
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

        frameTime.FullFrameTime         = (float)m_Timing.First().cpuFrameTime;
        frameTime.LogicCPUFrameTime     = (float)m_Timing.First().logicCpuFrameTime;
        frameTime.CombinedCPUFrameTime  = (float)m_Timing.First().combinedCpuFrameTime;
        frameTime.GPUFrameTime          = (float)m_Timing.First().gpuFrameTime;

        FrameTime = frameTime;
    }
}
