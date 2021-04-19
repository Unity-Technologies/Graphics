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
using static UnityEditor.Rendering.HighDefinition.HDFields;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed partial class FabricSubTarget : LightingSubTarget, ILegacyTarget, IRequiresData<FabricData>
    {
        public FabricSubTarget() => displayName = "Fabric";

        static readonly GUID kSubTargetSourceCodeGuid = new GUID("74f1a4749bab90d429ac01d094be0aeb");  // FabricSubTarget.cs

        static string[] passTemplateMaterialDirectories = new string[]
        {
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Fabric/ShaderGraph/",
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/ShaderGraph/Templates/"
        };

        protected override string[] templateMaterialDirectories => passTemplateMaterialDirectories;
        protected override GUID subTargetAssetGuid => kSubTargetSourceCodeGuid;
        protected override string customInspector => "Rendering.HighDefinition.LightingShaderGraphGUI";
        protected override ShaderID shaderID => HDShaderUtils.ShaderID.SG_Fabric;
        protected override string subShaderInclude => "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl";
        protected override string raytracingInclude => CoreIncludes.kFabricRaytracing;
        protected override FieldDescriptor subShaderField => new FieldDescriptor(kSubShader, "Fabric SubShader", "");
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

        public static FieldDescriptor CottonWool =              new FieldDescriptor(kMaterial, "CottonWool", "_MATERIAL_FEATURE_COTTON_WOOL 1");
        public static FieldDescriptor Silk =                    new FieldDescriptor(kMaterial, "Silk", "_MATERIAL_FEATURE_SILK 1");

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
            context.AddField(CottonWool,                           fabricData.materialType == FabricData.MaterialType.CottonWool);
            context.AddField(Silk,                                 fabricData.materialType == FabricData.MaterialType.Silk);
            context.AddField(SubsurfaceScattering,                 fabricData.subsurfaceScattering && systemData.surfaceType != SurfaceType.Transparent);
            context.AddField(Transmission,                         fabricData.transmission);
            context.AddField(EnergyConservingSpecular,             fabricData.energyConservingSpecular);

            context.AddField(SpecularAA, lightingData.specularAA &&
                                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAThreshold) &&
                                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance));
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
                BlockFieldDescriptor tangentBlock;
                switch (lightingData.normalDropOffSpace)
                {
                    case NormalDropOffSpace.Object:
                        tangentBlock = HDBlockFields.SurfaceDescription.TangentOS;
                        break;
                    case NormalDropOffSpace.World:
                        tangentBlock = HDBlockFields.SurfaceDescription.TangentWS;
                        break;
                    default:
                        tangentBlock = HDBlockFields.SurfaceDescription.TangentTS;
                        break;
                }

                context.AddBlock(tangentBlock);
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
