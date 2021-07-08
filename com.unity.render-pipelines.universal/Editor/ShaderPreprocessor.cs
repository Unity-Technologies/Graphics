using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using LocalKeyword = UnityEngine.Rendering.ShaderKeyword;

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

        /*LocalKeyword m_MainLightShadows;
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
        LocalKeyword m_LocalClearCoatMap;*/

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

        LocalKeyword m_LocalMainLightShadows;
        LocalKeyword m_LocalMainLightShadowsCascades;
        LocalKeyword m_LocalMainLightShadowsScreen;
        LocalKeyword m_LocalSoftShadows;
        LocalKeyword m_LocalLightCookies;
        LocalKeyword m_LocalLightLayers;
        LocalKeyword m_LocalAdditionalLightShadows;
        LocalKeyword m_LocalReflectionProbeBlending;
        LocalKeyword m_LocalReflectionProbeBoxProjection;
        LocalKeyword m_LocalCastingPunctualLightShadow;
        LocalKeyword m_LocalAdditionalLightsVertex;
        LocalKeyword m_LocalAdditionalLightsPixel;
        LocalKeyword m_LocalDBufferMRT1;
        LocalKeyword m_LocalDBufferMRT2;
        LocalKeyword m_LocalDBufferMRT3;
        LocalKeyword m_LocalScreenSpaceOcclusion;
        LocalKeyword m_LocalUseDrawProcedural;
        LocalKeyword m_LocalUseFastSRGBLinearConversion;
        LocalKeyword m_LocalGbufferNormalsOct;
        LocalKeyword m_LocalRenderPassEnabled;
        HashSet<string> m_ContainedLocalKeywords;
        bool m_StripDisabledKeywords;
        bool m_IsShaderGraph;
        bool m_MainLightFragment;

        LocalKeyword m_LocalLensDistortion;
        LocalKeyword _CHROMATIC_ABERRATION;
        LocalKeyword m_LocalBloomLq;
        LocalKeyword m_LocalBloomHq;
        LocalKeyword m_LocalBloomLqDirt;
        LocalKeyword m_LocalBloomHqDirt;
        LocalKeyword _HDR_GRADING;
        LocalKeyword _TONEMAP_ACES;
        LocalKeyword _TONEMAP_NEUTRAL;
        LocalKeyword _FILM_GRAIN;

        HashSet<string> m_ContainedKeywordNames;

        // Multiple callback may be implemented.
        // The first one executed is the one where callbackOrder is returning the smallest number.
        public int callbackOrder { get { return 0; } }

        LocalKeyword TryGetLocalKeyword(Shader shader, string name)
        {
            return new LocalKeyword(shader, name);
            //return new ShaderKeyword(name);
            if (m_ContainedKeywordNames.Contains(name))
                return new LocalKeyword(shader, name);
            return new LocalKeyword();
        }

        void InitializeLocalShaderKeywords(Shader shader)
        {
            /*var names = shader.keywordSpace.keywordNames;
            var nameHash = new HashSet<string>();
            foreach (var name in names)
                nameHash.Add(name);
            if (nameHash.Contains(ShaderKeywordStrings.MainLightShadows))
                m_MainLightShadows = new LocalKeyword(shader, ShaderKeywordStrings.MainLightShadows);
            if (nameHash.Contains(ShaderKeywordStrings.MainLightShadowCascades))
                m_MainLightShadowsCascades = new LocalKeyword(shader, ShaderKeywordStrings.MainLightShadowCascades);
            if (nameHash.Contains(ShaderKeywordStrings.MainLightShadowScreen))
                m_MainLightShadowsScreen = new LocalKeyword(shader, ShaderKeywordStrings.MainLightShadowScreen);
            if (nameHash.Contains(ShaderKeywordStrings.AdditionalLightsVertex))
                m_AdditionalLightsVertex = new LocalKeyword(shader, ShaderKeywordStrings.AdditionalLightsVertex);
            if (nameHash.Contains(ShaderKeywordStrings.AdditionalLightsPixel))
                m_AdditionalLightsPixel = new LocalKeyword(shader, ShaderKeywordStrings.AdditionalLightsPixel);
            if (nameHash.Contains(ShaderKeywordStrings.AdditionalLightShadows))
                m_AdditionalLightShadows = new LocalKeyword(shader, ShaderKeywordStrings.AdditionalLightShadows);
            if (nameHash.Contains(ShaderKeywordStrings.ReflectionProbeBlending))
                m_ReflectionProbeBlending = new LocalKeyword(shader, ShaderKeywordStrings.ReflectionProbeBlending);
            if (nameHash.Contains(ShaderKeywordStrings.ReflectionProbeBoxProjection))
                m_ReflectionProbeBoxProjection = new LocalKeyword(shader, ShaderKeywordStrings.ReflectionProbeBoxProjection);
            if (nameHash.Contains(ShaderKeywordStrings.CastingPunctualLightShadow))
                m_CastingPunctualLightShadow = new LocalKeyword(shader, ShaderKeywordStrings.CastingPunctualLightShadow);
            if (nameHash.Contains(ShaderKeywordStrings.SoftShadows))
                m_SoftShadows = new LocalKeyword(shader, ShaderKeywordStrings.SoftShadows);
            if (nameHash.Contains(ShaderKeywordStrings.MixedLightingSubtractive))
                m_MixedLightingSubtractive = new LocalKeyword(shader, ShaderKeywordStrings.MixedLightingSubtractive);
            if (nameHash.Contains(ShaderKeywordStrings.LightmapShadowMixing))
                m_LightmapShadowMixing = new LocalKeyword(shader, ShaderKeywordStrings.LightmapShadowMixing);
            if (nameHash.Contains(ShaderKeywordStrings.ShadowsShadowMask))
                m_ShadowsShadowMask = new LocalKeyword(shader, ShaderKeywordStrings.ShadowsShadowMask);
             m_Lightmap = new LocalKeyword(shader, ShaderKeywordStrings.LIGHTMAP_ON);
             m_DynamicLightmap = new LocalKeyword(shader, ShaderKeywordStrings.DYNAMICLIGHTMAP_ON);
             m_DirectionalLightmap = new LocalKeyword(shader, ShaderKeywordStrings.DIRLIGHTMAP_COMBINED);
             m_AlphaTestOn = new LocalKeyword(shader, ShaderKeywordStrings._ALPHATEST_ON);
             m_DeferredStencil = new LocalKeyword(shader, ShaderKeywordStrings._DEFERRED_STENCIL);
             m_GbufferNormalsOct = new LocalKeyword(shader, ShaderKeywordStrings._GBUFFER_NORMALS_OCT);
             m_UseDrawProcedural = new LocalKeyword(shader, ShaderKeywordStrings.UseDrawProcedural);
             m_ScreenSpaceOcclusion = new LocalKeyword(shader, ShaderKeywordStrings.ScreenSpaceOcclusion);
             m_UseFastSRGBLinearConversion = new LocalKeyword(shader, ShaderKeywordStrings.UseFastSRGBLinearConversion);
             m_LightLayers = new LocalKeyword(shader, ShaderKeywordStrings.LightLayers);
             m_RenderPassEnabled = new LocalKeyword(shader, ShaderKeywordStrings.RenderPassEnabled);
             m_DebugDisplay = new LocalKeyword(shader, ShaderKeywordStrings.DEBUG_DISPLAY);
             m_DBufferMRT1 = new LocalKeyword(shader, ShaderKeywordStrings.DBufferMRT1);
             m_DBufferMRT2 = new LocalKeyword(shader, ShaderKeywordStrings.DBufferMRT2);
             m_DBufferMRT3 = new LocalKeyword(shader, ShaderKeywordStrings.DBufferMRT3);
             m_DecalNormalBlendLow = new LocalKeyword(shader, ShaderKeywordStrings.DecalNormalBlendLow);
             m_DecalNormalBlendMedium = new LocalKeyword(shader, ShaderKeywordStrings.DecalNormalBlendMedium);
             m_DecalNormalBlendHigh = new LocalKeyword(shader, ShaderKeywordStrings.DecalNormalBlendHigh);
             m_ClusteredRendering = new LocalKeyword(shader, ShaderKeywordStrings.ClusteredRendering);
             m_EditorVisualization = new LocalKeyword(shader, ShaderKeywordStrings.EDITOR_VISUALIZATION);

            m_LocalDetailMulx2 = new LocalKeyword(shader, ShaderKeywordStrings._DETAIL_MULX2);
            m_LocalDetailScaled = new LocalKeyword(shader, ShaderKeywordStrings._DETAIL_SCALED);
            m_LocalClearCoat = new LocalKeyword(shader, ShaderKeywordStrings._CLEARCOAT);
            m_LocalClearCoatMap = new LocalKeyword(shader, ShaderKeywordStrings._CLEARCOATMAP);*/

            var names = shader.keywordSpace.keywordNames;
            m_ContainedKeywordNames = new HashSet<string>();
            foreach (var name in names)
                m_ContainedKeywordNames.Add(name);

            m_LocalDetailMulx2 = new ShaderKeyword(shader, ShaderKeywordStrings._DETAIL_MULX2);
            m_LocalDetailScaled = new ShaderKeyword(shader, ShaderKeywordStrings._DETAIL_SCALED);
            m_LocalClearCoat = new ShaderKeyword(shader, ShaderKeywordStrings._CLEARCOAT);
            m_LocalClearCoatMap = new ShaderKeyword(shader, ShaderKeywordStrings._CLEARCOATMAP);

            var isUrpShader = shader.name.StartsWith("Universal Render Pipeline/");
            var isStencilDeferred = shader.name == "Hidden/Universal Render Pipeline/StencilDeferred";
            /*if (shader.name.StartsWith("Universal Render Pipeline/") || 
                shader.name == "Universal Render Pipeline/Simple Lit" || 
                shader.name == "Universal Render Pipeline/Complex Lit" || 
                shader.name == "Universal Render Pipeline/Baked Lit" || 
                shader.name == "Unlit/Testy" ||
                shader.name == "Hidden/Universal Render Pipeline/StencilDeferred" || 
                m_IsShaderGraph)
                m_StripDisabledKeywords = true;*/
            m_StripDisabledKeywords = isUrpShader | isStencilDeferred;
            //m_StripDisabledKeywords= false;
            m_MainLightFragment = isStencilDeferred;

            m_LocalMainLightShadows = TryGetLocalKeyword(shader, ShaderKeywordStrings.MainLightShadows);
            m_LocalMainLightShadowsCascades = TryGetLocalKeyword(shader, ShaderKeywordStrings.MainLightShadowCascades);
            m_LocalMainLightShadowsScreen = TryGetLocalKeyword(shader, ShaderKeywordStrings.MainLightShadowScreen);
            m_LocalSoftShadows = TryGetLocalKeyword(shader, ShaderKeywordStrings.SoftShadows);
            m_LocalLightCookies = TryGetLocalKeyword(shader, ShaderKeywordStrings.LightCookies);
            m_LocalLightLayers = TryGetLocalKeyword(shader, ShaderKeywordStrings.LightLayers);
            m_LocalAdditionalLightShadows = TryGetLocalKeyword(shader, ShaderKeywordStrings.AdditionalLightShadows);
            m_LocalReflectionProbeBlending = TryGetLocalKeyword(shader, ShaderKeywordStrings.ReflectionProbeBlending);
            m_LocalReflectionProbeBoxProjection = TryGetLocalKeyword(shader, ShaderKeywordStrings.ReflectionProbeBoxProjection);
            m_LocalCastingPunctualLightShadow = TryGetLocalKeyword(shader, ShaderKeywordStrings.CastingPunctualLightShadow);
            m_LocalAdditionalLightsVertex = TryGetLocalKeyword(shader, ShaderKeywordStrings.AdditionalLightsVertex);
            m_LocalAdditionalLightsPixel = TryGetLocalKeyword(shader, ShaderKeywordStrings.AdditionalLightsPixel);
            m_LocalDBufferMRT1 = TryGetLocalKeyword(shader, ShaderKeywordStrings.DBufferMRT1);
            m_LocalDBufferMRT2 = TryGetLocalKeyword(shader, ShaderKeywordStrings.DBufferMRT2);
            m_LocalDBufferMRT3 = TryGetLocalKeyword(shader, ShaderKeywordStrings.DBufferMRT3);
            m_LocalScreenSpaceOcclusion = TryGetLocalKeyword(shader, ShaderKeywordStrings.ScreenSpaceOcclusion);
            m_LocalUseDrawProcedural = TryGetLocalKeyword(shader, ShaderKeywordStrings.UseDrawProcedural);
            m_LocalUseFastSRGBLinearConversion = TryGetLocalKeyword(shader, ShaderKeywordStrings.UseFastSRGBLinearConversion);
            m_LocalGbufferNormalsOct = TryGetLocalKeyword(shader, ShaderKeywordStrings._GBUFFER_NORMALS_OCT);
            m_LocalRenderPassEnabled = TryGetLocalKeyword(shader, ShaderKeywordStrings.RenderPassEnabled);

            m_LocalLensDistortion = TryGetLocalKeyword(shader, "_DISTORTION");
            _CHROMATIC_ABERRATION = TryGetLocalKeyword(shader, "_CHROMATIC_ABERRATION");
            m_LocalBloomLq = TryGetLocalKeyword(shader, "_BLOOM_LQ");
            m_LocalBloomHq = TryGetLocalKeyword(shader, "_BLOOM_HQ");
            m_LocalBloomLqDirt = TryGetLocalKeyword(shader, "_BLOOM_LQ_DIRT");
            m_LocalBloomHqDirt = TryGetLocalKeyword(shader, "_BLOOM_HQ_DIRT");

            _HDR_GRADING = TryGetLocalKeyword(shader, "_HDR_GRADING");
            _TONEMAP_ACES = TryGetLocalKeyword(shader, "_TONEMAP_ACES");
            _TONEMAP_NEUTRAL = TryGetLocalKeyword(shader, "_TONEMAP_NEUTRAL");
            _FILM_GRAIN = TryGetLocalKeyword(shader, "_FILM_GRAIN");
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
            HashSet<string> m_ContainedLocalKeywords;
            ShaderFeatures m_Features;
            ShaderKeywordSet keywordSet;
            ShaderSnippetData snippetData;
            bool m_StripDisabledKeywords;
            bool m_IsShaderGraph;

            bool m_CachedStripDisabledKeywords;

            public StripTool(HashSet<string> containedLocalKeywords, bool stripDisabledKeywords, bool isShaderGraph, ShaderFeatures features, ShaderSnippetData snippetData, in ShaderKeywordSet keywordSet)
            {
                m_ContainedLocalKeywords = containedLocalKeywords;
                m_Features = features;
                this.snippetData = snippetData;
                this.keywordSet = keywordSet;
                m_StripDisabledKeywords = stripDisabledKeywords;
                m_IsShaderGraph = isShaderGraph;
                m_CachedStripDisabledKeywords = false;
            }

            bool ContainsKeyword(in LocalKeyword kw)
            {
                return m_ContainedLocalKeywords.Contains(kw.name);
            }

            public bool StripFragmentFeature(in LocalKeyword kw, ShaderFeatures feature, in LocalKeyword kw2, ShaderFeatures feature2, in LocalKeyword kw3, ShaderFeatures feature3, in LocalKeyword kw4, ShaderFeatures feature4)
            {
                if ((m_Features & feature) == 0 && keywordSet.IsEnabled(kw)) // disabled
                    return true;
                if ((m_Features & feature2) == 0 && keywordSet.IsEnabled(kw2)) // disabled
                    return true;
                if ((m_Features & feature3) == 0 && keywordSet.IsEnabled(kw3)) // disabled
                    return true;
                if ((m_Features & feature4) == 0 && keywordSet.IsEnabled(kw4)) // disabled
                    return true;

                var checkShaderType = m_IsShaderGraph || snippetData.shaderType == ShaderType.Fragment;

                if (m_StripDisabledKeywords && ContainsKeyword(kw) && checkShaderType &&
                    ((m_Features & feature) != 0 || (m_Features & feature2) != 0 || (m_Features & feature3) != 0) || (m_Features & feature4) != 0 &&
                    !keywordSet.IsEnabled(kw) && !keywordSet.IsEnabled(kw2) && !keywordSet.IsEnabled(kw3) && !keywordSet.IsEnabled(kw4))
                    return true;

                return false;
            }

            public bool StripFragmentFeature(in LocalKeyword kw, ShaderFeatures feature, in LocalKeyword kw2, ShaderFeatures feature2, in LocalKeyword kw3, ShaderFeatures feature3)
            {
                if ((m_Features & feature) == 0 && keywordSet.IsEnabled(kw)) // disabled
                    return true;
                if ((m_Features & feature2) == 0 && keywordSet.IsEnabled(kw2)) // disabled
                    return true;
                if ((m_Features & feature3) == 0 && keywordSet.IsEnabled(kw3)) // disabled
                    return true;

                var checkShaderType = m_IsShaderGraph || snippetData.shaderType == ShaderType.Fragment;

                if (m_StripDisabledKeywords && ContainsKeyword(kw) && checkShaderType &&
                    ((m_Features & feature) != 0 || (m_Features & feature2) != 0 || (m_Features & feature3) != 0) &&
                    !keywordSet.IsEnabled(kw) && !keywordSet.IsEnabled(kw2) && !keywordSet.IsEnabled(kw3))
                    return true;

                return false;
            }

            public bool StripFeature(in LocalKeyword kw, ShaderFeatures feature, in LocalKeyword kw2, ShaderFeatures feature2, in LocalKeyword kw3, ShaderFeatures feature3)
            {
                if ((m_Features & feature) == 0 && keywordSet.IsEnabled(kw)) // disabled
                    return true;
                if ((m_Features & feature2) == 0 && keywordSet.IsEnabled(kw2)) // disabled
                    return true;
                if ((m_Features & feature3) == 0 && keywordSet.IsEnabled(kw3)) // disabled
                    return true;

                if (m_StripDisabledKeywords && ContainsKeyword(kw) &&
                    ((m_Features & feature) != 0 || (m_Features & feature2) != 0 || (m_Features & feature3) != 0) &&
                    !keywordSet.IsEnabled(kw) && !keywordSet.IsEnabled(kw2) && !keywordSet.IsEnabled(kw3))
                    return true;

                return false;
            }

            public bool StripFeature(in LocalKeyword kw, ShaderFeatures feature, in LocalKeyword kw2, ShaderFeatures feature2)
            {
                if ((m_Features & feature) == 0 && keywordSet.IsEnabled(kw)) // disabled
                    return true;
                if ((m_Features & feature2) == 0 && keywordSet.IsEnabled(kw2)) // disabled
                    return true;

                if (m_StripDisabledKeywords && ContainsKeyword(kw) &&
                    ((m_Features & feature) != 0 || (m_Features & feature2) != 0) &&
                    !keywordSet.IsEnabled(kw) && !keywordSet.IsEnabled(kw2))
                        return true;

                return false;
            }

            public bool StripFragmentFeature(in LocalKeyword kw, ShaderFeatures feature)
            {
                return StripFeature(kw, (m_Features & feature) != 0, ShaderType.Fragment);
            }

            public bool StripFragmentFeature(in LocalKeyword kw, bool feature)
            {
                return StripFeature(kw, feature, ShaderType.Fragment);
            }

            public bool StripVertexFeature(in LocalKeyword kw, ShaderFeatures feature)
            {
                return StripFeature(kw, (m_Features & feature) != 0, ShaderType.Vertex);
            }

            public bool StripFeature(in LocalKeyword kw, bool feature, ShaderType shaderType)
            {
                if (snippetData.shaderType != shaderType && !m_IsShaderGraph)
                    return false;

                if (!feature) // disabled
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

            public void DisableOffStrip()
            {
                m_CachedStripDisabledKeywords = m_StripDisabledKeywords;
                m_StripDisabledKeywords = false;
            }

            public void EnableOffStrip()
            {
                m_StripDisabledKeywords = m_CachedStripDisabledKeywords;
            }
        }

        bool StripUnusedFeaturesOld(ShaderFeatures features, Shader shader, ShaderSnippetData snippetData, ShaderCompilerData compilerData)
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


            bool isSoftShadow = compilerData.shaderKeywordSet.IsEnabled(m_LocalSoftShadows);
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

            var stripTool = new StripTool(m_ContainedLocalKeywords, m_StripDisabledKeywords, m_IsShaderGraph, features, snippetData, compilerData.shaderKeywordSet);

            // strip main light shadows, cascade and screen variants
            /*if (!IsFeatureEnabled(features, ShaderFeatures.MainLightShadows))
            {
                if (compilerData.shaderKeywordSet.IsEnabled(m_MainLightShadows))
                    return true;

                if (compilerData.shaderKeywordSet.IsEnabled(m_MainLightShadowsCascades))
                    return true;

                if (compilerData.shaderKeywordSet.IsEnabled(m_MainLightShadowsScreen))
                    return true;

                if (snippetData.passType == PassType.ShadowCaster && !compilerData.shaderKeywordSet.IsEnabled(m_CastingPunctualLightShadow))
                    return true;
            }*/
            
            stripTool.DisableOffStrip();
            
            if (m_MainLightFragment)
            {
                if (stripTool.StripFragmentFeature(
                    m_LocalMainLightShadows, ShaderFeatures.MainLightShadows,
                    m_LocalMainLightShadowsCascades, ShaderFeatures.MainLightShadowsCascade,
                    m_LocalMainLightShadowsScreen, ShaderFeatures.ScreenSpaceShadows))
                    return true;
            }
            else
            {
                if (stripTool.StripFeature(
                    m_LocalMainLightShadows, ShaderFeatures.MainLightShadows,
                    m_LocalMainLightShadowsCascades, ShaderFeatures.MainLightShadowsCascade,
                    m_LocalMainLightShadowsScreen, ShaderFeatures.ScreenSpaceShadows))
                    return true;
            }
stripTool.EnableOffStrip();


            //if (stripTool.StripFragmentFeature(m_LocalLightCookies, globalSettings.supportLightCookies))
            //    return true;

            //bool isSoftShadow = compilerData.shaderKeywordSet.IsEnabled(m_LocalSoftShadows);
            //if (!IsFeatureEnabled(features, ShaderFeatures.SoftShadows) && isSoftShadow)
            //    return true;
            /*if (snippetData.passName == kPassNameGBuffer)
            {
                if (stripTool.StripFeature(m_LocalSoftShadows, ShaderFeatures.SoftShadows))
                    return true;
            }
            else
            {
                if (stripTool.StripFragmentFeature(m_LocalSoftShadows, ShaderFeatures.SoftShadows))
                    return true;
            }*/
            if (stripTool.StripFragmentFeature(m_LocalSoftShadows, ShaderFeatures.SoftShadows))
                return true;
                

            // Left for backward compatibility
            if (compilerData.shaderKeywordSet.IsEnabled(m_MixedLightingSubtractive) &&
                !IsFeatureEnabled(features, ShaderFeatures.MixedLighting))
                return true;

            //if (compilerData.shaderKeywordSet.IsEnabled(m_UseFastSRGBLinearConversion) &&
            //    !IsFeatureEnabled(features, ShaderFeatures.UseFastSRGBLinearConversion))
            //    return true;
            if (stripTool.StripFragmentFeature(m_LocalUseFastSRGBLinearConversion, ShaderFeatures.UseFastSRGBLinearConversion))
                return true;

            // Strip here only if mixed lighting is disabled
            // No need to check here if actually used by scenes as this taken care by builtin stripper
            if ((compilerData.shaderKeywordSet.IsEnabled(m_LightmapShadowMixing) ||
                 compilerData.shaderKeywordSet.IsEnabled(m_ShadowsShadowMask)) &&
                !IsFeatureEnabled(features, ShaderFeatures.MixedLighting))
                return true;

            //if (compilerData.shaderKeywordSet.IsEnabled(m_LightLayers) &&
            //    !IsFeatureEnabled(features, ShaderFeatures.LightLayers))
            //    return true;
            if (stripTool.StripFragmentFeature(m_LocalLightLayers, ShaderFeatures.LightLayers))
                return true;

            //if (compilerData.shaderKeywordSet.IsEnabled(m_RenderPassEnabled) &&
            //    !IsFeatureEnabled(features, ShaderFeatures.RenderPassEnabled))
            //    return true;
            if (stripTool.StripFragmentFeature(m_LocalRenderPassEnabled, ShaderFeatures.RenderPassEnabled))
                return true;
                

            // No additional light shadows
            //bool isAdditionalLightShadow = compilerData.shaderKeywordSet.IsEnabled(m_AdditionalLightShadows);
            //if (!IsFeatureEnabled(features, ShaderFeatures.AdditionalLightShadows) && isAdditionalLightShadow)
            //    return true;
            if (stripTool.StripFragmentFeature(m_LocalAdditionalLightShadows, ShaderFeatures.AdditionalLightShadows))
                return true;

            //bool isReflectionProbeBlending = compilerData.shaderKeywordSet.IsEnabled(m_ReflectionProbeBlending);
            //if (!IsFeatureEnabled(features, ShaderFeatures.ReflectionProbeBlending) && isReflectionProbeBlending)
            //    return true;
            if (stripTool.StripFragmentFeature(m_LocalReflectionProbeBlending, ShaderFeatures.ReflectionProbeBlending))
                return true;

            //bool isReflectionProbeBoxProjection = compilerData.shaderKeywordSet.IsEnabled(m_ReflectionProbeBoxProjection);
            //if (!IsFeatureEnabled(features, ShaderFeatures.ReflectionProbeBoxProjection) && isReflectionProbeBoxProjection)
            //    return true;
            if (stripTool.StripFragmentFeature(m_LocalReflectionProbeBoxProjection, ShaderFeatures.ReflectionProbeBoxProjection))
                return true;

            //bool isPunctualLightShadowCasterPass = (snippetData.passType == PassType.ShadowCaster) && compilerData.shaderKeywordSet.IsEnabled(m_CastingPunctualLightShadow);
            //if (!IsFeatureEnabled(features, ShaderFeatures.AdditionalLightShadows) && isPunctualLightShadowCasterPass)
            //    return true;
            
            // Shadow caster punctual light strip
            if (snippetData.passType == PassType.ShadowCaster && m_ContainedKeywordNames.Contains(m_LocalCastingPunctualLightShadow.name) && snippetData.shaderType == ShaderType.Vertex)
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
            //bool isFeaturePerPixelLightingEnabled = IsFeatureEnabled(features, ShaderFeatures.AdditionalLights);
            //bool isFeaturePerVertexLightingEnabled = IsFeatureEnabled(features, ShaderFeatures.VertexLighting);
            bool clusteredRendering = IsFeatureEnabled(features, ShaderFeatures.ClusteredRendering);
            //bool isAdditionalLightPerPixel = compilerData.shaderKeywordSet.IsEnabled(m_AdditionalLightsPixel);
            //bool isAdditionalLightPerVertex = compilerData.shaderKeywordSet.IsEnabled(m_AdditionalLightsVertex);

            // Strip if Per-Pixel lighting is NOT used in the project and the
            // Per-Pixel (_ADDITIONAL_LIGHTS) variant is enabled in the shader.
            //if (!isFeaturePerPixelLightingEnabled && isAdditionalLightPerPixel)
            //    return true;

            // Strip if Per-Vertex lighting is NOT used in the project and the
            // Per-Vertex (_ADDITIONAL_LIGHTS_VERTEX) variant is enabled in the shader.
            //if (!isFeaturePerVertexLightingEnabled && isAdditionalLightPerVertex)
            //    return true;

            if (stripTool.StripFeature(m_LocalAdditionalLightsVertex, ShaderFeatures.VertexLighting,
                m_LocalAdditionalLightsPixel, ShaderFeatures.AdditionalLights))
                return true;

            if (!clusteredRendering && compilerData.shaderKeywordSet.IsEnabled(m_ClusteredRendering))
                return true;

            // Screen Space Shadows
            //if (!IsFeatureEnabled(features, ShaderFeatures.ScreenSpaceShadows) &&
            //    compilerData.shaderKeywordSet.IsEnabled(m_MainLightShadowsScreen))
            //    return true;

            // Screen Space Occlusion
            //if (!IsFeatureEnabled(features, ShaderFeatures.ScreenSpaceOcclusion) &&
            //    compilerData.shaderKeywordSet.IsEnabled(m_ScreenSpaceOcclusion))
            //    return true;
            if (stripTool.StripFragmentFeature(m_LocalScreenSpaceOcclusion, ShaderFeatures.ScreenSpaceOcclusion))
                return true;

            // Decal DBuffer
            /*if (!IsFeatureEnabled(features, ShaderFeatures.DBufferMRT1) &&
                compilerData.shaderKeywordSet.IsEnabled(m_DBufferMRT1))
                return true;
            if (!IsFeatureEnabled(features, ShaderFeatures.DBufferMRT2) &&
                compilerData.shaderKeywordSet.IsEnabled(m_DBufferMRT2))
                return true;
            if (!IsFeatureEnabled(features, ShaderFeatures.DBufferMRT3) &&
                compilerData.shaderKeywordSet.IsEnabled(m_DBufferMRT3))
                return true;*/
            if (stripTool.StripFragmentFeature(
                m_LocalDBufferMRT1, ShaderFeatures.DBufferMRT1,
                m_LocalDBufferMRT2, ShaderFeatures.DBufferMRT2,
                m_LocalDBufferMRT3, ShaderFeatures.DBufferMRT3))
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
                stripTool.StripFragmentFeature(m_LocalGbufferNormalsOct, ShaderFeatures.DeferredWithAccurateGbufferNormals))
                return true;

            if (compilerData.shaderKeywordSet.IsEnabled(m_LocalUseDrawProcedural) &&
                !IsFeatureEnabled(features, ShaderFeatures.DrawProcedural))
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

            var stripTool2 = new StripTool(m_ContainedLocalKeywords, m_StripDisabledKeywords, m_IsShaderGraph, (ShaderFeatures)ShaderBuildPreprocessor.volumeFeatures, snippetData, compilerData.shaderKeywordSet);
            if (stripTool2.StripFragmentFeature(m_LocalLensDistortion, (ShaderFeatures)VolumeFeatures.LensDistortion))
                return true;

            if (stripTool2.StripFragmentFeature(_CHROMATIC_ABERRATION, (ShaderFeatures)VolumeFeatures.CHROMATIC_ABERRATION))
                return true;

            if (stripTool2.StripFragmentFeature(m_LocalBloomLq, (ShaderFeatures)VolumeFeatures.Bloom))
                return true;
            if (stripTool2.StripFragmentFeature(m_LocalBloomHq, (ShaderFeatures)VolumeFeatures.Bloom))
                return true;
            if (stripTool2.StripFragmentFeature(m_LocalBloomLqDirt, (ShaderFeatures)VolumeFeatures.Bloom))
                return true;
            if (stripTool2.StripFragmentFeature(m_LocalBloomHqDirt, (ShaderFeatures)VolumeFeatures.Bloom))
                return true;
            if (!IsFeatureEnabled(ShaderBuildPreprocessor.volumeFeatures, VolumeFeatures.Bloom) &&
                shader.name == "Hidden/Universal Render Pipeline/Bloom")
                return true;

            if (stripTool2.StripFragmentFeature(_HDR_GRADING, (ShaderFeatures)VolumeFeatures.ToneMaping))
                return true;
            if (stripTool2.StripFragmentFeature(_TONEMAP_ACES, (ShaderFeatures)VolumeFeatures.ToneMaping))
                return true;
            if (stripTool2.StripFragmentFeature(_TONEMAP_NEUTRAL, (ShaderFeatures)VolumeFeatures.ToneMaping))
                return true;

            if (stripTool2.StripFragmentFeature(_FILM_GRAIN, (ShaderFeatures)VolumeFeatures.FilmGrain))
                return true;


            if (!IsFeatureEnabled(ShaderBuildPreprocessor.volumeFeatures, VolumeFeatures.DepthOfField) &&
                shader.name == "Hidden/Universal Render Pipeline/BokehDepthOfField")
                return true;
            if (!IsFeatureEnabled(ShaderBuildPreprocessor.volumeFeatures, VolumeFeatures.DepthOfField) &&
                shader.name == "Hidden/Universal Render Pipeline/GaussianDepthOfField")
                return true;

            if (!IsFeatureEnabled(ShaderBuildPreprocessor.volumeFeatures, VolumeFeatures.CameraMotionBlur) &&
                shader.name == "Hidden/Universal Render Pipeline/CameraMotionBlur")
                return true;

            if (!IsFeatureEnabled(ShaderBuildPreprocessor.volumeFeatures, VolumeFeatures.PaniniProjection) &&
                shader.name == "Hidden/Universal Render Pipeline/PaniniProjection")
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
            if (shader.name == "Hidden/Internal-DeferredShading")
                return true;
            if (shader.name == "Hidden/Internal-DeferredReflections")
                return true;
            if (shader.name == "Hidden/Internal-PrePassLighting")
                return true;
            if (shader.name == "Hidden/Internal-ScreenSpaceShadows")
                return true;
            if (shader.name == "Hidden/Internal-Flare")
                return true;
            if (shader.name == "Hidden/Internal-Halo")
                return true;
            if (shader.name == "Hidden/Internal-MotionVectors")
                return true;

            // Strip builtin pipeline vr shaders
            if (shader.name == "Hidden/VR/BlitFromTex2DToTexArraySlice")
                return true;
            if (shader.name == "Hidden/VR/BlitTexArraySlice")
                return true;

            if (!IsFeatureEnabled(features, ShaderFeatures.DeferredShading))
            {
                if (shader.name == "Hidden/Universal Render Pipeline/StencilDeferred")
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

            if (StripUnusedShaders(features, shader))
                return true;

            // Strip terrain holes
            // TODO: checking for the string name here is expensive
            // maybe we can rename alpha clip keyword name to be specific to terrain?
            if (compilerData.shaderKeywordSet.IsEnabled(m_AlphaTestOn) &&
                !IsFeatureEnabled(features, ShaderFeatures.TerrainHoles) &&
                shader.name.Contains(kTerrainShaderName))
                return true;

            // TODO: Test against lightMode tag instead.
            /*if (snippetData.passName == kPassNameGBuffer)
            {
                if (!IsFeatureEnabled(features, ShaderFeatures.DeferredShading))
                    return true;

                // Do not strip accurateGbufferNormals on Mobile Vulkan as some GPUs do not support R8G8B8A8_SNorm, which then force us to use accurateGbufferNormals
                if (!IsFeatureEnabled(features, ShaderFeatures.DeferredWithAccurateGbufferNormals) && compilerData.shaderKeywordSet.IsEnabled(m_GbufferNormalsOct) && compilerData.shaderCompilerPlatform != ShaderCompilerPlatform.Vulkan)
                    return true;
                if (!IsFeatureEnabled(features, ShaderFeatures.DeferredWithoutAccurateGbufferNormals) && !compilerData.shaderKeywordSet.IsEnabled(m_GbufferNormalsOct))
                    return true;
            }*/
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
                    string msg = $"{m_IsShaderGraph}";
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

                //Debug.Log($"post process {shader.name} {shader.passCount} {snippetData.pass}");

            // Get all contained local keywords by this pass
            if (shader.name != "Hidden/VR/BlitFromTex2DToTexArraySlice") // TODO: Bug
            {  
            m_ContainedLocalKeywords = new HashSet<string>();
            var keywords = ShaderUtil.GetPassKeywords(shader, snippetData.pass);
            foreach (var kw in keywords)
                m_ContainedLocalKeywords.Add(kw.name);
            }

            if (shader.name == "Unlit/Testy")
            {
                Debug.Log("Testy");
            }

            var s = shader.FindPassTagValue(0, new ShaderTagId("ShaderGraphShader"));
            if (s == new ShaderTagId("True"))
            {
                //ebug.Log($"{shader.name} is shader graph");
                m_IsShaderGraph = true;
            }
            else
            {
                m_IsShaderGraph = false;
            }

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
                    int rendererCount = urp.m_RendererDataList.Length;

                    for (int i = 0; i < rendererCount; ++i)
                        s_SupportedFeaturesList.Add(GetSupportedShaderFeatures(urp, i));
                }
            }
        }

        private static void FetchAllSupportedFeaturesFromVolumes()
        {
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
