using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using System.Runtime.CompilerServices;

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
        AccurateGbufferNormals = (1 << 9),
        ScreenSpaceOcclusion = (1 << 10),
        ScreenSpaceShadows = (1 << 11),
        UseFastSRGBLinearConversion = (1 << 12),
        LightLayers = (1 << 13),
        ReflectionProbeBlending = (1 << 14),
        ReflectionProbeBoxProjection = (1 << 15),
        DBufferMRT1 = (1 << 16),
        DBufferMRT2 = (1 << 17),
        DBufferMRT3 = (1 << 18),
        DecalScreenSpace = (1 << 19),
        DecalGBuffer = (1 << 20),
        DecalNormalBlendLow = (1 << 21),
        DecalNormalBlendMedium = (1 << 22),
        DecalNormalBlendHigh = (1 << 23),
        ClusteredRendering = (1 << 24),
        RenderPassEnabled = (1 << 25),
        MainLightShadowsCascade = (1 << 26),
        DrawProcedural = (1 << 27),
        ScreenSpaceOcclusionAfterOpaque = (1 << 28),
        AdditionalLightsKeepOffVariants = (1 << 29),
        ShadowsKeepOffVariants = (1 << 30),
    }

    [Flags]
    enum VolumeFeatures
    {
        None = 0,
        Calculated = (1 << 0),
        LensDistortion = (1 << 1),
        Bloom = (1 << 2),
        ChromaticAberration = (1 << 3),
        ToneMaping = (1 << 4),
        FilmGrain = (1 << 5),
        DepthOfField = (1 << 6),
        CameraMotionBlur = (1 << 7),
        PaniniProjection = (1 << 8),
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

        LocalKeyword m_MainLightShadows;
        LocalKeyword m_MainLightShadowsCascades;
        LocalKeyword m_MainLightShadowsScreen;
        LocalKeyword m_AdditionalLightsVertex;
        LocalKeyword m_AdditionalLightsPixel;
        LocalKeyword m_AdditionalLightShadows;
        LocalKeyword m_ReflectionProbeBlending;
        LocalKeyword m_ReflectionProbeBoxProjection;
        LocalKeyword m_CastingPunctualLightShadow;
        LocalKeyword m_SoftShadows;
        LocalKeyword m_MixedLightingSubtractive;
        LocalKeyword m_LightmapShadowMixing;
        LocalKeyword m_ShadowsShadowMask;
        LocalKeyword m_Lightmap;
        LocalKeyword m_DynamicLightmap;
        LocalKeyword m_DirectionalLightmap;
        LocalKeyword m_AlphaTestOn;
        LocalKeyword m_DeferredStencil;
        LocalKeyword m_GbufferNormalsOct;
        LocalKeyword m_UseDrawProcedural;
        LocalKeyword m_ScreenSpaceOcclusion;
        LocalKeyword m_UseFastSRGBLinearConversion;
        LocalKeyword m_LightLayers;
        LocalKeyword m_RenderPassEnabled;
        LocalKeyword m_DebugDisplay;
        LocalKeyword m_DBufferMRT1;
        LocalKeyword m_DBufferMRT2;
        LocalKeyword m_DBufferMRT3;
        LocalKeyword m_DecalNormalBlendLow;
        LocalKeyword m_DecalNormalBlendMedium;
        LocalKeyword m_DecalNormalBlendHigh;
        LocalKeyword m_ClusteredRendering;
        LocalKeyword m_EditorVisualization;

        LocalKeyword m_LocalDetailMulx2;
        LocalKeyword m_LocalDetailScaled;
        LocalKeyword m_LocalClearCoat;
        LocalKeyword m_LocalClearCoatMap;

        LocalKeyword m_LensDistortion;
        LocalKeyword m_ChromaticAberration;
        LocalKeyword m_BloomLQ;
        LocalKeyword m_BloomHQ;
        LocalKeyword m_BloomLQDirt;
        LocalKeyword m_BloomHQDirt;
        LocalKeyword m_HdrGrading;
        LocalKeyword m_ToneMapACES;
        LocalKeyword m_ToneMapNeutral;
        LocalKeyword m_FilmGrain;

        Shader m_BokehDepthOfField = Shader.Find("Hidden/Universal Render Pipeline/BokehDepthOfField");
        Shader m_GaussianDepthOfField = Shader.Find("Hidden/Universal Render Pipeline/GaussianDepthOfField");
        Shader m_CameraMotionBlur = Shader.Find("Hidden/Universal Render Pipeline/CameraMotionBlur");
        Shader m_PaniniProjection = Shader.Find("Hidden/Universal Render Pipeline/PaniniProjection");
        Shader m_Bloom = Shader.Find("Hidden/Universal Render Pipeline/Bloom");

        Shader StencilDeferred = Shader.Find("Hidden/Universal Render Pipeline/StencilDeferred");

        int m_TotalVariantsInputCount;
        int m_TotalVariantsOutputCount;

        // Multiple callback may be implemented.
        // The first one executed is the one where callbackOrder is returning the smallest number.
        public int callbackOrder { get { return 0; } }

        LocalKeyword TryGetLocalKeyword(Shader shader, string name)
        {
            return shader.keywordSpace.FindKeyword(name);
        }

        void InitializeLocalShaderKeywords(Shader shader)
        {
            m_MainLightShadows = TryGetLocalKeyword(shader, ShaderKeywordStrings.MainLightShadows);
            m_MainLightShadowsCascades = TryGetLocalKeyword(shader, ShaderKeywordStrings.MainLightShadowCascades);
            m_MainLightShadowsScreen = TryGetLocalKeyword(shader, ShaderKeywordStrings.MainLightShadowScreen);
            m_AdditionalLightsVertex = TryGetLocalKeyword(shader, ShaderKeywordStrings.AdditionalLightsVertex);
            m_AdditionalLightsPixel = TryGetLocalKeyword(shader, ShaderKeywordStrings.AdditionalLightsPixel);
            m_AdditionalLightShadows = TryGetLocalKeyword(shader, ShaderKeywordStrings.AdditionalLightShadows);
            m_ReflectionProbeBlending = TryGetLocalKeyword(shader, ShaderKeywordStrings.ReflectionProbeBlending);
            m_ReflectionProbeBoxProjection = TryGetLocalKeyword(shader, ShaderKeywordStrings.ReflectionProbeBoxProjection);
            m_CastingPunctualLightShadow = TryGetLocalKeyword(shader, ShaderKeywordStrings.CastingPunctualLightShadow);
            m_SoftShadows = TryGetLocalKeyword(shader, ShaderKeywordStrings.SoftShadows);
            m_MixedLightingSubtractive = TryGetLocalKeyword(shader, ShaderKeywordStrings.MixedLightingSubtractive);
            m_LightmapShadowMixing = TryGetLocalKeyword(shader, ShaderKeywordStrings.LightmapShadowMixing);
            m_ShadowsShadowMask = TryGetLocalKeyword(shader, ShaderKeywordStrings.ShadowsShadowMask);
            m_Lightmap = TryGetLocalKeyword(shader, ShaderKeywordStrings.LIGHTMAP_ON);
            m_DynamicLightmap = TryGetLocalKeyword(shader, ShaderKeywordStrings.DYNAMICLIGHTMAP_ON);
            m_DirectionalLightmap = TryGetLocalKeyword(shader, ShaderKeywordStrings.DIRLIGHTMAP_COMBINED);
            m_AlphaTestOn = TryGetLocalKeyword(shader, ShaderKeywordStrings._ALPHATEST_ON);
            m_DeferredStencil = TryGetLocalKeyword(shader, ShaderKeywordStrings._DEFERRED_STENCIL);
            m_GbufferNormalsOct = TryGetLocalKeyword(shader, ShaderKeywordStrings._GBUFFER_NORMALS_OCT);
            m_UseDrawProcedural = TryGetLocalKeyword(shader, ShaderKeywordStrings.UseDrawProcedural);
            m_ScreenSpaceOcclusion = TryGetLocalKeyword(shader, ShaderKeywordStrings.ScreenSpaceOcclusion);
            m_UseFastSRGBLinearConversion = TryGetLocalKeyword(shader, ShaderKeywordStrings.UseFastSRGBLinearConversion);
            m_LightLayers = TryGetLocalKeyword(shader, ShaderKeywordStrings.LightLayers);
            m_RenderPassEnabled = TryGetLocalKeyword(shader, ShaderKeywordStrings.RenderPassEnabled);
            m_DebugDisplay = TryGetLocalKeyword(shader, ShaderKeywordStrings.DEBUG_DISPLAY);
            m_DBufferMRT1 = TryGetLocalKeyword(shader, ShaderKeywordStrings.DBufferMRT1);
            m_DBufferMRT2 = TryGetLocalKeyword(shader, ShaderKeywordStrings.DBufferMRT2);
            m_DBufferMRT3 = TryGetLocalKeyword(shader, ShaderKeywordStrings.DBufferMRT3);
            m_DecalNormalBlendLow = TryGetLocalKeyword(shader, ShaderKeywordStrings.DecalNormalBlendLow);
            m_DecalNormalBlendMedium = TryGetLocalKeyword(shader, ShaderKeywordStrings.DecalNormalBlendMedium);
            m_DecalNormalBlendHigh = TryGetLocalKeyword(shader, ShaderKeywordStrings.DecalNormalBlendHigh);
            m_ClusteredRendering = TryGetLocalKeyword(shader, ShaderKeywordStrings.ClusteredRendering);
            m_EditorVisualization = TryGetLocalKeyword(shader, ShaderKeywordStrings.EDITOR_VISUALIZATION);

            m_LocalDetailMulx2 = TryGetLocalKeyword(shader, ShaderKeywordStrings._DETAIL_MULX2);
            m_LocalDetailScaled = TryGetLocalKeyword(shader, ShaderKeywordStrings._DETAIL_SCALED);
            m_LocalClearCoat = TryGetLocalKeyword(shader, ShaderKeywordStrings._CLEARCOAT);
            m_LocalClearCoatMap = TryGetLocalKeyword(shader, ShaderKeywordStrings._CLEARCOATMAP);

            // Post processing
            m_LensDistortion = TryGetLocalKeyword(shader, ShaderKeywordStrings.Distortion);
            m_ChromaticAberration = TryGetLocalKeyword(shader, ShaderKeywordStrings.ChromaticAberration);
            m_BloomLQ = TryGetLocalKeyword(shader, ShaderKeywordStrings.BloomLQ);
            m_BloomHQ = TryGetLocalKeyword(shader, ShaderKeywordStrings.BloomHQ);
            m_BloomLQDirt = TryGetLocalKeyword(shader, ShaderKeywordStrings.BloomLQDirt);
            m_BloomHQDirt = TryGetLocalKeyword(shader, ShaderKeywordStrings.BloomHQDirt);
            m_HdrGrading = TryGetLocalKeyword(shader, ShaderKeywordStrings.HDRGrading);
            m_ToneMapACES = TryGetLocalKeyword(shader, ShaderKeywordStrings.TonemapACES);
            m_ToneMapNeutral = TryGetLocalKeyword(shader, ShaderKeywordStrings.TonemapNeutral);
            m_FilmGrain = TryGetLocalKeyword(shader, ShaderKeywordStrings.FilmGrain);
        }

        bool IsFeatureEnabled(ShaderFeatures featureMask, ShaderFeatures feature)
        {
            return (featureMask & feature) != 0;
        }

        bool IsFeatureEnabled(VolumeFeatures featureMask, VolumeFeatures feature)
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

        struct StripTool<T> where T : System.Enum
        {
            T m_Features;
            Shader m_Shader;
            ShaderKeywordSet m_KeywordSet;
            ShaderSnippetData m_SnippetData;
            ShaderCompilerPlatform m_ShaderCompilerPlatform;
            bool m_stripUnusedVariants;

            public StripTool(T features, Shader shader, ShaderSnippetData snippetData, in ShaderKeywordSet keywordSet, bool stripUnusedVariants, ShaderCompilerPlatform shaderCompilerPlatform)
            {
                m_Features = features;
                m_Shader = shader;
                m_SnippetData = snippetData;
                m_KeywordSet = keywordSet;
                m_stripUnusedVariants = stripUnusedVariants;
                m_ShaderCompilerPlatform = shaderCompilerPlatform;
            }

            bool ContainsKeyword(in LocalKeyword kw)
            {
                return ShaderUtil.PassHasKeyword(m_Shader, m_SnippetData.pass, kw, m_SnippetData.shaderType, m_ShaderCompilerPlatform);
            }

            public bool StripMultiCompileKeepOffVariant(in LocalKeyword kw, T feature, in LocalKeyword kw2, T feature2, in LocalKeyword kw3, T feature3)
            {
                if (StripMultiCompileKeepOffVariant(kw, feature))
                    return true;
                if (StripMultiCompileKeepOffVariant(kw2, feature2))
                    return true;
                if (StripMultiCompileKeepOffVariant(kw3, feature3))
                    return true;
                return false;
            }

            public bool StripMultiCompile(in LocalKeyword kw, T feature, in LocalKeyword kw2, T feature2, in LocalKeyword kw3, T feature3)
            {
                if (StripMultiCompileKeepOffVariant(kw, feature, kw2, feature2, kw3, feature3))
                    return true;

                bool containsKeywords = ContainsKeyword(kw) && ContainsKeyword(kw2) && ContainsKeyword(kw3);
                bool keywordsDisabled = !m_KeywordSet.IsEnabled(kw) && !m_KeywordSet.IsEnabled(kw2) && !m_KeywordSet.IsEnabled(kw3);
                bool hasAnyFeatureEnabled = m_Features.HasFlag(feature) || m_Features.HasFlag(feature2) || m_Features.HasFlag(feature3);
                if (m_stripUnusedVariants && containsKeywords && keywordsDisabled && hasAnyFeatureEnabled)
                    return true;

                return false;
            }

            public bool StripMultiCompileKeepOffVariant(in LocalKeyword kw, T feature, in LocalKeyword kw2, T feature2)
            {
                if (StripMultiCompileKeepOffVariant(kw, feature))
                    return true;
                if (StripMultiCompileKeepOffVariant(kw2, feature2))
                    return true;
                return false;
            }

            public bool StripMultiCompile(in LocalKeyword kw, T feature, in LocalKeyword kw2, T feature2)
            {
                if (StripMultiCompileKeepOffVariant(kw, feature, kw2, feature2))
                    return true;

                bool containsKeywords = ContainsKeyword(kw) && ContainsKeyword(kw2);
                bool keywordsDisabled = !m_KeywordSet.IsEnabled(kw) && !m_KeywordSet.IsEnabled(kw2);
                bool hasAnyFeatureEnabled = m_Features.HasFlag(feature) || m_Features.HasFlag(feature2);
                if (m_stripUnusedVariants && containsKeywords && keywordsDisabled && hasAnyFeatureEnabled)
                    return true;

                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool StripMultiCompileKeepOffVariant(in LocalKeyword kw, T feature)
            {
                return !m_Features.HasFlag(feature) && m_KeywordSet.IsEnabled(kw);
            }

            public bool StripMultiCompile(in LocalKeyword kw, T feature)
            {
                if (!m_Features.HasFlag(feature))
                {
                    if (m_KeywordSet.IsEnabled(kw))
                        return true;
                }
                else if (m_stripUnusedVariants)
                {
                    if (!m_KeywordSet.IsEnabled(kw) && ContainsKeyword(kw))
                        return true;
                }
                return false;
            }
        }

        bool StripUnusedFeatures(ShaderFeatures features, Shader shader, ShaderSnippetData snippetData, ShaderCompilerData compilerData)
        {
            var globalSettings = UniversalRenderPipelineGlobalSettings.instance;
            bool stripDebugDisplayShaders = !Debug.isDebugBuild || (globalSettings == null || globalSettings.stripDebugVariants);

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

            var stripUnusedVariants = UniversalRenderPipelineGlobalSettings.instance?.stripUnusedVariants == true;
            var stripTool = new StripTool<ShaderFeatures>(features, shader, snippetData, compilerData.shaderKeywordSet, stripUnusedVariants, compilerData.shaderCompilerPlatform);

            // strip main light shadows, cascade and screen variants
            if (IsFeatureEnabled(ShaderFeatures.ShadowsKeepOffVariants, features))
            {
                if (stripTool.StripMultiCompileKeepOffVariant(
                    m_MainLightShadows, ShaderFeatures.MainLightShadows,
                    m_MainLightShadowsCascades, ShaderFeatures.MainLightShadowsCascade,
                    m_MainLightShadowsScreen, ShaderFeatures.ScreenSpaceShadows))
                    return true;
            }
            else
            {
                if (stripTool.StripMultiCompile(
                    m_MainLightShadows, ShaderFeatures.MainLightShadows,
                    m_MainLightShadowsCascades, ShaderFeatures.MainLightShadowsCascade,
                    m_MainLightShadowsScreen, ShaderFeatures.ScreenSpaceShadows))
                    return true;
            }

            // TODO: Strip off variants once we have global soft shadows option for forcing instead as support
            if (stripTool.StripMultiCompileKeepOffVariant(m_SoftShadows, ShaderFeatures.SoftShadows))
                return true;

            // Left for backward compatibility
            if (compilerData.shaderKeywordSet.IsEnabled(m_MixedLightingSubtractive) &&
                !IsFeatureEnabled(features, ShaderFeatures.MixedLighting))
                return true;

            if (stripTool.StripMultiCompile(m_UseFastSRGBLinearConversion, ShaderFeatures.UseFastSRGBLinearConversion))
                return true;

            // Strip here only if mixed lighting is disabled
            // No need to check here if actually used by scenes as this taken care by builtin stripper
            if ((compilerData.shaderKeywordSet.IsEnabled(m_LightmapShadowMixing) ||
                 compilerData.shaderKeywordSet.IsEnabled(m_ShadowsShadowMask)) &&
                !IsFeatureEnabled(features, ShaderFeatures.MixedLighting))
                return true;

            if (compilerData.shaderCompilerPlatform == ShaderCompilerPlatform.GLES20)
            {
                // GLES2 does not support bitwise operations.
                if (compilerData.shaderKeywordSet.IsEnabled(m_LightLayers))
                    return true;
            }
            else
            {
                if (stripTool.StripMultiCompile(m_LightLayers, ShaderFeatures.LightLayers))
                    return true;
            }

            if (stripTool.StripMultiCompile(m_RenderPassEnabled, ShaderFeatures.RenderPassEnabled))
                return true;

            // No additional light shadows
            if (IsFeatureEnabled(ShaderFeatures.ShadowsKeepOffVariants, features))
            {
                if (stripTool.StripMultiCompileKeepOffVariant(m_AdditionalLightShadows, ShaderFeatures.AdditionalLightShadows))
                    return true;
            }
            else
            {
                if (stripTool.StripMultiCompile(m_AdditionalLightShadows, ShaderFeatures.AdditionalLightShadows))
                    return true;
            }

            if (stripTool.StripMultiCompile(m_ReflectionProbeBlending, ShaderFeatures.ReflectionProbeBlending))
                return true;

            if (stripTool.StripMultiCompile(m_ReflectionProbeBoxProjection, ShaderFeatures.ReflectionProbeBoxProjection))
                return true;

            // Shadow caster punctual light strip
            if (snippetData.passType == PassType.ShadowCaster && ShaderUtil.PassHasKeyword(shader, snippetData.pass, m_CastingPunctualLightShadow, snippetData.shaderType, compilerData.shaderCompilerPlatform))
            {
                if (!IsFeatureEnabled(features, ShaderFeatures.AdditionalLightShadows) && compilerData.shaderKeywordSet.IsEnabled(m_CastingPunctualLightShadow))
                    return true;

                bool mainLightShadows =
                    !IsFeatureEnabled(features, ShaderFeatures.MainLightShadows) &&
                    !IsFeatureEnabled(features, ShaderFeatures.MainLightShadowsCascade) &&
                    !IsFeatureEnabled(features, ShaderFeatures.ScreenSpaceShadows);
                if (mainLightShadows && !compilerData.shaderKeywordSet.IsEnabled(m_CastingPunctualLightShadow))
                    return true;
            }

            // Additional light are shaded per-vertex or per-pixel.
            if (IsFeatureEnabled(ShaderFeatures.AdditionalLightsKeepOffVariants, features))
            {
                if (stripTool.StripMultiCompileKeepOffVariant(m_AdditionalLightsVertex, ShaderFeatures.VertexLighting,
                    m_AdditionalLightsPixel, ShaderFeatures.AdditionalLights))
                    return true;
            }
            else
            {
                if (stripTool.StripMultiCompile(m_AdditionalLightsVertex, ShaderFeatures.VertexLighting,
                    m_AdditionalLightsPixel, ShaderFeatures.AdditionalLights))
                    return true;
            }

            if (stripTool.StripMultiCompile(m_ClusteredRendering, ShaderFeatures.ClusteredRendering))
                return true;

            // Screen Space Occlusion
            if (IsFeatureEnabled(features, ShaderFeatures.ScreenSpaceOcclusionAfterOpaque))
            {
                // SSAO after opaque setting requires off variants
                if (stripTool.StripMultiCompileKeepOffVariant(m_ScreenSpaceOcclusion, ShaderFeatures.ScreenSpaceOcclusion))
                    return true;
            }
            else
            {
                if (stripTool.StripMultiCompile(m_ScreenSpaceOcclusion, ShaderFeatures.ScreenSpaceOcclusion))
                    return true;
            }

            // Decal DBuffer
            if (stripTool.StripMultiCompile(
                m_DBufferMRT1, ShaderFeatures.DBufferMRT1,
                m_DBufferMRT2, ShaderFeatures.DBufferMRT2,
                m_DBufferMRT3, ShaderFeatures.DBufferMRT3))
                return true;

            // TODO: Test against lightMode tag instead.
            if (snippetData.passName == kPassNameGBuffer)
            {
                if (!IsFeatureEnabled(features, ShaderFeatures.DeferredShading))
                    return true;
            }

            // Do not strip accurateGbufferNormals on Mobile Vulkan as some GPUs do not support R8G8B8A8_SNorm, which then force us to use accurateGbufferNormals
            if (compilerData.shaderCompilerPlatform != ShaderCompilerPlatform.Vulkan &&
                stripTool.StripMultiCompile(m_GbufferNormalsOct, ShaderFeatures.AccurateGbufferNormals))
                return true;

            if (compilerData.shaderKeywordSet.IsEnabled(m_UseDrawProcedural) &&
                !IsFeatureEnabled(features, ShaderFeatures.DrawProcedural))
                return true;

            // Decal Normal Blend
            if (stripTool.StripMultiCompile(
                m_DecalNormalBlendLow, ShaderFeatures.DecalNormalBlendLow,
                m_DecalNormalBlendMedium, ShaderFeatures.DecalNormalBlendMedium,
                m_DecalNormalBlendHigh, ShaderFeatures.DecalNormalBlendHigh))
                return true;

            return false;
        }

        bool StripVolumeFeatures(VolumeFeatures features, Shader shader, ShaderSnippetData snippetData, ShaderCompilerData compilerData)
        {
            var stripUnusedVariants = UniversalRenderPipelineGlobalSettings.instance?.stripUnusedVariants == true;
            var stripTool = new StripTool<VolumeFeatures>(features, shader, snippetData, compilerData.shaderKeywordSet, stripUnusedVariants, compilerData.shaderCompilerPlatform);

            if (stripTool.StripMultiCompileKeepOffVariant(m_LensDistortion, VolumeFeatures.LensDistortion))
                return true;

            if (stripTool.StripMultiCompileKeepOffVariant(m_ChromaticAberration, VolumeFeatures.ChromaticAberration))
                return true;

            if (stripTool.StripMultiCompileKeepOffVariant(m_BloomLQ, VolumeFeatures.Bloom))
                return true;
            if (stripTool.StripMultiCompileKeepOffVariant(m_BloomHQ, VolumeFeatures.Bloom))
                return true;
            if (stripTool.StripMultiCompileKeepOffVariant(m_BloomLQDirt, VolumeFeatures.Bloom))
                return true;
            if (stripTool.StripMultiCompileKeepOffVariant(m_BloomHQDirt, VolumeFeatures.Bloom))
                return true;

            if (stripTool.StripMultiCompileKeepOffVariant(m_HdrGrading, VolumeFeatures.ToneMaping))
                return true;
            if (stripTool.StripMultiCompileKeepOffVariant(m_ToneMapACES, VolumeFeatures.ToneMaping))
                return true;
            if (stripTool.StripMultiCompileKeepOffVariant(m_ToneMapNeutral, VolumeFeatures.ToneMaping))
                return true;
            if (stripTool.StripMultiCompileKeepOffVariant(m_FilmGrain, VolumeFeatures.FilmGrain))
                return true;

            // Strip post processing shaders
            if (shader == m_BokehDepthOfField && !IsFeatureEnabled(ShaderBuildPreprocessor.volumeFeatures, VolumeFeatures.DepthOfField))
                return true;
            if (shader == m_GaussianDepthOfField && !IsFeatureEnabled(ShaderBuildPreprocessor.volumeFeatures, VolumeFeatures.DepthOfField))
                return true;
            if (shader == m_CameraMotionBlur && !IsFeatureEnabled(ShaderBuildPreprocessor.volumeFeatures, VolumeFeatures.CameraMotionBlur))
                return true;
            if (shader == m_PaniniProjection && !IsFeatureEnabled(ShaderBuildPreprocessor.volumeFeatures, VolumeFeatures.PaniniProjection))
                return true;
            if (shader == m_Bloom && !IsFeatureEnabled(ShaderBuildPreprocessor.volumeFeatures, VolumeFeatures.Bloom))
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

        bool StripUnusedShaders(ShaderFeatures features, Shader shader)
        {
            if (!IsFeatureEnabled(features, ShaderFeatures.DeferredShading))
            {
                if (shader == StencilDeferred)
                    return true;
            }

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

            if (UniversalRenderPipelineGlobalSettings.instance?.stripUnusedVariants == true)
            {
                if (StripUnusedShaders(features, shader))
                    return true;
            }

            // Strip terrain holes
            // TODO: checking for the string name here is expensive
            // maybe we can rename alpha clip keyword name to be specific to terrain?
            if (compilerData.shaderKeywordSet.IsEnabled(m_AlphaTestOn) &&
                !IsFeatureEnabled(features, ShaderFeatures.TerrainHoles) &&
                shader.name.Contains(kTerrainShaderName))
                return true;

            return false;
        }

        void LogShaderVariants(Shader shader, ShaderSnippetData snippetData, ShaderVariantLogLevel logLevel, int prevVariantsCount, int currVariantsCount, double stripTimeMs)
        {
            if (logLevel == ShaderVariantLogLevel.AllShaders || shader.name.Contains("Universal Render Pipeline"))
            {
                float percentageCurrent = (float)currVariantsCount / (float)prevVariantsCount * 100f;
                float percentageTotal = (float)m_TotalVariantsOutputCount / (float)m_TotalVariantsInputCount * 100f;

                string result = string.Format("STRIPPING: {0} ({1} pass) ({2}) -" +
                    " Remaining shader variants = {3}/{4} = {5}% - Total = {6}/{7} = {8}% TimeMs={9}",
                    shader.name, snippetData.passName, snippetData.shaderType.ToString(), currVariantsCount,
                    prevVariantsCount, percentageCurrent, m_TotalVariantsOutputCount, m_TotalVariantsInputCount,
                    percentageTotal, stripTimeMs);
                Debug.Log(result);
            }
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippetData, IList<ShaderCompilerData> compilerDataList)
        {
#if PROFILE_BUILD
            Profiler.BeginSample(k_ProcessShaderTag);
#endif

            UniversalRenderPipelineAsset urpAsset = UniversalRenderPipeline.asset;
            if (urpAsset == null || compilerDataList == null || compilerDataList.Count == 0)
                return;

            m_stripTimer.Start();

            InitializeLocalShaderKeywords(shader);

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

                if (UniversalRenderPipelineGlobalSettings.instance?.stripUnusedPostProcessingVariants == true)
                {
                    if (!removeInput && StripVolumeFeatures(ShaderBuildPreprocessor.volumeFeatures, shader, snippetData, compilerDataList[i]))
                    {
                        removeInput = true;
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

            if (urpAsset.shaderVariantLogLevel != ShaderVariantLogLevel.Disabled)
            {
                m_TotalVariantsInputCount += prevVariantCount;
                m_TotalVariantsOutputCount += compilerDataList.Count;
                LogShaderVariants(shader, snippetData, urpAsset.shaderVariantLogLevel, prevVariantCount, compilerDataList.Count, stripTimeMs);
            }

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

        public static VolumeFeatures volumeFeatures
        {
            get
            {
                if (s_VolumeFeatures == VolumeFeatures.None)
                    FetchAllSupportedFeaturesFromVolumes();
                return s_VolumeFeatures;
            }
        }
        private static VolumeFeatures s_VolumeFeatures;

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
            FetchAllSupportedFeaturesFromVolumes();
#if PROFILE_BUILD
            Profiler.enableBinaryLog = true;
            Profiler.logFile = "profilerlog.raw";
            Profiler.enabled = true;
#endif
        }

        static bool TryGetRenderPipelineAssetsForBuildTarget(BuildTarget buildTarget, List<UniversalRenderPipelineAsset> urps)
        {
            var qualitySettings = new SerializedObject(QualitySettings.GetQualitySettings());
            if (qualitySettings == null)
                return false;

            var property = qualitySettings.FindProperty("m_QualitySettings");
            if (property == null)
                return false;

            var activeBuildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var activeBuildTargetGroupName = activeBuildTargetGroup.ToString();

            for (int i = 0; i < property.arraySize; i++)
            {
                bool isExcluded = false;

                var excludedTargetPlatforms = property.GetArrayElementAtIndex(i).FindPropertyRelative("excludedTargetPlatforms");
                if (excludedTargetPlatforms == null)
                    return false;

                foreach (SerializedProperty excludedTargetPlatform in excludedTargetPlatforms)
                {
                    var excludedBuildTargetGroupName = excludedTargetPlatform.stringValue;
                    if (activeBuildTargetGroupName == excludedBuildTargetGroupName)
                    {
                        Debug.Log($"Excluding quality level {QualitySettings.names[i]} from stripping."); // TODO: remove after QA
                        isExcluded = true;
                        break;
                    }
                }

                if (!isExcluded)
                    urps.Add(QualitySettings.GetRenderPipelineAssetAt(i) as UniversalRenderPipelineAsset);
            }

            return true;
        }

        private static void FetchAllSupportedFeatures()
        {
            List<UniversalRenderPipelineAsset> urps = new List<UniversalRenderPipelineAsset>();
            urps.Add(GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset);

            // TODO: Replace once we have official API for filtering urps per build target
            if (!TryGetRenderPipelineAssetsForBuildTarget(EditorUserBuildSettings.activeBuildTarget, urps))
            {
                // Fallback
                Debug.LogWarning("Shader stripping per enabled quality levels failed! Stripping will use all quality levels. Please report a bug!");
                for (int i = 0; i < QualitySettings.names.Length; i++)
                {
                    urps.Add(QualitySettings.GetRenderPipelineAssetAt(i) as UniversalRenderPipelineAsset);
                }
            }

            s_SupportedFeaturesList.Clear();

            foreach (UniversalRenderPipelineAsset urp in urps)
            {
                if (urp != null)
                {
                    int rendererCount = urp.m_RendererDataList.Length;

                    for (int i = 0; i < rendererCount; ++i)
                        s_SupportedFeaturesList.Add(GetSupportedShaderFeatures(urp, i));
                }
            }
        }

        private static void FetchAllSupportedFeaturesFromVolumes()
        {
            if (UniversalRenderPipelineGlobalSettings.instance?.stripUnusedPostProcessingVariants == false)
                return;

            s_VolumeFeatures = VolumeFeatures.Calculated;
            var guids = AssetDatabase.FindAssets("t:VolumeProfile");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                // We only care what is in assets folder
                if (!path.StartsWith("Assets"))
                    continue;

                var asset = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                if (asset == null)
                    continue;

                if (asset.Has<LensDistortion>())
                    s_VolumeFeatures |= VolumeFeatures.LensDistortion;
                if (asset.Has<Bloom>())
                    s_VolumeFeatures |= VolumeFeatures.Bloom;
                if (asset.Has<Tonemapping>())
                    s_VolumeFeatures |= VolumeFeatures.ToneMaping;
                if (asset.Has<FilmGrain>())
                    s_VolumeFeatures |= VolumeFeatures.FilmGrain;
                if (asset.Has<DepthOfField>())
                    s_VolumeFeatures |= VolumeFeatures.DepthOfField;
                if (asset.Has<MotionBlur>())
                    s_VolumeFeatures |= VolumeFeatures.CameraMotionBlur;
                if (asset.Has<PaniniProjection>())
                    s_VolumeFeatures |= VolumeFeatures.PaniniProjection;
                if (asset.Has<ChromaticAberration>())
                    s_VolumeFeatures |= VolumeFeatures.ChromaticAberration;
            }
        }

        private static ShaderFeatures GetSupportedShaderFeatures(UniversalRenderPipelineAsset pipelineAsset, int rendererIndex)
        {
            ShaderFeatures shaderFeatures;
            shaderFeatures = ShaderFeatures.MainLight;

            if (pipelineAsset.supportsMainLightShadows)
            {
                // User can change cascade count at runtime, so we have to include both of them for now
                shaderFeatures |= ShaderFeatures.MainLightShadows;
                shaderFeatures |= ShaderFeatures.MainLightShadowsCascade;
            }

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
            bool accurateGbufferNormals = false;
            bool clusteredRendering = false;
            bool onlyClusteredRendering = false;
            bool usesRenderPass = false;

            {
                ScriptableRenderer renderer = pipelineAsset.GetRenderer(rendererIndex);
                if (renderer is UniversalRenderer)
                {
                    UniversalRenderer universalRenderer = (UniversalRenderer)renderer;
                    if (universalRenderer.renderingModeRequested == RenderingMode.Deferred)
                    {
                        hasDeferredRenderer |= true;
                        accurateGbufferNormals |= universalRenderer.accurateGbufferNormals;
                        usesRenderPass |= universalRenderer.useRenderPassEnabled;
                    }
                }

                if (!renderer.stripShadowsOffVariants)
                    shaderFeatures |= ShaderFeatures.ShadowsKeepOffVariants;

                if (!renderer.stripAdditionalLightOffVariants)
                    shaderFeatures |= ShaderFeatures.AdditionalLightsKeepOffVariants;

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

                        if (ssao?.afterOpaque ?? false)
                            shaderFeatures |= ShaderFeatures.ScreenSpaceOcclusionAfterOpaque;

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

#if ENABLE_VR && ENABLE_XR_MODULE
                        if (universalRendererData.xrSystemData != null)
                            shaderFeatures |= ShaderFeatures.DrawProcedural;
#endif
                    }
                }

                clusteredRendering |= rendererClustered;
                onlyClusteredRendering &= rendererClustered;
            }

            if (hasDeferredRenderer)
                shaderFeatures |= ShaderFeatures.DeferredShading;

            if (accurateGbufferNormals)
                shaderFeatures |= ShaderFeatures.AccurateGbufferNormals;

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
