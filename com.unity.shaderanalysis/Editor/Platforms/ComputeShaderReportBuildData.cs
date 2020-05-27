using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.ShaderAnalysis.Internal;
using UnityEngine;

namespace UnityEditor.ShaderAnalysis
{
    public abstract class ComputeShaderReportBuildData : ReportBuildData
    {
        protected class Kernel
        {
            public readonly string name;
            public HashSet<string> defines { get; }

            public Kernel(string name, HashSet<string> defines)
            {
                this.name = name;
                this.defines = defines != null ? new HashSet<string>(defines) : new HashSet<string>();
            }
        }

        // Inputs
        internal readonly ComputeShader compute;

        // Outputs
        public string sourceCode { get; private set; }
        public FileInfo sourceCodeFile { get; private set; }

        protected List<Kernel> kernels { get; }

        protected ComputeShaderReportBuildData(
            ComputeShader compute,
            DirectoryInfo temporaryDirectory,
            ShaderProgramFilter filter,
            ProgressWrapper progress) : base(temporaryDirectory, progress, filter)
        {
            this.compute = compute;

            kernels = new List<Kernel>();
        }

        public IEnumerator FetchSourceCode()
        {
            var sourceFilePath = AssetDatabase.GetAssetPath(compute);
            sourceCodeFile = new FileInfo(sourceFilePath);
            sourceCode = File.ReadAllText(sourceCodeFile.FullName);
            yield break;
        }

        public IEnumerator ParseComputeShaderKernels()
        {
            var sourceFilePath = AssetDatabase.GetAssetPath(compute);
            var sourceFile = new FileInfo(sourceFilePath);
            var computeBody = File.ReadAllText(sourceFile.FullName);

            Dictionary<string, HashSet<string>> parsedKernels = new Dictionary<string, HashSet<string>>();

            ShaderAnalysisUtils.ParseComputeShaderKernels(computeBody, parsedKernels);

            kernels.Clear();

            foreach (var parsedKernel in parsedKernels)
            {
                var kernel = new Kernel(parsedKernel.Key, parsedKernel.Value);
                kernels.Add(kernel);
            }

            yield break;
        }

        protected IEnumerator BuildCompileUnits_Internal(BuildTarget target, DirectoryInfo sourceDir)
        {
            ClearCompileUnits();

            var c = kernels.Count;
            var s = 1f / Mathf.Max(1, c - 1);
            for (var i = 0; i < c; i++)
            {
                var kernel = kernels[i];

                progress.SetNormalizedProgress(s * i, "Building compile units {0:D3} / {1:D3}", i + 1, c);

                var compileOptions = ShaderAnalysisUtils.DefaultCompileOptions(kernel.defines, kernel.name, sourceDir);
                compileOptions.defines.Add(ShaderAnalysisUtils.DefineCompute);


                compileOptions.defines.Add(ShaderAnalysisUtils.DefineCompute);

                var unit = new CompileUnit
                {
                    sourceCodeFile = sourceCodeFile,
                    compileOptions = compileOptions,
                    compileProfile = ShaderProfile.ComputeProgram,
                    compileTarget = ShaderTarget.CS_5,
                    compiledFile = ShaderAnalysisUtils.GetTemporaryProgramCompiledFile(sourceCodeFile, temporaryDirectory, kernel.name)
                };

                AddCompileUnit(unit);

                yield return null;
            }
        }

        // This will call BuildCompileUnits_Internal with the right options

        public abstract IEnumerator BuildCompileUnits();

        public override IEnumerator ExportBuildReport()
        {
            m_Report = new ShaderBuildReport();

            var c = kernels.Count;
            for (var i = 0; i < c; i++)
            {
                var kernel = kernels[i];
                var program = m_Report.AddGPUProgram(
                    kernel.name,
                    sourceCode,
                    kernel.defines.ToArray(),
                    new ShaderBuildReport.DefineSet[0],
                    new ShaderBuildReport.DefineSet[0]);

                var unit = GetCompileUnitAt(i);
                program.AddCompileUnit(
                    -1,
                    unit.compileOptions.defines.ToArray(),
                    unit.warnings.ToArray(),
                    unit.errors.ToArray(),
                    ShaderProfile.ComputeProgram,
                    unit.compileOptions.entry
                );

                var perf = GetPerfReportAt(i);
                program.AddPerformanceReport(-1, perf.rawReport, perf.parsedReport);
            }

            yield break;
        }
    }
}
