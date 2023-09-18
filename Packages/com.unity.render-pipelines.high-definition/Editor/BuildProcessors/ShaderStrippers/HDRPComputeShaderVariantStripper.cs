using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDRPComputeShaderVariantStripper : IComputeShaderVariantStripper, IComputeShaderVariantStripperSkipper
    {
        protected ShadowKeywords m_ShadowKeywords = new ShadowKeywords();
        protected ShaderKeyword m_EnableAlpha = new ShaderKeyword("ENABLE_ALPHA");
        protected ShaderKeyword m_MSAA = new ShaderKeyword("ENABLE_MSAA");
        protected ShaderKeyword m_ScreenSpaceShadowOFFKeywords = new ShaderKeyword("SCREEN_SPACE_SHADOWS_OFF");
        protected ShaderKeyword m_ScreenSpaceShadowONKeywords = new ShaderKeyword("SCREEN_SPACE_SHADOWS_ON");
        protected ShaderKeyword m_ProbeVolumesL1 = new ShaderKeyword("PROBE_VOLUMES_L1");
        protected ShaderKeyword m_ProbeVolumesL2 = new ShaderKeyword("PROBE_VOLUMES_L2");

        // Modify this function to add more stripping clauses
        internal bool StripShader(HDRenderPipelineAsset hdAsset, ComputeShader shader, string kernelName, ShaderCompilerData inputData)
        {
            bool stripDebug = !Debug.isDebugBuild || !HDRenderPipelineGlobalSettings.Ensure().supportRuntimeDebugDisplay;

            // Strip debug compute shaders
            if (stripDebug && !hdAsset.currentPlatformRenderPipelineSettings.supportRuntimeAOVAPI)
            {
                if (shader == hdAsset.renderPipelineResources.shaders.debugLightVolumeCS ||
                    shader == hdAsset.renderPipelineResources.shaders.clearDebugBufferCS ||
                    shader == hdAsset.renderPipelineResources.shaders.debugWaveformCS ||
                    shader == hdAsset.renderPipelineResources.shaders.debugVectorscopeCS)
                    return true;
            }

            // Remove water if disabled
            if (!hdAsset.currentPlatformRenderPipelineSettings.supportWater)
            {
                if (shader == hdAsset.renderPipelineResources.shaders.waterSimulationCS ||
                    shader == hdAsset.renderPipelineResources.shaders.fourierTransformCS ||
                    shader == hdAsset.renderPipelineResources.shaders.waterLightingCS)
                    return true;
            }

            // Remove volumetric clouds if disabled
            if (!hdAsset.currentPlatformRenderPipelineSettings.supportVolumetricClouds)
            {
                if (shader == hdAsset.renderPipelineResources.shaders.volumetricCloudsCS ||
                    shader == hdAsset.renderPipelineResources.shaders.volumetricCloudMapGeneratorCS)
                    return true;
            }

            // Remove volumetric fog if disabled
            if (!hdAsset.currentPlatformRenderPipelineSettings.supportVolumetrics)
            {
                if (shader == hdAsset.renderPipelineResources.shaders.volumeVoxelizationCS ||
                    shader == hdAsset.renderPipelineResources.shaders.volumetricLightingCS ||
                    shader == hdAsset.renderPipelineResources.shaders.volumetricLightingFilteringCS)
                    return true;
            }

            // Remove SSR if disabled
            if (!hdAsset.currentPlatformRenderPipelineSettings.supportSSR)
            {
                if (shader == hdAsset.renderPipelineResources.shaders.screenSpaceReflectionsCS)
                    return true;
            }

            // Remove SSGI if disabled
            if (!hdAsset.currentPlatformRenderPipelineSettings.supportSSGI)
            {
                if (shader == hdAsset.renderPipelineResources.shaders.screenSpaceGlobalIlluminationCS)
                    return true;
            }

            // Remove SSS if disabled
            if (!hdAsset.currentPlatformRenderPipelineSettings.supportSubsurfaceScattering)
            {
                if (shader == hdAsset.renderPipelineResources.shaders.subsurfaceScatteringCS)
                    return true;
            }

            // Strip every useless shadow configs
            var shadowInitParams = hdAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams;

            foreach (var shadowVariant in m_ShadowKeywords.ShadowVariants)
            {
                if (shadowVariant.Key != shadowInitParams.shadowFilteringQuality)
                {
                    if (inputData.shaderKeywordSet.IsEnabled(shadowVariant.Value))
                        return true;
                }
            }

            foreach (var areaShadowVariant in m_ShadowKeywords.AreaShadowVariants)
            {
                if (areaShadowVariant.Key != shadowInitParams.areaShadowFilteringQuality)
                {
                    if (inputData.shaderKeywordSet.IsEnabled(areaShadowVariant.Value))
                        return true;
                }
            }

            // Screen space shadow variant is exclusive, either we have a variant with dynamic if that support screen space shadow or not
            // either we have a variant that don't support at all. We can't have both at the same time.
            if (inputData.shaderKeywordSet.IsEnabled(m_ScreenSpaceShadowOFFKeywords) && shadowInitParams.supportScreenSpaceShadows)
                return true;

            // In forward only, strip deferred shaders
            if (hdAsset.currentPlatformRenderPipelineSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly)
            {
                if (shader == hdAsset.renderPipelineResources.shaders.clearDispatchIndirectCS ||
                    shader == hdAsset.renderPipelineResources.shaders.buildDispatchIndirectCS ||
                    shader == hdAsset.renderPipelineResources.shaders.buildMaterialFlagsCS ||
                    shader == hdAsset.renderPipelineResources.shaders.deferredCS)
                    return true;
            }

            // In deferred only, strip MSAA variants
            if (inputData.shaderKeywordSet.IsEnabled(m_MSAA) && (hdAsset.currentPlatformRenderPipelineSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly))
            {
                return true;
            }

            if (inputData.shaderKeywordSet.IsEnabled(m_ScreenSpaceShadowONKeywords) && !shadowInitParams.supportScreenSpaceShadows)
                return true;

            if (inputData.shaderKeywordSet.IsEnabled(m_EnableAlpha) && !hdAsset.currentPlatformRenderPipelineSettings.SupportsAlpha())
            {
                return true;
            }

            // Global Illumination
            if (inputData.shaderKeywordSet.IsEnabled(m_ProbeVolumesL1) &&
                (!hdAsset.currentPlatformRenderPipelineSettings.supportProbeVolume || hdAsset.currentPlatformRenderPipelineSettings.probeVolumeSHBands != ProbeVolumeSHBands.SphericalHarmonicsL1))
                return true;

            if (inputData.shaderKeywordSet.IsEnabled(m_ProbeVolumesL2) &&
                (!hdAsset.currentPlatformRenderPipelineSettings.supportProbeVolume || hdAsset.currentPlatformRenderPipelineSettings.probeVolumeSHBands != ProbeVolumeSHBands.SphericalHarmonicsL2))
                return true;

            // HDR Output
            if (!HDROutputUtils.IsShaderVariantValid(inputData.shaderKeywordSet, PlayerSettings.allowHDRDisplaySupport))
                return true;

            return false;
        }

        public bool active
        {
            get
            {
                if (HDRenderPipeline.currentAsset == null)
                    return false;

                if (HDRenderPipelineGlobalSettings.Ensure(canCreateNewAsset: false) == null)
                    return false;

                if (ShaderBuildPreprocessor.hdrpAssets.Count == 0)
                    return false;

                return true;
            }
        }

        public bool CanRemoveVariant([DisallowNull] ComputeShader shader, string shaderVariant, ShaderCompilerData shaderCompilerData)
        {
            bool removeInput = true;
            foreach (var hdAsset in ShaderBuildPreprocessor.hdrpAssets)
            {
                if (!StripShader(hdAsset, shader, shaderVariant, shaderCompilerData))
                {
                    removeInput = false;
                    break;
                }
            }

            return removeInput;
        }

        public bool SkipShader([DisallowNull] ComputeShader shader, string shaderVariant)
        {
            // Discard any compute shader use for raytracing if none of the RP asset required it
            if (!ShaderBuildPreprocessor.playerNeedRaytracing && ShaderBuildPreprocessor.computeShaderCache.TryGetValue(shader.GetInstanceID(), out ComputeShader _))
                return true;

            return false;
        }
    }
}
