using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal
{
    internal class ShaderPreprocessor : IPreprocessShaders
    {
        [Flags]
        enum ShaderFeatures
        {
            MainLight = (1 << 0),
            MainLightShadows = (1 << 1),
            AdditionalLights = (1 << 2),
            AdditionalLightShadows = (1 << 3),
            VertexLighting = (1 << 4),
            SoftShadows = (1 << 5),
            MixedLighting = (1 << 6),
        }

        ShaderKeyword m_MainLightShadows = new ShaderKeyword(ShaderKeywordStrings.MainLightShadows);
        ShaderKeyword m_AdditionalLightsVertex = new ShaderKeyword(ShaderKeywordStrings.AdditionalLightsVertex);
        ShaderKeyword m_AdditionalLightsPixel = new ShaderKeyword(ShaderKeywordStrings.AdditionalLightsPixel);
        ShaderKeyword m_AdditionalLightShadows = new ShaderKeyword(ShaderKeywordStrings.AdditionalLightShadows);
        ShaderKeyword m_CascadeShadows = new ShaderKeyword(ShaderKeywordStrings.MainLightShadowCascades);
        ShaderKeyword m_SoftShadows = new ShaderKeyword(ShaderKeywordStrings.SoftShadows);
        ShaderKeyword m_MixedLightingSubtractive = new ShaderKeyword(ShaderKeywordStrings.MixedLightingSubtractive);
        ShaderKeyword m_Lightmap = new ShaderKeyword("LIGHTMAP_ON");
        ShaderKeyword m_DirectionalLightmap = new ShaderKeyword("DIRLIGHTMAP_COMBINED");
        ShaderKeyword m_KillAlpha = new ShaderKeyword("_KILL_ALPHA");

        ShaderKeyword m_DeprecatedVertexLights = new ShaderKeyword("_VERTEX_LIGHTS");
        ShaderKeyword m_DeprecatedShadowsEnabled = new ShaderKeyword("_SHADOWS_ENABLED");
        ShaderKeyword m_DeprecatedShadowsCascade = new ShaderKeyword("_SHADOWS_CASCADE");
        ShaderKeyword m_DeprecatedLocalShadowsEnabled = new ShaderKeyword("_LOCAL_SHADOWS_ENABLED");

        int m_TotalVariantsInputCount;
        int m_TotalVariantsOutputCount;

        // Multiple callback may be implemented.
        // The first one executed is the one where callbackOrder is returning the smallest number.
        public int callbackOrder { get { return 0; } }

        bool StripUnusedShader(ShaderFeatures features, Shader shader, ShaderCompilerData compilerData)
        {
            if (shader.name.Contains("HDRP"))
                return true;

            if (!CoreUtils.HasFlag(features, ShaderFeatures.MainLightShadows) &&
                shader.name.Contains("ScreenSpaceShadows"))
                return true;

            if (shader.name == "Hidden/Universal Render Pipeline/Blit")
            {
                bool preserveAlpha = PlayerSettings.preserveFramebufferAlpha && compilerData.platformKeywordSet.IsEnabled(BuiltinShaderDefine.SHADER_API_MOBILE);
                bool shaderPreserveAlpha = !compilerData.shaderKeywordSet.IsEnabled(m_KillAlpha);

                if (preserveAlpha != shaderPreserveAlpha)
                    return true;
            }

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
            if (StripUnusedShader(features, shader, compilerData))
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

        void LogShaderVariants(Shader shader, ShaderSnippetData snippetData, ShaderVariantLogLevel logLevel, int prevVariantsCount, int currVariantsCount)
        {
            if (logLevel == ShaderVariantLogLevel.AllShaders || shader.name.Contains("Universal Render Pipeline"))
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

        public void OnProcessShader(Shader shader, ShaderSnippetData snippetData, IList<ShaderCompilerData> compilerDataList)
        {
            UniversalRenderPipelineAsset lwrpAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            if (lwrpAsset == null || compilerDataList == null || compilerDataList.Count == 0)
                return;

            ShaderFeatures features = GetSupportedShaderFeatures(lwrpAsset);

            int prevVariantCount = compilerDataList.Count;

            for (int i = 0; i < compilerDataList.Count; ++i)
            {
                if (StripUnused(features, shader, snippetData, compilerDataList[i]))
                {
                    compilerDataList.RemoveAt(i);
                    --i;
                }
            }

            if (lwrpAsset.shaderVariantLogLevel != ShaderVariantLogLevel.Disabled)
            {
                m_TotalVariantsInputCount += prevVariantCount;
                m_TotalVariantsOutputCount += compilerDataList.Count;
                LogShaderVariants(shader, snippetData, lwrpAsset.shaderVariantLogLevel, prevVariantCount, compilerDataList.Count);
            }
        }

        ShaderFeatures GetSupportedShaderFeatures(UniversalRenderPipelineAsset pipelineAsset)
        {
            ShaderFeatures shaderFeatures;
            shaderFeatures = ShaderFeatures.MainLight;

            if (pipelineAsset.supportsMainLightShadows)
                shaderFeatures |= ShaderFeatures.MainLightShadows;

            if (pipelineAsset.additionalLightsRenderingMode == LightRenderingMode.PerVertex)
            {
                shaderFeatures |= ShaderFeatures.AdditionalLights;
                shaderFeatures |= ShaderFeatures.VertexLighting;
            }
            else if (pipelineAsset.additionalLightsRenderingMode == LightRenderingMode.PerPixel)
            {
                shaderFeatures |= ShaderFeatures.AdditionalLights;

                if (pipelineAsset.supportsAdditionalLightShadows)
                    shaderFeatures |= ShaderFeatures.AdditionalLightShadows;
            }

            bool anyShadows = pipelineAsset.supportsMainLightShadows ||
                              CoreUtils.HasFlag(shaderFeatures, ShaderFeatures.AdditionalLightShadows);
            if (pipelineAsset.supportsSoftShadows && anyShadows)
                shaderFeatures |= ShaderFeatures.SoftShadows;

            if (pipelineAsset.supportsMixedLighting)
                shaderFeatures |= ShaderFeatures.MixedLighting;

            return shaderFeatures;
        }
    }
}
