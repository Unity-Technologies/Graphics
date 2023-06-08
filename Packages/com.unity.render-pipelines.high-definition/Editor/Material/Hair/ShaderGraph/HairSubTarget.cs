using System;
using System.Linq;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;

using static UnityEngine.Rendering.HighDefinition.HDMaterial;
using static UnityEditor.Rendering.HighDefinition.HDFields;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed partial class HairSubTarget : LightingSubTarget, ILegacyTarget, IRequiresData<HairData>
    {
        public HairSubTarget() => displayName = "Hair";

        static readonly GUID kSubTargetSourceCodeGuid = new GUID("7e681cc79dd8e6c46ba1e8412d519e26");  // HairSubTarget.cs

        static string[] passTemplateMaterialDirectories = new string[]
        {
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Hair/ShaderGraph/"
        };

        protected override string[] templateMaterialDirectories => passTemplateMaterialDirectories;
        protected override GUID subTargetAssetGuid => kSubTargetSourceCodeGuid;
        protected override ShaderID shaderID => ShaderID.SG_Hair;
        protected override string subShaderInclude => CoreIncludes.kHair;
        protected override string raytracingInclude => CoreIncludes.kHairRaytracing;
        protected override FieldDescriptor subShaderField => new FieldDescriptor(kSubShader, "Hair SubShader", "");
        protected override bool requireSplitLighting => false;
        protected override bool supportPathtracing => !TargetsVFX();
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

        public static FieldDescriptor KajiyaKay = new FieldDescriptor(kMaterial, "KajiyaKay", "_MATERIAL_FEATURE_HAIR_KAJIYA_KAY 1");
        public static FieldDescriptor Marschner = new FieldDescriptor(kMaterial, "Marschner", "_MATERIAL_FEATURE_HAIR_MARSCHNER 1");
        public static FieldDescriptor MarschnerCinematic = new FieldDescriptor(kMaterial, "MarschnerCinematic", "_MATERIAL_FEATURE_HAIR_MARSCHNER_CINEMATIC 1");
        public static FieldDescriptor RimTransmissionIntensity = new FieldDescriptor(string.Empty, "RimTransmissionIntensity", "_RIM_TRANSMISSION_INTENSITY 1");
        public static FieldDescriptor HairStrandDirection = new FieldDescriptor(string.Empty, "HairStrandDirection", "_HAIR_STRAND_DIRECTION 1");
        public static FieldDescriptor UseLightFacingNormal = new FieldDescriptor(string.Empty, "UseLightFacingNormal", "_USE_LIGHT_FACING_NORMAL 1");
        public static FieldDescriptor Transmittance = new FieldDescriptor(string.Empty, "Transmittance", "_TRANSMITTANCE 1");
        public static FieldDescriptor AbsorptionFromColor = new FieldDescriptor(string.Empty, "AbsorptionFromColor", "_ABSORPTION_FROM_COLOR 1");
        public static FieldDescriptor AbsorptionFromMelanin = new FieldDescriptor(string.Empty, "AbsorptionFromMelanin", "_ABSORPTION_FROM_MELANIN 1");
        public static FieldDescriptor UseSplineVisibilityForScattering = new FieldDescriptor(string.Empty, "UseSplineVisibilityForScattering", "_USE_SPLINE_VISIBILITY_FOR_MULTIPLE_SCATTERING 1");

        // Cinematic sample counts
        public static FieldDescriptor EnvironmentLightSamplesLow    = new FieldDescriptor(string.Empty, "EnvironmentLightSamplesLow", "#define ENVIRONMENT_LIGHT_SAMPLE_COUNT 4");
        public static FieldDescriptor EnvironmentLightSamplesMedium = new FieldDescriptor(string.Empty, "EnvironmentLightSamplesMedium", "#define ENVIRONMENT_LIGHT_SAMPLE_COUNT 8");
        public static FieldDescriptor EnvironmentLightSamplesHigh   = new FieldDescriptor(string.Empty, "EnvironmentLightSamplesHigh", "#define ENVIRONMENT_LIGHT_SAMPLE_COUNT 12");
        public static FieldDescriptor EnvironmentLightSamplesUltra  = new FieldDescriptor(string.Empty, "EnvironmentLightSamplesUltra", "#define ENVIRONMENT_LIGHT_SAMPLE_COUNT 24");

        public static FieldDescriptor AreaLightSamplesLow    = new FieldDescriptor(string.Empty, "AreaLightSamplesLow", "#define _AREA_LIGHT_SAMPLE_COUNT 4");
        public static FieldDescriptor AreaLightSamplesMedium = new FieldDescriptor(string.Empty, "AreaLightSamplesMedium", "#define _AREA_LIGHT_SAMPLE_COUNT 8");
        public static FieldDescriptor AreaLightSamplesHigh   = new FieldDescriptor(string.Empty, "AreaLightSamplesHigh", "#define _AREA_LIGHT_SAMPLE_COUNT 12");
        public static FieldDescriptor AreaLightSamplesUltra  = new FieldDescriptor(string.Empty, "AreaLightSamplesUltra", "#define _AREA_LIGHT_SAMPLE_COUNT 24");

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            var descs = context.blocks.Select(x => x.descriptor);

            // Hair specific properties:
            context.AddField(KajiyaKay, hairData.materialType == HairData.MaterialType.Approximate);
            context.AddField(Marschner, hairData.materialType == HairData.MaterialType.Physical);
            context.AddField(MarschnerCinematic, hairData.materialType == HairData.MaterialType.PhysicalCinematic);
            context.AddField(HairStrandDirection, descs.Contains(HDBlockFields.SurfaceDescription.HairStrandDirection) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.HairStrandDirection));
            context.AddField(RimTransmissionIntensity, descs.Contains(HDBlockFields.SurfaceDescription.RimTransmissionIntensity) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.RimTransmissionIntensity));
            context.AddField(UseLightFacingNormal, hairData.geometryType == HairData.GeometryType.Strands || hairData.materialType != HairData.MaterialType.Approximate);
            context.AddField(Transmittance, descs.Contains(HDBlockFields.SurfaceDescription.Transmittance) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.Transmittance));
            context.AddField(AbsorptionFromColor, hairData.colorParameterization == HairData.ColorParameterization.BaseColor);
            context.AddField(AbsorptionFromMelanin, hairData.colorParameterization == HairData.ColorParameterization.Melanin);
            context.AddField(UseSplineVisibilityForScattering, hairData.materialType == HairData.MaterialType.PhysicalCinematic && hairData.directionalFractionMode == HairData.DirectionalFractionMode.ShadowMap);

            switch (hairData.environmentSamples)
            {
                case HairData.CinematicSampleCount.Low:
                    context.AddField(EnvironmentLightSamplesLow);
                    break;
                case HairData.CinematicSampleCount.Medium:
                    context.AddField(EnvironmentLightSamplesMedium);
                    break;
                case HairData.CinematicSampleCount.High:
                    context.AddField(EnvironmentLightSamplesHigh);
                    break;
                case HairData.CinematicSampleCount.Ultra:
                    context.AddField(EnvironmentLightSamplesUltra);
                    break;
            }

            switch (hairData.areaLightSamples)
            {
                case HairData.CinematicSampleCount.Low:
                    context.AddField(AreaLightSamplesLow);
                    break;
                case HairData.CinematicSampleCount.Medium:
                    context.AddField(AreaLightSamplesMedium);
                    break;
                case HairData.CinematicSampleCount.High:
                    context.AddField(AreaLightSamplesHigh);
                    break;
                case HairData.CinematicSampleCount.Ultra:
                    context.AddField(AreaLightSamplesUltra);
                    break;
            }

            // Misc
            context.AddField(SpecularAA, lightingData.specularAA &&
                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAThreshold) &&
                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance));
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            base.GetActiveBlocks(ref context);

            // Hair specific blocks
            context.AddBlock(HDBlockFields.SurfaceDescription.HairStrandDirection);

            // Parametrization for Kajiya-Kay and Marschner models.
            if (hairData.materialType == HairData.MaterialType.Approximate)
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
                // Color parameterization for cortex (default is base color)
                context.AddBlock(HDBlockFields.SurfaceDescription.AbsorptionCoefficient, hairData.colorParameterization == HairData.ColorParameterization.Absorption);
                context.AddBlock(HDBlockFields.SurfaceDescription.Eumelanin, hairData.colorParameterization == HairData.ColorParameterization.Melanin);
                context.AddBlock(HDBlockFields.SurfaceDescription.Pheomelanin, hairData.colorParameterization == HairData.ColorParameterization.Melanin);

                // Need to explicitly remove the base color here as it is by default always included.
                if (hairData.colorParameterization != HairData.ColorParameterization.BaseColor)
                    context.activeBlocks.Remove(BlockFields.SurfaceDescription.BaseColor);

                context.AddBlock(HDBlockFields.SurfaceDescription.RadialSmoothness);
                context.AddBlock(HDBlockFields.SurfaceDescription.CuticleAngle);

                // TODO: Refraction Index
                // Right now, the Marschner model implicitly assumes a human hair IOR of 1.55.

                context.AddBlock(HDBlockFields.SurfaceDescription.StrandCountProbe, hairData.materialType == HairData.MaterialType.PhysicalCinematic);
            }
        }

        protected override void CollectPassKeywords(ref PassDescriptor pass)
        {
            base.CollectPassKeywords(ref pass);

            if (pass.lightMode == (HDShaderPassNames.s_LineRenderingOffscreenShading))
            {
                pass.keywords.Add(CoreKeywordDescriptors.Native16Bit);
            }
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new HairSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features.Lit, hairData));

            if (hairData.materialType == HairData.MaterialType.PhysicalCinematic)
            {
#if !HAS_UNITY_HAIR_PACKAGE
                // Skip the advanced block, there will be a help box in the main one to tell the user that the hair package
                // should be installed to use cinematic hair shading.
                return;
#endif
            }

            blockList.AddPropertyBlock(new HairAdvancedOptionsPropertyBlock(hairData));
        }
    }
}
