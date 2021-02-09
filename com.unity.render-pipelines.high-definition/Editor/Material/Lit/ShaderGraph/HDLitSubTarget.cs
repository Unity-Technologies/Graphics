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

        static readonly GUID kSubTargetSourceCodeGuid = new GUID("caab952c840878340810cca27417971c");  // HDLitSubTarget.cs

        static string[] passTemplateMaterialDirectories = new string[]
        {
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Lit/ShaderGraph/",
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/ShaderGraph/Templates/"
        };

        protected override string[] templateMaterialDirectories => passTemplateMaterialDirectories;
        protected override GUID subTargetAssetGuid => kSubTargetSourceCodeGuid;
        protected override string postDecalsInclude => "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl";
        protected override ShaderID shaderID => HDShaderUtils.ShaderID.SG_Lit;
        protected override string raytracingInclude => CoreIncludes.kLitRaytracing;
        protected override string pathtracingInclude => CoreIncludes.kLitPathtracing;
        protected override FieldDescriptor subShaderField => new FieldDescriptor(kSubShader, "Lit Subshader", "");
        protected override string subShaderInclude => CoreIncludes.kLit;
        protected override string customInspector => "Rendering.HighDefinition.LitShaderGraphGUI";

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
                descriptor.passes.Add(HDShaderPasses.GenerateRaytracingSubsurface(true));

            return descriptor;
        }

        public static FieldDescriptor ClearCoat =               new FieldDescriptor(kMaterial, "ClearCoat", "_MATERIAL_FEATURE_CLEAR_COAT");
        public static FieldDescriptor Translucent =             new FieldDescriptor(kMaterial, "Translucent", "_MATERIAL_FEATURE_TRANSLUCENT 1");
        public static FieldDescriptor Standard =                new FieldDescriptor(kMaterial, "Standard", "_MATERIAL_FEATURE_TRANSMISSION 1");
        public static FieldDescriptor SpecularColor =           new FieldDescriptor(kMaterial, "SpecularColor", "_MATERIAL_FEATURE_TRANSMISSION 1");

        // Refraction
        public static FieldDescriptor Refraction =              new FieldDescriptor(string.Empty, "Refraction", "");
        public static KeywordDescriptor RefractionKeyword = new KeywordDescriptor()
        {
            displayName = "Refraction Model",
            referenceName = "_REFRACTION",
            type = KeywordType.Enum,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "Off", referenceName = "OFF" },
                new KeywordEntry() { displayName = "Plane", referenceName = "PLANE" },
                new KeywordEntry() { displayName = "Sphere", referenceName = "SPHERE" },
                new KeywordEntry() { displayName = "Thin", referenceName = "THIN" },
            }
        };

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);
            AddDistortionFields(ref context);
            var descs = context.blocks.Select(x => x.descriptor);

            bool hasRefraction = (systemData.surfaceType == SurfaceType.Transparent && litData.refractionModel != ScreenSpaceRefraction.RefractionModel.None);

            // Lit specific properties
            context.AddField(DotsProperties,                       context.hasDotsProperties);

            // Material
            context.AddField(Anisotropy,                           litData.materialType == HDLitData.MaterialType.Anisotropy);
            context.AddField(Iridescence,                          litData.materialType == HDLitData.MaterialType.Iridescence);
            context.AddField(SpecularColor,                        litData.materialType == HDLitData.MaterialType.SpecularColor);
            context.AddField(Standard,                             litData.materialType == HDLitData.MaterialType.Standard);
            context.AddField(SubsurfaceScattering,                 litData.materialType == HDLitData.MaterialType.SubsurfaceScattering && systemData.surfaceType != SurfaceType.Transparent);
            context.AddField(Transmission,                         (litData.materialType == HDLitData.MaterialType.SubsurfaceScattering && litData.sssTransmission) ||
                (litData.materialType == HDLitData.MaterialType.Translucent));
            context.AddField(Translucent,                          litData.materialType == HDLitData.MaterialType.Translucent);

            // Refraction
            context.AddField(Refraction,                           hasRefraction);

            // Misc
            context.AddField(EnergyConservingSpecular,             litData.energyConservingSpecular);
            context.AddField(CoatMask,                             descs.Contains(BlockFields.SurfaceDescription.CoatMask) && context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.CoatMask) && litData.clearCoat);
            context.AddField(ClearCoat,                            litData.clearCoat); // Enable clear coat material feature
            context.AddField(RayTracing,                           litData.rayTracing);

            context.AddField(SpecularAA, lightingData.specularAA &&
                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAThreshold) &&
                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance));
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            bool hasRefraction = (systemData.surfaceType == SurfaceType.Transparent && systemData.renderQueueType != HDRenderQueue.RenderQueueType.PreRefraction && litData.refractionModel != ScreenSpaceRefraction.RefractionModel.None);
            bool hasDistortion = (systemData.surfaceType == SurfaceType.Transparent && builtinData.distortion);

            // Vertex
            base.GetActiveBlocks(ref context);

            // Common
            context.AddBlock(BlockFields.SurfaceDescription.CoatMask,             litData.clearCoat);

            // Refraction
            context.AddBlock(HDBlockFields.SurfaceDescription.RefractionIndex,      hasRefraction);
            context.AddBlock(HDBlockFields.SurfaceDescription.RefractionColor,      hasRefraction);
            context.AddBlock(HDBlockFields.SurfaceDescription.RefractionDistance,   hasRefraction);

            // Material

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

            context.AddBlock(tangentBlock,                                          litData.materialType == HDLitData.MaterialType.Anisotropy);
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

            // Refraction model property allow the material inspector to check if refraction is enabled in the shader.
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                floatType = FloatType.Enum,
                hidden = true,
                value = (int)litData.refractionModel,
                enumNames = Enum.GetNames(typeof(ScreenSpaceRefraction.RefractionModel)).ToList(),
                overrideReferenceName = kRefractionModel,
            });
        }

        protected override void CollectPassKeywords(ref PassDescriptor pass)
        {
            base.CollectPassKeywords(ref pass);
            pass.keywords.Add(RefractionKeyword);
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
