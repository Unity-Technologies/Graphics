using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal
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
        TerrainHoles = (1 << 7)
    }
    internal class ShaderPreprocessor : IPreprocessShaders
    {
        public static readonly string kTerrainShaderName = "Universal Render Pipeline/Terrain/Lit";
#if PROFILE_BUILD
        private const string k_ProcessShaderTag = "OnProcessShader";
#endif

        ShaderKeyword m_MainLightShadows = new ShaderKeyword(ShaderKeywordStrings.MainLightShadows);
        ShaderKeyword m_AdditionalLightsVertex = new ShaderKeyword(ShaderKeywordStrings.AdditionalLightsVertex);
        ShaderKeyword m_AdditionalLightsPixel = new ShaderKeyword(ShaderKeywordStrings.AdditionalLightsPixel);
        ShaderKeyword m_AdditionalLightShadows = new ShaderKeyword(ShaderKeywordStrings.AdditionalLightShadows);
        ShaderKeyword m_CascadeShadows = new ShaderKeyword(ShaderKeywordStrings.MainLightShadowCascades);
        ShaderKeyword m_SoftShadows = new ShaderKeyword(ShaderKeywordStrings.SoftShadows);
        ShaderKeyword m_MixedLightingSubtractive = new ShaderKeyword(ShaderKeywordStrings.MixedLightingSubtractive);
        ShaderKeyword m_Lightmap = new ShaderKeyword("LIGHTMAP_ON");
        ShaderKeyword m_DirectionalLightmap = new ShaderKeyword("DIRLIGHTMAP_COMBINED");
        ShaderKeyword m_AlphaTestOn = new ShaderKeyword("_ALPHATEST_ON");

        int m_TotalVariantsInputCount;
        int m_TotalVariantsOutputCount;

        // Multiple callback may be implemented.
        // The first one executed is the one where callbackOrder is returning the smallest number.
        public int callbackOrder { get { return 0; } }

        bool IsFeatureEnabled(ShaderFeatures featureMask, ShaderFeatures feature)
        {
            return (featureMask & feature) != 0;
        }

        bool StripUnusedPass(ShaderFeatures features, ShaderSnippetData snippetData)
        {
            if (snippetData.passType == PassType.Meta)
                return true;

            if (snippetData.passType == PassType.ShadowCaster)
                if (!IsFeatureEnabled(features, ShaderFeatures.MainLightShadows) && !IsFeatureEnabled(features, ShaderFeatures.AdditionalLightShadows))
                    return true;

            return false;
        }

        bool StripUnusedFeatures(ShaderFeatures features, Shader shader, ShaderSnippetData snippetData, ShaderCompilerData compilerData)
        {
            // strip main light shadows and cascade variants
            if (!IsFeatureEnabled(features, ShaderFeatures.MainLightShadows))
            {
                if (compilerData.shaderKeywordSet.IsEnabled(m_MainLightShadows))
                    return true;

                if (compilerData.shaderKeywordSet.IsEnabled(m_CascadeShadows))
                    return true;
            }

            if (!IsFeatureEnabled(features, ShaderFeatures.SoftShadows) &&
                compilerData.shaderKeywordSet.IsEnabled(m_SoftShadows))
                return true;

            if (compilerData.shaderKeywordSet.IsEnabled(m_MixedLightingSubtractive) &&
                !IsFeatureEnabled(features, ShaderFeatures.MixedLighting))
                return true;

            // No additional light shadows
            bool isAdditionalLightShadow = compilerData.shaderKeywordSet.IsEnabled(m_AdditionalLightShadows);
            if (!IsFeatureEnabled(features, ShaderFeatures.AdditionalLightShadows) && isAdditionalLightShadow)
                return true;


            // Additional light are shaded per-vertex or per-pixel.
            bool isFeaturePerPixelLightingEnabled = IsFeatureEnabled(features, ShaderFeatures.AdditionalLights);
            bool isFeaturePerVertexLightingEnabled = IsFeatureEnabled(features, ShaderFeatures.VertexLighting);
            bool isAdditionalLightPerPixel = compilerData.shaderKeywordSet.IsEnabled(m_AdditionalLightsPixel);
            bool isAdditionalLightPerVertex = compilerData.shaderKeywordSet.IsEnabled(m_AdditionalLightsVertex);

            // Strip if Per-Pixel lighting is NOT used in the project and the
            // Per-Pixel (_ADDITIONAL_LIGHTS) or additional shadows (_ADDITIONAL_LIGHT_SHADOWS)
            // variants are enabled in the shader.
            if (!isFeaturePerPixelLightingEnabled && (isAdditionalLightPerPixel || isAdditionalLightShadow))
                return true;

            // Strip if Per-Vertex lighting is NOT used in the project and the
            // Per-Vertex (_ADDITIONAL_LIGHTS_VERTEX) variant is enabled in the shader.
            if (!isFeaturePerVertexLightingEnabled && isAdditionalLightPerVertex)
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
            if (!isMainShadow && compilerData.shaderKeywordSet.IsEnabled(m_CascadeShadows))
                return true;

            bool isAdditionalShadow = compilerData.shaderKeywordSet.IsEnabled(m_AdditionalLightShadows);
            if (isAdditionalShadow && !compilerData.shaderKeywordSet.IsEnabled(m_AdditionalLightsPixel))
                return true;

            bool isShadowVariant = isMainShadow || isAdditionalShadow;
            if (!isShadowVariant && compilerData.shaderKeywordSet.IsEnabled(m_SoftShadows))
                return true;

            return false;
        }

        bool StripUnused(ShaderFeatures features, Shader shader, ShaderSnippetData snippetData, ShaderCompilerData compilerData)
        {
            if (StripUnusedFeatures(features, shader, snippetData, compilerData))
                return true;

            if (StripInvalidVariants(compilerData))
                return true;

            if (StripUnsupportedVariants(compilerData))
                return true;

            if (StripUnusedPass(features, snippetData))
                return true;

            // Strip terrain holes
            // TODO: checking for the string name here is expensive
            // maybe we can rename alpha clip keyword name to be specific to terrain?
            if (compilerData.shaderKeywordSet.IsEnabled(m_AlphaTestOn) &&
                !IsFeatureEnabled(features, ShaderFeatures.TerrainHoles) &&
                shader.name.Contains(kTerrainShaderName))
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
#if PROFILE_BUILD
            Profiler.BeginSample(k_ProcessShaderTag);
#endif
            UniversalRenderPipelineAsset urpAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            if (urpAsset == null || compilerDataList == null || compilerDataList.Count == 0)
                return;


            int prevVariantCount = compilerDataList.Count;

            var inputShaderVariantCount = compilerDataList.Count;
            for (int i = 0; i < inputShaderVariantCount;)
            {
                bool removeInput = StripUnused(ShaderBuildPreprocessor.supportedFeatures, shader, snippetData, compilerDataList[i]);
                if (removeInput)
                    compilerDataList[i] = compilerDataList[--inputShaderVariantCount];
                else
                    ++i;
            }

            if(compilerDataList is List<ShaderCompilerData> inputDataList)
                inputDataList.RemoveRange(inputShaderVariantCount, inputDataList.Count - inputShaderVariantCount);
            else
            {
                for(int i = compilerDataList.Count -1; i >= inputShaderVariantCount; --i)
                    compilerDataList.RemoveAt(i);
            }

            if (urpAsset.shaderVariantLogLevel != ShaderVariantLogLevel.Disabled)
            {
                m_TotalVariantsInputCount += prevVariantCount;
                m_TotalVariantsOutputCount += compilerDataList.Count;
                LogShaderVariants(shader, snippetData, urpAsset.shaderVariantLogLevel, prevVariantCount, compilerDataList.Count);
            }
#if PROFILE_BUILD
            Profiler.EndSample();
#endif
        }
    }
    class ShaderBuildPreprocessor : IPreprocessBuildWithReport
#if PROFILE_BUILD
        , IPostprocessBuildWithReport
#endif
    {
        public static ShaderFeatures supportedFeatures
        {
            get
            {
                if (_supportedFeatures <= 0)
                {
                    FetchAllSupportedFeatures();
                }
                return _supportedFeatures;
            }
        }

        private static ShaderFeatures _supportedFeatures = 0;
        public int callbackOrder { get { return 0; } }
#if PROFILE_BUILD
        public void OnPostprocessBuild(BuildReport report)
        {
            Profiler.enabled = false;
        }
#endif
        public void OnPreprocessBuild(BuildReport report)
        {
            FetchAllSupportedFeatures();
#if PROFILE_BUILD
            Profiler.enableBinaryLog = true;
            Profiler.logFile = "profilerlog.raw";
            Profiler.enabled = true;
#endif
        }

        private static void FetchAllSupportedFeatures()
        {
            List<UniversalRenderPipelineAsset> urps = new List<UniversalRenderPipelineAsset>();
            urps.Add(GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset);
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                urps.Add(QualitySettings.GetRenderPipelineAssetAt(i) as UniversalRenderPipelineAsset);
            }
            foreach (UniversalRenderPipelineAsset urp in urps)
            {
                if (urp != null)
                {
                    _supportedFeatures |= GetSupportedShaderFeatures(urp);
                }
            }
        }

        private static ShaderFeatures GetSupportedShaderFeatures(UniversalRenderPipelineAsset pipelineAsset)
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
                              (shaderFeatures & ShaderFeatures.AdditionalLightShadows) != 0;
            if (pipelineAsset.supportsSoftShadows && anyShadows)
                shaderFeatures |= ShaderFeatures.SoftShadows;

            if (pipelineAsset.supportsMixedLighting)
                shaderFeatures |= ShaderFeatures.MixedLighting;

            if (pipelineAsset.supportsTerrainHoles)
                shaderFeatures |= ShaderFeatures.TerrainHoles;

            return shaderFeatures;
        }
    }
}
