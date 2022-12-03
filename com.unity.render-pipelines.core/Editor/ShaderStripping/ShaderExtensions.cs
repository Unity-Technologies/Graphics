using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Collection of extensions for <see cref="Shader"/>
    /// </summary>
    static class ShaderExtensions
    {
        private static readonly ShaderTagId s_RenderPipelineShaderTagId = new ShaderTagId("RenderPipeline");

        /// <summary>
        /// Tries to find the "RenderPipeline" on the shader by the given <see cref="ShaderSnippetData"/>
        /// </summary>
        /// <param name="shader"><see cref="Shader"/> The shader to look for the tag</param>
        /// <param name="snippetData"><see cref="ShaderSnippetData"/></param>
        /// <param name="renderPipelineTag"><see cref="string"/> containing the value of the tag "RenderPipeline"</param>
        /// <returns>true if the tag is found and has a value</returns>
        public static bool TryGetRenderPipelineTag([DisallowNull] this Shader shader, ShaderSnippetData snippetData, [NotNullWhen(true)] out string renderPipelineTag)
        {
            renderPipelineTag = string.Empty;

            var shaderData = ShaderUtil.GetShaderData(shader);
            if (shaderData == null)
                return false;

            int subshaderIndex = (int)snippetData.pass.SubshaderIndex;
            if (subshaderIndex < 0 || subshaderIndex >= shader.subshaderCount)
                return false;

            var subShader = shaderData.GetSerializedSubshader(subshaderIndex);
            if (subShader == null)
                return false;

            var shaderTag = subShader.FindTagValue(s_RenderPipelineShaderTagId);
            if (string.IsNullOrEmpty(shaderTag.name))
                return false;

            renderPipelineTag = shaderTag.name;
            return true;
        }
    }
}
