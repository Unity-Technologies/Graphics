using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal class UniversalRenderPipelineShaderStripper : SRPShaderStripper
    {
        private readonly ShaderPreprocessor m_Preprocessor = new ShaderPreprocessor();

        /// <summary>
        /// Specifies the priority of the stripper
        /// </summary>
        public override int priority => 50;

        #region isActive

        private readonly Lazy<bool> m_IsActive = new Lazy<bool>(CheckIfStripperIsActive);

        /// <summary>
        /// Returns if the stripper is active
        /// </summary>
        public override bool isActive => m_IsActive.Value;

        static bool CheckIfStripperIsActive()
        {
            if (UniversalRenderPipeline.asset == null)
                return false;

            if (UniversalRenderPipelineGlobalSettings.Ensure(canCreateNewAsset: false) == null)
                return false;

            return true;
        }

        #endregion

        #region IShaderStripper

        /// <summary>
        /// Returns the shader tag id that identifies the SRP
        /// </summary>
        protected override string shaderTagId => UniversalRenderPipeline.k_ShaderTagName;

        /// <summary>
        /// Specifies if the variant is not used by the stripper, so it can be removed from the build
        /// </summary>
        /// <param name="shader">The shader to check if the variant can be stripped</param>
        /// <param name="snippetData">The variant to check if it can be stripped</param>
        /// <param name="inputData">The <see cref="ShaderCompilerData"/></param>
        /// <returns>If the Shader Variant can be stripped</returns>
        public override bool CanRemoveVariant([NotNull] Shader shader, ShaderSnippetData snippetData, ShaderCompilerData inputData)
        {
            m_Preprocessor.InitializeLocalShaderKeywords(shader);

            // Remove the input by default, until we find a Build Preprocessor in the list that needs it.
            bool removeInput = ShaderBuildPreprocessor.supportedFeaturesList.All(supportedFeatures => m_Preprocessor.StripUnused(supportedFeatures, shader, snippetData, inputData));

            if (UniversalRenderPipelineGlobalSettings.instance?.stripUnusedPostProcessingVariants == true)
            {
                if (!removeInput && m_Preprocessor.StripVolumeFeatures(ShaderBuildPreprocessor.volumeFeatures, shader, snippetData, inputData))
                {
                    removeInput = true;
                }
            }

            return removeInput;
        }

        #endregion
    }
}
