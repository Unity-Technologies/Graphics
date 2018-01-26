
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Experimental.ShaderTools.Internal;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.Experimental.ShaderTools.PSSLInternal
{
    using Utils = EditorShaderPerformanceReportUtil;

    partial class BuildReportPS4JobAsync
    {
        public Shader shader { get; private set; }

        public BuildReportPS4JobAsync(BuildTarget target, Shader shader)
            : base(target)
        {
            this.shader = shader;
            m_Compiler.Initialize();
        }

        IEnumerator DoTick_Shader()
        {
            IEnumerator e = null;

            var temporaryDirectory = Utils.GetTemporaryDirectory(shader, BuildTarget.PS4);

            e = DoTick_Shader_Internal(null, temporaryDirectory, null);

            while (e.MoveNext()) yield return null;
            if (m_Cancelled) yield break;

            SetProgress(1, "Completed");
        }

        class ShaderBuildData : ShaderBuildDataBase
        {
            internal class Pass
            {
                internal string shaderModel;
                internal readonly string name;
                internal readonly int shaderPassIndex;
                internal string sourceCode;
                internal FileInfo sourceCodeFile;
                internal List<HashSet<string>> multicompiles { get; private set; }
                internal List<HashSet<string>> combinedMulticompiles { get; private set; }

                public Pass(string name, int shaderPassIndex)
                {
                    this.name = name;
                    this.shaderPassIndex = shaderPassIndex;

                    multicompiles = new List<HashSet<string>>();
                    combinedMulticompiles = new List<HashSet<string>>();
                }
            }

            // Inputs
            internal readonly Shader shader;
            internal readonly HashSet<int> skippedPasses;
            internal readonly HashSet<string> shaderKeywords;

            // Outputs
#if UNITY_2018_1_OR_NEWER
            public ShaderData shaderData { get; private set; }
#endif

            public List<Pass> passes { get; private set; }

            Dictionary<int, List<int>> m_CompileUnitPerPass = new Dictionary<int, List<int>>();

            public ShaderBuildData(
                Shader shader,
                DirectoryInfo temporaryDirectory,
                IEnumerable<int> skippedPasses,
                IEnumerable<string> shaderKeywords,
                ProgressWrapper progress) : base(temporaryDirectory, progress)
            {
                this.shader = shader;
                this.skippedPasses = skippedPasses != null ? new HashSet<int>(skippedPasses) : new HashSet<int>();
                this.shaderKeywords = shaderKeywords != null ? new HashSet<string>(shaderKeywords) : new HashSet<string>();

                passes = new List<Pass>();
            }

            public void FetchShaderData()
            {
#if UNITY_2018_1_OR_NEWER
                shaderData = ShaderUtil.GetShaderData(shader);
#else
                throw new Exception("Missing Unity ShaderData feature, It requires Unity 2018.1 or newer.");
#endif
            }

            public void BuildPassesData()
            {
#if UNITY_2018_1_OR_NEWER
                Assert.IsNotNull(shaderData);
                passes.Clear();

                var activeSubshader = shaderData.ActiveSubshader;
                if (activeSubshader == null)
                    return;

                for (int i = 0, c = activeSubshader.PassCount; i < c; ++i)
                {
                    if (skippedPasses.Contains(i))
                        continue;

                    var passData = activeSubshader.GetPass(i);
                    var pass = new Pass(string.IsNullOrEmpty(passData.Name) ? i.ToString("D3") : passData.Name, i);
                    pass.sourceCode = passData.SourceCode;
                    pass.sourceCodeFile = Utils.GetTemporaryProgramSourceCodeFile(temporaryDirectory, i);
                    passes.Add(pass);
                }
#endif
            }

            public IEnumerator ExportPassSourceFiles()
            {
                var c = passes.Count;
                var s = 1f / Mathf.Max(1, c - 1);
                for (var i = 0; i < c; i++)
                {
                    progress.SetNormalizedProgress(s * i, "Exporting Pass Source File {0:D3} / {1:D3}", i + 1, c);

                    var pass = passes[i];
                    File.WriteAllText(pass.sourceCodeFile.FullName, pass.sourceCode);
                    yield return null;
                }
            }

            public IEnumerator ParseMultiCompiles()
            {
                var c = passes.Count;
                var s = 1f / Mathf.Max(1, c - 1);
                for (var i = 0; i < c; i++)
                {
                    progress.SetNormalizedProgress(s * i, "Parsing multi compiles {0:D3} / {1:D3}", i + 1, c);

                    var pass = passes[i];
                    Utils.ParseVariantMultiCompiles(pass.sourceCode, pass.multicompiles);
                    yield return null;
                }
            }

            public IEnumerator ParseShaderModel()
            {
                var c = passes.Count;
                var s = 1f / Mathf.Max(1, c - 1);
                for (var i = 0; i < c; i++)
                {
                    progress.SetNormalizedProgress(s * i, "Parsing multi compiles {0:D3} / {1:D3}", i + 1, c);

                    var pass = passes[i];
                    Utils.ParseShaderModel(pass.sourceCode, ref pass.shaderModel);
                    yield return null;
                }
            }

            public IEnumerator BuildMultiCompileCombination()
            {
                var c = passes.Count;
                var s = 1f / Mathf.Max(1, c - 1);
                for (var i = 0; i < c; i++)
                {
                    progress.SetNormalizedProgress(s * i, "Building multi compile tuples {0:D3} / {1:D3}", i + 1, c);

                    var pass = passes[i];
                    var enumerator = Utils.BuildDefinesFromMultiCompiles(pass.multicompiles, pass.combinedMulticompiles);
                    while (enumerator.MoveNext())
                        yield return null;
                }
            }

            public IEnumerator BuildCompileUnits()
            {
                ClearCompileUnits();
                m_CompileUnitPerPass.Clear();

                var c = passes.Count;
                var s = 1f / Mathf.Max(1, c - 1);
                for (var i = 0; i < c; i++)
                {
                    var pass = passes[i];
                    var c2 = pass.combinedMulticompiles.Count;
                    var s2 = 1f / Mathf.Max(1, c2 - 1);

                    m_CompileUnitPerPass[i] = new List<int>();

                    for (var j = 0; j < pass.combinedMulticompiles.Count; j++)
                    {
                        progress.SetNormalizedProgress(s * (i + s2 * j), "Building compile units pass: {0:D3} / {1:D3}, unit: {2:D3} / {3:D3}", i + 1, c, j + 1, c2);

                        var compileOptions = PS4Utility.DefaultHDRPCompileOptions(
                            shaderKeywords, 
                            "Frag", 
                            PS4Utility.currentSourceDir,
                            pass.shaderModel);
                        compileOptions.defines.UnionWith(pass.combinedMulticompiles[j]);
                        compileOptions.defines.Add(PS4Utility.k_DefineFragment);

                        var unit = new CompileUnit
                        {
                            sourceCodeFile = pass.sourceCodeFile,
                            compileOptions = compileOptions,
                            compileProfile = ShaderProfile.PixelProgram,
                            compiledFile = Utils.GetTemporaryProgramCompiledFile(pass.sourceCodeFile, temporaryDirectory, j.ToString("D3"))
                        };

                        m_CompileUnitPerPass[i].Add(m_CompileUnits.Count);
                        AddCompileUnit(unit);

                        yield return null;
                    }
                }
            }

            public override IEnumerator ExportBuildReport()
            {
                m_Report = new ShaderBuildReport();

#if UNITY_2018_1_OR_NEWER
                foreach (var skippedPassIndex in skippedPasses)
                {
                    var pass = shaderData.ActiveSubshader.GetPass(skippedPassIndex);
                    m_Report.AddSkippedPass(skippedPassIndex, pass.Name);
                }
#endif

                var c = passes.Count;
                for (var i = 0; i < c; i++)
                {
                    var pass = passes[i];
                    var program = m_Report.AddGPUProgram(
                        pass.name,
                        pass.sourceCode,
                        shaderKeywords.ToArray(),
                        DefineSetFromHashSets(pass.multicompiles),
                        DefineSetFromHashSets(pass.combinedMulticompiles));

                    var unitIndices = m_CompileUnitPerPass[i];
                    for (var j = 0; j < unitIndices.Count; j++)
                    {
                        var unit = m_CompileUnits[unitIndices[j]];
                        program.AddCompileUnit(j, unit.compileOptions.defines.ToArray(), unit.warnings.ToArray(), unit.errors.ToArray(), ShaderProfile.PixelProgram, unit.compileOptions.entry);
                    }

                    for (var j = 0; j < unitIndices.Count; j++)
                    {
                        var indice = unitIndices[j];
                        if (indice >= m_PerfReports.Count)
                            continue;

                        var perf = m_PerfReports[unitIndices[j]];
                        program.AddPerformanceReport(j, perf.rawReport, perf.parsedReport);
                    }
                }

                yield break;
            }
        }

        IEnumerator DoTick_Shader_Internal(string[] shaderKeywords, DirectoryInfo temporaryDirectory, HashSet<int> skippedVariantIndices)
        {
            var p = new ProgressWrapper(this);

            var buildData = new ShaderBuildData(shader, temporaryDirectory, skippedVariantIndices, shaderKeywords, p);
            buildData.ClearOrCreateTemporaryDirectory();
            buildData.FetchShaderData();
            buildData.BuildPassesData();

            var steps = new []
            {
                new { p = 0.1f, step = (Func<IEnumerator>)buildData.ExportPassSourceFiles },
                new { p = 0.1f, step = (Func<IEnumerator>)buildData.ParseShaderModel },
                new { p = 0.1f, step = (Func<IEnumerator>)buildData.ParseMultiCompiles },
                new { p = 0.1f, step = (Func<IEnumerator>)buildData.BuildMultiCompileCombination },
                new { p = 0.1f, step = (Func<IEnumerator>)buildData.BuildCompileUnits },
                new { p = 0.1f, step = (Func<IEnumerator>)buildData.CompileCompileUnits },
                new { p = 0.1f, step = (Func<IEnumerator>)buildData.BuildPerformanceUnits },
                new { p = 0.1f, step = (Func<IEnumerator>)buildData.GeneratePerformanceReports },
                new { p = 0.1f, step = (Func<IEnumerator>)buildData.ExportRawPerfReports },
                new { p = 0.1f, step = (Func<IEnumerator>)buildData.ExportBuildReport },
            };

            IEnumerator e = null;
            

            var min = 0f;
            var max = steps[0].p;
            for (var i = 0; i < steps.Length; i++)
            {
                p.SetProgressRange(min, max);
                e = steps[i].step();
                while (e.MoveNext())
                {
                    yield return null;
                    if (m_Cancelled)
                    {
                        buildData.Dispose();
                        yield break;
                    }
                }
                if (m_Cancelled)
                {
                    buildData.Dispose();
                    yield break;
                }

                min += steps[0].p;
                max += steps[0].p;
            }

            m_BuildReport = buildData.report;
        }
        
        static void ParseShaderCompileErrors(string error, List<ShaderBuildReport.LogItem> warnings, List<ShaderBuildReport.LogItem> errors)
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
