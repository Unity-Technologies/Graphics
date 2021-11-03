using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace UnityEditor.Rendering
{
    public interface IStripper<TShader>
    {
        bool isActive { get; }
        bool isProcessed(TShader shader);
    }

    public interface IShaderStripper : IStripper<Shader>
    {
        bool isVariantStripped([NotNull] Shader shader, ShaderSnippetData snippetData, ShaderCompilerData inputData);
    }

#if UNITY_2020_2_OR_NEWER
    public interface IComputeShaderStripper : IStripper<ComputeShader>
    {
        bool isVariantStripped([NotNull] ComputeShader shader, string kernelName, ShaderCompilerData inputData);
    }
#endif
}
