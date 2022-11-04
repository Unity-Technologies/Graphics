using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering.BuiltIn.ShaderGraph;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.BuiltIn
{
    [Flags]
    enum ShaderFeatures
    {
        None = 0,
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
        DeferredWithoutAccurateGbufferNormals = (1 << 10),
        ScreenSpaceOcclusion = (1 << 11),
        ScreenSpaceShadows = (1 << 12),
        ReflectionProbeBlending = (1 << 13),
        ReflectionProbeBoxProjection = (1 << 14),
    }

    // This should really be part of the main BuiltIn Target Keyword list we use for reference names
    public static class ShaderKeywordStrings
    {
        public static readonly string MainLightShadows = "_MAIN_LIGHT_SHADOWS";
        public static readonly string MainLightShadowCascades = "_MAIN_LIGHT_SHADOWS_CASCADE";
        public static readonly string MainLightShadowScreen = "_MAIN_LIGHT_SHADOWS_SCREEN";
        public static readonly string AdditionalLightsVertex = "_ADDITIONAL_LIGHTS_VERTEX";
        public static readonly string AdditionalLightsPixel = "_ADDITIONAL_LIGHTS";
        public static readonly string AdditionalLightShadows = "_ADDITIONAL_LIGHT_SHADOWS";
        public static readonly string ReflectionProbeBlending = "_REFLECTION_PROBE_BLENDING";
        public static readonly string ReflectionProbeBoxProjection = "_REFLECTION_PROBE_BOX_PROJECTION";
        // This is used during shadow map generation to differentiate between directional and punctual light shadows,
        // as they use different formulas to apply Normal Bias
        public static readonly string SoftShadows = "_SHADOWS_SOFT";
        public static readonly string CastingPunctualLightShadow = "_CASTING_PUNCTUAL_LIGHT_SHADOW";
        public static readonly string MixedLightingSubtractive = "_MIXED_LIGHTING_SUBTRACTIVE"; // Backward compatibility
        public static readonly string LightmapShadowMixing = "LIGHTMAP_SHADOW_MIXING";
        public static readonly string ShadowsShadowMask = "SHADOWS_SHADOWMASK";
        public static readonly string ScreenSpaceOcclusion = "_SCREEN_SPACE_OCCLUSION";
        public static readonly string _GBUFFER_NORMALS_OCT = "_GBUFFER_NORMALS_OCT";
        public static readonly string LIGHTMAP_ON = "LIGHTMAP_ON";
        public static readonly string DYNAMICLIGHTMAP_ON = "DYNAMICLIGHTMAP_ON";
        public static readonly string _ALPHATEST_ON = "_ALPHATEST_ON";
        public static readonly string DIRLIGHTMAP_COMBINED = "DIRLIGHTMAP_COMBINED";
        public static readonly string EDITOR_VISUALIZATION = "EDITOR_VISUALIZATION";
    }

    internal class ShaderPreprocessor : IPreprocessShaders
    {
        public static readonly string kPassNameGBuffer = "BuiltIn Deferred";
#if PROFILE_BUILD
        private const string k_ProcessShaderTag = "OnProcessShader";
#endif
        // Event callback to report shader stripping info. Form:
        // ReportShaderStrippingData(Shader shader, ShaderSnippetData data, int currentVariantCount, double strippingTime)
        internal static event Action<Shader, ShaderSnippetData, int, double> shaderPreprocessed;
        private static readonly System.Diagnostics.Stopwatch m_stripTimer = new System.Diagnostics.Stopwatch();

        ShaderKeyword m_MainLightShadows = new ShaderKeyword(ShaderKeywordStrings.MainLightShadows);
        ShaderKeyword m_MainLightShadowsCascades = new ShaderKeyword(ShaderKeywordStrings.MainLightShadowCascades);
        ShaderKeyword m_MainLightShadowsScreen = new ShaderKeyword(ShaderKeywordStrings.MainLightShadowScreen);
        ShaderKeyword m_AdditionalLightsVertex = new ShaderKeyword(ShaderKeywordStrings.AdditionalLightsVertex);
        ShaderKeyword m_AdditionalLightsPixel = new ShaderKeyword(ShaderKeywordStrings.AdditionalLightsPixel);
        ShaderKeyword m_AdditionalLightShadows = new ShaderKeyword(ShaderKeywordStrings.AdditionalLightShadows);
        ShaderKeyword m_ReflectionProbeBlending = new ShaderKeyword(ShaderKeywordStrings.ReflectionProbeBlending);
        ShaderKeyword m_ReflectionProbeBoxProjection = new ShaderKeyword(ShaderKeywordStrings.ReflectionProbeBoxProjection);
        ShaderKeyword m_CastingPunctualLightShadow = new ShaderKeyword(ShaderKeywordStrings.CastingPunctualLightShadow);
        ShaderKeyword m_SoftShadows = new ShaderKeyword(ShaderKeywordStrings.SoftShadows);
        ShaderKeyword m_MixedLightingSubtractive = new ShaderKeyword(ShaderKeywordStrings.MixedLightingSubtractive);
        ShaderKeyword m_LightmapShadowMixing = new ShaderKeyword(ShaderKeywordStrings.LightmapShadowMixing);
        ShaderKeyword m_ShadowsShadowMask = new ShaderKeyword(ShaderKeywordStrings.ShadowsShadowMask);
        ShaderKeyword m_Lightmap = new ShaderKeyword(ShaderKeywordStrings.LIGHTMAP_ON);
        ShaderKeyword m_DynamicLightmap = new ShaderKeyword(ShaderKeywordStrings.DYNAMICLIGHTMAP_ON);
        ShaderKeyword m_DirectionalLightmap = new ShaderKeyword(ShaderKeywordStrings.DIRLIGHTMAP_COMBINED);
        ShaderKeyword m_AlphaTestOn = new ShaderKeyword(ShaderKeywordStrings._ALPHATEST_ON);
        ShaderKeyword m_GbufferNormalsOct = new ShaderKeyword(ShaderKeywordStrings._GBUFFER_NORMALS_OCT);
        ShaderKeyword m_ScreenSpaceOcclusion = new ShaderKeyword(ShaderKeywordStrings.ScreenSpaceOcclusion);
        ShaderKeyword m_EditorVisualization = new ShaderKeyword(ShaderKeywordStrings.EDITOR_VISUALIZATION);
        ShaderTagId m_ShaderGraphShaderTag = new ShaderTagId("ShaderGraphShader");

        int m_TotalVariantsInputCount;
        int m_TotalVariantsOutputCount;

        // Multiple callback may be implemented.
        // The first one executed is the one where callbackOrder is returning the smallest number.
        public int callbackOrder { get { return 0; } }

        bool IsFeatureEnabled(ShaderFeatures featureMask, ShaderFeatures feature)
        {
            return (featureMask & feature) != 0;
        }

        bool IsSRPPass(ShaderSnippetData snippetData)
        {
            return (snippetData.passType == PassType.ScriptableRenderPipeline ||
                snippetData.passType == PassType.ScriptableRenderPipelineDefaultUnlit);
        }

        bool IsShaderGraphShader(Shader shader, ShaderSnippetData snippetData)
        {
            var shaderGraphTag = shader.FindSubshaderTagValue((int)snippetData.pass.SubshaderIndex, m_ShaderGraphShaderTag);
            if (shaderGraphTag == ShaderTagId.none)
                return false;
            return true;
        }

        bool StripUnusedPass(ShaderFeatures features, ShaderSnippetData snippetData)
        {
            // Strip all SRP shader variants
            if (IsSRPPass(snippetData))
            {
                return true;
            }

            // Meta pass is needed in the player for Enlighten Precomputed Realtime GI albedo and emission.
            if (snippetData.passType == PassType.Meta)
            {
                if (SupportedRenderingFeatures.active.enlighten == false ||
                    ((int)SupportedRenderingFeatures.active.lightmapBakeTypes | (int)LightmapBakeType.Realtime) == 0)
                    return true;
            }

            if (snippetData.passType == PassType.ShadowCaster)
                if (!IsFeatureEnabled(features, ShaderFeatures.MainLightShadows) && !IsFeatureEnabled(features, ShaderFeatures.AdditionalLightShadows))
                    return true;

            return false;
        }

        bool StripUnusedFeatures(ShaderFeatures features, Shader shader, ShaderSnippetData snippetData, ShaderCompilerData compilerData)
        {
            // strip main light shadows, cascade and screen variants
            if (!IsFeatureEnabled(features, ShaderFeatures.MainLightShadows))
            {
                if (compilerData.shaderKeywordSet.IsEnabled(m_MainLightShadows))
                    return true;

                if (compilerData.shaderKeywordSet.IsEnabled(m_MainLightShadowsCascades))
                    return true;

                if (compilerData.shaderKeywordSet.IsEnabled(m_MainLightShadowsScreen))
                    return true;

                if (snippetData.passType == PassType.ShadowCaster && !compilerData.shaderKeywordSet.IsEnabled(m_CastingPunctualLightShadow))
                    return true;
            }

            bool isSoftShadow = compilerData.shaderKeywordSet.IsEnabled(m_SoftShadows);
            if (!IsFeatureEnabled(features, ShaderFeatures.SoftShadows) && isSoftShadow)
                return true;

            // Left for backward compatibility
            if (compilerData.shaderKeywordSet.IsEnabled(m_MixedLightingSubtractive) &&
                !IsFeatureEnabled(features, ShaderFeatures.MixedLighting))
                return true;

            // Strip here only if mixed lighting is disabled
            // No need to check here if actually used by scenes as this taken care by builtin stripper
            if ((compilerData.shaderKeywordSet.IsEnabled(m_LightmapShadowMixing) ||
                 compilerData.shaderKeywordSet.IsEnabled(m_ShadowsShadowMask)) &&
                !IsFeatureEnabled(features, ShaderFeatures.MixedLighting))
                return true;

            // No additional light shadows
            bool isAdditionalLightShadow = compilerData.shaderKeywordSet.IsEnabled(m_AdditionalLightShadows);
            if (!IsFeatureEnabled(features, ShaderFeatures.AdditionalLightShadows) && isAdditionalLightShadow)
                return true;

            bool isReflectionProbeBlending = compilerData.shaderKeywordSet.IsEnabled(m_ReflectionProbeBlending);
            if (!IsFeatureEnabled(features, ShaderFeatures.ReflectionProbeBlending) && isReflectionProbeBlending)
                return true;

            bool isReflectionProbeBoxProjection = compilerData.shaderKeywordSet.IsEnabled(m_ReflectionProbeBoxProjection);
            if (!IsFeatureEnabled(features, ShaderFeatures.ReflectionProbeBoxProjection) && isReflectionProbeBoxProjection)
                return true;

            bool isPunctualLightShadowCasterPass = (snippetData.passType == PassType.ShadowCaster) && compilerData.shaderKeywordSet.IsEnabled(m_CastingPunctualLightShadow);
            if (!IsFeatureEnabled(features, ShaderFeatures.AdditionalLightShadows) && isPunctualLightShadowCasterPass)
                return true;

            // Additional light are shaded per-vertex or per-pixel.
            bool isFeaturePerPixelLightingEnabled = IsFeatureEnabled(features, ShaderFeatures.AdditionalLights);
            bool isFeaturePerVertexLightingEnabled = IsFeatureEnabled(features, ShaderFeatures.VertexLighting);
            bool isAdditionalLightPerPixel = compilerData.shaderKeywordSet.IsEnabled(m_AdditionalLightsPixel);
            bool isAdditionalLightPerVertex = compilerData.shaderKeywordSet.IsEnabled(m_AdditionalLightsVertex);

            // Strip if Per-Pixel lighting is NOT used in the project and the
            // Per-Pixel (_ADDITIONAL_LIGHTS) variant is enabled in the shader.
            if (!isFeaturePerPixelLightingEnabled && isAdditionalLightPerPixel)
                return true;

            // Strip if Per-Vertex lighting is NOT used in the project and the
            // Per-Vertex (_ADDITIONAL_LIGHTS_VERTEX) variant is enabled in the shader.
            if (!isFeaturePerVertexLightingEnabled && isAdditionalLightPerVertex)
                return true;

            // Screen Space Shadows
            if (!IsFeatureEnabled(features, ShaderFeatures.ScreenSpaceShadows) &&
                compilerData.shaderKeywordSet.IsEnabled(m_MainLightShadowsScreen))
                return true;

            // Screen Space Occlusion
            if (!IsFeatureEnabled(features, ShaderFeatures.ScreenSpaceOcclusion) &&
                compilerData.shaderKeywordSet.IsEnabled(m_ScreenSpaceOcclusion))
                return true;

            return false;
        }

        bool StripUnsupportedVariants(ShaderCompilerData compilerData)
        {
            // We can strip variants that have directional lightmap enabled but not static nor dynamic lightmap.
            if (compilerData.shaderKeywordSet.IsEnabled(m_DirectionalLightmap) &&
                !(compilerData.shaderKeywordSet.IsEnabled(m_Lightmap) ||
                  compilerData.shaderKeywordSet.IsEnabled(m_DynamicLightmap)))
                return true;

            // As GLES2 has low amount of registers, we strip:
            if (compilerData.shaderCompilerPlatform == ShaderCompilerPlatform.GLES20)
            {
                // Cascade shadows
                if (compilerData.shaderKeywordSet.IsEnabled(m_MainLightShadowsCascades))
                    return true;

                // Screen space shadows
                if (compilerData.shaderKeywordSet.IsEnabled(m_MainLightShadowsScreen))
                    return true;
            }

            // Editor visualization is only used in scene view debug modes.
            if (compilerData.shaderKeywordSet.IsEnabled(m_EditorVisualization))
                return true;

            return false;
        }

        bool StripInvalidVariants(ShaderCompilerData compilerData)
        {
            bool isMainShadowNoCascades = compilerData.shaderKeywordSet.IsEnabled(m_MainLightShadows);
            bool isMainShadowCascades = compilerData.shaderKeywordSet.IsEnabled(m_MainLightShadowsCascades);
            bool isMainShadowScreen = compilerData.shaderKeywordSet.IsEnabled(m_MainLightShadowsScreen);
            bool isMainShadow = isMainShadowNoCascades || isMainShadowCascades || isMainShadowScreen;

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

            // Skip any shaders that weren't built by Shader Graph.
            // Note: This needs to be after StripUnusedPass so that other SRP shaders (including hand-written) are stripped.
            if (!IsShaderGraphShader(shader, snippetData))
                return false;

            // // TODO: Test against lightMode tag instead.
            if (snippetData.passName == kPassNameGBuffer)
            {
                if (!IsFeatureEnabled(features, ShaderFeatures.DeferredShading))
                    return true;

                // Do not strip accurateGbufferNormals on Mobile Vulkan as some GPUs do not support R8G8B8A8_SNorm, which then force us to use accurateGbufferNormals
                if (!IsFeatureEnabled(features, ShaderFeatures.DeferredWithAccurateGbufferNormals) && compilerData.shaderKeywordSet.IsEnabled(m_GbufferNormalsOct) && compilerData.shaderCompilerPlatform != ShaderCompilerPlatform.Vulkan)
                    return true;
                if (!IsFeatureEnabled(features, ShaderFeatures.DeferredWithoutAccurateGbufferNormals) && !compilerData.shaderKeywordSet.IsEnabled(m_GbufferNormalsOct))
                    return true;
            }
            return false;
        }

        bool ShouldLogShaderVariant(Shader shader, ShaderSnippetData snippetData)
        {
            if (shader.name.Contains("Shader Graphs/") && !IsSRPPass(snippetData))
            {
                return true;
            }

            return false;
        }

        void LogShaderVariants(Shader shader, ShaderSnippetData snippetData, int prevVariantsCount, int currVariantsCount)
        {
            float percentageCurrent = (float)currVariantsCount / (float)prevVariantsCount * 100f;
            float percentageTotal = (float)m_TotalVariantsOutputCount / (float)m_TotalVariantsInputCount * 100f;

            string result = string.Format("STRIPPING: {0} ({1} pass) ({2}) -" +
                " Remaining shader variants = {3}/{4} = {5}% - Total = {6}/{7} = {8}%",
                shader.name, snippetData.passName, snippetData.shaderType.ToString(), currVariantsCount,
                prevVariantsCount, percentageCurrent, m_TotalVariantsOutputCount, m_TotalVariantsInputCount,
                percentageTotal);

            if (ShouldLogShaderVariant(shader, snippetData))
                Debug.Log(result);
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippetData, IList<ShaderCompilerData> compilerDataList)
        {
#if PROFILE_BUILD
            Profiler.BeginSample(k_ProcessShaderTag);
#endif

            // We only want to perform shader variant stripping if the built-in render pipeline
            // is the active render pipeline (i.e., there is no SRP asset in place).
            RenderPipelineAsset rpAsset = GraphicsSettings.currentRenderPipeline;
            if (rpAsset != null || compilerDataList == null || compilerDataList.Count == 0)
                return;

            m_stripTimer.Start();

            int prevVariantCount = compilerDataList.Count;
            var inputShaderVariantCount = compilerDataList.Count;
            for (int i = 0; i < inputShaderVariantCount;)
            {
                bool removeInput = true;
                foreach (var supportedFeatures in ShaderBuildPreprocessor.supportedFeaturesList)
                {
                    if (!StripUnused(supportedFeatures, shader, snippetData, compilerDataList[i]))
                    {
                        removeInput = false;
                        break;
                    }
                }

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

            m_stripTimer.Stop();
            double stripTimeMs = m_stripTimer.Elapsed.TotalMilliseconds;
            m_stripTimer.Reset();

            m_TotalVariantsInputCount += prevVariantCount;
            m_TotalVariantsOutputCount += compilerDataList.Count;
            LogShaderVariants(shader, snippetData, prevVariantCount, compilerDataList.Count);

#if PROFILE_BUILD
            Profiler.EndSample();
#endif
            shaderPreprocessed?.Invoke(shader, snippetData, prevVariantCount, stripTimeMs);
        }
    }

    class ShaderBuildPreprocessor : IPreprocessBuildWithReport
#if PROFILE_BUILD
        , IPostprocessBuildWithReport
#endif
    {
        public static List<ShaderFeatures> supportedFeaturesList
        {
            get
            {
                if (s_SupportedFeaturesList.Count == 0)
                    FetchAllSupportedFeatures();
                return s_SupportedFeaturesList;
            }
        }

        private static List<ShaderFeatures> s_SupportedFeaturesList = new List<ShaderFeatures>();


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
            s_SupportedFeaturesList.Clear();
            s_SupportedFeaturesList.Add(GetSupportedShaderFeatures());
        }

        private static ShaderFeatures GetSupportedShaderFeatures()
        {
            ShaderFeatures shaderFeatures;
            shaderFeatures = ShaderFeatures.MainLight;

            ShadowQuality shadows = QualitySettings.shadows;
            if (shadows != ShadowQuality.Disable)
            {
                shaderFeatures |= ShaderFeatures.MainLightShadows;

                if (shadows != ShadowQuality.HardOnly)
                    shaderFeatures |= ShaderFeatures.SoftShadows;
            }
            shaderFeatures |= ShaderFeatures.AdditionalLightShadows;

            // These are both always "on", and dictated by the number of lights
            shaderFeatures |= ShaderFeatures.VertexLighting;
            shaderFeatures |= ShaderFeatures.AdditionalLights;
            shaderFeatures |= ShaderFeatures.MixedLighting;

            // As this is settable per-camera (and the graphics settings UI depends on a camera being marked "main camera"),
            // we assume it's on (and strip if the shader doesn't have a deferred pass)
            shaderFeatures |= ShaderFeatures.DeferredShading;
            // Built-in doesn't throw this switch, but the shader library has it, so set it here
            shaderFeatures |= ShaderFeatures.DeferredWithoutAccurateGbufferNormals;

            return shaderFeatures;
        }
    }
}
