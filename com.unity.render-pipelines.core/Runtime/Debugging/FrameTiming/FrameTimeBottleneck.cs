using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Represents a system bottleneck, meaning the factor that is most dominant in determining
    /// the total frame time.
    /// </summary>
    internal enum PerformanceBottleneck
    {
        Indeterminate,      // Cannot be determined
        PresentLimited,     // Limited by presentation (vsync or framerate cap)
        CPU,                // Limited by CPU (main and/or render thread)
        GPU,                // Limited by GPU
        Balanced,           // Limited by both CPU and GPU, i.e. well balanced
    }

    /// <summary>
    /// BottleneckHistogram represents the distribution of bottlenecks over the Bottleneck History Window,
    /// the size of which is determined by <see cref="DebugFrameTiming.bottleneckHistorySize"/>.
    /// </summary>
    internal struct BottleneckHistogram
    {
        internal float PresentLimited;
        internal float CPU;
        internal float GPU;
        internal float Balanced;
    };

    /// <summary>
    /// Container class for bottleneck history with helper to calculate histogram.
    /// </summary>
    internal class BottleneckHistory
    {
        public BottleneckHistory(int initialCapacity)
        {
            m_Bottlenecks.Capacity = initialCapacity;
        }

        List<PerformanceBottleneck> m_Bottlenecks = new();

        internal BottleneckHistogram Histogram;

        internal void DiscardOldSamples(int historySize)
        {
            Debug.Assert(historySize > 0, "Invalid sampleHistorySize");

            while (m_Bottlenecks.Count >= historySize)
                m_Bottlenecks.RemoveAt(0);

            m_Bottlenecks.Capacity = historySize;
        }

        internal void AddBottleneckFromAveragedSample(FrameTimeSample frameHistorySampleAverage)
        {
            var bottleneck = DetermineBottleneck(frameHistorySampleAverage);
            m_Bottlenecks.Add(bottleneck);
        }

        internal void ComputeHistogram()
        {
            var stats = new BottleneckHistogram();
            for (int i = 0; i < m_Bottlenecks.Count; i++)
            {
                switch (m_Bottlenecks[i])
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

            stats.Balanced /= m_Bottlenecks.Count;
            stats.CPU /= m_Bottlenecks.Count;
            stats.GPU /= m_Bottlenecks.Count;
            stats.PresentLimited /= m_Bottlenecks.Count;

            Histogram = stats;
        }

        static PerformanceBottleneck DetermineBottleneck(FrameTimeSample s)
        {
            const float kNearFullFrameTimeThresholdPercent = 0.2f;
            const float kNonZeroPresentWaitTimeMs = 0.5f;

            if (s.GPUFrameTime == 0 || s.MainThreadCPUFrameTime == 0) // In direct mode, render thread doesn't exist
                return PerformanceBottleneck.Indeterminate; // Missing data
            float fullFrameTimeWithMargin = (1f - kNearFullFrameTimeThresholdPercent) * s.FullFrameTime;

            // GPU time is close to frame time, CPU times are not
            if (s.GPUFrameTime > fullFrameTimeWithMargin &&
                s.MainThreadCPUFrameTime < fullFrameTimeWithMargin &&
                s.RenderThreadCPUFrameTime < fullFrameTimeWithMargin)
                return PerformanceBottleneck.GPU;

            // One of the CPU times is close to frame time, GPU is not
            if (s.GPUFrameTime < fullFrameTimeWithMargin &&
                (s.MainThreadCPUFrameTime > fullFrameTimeWithMargin ||
                 s.RenderThreadCPUFrameTime > fullFrameTimeWithMargin))
                return PerformanceBottleneck.CPU;

            // Main thread waited due to Vsync or target frame rate
            if (s.MainThreadCPUPresentWaitTime > kNonZeroPresentWaitTimeMs)
            {
                // None of the times are close to frame time
                if (s.GPUFrameTime < fullFrameTimeWithMargin &&
                    s.MainThreadCPUFrameTime < fullFrameTimeWithMargin &&
                    s.RenderThreadCPUFrameTime < fullFrameTimeWithMargin)
                    return PerformanceBottleneck.PresentLimited;
            }

            return PerformanceBottleneck.Balanced;
        }

        internal void Clear()
        {
            m_Bottlenecks.Clear();
            Histogram = new BottleneckHistogram();
        }
    }
}
