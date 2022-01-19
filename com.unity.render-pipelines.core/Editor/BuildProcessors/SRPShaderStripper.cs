using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    public abstract class SRPShaderStripper : IShaderVariantStripper
    {
        /// <summary>
        /// Specifies the priority of the stripper
        /// </summary>
        public abstract int priority { get; }

        /// <summary>
        /// Returns if the stripper is active
        /// </summary>
        public abstract bool isActive { get; }

        /// <summary>
        /// Returns the shader tag id that identifies the SRP
        /// </summary>
        protected abstract string shaderTagId { get; }

        #region IShaderStripper

        /// <summary>
        /// Specifies if the variant is not used by the stripper, so it can be removed from the build
        /// </summary>
        /// <param name="shader">The shader to check if the variant can be stripped</param>
        /// <param name="snippetData">The variant to check if it can be stripped</param>
        /// <param name="inputData">The <see cref="ShaderCompilerData"/></param>
        /// <returns>If the Shader Variant can be stripped</returns>
        public abstract bool CanRemoveVariant([NotNull] Shader shader, ShaderSnippetData snippetData,
            ShaderCompilerData inputData);

        /// <summary>
        /// Returns if the variant is processed by the stripper
        /// </summary>
        /// <param name="shader">The shader to check if the variant can be stripped</param>
        /// <param name="snippetData">The variant to check if it can be stripped</param>
        /// <returns>If the Shader Variant is processed by the stripper</returns>
        public bool CanProcessVariant([NotNull]Shader shader, ShaderSnippetData snippetData)
        {
            return shader.TryToGetRenderPipelineTag(snippetData, out string tagName) && tagName.Equals(shaderTagId);
        }

        #endregion
    }
}
