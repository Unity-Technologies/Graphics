using System;
using UnityEditor.Experimental.Rendering.Universal;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    
    class DOTSUniversalMeshTarget : UniversalMeshTarget
    {
        public override void SetupTarget(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("7395c9320da217b42b9059744ceb1de6")); // MeshTarget
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("ac9e1a400a9ce404c8f26b9c1238417e")); // UniversalMeshTarget

            switch(context.masterNode)
            {
                case PBRMasterNode pbrMasterNode:
                    context.SetupSubShader(UniversalSubShaders.DOTSPBR);
                    break;
                case UnlitMasterNode unlitMasterNode:
                    context.SetupSubShader(UniversalSubShaders.DOTSUnlit);
                    break;
            }
        }
    }
    
    class UniversalMeshTarget : ITargetImplementation
    {
        public Type targetType => typeof(MeshTarget);
        public string displayName => "Universal";
        public string passTemplatePath => GenerationUtils.GetDefaultTemplatePath("PassMesh.template");
        public string sharedTemplateDirectory => GenerationUtils.GetDefaultSharedTemplateDirectory();

        public bool IsValid(IMasterNode masterNode)
        {
            return GetSubShaderDescriptorFromMasterNode(masterNode) != null;
        }

        public bool IsPipelineCompatible(RenderPipelineAsset currentPipeline)
        {
            return currentPipeline is UniversalRenderPipelineAsset;
        }

        public virtual void SetupTarget(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("7395c9320da217b42b9059744ceb1de6")); // MeshTarget
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("ac9e1a400a9ce404c8f26b9c1238417e")); // UniversalMeshTarget

            var subShader = GetSubShaderDescriptorFromMasterNode(context.masterNode);
            if (subShader != null)
                context.SetupSubShader(subShader.Value);
        }

        public SubShaderDescriptor? GetSubShaderDescriptorFromMasterNode(IMasterNode masterNode)
        {
            switch (masterNode)
            {
                case PBRMasterNode _:
                    return UniversalSubShaders.PBR;
                case UnlitMasterNode _:
                    return UniversalSubShaders.Unlit;
                case SpriteLitMasterNode _:
                    return UniversalSubShaders.SpriteLit;
                case SpriteUnlitMasterNode _:
                    return UniversalSubShaders.SpriteUnlit;
                default:
                    return null;
            }
        }
    }
}
