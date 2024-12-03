using System;
using System.Linq;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Legacy;
using static UnityEngine.Rendering.HighDefinition.HDMaterial;
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;
using static UnityEditor.Rendering.HighDefinition.HDFields;
using System.Collections.Generic;
using UnityEditor.Rendering.Fullscreen.ShaderGraph;
using UnityEngine;

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
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Lit/ShaderGraph/"
        };

        protected override string[] templateMaterialDirectories => passTemplateMaterialDirectories;
        protected override GUID subTargetAssetGuid => kSubTargetSourceCodeGuid;
        protected override string postDecalsInclude => "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl";
        protected override ShaderID shaderID => ShaderID.SG_Lit;
        protected override string raytracingInclude => CoreIncludes.kLitRaytracing;
        protected override string pathtracingInclude => CoreIncludes.kLitPathtracing;
        protected override FieldDescriptor subShaderField => new FieldDescriptor(kSubShader, "Lit Subshader", "");
        protected override string subShaderInclude => CoreIncludes.kLit;
        protected override string customInspector => "Rendering.HighDefinition.LitShaderGraphGUI";

        // SubShader features
        protected override bool supportDistortion => true;
        protected override bool supportForward => false;
        protected override bool supportPathtracing => !TargetsVFX();
        protected override bool requireSplitLighting => litData.HasMaterialType(HDLitData.MaterialTypeMask.SubsurfaceScattering);

        protected override SubShaderDescriptor GetSubShaderDescriptor()
        {
            var descriptor = base.GetSubShaderDescriptor();

            descriptor.passes.Add(HDShaderPasses.GenerateLitDepthOnly(TargetsVFX(), systemData.tessellation));
            descriptor.passes.Add(HDShaderPasses.GenerateGBuffer(TargetsVFX(), systemData.tessellation));
            descriptor.passes.Add(HDShaderPasses.GenerateLitForward(TargetsVFX(), systemData.tessellation));
            if (!systemData.tessellation && supportRaytracing) // Raytracing don't support tessellation
                descriptor.passes.Add(HDShaderPasses.GenerateLitRaytracingPrepass());

            return descriptor;
        }

        protected override SubShaderDescriptor GetRaytracingSubShaderDescriptor()
        {
            var descriptor = base.GetRaytracingSubShaderDescriptor();

            if (litData.HasMaterialType(HDLitData.MaterialTypeMask.SubsurfaceScattering))
                descriptor.passes.Add(HDShaderPasses.GenerateRaytracingSubsurface());

            return descriptor;
        }

        // Refraction
        public static FieldDescriptor Refraction = new FieldDescriptor(string.Empty, "Refraction", "");
        public static KeywordDescriptor RefractionKeyword = new KeywordDescriptor()
        {
            displayName = "Refraction Model",
            referenceName = "_REFRACTION",
            type = KeywordType.Enum,
            definition = KeywordDefinition.ShaderFeature,
            scope = KeywordScope.Local,
            entries = new KeywordEntry[]
            {
                new KeywordEntry() { displayName = "Off", referenceName = "" },
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

            bool hasRefraction = systemData.surfaceType == SurfaceType.Transparent && litData.refractionModel != ScreenSpaceRefraction.RefractionModel.None;
            bool hasClearCoat = litData.clearCoat && litData.HasMaterialType(~HDLitData.MaterialTypeMask.ColoredTranslucent); // Colored translucent doesn't support clear coat

            // Lit specific properties
            context.AddField(DotsProperties, context.hasDotsProperties);

            // Refraction
            context.AddField(Refraction, hasRefraction);

            // Misc
            context.AddField(EnergyConservingSpecular, litData.energyConservingSpecular);
            context.AddField(CoatMask, descs.Contains(BlockFields.SurfaceDescription.CoatMask) && context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.CoatMask) && hasClearCoat);
            context.AddField(RayTracing, litData.rayTracing);

            context.AddField(SpecularAA, lightingData.specularAA &&
                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAThreshold) &&
                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance));
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            bool hasTransmissionTint = litData.HasMaterialType(HDLitData.MaterialTypeMask.ColoredTranslucent);
            bool hasTransmissionMask = litData.HasMaterialType(HDLitData.MaterialTypeMask.Translucent) || (litData.HasMaterialType(HDLitData.MaterialTypeMask.SubsurfaceScattering) && litData.sssTransmission);
            bool hasRefraction = systemData.surfaceType == SurfaceType.Transparent && systemData.renderQueueType != HDRenderQueue.RenderQueueType.PreRefraction && litData.refractionModel != ScreenSpaceRefraction.RefractionModel.None;
            bool hasClearCoat = litData.clearCoat && litData.HasMaterialType(~HDLitData.MaterialTypeMask.ColoredTranslucent); // Colored translucent doesn't support clear coat

            // Vertex
            base.GetActiveBlocks(ref context);

            // Common
            context.AddBlock(BlockFields.SurfaceDescription.CoatMask, hasClearCoat);

            // Refraction
            context.AddBlock(HDBlockFields.SurfaceDescription.RefractionIndex, hasRefraction);
            context.AddBlock(HDBlockFields.SurfaceDescription.RefractionColor, hasRefraction);
            context.AddBlock(HDBlockFields.SurfaceDescription.RefractionDistance, hasRefraction);

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

            context.AddBlock(tangentBlock, litData.HasMaterialType(HDLitData.MaterialTypeMask.Anisotropy));
            context.AddBlock(HDBlockFields.SurfaceDescription.Anisotropy, litData.HasMaterialType(HDLitData.MaterialTypeMask.Anisotropy));
            context.AddBlock(HDBlockFields.SurfaceDescription.SubsurfaceMask, litData.HasMaterialType(HDLitData.MaterialTypeMask.SubsurfaceScattering));
            context.AddBlock(HDBlockFields.SurfaceDescription.TransmissionMask, hasTransmissionMask);
            context.AddBlock(HDBlockFields.SurfaceDescription.TransmissionTint, hasTransmissionTint);
            context.AddBlock(HDBlockFields.SurfaceDescription.Thickness, hasTransmissionMask || hasRefraction);
            context.AddBlock(HDBlockFields.SurfaceDescription.DiffusionProfileHash, litData.HasMaterialType(HDLitData.MaterialTypeMask.SubsurfaceScattering) || litData.HasMaterialType(HDLitData.MaterialTypeMask.Translucent));
            context.AddBlock(HDBlockFields.SurfaceDescription.IridescenceMask, litData.HasMaterialType(HDLitData.MaterialTypeMask.Iridescence));
            context.AddBlock(HDBlockFields.SurfaceDescription.IridescenceThickness, litData.HasMaterialType(HDLitData.MaterialTypeMask.Iridescence));
            context.AddBlock(BlockFields.SurfaceDescription.Specular, litData.HasMaterialType(HDLitData.MaterialTypeMask.SpecularColor));
            context.AddBlock(BlockFields.SurfaceDescription.Metallic, litData.HasMaterialType(HDLitData.MaterialTypeMask.Standard) ||
                litData.HasMaterialType(HDLitData.MaterialTypeMask.Anisotropy) ||
                litData.HasMaterialType(HDLitData.MaterialTypeMask.Iridescence));
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

            var enumNames = new List<string>();
            var enumValues = new List<int>();
            foreach (HDLitData.MaterialTypeMask value in Enum.GetValues(typeof(HDLitData.MaterialTypeMask)))
            {
                if (litData.HasMaterialType(value))
                {
                    enumNames.Add(value.ToString());
                    enumValues.Add((int)Mathf.Log((int)value, 2)); // Convert mask value to index
                }
            }

            var defaultMaterialType = enumValues.First();
            if (generationMode == GenerationMode.Preview && enumValues.Count > 1)
            {
                // For the SG preview, we select the first material type after standard to show it
                if (enumValues[0] == (int)HDLitData.MaterialTypeMask.Standard)
                    defaultMaterialType = enumValues[1];
            }

            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                floatType = FloatType.Enum,
                enumType = EnumType.Enum,
                enumNames = enumNames,
                enumValues = enumValues,
                hidden = true,
                displayName = kMaterialID,
                overrideReferenceName = kMaterialID,
                value = defaultMaterialType,
            });

            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                floatType = FloatType.Default,
                hidden = true,
                displayName = kMaterialTypeMask,
                overrideReferenceName = kMaterialTypeMask,
                value = (int)litData.materialTypeMask,
            });

            collector.AddBoolProperty(kTransmissionEnable, litData.sssTransmission);
            if (litData.clearCoat && litData.HasMaterialType(~HDLitData.MaterialTypeMask.ColoredTranslucent))
                collector.AddBoolProperty(kClearCoatEnabled, true);
        }

        static readonly List<string> materialFeatureSuffixes = new()
        {
            "SUBSURFACE_SCATTERING",
            "TRANSMISSION",
            "ANISOTROPY",
            "IRIDESCENCE",
            "SPECULAR_COLOR",
            "COLORED_TRANSMISSION",
        };

        protected override void CollectPassKeywords(ref PassDescriptor pass)
        {
            base.CollectPassKeywords(ref pass);
            pass.keywords.Add(RefractionKeyword);

            if (pass.IsLightingOrMaterial())
            {
                foreach (var featureDefine in materialFeatureSuffixes)
                {
                    pass.keywords.Add(new KeywordDescriptor
                    {
                        displayName = "Material Type",
                        referenceName = "_MATERIAL_FEATURE",
                        type = KeywordType.Enum,
                        definition = KeywordDefinition.ShaderFeature,
                        scope = KeywordScope.Local,
                        stages = KeywordShaderStage.Fragment | (supportRaytracing ? KeywordShaderStage.RayTracing : 0),
                        entries = new KeywordEntry[]
                        {
                            new() { displayName = featureDefine, referenceName = featureDefine },
                        }
                    });
                }
            }

            if (!pass.IsShadow())
            {
                if (litData.clearCoat && litData.HasMaterialType(~HDLitData.MaterialTypeMask.ColoredTranslucent))
                {
                    pass.keywords.Add(new KeywordDescriptor
                    {
                        displayName = "Cleat Coat",
                        referenceName = "_MATERIAL_FEATURE_CLEAR_COAT",
                        type = KeywordType.Boolean,
                        definition = KeywordDefinition.ShaderFeature,
                        scope = KeywordScope.Local,
                        stages = KeywordShaderStage.Fragment | (supportRaytracing ? KeywordShaderStage.RayTracing : 0),
                    });
                }
            }
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new LitSurfaceOptionPropertyBlock(litData));
            if (systemData.surfaceType == SurfaceType.Transparent)
                blockList.AddPropertyBlock(new DistortionPropertyBlock());
            blockList.AddPropertyBlock(new AdvancedOptionsPropertyBlock());
        }

        protected override int ComputeMaterialNeedsUpdateHash()
        {
            int hash = base.ComputeMaterialNeedsUpdateHash();

            unchecked
            {
                // hash must be 0 by default when we create a ShaderGraph, otherwise it's dirty when opened for the first time.
                int h = (int)litData.materialTypeMask - (int)HDLitData.MaterialTypeMask.Standard;
                hash = hash * 23 + h;
            }

            return hash;
        }

        internal override void MigrateTo(ShaderGraphVersion version)
        {
            base.MigrateTo(version);

            if (version == ShaderGraphVersion.MaterialType)
                UpgradeToMaterialType();
        }
    }
}
