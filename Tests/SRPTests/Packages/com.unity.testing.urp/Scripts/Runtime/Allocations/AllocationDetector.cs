using System;
using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering.Universal.Tests.Allocations
{
    /// <summary>
    /// Which threads' managed allocations a detector counts.
    /// </summary>
    public enum AllocationThreadScope
    {
        MainThread,
        AllThreads
    }

    /// <summary>
    /// Result of one detector run. Keeps the whole per-iteration vector so that "allocates every
    /// iteration", "allocates once at iteration 21" and "allocates on 2 of 30" stay distinguishable.
    /// </summary>
    public sealed class AllocationResult
    {
        public string label;
        public string configuration;
        public AllocationThreadScope threadScope;
        public int threshold;

        /// <summary>Iterations spent reaching a steady state, or -1 if it never converged.</summary>
        public int warmUpIterations = -1;

        /// <summary>Warm-up iterations run before giving up or converging.</summary>
        public int warmUpAttempts;

        /// <summary>How many of those warm-up iterations allocated.</summary>
        public int warmUpAllocations;

        /// <summary>Allocation count for each measured iteration, in order.</summary>
        public int[] counts = Array.Empty<int>();

        /// <summary>Where the profiler trace with allocation callstacks went, filled in on failure.</summary>
        public string capture;

        /// <summary>Distinct log messages seen during the run.</summary>
        public string loggedMessages;

        /// <summary>Whether anything worse than a warning was logged during the run.</summary>
        public bool loggedErrors;

        public bool converged => warmUpIterations >= 0;

        public bool passed
        {
            get
            {
                if (!converged || loggedErrors)
                    return false;

                // Nothing measured is not a pass. A detector that reads zero because it never ran looks
                // exactly like a clean one, which is the failure this suite exists to rule out.
                if (counts.Length == 0)
                    return false;

                for (var i = 0; i < counts.Length; i++)
                {
                    if (counts[i] > threshold)
                        return false;
                }

                return true;
            }
        }

        public string Report()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"URP allocation test failed: {label}");
            sb.AppendLine($"  Configuration: {configuration}");
            if (converged && loggedErrors)
            {
                sb.AppendLine($"  Verdict:       LOGGED AN ERROR. Allocations settled after {warmUpIterations} "
                    + "render(s), but an error was logged during the run, which allocates in itself.");
            }
            else if (converged)
            {
                sb.AppendLine($"  Warm-up:       settled after {warmUpIterations} render(s)");
            }
            else if (warmUpAllocations >= warmUpAttempts)
            {
                sb.AppendLine($"  Verdict:       ALLOCATES EVERY FRAME. Every one of {warmUpAttempts} renders "
                    + "allocated, so this never stops allocating.");
            }
            else
            {
                sb.AppendLine($"  Verdict:       ALLOCATES INTERMITTENTLY. {warmUpAllocations} of "
                    + $"{warmUpAttempts} renders allocated, and it never went clean for long enough to "
                    + "call it settled.");
            }
            sb.AppendLine($"  Threshold:     {threshold} per iteration ({threadScope})");
            sb.AppendLine($"  Platform:      {Application.platform}, {SystemInfo.graphicsDeviceType}, {SystemInfo.renderingThreadingMode}");

            if (counts.Length > 0)
            {
                var offenders = new StringBuilder();
                var total = 0;
                var offendingIterations = 0;
                for (var i = 0; i < counts.Length; i++)
                {
                    total += counts[i];
                    if (counts[i] <= threshold)
                        continue;

                    offendingIterations++;
                    if (offendingIterations <= 10)
                    {
                        if (offenders.Length > 0)
                            offenders.Append(", ");
                        offenders.Append($"{i} ({counts[i]})");
                    }
                }

                sb.AppendLine($"  Measured:      {counts.Length} iterations, {total} allocation(s) total");
                if (offendingIterations > 0)
                {
                    sb.AppendLine($"  Over threshold on {offendingIterations} iteration(s), index (count): {offenders}"
                        + (offendingIterations > 10 ? ", ..." : ""));
                }
            }

            if (!string.IsNullOrEmpty(loggedMessages))
                sb.Append(loggedMessages);

            if (!string.IsNullOrEmpty(capture))
                sb.Append(capture);
            else
                sb.Append("  Capture:       none was attempted.");

            return sb.ToString();
        }
    }

    /// <summary>
    /// Counts managed allocations across repeated invocations of an action.
    ///
    /// The detector warms up until it sees <see cref="warmUpConvergenceRuns"/> consecutive
    /// allocation-free iterations before it starts measuring, so lazily initialised state and pool
    /// growth are excluded by observation rather than by a fixed guess. Failing to converge is
    /// itself a result.
    /// </summary>
    public sealed class AllocationDetector
    {
        internal const string markerName = "URP_GC_Alloc_Detector";

        /// <summary>Armed iterations run for the trace when warm-up never settles.</summary>
        internal const int k_CaptureIterationsWhenUnsettled = 3;

        /// <summary>Consecutive allocation-free iterations that count as a steady state.</summary>
        public int warmUpConvergenceRuns = 2;

        /// <summary>Give up warming up after this many iterations.</summary>
        public int warmUpMaxIterations = 32;

        /// <summary>Iterations to measure once warm.</summary>
        public int measuredIterations = 30;

        /// <summary>Allocations permitted per iteration.</summary>
        public int threshold = 0;

        public AllocationThreadScope threadScope = AllocationThreadScope.MainThread;

        /// <summary>
        /// Record allocation callstacks for the whole run so a one-off allocation is captured the first
        /// time it happens rather than on a retry that may not reproduce it.
        /// </summary>
        public bool captureCallstacks = true;

        /// <summary>
        /// Watcher to report logs through. When null the run uses one of its own, which only covers the
        /// measured window; the test framework evaluates captured logs after the test body, so a
        /// fixture-lived watcher is what lets this report take precedence over the framework's.
        /// </summary>
        internal AllocationLogWatcher logWatcher;

        /// <summary>
        /// Skips the test unless the GC.Alloc recorder is usable. A recorder that reports zero because
        /// it is unavailable looks exactly like a passing test, so this is checked rather than assumed.
        /// The skip is deliberate rather than a failure: a player without the recorder cannot measure
        /// allocations at all, which is an environment fact and not a URP defect.
        /// </summary>
        public static Recorder GetVerifiedRecorder(AllocationThreadScope scope)
        {
            var recorder = Recorder.Get("GC.Alloc");
            if (!recorder.isValid)
            {
                Assert.Ignore("The GC.Alloc profiler recorder is not available here, so allocations cannot "
                    + "be measured. Reporting this as skipped rather than passing, because a detector that "
                    + "cannot see allocations would report zero no matter what the code does.");
            }

            if (scope == AllocationThreadScope.MainThread)
                recorder.FilterToCurrentThread();
            else
                recorder.CollectFromAllThreads();

            return recorder;
        }

        /// <summary>
        /// Runs the whole warm-up and measurement loop in place. Use this when the caller can call the
        /// work repeatedly on demand; when iterations are driven by rendered frames, use
        /// <see cref="FrameAllocationRun"/>, which feeds them in one at a time.
        /// </summary>
        public AllocationResult Run(string label, string configuration, Action iteration)
        {
            AllocationResult result = null;
            var frames = RunIncremental(label, configuration, iteration, r => result = r);
            while (frames.MoveNext())
            {
            }

            return result;
        }

        public IEnumerator RunIncremental(
            string label,
            string configuration,
            Action iteration,
            Action<AllocationResult> onResult)
        {
            if (iteration == null)
                throw new ArgumentNullException(nameof(iteration));

            var recorder = GetVerifiedRecorder(threadScope);
            var result = new AllocationResult
            {
                label = label,
                configuration = configuration,
                threadScope = threadScope,
                threshold = threshold
            };

            // Allocate the results buffer before the measured window opens.
            var counts = new int[measuredIterations];

            using var capture = captureCallstacks ? new AllocationCapture() : null;

            // Logging allocates, so a message repeating every iteration is usually the cause. Text works
            // on every platform, unlike the profiler trace.
            var logs = logWatcher;
            var ownsLogs = logs == null;
            if (ownsLogs)
            {
                logs = new AllocationLogWatcher();
                logs.Start();
            }

            try
            {
                var frames = MeasureFrames(result, counts, recorder, iteration, capture, logs);
                while (frames.MoveNext())
                    yield return frames.Current;

                onResult?.Invoke(result);
            }
            finally
            {
                if (ownsLogs)
                    logs.Dispose();
            }
        }

        IEnumerator MeasureFrames(
            AllocationResult result,
            int[] counts,
            Recorder recorder,
            Action iteration,
            AllocationCapture capture,
            AllocationLogWatcher logs)
        {
            if (warmUpConvergenceRuns <= 0)
            {
                // No warm-up requested: measure from the first iteration.
                result.warmUpIterations = 0;
            }
            else
            {
                var consecutiveClean = 0;
                for (var i = 0; i < warmUpMaxIterations && consecutiveClean < warmUpConvergenceRuns; i++)
                {
                    result.warmUpAttempts++;
                    if (Measure(recorder, iteration) <= result.threshold)
                    {
                        consecutiveClean++;
                    }
                    else
                    {
                        consecutiveClean = 0;
                        result.warmUpAllocations++;
                    }

                    if (consecutiveClean >= warmUpConvergenceRuns)
                        result.warmUpIterations = i + 1;

                    yield return null;
                }
            }

            // Armed after warm-up, not before it. Warm-up allocations are lazy initialisation and pool
            // growth, which nobody is going to fix, and tracing them buries the steady-state allocation
            // being hunted. Still armed before the first measured iteration, so a one-in-thirty
            // allocation is caught the first time it happens.
            capture?.Arm();

            if (result.converged)
            {
                for (var i = 0; i < counts.Length; i++)
                {
                    counts[i] = Measure(recorder, iteration);
                    yield return null;
                }

                result.counts = counts;
            }
            else if (capture != null)
            {
                // Never settled, so there are no measured iterations to trace. Run a few armed ones
                // purely so the repeating allocation appears in the capture.
                for (var i = 0; i < k_CaptureIterationsWhenUnsettled; i++)
                {
                    Measure(recorder, iteration);
                    yield return null;
                }
            }

            result.loggedErrors = logs.hasErrors;
            result.loggedMessages = logs.Describe();

            // Disarm first: the trace file is only flushed and closed when the profiler's file stream
            // is disabled, so sending while still armed reads a file that has not been written out.
            capture?.Disarm();

            if (!result.passed && capture != null)
                result.capture = capture.Send();
        }

        /// <summary>
        /// Measures one iteration. Stopping is what flushes the accumulator into the sample ring and
        /// clears it, so the count is only readable after the stop and one start/stop cycle gives
        /// exactly this iteration.
        /// </summary>
        internal static int Measure(Recorder recorder, Action iteration)
        {
            Profiler.BeginSample(markerName);
            try
            {
                recorder.enabled = true;
                iteration();
            }
            finally
            {
                recorder.enabled = false;
                Profiler.EndSample();
            }

            return recorder.sampleBlockCount;
        }
    }
}
