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
            return (masterNode is PBRMasterNode ||
                    masterNode is UnlitMasterNode ||
                    masterNode is HDUnlitMasterNode ||
                    masterNode is HDLitMasterNode ||
                    masterNode is StackLitMasterNode ||
                    masterNode is HairMasterNode ||
                    masterNode is FabricMasterNode ||
                    masterNode is EyeMasterNode);
        }
        public bool IsPipelineCompatible(RenderPipelineAsset currentPipeline)
        {
            return currentPipeline is HDRenderPipelineAsset;
        }

        public void SetupTarget(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("7395c9320da217b42b9059744ceb1de6")); // MeshTarget
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("326a52113ee5a7d46bf9145976dcb7f6")); // HDRPMeshTarget

            switch(context.masterNode)
            {
                case PBRMasterNode pbrMasterNode:
                    context.SetupSubShader(HDSubShaders.PBR);
                    break;
                case UnlitMasterNode unlitMasterNode:
                    context.SetupSubShader(HDSubShaders.Unlit);
                    break;
                case HDUnlitMasterNode hdUnlitMasterNode:
                    context.SetupSubShader(HDSubShaders.HDUnlit);
                    break;
                case HDLitMasterNode hdLitMasterNode:
                    context.SetupSubShader(HDSubShaders.HDLit);
                    break;
                case EyeMasterNode eyeMasterNode:
                    context.SetupSubShader(HDSubShaders.Eye);
                    break;
                case FabricMasterNode fabricMasterNode:
                    context.SetupSubShader(HDSubShaders.Fabric);
                    break;
                case HairMasterNode hairMasterNode:
                    context.SetupSubShader(HDSubShaders.Hair);
                    break;
                case StackLitMasterNode stackLitMasterNode:
                    context.SetupSubShader(HDSubShaders.StackLit);
                    break;
            }
        }
    }
}
