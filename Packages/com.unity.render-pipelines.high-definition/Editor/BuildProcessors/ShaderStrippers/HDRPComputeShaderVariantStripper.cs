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
        protected ShaderKeyword m_WaterAbsorption = new ShaderKeyword("SUPPORT_WATER_ABSORPTION");

        protected HDRenderPipelineRuntimeShaders m_Shaders;

        public HDRPComputeShaderVariantStripper()
        {
            m_Shaders = HDRPBuildData.instance.runtimeShaders;
        }

        // Modify this function to add more stripping clauses
        internal bool StripShader(HDRenderPipelineAsset hdAsset, ComputeShader shader, string kernelName, ShaderCompilerData inputData)
        {
            bool stripDebug = HDRPBuildData.instance.stripDebugVariants;
            var settings = hdAsset.currentPlatformRenderPipelineSettings;

            // Strip debug compute shaders
            if (stripDebug && !settings.supportRuntimeAOVAPI)
            {
                if (shader == m_Shaders.debugLightVolumeCS ||
                    shader == m_Shaders.clearDebugBufferCS ||
                    shader == m_Shaders.debugWaveformCS ||
                    shader == m_Shaders.debugVectorscopeCS ||
                    shader == m_Shaders.probeVolumeSamplingDebugComputeShader)
                    return true;
            }

            // Remove water if disabled
            if (!settings.supportWater)
            {
                if (inputData.shaderKeywordSet.IsEnabled(m_WaterAbsorption))
                    return true;
            }

            // Remove volumetric fog if disabled
            if (!settings.supportVolumetrics)
            {
                if (shader == m_Shaders.volumeVoxelizationCS ||
                    shader == m_Shaders.volumetricLightingCS ||
                    shader == m_Shaders.volumetricLightingFilteringCS)
                    return true;
            }

            // Remove SSR if disabled
            if (!settings.supportSSR)
            {
                if (shader == m_Shaders.screenSpaceReflectionsCS)
                    return true;
            }

            // Remove SSGI if disabled
            if (!settings.supportSSGI)
            {
                if (shader == m_Shaders.screenSpaceGlobalIlluminationCS)
                    return true;
            }

            // Remove SSS if disabled
            if (!settings.supportSubsurfaceScattering)
            {
                if (shader == m_Shaders.subsurfaceScatteringCS ||
                    shader == m_Shaders.subsurfaceScatteringDownsampleCS)
                    return true;
            }

            // Remove Line Rendering if disabled
            if (!settings.supportHighQualityLineRendering)
            {
                if (shader == m_Shaders.lineStagePrepareCS ||
                    shader == m_Shaders.lineStageSetupSegmentCS ||
                    shader == m_Shaders.lineStageShadingSetupCS ||
                    shader == m_Shaders.lineStageRasterBinCS ||
                    shader == m_Shaders.lineStageWorkQueueCS ||
                    shader == m_Shaders.lineStageRasterFineCS)
                    return true;
            }

            // Strip every useless shadow configs
            var shadowInitParams = settings.hdShadowInitParams;

            foreach (var shadowVariant in m_ShadowKeywords.PunctualShadowVariants)
            {
                if (shadowVariant.Key != shadowInitParams.punctualShadowFilteringQuality)
                {
                    if (inputData.shaderKeywordSet.IsEnabled(shadowVariant.Value))
                        return true;
                }
            }

            foreach (var shadowVariant in m_ShadowKeywords.DirectionalShadowVariants)
            {
                if (shadowVariant.Key != shadowInitParams.directionalShadowFilteringQuality)
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
            if (settings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly)
            {
                if (shader == m_Shaders.clearDispatchIndirectCS ||
                    shader == m_Shaders.buildDispatchIndirectCS ||
                    shader == m_Shaders.buildMaterialFlagsCS ||
                    shader == m_Shaders.deferredCS)
                    return true;
            }

            // In deferred only, strip MSAA variants
            if (inputData.shaderKeywordSet.IsEnabled(m_MSAA) && (settings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly))
            {
                return true;
            }

            if (inputData.shaderKeywordSet.IsEnabled(m_ScreenSpaceShadowONKeywords) && !shadowInitParams.supportScreenSpaceShadows)
                return true;

            if (inputData.shaderKeywordSet.IsEnabled(m_EnableAlpha) && !settings.SupportsAlpha())
            {
                return true;
            }

            // Global Illumination
            if (inputData.shaderKeywordSet.IsEnabled(m_ProbeVolumesL1) &&
                (!settings.supportProbeVolume || settings.probeVolumeSHBands != ProbeVolumeSHBands.SphericalHarmonicsL1))
                return true;

            if (inputData.shaderKeywordSet.IsEnabled(m_ProbeVolumesL2) &&
                (!settings.supportProbeVolume || settings.probeVolumeSHBands != ProbeVolumeSHBands.SphericalHarmonicsL2))
                return true;

            // HDR Output
            if (!HDROutputUtils.IsShaderVariantValid(inputData.shaderKeywordSet, PlayerSettings.allowHDRDisplaySupport))
                return true;

            return false;
        }

        public bool active => HDRPBuildData.instance.buildingPlayerForHDRenderPipeline;

        public bool CanRemoveVariant([DisallowNull] ComputeShader shader, string shaderVariant, ShaderCompilerData shaderCompilerData)
        {
            bool removeInput = true;
            foreach (var hdAsset in HDRPBuildData.instance.renderPipelineAssets)
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
            if (!HDRPBuildData.instance.playerNeedRaytracing && HDRPBuildData.instance.rayTracingComputeShaderCache.ContainsKey(shader.GetInstanceID()))
                return true;

            return false;
        }
    }
}
