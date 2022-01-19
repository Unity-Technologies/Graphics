using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    static class ShaderExtensions
    {
        private static readonly ShaderTagId s_RenderPipelineShaderTagId = new ShaderTagId("RenderPipeline");

        /// <summary>
        /// If the tag "RenderPipeline" it is returned
        /// </summary>
        /// <param name="shader"><see cref="Shader"/> The shader to look for the tag</param>
        /// <param name="snippetData"><see cref="ShaderSnippetData"/></param>
        /// <param name="renderPipeline"><see cref="string"/> containing the value of the tag "RenderPipeline"</param>
        /// <returns>true if the tag is found and has a value</returns>
        public static bool TryToGetRenderPipelineTag([NotNull] this Shader shader, ShaderSnippetData snippetData,  out string renderPipeline)
        {
            renderPipeline = string.Empty;

            var shaderTag = shader.FindPassTagValue((int)snippetData.pass.SubshaderIndex, (int)snippetData.pass.PassIndex, s_RenderPipelineShaderTagId);
            if (string.IsNullOrEmpty(shaderTag.name))
            {
                return false;
            }

            renderPipeline = shaderTag.name;
            return true;
        }
    }
}
