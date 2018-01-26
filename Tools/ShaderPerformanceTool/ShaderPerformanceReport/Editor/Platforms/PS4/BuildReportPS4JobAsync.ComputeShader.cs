using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Experimental.ShaderTools.Internal;
using UnityEngine;

namespace UnityEditor.Experimental.ShaderTools.PSSLInternal
{
    using Utils = EditorShaderPerformanceReportUtil;

    partial class BuildReportPS4JobAsync
    {
        public ComputeShader compute { get; private set; }


        public BuildReportPS4JobAsync(BuildTarget target, ComputeShader compute)
            : base(target)
        {
            this.compute = compute;
            m_Compiler.Initialize();
        }

        class ComputeShaderBuildData : ShaderBuildDataBase
        {
            internal class Kernel
            {
                internal readonly string name;
                internal HashSet<string> defines { get; private set; }

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

            public List<Kernel> kernels { get; private set; }

            public ComputeShaderBuildData(
                ComputeShader compute,
                DirectoryInfo temporaryDirectory,
                ProgressWrapper progress) : base (temporaryDirectory, progress)
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

                Utils.ParseComputeShaderKernels(computeBody, parsedKernels);

                kernels.Clear();

                foreach (var parsedKernel in parsedKernels)
                {
                    var kernel = new Kernel(parsedKernel.Key, parsedKernel.Value);
                    kernels.Add(kernel);
                }

                yield break;
            }

            public IEnumerator BuildCompileUnits()
            {
                ClearCompileUnits();

                var c = kernels.Count;
                var s = 1f / Mathf.Max(1, c - 1);
                for (var i = 0; i < c; i++)
                {
                    var kernel = kernels[i];
                    
                    progress.SetNormalizedProgress(s * i, "Building compile units {0:D3} / {1:D3}", i + 1, c);

                    var compileOptions = PS4Utility.DefaultHDRPCompileOptions(kernel.defines, kernel.name, PS4Utility.currentSourceDir);
                    compileOptions.defines.Add(PS4Utility.k_DefineCompute);

                    var unit = new CompileUnit
                    {
                        sourceCodeFile = sourceCodeFile,
                        compileOptions = compileOptions,
                        compileProfile = ShaderProfile.ComputeProgram,
                        compiledFile = Utils.GetTemporaryProgramCompiledFile(sourceCodeFile, temporaryDirectory, kernel.name)
                    };

                    AddCompileUnit(unit);

                    yield return null;
                }
            }

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

                    var unit = m_CompileUnits[i];
                    program.AddCompileUnit(-1, unit.compileOptions.defines.ToArray(), unit.warnings.ToArray(), unit.errors.ToArray(), ShaderProfile.ComputeProgram, unit.compileOptions.entry);

                    var perf = m_PerfReports[i];
                    program.AddPerformanceReport(-1, perf.rawReport, perf.parsedReport);
                }

                yield break;
            }
        }

        IEnumerator DoTick_ComputeShader()
        {
            var temporaryDirectory = Utils.GetTemporaryDirectory(compute, BuildTarget.PS4);

            var p = new ProgressWrapper(this);

            var buildData = new ComputeShaderBuildData(compute, temporaryDirectory, p);
            buildData.ClearOrCreateTemporaryDirectory();

            var steps = new[]
            {
                new { p = 0.1f, step = (Func<IEnumerator>)buildData.FetchSourceCode },
                new { p = 0.1f, step = (Func<IEnumerator>)buildData.ParseComputeShaderKernels },
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

            SetProgress(1, "Completed");
        }
    }
}
