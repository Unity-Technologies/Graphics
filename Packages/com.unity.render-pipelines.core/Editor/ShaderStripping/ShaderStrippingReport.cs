using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    [Serializable]
    class VariantCounter
    {
        public uint inputVariants;
        public uint outputVariants;
        public override string ToString() => $"Total={outputVariants}/{inputVariants}({outputVariants / (float)inputVariants * 100f:0.00}%)";
    }

    [Serializable]
    class ShaderVariantInfo : VariantCounter
    {
        public string variantName;
        public double stripTimeMs;
        public override string ToString() => $"{variantName} - {base.ToString()} - Time={stripTimeMs}ms";
    }

    [Serializable]
    class ShaderStrippingInfo : VariantCounter, ISerializationCallbackReceiver
    {
        public string name;

        private Dictionary<string, (VariantCounter count, List<ShaderVariantInfo> variantInfos)> m_VariantsByPipeline = new();

        public void AddVariant(string pipeline, ShaderVariantInfo variant)
        {
            if (!m_VariantsByPipeline.TryGetValue(pipeline, out var list))
            {
                list.count = new VariantCounter();
                list.variantInfos = new List<ShaderVariantInfo>();
                m_VariantsByPipeline.Add(pipeline, list);
            }

            list.count.inputVariants += variant.inputVariants;
            list.count.outputVariants += variant.outputVariants;

            inputVariants += variant.inputVariants;
            outputVariants += variant.outputVariants;

            list.variantInfos.Add(variant);
        }

        public override string ToString() => $"{name} - {base.ToString()}";

        public void Log(ShaderVariantLogLevel logLevel)
        {
            IEnumerable<ShaderVariantInfo> variantsToLog = null;
            switch (logLevel)
            {
                case ShaderVariantLogLevel.AllShaders:
                {
                    variantsToLog = m_VariantsByPipeline.SelectMany(i => i.Value.variantInfos);
                    break;
                }
                case ShaderVariantLogLevel.OnlySRPShaders:
                {
                    variantsToLog = m_VariantsByPipeline
                        .Where(i => !string.IsNullOrEmpty(i.Key))
                        .SelectMany(i => i.Value.variantInfos);
                    break;
                }
            }

            if (variantsToLog != null && variantsToLog.Any())
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"{this}");
                foreach (var variant in variantsToLog)
                {
                    sb.AppendLine($"- {variant}");
                }

                Debug.Log(sb.ToString());
            }
        }

        [Serializable]
        class PipelineVariants : VariantCounter
        {
            public string pipeline;
            public ShaderVariantInfo[] variants;
        }

        [SerializeField] private PipelineVariants[] pipelines;
        public void OnBeforeSerialize()
        {
            pipelines = m_VariantsByPipeline
                .Select(pipeline => new PipelineVariants()
                {
                    pipeline = pipeline.Key,
                    variants = pipeline.Value.variantInfos.ToArray(),
                    inputVariants = pipeline.Value.count.inputVariants,
                    outputVariants = pipeline.Value.count.outputVariants,
                })
                .ToArray();
        }

        public void OnAfterDeserialize()
        {
            pipelines = null;
        }
    }

    /// <summary>
    /// This class works as an scope of the <see cref="ShaderStrippingReport"/> hooking into the
    /// <see cref="IPreprocessBuildWithReport"/> that are being called at the begin of the build and
    /// to <see cref="IPostprocessBuildWithReport"/> that are the ones called after the build is finished
    /// </summary>
    class ShaderStrippingReportScope : IPostprocessBuildWithReport, IPreprocessBuildWithReport
    {
        internal static bool s_DefaultExport = false; // This variable is used by reflection by unit tests

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            ShaderVariantLogLevel logStrippedVariants = ShaderVariantLogLevel.Disabled;
            bool exportStrippedVariants = s_DefaultExport;

            // Check the current pipeline and check the shader variant settings
            if (RenderPipelineManager.currentPipeline != null && RenderPipelineManager.currentPipeline.defaultSettings is IShaderVariantSettings shaderVariantSettings)
            {
                logStrippedVariants = shaderVariantSettings.shaderVariantLogLevel;
                exportStrippedVariants = shaderVariantSettings.exportShaderVariants;
            }

            ShaderStripping.reporter = (logStrippedVariants == ShaderVariantLogLevel.Disabled && exportStrippedVariants == false) ?
                new ShaderStrippingReportEmpty() : new ShaderStrippingReport(logStrippedVariants, exportStrippedVariants);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            ShaderStripping.reporter.DumpReport();
            ShaderStripping.reporter = null;
        }
    }

    /// <summary>
    /// This class is instantiated as the reporter if the logging and exporting is disabled
    /// this avoid tracking all the variants, allocating memory, and doing work that is not need
    /// </summary>
    class ShaderStrippingReportEmpty : IShaderStrippingReport
    {
        public void OnShaderProcessed<TShader, TShaderVariant>([DisallowNull] TShader shader, TShaderVariant shaderVariant, string pipeline, uint variantsIn, uint variantsOut, double stripTimeMs)
            where TShader : UnityEngine.Object
        { }
        public void DumpReport() { }
    }

    /// <summary>
    /// This class is instantiated as the reporter if the reporter is null because we are building asset bundles
    /// </summary>
    class ShaderStrippingReportLogger : IShaderStrippingReport
    {
        private bool m_IsLogEnabled = false;

        public ShaderStrippingReportLogger()
        {
            // Check the current pipeline and check the shader variant settings
            if (RenderPipelineManager.currentPipeline != null && RenderPipelineManager.currentPipeline.defaultSettings is IShaderVariantSettings shaderVariantSettings)
            {
                m_IsLogEnabled = shaderVariantSettings.shaderVariantLogLevel != ShaderVariantLogLevel.Disabled;
            }
        }

        public void OnShaderProcessed<TShader, TShaderVariant>([DisallowNull] TShader shader, TShaderVariant shaderVariant, string pipeline, uint variantsIn, uint variantsOut, double stripTimeMs)
            where TShader : UnityEngine.Object
        {
            if (!m_IsLogEnabled)
                return;

            if (!ShaderStrippingReport.TryGetVariantName(shader, shaderVariant, out string variantName))
                return;

            Debug.Log($"Shader={shader.name}{variantName} Pipeline={pipeline} Total={variantsIn}/{variantsOut}({variantsOut / (float)variantsIn * 100f:0.00}%) Time={stripTimeMs}ms");
        }

        public void DumpReport()
        {
            // Just logs variants into the console
        }
    }

    /// <summary>
    /// Class to gather all the information about stripping in SRP packages
    /// </summary>
    class ShaderStrippingReport : IShaderStrippingReport
    {
        private readonly ShaderVariantLogLevel m_LogStrippedVariants;
        private readonly bool m_ExportStrippedVariants;

        // Shader
        private readonly List<ShaderStrippingInfo> m_ShaderInfos = new();
        private readonly VariantCounter m_ShaderVariantCounter = new();

        // Compute Shader
        private readonly List<ShaderStrippingInfo> m_ComputeShaderInfos = new();
        private readonly VariantCounter m_ComputeShaderVariantCounter = new();

        public ShaderStrippingReport(ShaderVariantLogLevel logLevel, bool export)
        {
            m_LogStrippedVariants = logLevel;
            m_ExportStrippedVariants = export;
        }

        public void OnShaderProcessed<TShader, TShaderVariant>([DisallowNull] TShader shader, TShaderVariant shaderVariant, string pipeline, uint variantsIn, uint variantsOut, double stripTimeMs)
            where TShader : UnityEngine.Object
        {
            if (!TryGetVariantName(shader, shaderVariant, out string variantName))
                throw new NotImplementedException($"Report is not enabled for {typeof(TShader)} and {typeof(TShaderVariant)}");

            var lastShaderStrippingInfo = FindLastShaderStrippingInfo(shader);
            if (typeof(TShader) == typeof(Shader))
            {
                m_ShaderVariantCounter.inputVariants += variantsIn;
                m_ShaderVariantCounter.outputVariants += variantsOut;
            }
            else if (typeof(TShader) == typeof(ComputeShader))
            {
                m_ComputeShaderVariantCounter.inputVariants += variantsIn;
                m_ComputeShaderVariantCounter.outputVariants += variantsOut;
            }

            lastShaderStrippingInfo.AddVariant(pipeline, new ShaderVariantInfo()
            {
                inputVariants = variantsIn,
                outputVariants = variantsOut,
                stripTimeMs = stripTimeMs,
                variantName = variantName
            });
        }

        internal static string k_ShaderOutputPath = "Temp/shader-stripping.json";
        internal static string k_ComputeShaderOutputPath = "Temp/compute-shader-stripping.json";

        public void DumpReport()
        {
            if (m_LogStrippedVariants != ShaderVariantLogLevel.Disabled)
            {
                Debug.Log($"Shader Stripping - {m_ShaderVariantCounter}");
                foreach (var info in m_ShaderInfos)
                {
                    info.Log(m_LogStrippedVariants);
                }

                Debug.Log($"Compute Shader Stripping - {m_ComputeShaderVariantCounter}");
                foreach (var info in m_ComputeShaderInfos)
                {
                    info.Log(m_LogStrippedVariants);
                }
            }

            if (m_ExportStrippedVariants)
            {
                ExportShaderStrippingInfo(k_ShaderOutputPath, m_ShaderVariantCounter, m_ShaderInfos);
                ExportShaderStrippingInfo(k_ComputeShaderOutputPath, m_ComputeShaderVariantCounter, m_ComputeShaderInfos);
            }
        }

        [CanBeNull] private ShaderStrippingInfo m_LastShaderStrippingInfo = null;

        private ShaderStrippingInfo FindLastShaderStrippingInfo<TShader>([DisallowNull] TShader shader)
            where TShader : UnityEngine.Object
        {
            if (m_LastShaderStrippingInfo != null && m_LastShaderStrippingInfo.name.Equals(shader.name))
                return m_LastShaderStrippingInfo;

            // We are reporting a new shader variant, need to create a new one
            m_LastShaderStrippingInfo = new ShaderStrippingInfo()
            {
                name = shader.name
            };

            // The compiler will strip the branch that we are not using
            if (typeof(TShader) == typeof(Shader))
            {
                m_ShaderInfos.Add(m_LastShaderStrippingInfo);
            }
            else if (typeof(TShader) == typeof(ComputeShader))
            {
                m_ComputeShaderInfos.Add(m_LastShaderStrippingInfo);
            }

            return m_LastShaderStrippingInfo;
        }

        [MustUseReturnValue]
        internal static bool TryGetVariantName<TShader, TShaderVariant>([DisallowNull] TShader shader, TShaderVariant shaderVariant, out string variantName)
            where TShader : UnityEngine.Object
        {
            variantName = string.Empty;

            if (typeof(TShader) == typeof(Shader) && typeof(TShaderVariant) == typeof(ShaderSnippetData))
            {
                var snippetData = (ShaderSnippetData)Convert.ChangeType(shaderVariant, typeof(ShaderSnippetData));
                string passName = string.IsNullOrEmpty(snippetData.passName) ? $"Pass {snippetData.pass.PassIndex}" : snippetData.passName;
                variantName = $"{passName} ({snippetData.passType}) (SubShader: {snippetData.pass.SubshaderIndex}) (ShaderType: {snippetData.shaderType.ToString()})";
            }
            else if (typeof(TShader) == typeof(ComputeShader) && typeof(TShaderVariant) == typeof(string))
            {
                variantName = $"Kernel: {shaderVariant}";
            }
            else
            {
                return false;
            }

            return true;
        }

        [Serializable]
        class Export
        {
            public uint totalVariantsIn;
            public uint totalVariantsOut;
            public ShaderStrippingInfo[] shaders;
        }

        void ExportShaderStrippingInfo(string path, VariantCounter variantCounter, List<ShaderStrippingInfo> shaders)
        {
            try
            {
                var export = new Export()
                {
                    totalVariantsIn = variantCounter.inputVariants,
                    totalVariantsOut = variantCounter.outputVariants,
                    shaders = shaders.ToArray()
                };

                File.WriteAllText(path, JsonUtility.ToJson(export, true));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    interface IShaderStrippingReport
    {
        void OnShaderProcessed<TShader, TShaderVariant>([DisallowNull] TShader shader,
            TShaderVariant shaderVariant, string pipeline, uint variantsIn, uint variantsOut, double stripTimeMs)
            where TShader : UnityEngine.Object;

        void DumpReport();
    }

    static class ShaderStripping
    {
        private static IShaderStrippingReport m_Reporter;
        public static IShaderStrippingReport reporter
        {
            get => m_Reporter ??= new ShaderStrippingReportLogger();
            internal set => m_Reporter = value;
        }
    }
}
