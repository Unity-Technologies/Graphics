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
        TerrainHoles = (1 << 7),
        DeferredShading = (1 << 8), // DeferredRenderer is in the list of renderer
        DeferredWithAccurateGbufferNormals = (1 << 9),
        DeferredWithoutAccurateGbufferNormals = (1 << 10)
    }
    internal class ShaderPreprocessor : IPreprocessShaders
    {
        public static readonly string kPassNameGBuffer = "GBuffer";

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
        ShaderKeyword m_GbufferNormalsOct = new ShaderKeyword("_GBUFFER_NORMALS_OCT");

        ShaderKeyword Feature00 = new ShaderKeyword("_FEATURE00");
        ShaderKeyword Feature01 = new ShaderKeyword("_FEATURE01");
        ShaderKeyword Feature02 = new ShaderKeyword("_FEATURE02");
        ShaderKeyword Feature03 = new ShaderKeyword("_FEATURE03");
        ShaderKeyword Feature04 = new ShaderKeyword("_FEATURE04");
        ShaderKeyword Feature05 = new ShaderKeyword("_FEATURE05");
        ShaderKeyword Feature06 = new ShaderKeyword("_FEATURE06");
        ShaderKeyword Feature07 = new ShaderKeyword("_FEATURE07");
        ShaderKeyword Feature08 = new ShaderKeyword("_FEATURE08");
        ShaderKeyword Feature09 = new ShaderKeyword("_FEATURE09");
        ShaderKeyword Feature10 = new ShaderKeyword("_FEATURE10");
        ShaderKeyword Feature11 = new ShaderKeyword("_FEATURE11");
        ShaderKeyword Feature12 = new ShaderKeyword("_FEATURE12");
        ShaderKeyword Feature13 = new ShaderKeyword("_FEATURE13");
        ShaderKeyword Feature14 = new ShaderKeyword("_FEATURE14");
        ShaderKeyword Feature15 = new ShaderKeyword("_FEATURE15");
        ShaderKeyword Feature16 = new ShaderKeyword("_FEATURE16");
        ShaderKeyword Feature17 = new ShaderKeyword("_FEATURE17");
        ShaderKeyword Feature18 = new ShaderKeyword("_FEATURE18");
        ShaderKeyword Feature19 = new ShaderKeyword("_FEATURE19");

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

            // TODO: Test against lightMode tag instead.
            if (!CoreUtils.HasFlag(features, ShaderFeatures.DeferredShading) && snippetData.passName == kPassNameGBuffer)
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

            // Additional light are shaded per-vertex. Strip additional lights per-pixel and shadow variants
            bool isAdditionalLightPerPixel = compilerData.shaderKeywordSet.IsEnabled(m_AdditionalLightsPixel);
            if (IsFeatureEnabled(features, ShaderFeatures.VertexLighting) &&
                (isAdditionalLightPerPixel || isAdditionalLightShadow))
                return true;

            // No additional lights
            if (!IsFeatureEnabled(features, ShaderFeatures.AdditionalLights) &&
                (isAdditionalLightPerPixel || isAdditionalLightShadow || compilerData.shaderKeywordSet.IsEnabled(m_AdditionalLightsVertex)))
                return true;

            // TODO: Test against lightMode tag instead.
            if (snippetData.passName == kPassNameGBuffer)
            {
                if (CoreUtils.HasFlag(features, ShaderFeatures.DeferredWithAccurateGbufferNormals) && !compilerData.shaderKeywordSet.IsEnabled(m_GbufferNormalsOct))
                    return true;
                if (CoreUtils.HasFlag(features, ShaderFeatures.DeferredWithoutAccurateGbufferNormals) && compilerData.shaderKeywordSet.IsEnabled(m_GbufferNormalsOct))
                    return true;
            }
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

            if (compilerData.shaderKeywordSet.IsEnabled(Feature00))
                return true;
            if (compilerData.shaderKeywordSet.IsEnabled(Feature01))
                return true;
            if (compilerData.shaderKeywordSet.IsEnabled(Feature02))
                return true;
            
            if (compilerData.shaderKeywordSet.IsEnabled(Feature03))
                return true;
            
            if (compilerData.shaderKeywordSet.IsEnabled(Feature04))
                return true;
            
            if (compilerData.shaderKeywordSet.IsEnabled(Feature05))
                return true;
            
            if (compilerData.shaderKeywordSet.IsEnabled(Feature06))
                return true;
            
            if (compilerData.shaderKeywordSet.IsEnabled(Feature07))
                return true;
            
            if (compilerData.shaderKeywordSet.IsEnabled(Feature08))
                return true;
            
            if (compilerData.shaderKeywordSet.IsEnabled(Feature09))
                return true;
            
//            if (compilerData.shaderKeywordSet.IsEnabled(Feature10))
//                return true;
//            
//            if (compilerData.shaderKeywordSet.IsEnabled(Feature11))
//                return true;
//            
//            if (compilerData.shaderKeywordSet.IsEnabled(Feature12))
//                return true;
//            
//            if (compilerData.shaderKeywordSet.IsEnabled(Feature13))
//                return true;
//            
//            if (compilerData.shaderKeywordSet.IsEnabled(Feature14))
//                return true;
//                
//                
//            if (compilerData.shaderKeywordSet.IsEnabled(Feature15))
//                return true;
//            
//            if (compilerData.shaderKeywordSet.IsEnabled(Feature16))
//                return true;
//            if (compilerData.shaderKeywordSet.IsEnabled(Feature17))
//                return true;
//            if (compilerData.shaderKeywordSet.IsEnabled(Feature18))
//                return true;
//            if (compilerData.shaderKeywordSet.IsEnabled(Feature19))
//                return true;

            // Strip terrain holes
            // TODO: checking for the string name here is expensive
            // maybe we can rename alpha clip keyword name to be specific to terrain?
            if (compilerData.shaderKeywordSet.IsEnabled(m_AlphaTestOn) &&
                !CoreUtils.HasFlag(features, ShaderFeatures.TerrainHoles) &&
                shader.name.Contains("Universal Render Pipeline/Terrain/Lit"))
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

        private const string k_ProcessShaderTag = "OnProcessShader";
        public void OnProcessShader(Shader shader, ShaderSnippetData snippetData, IList<ShaderCompilerData> compilerDataList)
        {
            Profiler.BeginSample(k_ProcessShaderTag);
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
            Profiler.EndSample();
        }  
    }
    class ShaderBuildPreprocessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public static ShaderFeatures supportedFeatures
        {
            get {
                if (_supportedFeatures <= 0)
                {
                    FetchAllSupportedFeatures();
                }
                return _supportedFeatures;
            }
        }

        private static ShaderFeatures _supportedFeatures = 0;
        public int callbackOrder { get { return 0; } }
        public void OnPostprocessBuild(BuildReport report)
        {
            Debug.Log("Disabling Profiler");
            Profiler.enabled = false;
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            FetchAllSupportedFeatures();
            Profiler.enabled = true;
            Profiler.enableBinaryLog = true;
            Profiler.logFile = "profilerlognew6.raw";
            Debug.Log("Profiler State: " + Profiler.enabled);
        }
        
        private static void FetchAllSupportedFeatures()
        {
            List<UniversalRenderPipelineAsset> urps = new List<UniversalRenderPipelineAsset>();
            urps.Add(GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset);
            for(int i = 0; i < QualitySettings.names.Length; i++)
            {
                urps.Add(QualitySettings.GetRenderPipelineAssetAt(i) as UniversalRenderPipelineAsset);
            }

            // Must reset flags.
            _supportedFeatures = 0;
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

            bool hasDeferredRenderer = false;
            bool withAccurateGbufferNormals = false;
            bool withoutAccurateGbufferNormals = false;

            int rendererCount = pipelineAsset.m_RendererDataList.Length;
            for (int rendererIndex = 0; rendererIndex < rendererCount; ++rendererIndex)
            {
                ScriptableRenderer renderer = pipelineAsset.GetRenderer(rendererIndex);
                if (renderer is DeferredRenderer)
                {
                    hasDeferredRenderer |= true;
                    DeferredRenderer deferredRenderer = (DeferredRenderer)renderer;
                    withAccurateGbufferNormals |= deferredRenderer.AccurateGbufferNormals;
                    withoutAccurateGbufferNormals |= !deferredRenderer.AccurateGbufferNormals;
                }
            }

            if (hasDeferredRenderer)
                shaderFeatures |= ShaderFeatures.DeferredShading;

            // We can only strip accurateGbufferNormals related variants if all DeferredRenderers use the same option.
            if (withAccurateGbufferNormals && !withoutAccurateGbufferNormals)
                shaderFeatures |= ShaderFeatures.DeferredWithAccurateGbufferNormals;
            if (!withAccurateGbufferNormals && withoutAccurateGbufferNormals)
                shaderFeatures |= ShaderFeatures.DeferredWithoutAccurateGbufferNormals;

            return shaderFeatures;
        }
    }
}
