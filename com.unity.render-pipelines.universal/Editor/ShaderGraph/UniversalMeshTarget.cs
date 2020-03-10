using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    public enum MaterialType
    {
        Lit,
        Unlit,
        SpriteLit,
        SpriteUnlit,
    }

    public enum WorkflowMode
    {
        Specular,
        Metallic,
    }
    
    class UniversalMeshTarget : ITargetImplementation
    {
        public Type targetType => typeof(MeshTarget);
        public string displayName => "Universal";
        public string passTemplatePath => GenerationUtils.GetDefaultTemplatePath("PassMesh.template");
        public string sharedTemplateDirectory => GenerationUtils.GetDefaultSharedTemplateDirectory();

        public Type dataType => typeof(UniversalMeshTargetData);
        public TargetImplementationData data { get; set; }
        public UniversalMeshTargetData universalData => (UniversalMeshTargetData)data;

        public bool IsPipelineCompatible(RenderPipelineAsset currentPipeline)
        {
            return currentPipeline is UniversalRenderPipelineAsset;
        }

        public void SetupTarget(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("7395c9320da217b42b9059744ceb1de6")); // MeshTarget
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("ac9e1a400a9ce404c8f26b9c1238417e")); // UniversalMeshTarget

            switch(universalData.materialType)
            {
                case MaterialType.Lit:
                    context.SetupSubShader(UniversalSubShaders.PBR);
                    break;
                case MaterialType.Unlit:
                    context.SetupSubShader(UniversalSubShaders.Unlit);
                    break;
                case MaterialType.SpriteLit:
                    context.SetupSubShader(UniversalSubShaders.SpriteLit);
                    break;
                case MaterialType.SpriteUnlit:
                    context.SetupSubShader(UniversalSubShaders.SpriteUnlit);
                    break;
            }
        }

        public List<BlockFieldDescriptor> GetSupportedBlocks()
        {
            var supportedBlocks = new List<BlockFieldDescriptor>();

            // Always supported Blocks
            supportedBlocks.Add(BlockFields.VertexDescription.Position);
            supportedBlocks.Add(BlockFields.VertexDescription.Normal);
            supportedBlocks.Add(BlockFields.VertexDescription.Tangent);
            supportedBlocks.Add(BlockFields.SurfaceDescription.BaseColor);

            // Lit Blocks
            if(universalData.materialType == MaterialType.Lit)
            {
                if(universalData.workflowMode == WorkflowMode.Specular)
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
            else if(universalData.materialType == MaterialType.SpriteUnlit)
            {
                supportedBlocks.Add(BlockFields.SurfaceDescription.SpriteMask);
            }
            else if(universalData.materialType == MaterialType.SpriteLit)
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

        public ConditionalField[] GetConditionalFields(PassDescriptor pass, List<BlockFieldDescriptor> blocks)
        {
            bool isSprite = universalData.materialType == MaterialType.SpriteLit || universalData.materialType == MaterialType.SpriteUnlit;

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
                new ConditionalField(Fields.NormalDropOffOS,     universalData.materialType == MaterialType.Lit && universalData.normalDropOffSpace == NormalDropOffSpace.Object),
                new ConditionalField(Fields.NormalDropOffTS,     universalData.materialType == MaterialType.Lit && universalData.normalDropOffSpace == NormalDropOffSpace.Tangent),
                new ConditionalField(Fields.NormalDropOffWS,     universalData.materialType == MaterialType.Lit && universalData.normalDropOffSpace == NormalDropOffSpace.World),

                // Misc
                new ConditionalField(Fields.AlphaClip,           !isSprite && universalData.alphaClip),
                new ConditionalField(Fields.VelocityPrecomputed, !isSprite && universalData.addPrecomputedVelocity),
                new ConditionalField(Fields.DoubleSided,         !isSprite && universalData.twoSided),
                new ConditionalField(Fields.SpecularSetup,       universalData.materialType == MaterialType.Lit && universalData.workflowMode == WorkflowMode.Specular),
                new ConditionalField(Fields.Normal,              universalData.materialType == MaterialType.Lit && blocks.Contains(BlockFields.SurfaceDescription.Normal)),
            };
        }

        public void GetInspectorContent(PropertySheet propertySheet, Action onChange)
        {
            propertySheet.Add(new PropertyRow(new Label("Material")), (row) =>
                {
                    row.Add(new EnumField(MaterialType.Lit), (field) =>
                    {
                        field.value = universalData.materialType;
                        field.RegisterValueChangedCallback(evt => {
                            if (Equals(universalData.materialType, evt.newValue))
                                return;

                            universalData.materialType = (MaterialType)evt.newValue;
                            onChange();
                        });
                    });
                });

            if(universalData.materialType == MaterialType.Lit)
            {
                propertySheet.Add(new PropertyRow(new Label("Workflow")), (row) =>
                    {
                        row.Add(new EnumField(WorkflowMode.Metallic), (field) =>
                        {
                            field.value = universalData.workflowMode;
                            field.RegisterValueChangedCallback(evt => {
                                if (Equals(universalData.workflowMode, evt.newValue))
                                    return;

                                universalData.workflowMode = (WorkflowMode)evt.newValue;
                                onChange();
                            });
                        });
                    });
            }

            propertySheet.Add(new PropertyRow(new Label("Surface")), (row) =>
                {
                    row.Add(new EnumField(SurfaceType.Opaque), (field) =>
                    {
                        field.value = universalData.surfaceType;
                        field.RegisterValueChangedCallback(evt => {
                            if (Equals(universalData.surfaceType, evt.newValue))
                                return;

                            universalData.surfaceType = (SurfaceType)evt.newValue;
                            onChange();
                        });
                    });
                });

            if(universalData.surfaceType == SurfaceType.Transparent)
            {
                propertySheet.Add(new PropertyRow(new Label("Blend")), (row) =>
                    {
                        row.Add(new EnumField(AlphaMode.Additive), (field) =>
                        {
                            field.value = universalData.alphaMode;
                            field.RegisterValueChangedCallback(evt => {
                                if (Equals(universalData.alphaMode, evt.newValue))
                                    return;

                                universalData.alphaMode = (AlphaMode)evt.newValue;
                                onChange();
                            });
                        });
                    });
            }

            propertySheet.Add(new PropertyRow(new Label("Alpha Clip")), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = universalData.alphaClip;
                        toggle.OnToggleChanged(evt => {
                            if (Equals(universalData.alphaClip, evt.newValue))
                                return;
                            
                            universalData.alphaClip = evt.newValue;
                            onChange();
                        });
                    });
                });

            propertySheet.Add(new PropertyRow(new Label("Two Sided")), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = universalData.twoSided;
                        toggle.OnToggleChanged(evt => {
                            if (Equals(universalData.twoSided, evt.newValue))
                                return;
                            
                            universalData.twoSided = evt.newValue;
                            onChange();
                        });
                    });
                });

            if(universalData.materialType == MaterialType.Lit)
            {
                propertySheet.Add(new PropertyRow(new Label("Fragment Normal Space")), (row) =>
                    {
                        row.Add(new EnumField(NormalDropOffSpace.Tangent), (field) =>
                        {
                            field.value = universalData.normalDropOffSpace;
                            field.RegisterValueChangedCallback(evt => {
                                if (Equals(universalData.normalDropOffSpace, evt.newValue))
                                    return;

                                universalData.normalDropOffSpace = (NormalDropOffSpace)evt.newValue;
                                onChange();
                            });
                        });
                    });
            }
        }
    }
}
