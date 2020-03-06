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
        public Type dataType => typeof(UniversalMeshTargetData);
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
            if(!(context.data is UniversalMeshTargetData universalData))
                return;
            
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("7395c9320da217b42b9059744ceb1de6")); // MeshTarget
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("ac9e1a400a9ce404c8f26b9c1238417e")); // UniversalMeshTarget

            switch(universalData.materialType)
            {
                case UniversalMeshTargetData.MaterialType.Lit:
                    context.SetupSubShader(UniversalSubShaders.PBR);
                    break;
                case UniversalMeshTargetData.MaterialType.Unlit:
                    context.SetupSubShader(UniversalSubShaders.Unlit);
                    break;
                case UniversalMeshTargetData.MaterialType.SpriteLit:
                    context.SetupSubShader(UniversalSubShaders.SpriteLit);
                    break;
                case UniversalMeshTargetData.MaterialType.SpriteUnlit:
                    context.SetupSubShader(UniversalSubShaders.SpriteUnlit);
                    break;
            }
        }

        public List<BlockFieldDescriptor> GetSupportedBlocks(TargetImplementationData data)
        {
            if(!(data is UniversalMeshTargetData universalData))
                return null;

            var supportedBlocks = new List<BlockFieldDescriptor>();

            // Always supported Blocks
            supportedBlocks.Add(BlockFields.VertexDescription.Position);
            supportedBlocks.Add(BlockFields.VertexDescription.Normal);
            supportedBlocks.Add(BlockFields.VertexDescription.Tangent);
            supportedBlocks.Add(BlockFields.SurfaceDescription.BaseColor);

            // Lit Blocks
            if(universalData.materialType == UniversalMeshTargetData.MaterialType.Lit)
            {
                if(universalData.workflowMode == UniversalMeshTargetData.WorkflowMode.Specular)
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
            else if(universalData.materialType == UniversalMeshTargetData.MaterialType.SpriteUnlit)
            {
                supportedBlocks.Add(BlockFields.SurfaceDescription.SpriteMask);
            }
            else if(universalData.materialType == UniversalMeshTargetData.MaterialType.SpriteLit)
            {
                supportedBlocks.Add(BlockFields.SurfaceDescription.SpriteMask);
                supportedBlocks.Add(BlockFields.SurfaceDescription.Normal);
            }

            // Alpha Blocks
            if(universalData.surfaceType == SurfaceType.Transparent || universalData.alphaClip)
            {
                supportedBlocks.Add(BlockFields.SurfaceDescription.Alpha);
            }
            if(universalData.alphaClip)
            {
                supportedBlocks.Add(BlockFields.SurfaceDescription.ClipThreshold);
            }

            return supportedBlocks;
        }

        public ConditionalField[] GetConditionalFields(PassDescriptor pass, List<BlockFieldDescriptor> blocks, TargetImplementationData data)
        {
            if(!(data is UniversalMeshTargetData universalData))
                return null;

            bool isSprite = universalData.materialType == UniversalMeshTargetData.MaterialType.SpriteLit || universalData.materialType == UniversalMeshTargetData.MaterialType.SpriteUnlit;

            return new ConditionalField[]
            {
                // Features
                new ConditionalField(Fields.GraphVertex,         blocks.Contains(BlockFields.VertexDescription.Position) ||
                                                                    blocks.Contains(BlockFields.VertexDescription.Normal) ||
                                                                    blocks.Contains(BlockFields.VertexDescription.Tangent)),
                new ConditionalField(Fields.GraphPixel,          true),
                
                // Surface Type
                new ConditionalField(Fields.SurfaceOpaque,       !isSprite && universalData.surfaceType == SurfaceType.Opaque),
                new ConditionalField(Fields.SurfaceTransparent,  isSprite || universalData.surfaceType != SurfaceType.Opaque),
                
                // Blend Mode
                new ConditionalField(Fields.BlendAdd,            !isSprite && universalData.surfaceType != SurfaceType.Opaque && universalData.alphaMode == AlphaMode.Additive),
                new ConditionalField(Fields.BlendAlpha,          isSprite || universalData.surfaceType != SurfaceType.Opaque && universalData.alphaMode == AlphaMode.Alpha),
                new ConditionalField(Fields.BlendMultiply,       !isSprite && universalData.surfaceType != SurfaceType.Opaque && universalData.alphaMode == AlphaMode.Multiply),
                new ConditionalField(Fields.BlendPremultiply,    !isSprite && universalData.surfaceType != SurfaceType.Opaque && universalData.alphaMode == AlphaMode.Premultiply),

                // Normal Drop Off Space
                new ConditionalField(Fields.NormalDropOffOS,     universalData.materialType == UniversalMeshTargetData.MaterialType.Lit && universalData.normalDropOffSpace == NormalDropOffSpace.Object),
                new ConditionalField(Fields.NormalDropOffTS,     universalData.materialType == UniversalMeshTargetData.MaterialType.Lit && universalData.normalDropOffSpace == NormalDropOffSpace.Tangent),
                new ConditionalField(Fields.NormalDropOffWS,     universalData.materialType == UniversalMeshTargetData.MaterialType.Lit && universalData.normalDropOffSpace == NormalDropOffSpace.World),

                // Misc
                new ConditionalField(Fields.AlphaClip,           !isSprite && universalData.alphaClip),
                new ConditionalField(Fields.VelocityPrecomputed, !isSprite && universalData.addPrecomputedVelocity),
                new ConditionalField(Fields.DoubleSided,         !isSprite && universalData.twoSided),
                new ConditionalField(Fields.SpecularSetup,       universalData.materialType == UniversalMeshTargetData.MaterialType.Lit && universalData.workflowMode == UniversalMeshTargetData.WorkflowMode.Specular),
                new ConditionalField(Fields.Normal,              universalData.materialType == UniversalMeshTargetData.MaterialType.Lit && blocks.Contains(BlockFields.SurfaceDescription.Normal)),
            };
        }
    }
}
