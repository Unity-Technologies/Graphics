using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Experimental.ShaderTools.Internal;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.Experimental.ShaderTools.PSSLInternal
{
    using Utils = EditorShaderPerformanceReportUtil;

    partial class BuildReportPS4JobAsync
    {
        abstract class ShaderBuildDataBase : IDisposable
        {
            internal class CompileUnit
            {
                internal FileInfo sourceCodeFile;
                internal ShaderCompilerOptions compileOptions;
                internal FileInfo compiledFile;
                internal ShaderProfile compileProfile;
                internal List<ShaderBuildReport.LogItem> warnings = new List<ShaderBuildReport.LogItem>();
                internal List<ShaderBuildReport.LogItem> errors = new List<ShaderBuildReport.LogItem>();
            }

            internal class PerfReport
            {
                internal FileInfo compiledfile;
                internal FileInfo rawReportFile;
                internal string rawReport;
                internal ShaderBuildReport.ProgramPerformanceMetrics parsedReport;
            }

            // Inputs
            internal readonly DirectoryInfo temporaryDirectory;
            internal readonly ProgressWrapper progress;

            // Outputs
            protected List<CompileUnit> m_CompileUnits = new List<CompileUnit>();
            protected List<PerfReport> m_PerfReports = new List<PerfReport>();

            Dictionary<PSSLShaderCompiler.ICompileOperation, int> m_CompileJobMap = null;
            Dictionary<PSSLShaderCompiler.IShaderPerformanceAnalysis, int> m_PerfJobMap = null;

            protected ShaderBuildReport m_Report = null;
            public ShaderBuildReport report { get { return m_Report; } }

            public ShaderBuildDataBase(
                DirectoryInfo temporaryDirectory,
                ProgressWrapper progress)
            {
                this.temporaryDirectory = temporaryDirectory;
                this.progress = progress;
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

            public IEnumerator CompileCompileUnits()
            {
                var compiler = new PSSLShaderCompiler();
                compiler.Initialize();

                m_CompileJobMap = new Dictionary<PSSLShaderCompiler.ICompileOperation, int>();

                var c = m_CompileUnits.Count;
                var s = 1f / Mathf.Max(1, c - 1);
                for (var i = 0; i < c; i++)
                {
                    var unit = m_CompileUnits[i];
                    var job = compiler.Compile(unit.sourceCodeFile, temporaryDirectory, unit.compiledFile, unit.compileOptions, unit.compileProfile);
                    m_CompileJobMap[job] = i;
                }

                var retries = 100;

                var jobMapBuffer = new List<KeyValuePair<PSSLShaderCompiler.ICompileOperation, int>>();
                while (m_CompileJobMap.Count > 0)
                {
                    var compiledUnits = c - m_CompileJobMap.Count;
                    progress.SetNormalizedProgress(s * compiledUnits, "Compiling units {0:D3} / {1:D3}", compiledUnits, c);

                    jobMapBuffer.Clear();
                    jobMapBuffer.AddRange(m_CompileJobMap);
                    foreach (var job in jobMapBuffer)
                    {
                        if (job.Key.isComplete)
                        {
                            m_CompileJobMap.Remove(job.Key);
                            var unit = m_CompileUnits[job.Value];

                            if (!unit.compiledFile.Exists)
                            {
                                --retries;
                                Debug.LogWarningFormat("Failed to compile {0}, relaunching compile job, reason: {1}", unit.sourceCodeFile, job.Key.errors);
                                var retryJob = compiler.Compile(unit.sourceCodeFile, temporaryDirectory, unit.compiledFile, unit.compileOptions, unit.compileProfile);
                                m_CompileJobMap[retryJob] = job.Value;
                            }
                            else if (!string.IsNullOrEmpty(job.Key.errors))
                                ParseShaderCompileErrors(job.Key.errors, unit.warnings, unit.errors);
                        }
                    }
                    yield return null;
                }
            }

            public IEnumerator BuildPerformanceUnits()
            {
                m_PerfReports.Clear();

                var c = m_CompileUnits.Count;
                var s = 1f / Mathf.Max(1, c - 1);
                for (var i = 0; i < c; i++)
                {
                    var unit = m_CompileUnits[i];
                    progress.SetNormalizedProgress(s * i, "Building performance unit {0:D3} / {1:D3}", i + 1, c);

                    var perf = new PerfReport
                    {
                        compiledfile = unit.compiledFile,
                        rawReportFile = Utils.GetTemporaryPerformanceReportFile(unit.compiledFile)
                    };

                    m_PerfReports.Add(perf);

                    yield return null;
                }
            }

            public IEnumerator GeneratePerformanceReports()
            {
                var compiler = new PSSLShaderCompiler();
                compiler.Initialize();

                m_PerfJobMap = new Dictionary<PSSLShaderCompiler.IShaderPerformanceAnalysis, int>();

                var c = m_PerfReports.Count;
                var s = 1f / Mathf.Max(1, c - 1);
                for (var i = 0; i < c; i++)
                {
                    var perf = m_PerfReports[i];
                    var job = compiler.PerformanceAnalysis(perf.compiledfile);
                    m_PerfJobMap[job] = i;
                }

                var jobMapBuffer = new List<KeyValuePair<PSSLShaderCompiler.IShaderPerformanceAnalysis, int>>();
                while (m_PerfJobMap.Count > 0)
                {
                    var processed = c - m_PerfJobMap.Count;
                    progress.SetNormalizedProgress(s * processed, "Generating Performance Reports {0:D3} / {1:D3}", processed, c);

                    jobMapBuffer.Clear();
                    jobMapBuffer.AddRange(m_PerfJobMap);
                    foreach (var job in jobMapBuffer)
                    {
                        if (job.Key.isComplete)
                        {
                            m_PerfJobMap.Remove(job.Key);
                            var perf = m_PerfReports[job.Value];

                            perf.rawReport = job.Key.report;

                            int successfulMatches, failedMatches;
                            perf.parsedReport = ParsePerformanceReport(job.Key.report, out successfulMatches, out failedMatches);

                            if (failedMatches > 0)
                            {
                                var retryJob = compiler.PerformanceAnalysis(perf.compiledfile);
                                m_PerfJobMap[retryJob] = job.Value;
                            }
                        }
                    }
                    yield return null;
                }
            }

            public IEnumerator ExportRawPerfReports()
            {
                var c = m_PerfReports.Count;
                var s = 1f / Mathf.Max(1, c - 1);
                for (var i = 0; i < c; i++)
                {
                    progress.SetNormalizedProgress(s * i, "Exporting Raw Performance Reports {0:D3} / {1:D3}", i + 1, c);

                    var perf = m_PerfReports[i];
                    File.WriteAllText(perf.rawReportFile.FullName, perf.rawReport);
                    yield return null;
                }
            }

            public abstract IEnumerator ExportBuildReport();

            protected void ClearCompileUnits()
            {
                m_CompileUnits.Clear();
            }

            protected void AddCompileUnit(CompileUnit unit)
            {
                m_CompileUnits.Add(unit);
            }

            protected ShaderBuildReport.DefineSet[] DefineSetFromHashSets(List<HashSet<string>> defines)
            {
                var result = new ShaderBuildReport.DefineSet[defines.Count];
                for (var i = 0; i < defines.Count; i++)
                    result[i] = new ShaderBuildReport.DefineSet(defines[i]);

                return result;
            }
        }

        static ShaderBuildReport.ProgramPerformanceMetrics ParsePerformanceReport(string operationReport, out int successfulMatches, out int failedMatches)
        {
            var result = new ShaderBuildReport.ProgramPerformanceMetrics();

            successfulMatches = 0;
            failedMatches = 0;

            Match m;
            if ((m = k_ShaderMicroCodeSize.Match(operationReport)).Success)
            {
                ++successfulMatches;
                result.microCodeSize = int.Parse(m.Groups[1].Value);
            }
            else
                ++failedMatches;

            if ((m = k_VGPRCount.Match(operationReport)).Success)
            {
                ++successfulMatches;
                result.VGPRCount = int.Parse(m.Groups[1].Value);
                result.VGPRUsedCount = int.Parse(m.Groups[2].Value);
            }
            else
                ++failedMatches;

            if ((m = k_SGPRCount.Match(operationReport)).Success)
            {
                ++successfulMatches;
                var a = int.Parse(m.Groups[1].Value);
                var b = int.Parse(m.Groups[2].Value);
                result.SGPRCount = a + b;
                result.SGPRUsedCount = int.Parse(m.Groups[3].Value);
            }
            else
                ++failedMatches;

            if ((m = k_UserSGPRCount.Match(operationReport)).Success)
            {
                ++successfulMatches;
                result.UserSGPRCount = int.Parse(m.Groups[1].Value);
            }
            else
                ++failedMatches;

            if ((m = k_LDSSize.Match(operationReport)).Success)
            {
                ++successfulMatches;
                result.LDSSize = int.Parse(m.Groups[1].Value);
            }
            else
                ++failedMatches;

            if ((m = k_ThreadGroupWaves.Match(operationReport)).Success)
            {
                ++successfulMatches;
                result.threadGroupWaves = int.Parse(m.Groups[1].Value);
            }
            else
                ++failedMatches;

            if ((m = k_CUOccupancy.Match(operationReport)).Success)
            {
                ++successfulMatches;
                result.CUOccupancyCount = int.Parse(m.Groups[1].Value);
                result.CUOccupancyMax = int.Parse(m.Groups[2].Value);
            }
            else
                ++failedMatches;

            if ((m = k_SIMDOccupancy.Match(operationReport)).Success)
            {
                ++successfulMatches;
                result.SIMDOccupancyCount = int.Parse(m.Groups[1].Value);
                result.SIMDOccupancyMax = int.Parse(m.Groups[2].Value);
            }
            else
                ++failedMatches;

            return result;
        }
    }
}
