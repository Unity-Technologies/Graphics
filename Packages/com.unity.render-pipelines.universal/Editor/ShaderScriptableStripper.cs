using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using System.Diagnostics.CodeAnalysis;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Class used for Scriptable Shader keyword stripping in URP.
    /// </summary>
    internal class ShaderScriptableStripper : IShaderVariantStripper, IShaderVariantStripperScope
    {
        public bool active => UniversalRenderPipeline.asset != null;

        // Shaders
        Shader m_BokehDepthOfField = Shader.Find("Hidden/Universal Render Pipeline/BokehDepthOfField");
        Shader m_GaussianDepthOfField = Shader.Find("Hidden/Universal Render Pipeline/GaussianDepthOfField");
        Shader m_CameraMotionBlur = Shader.Find("Hidden/Universal Render Pipeline/CameraMotionBlur");
        Shader m_PaniniProjection = Shader.Find("Hidden/Universal Render Pipeline/PaniniProjection");
        Shader m_Bloom = Shader.Find("Hidden/Universal Render Pipeline/Bloom");
        Shader m_TerrainLit = Shader.Find("Universal Render Pipeline/Terrain/Lit");
        Shader m_StencilDeferred = Shader.Find("Hidden/Universal Render Pipeline/StencilDeferred");

        // Pass names
        public static readonly string kPassNameUniversal2D = "Universal2D";
        public static readonly string kPassNameGBuffer = "GBuffer";
        public static readonly string kPassNameForwardLit = "ForwardLit";
        public static readonly string kPassNameDepthNormals = "DepthNormals";

        // Keywords
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
        LocalKeyword m_ProbeVolumesL1;
        LocalKeyword m_ProbeVolumesL2;


        private LocalKeyword TryGetLocalKeyword(Shader shader, string name)
        {
            return shader.keywordSpace.FindKeyword(name);
        }

        private void InitializeLocalShaderKeywords([DisallowNull] Shader shader)
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

            m_ScreenCoordOverride = TryGetLocalKeyword(shader, ShaderKeywordStrings.SCREEN_COORD_OVERRIDE);
            m_ProbeVolumesL1 = TryGetLocalKeyword(shader, "PROBE_VOLUMES_L1");
            m_ProbeVolumesL2 = TryGetLocalKeyword(shader, "PROBE_VOLUMES_L2");

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

        private bool IsFeatureEnabled(ShaderFeatures featureMask, ShaderFeatures feature)
        {
            return (featureMask & feature) != 0;
        }

        private bool IsFeatureEnabled(VolumeFeatures featureMask, VolumeFeatures feature)
        {
            return (featureMask & feature) != 0;
        }

        private bool IsGLDevice(ShaderCompilerData variantData)
        {
            return variantData.shaderCompilerPlatform == ShaderCompilerPlatform.GLES3x || variantData.shaderCompilerPlatform == ShaderCompilerPlatform.OpenGLCore;
        }

        private bool StripUnusedFeatures(ShaderFeatures features, Shader shader, ShaderSnippetData passData, ShaderCompilerData variantData)
        {
            bool stripDebugDisplayShaders = ShaderBuildPreprocessor.s_StripDebugDisplayShaders;
            if (stripDebugDisplayShaders && variantData.shaderKeywordSet.IsEnabled(m_DebugDisplay))
                return true;

            if (ShaderBuildPreprocessor.s_StripScreenCoordOverrideVariants && variantData.shaderKeywordSet.IsEnabled(m_ScreenCoordOverride))
                return true;

            var stripTool = new ShaderStripTool<ShaderFeatures>(features, shader, passData, variantData.shaderKeywordSet, variantData.shaderCompilerPlatform);

            // strip main light shadows, cascade and screen variants
            if (IsFeatureEnabled(features, ShaderFeatures.ShadowsKeepOffVariants))
            {
                if (stripTool.StripMultiCompileKeepOffVariant(
                        m_MainLightShadows, ShaderFeatures.MainLightShadows,
                        m_MainLightShadowsCascades, ShaderFeatures.MainLightShadowsCascade,
                        m_MainLightShadowsScreen, ShaderFeatures.ScreenSpaceShadows))
                {
                    // Here we want to keep MainLightShadows and the OFF variants...
                    bool canRemove = variantData.shaderKeywordSet.IsEnabled(m_MainLightShadowsCascades) || variantData.shaderKeywordSet.IsEnabled(m_MainLightShadowsScreen);
                    if (canRemove)
                        return true;
                }
            }
            else
            {
                if (stripTool.StripMultiCompile(
                        m_MainLightShadows, ShaderFeatures.MainLightShadows,
                        m_MainLightShadowsCascades, ShaderFeatures.MainLightShadowsCascade,
                        m_MainLightShadowsScreen, ShaderFeatures.ScreenSpaceShadows))
                {
                    // Here we want to only keep the MainLightShadows variant...
                    if (!variantData.shaderKeywordSet.IsEnabled(m_MainLightShadows))
                        return true;
                }
            }

            // TODO: Strip off variants once we have global soft shadows option for forcing instead as support
            if (stripTool.StripMultiCompileKeepOffVariant(m_SoftShadows, ShaderFeatures.SoftShadows))
                return true;

            if (stripTool.StripMultiCompileKeepOffVariant(m_HdrGrading, ShaderFeatures.HdrGrading))
                return true;

            // Left for backward compatibility
            if (variantData.shaderKeywordSet.IsEnabled(m_MixedLightingSubtractive) && !IsFeatureEnabled(features, ShaderFeatures.MixedLighting))
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

            if (stripTool.StripMultiCompileKeepOffVariant(m_RenderPassEnabled, ShaderFeatures.RenderPassEnabled))
                return true;

            // No additional light shadows
            if (IsFeatureEnabled(features, ShaderFeatures.ShadowsKeepOffVariants))
            {
                if (stripTool.StripMultiCompileKeepOffVariant(m_AdditionalLightShadows, ShaderFeatures.AdditionalLightShadows))
                    return true;
            }
            else if (stripTool.StripMultiCompile(m_AdditionalLightShadows, ShaderFeatures.AdditionalLightShadows))
                return true;

            // Reflection probes
            if (stripTool.StripMultiCompile(m_ReflectionProbeBlending, ShaderFeatures.ReflectionProbeBlending))
                return true;

            if (stripTool.StripMultiCompile(m_ReflectionProbeBoxProjection, ShaderFeatures.ReflectionProbeBoxProjection))
                return true;

            // Shadow caster punctual light strip
            if (passData.passType == PassType.ShadowCaster
                && ShaderUtil.PassHasKeyword(shader, passData.pass, m_CastingPunctualLightShadow, passData.shaderType, variantData.shaderCompilerPlatform))
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

            // Forward Plus
            if (stripTool.StripMultiCompile(m_ForwardPlus, ShaderFeatures.ForwardPlus))
                return true;

            // Forward Plus doesn't use Vertex or the Pixel Light variants.
            // It enables the Pixel keyword through a define.
            if (IsFeatureEnabled(features, ShaderFeatures.ForwardPlus))
            {
                if (IsFeatureEnabled(features, ShaderFeatures.AdditionalLightsVertex))
                    return true;

                if (IsFeatureEnabled(features, ShaderFeatures.AdditionalLights))
                    return true;
            }

            // Additional light are shaded per-vertex or per-pixel.
            if (IsFeatureEnabled(features, ShaderFeatures.AdditionalLightsKeepOffVariants))
            {
                if (stripTool.StripMultiCompileKeepOffVariant(m_AdditionalLightsVertex, ShaderFeatures.AdditionalLightsVertex, m_AdditionalLightsPixel, ShaderFeatures.AdditionalLights))
                    return true;
            }
            else
            {
                if (stripTool.StripMultiCompile(m_AdditionalLightsVertex, ShaderFeatures.AdditionalLightsVertex, m_AdditionalLightsPixel, ShaderFeatures.AdditionalLights))
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

            if (IsGLDevice(variantData))
            {
                // Decal DBuffer is not supported on gl
                if (variantData.shaderKeywordSet.IsEnabled(m_DBufferMRT1) ||
                    variantData.shaderKeywordSet.IsEnabled(m_DBufferMRT2) ||
                    variantData.shaderKeywordSet.IsEnabled(m_DBufferMRT3))
                    return true;

                // Rendering layers are not supported on gl
                if (variantData.shaderKeywordSet.IsEnabled(m_DecalLayers))
                    return true;

                // Rendering layers are not supported on gl
                if (variantData.shaderKeywordSet.IsEnabled(m_WriteRenderingLayers))
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

                // Decal Layers
                if (stripTool.StripMultiCompile(m_DecalLayers, ShaderFeatures.DecalLayers))
                    return true;

                // Write Rendering Layers
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

            // TODO: Test against lightMode tag instead.
            if (passData.passName == kPassNameGBuffer)
            {
                if (!IsFeatureEnabled(features, ShaderFeatures.DeferredShading))
                    return true;
            }

            // Do not strip accurateGbufferNormals on Mobile Vulkan as some GPUs do not support R8G8B8A8_SNorm,
            // which then force us to use accurateGbufferNormals
            if (variantData.shaderCompilerPlatform != ShaderCompilerPlatform.Vulkan)
                if (stripTool.StripMultiCompile(m_GbufferNormalsOct, ShaderFeatures.AccurateGbufferNormals))
                    return true;

            // Decal Normal Blend
            if (stripTool.StripMultiCompile(
                m_DecalNormalBlendLow, ShaderFeatures.DecalNormalBlendLow,
                m_DecalNormalBlendMedium, ShaderFeatures.DecalNormalBlendMedium,
                m_DecalNormalBlendHigh, ShaderFeatures.DecalNormalBlendHigh))
                return true;

            if (stripTool.StripMultiCompileKeepOffVariant(m_LightCookies, ShaderFeatures.LightCookies))
                return true;

            if (stripTool.StripMultiCompileKeepOffVariant(m_ProbeVolumesL1, ShaderFeatures.ProbeVolumeL1, m_ProbeVolumesL2, ShaderFeatures.ProbeVolumeL2))
                return true;

            return false;
        }

        private bool StripVolumeFeatures(VolumeFeatures features, Shader shader, ShaderSnippetData passData, ShaderCompilerData variantData)
        {
            var stripTool = new ShaderStripTool<VolumeFeatures>(features, shader, passData, variantData.shaderKeywordSet, variantData.shaderCompilerPlatform);

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

        private bool StripUnsupportedVariants(ShaderCompilerData variantData)
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

        private bool StripInvalidVariants(ShaderFeatures features, Shader shader, ShaderCompilerData variantData)
        {
            // HDR Output
            if (!HDROutputUtils.IsShaderVariantValid(variantData.shaderKeywordSet, PlayerSettings.useHDRDisplay))
                return true;

            // Strip terrain holes
            if (shader == m_TerrainLit)
                if (!IsFeatureEnabled(features, ShaderFeatures.TerrainHoles) && variantData.shaderKeywordSet.IsEnabled(m_AlphaTestOn))
                    return true;

            // Strip Soft Shadows if shadows are disabled for both Main and Additional Lights...
            bool isMainShadowNoCascades = variantData.shaderKeywordSet.IsEnabled(m_MainLightShadows);
            bool isMainShadowCascades = variantData.shaderKeywordSet.IsEnabled(m_MainLightShadowsCascades);
            bool isMainShadowScreen = variantData.shaderKeywordSet.IsEnabled(m_MainLightShadowsScreen);
            bool isMainShadow = isMainShadowNoCascades || isMainShadowCascades || isMainShadowScreen;
            bool areAdditionalShadowsEnabled = variantData.shaderKeywordSet.IsEnabled(m_AdditionalLightShadows);
            bool isShadowVariant = isMainShadow || areAdditionalShadowsEnabled;
            if (!isShadowVariant && variantData.shaderKeywordSet.IsEnabled(m_SoftShadows))
                return true;

            return false;
        }

        private bool StripUnusedPass(ShaderFeatures features, ShaderSnippetData passData, ShaderCompilerData variantData)
        {
            // Strip 2D Passes if there are no 2D renderers...
            if (ShaderBuildPreprocessor.s_Strip2DPasses)
                if (passData.passName == kPassNameUniversal2D)
                    return true;

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
            if (!IsGLDevice(variantData))
            {
                // DBuffer
                if (passData.passName == DecalShaderPassNames.DBufferMesh
                    || passData.passName == DecalShaderPassNames.DBufferProjector
                    || passData.passName == DecalShaderPassNames.DecalMeshForwardEmissive
                    || passData.passName == DecalShaderPassNames.DecalProjectorForwardEmissive)
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

        private bool StripUnusedShaders(ShaderFeatures features, Shader shader)
        {
            if (!ShaderBuildPreprocessor.s_StripUnusedVariants)
                return false;

            // Remove DeferredStencil if Deferred Rendering is not used
            if (shader == m_StencilDeferred && !IsFeatureEnabled(features, ShaderFeatures.DeferredShading))
                return true;

            return false;
        }

        public bool CanRemoveVariant([DisallowNull] Shader shader, ShaderSnippetData passData, ShaderCompilerData variantData)
        {
            // All feature sets need to have this variant unused to be stripped out.
            bool removeInput = true;
            for (var index = 0; index < ShaderBuildPreprocessor.supportedFeaturesList.Count; index++)
            {
                ShaderFeatures supportedFeatures = ShaderBuildPreprocessor.supportedFeaturesList[index];

                if (StripUnusedShaders(supportedFeatures, shader))
                    continue;

                if (StripUnusedPass(supportedFeatures, passData, variantData))
                    continue;

                if (StripInvalidVariants(supportedFeatures, shader, variantData))
                    continue;

                if (StripUnsupportedVariants(variantData))
                    continue;

                if (StripUnusedFeatures(supportedFeatures, shader, passData, variantData))
                    continue;

                removeInput = false;
                break;
            }

            // Check PostProcessing variants...
            if (!removeInput && ShaderBuildPreprocessor.s_StripUnusedPostProcessingVariants)
                if (StripVolumeFeatures(ShaderBuildPreprocessor.volumeFeatures, shader, passData, variantData))
                    removeInput = true;

            return removeInput;
        }

        public void BeforeShaderStripping(Shader shader)
        {
            InitializeLocalShaderKeywords(shader);
        }

        public void AfterShaderStripping(Shader shader)
        {

        }
    }
}
