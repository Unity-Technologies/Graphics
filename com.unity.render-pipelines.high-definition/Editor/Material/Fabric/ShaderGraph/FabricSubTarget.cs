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
    sealed partial class FabricSubTarget : LightingSubTarget, ILegacyTarget, IRequiresData<FabricData>
    {
        public FabricSubTarget() => displayName = "Fabric";

        protected override string templateMaterialDirectory => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Fabric/ShaderGraph/";
        protected override string subTargetAssetGuid => "74f1a4749bab90d429ac01d094be0aeb"; // FabricSubTarget.cs
        protected override string customInspector => "Rendering.HighDefinition.FabricGUI";
        protected override ShaderID shaderID => HDShaderUtils.ShaderID.SG_Fabric;
        protected override string subShaderInclude => "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl";
        protected override FieldDescriptor subShaderField => HDFields.SubShader.Fabric;
        protected override bool requireSplitLighting => fabricData.subsurfaceScattering;

        FabricData m_FabricData;

        FabricData IRequiresData<FabricData>.data
        {
            get => m_FabricData;
            set => m_FabricData = value;
        }

        public FabricData fabricData
        {
            get => m_FabricData;
            set => m_FabricData = value;
        }

        protected override SubShaderDescriptor GetRaytracingSubShaderDescriptor()
        {
            var descriptor = base.GetRaytracingSubShaderDescriptor();

            if (fabricData.subsurfaceScattering)
                descriptor.passes.Add(HDShaderPasses.GenerateRaytracingSubsurface());
 
            return descriptor;
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            // Fabric specific properties
            context.AddField(HDFields.CottonWool,                           fabricData.materialType == FabricData.MaterialType.CottonWool);
            context.AddField(HDFields.Silk,                                 fabricData.materialType == FabricData.MaterialType.Silk);
            context.AddField(HDFields.SubsurfaceScattering,                 fabricData.subsurfaceScattering && systemData.surfaceType != SurfaceType.Transparent);
            context.AddField(HDFields.Transmission,                         fabricData.transmission);
            context.AddField(HDFields.DoAlphaTest,                          systemData.alphaTest && context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold));
            context.AddField(HDFields.EnergyConservingSpecular,             fabricData.energyConservingSpecular);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            base.GetActiveBlocks(ref context);

            // Fabric specific blocks
            context.AddBlock(BlockFields.SurfaceDescription.Specular);
            context.AddBlock(HDBlockFields.SurfaceDescription.DiffusionProfileHash, fabricData.subsurfaceScattering || fabricData.transmission);
            context.AddBlock(HDBlockFields.SurfaceDescription.SubsurfaceMask,       fabricData.subsurfaceScattering);
            context.AddBlock(HDBlockFields.SurfaceDescription.Thickness,            fabricData.transmission);

            // Fabric Silk
            if(fabricData.materialType == FabricData.MaterialType.Silk)
            {
                context.AddBlock(HDBlockFields.SurfaceDescription.Tangent);
                context.AddBlock(HDBlockFields.SurfaceDescription.Anisotropy);
            }
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new FabricSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features.Lit, fabricData));
            blockList.AddPropertyBlock(new AdvancedOptionsPropertyBlock());
        }

        protected override int ComputeMaterialNeedsUpdateHash()
            => base.ComputeMaterialNeedsUpdateHash() * 23 + fabricData.subsurfaceScattering.GetHashCode();
    }
}
