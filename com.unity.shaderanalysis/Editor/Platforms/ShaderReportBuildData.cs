using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.ShaderAnalysis.Internal;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.ShaderAnalysis
{
    public abstract class ShaderBuildData : ReportBuildData
    {
        public class Pass
        {
            public string shaderModel;
            public string name;
            internal readonly int shaderPassIndex;
            public string sourceCode;
            public FileInfo sourceCodeFile;
            public List<HashSet<string>> multicompiles { get; }
            public List<HashSet<string>> combinedMulticompiles { get; }

            public Pass(string name, int shaderPassIndex)
            {
                this.name = name;
                this.shaderPassIndex = shaderPassIndex;

                multicompiles = new List<HashSet<string>>();
                combinedMulticompiles = new List<HashSet<string>>();
            }
        }

        static readonly Regex k_FragmentDeclaration = new Regex(@"#pragma\s+fragment\s([^\s]+)");

        // Inputs
        readonly Shader m_Shader;
        readonly HashSet<int> m_SkippedPassesFromFilter = new HashSet<int>();
        readonly HashSet<string> m_ShaderKeywords;

        // Outputs
#if UNITY_2018_1_OR_NEWER
        public ShaderData shaderData { get; private set; }
#endif

        public List<Pass> passes { get; set; }

        Dictionary<int, List<int>> m_CompileUnitPerPass = new Dictionary<int, List<int>>();

        protected ShaderBuildData(
            Shader shader,
            DirectoryInfo temporaryDirectory,
            IEnumerable<string> shaderKeywords,
            ShaderProgramFilter filter,
            ProgressWrapper progress) : base(temporaryDirectory, progress, filter)
        {
            this.m_Shader = shader;
            this.m_ShaderKeywords = shaderKeywords != null ? new HashSet<string>(shaderKeywords) : new HashSet<string>();

            passes = new List<Pass>();
        }

        public void FetchShaderData()
        {
#if UNITY_2018_1_OR_NEWER
            shaderData = ShaderUtil.GetShaderData(m_Shader);
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
                var passData = activeSubshader.GetPass(i);
                if (filter != null && (filter.includedPassNames.Count > 0 && !filter.includedPassNames.Contains(passData.Name)
                    || filter.excludedPassNames.Contains(passData.Name)))
                {
                    m_SkippedPassesFromFilter.Add(i);
                    continue;
                }

                var pass = new Pass(string.IsNullOrEmpty(passData.Name) ? i.ToString("D3") : passData.Name, i)
                {
                    sourceCode = passData.SourceCode,
                    sourceCodeFile = ShaderAnalysisUtils.GetTemporaryProgramSourceCodeFile(temporaryDirectory, i)
                };
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
                ShaderAnalysisUtils.ParseVariantMultiCompiles(pass.sourceCode, pass.multicompiles);
                yield return null;
            }
        }

        public IEnumerator ParseShaderModel()
        {
            var c = passes.Count;
            var s = 1f / Mathf.Max(1, c - 1);
            for (var i = 0; i < c; i++)
            {
                progress.SetNormalizedProgress(s * i, "Parsing shader model {0:D3} / {1:D3}", i + 1, c);

                var pass = passes[i];
                ShaderAnalysisUtils.ParseShaderModel(pass.sourceCode, ref pass.shaderModel);
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
                var enumerator = ShaderAnalysisUtils.BuildDefinesFromMultiCompiles(pass.multicompiles, pass.combinedMulticompiles, filter);
                while (enumerator.MoveNext())
                    yield return null;
            }
        }

        protected IEnumerator BuildCompileUnits_Internal(BuildTarget target, DirectoryInfo sourceDir)
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

                    var match = k_FragmentDeclaration.Match(pass.sourceCode);
                    Assert.IsTrue(match.Success);

                    var entryPoint = match.Groups[1].Value;

                    var compileOptions = ShaderAnalysisUtils.DefaultCompileOptions(
                        m_ShaderKeywords,
                        entryPoint,
                        sourceDir,
                        pass.shaderModel);
                    compileOptions.defines.UnionWith(pass.combinedMulticompiles[j]);
                    compileOptions.defines.Add(ShaderAnalysisUtils.DefineFragment);

                    var unit = new CompileUnit
                    {
                        sourceCodeFile = pass.sourceCodeFile,
                        compileOptions = compileOptions,
                        compileProfile = ShaderProfile.PixelProgram,
                        compileTarget = ShaderTarget.PS_5,
                        compiledFile = ShaderAnalysisUtils.GetTemporaryProgramCompiledFile(pass.sourceCodeFile, temporaryDirectory, j.ToString("D3"))
                    };

                    m_CompileUnitPerPass[i].Add(compileUnitCount);
                    AddCompileUnit(unit);

                    yield return null;
                }
            }
        }

        // This will call BuildCompileUnits_Internal with the right options

        public abstract IEnumerator BuildCompileUnits();

        public override IEnumerator ExportBuildReport()
        {
            m_Report = new ShaderBuildReport();

#if UNITY_2018_1_OR_NEWER
            foreach (var skippedPassIndex in m_SkippedPassesFromFilter)
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
                    m_ShaderKeywords.ToArray(),
                    DefineSetFromHashSets(pass.multicompiles),
                    DefineSetFromHashSets(pass.combinedMulticompiles));

                var unitIndices = m_CompileUnitPerPass[i];
                for (var j = 0; j < unitIndices.Count; j++)
                {
                    var unit = GetCompileUnitAt(unitIndices[j]);
                    program.AddCompileUnit(j, unit.compileOptions.defines.ToArray(), unit.warnings.ToArray(), unit.errors.ToArray(), ShaderProfile.PixelProgram, unit.compileOptions.entry);
                }

                for (var j = 0; j < unitIndices.Count; j++)
                {
                    var indice = unitIndices[j];
                    if (indice >= perfReportCount)
                        continue;

                    var perf = GetPerfReportAt(unitIndices[j]);
                    program.AddPerformanceReport(j, perf.rawReport, perf.parsedReport);
                }
            }

            yield break;
        }
    }
}
