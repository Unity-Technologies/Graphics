using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    static class ShaderExtensions
    {
        private static readonly ShaderTagId s_RenderPipelineShaderTagId = new ShaderTagId("RenderPipeline");

        /// <summary>
        /// Obtains the tag "RenderPipeline" from a <see cref="Shader"/> and a <see cref="ShaderSnippetData"/>
        /// </summary>
        /// <param name="shader"><see cref="Shader"/> The shader to look for the tag</param>
        /// <param name="snippetData"><see cref="ShaderSnippetData"/></param>
        /// <param name="renderPipeline"><see cref="string"/> containing the value of the tag "RenderPipeline"</param>
        /// <returns>true if the tag is found and has a value</returns>
        public static bool TryToGetRenderPipelineTag([NotNull] this Shader shader, ShaderSnippetData snippetData,  out string renderPipeline)
        {
            renderPipeline = string.Empty;

            try
            {
                var shaderTag = shader.FindPassTagValue((int)snippetData.pass.SubshaderIndex, (int)snippetData.pass.PassIndex, s_RenderPipelineShaderTagId);
                if (string.IsNullOrEmpty(shaderTag.name))
                {
                    return false;
                }

                renderPipeline = shaderTag.name;
                return true;
            }
            catch (Exception e)
            {
                Debug.Log($"Exception {e.Message} on {shader.name} with pass {snippetData.passName}({snippetData.pass.PassIndex}) and subshaderindex {snippetData.pass.SubshaderIndex}");
                return false;
            }
        }
    }
}
