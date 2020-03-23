using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class HDRaytracingMeshTarget : ITargetImplementation
    {
        public Type targetType => typeof(MeshTarget);
        public string displayName => "HDRP Raytracing";
        public string passTemplatePath => string.Empty;
        public string sharedTemplateDirectory => $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph/Templates";

        public bool IsValid(IMasterNode masterNode)
        {
            return GetSubShaderDescriptorFromMasterNode(masterNode) != null;
        }
        public bool IsPipelineCompatible(RenderPipelineAsset currentPipeline)
        {
            return currentPipeline is HDRenderPipelineAsset;
        }

        public void SetupTarget(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("7395c9320da217b42b9059744ceb1de6")); // MeshTarget
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("a3b60b90b9eb3e549adfd57a75e77811")); // HDRPRaytracingMeshTarget

            var subShader = GetSubShaderDescriptorFromMasterNode(context.masterNode);
            if (subShader != null)
                context.SetupSubShader(subShader.Value);
        }

        public SubShaderDescriptor? GetSubShaderDescriptorFromMasterNode(IMasterNode masterNode)
        {
            switch (masterNode)
            {
                case FabricMasterNode _:
                    return HDSubShaders.FabricRaytracing;
                case HDLitMasterNode _:
                    return HDSubShaders.HDLitRaytracing;
                case HDUnlitMasterNode _:
                    return HDSubShaders.HDUnlitRaytracing;
                default:
                    return null;
            }
        }
    }
}
