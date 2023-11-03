using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace UnityEditor.Rendering
{
    class SRPDisabledComputeShaderVariantStripper : IComputeShaderVariantStripper
    {
        public bool active => !CoreBuildData.instance.buildingPlayerForRenderPipeline;

        public bool CanRemoveVariant([DisallowNull] ComputeShader shader, string _, ShaderCompilerData __)
            => CoreBuildData.instance.computeShaderCache.ContainsKey(shader.GetInstanceID());
    }
}