using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class HDMeshTarget : ITargetImplementation
    {
        public Type targetType => typeof(MeshTarget);
        public string displayName => "HDRP";
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
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("326a52113ee5a7d46bf9145976dcb7f6")); // HDRPMeshTarget

            var subShader = GetSubShaderDescriptorFromMasterNode(context.masterNode);
            if (subShader != null)
                context.SetupSubShader(subShader.Value);
        }

        public SubShaderDescriptor? GetSubShaderDescriptorFromMasterNode(IMasterNode masterNode)
        {
            switch (masterNode)
            {
                case PBRMasterNode _:
                    return HDSubShaders.PBR;
                case UnlitMasterNode _:
                    return HDSubShaders.Unlit;
                case HDUnlitMasterNode _:
                    return HDSubShaders.HDUnlit;
                case HDLitMasterNode _:
                    return HDSubShaders.HDLit;
                case EyeMasterNode _:
                    return HDSubShaders.Eye;
                case FabricMasterNode _:
                    return HDSubShaders.Fabric;
                case HairMasterNode _:
                    return HDSubShaders.Hair;
                case StackLitMasterNode _:
                    return HDSubShaders.StackLit;
                default:
                    return null;
            }
        }
    }
}
