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

            // Screen space shadow variant is exclusive, either we have a variant with dynamic if that support screen space shadow or not
            // either we have a variant that don't support at all. We can't have both at the same time.
            if (inputData.shaderKeywordSet.IsEnabled(m_ScreenSpaceShadowOFFKeywords) && shadowInitParams.supportScreenSpaceShadows)
                return true;

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
