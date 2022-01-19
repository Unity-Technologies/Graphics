using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    internal class HDRPShaderStripper : SRPShaderStripper, IComputeVariantStripper
    {
        readonly List<BaseShaderPreprocessor> m_ShaderProcessorsList;

        /// <summary>
        /// Constructs the stripper for HDRP shader variants
        /// </summary>
        public HDRPShaderStripper()
        {
            m_ShaderProcessorsList = HDShaderUtils.GetBaseShaderPreprocessorList();
        }

        /// <summary>
        /// Specifies the priority of the stripper
        /// </summary>
        public override int priority => 100;

        #region isActive

        private readonly Lazy<bool> m_IsActive = new Lazy<bool>(CheckIfStripperIsActive);

        /// <summary>
        /// Returns if the stripper is active
        /// </summary>
        public override bool isActive => m_IsActive.Value;

        /// <summary>
        /// Returns the shader tag id that identifies the SRP
        /// </summary>
        protected override string shaderTagId => HDRenderPipeline.k_ShaderTagName;

        static bool CheckIfStripperIsActive()
        {
            if (HDRenderPipeline.currentAsset == null)
                return false;

            if (HDRenderPipelineGlobalSettings.Ensure(canCreateNewAsset: false) == null)
                return false;

            // TODO: Grab correct configuration/quality asset.
            var hdPipelineAssets = ShaderBuildPreprocessor.hdrpAssets;

            // Test if striping is enabled in any of the found HDRP assets.
            return hdPipelineAssets.Count != 0 && hdPipelineAssets.Any(a => a.allowShaderVariantStripping);
        }

        #endregion

        #region IShaderStripper

        /// <summary>
        /// Specifies if the variant is not used by the stripper, so it can be removed from the build
        /// </summary>
        /// <param name="shader">The shader to check if the variant can be stripped</param>
        /// <param name="snippetData">The variant to check if it can be stripped</param>
        /// <param name="inputData">The <see cref="ShaderCompilerData"/></param>
        /// <returns>If the Shader Variant can be stripped</returns>
        public override bool CanRemoveVariant([NotNull] Shader shader, ShaderSnippetData snippetData, ShaderCompilerData inputData)
        {
            // Remove the input by default, until we find a HDRP Asset in the list that needs it.
            bool removeInput = true;

            foreach (var hdAsset in ShaderBuildPreprocessor.hdrpAssets)
            {
                // Call list of strippers
                // Note that all strippers cumulate each other, so be aware of any conflict here
                var strippedByPreprocessor = m_ShaderProcessorsList
                    .Any(shaderPreprocessor => shaderPreprocessor.ShadersStripper(hdAsset, shader, snippetData, inputData));

                if (strippedByPreprocessor)
                    continue;

                removeInput = false;
                break;
            }

            return removeInput;
        }

        #endregion

        #region IComputeShaderStripper

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

        /// <summary>
        /// Specifies if the variant is not used by the stripper, so it can be removed from the build
        /// </summary>
        /// <param name="shader">The shader to check if the variant can be stripped</param>
        /// <param name="kernelName">The variant to check if it can be stripped</param>
        /// <param name="compilerData">The <see cref="ShaderCompilerData"/></param>
        /// <returns>If the Shader Variant can be stripped</returns>
        public bool CanRemoveVariant([NotNull] ComputeShader shader, string kernelName, ShaderCompilerData compilerData)
        {
            // Discard any compute shader use for raytracing if none of the RP asset required it
            if (!ShaderBuildPreprocessor.playerNeedRaytracing &&
                ShaderBuildPreprocessor.computeShaderCache.TryGetValue(shader.GetInstanceID(), out _))
                return false;

            bool removeInput = true;

            foreach (var hdAsset in ShaderBuildPreprocessor.hdrpAssets)
            {
                if (!StripShader(hdAsset, shader, kernelName, compilerData))
                {
                    removeInput = false;
                    break;
                }
            }

            return removeInput;
        }

        /// <summary>
        /// Returns if the variant is processed by the stripper
        /// </summary>
        /// <param name="shader">The shader to check if the variant can be stripped</param>
        /// <param name="shaderInput">The variant to check if it can be stripped</param>
        /// <returns>If the Shader Variant is processed by the stripper</returns>
        public bool CanProcessVariant(ComputeShader shader, string shaderInput) => true;

        #endregion
    }
}
