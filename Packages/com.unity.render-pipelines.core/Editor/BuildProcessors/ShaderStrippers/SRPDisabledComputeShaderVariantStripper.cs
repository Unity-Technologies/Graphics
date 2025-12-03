using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace UnityEditor.Rendering
{
    class SRPDisabledComputeShaderVariantStripper : IComputeShaderVariantStripper
    {
        public bool active => !CoreBuildData.instance.buildingPlayerForRenderPipeline;

#pragma warning disable 618 // Todo(@daniel.andersen): Remove deprecated API usage
        public bool CanRemoveVariant([DisallowNull] ComputeShader shader, string _, ShaderCompilerData __)
            => CoreBuildData.instance.computeShaderCache.ContainsKey(shader.GetEntityId());
#pragma warning restore 618
    }
}