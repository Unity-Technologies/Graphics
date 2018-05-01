//#define LOG_VARIANTS
//#define LOG_ONLY_LWRP_VARIANTS

using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Rendering;
using LightweightRP = UnityEngine.Experimental.Rendering.LightweightPipeline.LightweightPipeline;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
#if UNITY_2018_2_OR_NEWER
    public class ShaderPreprocessor : IPreprocessShaders
    {
#if LOG_VARIANTS
        int m_TotalVariantsInputCount;
        int m_TotalVariantsOutputCount;
#endif

        // Multiple callback may be implemented.
        // The first one executed is the one where callbackOrder is returning the smallest number.
        public int callbackOrder { get { return 0; } }

        bool StripUnusedShader(PipelineCapabilities capabilities, Shader shader)
        {
            if (shader.name.Contains("Debug"))
                return true;

            if (shader.name.Contains("HDRenderPipeline"))
                return true;

            if (!CoreUtils.HasFlag(capabilities, PipelineCapabilities.DirectionalShadows) &&
                shader.name.Contains("ScreenSpaceShadows"))
                return true;

            return false;
        }

        bool StripUnusedPass(PipelineCapabilities capabilities, ShaderSnippetData snippetData)
        {
            if (snippetData.passType == PassType.Meta)
                return true;

            if (snippetData.passType == PassType.ShadowCaster)
                if (!CoreUtils.HasFlag(capabilities, PipelineCapabilities.DirectionalShadows) && !CoreUtils.HasFlag(capabilities, PipelineCapabilities.LocalShadows))
                    return true;

            return false;
        }

        bool StripUnusedVariant(PipelineCapabilities capabilities, ShaderCompilerData compilerData)
        {
            if (compilerData.shaderKeywordSet.IsEnabled(LightweightKeywords.AdditionalLights) &&
                !CoreUtils.HasFlag(capabilities, PipelineCapabilities.AdditionalLights))
                return true;

            if (compilerData.shaderKeywordSet.IsEnabled(LightweightKeywords.VertexLights) &&
                !CoreUtils.HasFlag(capabilities, PipelineCapabilities.VertexLights))
                return true;

            if (compilerData.shaderKeywordSet.IsEnabled(LightweightKeywords.DirectionalShadows) &&
                !CoreUtils.HasFlag(capabilities, PipelineCapabilities.DirectionalShadows))
                return true;

            if (compilerData.shaderKeywordSet.IsEnabled(LightweightKeywords.LocalShadows) &&
                !CoreUtils.HasFlag(capabilities, PipelineCapabilities.LocalShadows))
                return true;

            if (compilerData.shaderKeywordSet.IsEnabled(LightweightKeywords.SoftShadows) &&
                !CoreUtils.HasFlag(capabilities, PipelineCapabilities.SoftShadows))
                return true;

            return false;
        }

        bool StripInvalidVariants(ShaderCompilerData compilerData)
        {
            bool isShadowVariant = compilerData.shaderKeywordSet.IsEnabled(LightweightKeywords.DirectionalShadows) ||
                compilerData.shaderKeywordSet.IsEnabled(LightweightKeywords.LocalShadows);

            if (compilerData.shaderKeywordSet.IsEnabled(LightweightKeywords.SoftShadows) && !isShadowVariant)
                return true;

            if (compilerData.shaderKeywordSet.IsEnabled(LightweightKeywords.VertexLights) &&
                !compilerData.shaderKeywordSet.IsEnabled(LightweightKeywords.AdditionalLights))
                return true;

            // Note: LWRP doesn't support Dynamic Lightmap.
            if (compilerData.shaderKeywordSet.IsEnabled(LightweightKeywords.DirectionalLightmap) &&
                !compilerData.shaderKeywordSet.IsEnabled(LightweightKeywords.Lightmap))
                return true;

            return false;
        }

        bool StripUnused(PipelineCapabilities capabilities, Shader shader, ShaderSnippetData snippetData, ShaderCompilerData compilerData)
        {
            if (StripUnusedShader(capabilities, shader))
                return true;

            if (StripUnusedPass(capabilities, snippetData))
                return true;

            if (StripUnusedVariant(capabilities, compilerData))
                return true;

            if (StripInvalidVariants(compilerData))
                return true;

            return false;
        }

#if LOG_VARIANTS
        void LogVariants(Shader shader, ShaderSnippetData snippetData, int prevVariantsCount, int currVariantsCount)
        {
#if LOG_ONLY_LWRP_VARIANTS
            if (shader.name.Contains("LightweightPipeline"))
#endif
            {
                float percentageCurrent = (float)currVariantsCount / (float)prevVariantsCount * 100f;
                float percentageTotal = (float)m_TotalVariantsOutputCount / (float)m_TotalVariantsInputCount * 100f;

                string result = string.Format("STRIPPING: {0} ({1} pass) ({2}) -" +
                        " Remaining shader variants = {3}/{4} = {5}% - Total = {6}/{7} = {8}%",
                        shader.name, snippetData.passName, snippetData.shaderType.ToString(), currVariantsCount,
                        prevVariantsCount, percentageCurrent, m_TotalVariantsOutputCount, m_TotalVariantsInputCount,
                        percentageTotal);
                Debug.Log(result);
            }
        }

#endif

        public void OnProcessShader(Shader shader, ShaderSnippetData snippetData, IList<ShaderCompilerData> compilerDataList)
        {
            LightweightRP lw = RenderPipelineManager.currentPipeline as LightweightRP;
            if (lw == null)
                return;

            PipelineCapabilities capabilities = LightweightRP.GetPipelineCapabilities();
            int prevVariantCount = compilerDataList.Count;

            for (int i = 0; i < compilerDataList.Count; ++i)
            {
                if (StripUnused(capabilities, shader, snippetData, compilerDataList[i]))
                {
                    compilerDataList.RemoveAt(i);
                    --i;
                }
            }

#if LOG_VARIANTS
            m_TotalVariantsInputCount += prevVariantCount;
            m_TotalVariantsOutputCount += compilerDataList.Count;

            LogVariants(shader, snippetData, prevVariantCount, compilerDataList.Count);
#endif
        }
    }
#endif // UNITY_2018_2_OR_NEWER
}
