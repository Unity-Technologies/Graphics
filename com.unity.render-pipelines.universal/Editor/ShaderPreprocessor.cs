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
        MainLightShadowsCascade = (1 << 27),
        DrawProcedural = (1 << 28),
    }

    [Flags]
    enum VolumeFeatures
    {
        None = 0,
        Calculated = (1 << 1),
        LensDistortion = (1 << 2),
        Bloom = (1 << 3),
        CHROMATIC_ABERRATION = (1 << 4),
        ToneMaping = (1 << 5),
        FilmGrain = (1 << 6),
        DepthOfField = (1 << 7),
        CameraMotionBlur = (1 << 8),
        PaniniProjection = (1 << 9),
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

        Shader m_InternalDeferredShading = Shader.Find("Hidden/Internal-DeferredShading");
        Shader m_InternalDeferredReflections = Shader.Find("Hidden/Internal-DeferredReflections");
        Shader m_InternalPrePassLighting = Shader.Find("Hidden/Internal-PrePassLighting");
        Shader m_InternalScreenSpaceShadows = Shader.Find("Hidden/Internal-ScreenSpaceShadows");
        Shader m_InternalFlare = Shader.Find("Hidden/Internal-Flare");
        Shader m_InternalHalo = Shader.Find("Hidden/Internal-Halo");
        Shader m_InternalMotionVectors = Shader.Find("Hidden/Internal-MotionVectors");
        Shader m_BlitFromTex2DToTexArraySlice = Shader.Find("Hidden/VR/BlitFromTex2DToTexArraySlice");
        Shader BlitTexArraySlice = Shader.Find("Hidden/VR/BlitTexArraySlice");
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

        struct StripTool
        {
            Shader m_Shader;
            ShaderFeatures m_Features;
            ShaderKeywordSet keywordSet;
            ShaderSnippetData snippetData;
            bool m_StripDisabledKeywords;

            public StripTool(Shader shader, bool stripDisabledKeywords, ShaderFeatures features, ShaderSnippetData snippetData, in ShaderKeywordSet keywordSet)
            {
                m_Shader = shader;
                m_Features = features;
                this.snippetData = snippetData;
                this.keywordSet = keywordSet;
                m_StripDisabledKeywords = stripDisabledKeywords;
            }

            bool ContainsKeyword(in LocalKeyword kw)
            {
                return ShaderUtil.PassHasKeyword(m_Shader, snippetData.pass, kw, snippetData.shaderType);
            }

            public bool StripFeature(in LocalKeyword kw, ShaderFeatures feature, in LocalKeyword kw2, ShaderFeatures feature2, in LocalKeyword kw3, ShaderFeatures feature3, in LocalKeyword kw4, ShaderFeatures feature4)
            {
                if ((m_Features & feature) == 0 && keywordSet.IsEnabled(kw)) // disabled
                    return true;
                if ((m_Features & feature2) == 0 && keywordSet.IsEnabled(kw2)) // disabled
                    return true;
                if ((m_Features & feature3) == 0 && keywordSet.IsEnabled(kw3)) // disabled
                    return true;
                if ((m_Features & feature4) == 0 && keywordSet.IsEnabled(kw4)) // disabled
                    return true;

                if (m_StripDisabledKeywords && ContainsKeyword(kw) &&
                    ((m_Features & feature) != 0 || (m_Features & feature2) != 0 || (m_Features & feature3) != 0) || (m_Features & feature4) != 0 &&
                    !keywordSet.IsEnabled(kw) && !keywordSet.IsEnabled(kw2) && !keywordSet.IsEnabled(kw3) && !keywordSet.IsEnabled(kw4))
                    return true;

                return false;
            }

            public bool StripFeatureKeepDisabled(in LocalKeyword kw, ShaderFeatures feature, in LocalKeyword kw2, ShaderFeatures feature2, in LocalKeyword kw3, ShaderFeatures feature3)
            {
                if ((m_Features & feature) == 0 && keywordSet.IsEnabled(kw)) // disabled
                    return true;
                if ((m_Features & feature2) == 0 && keywordSet.IsEnabled(kw2)) // disabled
                    return true;
                if ((m_Features & feature3) == 0 && keywordSet.IsEnabled(kw3)) // disabled
                    return true;

                return false;
            }

            public bool StripFeature(in LocalKeyword kw, ShaderFeatures feature, in LocalKeyword kw2, ShaderFeatures feature2, in LocalKeyword kw3, ShaderFeatures feature3)
            {
                if (StripFeatureKeepDisabled(kw, feature, kw2, feature2, kw3, feature3))
                    return true;

                if (m_StripDisabledKeywords && ContainsKeyword(kw) &&
                    ((m_Features & feature) != 0 || (m_Features & feature2) != 0 || (m_Features & feature3) != 0) &&
                    !keywordSet.IsEnabled(kw) && !keywordSet.IsEnabled(kw2) && !keywordSet.IsEnabled(kw3))
                    return true;

                return false;
            }

            public bool StripFeatureKeepDisabled(in LocalKeyword kw, ShaderFeatures feature, in LocalKeyword kw2, ShaderFeatures feature2)
            {
                if ((m_Features & feature) == 0 && keywordSet.IsEnabled(kw)) // disabled
                    return true;
                if ((m_Features & feature2) == 0 && keywordSet.IsEnabled(kw2)) // disabled
                    return true;

                return false;
            }

            public bool StripFeature(in LocalKeyword kw, ShaderFeatures feature, in LocalKeyword kw2, ShaderFeatures feature2)
            {
                if (StripFeatureKeepDisabled(kw, feature, kw2, feature2))
                    return true;

                if (m_StripDisabledKeywords && ContainsKeyword(kw) &&
                    ((m_Features & feature) != 0 || (m_Features & feature2) != 0) &&
                    !keywordSet.IsEnabled(kw) && !keywordSet.IsEnabled(kw2))
                        return true;

                return false;
            }

            public bool StripFeatureKeepDisabled(in LocalKeyword kw, ShaderFeatures feature)
            {
                return (m_Features & feature) == 0 && keywordSet.IsEnabled(kw);
            }

            public bool StripFeature(in LocalKeyword kw, ShaderFeatures feature)
            {
                if ((m_Features & feature) == 0) // disabled
                {
                    if (keywordSet.IsEnabled(kw))
                        return true;
                }
                else if (m_StripDisabledKeywords)
                {
                    if (!keywordSet.IsEnabled(kw) && ContainsKeyword(kw))
                        return true;
                }
                return false;
            }
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

            var stripDisabledKeywords = globalSettings?.stripDisabledKeywordVariants ?? true;
            var stripTool = new StripTool(shader, stripDisabledKeywords, features, snippetData, compilerData.shaderKeywordSet);

            // strip main light shadows, cascade and screen variants
            // TODO: Strip disabled keyword once no light will re-use same variant  
            if (stripTool.StripFeatureKeepDisabled(
                m_MainLightShadows, ShaderFeatures.MainLightShadows,
                m_MainLightShadowsCascades, ShaderFeatures.MainLightShadowsCascade,
                m_MainLightShadowsScreen, ShaderFeatures.ScreenSpaceShadows))
                return true;

            // TODO: Strip disabled keyword once we support global soft shadows option
            if (stripTool.StripFeatureKeepDisabled(m_SoftShadows, ShaderFeatures.SoftShadows))
                return true;

            // Left for backward compatibility
            if (compilerData.shaderKeywordSet.IsEnabled(m_MixedLightingSubtractive) &&
                !IsFeatureEnabled(features, ShaderFeatures.MixedLighting))
                return true;

            if (stripTool.StripFeature(m_UseFastSRGBLinearConversion, ShaderFeatures.UseFastSRGBLinearConversion))
                return true;

            // Strip here only if mixed lighting is disabled
            // No need to check here if actually used by scenes as this taken care by builtin stripper
            if ((compilerData.shaderKeywordSet.IsEnabled(m_LightmapShadowMixing) ||
                 compilerData.shaderKeywordSet.IsEnabled(m_ShadowsShadowMask)) &&
                !IsFeatureEnabled(features, ShaderFeatures.MixedLighting))
                return true;

            if (stripTool.StripFeature(m_LightLayers, ShaderFeatures.LightLayers))
                return true;

            if (stripTool.StripFeature(m_RenderPassEnabled, ShaderFeatures.RenderPassEnabled))
                return true;             

            // No additional light shadows
            // TODO: Strip disabled keyword once we support no shadow lights re-use same variant
            if (stripTool.StripFeatureKeepDisabled(m_AdditionalLightShadows, ShaderFeatures.AdditionalLightShadows))
                return true;

            if (stripTool.StripFeature(m_ReflectionProbeBlending, ShaderFeatures.ReflectionProbeBlending))
                return true;

            if (stripTool.StripFeature(m_ReflectionProbeBoxProjection, ShaderFeatures.ReflectionProbeBoxProjection))
                return true;
            
            // Shadow caster punctual light strip
            if (snippetData.passType == PassType.ShadowCaster && ShaderUtil.PassHasKeyword(shader, snippetData.pass, m_CastingPunctualLightShadow, snippetData.shaderType))
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
            // TODO: Strip disabled keyword once we support no additional lights re-used variants
            if (stripTool.StripFeatureKeepDisabled(m_AdditionalLightsVertex, ShaderFeatures.VertexLighting,
                m_AdditionalLightsPixel, ShaderFeatures.AdditionalLights))
                return true;

            if (stripTool.StripFeature(m_ClusteredRendering, ShaderFeatures.ClusteredRendering))
                return true;

            // Screen Space Occlusion
            if (stripTool.StripFeature(m_ScreenSpaceOcclusion, ShaderFeatures.ScreenSpaceOcclusion))
                return true;

            // Decal DBuffer
            if (stripTool.StripFeature(
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

            // TODO: make sure vulkan works after refactor
            // Do not strip accurateGbufferNormals on Mobile Vulkan as some GPUs do not support R8G8B8A8_SNorm, which then force us to use accurateGbufferNormals
            if (compilerData.shaderCompilerPlatform != ShaderCompilerPlatform.Vulkan &&
                stripTool.StripFeature(m_GbufferNormalsOct, ShaderFeatures.DeferredWithAccurateGbufferNormals))
                return true;

            if (compilerData.shaderKeywordSet.IsEnabled(m_UseDrawProcedural) &&
                !IsFeatureEnabled(features, ShaderFeatures.DrawProcedural))
                return true;

            // Decal Normal Blend
            if (stripTool.StripFeature(
                m_DecalNormalBlendLow, ShaderFeatures.DecalNormalBlendLow,
                m_DecalNormalBlendMedium, ShaderFeatures.DecalNormalBlendMedium,
                m_DecalNormalBlendHigh, ShaderFeatures.DecalNormalBlendHigh))
                return true;

            return false;
        }

        bool StripVolumeFeatures(ShaderFeatures features, Shader shader, ShaderSnippetData snippetData, ShaderCompilerData compilerData)
        {
            var stripDisabledKeywords = UniversalRenderPipelineGlobalSettings.instance?.stripDisabledKeywordVariants ?? true;
            var stripTool = new StripTool(shader, stripDisabledKeywords, (ShaderFeatures)ShaderBuildPreprocessor.volumeFeatures, snippetData, compilerData.shaderKeywordSet);

            if (stripTool.StripFeature(m_LensDistortion, (ShaderFeatures)VolumeFeatures.LensDistortion))
                return true;

            if (stripTool.StripFeature(m_ChromaticAberration, (ShaderFeatures)VolumeFeatures.CHROMATIC_ABERRATION))
                return true;

            if (stripTool.StripFeature(m_BloomLQ, (ShaderFeatures)VolumeFeatures.Bloom))
                return true;
            if (stripTool.StripFeature(m_BloomHQ, (ShaderFeatures)VolumeFeatures.Bloom))
                return true;
            if (stripTool.StripFeature(m_BloomLQDirt, (ShaderFeatures)VolumeFeatures.Bloom))
                return true;
            if (stripTool.StripFeature(m_BloomHQDirt, (ShaderFeatures)VolumeFeatures.Bloom))
                return true;

            if (stripTool.StripFeature(m_HdrGrading, (ShaderFeatures)VolumeFeatures.ToneMaping))
                return true;
            if (stripTool.StripFeature(m_ToneMapACES, (ShaderFeatures)VolumeFeatures.ToneMaping))
                return true;
            if (stripTool.StripFeature(m_ToneMapNeutral, (ShaderFeatures)VolumeFeatures.ToneMaping))
                return true;
            if (stripTool.StripFeature(m_FilmGrain, (ShaderFeatures)VolumeFeatures.FilmGrain))
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
            // Strip builtin pipeline shaders
            if (shader == m_InternalDeferredShading)
                return true;
            if (shader == m_InternalDeferredReflections)
                return true;
            if (shader == m_InternalPrePassLighting)
                return true;
            if (shader == m_InternalScreenSpaceShadows)
                return true;
            if (shader == m_InternalFlare)
                return true;
            if (shader == m_InternalHalo)
                return true;
            if (shader == m_InternalMotionVectors)
                return true;

            // Strip builtin pipeline vr shaders
            if (shader == m_BlitFromTex2DToTexArraySlice)
                return true;
            if (shader == BlitTexArraySlice)
                return true;

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

            if (UniversalRenderPipelineGlobalSettings.instance?.stripBuiltinShaders ?? true)
            {
                if (StripUnusedShaders(features, shader))
                    return true;
            }

            if (UniversalRenderPipelineGlobalSettings.instance?.stripPostProcessingShaderVariants ?? true)
            {
                if (StripVolumeFeatures(features, shader, snippetData, compilerData))
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

        void LogShaderVariants(Shader shader, ShaderSnippetData snippetData, ShaderVariantLogLevel logLevel, IList<ShaderCompilerData> list, int prevVariantsCount, int currVariantsCount, double stripTimeMs)
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

                if (shader.name.Contains("Lit") || shader.name.Contains("StencilDeferred") || shader.name.Contains("Testy"))
                {
                    string msg = $"";
                    foreach (var item in list)
                    {
                        msg += $"{snippetData.passName}:\n";
                        var set = item.shaderKeywordSet;
                        for (int i = 0; i < shader.keywordSpace.keywordCount; ++i)
                        {
                            if (set.IsEnabled(shader.keywordSpace.keywords[i]))
                            {
                                msg += shader.keywordSpace.keywordNames[i] + "\n";
                            }
                        }
                        msg += "\n";
                    }
                    Debug.Log(msg);
                }
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

            m_stripTimer.Stop();
            double stripTimeMs = m_stripTimer.Elapsed.TotalMilliseconds;
            m_stripTimer.Reset();

            if (urpAsset.shaderVariantLogLevel != ShaderVariantLogLevel.Disabled)
            {
                m_TotalVariantsInputCount += prevVariantCount;
                m_TotalVariantsOutputCount += compilerDataList.Count;
                LogShaderVariants(shader, snippetData, urpAsset.shaderVariantLogLevel, compilerDataList, prevVariantCount, compilerDataList.Count, stripTimeMs);
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
                        Debug.Log($"Excluding {QualitySettings.names[i]}"); // TODO: remove it
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
            if (UniversalRenderPipelineGlobalSettings.instance?.stripPostProcessingShaderVariants ?? false)
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
            }
        }

        private static ShaderFeatures GetSupportedShaderFeatures(UniversalRenderPipelineAsset pipelineAsset, int rendererIndex)
        {
            ShaderFeatures shaderFeatures;
            shaderFeatures = ShaderFeatures.MainLight;

            if (pipelineAsset.supportsMainLightShadows && pipelineAsset.shadowCascadeCount > 1)
                shaderFeatures |= ShaderFeatures.MainLightShadowsCascade;
            else if (pipelineAsset.supportsMainLightShadows)
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

            //int rendererCount = pipelineAsset.m_RendererDataList.Length;
            //for (int rendererIndex = 0; rendererIndex < rendererCount; ++rendererIndex)
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

            Debug.Log($"FEATURES {pipelineAsset.name} {shaderFeatures}");

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
