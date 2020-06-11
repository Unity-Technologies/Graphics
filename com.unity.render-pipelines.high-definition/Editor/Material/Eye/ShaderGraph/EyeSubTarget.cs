using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.Rendering.HighDefinition.ShaderGraph.Legacy;
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;
using static UnityEditor.Rendering.HighDefinition.HDShaderUtils;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed partial class EyeSubTarget : LightingSubTarget, ILegacyTarget, IRequiresData<EyeData>
    {
        public EyeSubTarget() => displayName = "Eye";

        protected override string templatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Eye/ShaderGraph/EyePass.template";
        protected override string customInspector => "Rendering.HighDefinition.EyeGUI";
        protected override string subTargetAssetGuid => "864e4e09d6293cf4d98457f740bb3301";
        protected override ShaderID shaderID => HDShaderUtils.ShaderID.SG_Eye;
        protected override string subShaderInclude => "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/Eye.hlsl";
        protected override FieldDescriptor subShaderField => HDFields.SubShader.Eye;

        protected override bool supportRaytracing => false;
        protected override bool requireSplitLighting => eyeData.subsurfaceScattering;

        EyeData m_EyeData;

        EyeData IRequiresData<EyeData>.data
        {
            get => m_EyeData;
            set => m_EyeData = value;
        }

        public EyeData eyeData
        {
            get => m_EyeData;
            set => m_EyeData = value;
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            // Eye specific properties
            context.AddField(HDFields.Eye,                                  eyeData.materialType == EyeData.MaterialType.Eye);
            context.AddField(HDFields.EyeCinematic,                         eyeData.materialType == EyeData.MaterialType.EyeCinematic);
            context.AddField(HDFields.SubsurfaceScattering,                 eyeData.subsurfaceScattering && systemData.surfaceType != SurfaceType.Transparent);
            context.AddField(HDFields.DoAlphaTest,                          systemData.alphaTest && context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold));
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            base.GetActiveBlocks(ref context);

            // Eye specific blocks
            context.AddBlock(HDBlockFields.SurfaceDescription.IrisNormalOS,               eyeData.irisNormal && lightingData.normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddBlock(HDBlockFields.SurfaceDescription.IrisNormalTS,               eyeData.irisNormal && lightingData.normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddBlock(HDBlockFields.SurfaceDescription.IrisNormalWS,               eyeData.irisNormal && lightingData.normalDropOffSpace == NormalDropOffSpace.World);

            context.AddBlock(HDBlockFields.SurfaceDescription.IOR);
            context.AddBlock(HDBlockFields.SurfaceDescription.Mask);
            context.AddBlock(HDBlockFields.SurfaceDescription.DiffusionProfileHash,     eyeData.subsurfaceScattering);
            context.AddBlock(HDBlockFields.SurfaceDescription.SubsurfaceMask,           eyeData.subsurfaceScattering);
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new EyeSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features.Lit, eyeData));
            blockList.AddPropertyBlock(new AdvancedOptionsPropertyBlock());
        }

        protected override int ComputeMaterialNeedsUpdateHash()
            => base.ComputeMaterialNeedsUpdateHash() * 23 + eyeData.subsurfaceScattering.GetHashCode();
    }
}
