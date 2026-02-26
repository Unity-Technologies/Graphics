using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// Utility class for analyzing Asset Import Worker log files to verify which workers
    /// are processing specific asset import requests.
    /// </summary>
    public static class WorkerLogAnalyzer
    {
        /// <summary>
        /// Represents a snapshot of import counts across all Asset Import Workers for a specific asset.
        /// </summary>
        public class WorkerLogSnapshot
        {
            public Dictionary<int, int> WorkerImportCounts { get; private set; } = new Dictionary<int, int>();

            public void AddWorkerImportCount(int workerId, int count)
            {
                WorkerImportCounts[workerId] = count;
            }

            /// <summary>
            /// Calculates the difference between this snapshot and a baseline snapshot,
            /// returning only the new imports that occurred since the baseline.
            /// </summary>
            /// <param name="baseline">The baseline snapshot to compare against</param>
            /// <returns>A new snapshot containing only the import differences</returns>
            public WorkerLogSnapshot GetDifference(WorkerLogSnapshot baseline)
            {
                var diff = new WorkerLogSnapshot();
                foreach (var kvp in WorkerImportCounts)
                {
                    int baselineCount = baseline.WorkerImportCounts.GetValueOrDefault(kvp.Key, 0);
                    int newImports = kvp.Value - baselineCount;
                    if (newImports > 0)
                    {
                        diff.AddWorkerImportCount(kvp.Key, newImports);
                    }
                }
                return diff;
            }

            /// <summary>
            /// Gets a summary string of the worker import counts for debugging.
            /// </summary>
            public string GetSummary()
            {
                if (WorkerImportCounts.Count == 0)
                    return "No workers found";

                return string.Join(", ", WorkerImportCounts.Select(kvp => $"Worker{kvp.Key}:{kvp.Value}"));
            }
        }

        /// <summary>
        /// Creates a snapshot of the current import counts for the specified asset across all Asset Import Workers.
        /// </summary>
        /// <param name="assetPath">The asset path to look for in the worker logs</param>
        /// <returns>A snapshot of current import counts</returns>
        public static WorkerLogSnapshot SnapshotWorkerLogs(string assetPath)
        {
            var snapshot = new WorkerLogSnapshot();
            var logsDirectory = Path.Combine(Application.dataPath, "../Logs");

            if (!Directory.Exists(logsDirectory))
            {
                Debug.LogWarning($"Logs directory not found at: {logsDirectory}");
                return snapshot;
            }

            var logFiles = Directory.GetFiles(logsDirectory, "AssetImportWorkerHW*.log")
                .Where(f => !Path.GetFileName(f).Contains("-prev")) // Ignore previous run logs
                .ToArray();

            Debug.Log($"Found {logFiles.Length} current worker log files in {logsDirectory}");

            foreach (var logFile in logFiles)
            {
                var workerId = ExtractWorkerIdFromFilename(logFile);
                if (workerId >= 0)
                {
                    int importCount = CountAssetImportsInLog(logFile, assetPath);
                    snapshot.AddWorkerImportCount(workerId, importCount);
                    Debug.Log($"Worker{workerId}: Found {importCount} imports for {assetPath}");
                }
            }

            return snapshot;
        }

        /// <summary>
        /// Extracts the worker ID from an Asset Import Worker log filename.
        /// </summary>
        /// <param name="logFilePath">Path to the log file</param>
        /// <returns>Worker ID, or -1 if not found</returns>
        private static int ExtractWorkerIdFromFilename(string logFilePath)
        {
            var filename = Path.GetFileName(logFilePath);
            var match = Regex.Match(filename, @"AssetImportWorkerHW(\d+)\.log");
            return match.Success ? int.Parse(match.Groups[1].Value) : -1;
        }

        /// <summary>
        /// Counts the number of import requests for the specified asset in a worker log file.
        /// Looks for the pattern:
        /// ========================================================================
        /// Received Import Request.
        ///   Time since last request: XX.XXXXXX seconds.
        ///   path: [assetPath]
        /// </summary>
        /// <param name="logFilePath">Path to the worker log file</param>
        /// <param name="assetPath">The asset path to search for</param>
        /// <returns>Number of import requests found</returns>
        private static int CountAssetImportsInLog(string logFilePath, string assetPath)
        {
            if (!File.Exists(logFilePath))
                return 0;

            int count = 0;
            const int maxRetries = 2;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    // Use FileStream with FileShare.ReadWrite to allow reading while the worker has the file open for writing
                    using (var fileStream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fileStream))
                    {
                        var lines = new List<string>();
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }

                        for (int i = 0; i < lines.Count; i++)
                        {
                            // Look for the "Received Import Request." line
                            if (lines[i].Contains("Received Import Request."))
                            {
                                // Check the next few lines for the path
                                for (int j = i + 1; j < Math.Min(i + 5, lines.Count); j++)
                                {
                                    if (lines[j].Contains($"Path: {assetPath}"))
                                    {
                                        count++;
                                        break; // Found the path, move to next import request
                                    }
                                }
                            }
                        }
                    }
                    break; // Success, exit retry loop
                }
                catch (IOException ex) when ((ex.Message.Contains("sharing violation") || ex.Message.Contains("being used by another process")) && attempt < maxRetries - 1)
                {
                    Debug.LogWarning($"Worker log {logFilePath} is currently in use by another process. Retrying...");
                    // Retry after a short delay - the worker might have just finished writing
                    System.Threading.Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to read worker log {logFilePath}{(attempt > 0 ? " after retry" : "")}: {ex.Message}");
                    break; // Non-retryable exception, exit loop
                }
            }

            return count;
        }

        /// <summary>
        /// Asserts that a single worker processed an import for the specified asset at least twice
        /// and that the expected number of imports were processed overall.
        /// </summary>
        /// <param name="importDifference">The difference snapshot showing new imports</param>
        /// <param name="assetPath">The asset path being imported</param>
        /// <param name="expectedImports">The expected number of imports</param>
        public static void AssertSingleWorkerUsedAtLeastTwice(WorkerLogSnapshot importDifference, string assetPath, int expectedImports)
        {
            var workersWithImports = importDifference.WorkerImportCounts.Where(kvp => kvp.Value > 0).ToList();

            Assert.IsTrue(workersWithImports.Count > 0, "Expect at least 1 worker");

            var maxImportsWorker = workersWithImports.OrderByDescending(kvp => kvp.Value).First();
            Assert.IsTrue(maxImportsWorker.Value > 1,
                $"Expected at least 2 imports on a single Worker for {assetPath}");

            var sumImports = workersWithImports.Sum(kvp => kvp.Value);
            Assert.AreEqual(expectedImports, sumImports,
                $"Expected a total of {expectedImports} imports for {assetPath}, but found {sumImports} imports across all workers");
        }
    }
}
