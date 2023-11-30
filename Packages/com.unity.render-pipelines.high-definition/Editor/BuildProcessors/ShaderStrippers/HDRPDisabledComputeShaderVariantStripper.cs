using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDRPDisabledComputeShaderVariantStripper : IComputeShaderVariantStripper
    {
        public bool active => !HDRPBuildData.instance.buildingPlayerForHDRenderPipeline;

        public bool CanRemoveVariant([DisallowNull] ComputeShader shader, string _, ShaderCompilerData __)
        {
            var shaderInstanceID = shader.GetInstanceID();

            if (HDRPBuildData.instance.computeShaderCache.ContainsKey(shaderInstanceID))
                return true;

            if (HDRPBuildData.instance.rayTracingComputeShaderCache.ContainsKey(shaderInstanceID))
                return true;

            return false;
        }
    }
}
