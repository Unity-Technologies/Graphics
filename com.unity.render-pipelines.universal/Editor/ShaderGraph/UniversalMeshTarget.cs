using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph;
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
        [SerializeField]
        MaterialType m_MaterialType = MaterialType.Lit;

        [SerializeField]
        WorkflowMode m_WorkflowMode = WorkflowMode.Metallic;

        [SerializeField]
        SurfaceType m_SurfaceType = SurfaceType.Opaque;

        [SerializeField]
        AlphaMode m_AlphaMode = AlphaMode.Alpha;

        [SerializeField]
        bool m_TwoSided = false;

        [SerializeField]
        bool m_AlphaClip = false;

        [SerializeField]
        bool m_AddPrecomputedVelocity = false;

        [SerializeField]
        NormalDropOffSpace m_NormalDropOffSpace = NormalDropOffSpace.Tangent;

        public Type targetType => typeof(MeshTarget);
        public string displayName => "Universal";
        public string passTemplatePath => GenerationUtils.GetDefaultTemplatePath("PassMesh.template");
        public string sharedTemplateDirectory => GenerationUtils.GetDefaultSharedTemplateDirectory();

        public void SetupTarget(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("7395c9320da217b42b9059744ceb1de6")); // MeshTarget
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("ac9e1a400a9ce404c8f26b9c1238417e")); // UniversalMeshTarget

            switch(m_MaterialType)
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

        public void SetActiveBlocks(ref List<BlockFieldDescriptor> activeBlocks)
        {
            bool isSprite = m_MaterialType == MaterialType.SpriteLit || m_MaterialType == MaterialType.SpriteUnlit;

            // Always supported Blocks
            activeBlocks.Add(BlockFields.VertexDescription.Position);
            activeBlocks.Add(BlockFields.VertexDescription.Normal);
            activeBlocks.Add(BlockFields.VertexDescription.Tangent);
            activeBlocks.Add(BlockFields.SurfaceDescription.BaseColor);

            switch(m_MaterialType)
            {
                case MaterialType.Lit:
                    if(m_WorkflowMode == WorkflowMode.Specular)
                        activeBlocks.Add(BlockFields.SurfaceDescription.Specular);
                    else
                        activeBlocks.Add(BlockFields.SurfaceDescription.Metallic);

                    activeBlocks.Add(BlockFields.SurfaceDescription.Smoothness);
                    activeBlocks.Add(BlockFields.SurfaceDescription.Normal);
                    activeBlocks.Add(BlockFields.SurfaceDescription.Emission);
                    activeBlocks.Add(BlockFields.SurfaceDescription.Occlusion);
                    break;
                // TODO: Move Sprite to separate Target?
                case MaterialType.SpriteLit:
                    activeBlocks.Add(BlockFields.SurfaceDescription.SpriteMask);
                    activeBlocks.Add(BlockFields.SurfaceDescription.Normal);
                    break;
                case MaterialType.SpriteUnlit:
                    activeBlocks.Add(BlockFields.SurfaceDescription.SpriteMask);
                    break;
            }

            // Alpha Blocks
            if(isSprite || m_SurfaceType == SurfaceType.Transparent || m_AlphaClip)
            {
                activeBlocks.Add(BlockFields.SurfaceDescription.Alpha);
            }
            if(m_AlphaClip)
            {
                activeBlocks.Add(BlockFields.SurfaceDescription.AlphaClipThreshold);
            }
        }

        public ConditionalField[] GetConditionalFields(PassDescriptor pass, List<BlockFieldDescriptor> blocks)
        {
            bool isSprite = m_MaterialType == MaterialType.SpriteLit || m_MaterialType == MaterialType.SpriteUnlit;

            return new ConditionalField[]
            {
                // Features
                new ConditionalField(Fields.GraphVertex,         blocks.Contains(BlockFields.VertexDescription.Position) ||
                                                                    blocks.Contains(BlockFields.VertexDescription.Normal) ||
                                                                    blocks.Contains(BlockFields.VertexDescription.Tangent)),
                new ConditionalField(Fields.GraphPixel,          true),
                
                // Surface Type
                new ConditionalField(Fields.SurfaceOpaque,       !isSprite && m_SurfaceType == SurfaceType.Opaque),
                new ConditionalField(Fields.SurfaceTransparent,  isSprite || m_SurfaceType != SurfaceType.Opaque),
                
                // Blend Mode
                new ConditionalField(Fields.BlendAdd,            !isSprite && m_SurfaceType != SurfaceType.Opaque && m_AlphaMode == AlphaMode.Additive),
                new ConditionalField(Fields.BlendAlpha,          isSprite || m_SurfaceType != SurfaceType.Opaque && m_AlphaMode == AlphaMode.Alpha),
                new ConditionalField(Fields.BlendMultiply,       !isSprite && m_SurfaceType != SurfaceType.Opaque && m_AlphaMode == AlphaMode.Multiply),
                new ConditionalField(Fields.BlendPremultiply,    !isSprite && m_SurfaceType != SurfaceType.Opaque && m_AlphaMode == AlphaMode.Premultiply),

                // Normal Drop Off Space
                new ConditionalField(Fields.NormalDropOffOS,     m_MaterialType == MaterialType.Lit && m_NormalDropOffSpace == NormalDropOffSpace.Object),
                new ConditionalField(Fields.NormalDropOffTS,     m_MaterialType == MaterialType.Lit && m_NormalDropOffSpace == NormalDropOffSpace.Tangent),
                new ConditionalField(Fields.NormalDropOffWS,     m_MaterialType == MaterialType.Lit && m_NormalDropOffSpace == NormalDropOffSpace.World),

                // Misc
                new ConditionalField(Fields.AlphaClip,           !isSprite && m_AlphaClip),
                new ConditionalField(Fields.VelocityPrecomputed, !isSprite && m_AddPrecomputedVelocity),
                new ConditionalField(Fields.DoubleSided,         !isSprite && m_TwoSided),
                new ConditionalField(Fields.SpecularSetup,       m_MaterialType == MaterialType.Lit && m_WorkflowMode == WorkflowMode.Specular),
                new ConditionalField(Fields.Normal,              m_MaterialType == MaterialType.Lit && blocks.Contains(BlockFields.SurfaceDescription.Normal)),
            };
        }

        public VisualElement GetSettings(Action onChange)
        {
            var element = new VisualElement() { name = "universalMeshSettings" };
            element.Add(new PropertyRow(new Label("Material")), (row) =>
                {
                    row.Add(new EnumField(MaterialType.Lit), (field) =>
                    {
                        field.value = m_MaterialType;
                        field.RegisterValueChangedCallback(evt => {
                            if (Equals(m_MaterialType, evt.newValue))
                                return;

                            m_MaterialType = (MaterialType)evt.newValue;
                            onChange();
                        });
                    });
                });

            if(m_MaterialType == MaterialType.Lit)
            {
                element.Add(new PropertyRow(new Label("Workflow")), (row) =>
                    {
                        row.Add(new EnumField(WorkflowMode.Metallic), (field) =>
                        {
                            field.value = m_WorkflowMode;
                            field.RegisterValueChangedCallback(evt => {
                                if (Equals(m_WorkflowMode, evt.newValue))
                                    return;

                                m_WorkflowMode = (WorkflowMode)evt.newValue;
                                onChange();
                            });
                        });
                    });
            }

            element.Add(new PropertyRow(new Label("Surface")), (row) =>
                {
                    row.Add(new EnumField(SurfaceType.Opaque), (field) =>
                    {
                        field.value = m_SurfaceType;
                        field.RegisterValueChangedCallback(evt => {
                            if (Equals(m_SurfaceType, evt.newValue))
                                return;

                            m_SurfaceType = (SurfaceType)evt.newValue;
                            onChange();
                        });
                    });
                });

            if(m_SurfaceType == SurfaceType.Transparent)
            {
                element.Add(new PropertyRow(new Label("Blend")), (row) =>
                    {
                        row.Add(new EnumField(AlphaMode.Additive), (field) =>
                        {
                            field.value = m_AlphaMode;
                            field.RegisterValueChangedCallback(evt => {
                                if (Equals(m_AlphaMode, evt.newValue))
                                    return;

                                m_AlphaMode = (AlphaMode)evt.newValue;
                                onChange();
                            });
                        });
                    });
            }

            element.Add(new PropertyRow(new Label("Alpha Clip")), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_AlphaClip;
                        toggle.OnToggleChanged(evt => {
                            if (Equals(m_AlphaClip, evt.newValue))
                                return;
                            
                            m_AlphaClip = evt.newValue;
                            onChange();
                        });
                    });
                });

            element.Add(new PropertyRow(new Label("Two Sided")), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_TwoSided;
                        toggle.OnToggleChanged(evt => {
                            if (Equals(m_TwoSided, evt.newValue))
                                return;
                            
                            m_TwoSided = evt.newValue;
                            onChange();
                        });
                    });
                });

            if(m_MaterialType == MaterialType.Lit)
            {
                element.Add(new PropertyRow(new Label("Fragment Normal Space")), (row) =>
                    {
                        row.Add(new EnumField(NormalDropOffSpace.Tangent), (field) =>
                        {
                            field.value = m_NormalDropOffSpace;
                            field.RegisterValueChangedCallback(evt => {
                                if (Equals(m_NormalDropOffSpace, evt.newValue))
                                    return;

                                m_NormalDropOffSpace = (NormalDropOffSpace)evt.newValue;
                                onChange();
                            });
                        });
                    });
            }

            return element;
        }
    }
}
