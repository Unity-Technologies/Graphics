using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Represents timing data captured from a single frame.
    /// </summary>
    internal struct FrameTimeSample
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
}
