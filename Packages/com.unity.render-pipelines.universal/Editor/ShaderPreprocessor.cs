using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
#if XR_MANAGEMENT_4_0_1_OR_NEWER
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;
#endif
using ShaderPrefilteringData = UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.ShaderPrefilteringData;
using PrefilteringMode = UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.PrefilteringMode;
using PrefilteringModeAdditionalLights = UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.PrefilteringModeAdditionalLights;

namespace UnityEditor.Rendering.Universal
{
    [Flags]
    enum ShaderFeatures : long
    {
        None = 0,
        MainLight = (1L << 0),
        MainLightShadows = (1L << 1),
        AdditionalLights = (1L << 2),
        AdditionalLightShadows = (1L << 3),
        VertexLighting = (1L << 4),
        SoftShadows = (1L << 5),
        MixedLighting = (1L << 6),
        TerrainHoles = (1L << 7),
        DeferredShading = (1L << 8), // DeferredRenderer is in the list of renderer
        AccurateGbufferNormals = (1L << 9),
        ScreenSpaceOcclusion = (1L << 10),
        ScreenSpaceShadows = (1L << 11),
        UseFastSRGBLinearConversion = (1L << 12),
        LightLayers = (1L << 13),
        ReflectionProbeBlending = (1L << 14),
        ReflectionProbeBoxProjection = (1L << 15),
        DBufferMRT1 = (1L << 16),
        DBufferMRT2 = (1L << 17),
        DBufferMRT3 = (1L << 18),
        DecalScreenSpace = (1L << 19),
        DecalGBuffer = (1L << 20),
        DecalNormalBlendLow = (1L << 21),
        DecalNormalBlendMedium = (1L << 22),
        DecalNormalBlendHigh = (1L << 23),
        ForwardPlus = (1L << 24),
        RenderPassEnabled = (1L << 25),
        MainLightShadowsCascade = (1L << 26),
        DrawProcedural = (1L << 27),
        ScreenSpaceOcclusionAfterOpaque = (1L << 28),
        AdditionalLightsKeepOffVariants = (1L << 29),
        ShadowsKeepOffVariants = (1L << 30),
        // Unused = (1L << 31),
        DecalLayers = (1L << 32),
        OpaqueWriteRenderingLayers = (1L << 33),
        GBufferWriteRenderingLayers = (1L << 34),
        DepthNormalPassRenderingLayers = (1L << 35),
        LightCookies = (1L << 36),
        ProbeVolumeOff = (1L << 37),
        ProbeVolumeL1 = (1L << 38),
        ProbeVolumeL2 = (1L << 39),
        HdrGrading = (1L << 40),
    }

    [Flags]
    enum VolumeFeatures
    {
        None = 0,
        Calculated = (1 << 0),
        LensDistortion = (1 << 1),
        Bloom = (1 << 2),
        ChromaticAberration = (1 << 3),
        ToneMapping = (1 << 4),
        FilmGrain = (1 << 5),
        DepthOfField = (1 << 6),
        CameraMotionBlur = (1 << 7),
        PaniniProjection = (1 << 8),
    }

    internal class ShaderPreprocessor : IShaderVariantStripper, IShaderVariantStripperScope
    {
        public bool active => UniversalRenderPipeline.asset != null;

        public static readonly string kPassNameUniversal2D = "Universal2D";
        public static readonly string kPassNameGBuffer = "GBuffer";
        public static readonly string kPassNameForwardLit = "ForwardLit";
        public static readonly string kPassNameDepthNormals = "DepthNormals";


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
        LocalKeyword m_ScreenSpaceOcclusion;
        LocalKeyword m_UseFastSRGBLinearConversion;
        LocalKeyword m_LightLayers;
        LocalKeyword m_DecalLayers;
        LocalKeyword m_WriteRenderingLayers;
        LocalKeyword m_RenderPassEnabled;
        LocalKeyword m_DebugDisplay;
        LocalKeyword m_DBufferMRT1;
        LocalKeyword m_DBufferMRT2;
        LocalKeyword m_DBufferMRT3;
        LocalKeyword m_DecalNormalBlendLow;
        LocalKeyword m_DecalNormalBlendMedium;
        LocalKeyword m_DecalNormalBlendHigh;
        LocalKeyword m_ForwardPlus;
        LocalKeyword m_FoveatedRenderingNonUniformRaster;
        LocalKeyword m_EditorVisualization;
        LocalKeyword m_LightCookies;

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
        LocalKeyword m_ScreenCoordOverride;
        LocalKeyword m_ProbeVolumesOff;
        LocalKeyword m_ProbeVolumesL1;
        LocalKeyword m_ProbeVolumesL2;

        Shader m_BokehDepthOfField = Shader.Find("Hidden/Universal Render Pipeline/BokehDepthOfField");
        Shader m_GaussianDepthOfField = Shader.Find("Hidden/Universal Render Pipeline/GaussianDepthOfField");
        Shader m_CameraMotionBlur = Shader.Find("Hidden/Universal Render Pipeline/CameraMotionBlur");
        Shader m_PaniniProjection = Shader.Find("Hidden/Universal Render Pipeline/PaniniProjection");
        Shader m_Bloom = Shader.Find("Hidden/Universal Render Pipeline/Bloom");
        Shader m_TerrainLit = Shader.Find("Universal Render Pipeline/Terrain/Lit");
        Shader StencilDeferred = Shader.Find("Hidden/Universal Render Pipeline/StencilDeferred");

        LocalKeyword TryGetLocalKeyword(Shader shader, string name)
        {
            return shader.keywordSpace.FindKeyword(name);
        }

        public void InitializeLocalShaderKeywords([DisallowNull] Shader shader)
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
            m_ScreenSpaceOcclusion = TryGetLocalKeyword(shader, ShaderKeywordStrings.ScreenSpaceOcclusion);
            m_UseFastSRGBLinearConversion = TryGetLocalKeyword(shader, ShaderKeywordStrings.UseFastSRGBLinearConversion);
            m_LightLayers = TryGetLocalKeyword(shader, ShaderKeywordStrings.LightLayers);
            m_DecalLayers = TryGetLocalKeyword(shader, ShaderKeywordStrings.DecalLayers);
            m_WriteRenderingLayers = TryGetLocalKeyword(shader, ShaderKeywordStrings.WriteRenderingLayers);
            m_RenderPassEnabled = TryGetLocalKeyword(shader, ShaderKeywordStrings.RenderPassEnabled);
            m_DebugDisplay = TryGetLocalKeyword(shader, ShaderKeywordStrings.DEBUG_DISPLAY);
            m_DBufferMRT1 = TryGetLocalKeyword(shader, ShaderKeywordStrings.DBufferMRT1);
            m_DBufferMRT2 = TryGetLocalKeyword(shader, ShaderKeywordStrings.DBufferMRT2);
            m_DBufferMRT3 = TryGetLocalKeyword(shader, ShaderKeywordStrings.DBufferMRT3);
            m_DecalNormalBlendLow = TryGetLocalKeyword(shader, ShaderKeywordStrings.DecalNormalBlendLow);
            m_DecalNormalBlendMedium = TryGetLocalKeyword(shader, ShaderKeywordStrings.DecalNormalBlendMedium);
            m_DecalNormalBlendHigh = TryGetLocalKeyword(shader, ShaderKeywordStrings.DecalNormalBlendHigh);
            m_ForwardPlus = TryGetLocalKeyword(shader, ShaderKeywordStrings.ForwardPlus);
            m_FoveatedRenderingNonUniformRaster = TryGetLocalKeyword(shader, ShaderKeywordStrings.FoveatedRenderingNonUniformRaster);
            m_EditorVisualization = TryGetLocalKeyword(shader, ShaderKeywordStrings.EDITOR_VISUALIZATION);
            m_LightCookies = TryGetLocalKeyword(shader, ShaderKeywordStrings.LightCookies);

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

            m_ScreenCoordOverride = TryGetLocalKeyword(shader, ShaderKeywordStrings.SCREEN_COORD_OVERRIDE);

            m_ProbeVolumesOff = TryGetLocalKeyword(shader, "PROBE_VOLUMES_OFF");
            m_ProbeVolumesL1 = TryGetLocalKeyword(shader, "PROBE_VOLUMES_L1");
            m_ProbeVolumesL2 = TryGetLocalKeyword(shader, "PROBE_VOLUMES_L2");
        }

        bool IsFeatureEnabled(ShaderFeatures featureMask, ShaderFeatures feature)
        {
            return (featureMask & feature) != 0;
        }

        bool IsFeatureEnabled(VolumeFeatures featureMask, VolumeFeatures feature)
        {
            return (featureMask & feature) != 0;
        }

        bool IsGLDevice(ShaderCompilerData variantData)
        {
            return variantData.shaderCompilerPlatform == ShaderCompilerPlatform.GLES3x || variantData.shaderCompilerPlatform == ShaderCompilerPlatform.OpenGLCore;
        }

        struct StripTool<T> where T : Enum
        {
            T m_Features;
            Shader m_Shader;
            ShaderKeywordSet m_KeywordSet;
            ShaderSnippetData m_passData;
            ShaderCompilerPlatform m_ShaderCompilerPlatform;

            public StripTool(T features, Shader shader, ShaderSnippetData passData, in ShaderKeywordSet keywordSet, ShaderCompilerPlatform shaderCompilerPlatform)
            {
                m_Features = features;
                m_Shader = shader;
                m_passData = passData;
                m_KeywordSet = keywordSet;
                m_ShaderCompilerPlatform = shaderCompilerPlatform;
            }

            bool ContainsKeyword(in LocalKeyword kw)
            {
                return ShaderUtil.PassHasKeyword(m_Shader, m_passData.pass, kw, m_passData.shaderType, m_ShaderCompilerPlatform);
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

                if (ShaderBuildPreprocessor.s_StripUnusedVariants)
                {
                    bool containsKeywords = ContainsKeyword(kw) && ContainsKeyword(kw2) && ContainsKeyword(kw3);
                    bool keywordsDisabled = !m_KeywordSet.IsEnabled(kw) && !m_KeywordSet.IsEnabled(kw2) && !m_KeywordSet.IsEnabled(kw3);
                    bool hasAnyFeatureEnabled = m_Features.HasFlag(feature) || m_Features.HasFlag(feature2) || m_Features.HasFlag(feature3);
                    if (containsKeywords && keywordsDisabled && hasAnyFeatureEnabled)
                        return true;
                }

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

                if (ShaderBuildPreprocessor.s_StripUnusedVariants)
                {
                    bool containsKeywords = ContainsKeyword(kw) && ContainsKeyword(kw2);
                    bool keywordsDisabled = !m_KeywordSet.IsEnabled(kw) && !m_KeywordSet.IsEnabled(kw2);
                    bool hasAnyFeatureEnabled = m_Features.HasFlag(feature) || m_Features.HasFlag(feature2);
                    if (containsKeywords && keywordsDisabled && hasAnyFeatureEnabled)
                        return true;
                }

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
                else if (ShaderBuildPreprocessor.s_StripUnusedVariants)
                {
                    if (!m_KeywordSet.IsEnabled(kw) && ContainsKeyword(kw))
                        return true;
                }
                return false;
            }
        }

        bool StripUnusedFeatures(ShaderFeatures features, Shader shader, ShaderSnippetData passData, ShaderCompilerData variantData)
        {
            bool stripDebugDisplayShaders = ShaderBuildPreprocessor.s_StripDebugDisplayShaders;
            if (stripDebugDisplayShaders && variantData.shaderKeywordSet.IsEnabled(m_DebugDisplay))
                return true;

            if (ShaderBuildPreprocessor.s_StripScreenCoordOverrideVariants && variantData.shaderKeywordSet.IsEnabled(m_ScreenCoordOverride))
                return true;

            var stripTool = new StripTool<ShaderFeatures>(features, shader, passData, variantData.shaderKeywordSet, variantData.shaderCompilerPlatform);

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

            if (stripTool.StripMultiCompileKeepOffVariant(m_HdrGrading, ShaderFeatures.HdrGrading))
                return true;

            // Left for backward compatibility
            if (variantData.shaderKeywordSet.IsEnabled(m_MixedLightingSubtractive) &&
                !IsFeatureEnabled(features, ShaderFeatures.MixedLighting))
                return true;

            if (stripTool.StripMultiCompile(m_UseFastSRGBLinearConversion, ShaderFeatures.UseFastSRGBLinearConversion))
                return true;

            // Strip here only if mixed lighting is disabled
            // No need to check here if actually used by scenes as this taken care by builtin stripper
            if ((variantData.shaderKeywordSet.IsEnabled(m_LightmapShadowMixing) ||
                 variantData.shaderKeywordSet.IsEnabled(m_ShadowsShadowMask)) &&
                !IsFeatureEnabled(features, ShaderFeatures.MixedLighting))
                return true;

            if (stripTool.StripMultiCompile(m_LightLayers, ShaderFeatures.LightLayers))
                return true;

            if (stripTool.StripMultiCompile(m_RenderPassEnabled, ShaderFeatures.RenderPassEnabled))
                return true;

            // TODO: Add this scriptable stripping back with SVC work. 10.049
            // No additional light shadows
           /* if (IsFeatureEnabled(ShaderFeatures.ShadowsKeepOffVariants, features))
            {
                if (stripTool.StripMultiCompileKeepOffVariant(m_AdditionalLightShadows, ShaderFeatures.AdditionalLightShadows))
                    return true;
            }
            else
            {
                // LOD_FADE_CROSSFADE PROBE_VOLUMES_OFF _ADDITIONAL_LIGHT_SHADOWS _ENVIRONMENTREFLECTIONS_OFF _MAIN_LIGHT_SHADOWS _RECEIVE_SHADOWS_OFF _SPECULARHIGHLIGHTS_OFF not found.
                if (stripTool.StripMultiCompileKeepOffVariant(m_AdditionalLightShadows, ShaderFeatures.AdditionalLightShadows))
                    return true;
            }*/

            if (stripTool.StripMultiCompile(m_ReflectionProbeBlending, ShaderFeatures.ReflectionProbeBlending))
                return true;

            if (stripTool.StripMultiCompile(m_ReflectionProbeBoxProjection, ShaderFeatures.ReflectionProbeBoxProjection))
                return true;

            // Shadow caster punctual light strip
            if (passData.passType == PassType.ShadowCaster && ShaderUtil.PassHasKeyword(shader, passData.pass, m_CastingPunctualLightShadow, passData.shaderType, variantData.shaderCompilerPlatform))
            {
                if (!IsFeatureEnabled(features, ShaderFeatures.AdditionalLightShadows) && variantData.shaderKeywordSet.IsEnabled(m_CastingPunctualLightShadow))
                    return true;

                bool mainLightShadows =
                    !IsFeatureEnabled(features, ShaderFeatures.MainLightShadows) &&
                    !IsFeatureEnabled(features, ShaderFeatures.MainLightShadowsCascade) &&
                    !IsFeatureEnabled(features, ShaderFeatures.ScreenSpaceShadows);
                if (mainLightShadows && !variantData.shaderKeywordSet.IsEnabled(m_CastingPunctualLightShadow))
                    return true;
            }

            if (stripTool.StripMultiCompile(m_ForwardPlus, ShaderFeatures.ForwardPlus))
                return true;

            // Additional light are shaded per-vertex or per-pixel.
            if (IsFeatureEnabled(ShaderFeatures.AdditionalLightsKeepOffVariants, features))
            {
                if (stripTool.StripMultiCompileKeepOffVariant(m_AdditionalLightsVertex, ShaderFeatures.VertexLighting, m_AdditionalLightsPixel, ShaderFeatures.AdditionalLights))
                    return true;
            }
            else
            {
                if (stripTool.StripMultiCompile(m_AdditionalLightsVertex, ShaderFeatures.VertexLighting, m_AdditionalLightsPixel, ShaderFeatures.AdditionalLights))
                    return true;
            }

            // Strip Foveated Rendering variants on all platforms (except PS5 and Metal)
            // TODO: add a way to communicate this requirement from the xr plugin directly
#if ENABLE_VR && ENABLE_XR_MODULE
            if (variantData.shaderCompilerPlatform != ShaderCompilerPlatform.PS5NGGC && variantData.shaderCompilerPlatform != ShaderCompilerPlatform.Metal)
#endif
            {
                if (variantData.shaderKeywordSet.IsEnabled(m_FoveatedRenderingNonUniformRaster))
                    return true;
            }

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

            // TODO: Add this scriptable stripping back with SVC work. 13.528
            /*if (IsGLDevice(variantData))
            {
                // Decal DBuffer is not supported on gl
                if (variantData.shaderKeywordSet.IsEnabled(m_DBufferMRT1) ||
                    variantData.shaderKeywordSet.IsEnabled(m_DBufferMRT2) ||
                    variantData.shaderKeywordSet.IsEnabled(m_DBufferMRT3))
                    return true;
            }
            else
            {
                // Decal DBuffer
                if (stripTool.StripMultiCompile(
                        m_DBufferMRT1, ShaderFeatures.DBufferMRT1,
                        m_DBufferMRT2, ShaderFeatures.DBufferMRT2,
                        m_DBufferMRT3, ShaderFeatures.DBufferMRT3))
                    return true;
            }*/

            if (IsGLDevice(variantData))
            {
                // Rendering layers are not supported on gl
                if (variantData.shaderKeywordSet.IsEnabled(m_DecalLayers))
                    return true;
            }
            else
            {
                // Decal Layers
                if (stripTool.StripMultiCompile(m_DecalLayers, ShaderFeatures.DecalLayers))
                    return true;
            }

            // TODO: Test against lightMode tag instead.
            if (passData.passName == kPassNameGBuffer)
            {
                if (!IsFeatureEnabled(features, ShaderFeatures.DeferredShading))
                    return true;
            }

            // Do not strip accurateGbufferNormals on Mobile Vulkan as some GPUs do not support R8G8B8A8_SNorm, which then force us to use accurateGbufferNormals
            if (variantData.shaderCompilerPlatform != ShaderCompilerPlatform.Vulkan &&
                stripTool.StripMultiCompile(m_GbufferNormalsOct, ShaderFeatures.AccurateGbufferNormals))
                return true;

            // Decal Normal Blend
            if (stripTool.StripMultiCompile(
                m_DecalNormalBlendLow, ShaderFeatures.DecalNormalBlendLow,
                m_DecalNormalBlendMedium, ShaderFeatures.DecalNormalBlendMedium,
                m_DecalNormalBlendHigh, ShaderFeatures.DecalNormalBlendHigh))
                return true;

            if (stripTool.StripMultiCompileKeepOffVariant(m_LightCookies, ShaderFeatures.LightCookies))
                return true;

            // Write Rendering Layers
            if (IsGLDevice(variantData))
            {
                // Rendering layers are not supported on gl
                if (variantData.shaderKeywordSet.IsEnabled(m_WriteRenderingLayers))
                    return true;
            }
            else
            {
                if (passData.passName == kPassNameDepthNormals)
                {
                    if (stripTool.StripMultiCompile(m_WriteRenderingLayers, ShaderFeatures.DepthNormalPassRenderingLayers))
                        return true;
                }
                if (passData.passName == kPassNameForwardLit)
                {
                    if (stripTool.StripMultiCompile(m_WriteRenderingLayers, ShaderFeatures.OpaqueWriteRenderingLayers))
                        return true;
                }
                if (passData.passName == kPassNameGBuffer)
                {
                    if (stripTool.StripMultiCompile(m_WriteRenderingLayers, ShaderFeatures.GBufferWriteRenderingLayers))
                        return true;
                }
            }

            if (stripTool.StripMultiCompileKeepOffVariant(m_ProbeVolumesL1, ShaderFeatures.ProbeVolumeL1, m_ProbeVolumesL2, ShaderFeatures.ProbeVolumeL2))
                return true;

            return false;
        }

        bool StripVolumeFeatures(VolumeFeatures features, Shader shader, ShaderSnippetData passData, ShaderCompilerData variantData)
        {
            var stripTool = new StripTool<VolumeFeatures>(features, shader, passData, variantData.shaderKeywordSet, variantData.shaderCompilerPlatform);

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

            if (stripTool.StripMultiCompileKeepOffVariant(m_ToneMapACES, VolumeFeatures.ToneMapping))
                return true;
            if (stripTool.StripMultiCompileKeepOffVariant(m_ToneMapNeutral, VolumeFeatures.ToneMapping))
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

        bool StripUnsupportedVariants(ShaderCompilerData variantData)
        {
            // We can strip variants that have directional lightmap enabled but not static nor dynamic lightmap.
            if (variantData.shaderKeywordSet.IsEnabled(m_DirectionalLightmap) &&
                !(variantData.shaderKeywordSet.IsEnabled(m_Lightmap) || variantData.shaderKeywordSet.IsEnabled(m_DynamicLightmap)))
                return true;

            // We can strip shaders where both lightmaps and probe volumes are enabled
            if ((variantData.shaderKeywordSet.IsEnabled(m_Lightmap) || variantData.shaderKeywordSet.IsEnabled(m_DynamicLightmap)) &&
                (variantData.shaderKeywordSet.IsEnabled(m_ProbeVolumesL1) || variantData.shaderKeywordSet.IsEnabled(m_ProbeVolumesL2)))
                return true;

            // Editor visualization is only used in scene view debug modes.
            if (variantData.shaderKeywordSet.IsEnabled(m_EditorVisualization))
                return true;

            return false;
        }

        bool StripInvalidVariants(ShaderFeatures features, Shader shader, ShaderCompilerData variantData)
        {
            // HDR Output
            if (!HDROutputUtils.IsShaderVariantValid(variantData.shaderKeywordSet, PlayerSettings.useHDRDisplay))
                return true;

            // Strip terrain holes
            if (shader == m_TerrainLit
                && !IsFeatureEnabled(features, ShaderFeatures.TerrainHoles)
                && variantData.shaderKeywordSet.IsEnabled(m_AlphaTestOn)
               )
                return true;

            // Strip Additional Light Shadows when disabled and not using ForwardPlus or Deferred...
            bool areAdditionalShadowsEnabled = variantData.shaderKeywordSet.IsEnabled(m_AdditionalLightShadows);
            if (areAdditionalShadowsEnabled && !(variantData.shaderKeywordSet.IsEnabled(m_AdditionalLightsPixel) || variantData.shaderKeywordSet.IsEnabled(m_ForwardPlus) || variantData.shaderKeywordSet.IsEnabled(m_DeferredStencil)))
                return true;

            // Strip Soft Shadows if shadows are disabled for both Main and Additional Lights...
            bool isMainShadowNoCascades = variantData.shaderKeywordSet.IsEnabled(m_MainLightShadows);
            bool isMainShadowCascades = variantData.shaderKeywordSet.IsEnabled(m_MainLightShadowsCascades);
            bool isMainShadowScreen = variantData.shaderKeywordSet.IsEnabled(m_MainLightShadowsScreen);
            bool isMainShadow = isMainShadowNoCascades || isMainShadowCascades || isMainShadowScreen;
            bool isShadowVariant = isMainShadow || areAdditionalShadowsEnabled;
            if (!isShadowVariant && variantData.shaderKeywordSet.IsEnabled(m_SoftShadows))
                return true;

            return false;
        }

        bool StripUnusedPass(ShaderFeatures features, ShaderSnippetData passData, ShaderCompilerData variantData)
        {
            if (ShaderBuildPreprocessor.s_Strip2DPasses)
            {
                if (passData.passName == kPassNameUniversal2D)
                    return true;
            }

            // Meta pass is needed in the player for Enlighten Precomputed Realtime GI albedo and emission.
            if (passData.passType == PassType.Meta)
            {
                if (SupportedRenderingFeatures.active.enlighten == false ||
                    ((int)SupportedRenderingFeatures.active.lightmapBakeTypes | (int)LightmapBakeType.Realtime) == 0)
                    return true;
            }

            if (passData.passType == PassType.ShadowCaster)
                if (!IsFeatureEnabled(features, ShaderFeatures.MainLightShadows) && !IsFeatureEnabled(features, ShaderFeatures.AdditionalLightShadows))
                    return true;

            // Do not strip GL passes as there are only screen space forward
            if (variantData.shaderCompilerPlatform != ShaderCompilerPlatform.GLES3x && variantData.shaderCompilerPlatform != ShaderCompilerPlatform.OpenGLCore)
            {
                // DBuffer
                if (passData.passName == DecalShaderPassNames.DBufferMesh || passData.passName == DecalShaderPassNames.DBufferProjector ||
                    passData.passName == DecalShaderPassNames.DecalMeshForwardEmissive || passData.passName == DecalShaderPassNames.DecalProjectorForwardEmissive)
                    if (!IsFeatureEnabled(features, ShaderFeatures.DBufferMRT1) && !IsFeatureEnabled(features, ShaderFeatures.DBufferMRT2) && !IsFeatureEnabled(features, ShaderFeatures.DBufferMRT3))
                        return true;

                // Decal Screen Space
                if (passData.passName == DecalShaderPassNames.DecalScreenSpaceMesh || passData.passName == DecalShaderPassNames.DecalScreenSpaceProjector)
                    if (!IsFeatureEnabled(features, ShaderFeatures.DecalScreenSpace))
                        return true;

                // Decal GBuffer
                if (passData.passName == DecalShaderPassNames.DecalGBufferMesh || passData.passName == DecalShaderPassNames.DecalGBufferProjector)
                    if (!IsFeatureEnabled(features, ShaderFeatures.DecalGBuffer))
                        return true;
            }

            return false;
        }

        bool StripUnusedShaders(ShaderFeatures features, Shader shader)
        {
            if (!ShaderBuildPreprocessor.s_StripUnusedVariants)
                return false;

            if (shader == StencilDeferred && !IsFeatureEnabled(features, ShaderFeatures.DeferredShading))
                return true;

            return false;
        }

        bool StripUnused(ShaderFeatures features, Shader shader, ShaderSnippetData passData, ShaderCompilerData variantData)
        {
            if (StripUnusedShaders(features, shader))
                return true;

            if (StripUnusedPass(features, passData, variantData))
                return true;

            if (StripInvalidVariants(features, shader, variantData))
                return true;

            if (StripUnsupportedVariants(variantData))
                return true;

            if (StripUnusedFeatures(features, shader, passData, variantData))
                return true;

            return false;
        }

        public bool CanRemoveVariant([DisallowNull] Shader shader, ShaderSnippetData passData, ShaderCompilerData variantData)
        {
            bool removeInput = true;
            for (var index = 0; index < ShaderBuildPreprocessor.supportedFeaturesList.Count; index++)
            {
                ShaderFeatures supportedFeatures = ShaderBuildPreprocessor.supportedFeaturesList[index];

                // All feature sets need to have this variant set unused to be stripped out.
                bool shouldStrip = StripUnused(supportedFeatures, shader, passData, variantData);
                if (shouldStrip)
                    continue;

                removeInput = false;
                break;
            }

            if (!removeInput && ShaderBuildPreprocessor.s_StripUnusedPostProcessingVariants)
                if (StripVolumeFeatures(ShaderBuildPreprocessor.volumeFeatures, shader, passData, variantData))
                    removeInput = true;

            return removeInput;
        }

        public void BeforeShaderStripping(Shader shader)
        {
            InitializeLocalShaderKeywords(shader);
        }

        public void AfterShaderStripping(Shader shader) {}
    }

    // *********************************************************************************
    // Preprocess Build class used to determine the shader features used in the project.
    // *********************************************************************************
    class ShaderBuildPreprocessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        // Public
        public int callbackOrder => 0;
        public static bool s_StripUnusedVariants;
        public static bool s_StripDebugDisplayShaders;
        public static bool s_StripUnusedPostProcessingVariants;
        public static bool s_StripScreenCoordOverrideVariants;
        public static bool s_Strip2DPasses;

        public static List<ShaderFeatures> supportedFeaturesList => s_SupportedFeaturesList;

        public static VolumeFeatures volumeFeatures
        {
            get
            {
                if (s_VolumeFeatures == VolumeFeatures.None)
                    GetSupportedFeaturesFromVolumes();
                return s_VolumeFeatures;
            }
        }

        // Private
        private static bool s_StripXRVariants;
        private static bool s_KeepOffVariantForAdditionalLights;
        private static VolumeFeatures s_VolumeFeatures;
        private static List<ShaderFeatures> s_SupportedFeaturesList = new List<ShaderFeatures>();

        // Struct used to contain data used in various functions
        // while determining the features used in the build.
        private struct StrippingData
        {
            public bool isUsingDeferred;
            public bool isUsing2D;
            public UniversalRenderPipelineAsset urpAsset;
            public ShaderFeatures shaderFeatures;
            public ShaderFeatures urpAssetShaderFeatures;
            public ShaderPrefilteringData prefilteringData;
            public ScriptableRenderer renderer;
            public UniversalRenderer urpRenderer;
            public ScriptableRendererData rendererData;
            public UniversalRendererData universalRendererData;
            public ScriptableRendererFeature rendererFeature;

            public StrippingData(UniversalRenderPipelineAsset pipelineAsset, bool stripXR, bool stripHDR, bool stripDebug, bool stripScreenCoord)
            {
                isUsingDeferred = false;
                isUsing2D = false;
                urpAsset = pipelineAsset;
                shaderFeatures = new ShaderFeatures();
                urpAssetShaderFeatures = new ShaderFeatures();
                prefilteringData = new ShaderPrefilteringData()
                {
                    forwardPrefilteringMode = PrefilteringMode.Remove,
                    forwardPlusPrefilteringMode = PrefilteringMode.Remove,
                    deferredPrefilteringMode = PrefilteringMode.Remove,

                    stripXRKeywords = stripXR,
                    stripHDRKeywords = stripHDR,
                    stripDebugDisplay = stripDebug,
                    stripWriteRenderingLayers = true,
                    stripScreenCoordOverride = stripScreenCoord,
                    additionalLightsPrefilteringMode = PrefilteringModeAdditionalLights.SelectPixelAndOff,
                    additionalLightsShadowsPrefilteringMode = PrefilteringMode.SelectOnly,
                    stripDBufferMRT1 = true,
                    stripDBufferMRT2 = true,
                    stripDBufferMRT3 = true,
                    stripNativeRenderPass = true,

                    screenSpaceOcclusionPrefilteringMode = PrefilteringMode.Remove,
                    stripSSAOBlueNoise = true,
                    stripSSAOInterleaved = true,
                    stripSSAODepthNormals = true,
                    stripSSAOSourceDepthLow = true,
                    stripSSAOSourceDepthMedium = true,
                    stripSSAOSourceDepthHigh = true,
                    stripSSAOSampleCountLow = true,
                    stripSSAOSampleCountMedium = true,
                    stripSSAOSampleCountHigh = true,
                };
                renderer = null;
                urpRenderer = null;
                rendererData = null;
                universalRendererData = null;
                rendererFeature = null;
            }
        }

        // Called before the build is started...
        public void OnPreprocessBuild(BuildReport report)
        {
            GatherGlobalAndPlatformSettings(report);
            GetSupportedFeaturesFromVolumes();
            GetSupportedShaderFeaturesFromAssets();

#if PROFILE_BUILD
            Profiler.enableBinaryLog = true;
            Profiler.logFile = "profilerlog.raw";
            Profiler.enabled = true;
#endif
        }

        public void OnPostprocessBuild(BuildReport report)
        {
#if PROFILE_BUILD
            Profiler.enabled = false;
#endif
        }

        // Retrieves the global and platform settings used in the project...
        private static void GatherGlobalAndPlatformSettings(BuildReport report)
        {
            bool isDevelopmentBuild = (report.summary.options & BuildOptions.Development) != 0;

            UniversalRenderPipelineGlobalSettings globalSettings = UniversalRenderPipelineGlobalSettings.instance;
            if (globalSettings)
            {
                s_StripUnusedPostProcessingVariants = globalSettings.stripUnusedPostProcessingVariants;
                s_StripDebugDisplayShaders = !isDevelopmentBuild || globalSettings.stripDebugVariants;
                s_StripUnusedVariants = globalSettings.stripUnusedVariants;
                s_StripScreenCoordOverrideVariants = globalSettings.stripScreenCoordOverrideVariants;
            }
            else
            {
                s_StripDebugDisplayShaders = true;
            }

#if XR_MANAGEMENT_4_0_1_OR_NEWER
            // XR Stripping
            XRGeneralSettings generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
            s_StripXRVariants = generalSettings == null || generalSettings.Manager == null || generalSettings.Manager.activeLoaders.Count <= 0;

            var buildTargetSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (buildTargetSettings != null && buildTargetSettings.AssignedSettings != null && buildTargetSettings.AssignedSettings.activeLoaders.Count > 0)
                s_StripDebugDisplayShaders = true;

            // Additional Lights for XR...

            // XRTODO: We need to figure out what's the proper way to detect HL target platform when building.
            // For now, HL is the only XR platform available on WSA so we assume this case targets HL platform.
            // Due to the performance consideration, keep additional light off variant to avoid extra ALU cost related to dummy additional light handling.
            XRGeneralSettings wsaTargetSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.WSA);
            if (wsaTargetSettings != null && wsaTargetSettings.AssignedSettings != null && wsaTargetSettings.AssignedSettings.activeLoaders.Count > 0)
                s_KeepOffVariantForAdditionalLights = true;

            XRGeneralSettings questTargetSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (questTargetSettings != null && questTargetSettings.AssignedSettings != null && questTargetSettings.AssignedSettings.activeLoaders.Count > 0)
                s_KeepOffVariantForAdditionalLights = true;
#else
            s_StripXRVariants = true;
#endif
        }

        // Checks each Volume Profile Assets for used features...
        private static void GetSupportedFeaturesFromVolumes()
        {
            if (!s_StripUnusedPostProcessingVariants)
                return;

            s_VolumeFeatures = VolumeFeatures.Calculated;
            string[] guids = AssetDatabase.FindAssets("t:VolumeProfile");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // We only care what is in assets folder
                if (!path.StartsWith("Assets"))
                    continue;

                VolumeProfile asset = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                if (asset == null)
                    continue;

                if (asset.Has<LensDistortion>())
                    s_VolumeFeatures |= VolumeFeatures.LensDistortion;
                if (asset.Has<Bloom>())
                    s_VolumeFeatures |= VolumeFeatures.Bloom;
                if (asset.Has<Tonemapping>())
                    s_VolumeFeatures |= VolumeFeatures.ToneMapping;
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

        // Checks each Universal Render Pipeline Asset for features used...
        private static void GetSupportedShaderFeaturesFromAssets()
        {
            using (ListPool<UniversalRenderPipelineAsset>.Get(out List<UniversalRenderPipelineAsset> urpAssets))
            {
                bool success = EditorUserBuildSettings.activeBuildTarget.TryGetRenderPipelineAssets(urpAssets);
                if (!success)
                {
                    Debug.LogError("Unable to get UniversalRenderPipelineAssets from EditorUserBuildSettings.activeBuildTarget.");
                    return;
                }

                // Get Supported features & update data used for Shader Prefiltering
                s_Strip2DPasses = true;
                s_SupportedFeaturesList.Clear();
                for (int urpAssetIndex = 0; urpAssetIndex < urpAssets.Count; urpAssetIndex++)
                {
                    UniversalRenderPipelineAsset urpAsset = urpAssets[urpAssetIndex];
                    if (urpAsset == null)
                        continue;

                    // Set some initial values for the data
                    StrippingData data = new(
                        urpAsset,
                        s_StripXRVariants,
                        !PlayerSettings.useHDRDisplay || !urpAsset.supportsHDR,
                        s_StripDebugDisplayShaders,
                        s_StripScreenCoordOverrideVariants
                    );

                    // Check the asset for supported features
                    data.urpAssetShaderFeatures = GetSupportedShaderFeaturesFromAsset(ref data);

                    // Check each renderer & renderer feature
                    GetSupportedShaderFeaturesFromRenderers(ref data);

                    // Update the Shader Prefiltering data and send it to the URP Asset
                    urpAsset.UpdateShaderKeywordPrefiltering(ref data.prefilteringData);

                    // Update whether 2D passes can be stripped
                    s_Strip2DPasses &= !data.isUsing2D;

                    EditorUtility.SetDirty(urpAsset);
                }
                AssetDatabase.SaveAssets();
            }
        }

        // Checks the assigned Universal Pipeline Asset for features used...
        private static ShaderFeatures GetSupportedShaderFeaturesFromAsset(ref StrippingData data)
        {
            ShaderFeatures shaderFeatures = ShaderFeatures.MainLight;
            ref UniversalRenderPipelineAsset urpAsset = ref data.urpAsset;
            ref ShaderPrefilteringData prefilteringData = ref data.prefilteringData;

            shaderFeatures |= ShaderFeatures.MainLightShadows;
            if (urpAsset.supportsMainLightShadows)
            {
                // User can change cascade count at runtime, so we have to include both of them for now
                shaderFeatures |= ShaderFeatures.MainLightShadowsCascade;
            }

            switch (urpAsset.additionalLightsRenderingMode)
            {
                case LightRenderingMode.PerVertex:
                    shaderFeatures |= ShaderFeatures.VertexLighting;
                    break;
                case LightRenderingMode.PerPixel:
                    shaderFeatures |= ShaderFeatures.AdditionalLights;
                    break;
                case LightRenderingMode.Disabled:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (urpAsset.supportsMixedLighting)
                shaderFeatures |= ShaderFeatures.MixedLighting;

            if (urpAsset.supportsTerrainHoles)
                shaderFeatures |= ShaderFeatures.TerrainHoles;

            if (urpAsset.useFastSRGBLinearConversion)
                shaderFeatures |= ShaderFeatures.UseFastSRGBLinearConversion;

            if (urpAsset.useRenderingLayers)
                shaderFeatures |= ShaderFeatures.LightLayers;

            if (urpAsset.supportsLightCookies)
                shaderFeatures |= ShaderFeatures.LightCookies;

            bool anyShadows = urpAsset.supportsMainLightShadows || (shaderFeatures & ShaderFeatures.AdditionalLightShadows) != 0;
            if (urpAsset.supportsSoftShadows && anyShadows)
                shaderFeatures |= ShaderFeatures.SoftShadows;

            if (urpAsset.colorGradingMode == ColorGradingMode.HighDynamicRange)
                shaderFeatures |= ShaderFeatures.HdrGrading;

            // Main Light Shadows...
            prefilteringData.stripMainLightShadows = !urpAsset.supportsMainLightShadows;

            // Additional Lights and Shadows...
            switch (urpAsset.additionalLightsRenderingMode)
            {
                case LightRenderingMode.PerVertex:
                    prefilteringData.additionalLightsPrefilteringMode = s_StripXRVariants ? PrefilteringModeAdditionalLights.SelectVertex : PrefilteringModeAdditionalLights.SelectVertexAndOff;
                    prefilteringData.additionalLightsShadowsPrefilteringMode = PrefilteringMode.Remove;
                    break;
                case LightRenderingMode.PerPixel:
                    prefilteringData.additionalLightsPrefilteringMode = s_StripXRVariants ? PrefilteringModeAdditionalLights.SelectPixel : PrefilteringModeAdditionalLights.SelectPixelAndOff;
                    prefilteringData.additionalLightsShadowsPrefilteringMode = PrefilteringMode.SelectOnly;
                    break;
                case LightRenderingMode.Disabled:
                    prefilteringData.additionalLightsPrefilteringMode = PrefilteringModeAdditionalLights.Remove;
                    prefilteringData.additionalLightsShadowsPrefilteringMode = PrefilteringMode.Remove;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return shaderFeatures;
        }

        // Checks each Universal Renderer in the assigned URP Asset for features used...
        private static void GetSupportedShaderFeaturesFromRenderers(ref StrippingData data)
        {
            ref ShaderPrefilteringData prefilteringData = ref data.prefilteringData;
            ref UniversalRenderPipelineAsset urpAsset = ref data.urpAsset;

            bool everyRendererUsesSSAO = true;
            ScriptableRendererData[] rendererDataArray = urpAsset.m_RendererDataList;
            for (int rendererIndex = 0; rendererIndex < rendererDataArray.Length; ++rendererIndex)
            {
                data.renderer = urpAsset.GetRenderer(rendererIndex);
                data.rendererData = rendererDataArray[rendererIndex];
                data.universalRendererData = (data.rendererData != null) ? data.rendererData as UniversalRendererData : null;
                data.isUsing2D |= data.renderer is Renderer2D;

                // Get & add Supported features from renderers used for
                // Scriptable Stripping and update the prefiltering data.
                GetSupportedShaderFeaturesFromRenderer(ref data);
                s_SupportedFeaturesList.Add(data.shaderFeatures);

                // Check to see if it's possible to remove the OFF variant for SSAO
                everyRendererUsesSSAO &= prefilteringData.screenSpaceOcclusionPrefilteringMode == PrefilteringMode.Select;
            }

            // Remove the SSAO's OFF variant if Global Settings allow it and every renderer uses it.
            if (s_StripUnusedVariants && everyRendererUsesSSAO)
                prefilteringData.screenSpaceOcclusionPrefilteringMode = PrefilteringMode.SelectOnly;

            // Check if only Deferred or F+ renderers are in the URP Asset to remove off variants...
            if (prefilteringData.forwardPrefilteringMode == PrefilteringMode.Remove)
            {
                // Only Forward Plus...
                if (prefilteringData.deferredPrefilteringMode == PrefilteringMode.Remove)
                {
                    prefilteringData.forwardPlusPrefilteringMode = PrefilteringMode.SelectOnly;
                    prefilteringData.additionalLightsPrefilteringMode = PrefilteringModeAdditionalLights.Remove;
                }

                // Only Deferred...
                else if (prefilteringData.forwardPlusPrefilteringMode == PrefilteringMode.Remove)
                    prefilteringData.deferredPrefilteringMode = PrefilteringMode.SelectOnly;
            }
        }

        // Checks the assigned Universal renderer for features used...
        private static void GetSupportedShaderFeaturesFromRenderer(ref StrippingData data)
        {
            ref UniversalRenderPipelineAsset urpAsset = ref data.urpAsset;
            ref ShaderPrefilteringData prefilteringData = ref data.prefilteringData;
            data.shaderFeatures = data.urpAssetShaderFeatures;
            ref ShaderFeatures shaderFeatures = ref data.shaderFeatures;

            bool accurateGbufferNormals = false;
            bool isUsingForwardPlus = false;
            bool usesRenderPass = false;
            if (data.rendererData != null)
            {
                if (data.universalRendererData != null)
                {
                    switch (data.universalRendererData.renderingMode)
                    {
                        case RenderingMode.Forward:
                            prefilteringData.forwardPrefilteringMode = PrefilteringMode.Select;
                            break;
                        case RenderingMode.ForwardPlus:
                            isUsingForwardPlus = true;
                            prefilteringData.forwardPlusPrefilteringMode = PrefilteringMode.Select;
                            break;
                        case RenderingMode.Deferred:
                            data.isUsingDeferred = true;
                            prefilteringData.deferredPrefilteringMode = PrefilteringMode.Select;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    #if ENABLE_VR && ENABLE_XR_MODULE
                    if (data.universalRendererData.xrSystemData != null)
                        shaderFeatures |= ShaderFeatures.DrawProcedural;
                    #endif
                }

                // Check renderer features...
                GetSupportedShaderFeaturesFromRendererFeatures(ref data);
            }

            if (!data.renderer.stripShadowsOffVariants)
                shaderFeatures |= ShaderFeatures.ShadowsKeepOffVariants;

            if (s_KeepOffVariantForAdditionalLights || !data.renderer.stripAdditionalLightOffVariants)
            {
                shaderFeatures |= ShaderFeatures.AdditionalLightsKeepOffVariants;

                if (prefilteringData.additionalLightsShadowsPrefilteringMode == PrefilteringMode.SelectOnly)
                    prefilteringData.additionalLightsShadowsPrefilteringMode = PrefilteringMode.Select;
            }

            // For deferred and Forward+ we need to keep the OFF variant for additional light shadows...
            if (data.isUsingDeferred || isUsingForwardPlus)
            {
                if (prefilteringData.additionalLightsShadowsPrefilteringMode == PrefilteringMode.SelectOnly)
                    prefilteringData.additionalLightsShadowsPrefilteringMode = PrefilteringMode.Select;
            }

            if (isUsingForwardPlus)
            {
                shaderFeatures |= ShaderFeatures.ForwardPlus;
                {
                    shaderFeatures &= ~(ShaderFeatures.AdditionalLights | ShaderFeatures.VertexLighting);
                }
            }

            shaderFeatures |= ShaderFeatures.AdditionalLightShadows;

            if (data.isUsingDeferred)
            {
                shaderFeatures |= ShaderFeatures.DeferredShading;

                if (urpAsset.useRenderingLayers)
                {
                    shaderFeatures |= ShaderFeatures.GBufferWriteRenderingLayers;
                    prefilteringData.stripWriteRenderingLayers = false;
                }

                if (data.renderer is UniversalRenderer universalRenderer)
                {
                    accurateGbufferNormals |= universalRenderer.accurateGbufferNormals;
                    usesRenderPass |= universalRenderer.useRenderPassEnabled;
                }
            }

            if (accurateGbufferNormals)
                shaderFeatures |= ShaderFeatures.AccurateGbufferNormals;

            if (usesRenderPass)
            {
                shaderFeatures |= ShaderFeatures.RenderPassEnabled;
                prefilteringData.stripNativeRenderPass = false;
            }

            if (urpAsset.reflectionProbeBlending)
                shaderFeatures |= ShaderFeatures.ReflectionProbeBlending;

            if (urpAsset.reflectionProbeBoxProjection)
                shaderFeatures |= ShaderFeatures.ReflectionProbeBoxProjection;

            // Used for removing Decals' MRT keywords if possible.
            prefilteringData.stripDBufferMRT1 &= !IsFeatureEnabled(shaderFeatures, ShaderFeatures.DBufferMRT1);
            prefilteringData.stripDBufferMRT2 &= !IsFeatureEnabled(shaderFeatures, ShaderFeatures.DBufferMRT2);
            prefilteringData.stripDBufferMRT3 &= !IsFeatureEnabled(shaderFeatures, ShaderFeatures.DBufferMRT3);
        }

        // Checks each Universal Renderer Feature in the assigned renderer...
        private static void GetSupportedShaderFeaturesFromRendererFeatures(ref StrippingData data)
        {
            bool usesRenderingLayers = false;
            RenderingLayerUtils.Event renderingLayersEvent = RenderingLayerUtils.Event.Opaque;
            for (int rendererFeatureIndex = 0; rendererFeatureIndex < data.rendererData.rendererFeatures.Count; rendererFeatureIndex++)
            {
                data.rendererFeature = data.rendererData.rendererFeatures[rendererFeatureIndex];

                // We don't add disabled renderer features if "Strip Unused Variants" is enabled.
                if (s_StripUnusedVariants && !data.rendererFeature.isActive)
                    continue;

                // Rendering Layers...
                if (data.universalRendererData != null && data.rendererFeature.RequireRenderingLayers(data.isUsingDeferred, out RenderingLayerUtils.Event rendererEvent, out _))
                {
                    usesRenderingLayers = true;
                    RenderingLayerUtils.CombineRendererEvents(data.isUsingDeferred, data.urpAsset.msaaSampleCount, rendererEvent, ref renderingLayersEvent);
                }

                // Check the remaining Renderer Features...
                GetSupportedShaderFeaturesFromRendererFeature(ref data);
            }

            data.prefilteringData.stripWriteRenderingLayers &= !usesRenderingLayers;

            // If using rendering layers, enable the appropriate feature
            if (usesRenderingLayers)
            {
                switch (renderingLayersEvent)
                {
                    case RenderingLayerUtils.Event.DepthNormalPrePass:
                        data.shaderFeatures |= ShaderFeatures.DepthNormalPassRenderingLayers;
                        break;

                    case RenderingLayerUtils.Event.Opaque:
                        data.shaderFeatures |= data.isUsingDeferred ? ShaderFeatures.GBufferWriteRenderingLayers : ShaderFeatures.OpaqueWriteRenderingLayers;
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        // Checks the assigned Universal Renderer Features for features used...
        private static void GetSupportedShaderFeaturesFromRendererFeature(ref StrippingData data)
        {
            ref ShaderFeatures shaderFeatures = ref data.shaderFeatures;
            ref ShaderPrefilteringData prefilteringData = ref data.prefilteringData;

            // Screen Space Shadows...
            ScreenSpaceShadows sssFeature = data.rendererFeature as ScreenSpaceShadows;
            if (sssFeature != null)
            {
                // Add it if it's enabled or if unused variants should not be stripped...
                if (sssFeature.isActive || !s_StripUnusedVariants)
                    shaderFeatures |= ShaderFeatures.ScreenSpaceShadows;

                return;
            }

            // Screen Space Ambient Occlusion (SSAO)...
            // Removing the OFF variant requires every renderer to use SSAO. That is checked later.
            ScreenSpaceAmbientOcclusion ssaoFeature = data.rendererFeature as ScreenSpaceAmbientOcclusion;
            if (ssaoFeature != null)
            {
                shaderFeatures |= ShaderFeatures.ScreenSpaceOcclusion;

                ScreenSpaceAmbientOcclusionSettings ssaoSettings = ssaoFeature.settings;
                bool isUsingDepthNormals = ssaoSettings.Source == ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals;
                prefilteringData.stripSSAODepthNormals      &= !isUsingDepthNormals;
                prefilteringData.stripSSAOSourceDepthLow    &= isUsingDepthNormals || ssaoSettings.NormalSamples != ScreenSpaceAmbientOcclusionSettings.NormalQuality.Low;
                prefilteringData.stripSSAOSourceDepthMedium &= isUsingDepthNormals || ssaoSettings.NormalSamples != ScreenSpaceAmbientOcclusionSettings.NormalQuality.Medium;
                prefilteringData.stripSSAOSourceDepthHigh   &= isUsingDepthNormals || ssaoSettings.NormalSamples != ScreenSpaceAmbientOcclusionSettings.NormalQuality.High;
                prefilteringData.stripSSAOBlueNoise         &= ssaoSettings.AOMethod != ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise;
                prefilteringData.stripSSAOInterleaved       &= ssaoSettings.AOMethod != ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.InterleavedGradient;
                prefilteringData.stripSSAOSampleCountLow    &= ssaoSettings.Samples != ScreenSpaceAmbientOcclusionSettings.AOSampleOption.Low;
                prefilteringData.stripSSAOSampleCountMedium &= ssaoSettings.Samples != ScreenSpaceAmbientOcclusionSettings.AOSampleOption.Medium;
                prefilteringData.stripSSAOSampleCountHigh   &= ssaoSettings.Samples != ScreenSpaceAmbientOcclusionSettings.AOSampleOption.High;

                // Keep _SCREEN_SPACE_OCCLUSION and the Off variant when stripping of unused variants is disabled
                if (!s_StripUnusedVariants)
                {
                    shaderFeatures |= ShaderFeatures.ScreenSpaceOcclusionAfterOpaque;
                    prefilteringData.screenSpaceOcclusionPrefilteringMode = PrefilteringMode.Select;
                }

                // If SSAO feature is active with After Opaque disabled, select it...
                else if (ssaoFeature.isActive && !ssaoSettings.AfterOpaque)
                    prefilteringData.screenSpaceOcclusionPrefilteringMode = PrefilteringMode.Select;

                // Otherwise remove it as the it will not be used
                else
                {
                    shaderFeatures |= ShaderFeatures.ScreenSpaceOcclusionAfterOpaque;
                    prefilteringData.screenSpaceOcclusionPrefilteringMode = PrefilteringMode.Remove;
                }

                return;
            }

            // Decals...
            DecalRendererFeature decal = data.rendererFeature as DecalRendererFeature;
            if (decal != null)
            {
                DecalTechnique technique = decal.GetTechnique(data.renderer);
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
                        //data.shaderFeatures |= ShaderFeatures.DecalScreenSpace; // In case deferred is not supported it will fallback to forward
                        break;
                }

                if (decal.requiresDecalLayers)
                    shaderFeatures |= ShaderFeatures.DecalLayers;
            }
        }

        // Retrieves the correct feature used from the Decal Surface Data Settings...
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

        // Retrieves the correct feature used from the Decal Normal Blend Settings...
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

        private static bool IsFeatureEnabled(ShaderFeatures featureMask, ShaderFeatures feature)
        {
            return (featureMask & feature) != 0;
        }
    }
}
