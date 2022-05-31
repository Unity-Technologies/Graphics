using System;
using System.Linq;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Legacy;

using static UnityEngine.Rendering.HighDefinition.HDMaterial;
using static UnityEditor.Rendering.HighDefinition.HDFields;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed partial class EyeSubTarget : LightingSubTarget, ILegacyTarget, IRequiresData<EyeData>
    {
        public EyeSubTarget() => displayName = "Eye";

        static readonly GUID kSubTargetSourceCodeGuid = new GUID("864e4e09d6293cf4d98457f740bb3301");  // EyeSubTarget.cs

        static string[] passTemplateMaterialDirectories = new string[]
        {
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Eye/ShaderGraph/"
        };

        protected override string[] templateMaterialDirectories => passTemplateMaterialDirectories;
        protected override GUID subTargetAssetGuid => kSubTargetSourceCodeGuid;
        protected override ShaderID shaderID => ShaderID.SG_Eye;
        protected override string subShaderInclude => "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Eye/Eye.hlsl";
        protected override FieldDescriptor subShaderField => new FieldDescriptor(kSubShader, "Eye SubShader", "");
        protected override string raytracingInclude => CoreIncludes.kEyeRaytracing;
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

        public static FieldDescriptor Eye = new FieldDescriptor(kMaterial, "Eye", "_MATERIAL_FEATURE_EYE 1");
        public static FieldDescriptor EyeCinematic = new FieldDescriptor(kMaterial, "EyeCinematic", "_MATERIAL_FEATURE_EYE_CINEMATIC 1");
        public static FieldDescriptor EyeCausticLut = new FieldDescriptor(kMaterial, "EyeCinematicWithCaustic", "_MATERIAL_FEATURE_EYE_CAUSTIC_LUT 1");

        protected override SubShaderDescriptor GetRaytracingSubShaderDescriptor()
        {
            var descriptor = base.GetRaytracingSubShaderDescriptor();

            if (eyeData.subsurfaceScattering)
                descriptor.passes.Add(HDShaderPasses.GenerateRaytracingSubsurface());

            return descriptor;
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            // Eye specific properties
            context.AddField(Eye, eyeData.materialType == EyeData.MaterialType.Eye);
            context.AddField(EyeCinematic, eyeData.materialType == EyeData.MaterialType.EyeCinematic);
            context.AddField(EyeCausticLut, eyeData.materialType == EyeData.MaterialType.EyeCinematicWithCaustic);
            context.AddField(SubsurfaceScattering, eyeData.subsurfaceScattering && systemData.surfaceType != SurfaceType.Transparent);

            context.AddField(SpecularAA, lightingData.specularAA &&
                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAThreshold) &&
                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance));
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            base.GetActiveBlocks(ref context);

            // Eye specific blocks
            context.AddBlock(HDBlockFields.SurfaceDescription.IrisNormalOS, eyeData.irisNormal && lightingData.normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddBlock(HDBlockFields.SurfaceDescription.IrisNormalTS, eyeData.irisNormal && lightingData.normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddBlock(HDBlockFields.SurfaceDescription.IrisNormalWS, eyeData.irisNormal && lightingData.normalDropOffSpace == NormalDropOffSpace.World);

            context.AddBlock(HDBlockFields.SurfaceDescription.IOR);
            context.AddBlock(HDBlockFields.SurfaceDescription.Mask);
            context.AddBlock(HDBlockFields.SurfaceDescription.DiffusionProfileHash, eyeData.subsurfaceScattering);
            context.AddBlock(HDBlockFields.SurfaceDescription.SubsurfaceMask, eyeData.subsurfaceScattering);
            context.AddBlock(HDBlockFields.SurfaceDescription.IrisPlaneOffset, eyeData.materialType == EyeData.MaterialType.EyeCinematicWithCaustic);
            context.AddBlock(HDBlockFields.SurfaceDescription.IrisRadius, eyeData.materialType == EyeData.MaterialType.EyeCinematicWithCaustic);
            context.AddBlock(HDBlockFields.SurfaceDescription.CausticIntensity, eyeData.materialType == EyeData.MaterialType.EyeCinematicWithCaustic);
            context.AddBlock(HDBlockFields.SurfaceDescription.CausticBlend, eyeData.materialType == EyeData.MaterialType.EyeCinematicWithCaustic);
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
