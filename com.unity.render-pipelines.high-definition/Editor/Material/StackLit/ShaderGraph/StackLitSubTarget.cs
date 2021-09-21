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
    //TODO:
    // clamp in shader code the ranged() properties
    // or let inputs (eg mask?) follow invalid values ? Lit does that (let them free running).
    sealed partial class StackLitSubTarget : LightingSubTarget, ILegacyTarget, IRequiresData<StackLitData>
    {
        public StackLitSubTarget() => displayName = "StackLit";

        static readonly GUID kSubTargetSourceCodeGuid = new GUID("5f7ba34a143e67647b202a662748dae3");  // StackLitSubTarget.cs

        static string[] passTemplateMaterialDirectories = new string[]
        {
            $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/StackLit/ShaderGraph/"
        };

        protected override string[] templateMaterialDirectories => passTemplateMaterialDirectories;
        protected override GUID subTargetAssetGuid => kSubTargetSourceCodeGuid;
        protected override ShaderID shaderID => HDShaderUtils.ShaderID.SG_StackLit;
        protected override FieldDescriptor subShaderField => new FieldDescriptor(kSubShader, "StackLit SubShader", "");
        protected override string raytracingInclude => CoreIncludes.kStackLitRaytracing;
        protected override string pathtracingInclude => CoreIncludes.kStackLitPathtracing;
        protected override string subShaderInclude => CoreIncludes.kStackLit;

        // SubShader features
        protected override bool supportPathtracing => true;
        protected override bool supportDistortion => true;
        protected override bool requireSplitLighting => stackLitData.subsurfaceScattering;

        StackLitData m_StackLitData;

        StackLitData IRequiresData<StackLitData>.data
        {
            get => m_StackLitData;
            set => m_StackLitData = value;
        }

        public StackLitData stackLitData
        {
            get => m_StackLitData;
            set => m_StackLitData = value;
        }

        const string kSSSpecularOcclusionBaseMode = "ScreenSpaceSpecularOcclusionBaseMode";
        const string kSSSpecularOcclusionAOConeSize = "ScreenSpaceSpecularOcclusionAOConeSize";
        const string kSSSpecularOcclusionAOConeDir = "ScreenSpaceSpecularOcclusionAOConeDir";
        const string kDataBasedSpecularOcclusionBaseMode = "DataBasedSpecularOcclusionBaseMode";
        const string kDataBasedSpecularOcclusionAOConeSize = "DataBasedSpecularOcclusionAOConeSize";
        const string kSpecularOcclusionConeFixupMethod = "SpecularOcclusionConeFixupMethod";
        const string kDualSpecularLobeParametrization = "DualSpecularLobeParametrization";
        const string kBaseParametrization = "BaseParametrization";

        // Material
        public static FieldDescriptor Coat = new FieldDescriptor(kMaterial, "Coat", "_MATERIAL_FEATURE_COAT");
        public static FieldDescriptor DualSpecularLobe = new FieldDescriptor(kMaterial, "DualSpecularLobe", "_MATERIAL_FEATURE_DUAL_SPECULAR_LOBE");
        public static FieldDescriptor CoatNormal = new FieldDescriptor(kMaterial, "CoatNormal", "_MATERIAL_FEATURE_COAT_NORMALMAP");

        // Advanced
        public static FieldDescriptor AnisotropyForAreaLights = new FieldDescriptor(string.Empty, "AnisotropyForAreaLights", "_ANISOTROPY_FOR_AREA_LIGHTS");
        public static FieldDescriptor RecomputeStackPerLight = new FieldDescriptor(string.Empty, "RecomputeStackPerLight", "_VLAYERED_RECOMPUTE_PERLIGHT");
        public static FieldDescriptor HonorPerLightMinRoughness = new FieldDescriptor(string.Empty, "HonorPerLightMinRoughness", "_STACK_LIT_HONORS_LIGHT_MIN_ROUGHNESS");
        public static FieldDescriptor ShadeBaseUsingRefractedAngles = new FieldDescriptor(string.Empty, "ShadeBaseUsingRefractedAngles", "_VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE");
        public static FieldDescriptor StackLitDebug = new FieldDescriptor(string.Empty, "StackLitDebug", "_STACKLIT_DEBUG");
        public static FieldDescriptor CapHazinessIfNotMetallic = new FieldDescriptor(string.Empty, "CapHazinessIfNotMetallic", "");
        public static FieldDescriptor GeometricSpecularAA = new FieldDescriptor(kSpecular, "GeometricAA", "_ENABLE_GEOMETRIC_SPECULAR_AA 1");

        // Screen Space Specular Occlusion Base Mode
        public static FieldDescriptor SSSpecularOcclusionBaseModeOff = new FieldDescriptor(kSSSpecularOcclusionBaseMode, "Off", "_SCREENSPACE_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_DISABLED");
        public static FieldDescriptor SSSpecularOcclusionBaseModeDirectFromAO = new FieldDescriptor(kSSSpecularOcclusionBaseMode, "DirectFromAO", "_SCREENSPACE_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_FROM_AO");
        public static FieldDescriptor SSSpecularOcclusionBaseModeConeConeFromBentAO = new FieldDescriptor(kSSSpecularOcclusionBaseMode, "ConeConeFromBentAO", "_SCREENSPACE_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_CONECONE");
        public static FieldDescriptor SSSpecularOcclusionBaseModeSPTDIntegrationOfBentAO = new FieldDescriptor(kSSSpecularOcclusionBaseMode, "SPTDIntegrationOfBentAO", "_SCREENSPACE_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_SPTD");
        public static FieldDescriptor SSSpecularOcclusionBaseModeCustom = new FieldDescriptor(kSSSpecularOcclusionBaseMode, "Custom", "_SCREENSPACE_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_FROM_AO");

        // Screen Space Specular Occlusion AO Cone Size
        public static FieldDescriptor SSSpecularOcclusionAOConeSizeUniformAO = new FieldDescriptor(kSSSpecularOcclusionAOConeSize, "UniformAO", "_SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT BENT_VISIBILITY_FROM_AO_UNIFORM");
        public static FieldDescriptor SSSpecularOcclusionAOConeSizeCosWeightedAO = new FieldDescriptor(kSSSpecularOcclusionAOConeSize, "CosWeightedAO", "_SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT BENT_VISIBILITY_FROM_AO_COS");
        public static FieldDescriptor SSSpecularOcclusionAOConeSizeCosWeightedBentCorrectAO = new FieldDescriptor(kSSSpecularOcclusionAOConeSize, "CosWeightedBentCorrectAO", "_SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT BENT_VISIBILITY_FROM_AO_COS_BENT_CORRECTION");

        // Screen Space Specular Occlusion AO Cone Dir
        public static FieldDescriptor SSSpecularOcclusionAOConeDirGeomNormal = new FieldDescriptor(kSSSpecularOcclusionAOConeDir, "GeomNormal", "_SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_DIR BENT_VISIBILITY_DIR_GEOM_NORMAL");
        public static FieldDescriptor SSSpecularOcclusionAOConeDirBentNormal = new FieldDescriptor(kSSSpecularOcclusionAOConeDir, "BentNormal", "_SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_DIR BENT_VISIBILITY_DIR_BENT_NORMAL");
        public static FieldDescriptor SSSpecularOcclusionAOConeDirShadingNormal = new FieldDescriptor(kSSSpecularOcclusionAOConeDir, "ShadingNormal", "_SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_DIR BENT_VISIBILITY_DIR_SHADING_NORMAL");

        // Data Bases Specular Occlusion Base Mode
        public static FieldDescriptor DataBasedSpecularOcclusionBaseModeOff = new FieldDescriptor(kDataBasedSpecularOcclusionBaseMode, "Off", "_DATABASED_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_DISABLED");
        public static FieldDescriptor DataBasedSpecularOcclusionBaseModeDirectFromAO = new FieldDescriptor(kDataBasedSpecularOcclusionBaseMode, "DirectFromAO", "_DATABASED_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_FROM_AO");
        public static FieldDescriptor DataBasedSpecularOcclusionBaseModeConeConeFromBentAO = new FieldDescriptor(kDataBasedSpecularOcclusionBaseMode, "ConeConeFromBentAO", "_DATABASED_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_CONECONE");
        public static FieldDescriptor DataBasedSpecularOcclusionBaseModeSPTDIntegrationOfBentAO = new FieldDescriptor(kDataBasedSpecularOcclusionBaseMode, "SPTDIntegrationOfBentAO", "_DATABASED_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_SPTD");
        public static FieldDescriptor DataBasedSpecularOcclusionBaseModeCustom = new FieldDescriptor(kDataBasedSpecularOcclusionBaseMode, "Custom", "_DATABASED_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_CUSTOM_EXT_INPUT");

        // Data Based Specular Occlusion AO Cone Size
        public static FieldDescriptor DataBasedSpecularOcclusionAOConeSizeUniformAO = new FieldDescriptor(kDataBasedSpecularOcclusionAOConeSize, "UniformAO", "_DATABASED_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT BENT_VISIBILITY_FROM_AO_UNIFORM");
        public static FieldDescriptor DataBasedSpecularOcclusionAOConeSizeCosWeightedAO = new FieldDescriptor(kDataBasedSpecularOcclusionAOConeSize, "CosWeightedAO", "_DATABASED_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT BENT_VISIBILITY_FROM_AO_COS");
        public static FieldDescriptor DataBasedSpecularOcclusionAOConeSizeCosWeightedBentCorrectAO = new FieldDescriptor(kDataBasedSpecularOcclusionAOConeSize, "CosWeightedBentCorrectAO", "_DATABASED_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT BENT_VISIBILITY_FROM_AO_COS_BENT_CORRECTION");

        // Specular Occlusion Cone Fixup Method
        public static FieldDescriptor SpecularOcclusionConeFixupMethodOff = new FieldDescriptor(kSpecularOcclusionConeFixupMethod, "Off", "_BENT_VISIBILITY_FIXUP_FLAGS BENT_VISIBILITY_FIXUP_FLAGS_NONE");
        public static FieldDescriptor SpecularOcclusionConeFixupMethodBoostBSDFRoughness = new FieldDescriptor(kSpecularOcclusionConeFixupMethod, "BoostBSDFRoughness", "_BENT_VISIBILITY_FIXUP_FLAGS BENT_VISIBILITY_FIXUP_FLAGS_BOOST_BSDF_ROUGHNESS");
        public static FieldDescriptor SpecularOcclusionConeFixupMethodTiltDirectionToGeomNormal = new FieldDescriptor(kSpecularOcclusionConeFixupMethod, "TiltDirectionToGeomNormal", "_BENT_VISIBILITY_FIXUP_FLAGS BENT_VISIBILITY_FIXUP_FLAGS_TILT_BENTNORMAL_TO_GEOM");
        public static FieldDescriptor SpecularOcclusionConeFixupMethodBoostAndTilt = new FieldDescriptor(kSpecularOcclusionConeFixupMethod, "BoostAndTilt", "_BENT_VISIBILITY_FIXUP_FLAGS (BENT_VISIBILITY_FIXUP_FLAGS_BOOST_BSDF_ROUGHNESS|BENT_VISIBILITY_FIXUP_FLAGS_TILT_BENTNORMAL_TO_GEOM)");

        // Dual Specular Lobe Parametrization
        public static FieldDescriptor HazyGloss = new FieldDescriptor(kDualSpecularLobeParametrization, "HazyGloss", "_MATERIAL_FEATURE_HAZY_GLOSS");

        // Base Parametrization
        public static FieldDescriptor BaseParamSpecularColor = new FieldDescriptor(kBaseParametrization, "SpecularColor", "_MATERIAL_FEATURE_SPECULAR_COLOR");

        protected override SubShaderDescriptor GetRaytracingSubShaderDescriptor()
        {
            var descriptor = base.GetRaytracingSubShaderDescriptor();

            if (stackLitData.subsurfaceScattering)
                descriptor.passes.Add(HDShaderPasses.GenerateRaytracingSubsurface());

            return descriptor;
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);
            AddDistortionFields(ref context);
            var descs = context.blocks.Select(x => x.descriptor);

            // StackLit specific properties
            // Material
            context.AddField(Anisotropy, stackLitData.anisotropy);
            context.AddField(Coat, stackLitData.coat);
            context.AddField(CoatMask, stackLitData.coat && context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.CoatMask) &&
                descs.Contains(BlockFields.SurfaceDescription.CoatMask));
            // context.AddField(CoatMaskZero,                 coat.isOn && pass.pixelBlocks.Contains(CoatMaskSlotId) &&
            //                                                                 FindSlot<Vector1MaterialSlot>(CoatMaskSlotId).value == 0.0f),
            // context.AddField(CoatMaskOne,                  coat.isOn && pass.pixelBlocks.Contains(CoatMaskSlotId) &&
            //                                                                 FindSlot<Vector1MaterialSlot>(CoatMaskSlotId).value == 1.0f),
            context.AddField(CoatNormal, stackLitData.coatNormal
                && (context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.CoatNormalOS)
                    || context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.CoatNormalTS)
                    || context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.CoatNormalWS)));
            context.AddField(Iridescence, stackLitData.iridescence);
            context.AddField(SubsurfaceScattering, stackLitData.subsurfaceScattering && systemData.surfaceType != SurfaceType.Transparent);
            context.AddField(Transmission, stackLitData.transmission);
            context.AddField(DualSpecularLobe, stackLitData.dualSpecularLobe);

            // Base Parametrization
            // Even though we can just always transfer the present (check with $SurfaceDescription.*) fields like specularcolor
            // and metallic, we still need to know the baseParametrization in the template to translate into the
            // _MATERIAL_FEATURE_SPECULAR_COLOR define:
            context.AddField(BaseParamSpecularColor, stackLitData.baseParametrization == StackLit.BaseParametrization.SpecularColor);

            // Dual Specular Lobe Parametrization
            context.AddField(HazyGloss, stackLitData.dualSpecularLobe &&
                stackLitData.dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.HazyGloss);

            // Misc
            context.AddField(EnergyConservingSpecular, stackLitData.energyConservingSpecular);
            // Option for baseParametrization == Metallic && DualSpecularLobeParametrization == HazyGloss:
            // Again we assume masternode has HazyGlossMaxDielectricF0 which should always be the case
            // if capHazinessWrtMetallic.isOn.
            context.AddField(CapHazinessIfNotMetallic, stackLitData.dualSpecularLobe &&
                stackLitData.dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.HazyGloss &&
                stackLitData.capHazinessWrtMetallic && stackLitData.baseParametrization == StackLit.BaseParametrization.BaseMetallic
                && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.HazyGlossMaxDielectricF0));
            // Note here we combine an "enable"-like predicate and the $SurfaceDescription.(slotname) predicate
            // into a single $GeometricSpecularAA pedicate.
            //
            // ($SurfaceDescription.* predicates are useful to make sure the field is present in the struct in the template.
            // The field will be present if both the master node and pass have the slotid, see this set intersection we make
            // in GenerateSurfaceDescriptionStruct(), with HDSubShaderUtilities.FindMaterialSlotsOnNode().)
            //
            // Normally, since the feature enable adds the required slots, only the $SurfaceDescription.* would be required,
            // but some passes might not need it and not declare the PixelShaderSlot, or, inversely, the pass might not
            // declare it as a way to avoid it.
            //
            // IE this has also the side effect to disable geometricSpecularAA - even if "on" - for passes that don't explicitly
            // advertise these slots(eg for a general feature, with separate "enable" and "field present" predicates, the
            // template could take a default value and process it anyway if a feature is "on").
            //
            // (Note we can achieve the same results in the template on just single predicates by making defines out of them,
            // and using #if defined() && etc)
            context.AddField(GeometricSpecularAA, lightingData.specularAA &&
                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance) &&
                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAThreshold));
            context.AddField(SpecularAA, lightingData.specularAA &&
                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance) &&
                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAThreshold));
            context.AddField(SpecularOcclusion, stackLitData.screenSpaceSpecularOcclusionBaseMode != StackLitData.SpecularOcclusionBaseMode.Off ||
                stackLitData.dataBasedSpecularOcclusionBaseMode != StackLitData.SpecularOcclusionBaseMode.Off);

            // Advanced
            context.AddField(AnisotropyForAreaLights, stackLitData.anisotropyForAreaLights);
            context.AddField(RecomputeStackPerLight, stackLitData.recomputeStackPerLight);
            context.AddField(HonorPerLightMinRoughness, stackLitData.honorPerLightMinRoughness);
            context.AddField(ShadeBaseUsingRefractedAngles, stackLitData.shadeBaseUsingRefractedAngles);
            context.AddField(StackLitDebug, stackLitData.debug);

            // Screen Space Specular Occlusion Base Mode
            context.AddField(SSSpecularOcclusionBaseModeOff, stackLitData.screenSpaceSpecularOcclusionBaseMode == StackLitData.SpecularOcclusionBaseMode.Off);
            context.AddField(SSSpecularOcclusionBaseModeDirectFromAO, stackLitData.screenSpaceSpecularOcclusionBaseMode == StackLitData.SpecularOcclusionBaseMode.DirectFromAO);
            context.AddField(SSSpecularOcclusionBaseModeConeConeFromBentAO, stackLitData.screenSpaceSpecularOcclusionBaseMode == StackLitData.SpecularOcclusionBaseMode.ConeConeFromBentAO);
            context.AddField(SSSpecularOcclusionBaseModeSPTDIntegrationOfBentAO, stackLitData.screenSpaceSpecularOcclusionBaseMode == StackLitData.SpecularOcclusionBaseMode.SPTDIntegrationOfBentAO);
            context.AddField(SSSpecularOcclusionBaseModeCustom, stackLitData.screenSpaceSpecularOcclusionBaseMode == StackLitData.SpecularOcclusionBaseMode.Custom);

            // Screen Space Specular Occlusion AO Cone Size
            context.AddField(SSSpecularOcclusionAOConeSizeUniformAO, SpecularOcclusionModeUsesVisibilityCone(stackLitData.screenSpaceSpecularOcclusionBaseMode) &&
                stackLitData.screenSpaceSpecularOcclusionAOConeSize == StackLitData.SpecularOcclusionAOConeSize.UniformAO);
            context.AddField(SSSpecularOcclusionAOConeSizeCosWeightedAO, SpecularOcclusionModeUsesVisibilityCone(stackLitData.screenSpaceSpecularOcclusionBaseMode) &&
                stackLitData.screenSpaceSpecularOcclusionAOConeSize == StackLitData.SpecularOcclusionAOConeSize.CosWeightedAO);
            context.AddField(SSSpecularOcclusionAOConeSizeCosWeightedBentCorrectAO, SpecularOcclusionModeUsesVisibilityCone(stackLitData.screenSpaceSpecularOcclusionBaseMode) &&
                stackLitData.screenSpaceSpecularOcclusionAOConeSize == StackLitData.SpecularOcclusionAOConeSize.CosWeightedBentCorrectAO);

            // Screen Space Specular Occlusion AO Cone Dir
            context.AddField(SSSpecularOcclusionAOConeDirGeomNormal, SpecularOcclusionModeUsesVisibilityCone(stackLitData.screenSpaceSpecularOcclusionBaseMode) &&
                stackLitData.screenSpaceSpecularOcclusionAOConeDir == StackLitData.SpecularOcclusionAOConeDir.GeomNormal);
            context.AddField(SSSpecularOcclusionAOConeDirBentNormal, SpecularOcclusionModeUsesVisibilityCone(stackLitData.screenSpaceSpecularOcclusionBaseMode) &&
                stackLitData.screenSpaceSpecularOcclusionAOConeDir == StackLitData.SpecularOcclusionAOConeDir.BentNormal);
            context.AddField(SSSpecularOcclusionAOConeDirShadingNormal, SpecularOcclusionModeUsesVisibilityCone(stackLitData.screenSpaceSpecularOcclusionBaseMode) &&
                stackLitData.screenSpaceSpecularOcclusionAOConeDir == StackLitData.SpecularOcclusionAOConeDir.ShadingNormal);

            // Data Based Specular Occlusion Base Mode
            context.AddField(DataBasedSpecularOcclusionBaseModeOff, stackLitData.dataBasedSpecularOcclusionBaseMode == StackLitData.SpecularOcclusionBaseMode.Off);
            context.AddField(DataBasedSpecularOcclusionBaseModeDirectFromAO, stackLitData.dataBasedSpecularOcclusionBaseMode == StackLitData.SpecularOcclusionBaseMode.DirectFromAO);
            context.AddField(DataBasedSpecularOcclusionBaseModeConeConeFromBentAO, stackLitData.dataBasedSpecularOcclusionBaseMode == StackLitData.SpecularOcclusionBaseMode.ConeConeFromBentAO);
            context.AddField(DataBasedSpecularOcclusionBaseModeSPTDIntegrationOfBentAO, stackLitData.dataBasedSpecularOcclusionBaseMode == StackLitData.SpecularOcclusionBaseMode.SPTDIntegrationOfBentAO);
            context.AddField(DataBasedSpecularOcclusionBaseModeCustom, stackLitData.dataBasedSpecularOcclusionBaseMode == StackLitData.SpecularOcclusionBaseMode.Custom);

            // Data Based Specular Occlusion AO Cone Size
            context.AddField(DataBasedSpecularOcclusionAOConeSizeUniformAO, SpecularOcclusionModeUsesVisibilityCone(stackLitData.dataBasedSpecularOcclusionBaseMode) &&
                stackLitData.dataBasedSpecularOcclusionAOConeSize == StackLitData.SpecularOcclusionAOConeSize.UniformAO);
            context.AddField(DataBasedSpecularOcclusionAOConeSizeCosWeightedAO, SpecularOcclusionModeUsesVisibilityCone(stackLitData.dataBasedSpecularOcclusionBaseMode) &&
                stackLitData.dataBasedSpecularOcclusionAOConeSize == StackLitData.SpecularOcclusionAOConeSize.CosWeightedAO);
            context.AddField(DataBasedSpecularOcclusionAOConeSizeCosWeightedBentCorrectAO, SpecularOcclusionModeUsesVisibilityCone(stackLitData.dataBasedSpecularOcclusionBaseMode) &&
                stackLitData.dataBasedSpecularOcclusionAOConeSize == StackLitData.SpecularOcclusionAOConeSize.CosWeightedBentCorrectAO);

            // Specular Occlusion Cone Fixup Method
            context.AddField(SpecularOcclusionConeFixupMethodOff, SpecularOcclusionUsesBentNormal(stackLitData) &&
                stackLitData.specularOcclusionConeFixupMethod == StackLitData.SpecularOcclusionConeFixupMethod.Off);
            context.AddField(SpecularOcclusionConeFixupMethodBoostBSDFRoughness, SpecularOcclusionUsesBentNormal(stackLitData) &&
                stackLitData.specularOcclusionConeFixupMethod == StackLitData.SpecularOcclusionConeFixupMethod.BoostBSDFRoughness);
            context.AddField(SpecularOcclusionConeFixupMethodTiltDirectionToGeomNormal, SpecularOcclusionUsesBentNormal(stackLitData) &&
                stackLitData.specularOcclusionConeFixupMethod == StackLitData.SpecularOcclusionConeFixupMethod.TiltDirectionToGeomNormal);
            context.AddField(SpecularOcclusionConeFixupMethodBoostAndTilt, SpecularOcclusionUsesBentNormal(stackLitData) &&
                stackLitData.specularOcclusionConeFixupMethod == StackLitData.SpecularOcclusionConeFixupMethod.BoostAndTilt);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            base.GetActiveBlocks(ref context);

            // Common

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
            context.AddBlock(HDBlockFields.SurfaceDescription.Anisotropy, stackLitData.anisotropy);
            context.AddBlock(HDBlockFields.SurfaceDescription.SubsurfaceMask, stackLitData.subsurfaceScattering);
            context.AddBlock(HDBlockFields.SurfaceDescription.Thickness, stackLitData.transmission);
            context.AddBlock(HDBlockFields.SurfaceDescription.DiffusionProfileHash, stackLitData.subsurfaceScattering || stackLitData.transmission);

            // Base Metallic
            context.AddBlock(BlockFields.SurfaceDescription.Metallic, stackLitData.baseParametrization == StackLit.BaseParametrization.BaseMetallic);
            context.AddBlock(HDBlockFields.SurfaceDescription.DielectricIor, stackLitData.baseParametrization == StackLit.BaseParametrization.BaseMetallic);

            // Base Specular
            context.AddBlock(BlockFields.SurfaceDescription.Specular, stackLitData.baseParametrization == StackLit.BaseParametrization.SpecularColor);

            // Specular Occlusion
            // for custom (external) SO replacing data based SO (which normally comes from some func of DataBasedSOMode(dataAO, optional bent normal))
            // TODO: we would ideally need one value per lobe
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularOcclusion, DataBasedSpecularOcclusionIsCustom());
            context.AddBlock(HDBlockFields.SurfaceDescription.SOFixupVisibilityRatioThreshold, SpecularOcclusionUsesBentNormal(stackLitData) &&
                stackLitData.specularOcclusionConeFixupMethod != StackLitData.SpecularOcclusionConeFixupMethod.Off);
            context.AddBlock(HDBlockFields.SurfaceDescription.SOFixupStrengthFactor, SpecularOcclusionUsesBentNormal(stackLitData) &&
                stackLitData.specularOcclusionConeFixupMethod != StackLitData.SpecularOcclusionConeFixupMethod.Off);
            context.AddBlock(HDBlockFields.SurfaceDescription.SOFixupMaxAddedRoughness, SpecularOcclusionUsesBentNormal(stackLitData) && SpecularOcclusionConeFixupMethodModifiesRoughness(stackLitData.specularOcclusionConeFixupMethod) &&
                stackLitData.specularOcclusionConeFixupMethod != StackLitData.SpecularOcclusionConeFixupMethod.Off);

            // Coat
            context.AddBlock(BlockFields.SurfaceDescription.CoatSmoothness, stackLitData.coat);
            context.AddBlock(HDBlockFields.SurfaceDescription.CoatIor, stackLitData.coat);
            context.AddBlock(HDBlockFields.SurfaceDescription.CoatThickness, stackLitData.coat);
            context.AddBlock(HDBlockFields.SurfaceDescription.CoatExtinction, stackLitData.coat);
            context.AddBlock(HDBlockFields.SurfaceDescription.CoatNormalOS, stackLitData.coat && stackLitData.coatNormal && lightingData.normalDropOffSpace == NormalDropOffSpace.Object);
            context.AddBlock(HDBlockFields.SurfaceDescription.CoatNormalTS, stackLitData.coat && stackLitData.coatNormal && lightingData.normalDropOffSpace == NormalDropOffSpace.Tangent);
            context.AddBlock(HDBlockFields.SurfaceDescription.CoatNormalWS, stackLitData.coat && stackLitData.coatNormal && lightingData.normalDropOffSpace == NormalDropOffSpace.World);
            context.AddBlock(BlockFields.SurfaceDescription.CoatMask, stackLitData.coat);

            // Dual Specular Lobe
            context.AddBlock(HDBlockFields.SurfaceDescription.SmoothnessB, stackLitData.dualSpecularLobe && stackLitData.dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.Direct);
            context.AddBlock(HDBlockFields.SurfaceDescription.LobeMix, stackLitData.dualSpecularLobe && stackLitData.dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.Direct);

            context.AddBlock(HDBlockFields.SurfaceDescription.Haziness, stackLitData.dualSpecularLobe && stackLitData.dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.HazyGloss);
            context.AddBlock(HDBlockFields.SurfaceDescription.HazeExtent, stackLitData.dualSpecularLobe && stackLitData.dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.HazyGloss);
            context.AddBlock(HDBlockFields.SurfaceDescription.HazyGlossMaxDielectricF0, stackLitData.dualSpecularLobe && stackLitData.dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.HazyGloss &&
                stackLitData.capHazinessWrtMetallic && stackLitData.baseParametrization == StackLit.BaseParametrization.BaseMetallic);
            context.AddBlock(HDBlockFields.SurfaceDescription.AnisotropyB, stackLitData.dualSpecularLobe && stackLitData.anisotropy);

            // Iridescence
            context.AddBlock(HDBlockFields.SurfaceDescription.IridescenceMask, stackLitData.iridescence);
            context.AddBlock(HDBlockFields.SurfaceDescription.IridescenceThickness, stackLitData.iridescence);
            context.AddBlock(HDBlockFields.SurfaceDescription.IridescenceCoatFixupTIR, stackLitData.iridescence && stackLitData.coat);
            context.AddBlock(HDBlockFields.SurfaceDescription.IridescenceCoatFixupTIRClamp, stackLitData.iridescence && stackLitData.coat);
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new StackLitSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features.Lit, stackLitData));
            if (systemData.surfaceType == SurfaceType.Transparent)
                blockList.AddPropertyBlock(new DistortionPropertyBlock());
            blockList.AddPropertyBlock(new StackLitAdvancedOptionsPropertyBlock(stackLitData));
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);

            if (stackLitData.debug)
            {
                // We have useful debug options in StackLit, so add them always, and let the UI editor (non shadergraph) handle displaying them
                // since this is also the editor that controls the keyword switching for the debug mode.
                collector.AddShaderProperty(new Vector4ShaderProperty()
                {
                    overrideReferenceName = "_DebugEnvLobeMask", // xyz is environments lights lobe 0 1 2 Enable, w is Enable VLayering
                    displayName = "_DebugEnvLobeMask",
                    value = new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                });
                collector.AddShaderProperty(new Vector4ShaderProperty()
                {
                    overrideReferenceName = "_DebugLobeMask", // xyz is analytical dirac lights lobe 0 1 2 Enable", false),
                    displayName = "_DebugLobeMask",
                    value = new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                });
                collector.AddShaderProperty(new Vector4ShaderProperty()
                {
                    overrideReferenceName = "_DebugAniso", // x is Hack Enable, w is factor
                    displayName = "_DebugAniso",
                    value = new Vector4(1.0f, 0.0f, 0.0f, 1000.0f)
                });
                // _DebugSpecularOcclusion:
                //
                // eg (2,2,1,2) :
                // .x = SO method {0 = fromAO, 1 = conecone, 2 = SPTD},
                // .y = bentao algo {0 = uniform, cos, bent cos},
                // .z = use upper visible hemisphere clipping,
                // .w = The last component of _DebugSpecularOcclusion controls debug visualization:
                //      -1 colors the object according to the SO algorithm used,
                //      and values from 1 to 4 controls what the lighting debug display mode will show when set to show "indirect specular occlusion":
                //      Since there's not one value in our case,
                //      0 will show the object all red to indicate to choose one, 1-4 corresponds to showing
                //      1 = coat SO, 2 = base lobe A SO, 3 = base lobe B SO, 4 = shows the result of sampling the SSAO texture (screenSpaceAmbientOcclusion).
                collector.AddShaderProperty(new Vector4ShaderProperty()
                {
                    overrideReferenceName = "_DebugSpecularOcclusion",
                    displayName = "_DebugSpecularOcclusion",
                    value = new Vector4(2.0f, 2.0f, 1.0f, 2.0f)
                });
            }
        }

        public static bool SpecularOcclusionModeUsesVisibilityCone(StackLitData.SpecularOcclusionBaseMode soMethod)
        {
            return (soMethod == StackLitData.SpecularOcclusionBaseMode.ConeConeFromBentAO
                || soMethod == StackLitData.SpecularOcclusionBaseMode.SPTDIntegrationOfBentAO);
        }

        public static bool SpecularOcclusionUsesBentNormal(StackLitData stackLitData)
        {
            return (SpecularOcclusionModeUsesVisibilityCone(stackLitData.dataBasedSpecularOcclusionBaseMode)
                || (SpecularOcclusionModeUsesVisibilityCone(stackLitData.screenSpaceSpecularOcclusionBaseMode)
                    && stackLitData.screenSpaceSpecularOcclusionAOConeDir == StackLitData.SpecularOcclusionAOConeDir.BentNormal));
        }

        bool DataBasedSpecularOcclusionIsCustom()
        {
            return stackLitData.dataBasedSpecularOcclusionBaseMode == StackLitData.SpecularOcclusionBaseMode.Custom;
        }

        public static bool SpecularOcclusionConeFixupMethodModifiesRoughness(StackLitData.SpecularOcclusionConeFixupMethod soConeFixupMethod)
        {
            return (soConeFixupMethod == StackLitData.SpecularOcclusionConeFixupMethod.BoostBSDFRoughness
                || soConeFixupMethod == StackLitData.SpecularOcclusionConeFixupMethod.BoostAndTilt);
        }

        protected override int ComputeMaterialNeedsUpdateHash()
            => base.ComputeMaterialNeedsUpdateHash() * 23 + stackLitData.subsurfaceScattering.GetHashCode();
    }
}
