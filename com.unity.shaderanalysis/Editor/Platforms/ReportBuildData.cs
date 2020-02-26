using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.ShaderAnalysis.Internal;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.ShaderAnalysis
{
    /// <summary>Derive from this class to implement the processing pipeline to produce a Shader Report.</summary>
    public abstract class ReportBuildData : IDisposable
    {
        /// <summary>Represent a unit of compilation.</summary>
        public class CompileUnit
        {
            /// <summary>Path to the source code to compile.</summary>
            public FileInfo sourceCodeFile;
            /// <summary>Options to use to compile.</summary>
            public ShaderCompilerOptions compileOptions;
            /// <summary>Path to the generated binary.</summary>
            public FileInfo compiledFile;
            /// <summary>Shader target to compile against.</summary>
            public ShaderTarget compileTarget;
            /// <summary>Shader profile to use during compilation.</summary>
            public ShaderProfile compileProfile;
            /// <summary>Warnings that occured during the compilation.</summary>
            public List<ShaderBuildReport.LogItem> warnings = new List<ShaderBuildReport.LogItem>();
            /// <summary>Errors that occured during the compilation.</summary>
            public List<ShaderBuildReport.LogItem> errors = new List<ShaderBuildReport.LogItem>();
        }

        /// <summary>A performance report.</summary>
        public class PerfReport
        {
            /// <summary>Path to the source code to compile.</summary>
            public FileInfo compiledfile;
            /// <summary>Path to the raw report file.</summary>
            public FileInfo rawReportFile;
            /// <summary>Content of the raw report.</summary>
            public string rawReport;
            /// <summary>Metrics of the report.</summary>
            public ShaderBuildReport.ProgramPerformanceMetrics parsedReport;
        }

        Dictionary<ShaderCompiler.CompileOperation, int> m_CompileJobMap;

        // Inputs
        /// <summary>A temporary that can be used during the operation.</summary>
        protected DirectoryInfo temporaryDirectory { get; }
        /// <summary>Use this member to notify progress of the operation.</summary>
        protected ProgressWrapper progress { get; }

        // Outputs
        /// <summary>The compilation units to compile to make the report.</summary>
        List<CompileUnit> compileUnits { get; } = new List<CompileUnit>();
        /// <summary>The performance report that are generated.</summary>
        List<PerfReport> perfReports { get; } = new List<PerfReport>();

        protected Dictionary<ShaderCompiler.IShaderPerformanceAnalysis, int> m_PerfJobMap;
        protected ShaderBuildReport m_Report = null;

        /// <summary>Number of compile units.</summary>
        protected int compileUnitCount => compileUnits.Count;
        /// <summary>Number of performance reports.</summary>
        protected int perfReportCount => perfReports.Count;

        public ShaderBuildReport report => m_Report;

        public ShaderProgramFilter filter { get; }

        public bool throwOnError { get; set; }
        public bool logCommandLine { get; set; }

        protected ReportBuildData(
            DirectoryInfo temporaryDirectory,
            ProgressWrapper progress,
            ShaderProgramFilter filter)
        {
            this.temporaryDirectory = temporaryDirectory;
            this.progress = progress;
            this.filter = filter;
        }

        /// <summary>Implement this to export the performance report.</summary>
        public abstract IEnumerator ExportBuildReport();

        /// <summary>Implement this to generate the performance report.</summary>
        public abstract IEnumerator GeneratePerformanceReports();

        /// <summary>Implement this to compile the CompileUnit.</summary>
        public abstract IEnumerator CompileCompileUnits();

        /// <summary>Tick this enumerator to build the performance units.</summary>
        public IEnumerator BuildPerformanceUnits()
        {
            perfReports.Clear();

            var c = compileUnits.Count;
            var s = 1f / Mathf.Max(1, c - 1);
            for (var i = 0; i < c; i++)
            {
                var unit = compileUnits[i];
                progress.SetNormalizedProgress(s * i, "Building performance unit {0:D3} / {1:D3}", i + 1, c);

                var perf = new PerfReport
                {
                    compiledfile = unit.compiledFile,
                    rawReportFile = ShaderAnalysisUtils.GetTemporaryPerformanceReportFile(unit.compiledFile)
                };

                perfReports.Add(perf);

                yield return null;
            }
        }

        public void Dispose()
        {
            if (m_CompileJobMap != null)
            {
                foreach (var job in m_CompileJobMap)
                    job.Key.Cancel();

                m_CompileJobMap.Clear();
                m_CompileJobMap = null;
            }

            if (m_PerfJobMap != null)
            {
                foreach (var job in m_PerfJobMap)
                    job.Key.Cancel();

                m_PerfJobMap.Clear();
                m_PerfJobMap = null;
            }
        }

        public void ClearOrCreateTemporaryDirectory()
        {
            Assert.IsNotNull(temporaryDirectory);

            if (temporaryDirectory.Exists)
            {
                foreach (var fileInfo in temporaryDirectory.GetFiles())
                    fileInfo.Delete();
            }
            if (!temporaryDirectory.Exists)
                temporaryDirectory.Create();
        }

        /// <summary>Tick this enumerator to compile the compile units.</summary>
        /// <param name="compiler">The compiler to use.</param>
        /// <returns>An enumerator to tick.</returns>
        protected IEnumerator CompileCompileUnits_Internal(ShaderCompiler compiler)
        {
            const int k_MaxParallelCompilation = 8;

            compiler.Initialize();

            m_CompileJobMap = new Dictionary<ShaderCompiler.CompileOperation, int>();

            var compileUnitToProcess = new List<(CompileUnit, int)>();
            compileUnitToProcess.AddRange(compileUnits.Select((v, i) => (v, i)));

            var jobMapBuffer = new List<KeyValuePair<ShaderCompiler.CompileOperation, int>>();
            var totalCompileUnit = compileUnitToProcess.Count;
            var totalCompileUnitInverse = 1f / Mathf.Max(1, totalCompileUnit - 1);
            var compiledUnitCount = 0;
            while (m_CompileJobMap.Count > 0 || compileUnitToProcess.Count > 0)
            {
                // Try to enqueue jobs
                while (m_CompileJobMap.Count < k_MaxParallelCompilation && compileUnitToProcess.Count > 0)
                {
                    var (unit, unitIndex) = compileUnitToProcess[0];
                    compileUnitToProcess.RemoveAt(0);
                    var job = compiler.Compile(unit.sourceCodeFile, temporaryDirectory, unit.compiledFile, unit.compileOptions, unit.compileProfile, unit.compileTarget);
                    if (logCommandLine)
                        Debug.Log($"[ShaderAnalysis][Compile CL] {job.commandLine}");
                    m_CompileJobMap[job] = unitIndex;
                }

                // Wait for job completions
                {
                    progress.SetNormalizedProgress(totalCompileUnitInverse * compiledUnitCount, "Compiling units {0:D3} / {1:D3}", compiledUnitCount, totalCompileUnit);

                    jobMapBuffer.Clear();
                    jobMapBuffer.AddRange(m_CompileJobMap);
                    foreach (var job in jobMapBuffer)
                    {
                        if (job.Key.isComplete)
                        {
                            m_CompileJobMap.Remove(job.Key);
                            var unit = compileUnits[job.Value];

                            var compiled = true;

                            if (!unit.compiledFile.Exists)
                            {
                                if (throwOnError)
                                    throw new Exception(
                                        $"Failed to compile {unit.sourceCodeFile}, relaunching compile job, reason: {job.Key.errors}");

                                Debug.LogWarningFormat("Failed to compile {0}, relaunching compile job, reason: {1}", unit.sourceCodeFile, job.Key.errors);
                                var retryJob = compiler.Compile(unit.sourceCodeFile, temporaryDirectory, unit.compiledFile, unit.compileOptions, unit.compileProfile, unit.compileTarget);
                                m_CompileJobMap[retryJob] = job.Value;
                                compiled = false;
                            }
                            else if (!string.IsNullOrEmpty(job.Key.errors))
                                ParseShaderCompileErrors(job.Key.errors, unit.warnings, unit.errors);

                            if (compiled)
                                ++compiledUnitCount;
                        }
                    }
                    yield return null;
                }
            }
        }

        /// <summary>Clear the compilation units</summary>
        protected void ClearCompileUnits() => compileUnits.Clear();

        /// <summary>Add a comilation unit.</summary>
        /// <param name="unit">The unit to add.</param>
        protected void AddCompileUnit(CompileUnit unit) => compileUnits.Add(unit);

        /// <summary>Get a compile unit.</summary>
        /// <param name="index">Index of the compile unit.</param>
        /// <returns>The compile unit at that index.</returns>
        /// <exception cref="IndexOutOfRangeException">when <paramref name="index"/> is out of range.</exception>
        protected CompileUnit GetCompileUnitAt(int index) => compileUnits[index];

        /// <summary>Add a performance report.</summary>
        /// <param name="value">The report to add.</param>
        protected void AddPerfReport(PerfReport value) => perfReports.Add(value);

        /// <summary>Get a performance report.</summary>
        /// <param name="index">Index of the performance report.</param>
        /// <returns>The performance report at that index.</returns>
        /// <exception cref="IndexOutOfRangeException">when <paramref name="index"/> is out of range.</exception>
        protected PerfReport GetPerfReportAt(int index) => perfReports[index];

        /// <summary>Build the <see cref="ShaderBuildReport.DefineSet"/> from <paramref name="defines"/></summary>
        /// <param name="defines">the define to parse.</param>
        /// <returns>An array of <see cref="ShaderBuildReport.DefineSet"/>.</returns>
        protected ShaderBuildReport.DefineSet[] DefineSetFromHashSets(List<HashSet<string>> defines)
        {
            var result = new ShaderBuildReport.DefineSet[defines.Count];
            for (var i = 0; i < defines.Count; i++)
                result[i] = new ShaderBuildReport.DefineSet(defines[i]);

            return result;
        }

        static void ParseShaderCompileErrors(
            string error,
            List<ShaderBuildReport.LogItem> warnings,
            List<ShaderBuildReport.LogItem> errors
        )
        {
            var logLines = new List<string>();
            var log = new ShaderBuildReport.LogItem();
            logLines.Clear();

            var lines = error.Split('\n', '\r');

            if (lines.Length > 0 && lines[0].Contains("error"))
            {
                errors.Add(new ShaderBuildReport.LogItem
                {
                    message = error,
                    stacktrace = new string[0]
                });
                return;
            }


            for (var i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i]))
                    continue;

                logLines.Add(lines[i]);

                if (i < lines.Length - 1 && lines[i + 1].Length > 0 && lines[i + 1][0] == '('
                    || i == lines.Length)
                {
                    log.stacktrace = logLines.ToArray();

                    var messageLine = log.stacktrace[log.stacktrace.Length - 3];
                    var sep = messageLine.IndexOf(':');
                    log.message = sep != -1
                        ? messageLine.Substring(sep)
                        : string.Empty;

                    if (log.message.Contains("warning"))
                        warnings.Add(log);
                    else if (log.message.Contains("error"))
                        errors.Add(log);

                    logLines.Clear();
                    log = new ShaderBuildReport.LogItem();
                }
            }
        }
    }
}
