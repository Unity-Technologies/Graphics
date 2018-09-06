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
    public static class LightweightKeyword
    {
        public static readonly ShaderKeyword AdditionalLights = new ShaderKeyword(LightweightKeywordStrings.AdditionalLights);
        public static readonly ShaderKeyword VertexLights = new ShaderKeyword(LightweightKeywordStrings.VertexLights);
        public static readonly ShaderKeyword MixedLightingSubtractive = new ShaderKeyword(LightweightKeywordStrings.MixedLightingSubtractive);
        public static readonly ShaderKeyword DirectionalShadows = new ShaderKeyword(LightweightKeywordStrings.DirectionalShadows);
        public static readonly ShaderKeyword LocalShadows = new ShaderKeyword(LightweightKeywordStrings.LocalShadows);
        public static readonly ShaderKeyword SoftShadows = new ShaderKeyword(LightweightKeywordStrings.SoftShadows);
        public static readonly ShaderKeyword CascadeShadows = new ShaderKeyword(LightweightKeywordStrings.CascadeShadows);

        public static readonly ShaderKeyword Lightmap = new ShaderKeyword("LIGHTMAP_ON");
        public static readonly ShaderKeyword DirectionalLightmap = new ShaderKeyword("DIRLIGHTMAP_COMBINED");
    }

    public class ShaderPreprocessor : IPreprocessShaders
    {
#if LOG_VARIANTS
        int m_TotalVariantsInputCount;
        int m_TotalVariantsOutputCount;
#endif

        // Multiple callback may be implemented.
        // The first one executed is the one where callbackOrder is returning the smallest number.
        public int callbackOrder { get { return 0; } }

        bool StripUnusedShader(ShaderFeatures features, Shader shader)
        {
            if (shader.name.Contains("Debug"))
                return true;

            if (shader.name.Contains("HDRenderPipeline"))
                return true;

            if (!CoreUtils.HasFlag(features, ShaderFeatures.DirectionalShadows) &&
                shader.name.Contains("ScreenSpaceShadows"))
                return true;

            return false;
        }

        bool StripUnusedPass(ShaderFeatures features, ShaderSnippetData snippetData)
        {
            if (snippetData.passType == PassType.Meta)
                return true;

            if (snippetData.passType == PassType.ShadowCaster)
                if (!CoreUtils.HasFlag(features, ShaderFeatures.DirectionalShadows) && !CoreUtils.HasFlag(features, ShaderFeatures.LocalShadows))
                    return true;

            return false;
        }

        bool StripUnusedVariant(ShaderFeatures features, ShaderCompilerData compilerData)
        {
            if (compilerData.shaderKeywordSet.IsEnabled(LightweightKeyword.AdditionalLights) &&
                !CoreUtils.HasFlag(features, ShaderFeatures.AdditionalLights))
                return true;

            if (compilerData.shaderKeywordSet.IsEnabled(LightweightKeyword.VertexLights) &&
                !CoreUtils.HasFlag(features, ShaderFeatures.VertexLights))
                return true;

            if (compilerData.shaderKeywordSet.IsEnabled(LightweightKeyword.DirectionalShadows) &&
                !CoreUtils.HasFlag(features, ShaderFeatures.DirectionalShadows))
                return true;

            if (compilerData.shaderKeywordSet.IsEnabled(LightweightKeyword.LocalShadows) &&
                !CoreUtils.HasFlag(features, ShaderFeatures.LocalShadows))
                return true;

            if (compilerData.shaderKeywordSet.IsEnabled(LightweightKeyword.SoftShadows) &&
                !CoreUtils.HasFlag(features, ShaderFeatures.SoftShadows))
                return true;

            return false;
        }

        bool StripInvalidVariants(ShaderCompilerData compilerData)
        {
            bool isDirectionalShadows = compilerData.shaderKeywordSet.IsEnabled(LightweightKeyword.DirectionalShadows);
            bool isShadowVariant = isDirectionalShadows || compilerData.shaderKeywordSet.IsEnabled(LightweightKeyword.LocalShadows);

            if (!isDirectionalShadows && compilerData.shaderKeywordSet.IsEnabled(LightweightKeyword.CascadeShadows))
                return true;

            if (!isShadowVariant && compilerData.shaderKeywordSet.IsEnabled(LightweightKeyword.SoftShadows))
                return true;

            if (compilerData.shaderKeywordSet.IsEnabled(LightweightKeyword.VertexLights) &&
                !compilerData.shaderKeywordSet.IsEnabled(LightweightKeyword.AdditionalLights))
                return true;

            // Note: LWRP doesn't support Dynamic Lightmap.
            if (compilerData.shaderKeywordSet.IsEnabled(LightweightKeyword.DirectionalLightmap) &&
                !compilerData.shaderKeywordSet.IsEnabled(LightweightKeyword.Lightmap))
                return true;

            return false;
        }

        bool StripUnused(ShaderFeatures features, Shader shader, ShaderSnippetData snippetData, ShaderCompilerData compilerData)
        {
            if (StripUnusedShader(features, shader))
                return true;

            if (StripUnusedPass(features, snippetData))
                return true;

            if (StripUnusedVariant(features, compilerData))
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

            ShaderFeatures features = LightweightRP.GetSupportedShaderFeatures();
            int prevVariantCount = compilerDataList.Count;

            for (int i = 0; i < compilerDataList.Count; ++i)
            {
                if (StripUnused(features, shader, snippetData, compilerDataList[i]))
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
}
