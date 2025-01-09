#if UNITY_EDITOR
using ShaderKeywordFilter = UnityEditor.ShaderKeywordFilter;
using HDRKeywords = UnityEngine.Rendering.HDROutputUtils.ShaderKeywords;

namespace UnityEngine.Rendering.Universal
{
    // This partial class is used for Shader Keyword Prefiltering
    // It's an editor only file and used when making builds to determine what keywords can
    // be removed early in the Shader Processing stage based on the settings in each URP Asset
    public partial class UniversalRenderPipelineAsset
    {
        internal enum PrefilteringMode
        {
            Remove,                     // Removes the keyword
            Select,                     // Keeps the keyword
            SelectOnly                  // Selects the keyword and removes others
        }

        internal enum PrefilteringModeMainLightShadows
        {
            Remove,                     // Removes the keyword
            SelectMainLight,            // Selects MainLightShadows variant & Removes OFF variant
            SelectMainLightAndOff,      // Selects MainLightShadows & OFF variants
            SelectMainLightAndCascades, // Selects MainLightShadows, MainLightShadowCascades & Removes OFF variant
            SelectAll,                  // Selects MainLightShadows, MainLightShadowCascades & OFF variant
        }

        internal enum PrefilteringModeAdditionalLights
        {
            Remove,                     // Removes the keyword
            SelectVertex,               // Selects Vertex & Removes OFF variant
            SelectVertexAndOff,         // Selects Vertex & OFF variant
            SelectPixel,                // Selects Pixel  & Removes OFF variant
            SelectPixelAndOff,          // Selects Pixel  & OFF variant
            SelectAll                   // Selects Vertex, Pixel & OFF variant
        }

        // Platform specific filtering overrides
        [ShaderKeywordFilter.ApplyRulesIfGraphicsAPI(GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.OpenGLCore)]
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.WriteRenderingLayers)]
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.DBufferMRT1)]
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.DBufferMRT2)]
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.DBufferMRT3)]
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.USE_LEGACY_LIGHTMAPS)]
        private const bool k_CommonGLDefaults = true;

        // Foveated Rendering
        #if ENABLE_VR && ENABLE_XR_MODULE
        [ShaderKeywordFilter.ApplyRulesIfNotGraphicsAPI(GraphicsDeviceType.PlayStation5NGGC, GraphicsDeviceType.Metal)]
        #endif
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.FoveatedRenderingNonUniformRaster)]
        private const bool k_PrefilterFoveatedRenderingNonUniformRaster = true;

        // User can change cascade count at runtime so we have to include both MainLightShadows and MainLightShadowCascades.
        // ScreenSpaceShadows renderer feature has separate filter attribute for keeping MainLightShadowScreen.
        // NOTE: off variants are atm always removed when shadows are supported
        [ShaderKeywordFilter.RemoveIf(PrefilteringModeMainLightShadows.Remove,                     keywordNames: new [] {ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades})]
        [ShaderKeywordFilter.SelectIf(PrefilteringModeMainLightShadows.SelectMainLight,            keywordNames: ShaderKeywordStrings.MainLightShadows)]
        [ShaderKeywordFilter.SelectIf(PrefilteringModeMainLightShadows.SelectMainLightAndOff,      keywordNames: new [] {"", ShaderKeywordStrings.MainLightShadows})]
        [ShaderKeywordFilter.SelectIf(PrefilteringModeMainLightShadows.SelectMainLightAndCascades, keywordNames: new [] {ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades})]
        [ShaderKeywordFilter.SelectIf(PrefilteringModeMainLightShadows.SelectAll,                  keywordNames: new [] {"", ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades})]
        [SerializeField] private PrefilteringModeMainLightShadows m_PrefilteringModeMainLightShadows = PrefilteringModeMainLightShadows.SelectMainLight;

        // Additional Lights
        // clustered renderer can override PerVertex/PerPixel to be disabled
        // NOTE: off variants are atm always kept when additional lights are enabled due to XR perf reasons
        // multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
        [ShaderKeywordFilter.RemoveIf(PrefilteringModeAdditionalLights.Remove,            keywordNames: new string[] {ShaderKeywordStrings.AdditionalLightsVertex, ShaderKeywordStrings.AdditionalLightsPixel})]
        [ShaderKeywordFilter.SelectIf(PrefilteringModeAdditionalLights.SelectVertex,      keywordNames: ShaderKeywordStrings.AdditionalLightsVertex)]
        [ShaderKeywordFilter.SelectIf(PrefilteringModeAdditionalLights.SelectVertexAndOff,keywordNames: new string[] {"", ShaderKeywordStrings.AdditionalLightsVertex})]
        [ShaderKeywordFilter.SelectIf(PrefilteringModeAdditionalLights.SelectPixel,       keywordNames: ShaderKeywordStrings.AdditionalLightsPixel)]
        [ShaderKeywordFilter.SelectIf(PrefilteringModeAdditionalLights.SelectPixelAndOff, keywordNames: new string[] {"", ShaderKeywordStrings.AdditionalLightsPixel})]
        [ShaderKeywordFilter.SelectIf(PrefilteringModeAdditionalLights.SelectAll,         keywordNames: new string[] {"", ShaderKeywordStrings.AdditionalLightsVertex, ShaderKeywordStrings.AdditionalLightsPixel})]
        [SerializeField] private PrefilteringModeAdditionalLights m_PrefilteringModeAdditionalLight = PrefilteringModeAdditionalLights.SelectPixelAndOff;

        // Additional Lights Shadows
        [ShaderKeywordFilter.RemoveIf(PrefilteringMode.Remove,     keywordNames: ShaderKeywordStrings.AdditionalLightShadows)]
        [ShaderKeywordFilter.SelectIf(PrefilteringMode.Select,     keywordNames: new string[] {"", ShaderKeywordStrings.AdditionalLightShadows})]
        [ShaderKeywordFilter.SelectIf(PrefilteringMode.SelectOnly, keywordNames: ShaderKeywordStrings.AdditionalLightShadows)]
        [SerializeField] private PrefilteringMode m_PrefilteringModeAdditionalLightShadows = PrefilteringMode.Select;

        // XR Specific keywords
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: new [] {
            ShaderKeywordStrings.BlitSingleSlice, ShaderKeywordStrings.XROcclusionMeshCombined
        })]
        [SerializeField] private bool m_PrefilterXRKeywords = false;

        // Forward+ / Deferred+
        [ShaderKeywordFilter.RemoveIf(PrefilteringMode.Remove,     keywordNames: ShaderKeywordStrings.ClusterLightLoop)]
        [ShaderKeywordFilter.SelectIf(PrefilteringMode.Select,     keywordNames: new [] { "", ShaderKeywordStrings.ClusterLightLoop })]
        [ShaderKeywordFilter.SelectIf(PrefilteringMode.SelectOnly, keywordNames: ShaderKeywordStrings.ClusterLightLoop)]
        [SerializeField] private PrefilteringMode m_PrefilteringModeForwardPlus = PrefilteringMode.Select;

        // Deferred Rendering / Deferred+
        [ShaderKeywordFilter.RemoveIf(PrefilteringMode.Remove, keywordNames: new [] {
            ShaderKeywordStrings._DEFERRED_FIRST_LIGHT, ShaderKeywordStrings._DEFERRED_MAIN_LIGHT,
            ShaderKeywordStrings._DEFERRED_MIXED_LIGHTING, ShaderKeywordStrings._GBUFFER_NORMALS_OCT
        })]
        [SerializeField] private PrefilteringMode m_PrefilteringModeDeferredRendering = PrefilteringMode.Select;

        // Screen Space Occlusion
        [ShaderKeywordFilter.RemoveIf(PrefilteringMode.Remove,     keywordNames: ShaderKeywordStrings.ScreenSpaceOcclusion)]
        [ShaderKeywordFilter.SelectIf(PrefilteringMode.Select,     keywordNames: new [] {"", ShaderKeywordStrings.ScreenSpaceOcclusion})]
        [ShaderKeywordFilter.SelectIf(PrefilteringMode.SelectOnly, keywordNames: ShaderKeywordStrings.ScreenSpaceOcclusion)]
        [SerializeField] private PrefilteringMode m_PrefilteringModeScreenSpaceOcclusion = PrefilteringMode.Select;

        // Rendering Debugger
        [ShaderKeywordFilter.RemoveIf(true, keywordNames:ShaderKeywordStrings.DEBUG_DISPLAY)]
        [SerializeField] private bool m_PrefilterDebugKeywords = false;

        // Filters out WriteRenderingLayers if nothing requires the feature
        // TODO: Implement a different filter triggers for different passes (i.e. per-pass filter attributes)
        [ShaderKeywordFilter.ApplyRulesIfNotGraphicsAPI(GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.OpenGLCore)]
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.WriteRenderingLayers)]
        [SerializeField] private bool m_PrefilterWriteRenderingLayers = false;

        // HDR Output
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: new [] {
            HDRKeywords.HDR_INPUT, HDRKeywords.HDR_COLORSPACE_CONVERSION, HDRKeywords.HDR_ENCODING, HDRKeywords.HDR_COLORSPACE_CONVERSION_AND_ENCODING
        })]
        [SerializeField] private bool m_PrefilterHDROutput = false;

        // Alpha Output
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT)]
        [SerializeField] private bool m_PrefilterAlphaOutput = false;

        // Screen Space Ambient Occlusion (SSAO) specific keywords
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ScreenSpaceAmbientOcclusion.k_SourceDepthNormalsKeyword)]
        [SerializeField] private bool m_PrefilterSSAODepthNormals = false;
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ScreenSpaceAmbientOcclusion.k_SourceDepthLowKeyword)]
        [SerializeField] private bool m_PrefilterSSAOSourceDepthLow = false;
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ScreenSpaceAmbientOcclusion.k_SourceDepthMediumKeyword)]
        [SerializeField] private bool m_PrefilterSSAOSourceDepthMedium = false;
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ScreenSpaceAmbientOcclusion.k_SourceDepthHighKeyword)]
        [SerializeField] private bool m_PrefilterSSAOSourceDepthHigh = false;
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ScreenSpaceAmbientOcclusion.k_AOInterleavedGradientKeyword)]
        [SerializeField] private bool m_PrefilterSSAOInterleaved = false;
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ScreenSpaceAmbientOcclusion.k_AOBlueNoiseKeyword)]
        [SerializeField] private bool m_PrefilterSSAOBlueNoise = false;
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ScreenSpaceAmbientOcclusion.k_SampleCountLowKeyword)]
        [SerializeField] private bool m_PrefilterSSAOSampleCountLow = false;
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ScreenSpaceAmbientOcclusion.k_SampleCountMediumKeyword)]
        [SerializeField] private bool m_PrefilterSSAOSampleCountMedium = false;
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ScreenSpaceAmbientOcclusion.k_SampleCountHighKeyword)]
        [SerializeField] private bool m_PrefilterSSAOSampleCountHigh = false;

        // Decals
        [ShaderKeywordFilter.ApplyRulesIfNotGraphicsAPI(GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.OpenGLCore)]
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.DBufferMRT1)]
        [SerializeField] private bool m_PrefilterDBufferMRT1 = false;

        [ShaderKeywordFilter.ApplyRulesIfNotGraphicsAPI(GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.OpenGLCore)]
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.DBufferMRT2)]
        [SerializeField] private bool m_PrefilterDBufferMRT2 = false;

        [ShaderKeywordFilter.ApplyRulesIfNotGraphicsAPI(GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.OpenGLCore)]
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.DBufferMRT3)]
        [SerializeField] private bool m_PrefilterDBufferMRT3 = false;

        // Decal Layers - Gets overridden in Decal renderer feature if enabled.
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.DecalLayers)]
        private const bool k_DecalLayersDefault = true;

        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.SoftShadowsLow)]
        [SerializeField] private bool m_PrefilterSoftShadowsQualityLow = false;
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.SoftShadowsMedium)]
        [SerializeField] private bool m_PrefilterSoftShadowsQualityMedium = false;
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.SoftShadowsHigh)]
        [SerializeField] private bool m_PrefilterSoftShadowsQualityHigh = false;
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.SoftShadows)]
        [SerializeField] private bool m_PrefilterSoftShadows = false;

        // Screen Coord Override - Controlled by the Global Settings
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.SCREEN_COORD_OVERRIDE)]
        [SerializeField] private bool m_PrefilterScreenCoord = false;

        // Native Render Pass
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.RenderPassEnabled)]
        [SerializeField] private bool m_PrefilterNativeRenderPass = false;

        // Use legacy lightmaps (GPU resident drawer)
        [ShaderKeywordFilter.ApplyRulesIfNotGraphicsAPI(GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.OpenGLCore)]
        [ShaderKeywordFilter.SelectOrRemove(true, keywordNames: ShaderKeywordStrings.USE_LEGACY_LIGHTMAPS)]
        [SerializeField] private bool m_PrefilterUseLegacyLightmaps = false;

        // Bicubic lightmap sampling
        [ShaderKeywordFilter.RemoveIf(true,  keywordNames: ShaderKeywordStrings.LIGHTMAP_BICUBIC_SAMPLING)]
        [ShaderKeywordFilter.SelectIf(false, keywordNames: ShaderKeywordStrings.LIGHTMAP_BICUBIC_SAMPLING)]
        [SerializeField] private bool m_PrefilterBicubicLightmapSampling = false;

        /// <summary>
        /// Data used for Shader Prefiltering. Gathered after going through the URP Assets,
        /// Renderers and Renderer Features in OnPreprocessBuild() inside ShaderPreprocessor.cs.
        /// </summary>
        internal struct ShaderPrefilteringData
        {
            public PrefilteringMode forwardPlusPrefilteringMode;
            public PrefilteringMode deferredPrefilteringMode;
            public PrefilteringModeMainLightShadows mainLightShadowsPrefilteringMode;
            public PrefilteringModeAdditionalLights additionalLightsPrefilteringMode;
            public PrefilteringMode additionalLightsShadowsPrefilteringMode;
            public PrefilteringMode screenSpaceOcclusionPrefilteringMode;
            public bool useLegacyLightmaps;

            public bool stripXRKeywords;
            public bool stripHDRKeywords;
            public bool stripAlphaOutputKeywords;
            public bool stripDebugDisplay;
            public bool stripScreenCoordOverride;
            public bool stripWriteRenderingLayers;
            public bool stripDBufferMRT1;
            public bool stripDBufferMRT2;
            public bool stripDBufferMRT3;
            public bool stripNativeRenderPass;
            public bool stripSoftShadowsQualityLow;
            public bool stripSoftShadowsQualityMedium;
            public bool stripSoftShadowsQualityHigh;

            public bool stripSSAOBlueNoise;
            public bool stripSSAOInterleaved;
            public bool stripSSAODepthNormals;
            public bool stripSSAOSourceDepthLow;
            public bool stripSSAOSourceDepthMedium;
            public bool stripSSAOSourceDepthHigh;
            public bool stripSSAOSampleCountLow;
            public bool stripSSAOSampleCountMedium;
            public bool stripSSAOSampleCountHigh;

            public bool stripBicubicLightmapSampling;

            public static ShaderPrefilteringData GetDefault()
            {
                return new ShaderPrefilteringData()
                {
                    forwardPlusPrefilteringMode = PrefilteringMode.Select,
                    deferredPrefilteringMode = PrefilteringMode.Select,
                    mainLightShadowsPrefilteringMode = PrefilteringModeMainLightShadows.SelectAll,
                    additionalLightsPrefilteringMode = PrefilteringModeAdditionalLights.SelectAll,
                    additionalLightsShadowsPrefilteringMode = PrefilteringMode.Select,
                    screenSpaceOcclusionPrefilteringMode = PrefilteringMode.Select,
                };
            }
        }

        /// <summary>
        /// Uses the data collected in the OnPreprocessBuild() to set the Shader Prefiltering variables.
        /// </summary>
        /// <param name="prefilteringData"></param>
        internal void UpdateShaderKeywordPrefiltering(ref ShaderPrefilteringData prefilteringData)
        {
            m_PrefilteringModeForwardPlus            = prefilteringData.forwardPlusPrefilteringMode;
            m_PrefilteringModeDeferredRendering      = prefilteringData.deferredPrefilteringMode;
            m_PrefilteringModeMainLightShadows       = prefilteringData.mainLightShadowsPrefilteringMode;
            m_PrefilteringModeAdditionalLight        = prefilteringData.additionalLightsPrefilteringMode;
            m_PrefilteringModeAdditionalLightShadows = prefilteringData.additionalLightsShadowsPrefilteringMode;
            m_PrefilteringModeScreenSpaceOcclusion   = prefilteringData.screenSpaceOcclusionPrefilteringMode;
            m_PrefilterUseLegacyLightmaps            = prefilteringData.useLegacyLightmaps;

            m_PrefilterXRKeywords                    = prefilteringData.stripXRKeywords;
            m_PrefilterHDROutput                     = prefilteringData.stripHDRKeywords;
            m_PrefilterAlphaOutput                   = prefilteringData.stripAlphaOutputKeywords;
            m_PrefilterDebugKeywords                 = prefilteringData.stripDebugDisplay;
            m_PrefilterWriteRenderingLayers          = prefilteringData.stripWriteRenderingLayers;
            m_PrefilterScreenCoord                   = prefilteringData.stripScreenCoordOverride;
            m_PrefilterDBufferMRT1                   = prefilteringData.stripDBufferMRT1;
            m_PrefilterDBufferMRT2                   = prefilteringData.stripDBufferMRT2;
            m_PrefilterDBufferMRT3                   = prefilteringData.stripDBufferMRT3;
            m_PrefilterNativeRenderPass              = prefilteringData.stripNativeRenderPass;

            m_PrefilterSoftShadowsQualityLow         = prefilteringData.stripSoftShadowsQualityLow;
            m_PrefilterSoftShadowsQualityMedium      = prefilteringData.stripSoftShadowsQualityMedium;
            m_PrefilterSoftShadowsQualityHigh        = prefilteringData.stripSoftShadowsQualityHigh;
            m_PrefilterSoftShadows                   = !m_PrefilterSoftShadowsQualityLow || !m_PrefilterSoftShadowsQualityMedium || !m_PrefilterSoftShadowsQualityHigh;

            m_PrefilterSSAOBlueNoise                 = prefilteringData.stripSSAOBlueNoise;
            m_PrefilterSSAOInterleaved               = prefilteringData.stripSSAOInterleaved;
            m_PrefilterSSAODepthNormals              = prefilteringData.stripSSAODepthNormals;
            m_PrefilterSSAOSourceDepthLow            = prefilteringData.stripSSAOSourceDepthLow;
            m_PrefilterSSAOSourceDepthMedium         = prefilteringData.stripSSAOSourceDepthMedium;
            m_PrefilterSSAOSourceDepthHigh           = prefilteringData.stripSSAOSourceDepthHigh;
            m_PrefilterSSAOSampleCountLow            = prefilteringData.stripSSAOSampleCountLow;
            m_PrefilterSSAOSampleCountMedium         = prefilteringData.stripSSAOSampleCountMedium;
            m_PrefilterSSAOSampleCountHigh           = prefilteringData.stripSSAOSampleCountHigh;

            m_PrefilterBicubicLightmapSampling       = prefilteringData.stripBicubicLightmapSampling;
        }
    }
}
#endif
