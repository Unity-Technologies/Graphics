using System;
using System.IO;
using NUnit.Framework;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.Profiling;
using UnityEngine.TestTools.Graphics;

namespace UnityEngine.Rendering.Universal.Tests.Allocations
{
    /// <summary>
    /// Records managed allocation callstacks for the whole of a measurement run, and ships the trace as
    /// a test artifact if the run failed.
    ///
    /// Recording is armed before the first measured iteration rather than after a failure. Capturing
    /// after the fact only works for an allocation that happens every time; a one-in-thirty allocation
    /// will usually not reproduce in a retry, which is how the macOS ghost went uncaptured 0 for 5 in
    /// every observed occurrence. It is armed after warm-up, though, so lazy initialisation and pool
    /// growth stay out of the trace.
    ///
    /// Toggling the profiler per test overflowed the stack on Switch2 in PR #113099, so this arms once
    /// per run and disarms once. The same PR saw counts inflated on PS5 with the profiler on. That does
    /// not reproduce now: PS5 measures 30 iterations at zero allocations with capture armed.
    ///
    /// The trace goes to the UnifiedTestRunner host over the player connection, so a player needs
    /// nothing listening on the editor side. Stacks are resolved later by opening the trace in the
    /// Profiler; a player cannot resolve them itself.
    /// </summary>
    sealed class AllocationCapture : IDisposable
    {
        const string k_ArtifactDirectory = "GCAllocCaptures";

        // A truncated binary trace will not open, so an oversized one is declined rather than cut short.
        const long k_MaxArtifactBytes = 32L * 1024 * 1024;

        readonly string m_CapturePath;
        readonly string m_RawPath;
        bool m_Armed;

        public AllocationCapture()
        {
            // Probe for somewhere the platform will actually let us write. On Switch2 the temporary
            // cache resolves to host:/Temp and the profiler silently writes nothing there, so a fixed
            // choice is not safe. persistentDataPath is what the graphics framework uses for
            // device-side files, so it goes first.
            m_CapturePath = Path.Combine(FindWritableDirectory(), "urp_gcalloc_capture");
            m_RawPath = m_CapturePath + ".raw";
        }

        static string FindWritableDirectory()
        {
            foreach (var candidate in new[] { Application.persistentDataPath, Application.temporaryCachePath })
            {
                if (string.IsNullOrEmpty(candidate))
                    continue;

                try
                {
                    Directory.CreateDirectory(candidate);
                    var probe = Path.Combine(candidate, "urp_gcalloc_probe.tmp");
                    File.WriteAllBytes(probe, new byte[] { 0 });
                    File.Delete(probe);
                    return candidate;
                }
                catch (Exception)
                {
                    // Try the next candidate. Which one works is platform-specific.
                }
            }

            return Application.persistentDataPath;
        }

        /// <summary>
        /// Starts recording allocation callstacks. Call once, before the first measured iteration.
        /// </summary>
        public void Arm()
        {
            if (m_Armed)
                return;

            try
            {
                if (File.Exists(m_RawPath))
                    File.Delete(m_RawPath);
            }
            catch (IOException)
            {
                // A stale capture we cannot remove is not worth failing the test over.
            }

            Profiler.logFile = m_CapturePath;
            Profiler.enableBinaryLog = true;
            Profiler.enableAllocationCallstacks = true;
            Profiler.enabled = true;
            m_Armed = true;
        }

        /// <summary>
        /// Stops recording and closes the trace file. Must be called before <see cref="Send"/>: the
        /// file stream is only flushed and destroyed when the profiler's user file stream is disabled
        /// (Profiler::SetUserFileStreamEnabled), which is what clearing logFile and enableBinaryLog
        /// maps to. Reading the file while still armed races a stream that has not been written out.
        /// </summary>
        public void Disarm()
        {
            if (!m_Armed)
                return;

            Profiler.enabled = false;
            Profiler.enableAllocationCallstacks = false;
            Profiler.enableBinaryLog = false;
            Profiler.logFile = string.Empty;
            m_Armed = false;
        }

        public void Dispose() => Disarm();

        /// <summary>
        /// Sends the recorded trace as an artifact.
        /// </summary>
        /// <returns>A line describing where the trace went, or why there is none.</returns>
        public string Send()
        {
            if (!File.Exists(m_RawPath))
            {
                return $"  Capture:       the profiler wrote no trace to '{m_RawPath}', so there is nothing "
                    + "to attach. That path passed a write probe, so the profiler itself declined to "
                    + "write there.";
            }

            var fileName = $"{TestContext.CurrentContext.Test.Name.ToValidPath()}.gcalloc.raw";

            try
            {
                var size = new FileInfo(m_RawPath).Length;
                if (size > k_MaxArtifactBytes)
                {
                    return $"  Capture:       the trace is {size / (1024 * 1024)} MB, over the "
                        + $"{k_MaxArtifactBytes / (1024 * 1024)} MB artifact limit, so it was not sent. It "
                        + $"is at '{m_RawPath}' on the device if it can be pulled off by hand.";
                }

                var data = File.ReadAllBytes(m_RawPath);
                var message = new ArtifactFileMessage
                {
                    PathName = k_ArtifactDirectory,
                    FileName = fileName,
                    Data = data
                };

                PlayerConnection.instance.Send(ArtifactFileMessage.MessageId, message.Serialize());

                return $"  Capture:       allocation callstacks for the whole run are in the artifact "
                    + $"'{k_ArtifactDirectory}/{fileName}' ({data.Length / 1024} KB). Open it in the "
                    + "Profiler (Window > Analysis > Profiler > Load) and sort the CPU Hierarchy by "
                    + $"GC Alloc under the '{AllocationDetector.markerName}' marker.";
            }
            catch (IOException exception)
            {
                return $"  Capture:       a trace was recorded but could not be read back: {exception.Message}";
            }
        }
    }
}
