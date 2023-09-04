using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDRPDisabledComputeShaderVariantStripper : IComputeShaderVariantStripper
    {
        public bool active => !HDRPBuildData.instance.buildingPlayerForHDRenderPipeline;

        public bool CanRemoveVariant([DisallowNull] ComputeShader shader, string shaderVariant, ShaderCompilerData shaderCompilerData)
        {
            return HDRPBuildData.instance.computeShaderCache.ContainsKey(shader.GetInstanceID());
        }
    }
}
