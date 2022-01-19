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

        private readonly Lazy<HDRPPreprocessShaders> m_PreprocessShaders = new (() => new HDRPPreprocessShaders());
        private HDRPPreprocessShaders preprocessShaders => m_PreprocessShaders.Value;

        public override bool CanRemoveVariant([NotNull] Shader shader, ShaderSnippetData snippetData, ShaderCompilerData inputData)
        {
            // Make sure all the HDRP Assets do not need the Shader
            return ShaderBuildPreprocessor.hdrpAssets
                .All(hdAsset => preprocessShaders.StripShader(hdAsset, shader, snippetData, inputData));
        }

        #endregion

        #region IComputeShaderStripper

        private readonly Lazy<HDRPPreprocessComputeShaders> m_PreprocessComputeShaders = new (() => new HDRPPreprocessComputeShaders());
        private HDRPPreprocessComputeShaders preprocessComputeShaders => m_PreprocessComputeShaders.Value;

        public bool CanRemoveVariant([NotNull] ComputeShader shader, string kernelName, ShaderCompilerData compilerData)
        {
            // Discard any compute shader use for raytracing if none of the RP asset required it
            if (!ShaderBuildPreprocessor.playerNeedRaytracing &&
                ShaderBuildPreprocessor.computeShaderCache.TryGetValue(shader.GetInstanceID(), out _))
                return false;

            // Make sure all the HDRP Assets do not need the Compute Shader
            return ShaderBuildPreprocessor.hdrpAssets
                .All(hdAsset => preprocessComputeShaders.StripShader(hdAsset, shader, kernelName, compilerData));
        }

        public bool CanProcessVariant(ComputeShader shader, string shaderInput) => true;

        #endregion
    }
}
