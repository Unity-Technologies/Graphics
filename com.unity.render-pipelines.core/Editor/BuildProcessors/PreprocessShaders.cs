using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEditor.Build;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Implements common functionality for SRP for the <see cref="IPreprocessShaders"/>
    /// </summary>
    internal sealed class PreprocessShaders : ShaderPreprocessor<Shader, ShaderSnippetData>, IPreprocessShaders
    {
        public void OnProcessShader([NotNull] Shader shader, ShaderSnippetData snippetData, IList<ShaderCompilerData> compilerDataList)
        {
            if (!StripShaderVariants(shader, snippetData, compilerDataList))
            {
                Debug.LogError("Error while stripping shader");
            }
        }
    }
}
