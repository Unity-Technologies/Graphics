//#define LOG_VARIANTS
//#define LOG_ONLY_LWRP_VARIANTS

using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    internal class ShaderPreprocessor : IPreprocessShaders
    {
        ShaderKeyword m_MainLightShadows = new ShaderKeyword(ShaderKeywordStrings.MainLightShadows);
        ShaderKeyword m_AdditionalLightsVertex = new ShaderKeyword(ShaderKeywordStrings.AdditionalLightsVertex);
        ShaderKeyword m_AdditionalLightsPixel = new ShaderKeyword(ShaderKeywordStrings.AdditionalLightsPixel);
        ShaderKeyword m_AdditionalLightShadows = new ShaderKeyword(ShaderKeywordStrings.AdditionalLightShadows);
        ShaderKeyword m_CascadeShadows = new ShaderKeyword(ShaderKeywordStrings.MainLightShadows);
        ShaderKeyword m_SoftShadows = new ShaderKeyword(ShaderKeywordStrings.SoftShadows);
        ShaderKeyword m_MixedLightingSubtractive = new ShaderKeyword(ShaderKeywordStrings.MixedLightingSubtractive);
        ShaderKeyword m_Lightmap = new ShaderKeyword("LIGHTMAP_ON");
        ShaderKeyword m_DirectionalLightmap = new ShaderKeyword("DIRLIGHTMAP_COMBINED");

        ShaderKeyword m_DeprecatedVertexLights = new ShaderKeyword("_VERTEX_LIGHTS");
        ShaderKeyword m_DeprecatedShadowsEnabled = new ShaderKeyword("_SHADOWS_ENABLED");
        ShaderKeyword m_DeprecatedShadowsCascade = new ShaderKeyword("_SHADOWS_CASCADE");
        ShaderKeyword m_DeprecatedLocalShadowsEnabled = new ShaderKeyword("_LOCAL_SHADOWS_ENABLED");

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

            if (!CoreUtils.HasFlag(features, ShaderFeatures.MainLightShadows) &&
                shader.name.Contains("ScreenSpaceShadows"))
                return true;

            return false;
        }

        bool StripUnusedPass(ShaderFeatures features, ShaderSnippetData snippetData)
        {
            if (snippetData.passType == PassType.Meta)
                return true;

            if (snippetData.passType == PassType.ShadowCaster)
                if (!CoreUtils.HasFlag(features, ShaderFeatures.MainLightShadows) && !CoreUtils.HasFlag(features, ShaderFeatures.AdditionalLightShadows))
                    return true;

            return false;
        }

        bool StripUnusedFeatures(ShaderFeatures features, ShaderCompilerData compilerData)
        {
            // strip main light shadows and cascade variants
            if (!CoreUtils.HasFlag(features, ShaderFeatures.MainLightShadows))
            {
                if (compilerData.shaderKeywordSet.IsEnabled(m_MainLightShadows))
                    return true;

                if (compilerData.shaderKeywordSet.IsEnabled(m_CascadeShadows))
                    return true;
            }

            bool isAdditionalLightPerVertex = compilerData.shaderKeywordSet.IsEnabled(m_AdditionalLightsVertex);
            bool isAdditionalLightPerPixel = compilerData.shaderKeywordSet.IsEnabled(m_AdditionalLightsPixel);
            bool isAdditionalLightShadow = compilerData.shaderKeywordSet.IsEnabled(m_AdditionalLightShadows);

            // Additional light are shaded per-vertex. Strip additional lights per-pixel and shadow variants
            if (CoreUtils.HasFlag(features, ShaderFeatures.VertexLighting) &&
                (isAdditionalLightPerPixel || isAdditionalLightShadow))
                return true;

            // No additional lights
            if (!CoreUtils.HasFlag(features, ShaderFeatures.AdditionalLights) &&
                (isAdditionalLightPerPixel || isAdditionalLightPerVertex || isAdditionalLightShadow))
                return true;

            // No additional light shadows
            if (!CoreUtils.HasFlag(features, ShaderFeatures.AdditionalLightShadows) && isAdditionalLightShadow)
                return true;

            if (!CoreUtils.HasFlag(features, ShaderFeatures.SoftShadows) &&
                compilerData.shaderKeywordSet.IsEnabled(m_SoftShadows))
                return true;

            if (compilerData.shaderKeywordSet.IsEnabled(m_MixedLightingSubtractive) &&
                !CoreUtils.HasFlag(features, ShaderFeatures.MixedLighting))
                return true;

            return false;
        }

        bool StripUnsupportedVariants(ShaderCompilerData compilerData)
        {
            // Dynamic GI is not supported so we can strip variants that have directional lightmap
            // enabled but not baked lightmap.
            if (compilerData.shaderKeywordSet.IsEnabled(m_DirectionalLightmap) &&
                !compilerData.shaderKeywordSet.IsEnabled(m_Lightmap))
                return true;

            if (compilerData.shaderCompilerPlatform == ShaderCompilerPlatform.GLES20)
            {
                if (compilerData.shaderKeywordSet.IsEnabled(m_CascadeShadows))
                    return true;
            }

            return false;
        }

        bool StripInvalidVariants(ShaderCompilerData compilerData)
        {
            bool isMainShadow = compilerData.shaderKeywordSet.IsEnabled(m_MainLightShadows);
            bool isAdditionalShadow = compilerData.shaderKeywordSet.IsEnabled(m_AdditionalLightShadows);
            bool isShadowVariant = isMainShadow || isAdditionalShadow;

            if (!isMainShadow && compilerData.shaderKeywordSet.IsEnabled(m_CascadeShadows))
                return true;

            if (!isShadowVariant && compilerData.shaderKeywordSet.IsEnabled(m_SoftShadows))
                return true;

            if (isAdditionalShadow && !compilerData.shaderKeywordSet.IsEnabled(m_AdditionalLightsPixel)) 
                return true;

            return false;
        }

        bool StripDeprecated(ShaderCompilerData compilerData)
        {
            if (compilerData.shaderKeywordSet.IsEnabled(m_DeprecatedVertexLights))
                return true;

            if (compilerData.shaderKeywordSet.IsEnabled(m_DeprecatedShadowsCascade))
                return true;

            if (compilerData.shaderKeywordSet.IsEnabled(m_DeprecatedShadowsEnabled))
                return true;

            if (compilerData.shaderKeywordSet.IsEnabled(m_DeprecatedLocalShadowsEnabled))
                return true;

            return false;
        }

        bool StripUnused(ShaderFeatures features, Shader shader, ShaderSnippetData snippetData, ShaderCompilerData compilerData)
        {
            if (StripUnusedShader(features, shader))
                return true;

            if (StripUnusedPass(features, snippetData))
                return true;

            if (StripUnusedFeatures(features, compilerData))
                return true;

            if (StripUnsupportedVariants(compilerData))
                return true;

            if (StripInvalidVariants(compilerData))
                return true;

            if (StripDeprecated(compilerData))
                return true;

            return false;
        }

#if LOG_VARIANTS
        void LogVariants(Shader shader, ShaderSnippetData snippetData, int prevVariantsCount, int currVariantsCount)
        {
#if LOG_ONLY_LWRP_VARIANTS
            if (shader.name.Contains("LightweightRenderPipeline"))
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
            LightweightRenderPipeline lw = RenderPipelineManager.currentPipeline as LightweightRenderPipeline;
            if (lw == null)
                return;

            ShaderFeatures features = LightweightRenderPipeline.supportedShaderFeatures;

            if (compilerDataList == null)
                return;

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
