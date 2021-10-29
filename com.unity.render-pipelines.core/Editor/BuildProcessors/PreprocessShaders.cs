using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;

namespace UnityEditor.Rendering
{
    public abstract class PreprocessShaders : IPreprocessShaders
    {
#if PROFILE_BUILD
        private const string k_ProcessShaderTag = "OnProcessShader";
#endif

        /// <summary>
        /// Event callback to report the shader stripping info
        /// </summary>
        /// <example>
        /// ReportShaderStrippingData(Shader shader, ShaderSnippetData data, int currentVariantCount, double strippingTime)
        /// </example>
        public static event Action<Shader, ShaderSnippetData, int, double> shaderPreprocessed;

        private static readonly System.Diagnostics.Stopwatch m_StripTimer = new();

        protected int totalVariantsInputCount = 0;
        protected int totalVariantsOutputCount = 0;

        /// <summary>
        /// Multiple callback may be implemented. The first one executed is the one where callbackOrder is returning the smallest number.
        /// </summary>
        public virtual int callbackOrder => 0;

        /// <summary>
        /// Initializes the local shader keywords from a <see cref="Shader"/>
        /// </summary>
        /// <param name="shader">The <see cref="Shader"/> to initialize the shader keywords.</param>
        protected virtual void InitializeLocalShaderKeywords(Shader shader)
        {
        }

        /// <summary>
        /// Strips the given <see cref="Shader"/>
        /// </summary>
        /// <param name="shader">The <see cref="Shader"/> that might be stripped.</param>
        /// <param name="snippetData">The <see cref="ShaderSnippetData"/></param>
        /// <param name="compilerDataList">A list of <see cref="ShaderCompilerData"/></param>
        public void OnProcessShader(Shader shader, ShaderSnippetData snippetData, IList<ShaderCompilerData> compilerDataList)
        {
            if (compilerDataList == null || compilerDataList.Count == 0)
                return;

            if (!IsActive())
                return;

#if PROFILE_BUILD
            Profiler.BeginSample(k_ProcessShaderTag);
#endif
            int variantsInCount = compilerDataList.Count;

            m_StripTimer.Start();

            InitializeLocalShaderKeywords(shader);

            var inputShaderVariantCount = compilerDataList.Count;
            for (int i = 0; i < inputShaderVariantCount;)
            {
                bool removeInput = CanRemoveInput(shader, snippetData, compilerDataList[i]);

                // Remove at swap back
                if (removeInput)
                    compilerDataList[i] = compilerDataList[--inputShaderVariantCount];
                else
                    ++i;
            }

            if (compilerDataList is List<ShaderCompilerData> inputDataList)
                inputDataList.RemoveRange(inputShaderVariantCount, inputDataList.Count - inputShaderVariantCount);
            else
            {
                for (int i = compilerDataList.Count - 1; i >= inputShaderVariantCount; --i)
                    compilerDataList.RemoveAt(i);
            }

            m_StripTimer.Stop();
            var stripTimeMs = m_StripTimer.Elapsed.TotalMilliseconds;
            m_StripTimer.Reset();

            if (IsLogVariantsEnabled(shader))
            {
                ComputeTotalVariants(variantsInCount, compilerDataList.Count);
                LogShaderVariants(shader, snippetData, variantsInCount, compilerDataList.Count, stripTimeMs);
            }

            // If the export is enabled export the shader data that has been stripped
            if (IsExportLogEnabled())
            {
                ShaderStripExporter.Data data = new ShaderStripExporter.Data(shader, snippetData, compilerDataList)
                {
                    variantIn = variantsInCount,
                    variantOut = compilerDataList.Count,
                    totalVariantIn = totalVariantsInputCount,
                    totalVariantOut = totalVariantsOutputCount
                };

                ShaderStripExporter.Export(data);
            }

#if PROFILE_BUILD
            Profiler.EndSample();
#endif
            shaderPreprocessed?.Invoke(shader, snippetData, variantsInCount, stripTimeMs);
        }

        /// <summary>
        /// Computes the total of the variants
        /// </summary>
        /// <param name="variantsInCount"></param>
        /// <param name="variantsOutCount"></param>
        public virtual void ComputeTotalVariants(int variantsInCount, int variantsOutCount)
        {
            totalVariantsInputCount += variantsInCount;
            totalVariantsOutputCount += variantsOutCount;
        }

        /// <summary>
        /// Strips the given <see cref="Shader"/>
        /// </summary>
        /// <param name="shader">The <see cref="Shader"/> that might be stripped.</param>
        /// <param name="snippetData">The <see cref="ShaderSnippetData"/></param>
        /// <param name="compilerDataList">A list of <see cref="ShaderCompilerData"/></param>
        protected abstract bool CanRemoveInput(Shader shader, ShaderSnippetData snippetData, ShaderCompilerData inputData);

        /// <summary>
        /// Returns if the the variants needs to be logged.
        /// </summary>
        /// <param name="shader">The shader that is generating the variants.</param>
        /// <returns>True if the variants for the given <see cref="Shader"/> should be logged.</returns>
        public abstract bool IsLogVariantsEnabled(Shader shader);

        /// <summary>
        /// Returns if the variants stripping needs to be exported
        /// </summary>
        /// <returns></returns>
        public abstract bool IsExportLogEnabled();

        /// <summary>
        /// Returns if the <see cref="IPreprocessShaders"/> is active.
        /// </summary>
        /// <returns>Returns true or false depending on the specified conditions.</returns>
        public abstract bool IsActive();

        protected void LogShaderVariants(Shader shader, ShaderSnippetData snippetData, int prevVariantsCount, int currVariantsCount, double stripTimeMs)
        {
            float percentageCurrent = currVariantsCount / (float)prevVariantsCount * 100f;
            float percentageTotal = totalVariantsOutputCount / (float)totalVariantsInputCount * 100f;

            string result = string.Format("STRIPPING: {0} ({1} pass) ({2}) -" +
                " Remaining shader variants = {3}/{4} = {5}% - Total = {6}/{7} = {8}% TimeMs={9}",
                shader.name, snippetData.passName, snippetData.shaderType.ToString(), currVariantsCount,
                prevVariantsCount, percentageCurrent, totalVariantsOutputCount, totalVariantsInputCount,
                percentageTotal, stripTimeMs);
            Debug.Log(result);
        }
    }
}
