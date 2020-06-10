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
    sealed partial class HDLitSubTarget : LightingSubTarget, ILegacyTarget, IRequiresData<HDLitData>
    {
        HDLitData m_LitData;

        HDLitData IRequiresData<HDLitData>.data
        {
            get => m_LitData;
            set => m_LitData = value;
        }

        public HDLitData litData
        {
            get => m_LitData;
            set => m_LitData = value;
        }

        public HDLitSubTarget() => displayName = "Lit";

        protected override string templatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Lit/ShaderGraph/LitPass.template";
        protected override string customInspector => "Rendering.HighDefinition.HDLitGUI";
        protected override string subTargetAssetGuid => "caab952c840878340810cca27417971c"; // HDLitSubTarget.cs
        protected override string postDecalsInclude => "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl";
        protected override ShaderID shaderID => HDShaderUtils.ShaderID.SG_Lit;
        protected override FieldDescriptor subShaderField => HDFields.SubShader.Lit;
        protected override string subShaderInclude => CoreIncludes.kLit;

        // SubShader features
        protected override bool supportDistortion => true;
        protected override bool supportForward => false;
        protected override bool supportPathtracing => true;
        protected override bool requireSplitLighting => litData.materialType == HDLitData.MaterialType.SubsurfaceScattering;

        protected override SubShaderDescriptor GetSubShaderDescriptor()
        {
            var descriptor = base.GetSubShaderDescriptor();

            descriptor.passes.Add(HDShaderPasses.GenerateLitDepthOnly());
            descriptor.passes.Add(HDShaderPasses.GenerateGBuffer());
            descriptor.passes.Add(HDShaderPasses.GenerateLitForward());
            descriptor.passes.Add(HDShaderPasses.GenerateLitRaytracingPrepass());

            return descriptor;
        }

        protected override SubShaderDescriptor GetRaytracingSubShaderDescriptor()
        {
            var descriptor = base.GetRaytracingSubShaderDescriptor();

            if (litData.materialType == HDLitData.MaterialType.SubsurfaceScattering)
                descriptor.passes.Add(HDShaderPasses.GenerateRaytracingSubsurface());

            return descriptor;
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);
            AddDistortionFields(ref context);
            var descs = context.blocks.Select(x => x.descriptor);

            bool hasRefraction = (systemData.surfaceType == SurfaceType.Transparent && systemData.renderingPass != HDRenderQueue.RenderQueueType.PreRefraction && litData.refractionModel != ScreenSpaceRefraction.RefractionModel.None);

            // Lit specific properties
            context.AddField(HDFields.DotsProperties,                       context.hasDotsProperties);

            // Material
            context.AddField(HDFields.Anisotropy,                           litData.materialType == HDLitData.MaterialType.Anisotropy);
            context.AddField(HDFields.Iridescence,                          litData.materialType == HDLitData.MaterialType.Iridescence);
            context.AddField(HDFields.SpecularColor,                        litData.materialType == HDLitData.MaterialType.SpecularColor);
            context.AddField(HDFields.Standard,                             litData.materialType == HDLitData.MaterialType.Standard);
            context.AddField(HDFields.SubsurfaceScattering,                 litData.materialType == HDLitData.MaterialType.SubsurfaceScattering && systemData.surfaceType != SurfaceType.Transparent);
            context.AddField(HDFields.Transmission,                         (litData.materialType == HDLitData.MaterialType.SubsurfaceScattering && litData.sssTransmission) ||
                                                                                (litData.materialType == HDLitData.MaterialType.Translucent));
            context.AddField(HDFields.Translucent,                          litData.materialType == HDLitData.MaterialType.Translucent);

            // Refraction
            context.AddField(HDFields.Refraction,                           hasRefraction);
            context.AddField(HDFields.RefractionBox,                        hasRefraction && litData.refractionModel == ScreenSpaceRefraction.RefractionModel.Box);
            context.AddField(HDFields.RefractionSphere,                     hasRefraction && litData.refractionModel == ScreenSpaceRefraction.RefractionModel.Sphere);
            context.AddField(HDFields.RefractionThin,                       hasRefraction && litData.refractionModel == ScreenSpaceRefraction.RefractionModel.Thin);

            // AlphaTest
            // All the DoAlphaXXX field drive the generation of which code to use for alpha test in the template
            // Do alpha test only if we aren't using the TestShadow one
            context.AddField(HDFields.DoAlphaTest,                          systemData.alphaTest && (context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold) &&
                                                                                !(builtinData.alphaTestShadow && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.AlphaClipThresholdShadow))));

            // Misc

            context.AddField(HDFields.EnergyConservingSpecular,             litData.energyConservingSpecular);
            context.AddField(HDFields.CoatMask,                             descs.Contains(HDBlockFields.SurfaceDescription.CoatMask) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.CoatMask) && litData.clearCoat);
            context.AddField(HDFields.ClearCoat,                            litData.clearCoat); // Enable clear coat material feature
            context.AddField(HDFields.Tangent,                              descs.Contains(HDBlockFields.SurfaceDescription.Tangent) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.Tangent));
            context.AddField(HDFields.RayTracing,                           litData.rayTracing);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            bool hasRefraction = (systemData.surfaceType == SurfaceType.Transparent && systemData.renderingPass != HDRenderQueue.RenderQueueType.PreRefraction && litData.refractionModel != ScreenSpaceRefraction.RefractionModel.None);
            bool hasDistortion = (systemData.surfaceType == SurfaceType.Transparent && builtinData.distortion);

            // Vertex
            base.GetActiveBlocks(ref context);

            // Common
            context.AddBlock(HDBlockFields.SurfaceDescription.CoatMask,             litData.clearCoat);

            // Refraction
            context.AddBlock(HDBlockFields.SurfaceDescription.RefractionIndex,      hasRefraction);
            context.AddBlock(HDBlockFields.SurfaceDescription.RefractionColor,      hasRefraction);
            context.AddBlock(HDBlockFields.SurfaceDescription.RefractionDistance,   hasRefraction);

            // Material
            context.AddBlock(HDBlockFields.SurfaceDescription.Tangent,              litData.materialType == HDLitData.MaterialType.Anisotropy);
            context.AddBlock(HDBlockFields.SurfaceDescription.Anisotropy,           litData.materialType == HDLitData.MaterialType.Anisotropy);
            context.AddBlock(HDBlockFields.SurfaceDescription.SubsurfaceMask,       litData.materialType == HDLitData.MaterialType.SubsurfaceScattering);
            context.AddBlock(HDBlockFields.SurfaceDescription.Thickness,            ((litData.materialType == HDLitData.MaterialType.SubsurfaceScattering || litData.materialType == HDLitData.MaterialType.Translucent) &&
                                                                                        (litData.sssTransmission || litData.materialType == HDLitData.MaterialType.Translucent)) || hasRefraction);
            context.AddBlock(HDBlockFields.SurfaceDescription.DiffusionProfileHash, litData.materialType == HDLitData.MaterialType.SubsurfaceScattering || litData.materialType == HDLitData.MaterialType.Translucent);
            context.AddBlock(HDBlockFields.SurfaceDescription.IridescenceMask,      litData.materialType == HDLitData.MaterialType.Iridescence);
            context.AddBlock(HDBlockFields.SurfaceDescription.IridescenceThickness, litData.materialType == HDLitData.MaterialType.Iridescence);
            context.AddBlock(BlockFields.SurfaceDescription.Specular,               litData.materialType == HDLitData.MaterialType.SpecularColor);
            context.AddBlock(BlockFields.SurfaceDescription.Metallic,               litData.materialType == HDLitData.MaterialType.Standard || 
                                                                                        litData.materialType == HDLitData.MaterialType.Anisotropy ||
                                                                                        litData.materialType == HDLitData.MaterialType.Iridescence);
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);

            HDSubShaderUtilities.AddRayTracingProperty(collector, litData.rayTracing);
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new LitSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features.Lit, litData));
            if (systemData.surfaceType == SurfaceType.Transparent)
                blockList.AddPropertyBlock(new DistortionPropertyBlock());
            blockList.AddPropertyBlock(new AdvancedOptionsPropertyBlock());
        }

        protected override int ComputeMaterialNeedsUpdateHash()
        {
            int hash = base.ComputeMaterialNeedsUpdateHash();

            unchecked
            {
                bool subsurfaceScattering = litData.materialType == HDLitData.MaterialType.SubsurfaceScattering;
                hash = hash * 23 + subsurfaceScattering.GetHashCode();
            }

            return hash;
        }
    }
}
