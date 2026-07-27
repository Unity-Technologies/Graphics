using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// Utility class for managing Asset Import Workers in tests using the reliable AssetDatabase.GetWorkerStats() API.
    /// </summary>
    public static class ImportWorkerTestUtility
    {
        /// <summary>
        /// Waits for the desired number, or fewer, of Asset Import Workers to be operational.
        /// </summary>
        /// <description>
        /// This method uses the AssetDatabase.GetWorkerStats() API to reliably check the
        /// current operational worker count.
        /// This is intended for tests that need to *reduce* the number of active workers
        /// to ensure repeatability.
        /// Workers are spawned by the Editor on demand when there is import work to do, so
        /// raising the desired worker count in the absence of import activity will not
        /// immediately spawn new workers. If this is called after increasing the desired
        /// worker count, it will likely time out or produce an unstable test.
        /// Similarly, it is possible for workers to shut down due to inactivity, and so
        /// lowering the desired worker count can result in there being fewer operational
        /// workers than desired, including there being zero operational workers.
        /// </description>
        /// <param name="desiredWorkerCount">The maximum number of operational workers to wait for</param>
        /// <param name="timeoutSeconds">Maximum time to wait in seconds (default: 60)</param>
        public static void WaitForDesiredWorkerCountOrFewer(int desiredWorkerCount, float timeoutSeconds = 60f)
        {
            var startTime = EditorApplication.timeSinceStartup;

            Debug.Log($"Waiting for {desiredWorkerCount} (or fewer) operational worker(s) (timeout: {timeoutSeconds}s)...");

            while (EditorApplication.timeSinceStartup - startTime < timeoutSeconds)
            {
                var workerStats = AssetDatabase.GetWorkerStats();
                var elapsed = EditorApplication.timeSinceStartup - startTime;
                Debug.Log($"[{elapsed:F1}s] Currently {workerStats.operationalWorkerCount} operational worker(s) detected (target: {desiredWorkerCount})");

                if (workerStats.operationalWorkerCount <= desiredWorkerCount)
                {
                    Debug.Log($"✓ Desired worker count (or fewer) of {desiredWorkerCount} achieved after {elapsed:F1}s");
                    return;
                }

                Thread.Sleep(500);
            }
            // Timeout reached but desired worker count not achieved.
            // Do not fail the test here, just log a message.
            // The scheduler operates asynchronously, and worker counts can take a while to stabilize.
            // Tests should be designed to handle transient states.
            Debug.Log($"⚠ Timeout reached ({timeoutSeconds}s) while waiting for {desiredWorkerCount} worker(s). " +
                      $"Current operational worker count: {GetOperationalWorkerCount()}");
        }

        /// <summary>
        /// Gets the current worker statistics from the AssetDatabase.
        /// </summary>
        /// <returns>Current WorkerStats containing operational worker count and other statistics</returns>
        internal static UnityEditor.AssetDatabase.WorkerStats GetWorkerStats()
        {
            return AssetDatabase.GetWorkerStats();
        }

        /// <summary>
        /// Gets the current number of operational workers.
        /// </summary>
        /// <returns>Number of currently operational workers</returns>
        public static int GetOperationalWorkerCount()
        {
            return AssetDatabase.GetWorkerStats().operationalWorkerCount;
        }
    }
}
