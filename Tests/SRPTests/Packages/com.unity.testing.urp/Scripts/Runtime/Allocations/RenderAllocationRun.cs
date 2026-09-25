using System;
using System.Collections.Generic;
using UnityEngine.Profiling;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.TestTools.Graphics.Allocations;

namespace UnityEngine.Rendering.Universal.Tests.Allocations
{
    /// <summary>
    /// The same warm-up and measurement contract as <see cref="AllocationDetector.Run"/>, but fed one
    /// iteration at a time. The measured work runs inside URP's render loop, so it cannot be called in
    /// a loop here; the caller drives renders and each one reports back through
    /// <see cref="OnRender"/>.
    ///
    /// A test sets <see cref="OnRender"/> as the measured work of an
    /// <see cref="AllocationMeasurementFeature"/>, then renders while
    /// <see cref="wantsMoreRenders"/> is true.
    /// </summary>
    sealed class RenderAllocationRun
    {
        readonly Action<RenderGraph, ContextContainer> m_Work;
        readonly AllocationResult m_Result;
        readonly List<int> m_Counts;

        // Everything the measured window touches is allocated once here. A closure or a recorder
        // fetched per frame would be counted as the allocation we are hunting for.
        readonly Action m_Invoke;
        Recorder m_Recorder;
        RenderGraph m_RenderGraph;
        ContextContainer m_FrameData;

        AllocationCapture m_Capture;
        AllocationLogWatcher m_Logs;
        bool m_OwnsLogs;
        bool m_Begun;

        int m_ConsecutiveClean;
        int m_WarmUpRenders;
        int m_UnsettledCaptureRenders;
        bool m_Warm;

        /// <summary>
        /// Record allocation callstacks for the whole run, as <see cref="AllocationDetector"/> does.
        /// </summary>
        public bool captureCallstacks = true;

        /// <summary>Watcher to report logs through, as on <see cref="AllocationDetector"/>.</summary>
        public AllocationLogWatcher logWatcher;

        /// <summary>Consecutive allocation-free frames that count as a steady state.</summary>
        public int warmUpConvergenceRuns = 2;

        /// <summary>Give up warming up after this many frames.</summary>
        public int warmUpMaxRenders = 32;

        /// <summary>Renders to measure once warm.</summary>
        public int measuredRenders = 30;

        public RenderAllocationRun(
            string label,
            string configuration,
            Action<RenderGraph, ContextContainer> work,
            int threshold = 0,
            AllocationThreadScope threadScope = AllocationThreadScope.MainThread)
        {
            m_Work = work ?? throw new ArgumentNullException(nameof(work));
            m_Invoke = InvokeWork;
            // No capacity hint: measuredRenders may still be assigned by the caller after construction.
            m_Counts = new List<int>();
            m_Result = new AllocationResult
            {
                label = label,
                configuration = configuration,
                threshold = threshold,
                threadScope = threadScope
            };
        }

        public bool wantsMoreRenders => !m_Warm
            ? m_WarmUpRenders < warmUpMaxRenders || m_UnsettledCaptureRenders < UnsettledCaptureRendersWanted
            : m_Counts.Count < measuredRenders;

        // Renders past an exhausted warm-up, run only to give the capture something to hold. Kept out of
        // the warm-up tally so they do not distort "every one of N renders allocated".
        int UnsettledCaptureRendersWanted =>
            m_Capture == null ? 0 : AllocationDetector.k_CaptureIterationsWhenUnsettled;

        public AllocationResult result
        {
            get
            {
                // Rebuilt only when a render has been added since the last read: this is called from
                // assertion paths more than once, and ToArray on every read would allocate each time.
                if (m_Result.counts.Length != m_Counts.Count)
                    m_Result.counts = m_Counts.ToArray();

                return m_Result;
            }
        }

        /// <summary>
        /// Arms capture and log watching. Call before the first render, so a one-off allocation is
        /// recorded the first time it happens rather than on a retry that may not reproduce it.
        /// </summary>
        public void Begin()
        {
            if (m_Begun)
                return;

            m_Begun = true;
            m_Recorder ??= AllocationDetector.GetVerifiedRecorder(m_Result.threadScope);

            // Created here, armed once warm-up is over: warm-up allocations are lazy initialisation and
            // pool growth, and tracing them buries the steady-state allocation being hunted.
            if (captureCallstacks)
                m_Capture = new AllocationCapture();

            m_OwnsLogs = logWatcher == null;
            m_Logs = logWatcher ?? new AllocationLogWatcher();
            if (m_OwnsLogs)
                m_Logs.Start();

            // Match AllocationDetector: no warm-up requested means measure from the first render rather
            // than spending one on warm-up.
            if (warmUpConvergenceRuns <= 0)
            {
                m_Warm = true;
                m_Result.warmUpIterations = 0;
            }
        }

        /// <summary>
        /// Disarms, and attaches the diagnosis if the run failed. Call after the last render.
        /// </summary>
        public void Finish()
        {
            if (m_Logs != null)
            {
                m_Result.loggedErrors = m_Logs.hasErrors;
                m_Result.loggedMessages = m_Logs.Describe();
                if (m_OwnsLogs)
                    m_Logs.Dispose();
                m_Logs = null;
            }

            if (m_Capture == null)
                return;

            // Disarm before reading: the trace file is only flushed and closed when the profiler's file
            // stream is disabled.
            m_Capture.Disarm();

            if (!result.passed)
                m_Result.capture = m_Capture.Send();

            m_Capture = null;
        }

        /// <summary>
        /// Called from inside the render frame, with the renderer's live render graph and frame data.
        /// </summary>
        public void OnRender(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!wantsMoreRenders)
                return;

            m_Recorder ??= AllocationDetector.GetVerifiedRecorder(m_Result.threadScope);
            m_RenderGraph = renderGraph;
            m_FrameData = frameData;

            if (!m_Warm && m_WarmUpRenders >= warmUpMaxRenders)
            {
                m_UnsettledCaptureRenders++;
                AllocationDetector.Measure(m_Recorder, m_Invoke);
                return;
            }

            var count = AllocationDetector.Measure(m_Recorder, m_Invoke);

            if (m_Warm)
            {
                m_Counts.Add(count);
                return;
            }

            m_WarmUpRenders++;
            m_Result.warmUpAttempts = m_WarmUpRenders;
            if (count > m_Result.threshold)
                m_Result.warmUpAllocations++;

            m_ConsecutiveClean = count <= m_Result.threshold ? m_ConsecutiveClean + 1 : 0;

            if (m_ConsecutiveClean < warmUpConvergenceRuns)
            {
                // Out of warm-up renders without settling, so there will be no measured ones to trace.
                // Arm now and let the caller drive a few more purely for the capture.
                if (m_WarmUpRenders >= warmUpMaxRenders)
                    m_Capture?.Arm();

                return;
            }

            m_Warm = true;
            m_Result.warmUpIterations = m_WarmUpRenders;
            m_Capture?.Arm();
        }

        void InvokeWork() => m_Work(m_RenderGraph, m_FrameData);
    }
}
