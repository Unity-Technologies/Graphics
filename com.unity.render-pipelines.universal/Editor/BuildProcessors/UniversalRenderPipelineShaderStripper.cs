using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal class UniversalRenderPipelineShaderStripper : SRPShaderStripper
    {
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

        private readonly Lazy<ShaderPreprocessor> m_PreprocessShaders = new (() => new ShaderPreprocessor());
        private ShaderPreprocessor preprocessShaders => m_PreprocessShaders.Value;

        public override bool CanRemoveVariant([NotNull] Shader shader, ShaderSnippetData snippetData, ShaderCompilerData inputData)
        {
            return preprocessShaders.StripShader(shader, snippetData, inputData);
        }

        /// <summary>
        /// Returns the shader tag id that identifies the SRP
        /// </summary>
        protected override string shaderTagId => UniversalRenderPipeline.k_ShaderTagName;

        #endregion
    }
}
