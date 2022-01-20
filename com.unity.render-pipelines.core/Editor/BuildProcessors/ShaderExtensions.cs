using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Collection of extensions for <see cref="Shader"/>
    /// </summary>
    public static class ShaderExtensions
    {
        private static readonly ShaderTagId s_RenderPipelineShaderTagId = new ShaderTagId("RenderPipeline");

        /// <summary>
        /// If the tag "RenderPipeline" it is returned
        /// </summary>
        /// <param name="shader"><see cref="Shader"/> The shader to look for the tag</param>
        /// <param name="snippetData"><see cref="ShaderSnippetData"/></param>
        /// <param name="renderPipelineTag"><see cref="string"/> containing the value of the tag "RenderPipeline"</param>
        /// <returns>true if the tag is found and has a value</returns>
        public static bool TryGetRenderPipelineTag([NotNull] this Shader shader, ShaderSnippetData snippetData,  out string renderPipelineTag)
        {
            renderPipelineTag = string.Empty;

            int subshaderIndex = (int)snippetData.pass.SubshaderIndex;
            if (subshaderIndex < 0 || subshaderIndex >= shader.subshaderCount)
                return false;

            int passIndex = (int)snippetData.pass.PassIndex;
            if (passIndex < 0 || passIndex >= shader.GetPassCountInSubshader(subshaderIndex))
                return false;

            var shaderTag = shader.FindPassTagValue(subshaderIndex, passIndex, s_RenderPipelineShaderTagId);
            if (string.IsNullOrEmpty(shaderTag.name))
                return false;

            renderPipelineTag = shaderTag.name;
            return true;
        }
    }
}
