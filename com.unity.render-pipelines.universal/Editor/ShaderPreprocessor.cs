using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

#if XR_MANAGEMENT_4_0_1_OR_NEWER
using UnityEditor.XR.Management;
#endif

namespace UnityEditor.Rendering.Universal
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
        UseFastSRGBLinearConversion = (1 << 13),
        LightLayers = (1 << 14),
        ReflectionProbeBlending = (1 << 15),
        ReflectionProbeBoxProjection = (1 << 16),
        DBufferMRT1 = (1 << 17),
        DBufferMRT2 = (1 << 18),
        DBufferMRT3 = (1 << 19),
        DecalScreenSpace = (1 << 20),
        DecalGBuffer = (1 << 21),
        DecalNormalBlendLow = (1 << 22),
        DecalNormalBlendMedium = (1 << 23),
        DecalNormalBlendHigh = (1 << 24),
        ClusteredRendering = (1 << 25),
        RenderPassEnabled = (1 << 26),
    }

    internal class ShaderPreprocessor : IPreprocessShaders
    {
        public static readonly string kPassNameGBuffer = "GBuffer";
        public static readonly string kTerrainShaderName = "Universal Render Pipeline/Terrain/Lit";
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
        ShaderKeyword m_DeferredStencil = new ShaderKeyword(ShaderKeywordStrings._DEFERRED_STENCIL);
        ShaderKeyword m_GbufferNormalsOct = new ShaderKeyword(ShaderKeywordStrings._GBUFFER_NORMALS_OCT);
        ShaderKeyword m_UseDrawProcedural = new ShaderKeyword(ShaderKeywordStrings.UseDrawProcedural);
        ShaderKeyword m_ScreenSpaceOcclusion = new ShaderKeyword(ShaderKeywordStrings.ScreenSpaceOcclusion);
        ShaderKeyword m_UseFastSRGBLinearConversion = new ShaderKeyword(ShaderKeywordStrings.UseFastSRGBLinearConversion);
        ShaderKeyword m_LightLayers = new ShaderKeyword(ShaderKeywordStrings.LightLayers);
        ShaderKeyword m_RenderPassEnabled = new ShaderKeyword(ShaderKeywordStrings.RenderPassEnabled);
        ShaderKeyword m_DebugDisplay = new ShaderKeyword(ShaderKeywordStrings.DEBUG_DISPLAY);
        ShaderKeyword m_DBufferMRT1 = new ShaderKeyword(ShaderKeywordStrings.DBufferMRT1);
        ShaderKeyword m_DBufferMRT2 = new ShaderKeyword(ShaderKeywordStrings.DBufferMRT2);
        ShaderKeyword m_DBufferMRT3 = new ShaderKeyword(ShaderKeywordStrings.DBufferMRT3);
        ShaderKeyword m_DecalNormalBlendLow = new ShaderKeyword(ShaderKeywordStrings.DecalNormalBlendLow);
        ShaderKeyword m_DecalNormalBlendMedium = new ShaderKeyword(ShaderKeywordStrings.DecalNormalBlendMedium);
        ShaderKeyword m_DecalNormalBlendHigh = new ShaderKeyword(ShaderKeywordStrings.DecalNormalBlendHigh);
        ShaderKeyword m_ClusteredRendering = new ShaderKeyword(ShaderKeywordStrings.ClusteredRendering);
        ShaderKeyword m_EditorVisualization = new ShaderKeyword(ShaderKeywordStrings.EDITOR_VISUALIZATION);

        ShaderKeyword m_LocalDetailMulx2;
        ShaderKeyword m_LocalDetailScaled;
        ShaderKeyword m_LocalClearCoat;
        ShaderKeyword m_LocalClearCoatMap;

        int m_TotalVariantsInputCount;
        int m_TotalVariantsOutputCount;

        // Multiple callback may be implemented.
        // The first one executed is the one where callbackOrder is returning the smallest number.
        public int callbackOrder { get { return 0; } }

        void InitializeLocalShaderKeywords(Shader shader)
        {
            m_LocalDetailMulx2 = new ShaderKeyword(shader, ShaderKeywordStrings._DETAIL_MULX2);
            m_LocalDetailScaled = new ShaderKeyword(shader, ShaderKeywordStrings._DETAIL_SCALED);
            m_LocalClearCoat = new ShaderKeyword(shader, ShaderKeywordStrings._CLEARCOAT);
            m_LocalClearCoatMap = new ShaderKeyword(shader, ShaderKeywordStrings._CLEARCOATMAP);
        }

        bool IsFeatureEnabled(ShaderFeatures featureMask, ShaderFeatures feature)
        {
            return (featureMask & feature) != 0;
        }

        bool StripUnusedPass(ShaderFeatures features, ShaderSnippetData snippetData)
        {
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

            // DBuffer
            if (snippetData.passName == DecalShaderPassNames.DBufferMesh || snippetData.passName == DecalShaderPassNames.DBufferProjector ||
                snippetData.passName == DecalShaderPassNames.DecalMeshForwardEmissive || snippetData.passName == DecalShaderPassNames.DecalProjectorForwardEmissive)
                if (!IsFeatureEnabled(features, ShaderFeatures.DBufferMRT1) && !IsFeatureEnabled(features, ShaderFeatures.DBufferMRT2) && !IsFeatureEnabled(features, ShaderFeatures.DBufferMRT3))
                    return true;

            // Decal Screen Space
            if (snippetData.passName == DecalShaderPassNames.DecalScreenSpaceMesh || snippetData.passName == DecalShaderPassNames.DecalScreenSpaceProjector)
                if (!IsFeatureEnabled(features, ShaderFeatures.DecalScreenSpace))
                    return true;

            // Decal GBuffer
            if (snippetData.passName == DecalShaderPassNames.DecalGBufferMesh || snippetData.passName == DecalShaderPassNames.DecalGBufferProjector)
                if (!IsFeatureEnabled(features, ShaderFeatures.DecalGBuffer))
                    return true;

            return false;
        }

        bool StripUnusedFeatures(ShaderFeatures features, Shader shader, ShaderSnippetData snippetData, ShaderCompilerData compilerData)
        {
            var globalSettings = UniversalRenderPipelineGlobalSettings.instance;
            bool stripDebugDisplayShaders = !Debug.isDebugBuild || (globalSettings == null || !globalSettings.supportRuntimeDebugDisplay);

#if XR_MANAGEMENT_4_0_1_OR_NEWER
            var buildTargetSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (buildTargetSettings != null && buildTargetSettings.AssignedSettings != null && buildTargetSettings.AssignedSettings.activeLoaders.Count > 0)
            {
                stripDebugDisplayShaders = true;
            }
#endif

            if (stripDebugDisplayShaders && compilerData.shaderKeywordSet.IsEnabled(m_DebugDisplay))
            {
                return true;
            }

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

            if (compilerData.shaderKeywordSet.IsEnabled(m_UseFastSRGBLinearConversion) &&
                !IsFeatureEnabled(features, ShaderFeatures.UseFastSRGBLinearConversion))
                return true;

            // Strip here only if mixed lighting is disabled
            // No need to check here if actually used by scenes as this taken care by builtin stripper
            if ((compilerData.shaderKeywordSet.IsEnabled(m_LightmapShadowMixing) ||
                 compilerData.shaderKeywordSet.IsEnabled(m_ShadowsShadowMask)) &&
                !IsFeatureEnabled(features, ShaderFeatures.MixedLighting))
                return true;

            if (compilerData.shaderKeywordSet.IsEnabled(m_LightLayers) &&
                !IsFeatureEnabled(features, ShaderFeatures.LightLayers))
                return true;

            if (compilerData.shaderKeywordSet.IsEnabled(m_RenderPassEnabled) &&
                !IsFeatureEnabled(features, ShaderFeatures.RenderPassEnabled))
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
            bool clusteredRendering = IsFeatureEnabled(features, ShaderFeatures.ClusteredRendering);
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

            if (!clusteredRendering && compilerData.shaderKeywordSet.IsEnabled(m_ClusteredRendering))
                return true;

            // Screen Space Shadows
            if (!IsFeatureEnabled(features, ShaderFeatures.ScreenSpaceShadows) &&
                compilerData.shaderKeywordSet.IsEnabled(m_MainLightShadowsScreen))
                return true;

            // Screen Space Occlusion
            if (!IsFeatureEnabled(features, ShaderFeatures.ScreenSpaceOcclusion) &&
                compilerData.shaderKeywordSet.IsEnabled(m_ScreenSpaceOcclusion))
                return true;

            // Decal DBuffer
            if (!IsFeatureEnabled(features, ShaderFeatures.DBufferMRT1) &&
                compilerData.shaderKeywordSet.IsEnabled(m_DBufferMRT1))
                return true;
            if (!IsFeatureEnabled(features, ShaderFeatures.DBufferMRT2) &&
                compilerData.shaderKeywordSet.IsEnabled(m_DBufferMRT2))
                return true;
            if (!IsFeatureEnabled(features, ShaderFeatures.DBufferMRT3) &&
                compilerData.shaderKeywordSet.IsEnabled(m_DBufferMRT3))
                return true;

            // Decal Normal Blend
            if (!IsFeatureEnabled(features, ShaderFeatures.DecalNormalBlendLow) &&
                compilerData.shaderKeywordSet.IsEnabled(m_DecalNormalBlendLow))
                return true;
            if (!IsFeatureEnabled(features, ShaderFeatures.DecalNormalBlendMedium) &&
                compilerData.shaderKeywordSet.IsEnabled(m_DecalNormalBlendMedium))
                return true;
            if (!IsFeatureEnabled(features, ShaderFeatures.DecalNormalBlendHigh) &&
                compilerData.shaderKeywordSet.IsEnabled(m_DecalNormalBlendHigh))
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
                // VertexID - as GLES2 does not support VertexID that is required for full screen draw procedural pass;
                if (compilerData.shaderKeywordSet.IsEnabled(m_UseDrawProcedural))
                    return true;

                // Cascade shadows
                if (compilerData.shaderKeywordSet.IsEnabled(m_MainLightShadowsCascades))
                    return true;

                // Screen space shadows
                if (compilerData.shaderKeywordSet.IsEnabled(m_MainLightShadowsScreen))
                    return true;

                // Detail
                if (compilerData.shaderKeywordSet.IsEnabled(m_LocalDetailMulx2) || compilerData.shaderKeywordSet.IsEnabled(m_LocalDetailScaled))
                    return true;

                // Clear Coat
                if (compilerData.shaderKeywordSet.IsEnabled(m_LocalClearCoat) || compilerData.shaderKeywordSet.IsEnabled(m_LocalClearCoatMap))
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
            if (isAdditionalShadow && !(compilerData.shaderKeywordSet.IsEnabled(m_AdditionalLightsPixel) || compilerData.shaderKeywordSet.IsEnabled(m_ClusteredRendering) || compilerData.shaderKeywordSet.IsEnabled(m_DeferredStencil)))
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

            // TODO: Test against lightMode tag instead.
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

            // Local Keywords need to be initialized with the shader
            InitializeLocalShaderKeywords(shader);

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

            if (urpAsset.shaderVariantLogLevel != ShaderVariantLogLevel.Disabled)
            {
                m_TotalVariantsInputCount += prevVariantCount;
                m_TotalVariantsOutputCount += compilerDataList.Count;
                LogShaderVariants(shader, snippetData, urpAsset.shaderVariantLogLevel, prevVariantCount, compilerDataList.Count);
            }
            m_stripTimer.Stop();
            double stripTimeMs = m_stripTimer.Elapsed.TotalMilliseconds;
            m_stripTimer.Reset();

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
            List<UniversalRenderPipelineAsset> urps = new List<UniversalRenderPipelineAsset>();
            urps.Add(GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset);
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                urps.Add(QualitySettings.GetRenderPipelineAssetAt(i) as UniversalRenderPipelineAsset);
            }

            s_SupportedFeaturesList.Clear();

            foreach (UniversalRenderPipelineAsset urp in urps)
            {
                if (urp != null)
                {
                    s_SupportedFeaturesList.Add(GetSupportedShaderFeatures(urp));
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
                shaderFeatures |= ShaderFeatures.VertexLighting;
            }
            else if (pipelineAsset.additionalLightsRenderingMode == LightRenderingMode.PerPixel)
            {
                shaderFeatures |= ShaderFeatures.AdditionalLights;
            }

            bool anyShadows = pipelineAsset.supportsMainLightShadows ||
                (shaderFeatures & ShaderFeatures.AdditionalLightShadows) != 0;
            if (pipelineAsset.supportsSoftShadows && anyShadows)
                shaderFeatures |= ShaderFeatures.SoftShadows;

            if (pipelineAsset.supportsMixedLighting)
                shaderFeatures |= ShaderFeatures.MixedLighting;

            if (pipelineAsset.supportsTerrainHoles)
                shaderFeatures |= ShaderFeatures.TerrainHoles;

            if (pipelineAsset.useFastSRGBLinearConversion)
                shaderFeatures |= ShaderFeatures.UseFastSRGBLinearConversion;

            if (pipelineAsset.supportsLightLayers)
                shaderFeatures |= ShaderFeatures.LightLayers;

            bool hasScreenSpaceShadows = false;
            bool hasScreenSpaceOcclusion = false;
            bool hasDeferredRenderer = false;
            bool withAccurateGbufferNormals = false;
            bool withoutAccurateGbufferNormals = false;
            bool clusteredRendering = false;
            bool onlyClusteredRendering = false;
            bool usesRenderPass = false;

            int rendererCount = pipelineAsset.m_RendererDataList.Length;
            for (int rendererIndex = 0; rendererIndex < rendererCount; ++rendererIndex)
            {
                ScriptableRenderer renderer = pipelineAsset.GetRenderer(rendererIndex);
                if (renderer is UniversalRenderer)
                {
                    UniversalRenderer universalRenderer = (UniversalRenderer)renderer;
                    if (universalRenderer.renderingMode == RenderingMode.Deferred)
                    {
                        hasDeferredRenderer |= true;
                        withAccurateGbufferNormals |= universalRenderer.accurateGbufferNormals;
                        withoutAccurateGbufferNormals |= !universalRenderer.accurateGbufferNormals;
                        usesRenderPass |= universalRenderer.useRenderPassEnabled;
                    }
                }

                var rendererClustered = false;

                ScriptableRendererData rendererData = pipelineAsset.m_RendererDataList[rendererIndex];
                if (rendererData != null)
                {
                    for (int rendererFeatureIndex = 0; rendererFeatureIndex < rendererData.rendererFeatures.Count; rendererFeatureIndex++)
                    {
                        ScriptableRendererFeature rendererFeature = rendererData.rendererFeatures[rendererFeatureIndex];

                        ScreenSpaceShadows ssshadows = rendererFeature as ScreenSpaceShadows;
                        hasScreenSpaceShadows |= ssshadows != null;

                        // Check for Screen Space Ambient Occlusion Renderer Feature
                        ScreenSpaceAmbientOcclusion ssao = rendererFeature as ScreenSpaceAmbientOcclusion;
                        hasScreenSpaceOcclusion |= ssao != null;

                        // Check for Decal Renderer Feature
                        DecalRendererFeature decal = rendererFeature as DecalRendererFeature;
                        if (decal != null)
                        {
                            var technique = decal.GetTechnique(renderer);
                            switch (technique)
                            {
                                case DecalTechnique.DBuffer:
                                    shaderFeatures |= GetFromDecalSurfaceData(decal.GetDBufferSettings().surfaceData);
                                    break;
                                case DecalTechnique.ScreenSpace:
                                    shaderFeatures |= GetFromNormalBlend(decal.GetScreenSpaceSettings().normalBlend);
                                    shaderFeatures |= ShaderFeatures.DecalScreenSpace;
                                    break;
                                case DecalTechnique.GBuffer:
                                    shaderFeatures |= GetFromNormalBlend(decal.GetScreenSpaceSettings().normalBlend);
                                    shaderFeatures |= ShaderFeatures.DecalGBuffer;
                                    break;
                            }
                        }
                    }

                    if (rendererData is UniversalRendererData universalRendererData)
                    {
                        rendererClustered = universalRendererData.renderingMode == RenderingMode.Forward &&
                            universalRendererData.clusteredRendering;
                    }
                }

                clusteredRendering |= rendererClustered;
                onlyClusteredRendering &= rendererClustered;
            }

            if (hasDeferredRenderer)
                shaderFeatures |= ShaderFeatures.DeferredShading;

            // We can only strip accurateGbufferNormals related variants if all DeferredRenderers use the same option.
            if (withAccurateGbufferNormals)
                shaderFeatures |= ShaderFeatures.DeferredWithAccurateGbufferNormals;

            if (withoutAccurateGbufferNormals)
                shaderFeatures |= ShaderFeatures.DeferredWithoutAccurateGbufferNormals;

            if (hasScreenSpaceShadows)
                shaderFeatures |= ShaderFeatures.ScreenSpaceShadows;

            if (hasScreenSpaceOcclusion)
                shaderFeatures |= ShaderFeatures.ScreenSpaceOcclusion;

            if (usesRenderPass)
                shaderFeatures |= ShaderFeatures.RenderPassEnabled;

            if (pipelineAsset.reflectionProbeBlending)
                shaderFeatures |= ShaderFeatures.ReflectionProbeBlending;

            if (pipelineAsset.reflectionProbeBoxProjection)
                shaderFeatures |= ShaderFeatures.ReflectionProbeBoxProjection;

            if (clusteredRendering)
            {
                shaderFeatures |= ShaderFeatures.ClusteredRendering;
            }

            if (onlyClusteredRendering)
            {
                shaderFeatures &= ~(ShaderFeatures.AdditionalLights | ShaderFeatures.VertexLighting);
            }

            if (pipelineAsset.additionalLightsRenderingMode == LightRenderingMode.PerPixel || clusteredRendering)
            {
                if (pipelineAsset.supportsAdditionalLightShadows)
                {
                    shaderFeatures |= ShaderFeatures.AdditionalLightShadows;
                }
            }

            return shaderFeatures;
        }

        private static ShaderFeatures GetFromDecalSurfaceData(DecalSurfaceData surfaceData)
        {
            ShaderFeatures shaderFeatures = ShaderFeatures.None;
            switch (surfaceData)
            {
                case DecalSurfaceData.Albedo:
                    shaderFeatures |= ShaderFeatures.DBufferMRT1;
                    break;
                case DecalSurfaceData.AlbedoNormal:
                    shaderFeatures |= ShaderFeatures.DBufferMRT2;
                    break;
                case DecalSurfaceData.AlbedoNormalMAOS:
                    shaderFeatures |= ShaderFeatures.DBufferMRT3;
                    break;
            }
            return shaderFeatures;
        }

        private static ShaderFeatures GetFromNormalBlend(DecalNormalBlend normalBlend)
        {
            ShaderFeatures shaderFeatures = ShaderFeatures.None;
            switch (normalBlend)
            {
                case DecalNormalBlend.Low:
                    shaderFeatures |= ShaderFeatures.DecalNormalBlendLow;
                    break;
                case DecalNormalBlend.Medium:
                    shaderFeatures |= ShaderFeatures.DecalNormalBlendMedium;
                    break;
                case DecalNormalBlend.High:
                    shaderFeatures |= ShaderFeatures.DecalNormalBlendHigh;
                    break;
            }
            return shaderFeatures;
        }
    }
}
