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
    sealed partial class HairSubTarget : LightingSubTarget, ILegacyTarget, IRequiresData<HairData>
    {
        public HairSubTarget() => displayName = "Hair";

        static readonly GUID kSubTargetSourceCodeGuid = new GUID("7e681cc79dd8e6c46ba1e8412d519e26");  // HairSubTarget.cs

        static string[] passTemplateMaterialDirectories = new string[]
        {
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Hair/ShaderGraph/",
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/ShaderGraph/Templates/"
        };

        protected override string[] templateMaterialDirectories => passTemplateMaterialDirectories;
        protected override GUID subTargetAssetGuid => kSubTargetSourceCodeGuid;
        protected override ShaderID shaderID => HDShaderUtils.ShaderID.SG_Hair;
        protected override string subShaderInclude => CoreIncludes.kHair;
        protected override string raytracingInclude => CoreIncludes.kHairRaytracing;
        protected override FieldDescriptor subShaderField => new FieldDescriptor(kSubShader, "Hair SubShader", "");
        protected override bool requireSplitLighting => false;
        protected override bool supportPathtracing => true;
        protected override string pathtracingInclude => CoreIncludes.kHairPathtracing;

        HairData m_HairData;

        HairData IRequiresData<HairData>.data
        {
            get => m_HairData;
            set => m_HairData = value;
        }

        public HairData hairData
        {
            get => m_HairData;
            set => m_HairData = value;
        }

        public static FieldDescriptor KajiyaKay =                       new FieldDescriptor(kMaterial, "KajiyaKay", "_MATERIAL_FEATURE_HAIR_KAJIYA_KAY 1");
        public static FieldDescriptor Marschner =                       new FieldDescriptor(kMaterial, "Marschner", "_MATERIAL_FEATURE_HAIR_MARSCHNER 1");
        public static FieldDescriptor RimTransmissionIntensity =        new FieldDescriptor(string.Empty, "RimTransmissionIntensity", "_RIM_TRANSMISSION_INTENSITY 1");
        public static FieldDescriptor HairStrandDirection =             new FieldDescriptor(string.Empty, "HairStrandDirection", "_HAIR_STRAND_DIRECTION 1");
        public static FieldDescriptor UseLightFacingNormal =            new FieldDescriptor(string.Empty, "UseLightFacingNormal", "_USE_LIGHT_FACING_NORMAL 1");
        public static FieldDescriptor Transmittance =                   new FieldDescriptor(string.Empty, "Transmittance", "_TRANSMITTANCE 1");
        public static FieldDescriptor UseRoughenedAzimuthalScattering = new FieldDescriptor(string.Empty, "UseRoughenedAzimuthalScattering", "_USE_ROUGHENED_AZIMUTHAL_SCATTERING 1");
        public static FieldDescriptor ScatteringDensityVolume  =        new FieldDescriptor(string.Empty, "ScatteringDensityVolume", "_USE_DENSITY_VOLUME_SCATTERING 1");

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            var descs = context.blocks.Select(x => x.descriptor);

            // Hair specific properties:
            context.AddField(KajiyaKay,                            hairData.materialType == HairData.MaterialType.KajiyaKay);
            context.AddField(Marschner,                            hairData.materialType == HairData.MaterialType.Marschner);
            context.AddField(HairStrandDirection,                  descs.Contains(HDBlockFields.SurfaceDescription.HairStrandDirection) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.HairStrandDirection));
            context.AddField(RimTransmissionIntensity,             descs.Contains(HDBlockFields.SurfaceDescription.RimTransmissionIntensity) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.RimTransmissionIntensity));
            context.AddField(UseLightFacingNormal,                 hairData.geometryType == HairData.GeometryType.Strands);
            context.AddField(Transmittance,                        descs.Contains(HDBlockFields.SurfaceDescription.Transmittance) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.Transmittance));
            context.AddField(UseRoughenedAzimuthalScattering,      hairData.useRoughenedAzimuthalScattering);
            context.AddField(ScatteringDensityVolume,              hairData.scatteringMode == HairData.ScatteringMode.DensityVolume);

            // Misc
            context.AddField(SpecularAA,                           lightingData.specularAA &&
                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAThreshold) &&
                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance));
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            base.GetActiveBlocks(ref context);

            // Hair specific blocks
            context.AddBlock(HDBlockFields.SurfaceDescription.HairStrandDirection);

            // Parametrization for Kajiya-Kay and Marschner models.
            if (hairData.materialType == HairData.MaterialType.KajiyaKay)
            {
                context.AddBlock(HDBlockFields.SurfaceDescription.Transmittance);
                context.AddBlock(HDBlockFields.SurfaceDescription.RimTransmissionIntensity);
                context.AddBlock(HDBlockFields.SurfaceDescription.SpecularTint);
                context.AddBlock(HDBlockFields.SurfaceDescription.SpecularShift);
                context.AddBlock(HDBlockFields.SurfaceDescription.SecondarySpecularTint);
                context.AddBlock(HDBlockFields.SurfaceDescription.SecondarySmoothness);
                context.AddBlock(HDBlockFields.SurfaceDescription.SecondarySpecularShift);
            }
            else
            {
                context.AddBlock(HDBlockFields.SurfaceDescription.RadialSmoothness, hairData.useRoughenedAzimuthalScattering);
                context.AddBlock(HDBlockFields.SurfaceDescription.CuticleAngle);

                // TODO: Refraction Index
                // Right now, the Marschner model implicitly assumes a human hair IOR of 1.55.
            }
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new HairSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features.Lit, hairData));
            blockList.AddPropertyBlock(new HairAdvancedOptionsPropertyBlock(hairData));
        }
    }
}
