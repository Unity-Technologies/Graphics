using System;
using System.Collections.Generic;
using UnityEditor.Experimental.Rendering.Universal;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    class UniversalMeshTarget : ITargetImplementation
    {
        public Type targetType => typeof(MeshTarget);
        public string displayName => "Universal";
        public string passTemplatePath => GenerationUtils.GetDefaultTemplatePath("PassMesh.template");
        public string sharedTemplateDirectory => GenerationUtils.GetDefaultSharedTemplateDirectory();

        public bool IsValid(IMasterNode masterNode)
        {
            return (masterNode is PBRMasterNode ||
                    masterNode is UnlitMasterNode ||
                    masterNode is SpriteLitMasterNode ||
                    masterNode is SpriteUnlitMasterNode);
        }

        public bool IsPipelineCompatible(RenderPipelineAsset currentPipeline)
        {
            return currentPipeline is UniversalRenderPipelineAsset;
        }

        public void SetupTarget(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("7395c9320da217b42b9059744ceb1de6")); // MeshTarget
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("ac9e1a400a9ce404c8f26b9c1238417e")); // UniversalMeshTarget

            switch(context.masterNode)
            {
                case PBRMasterNode pbrMasterNode:
                    context.SetupSubShader(UniversalSubShaders.PBR);
                    break;
                case UnlitMasterNode unlitMasterNode:
                    context.SetupSubShader(UniversalSubShaders.Unlit);
                    break;
                case SpriteLitMasterNode spriteLitMasterNode:
                    context.SetupSubShader(UniversalSubShaders.SpriteLit);
                    break;
                case SpriteUnlitMasterNode spriteUnlitMasterNode:
                    context.SetupSubShader(UniversalSubShaders.SpriteUnlit);
                    break;
            }
        }

        public List<BlockFieldDescriptor> GetSupportedBlocks(IMasterNode masterNode)
        {
            var supportedBlocks = new List<BlockFieldDescriptor>();

            // Always supported Blocks
            supportedBlocks.Add(BlockFields.VertexDescription.Position);
            supportedBlocks.Add(BlockFields.VertexDescription.Normal);
            supportedBlocks.Add(BlockFields.VertexDescription.Tangent);
            supportedBlocks.Add(BlockFields.SurfaceDescription.Color);

            // Lit Blocks
            if(masterNode is PBRMasterNode pbrMasterNode)
            {
                if(pbrMasterNode.model == PBRMasterNode.Model.Specular)
                {
                    supportedBlocks.Add(BlockFields.SurfaceDescription.Specular);
                }
                else
                {
                    supportedBlocks.Add(BlockFields.SurfaceDescription.Metallic);
                }

                supportedBlocks.Add(BlockFields.SurfaceDescription.Smoothness);
                supportedBlocks.Add(BlockFields.SurfaceDescription.Normal);
                supportedBlocks.Add(BlockFields.SurfaceDescription.Emission);
                supportedBlocks.Add(BlockFields.SurfaceDescription.Occlusion);
            }

            // TODO: Move Sprite to separate Target?
            if(masterNode is SpriteUnlitMasterNode spriteUnlitMasterNode)
            {
                supportedBlocks.Add(BlockFields.SurfaceDescription.SpriteMask);
            }
            else if(masterNode is SpriteLitMasterNode spriteLitMasterNode)
            {
                supportedBlocks.Add(BlockFields.SurfaceDescription.SpriteMask);
                supportedBlocks.Add(BlockFields.SurfaceDescription.Normal);
            }

            // TODO: This case is needed to determine alpha modes
            // TODO:  We can delete this when switching to Settings objects
            bool isTransparent = false;
            bool isAlphaClip = false;
            switch(masterNode)
            {
                case UnlitMasterNode unlitMaster:
                    isTransparent = unlitMaster.surfaceType == SurfaceType.Transparent;
                    isAlphaClip = unlitMaster.alphaClip.isOn;
                    break;
                case PBRMasterNode pbrMaster:
                    isTransparent = pbrMaster.surfaceType == SurfaceType.Transparent;
                    isAlphaClip = pbrMaster.alphaClip.isOn;
                    break;

                case SpriteLitMasterNode spriteLitMaster:
                    isTransparent = true;
                    break;
                case SpriteUnlitMasterNode spriteUnlitMaster:
                    isTransparent = true;
                    break;
            }

            // Alpha Blocks
            if(isTransparent || isAlphaClip)
            {
                supportedBlocks.Add(BlockFields.SurfaceDescription.Alpha);
            }
            if(isAlphaClip)
            {
                supportedBlocks.Add(BlockFields.SurfaceDescription.ClipThreshold);
            }

            return supportedBlocks;
        }
    }
}
