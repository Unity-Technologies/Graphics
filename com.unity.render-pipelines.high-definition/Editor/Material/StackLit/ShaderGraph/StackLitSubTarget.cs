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
    //TODO:
    // clamp in shader code the ranged() properties
    // or let inputs (eg mask?) follow invalid values ? Lit does that (let them free running).
    sealed partial class StackLitSubTarget : LightingSubTarget, ILegacyTarget, IRequiresData<StackLitData>
    {
        public StackLitSubTarget() => displayName = "StackLit";

        protected override string templatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/StackLit/ShaderGraph/StackLitPass.template";
        protected override string customInspector => "Rendering.HighDefinition.StackLitGUI";
        protected override string subTargetAssetGuid => "5f7ba34a143e67647b202a662748dae3"; // StackLitSubTarget.cs
        protected override ShaderID shaderID => HDShaderUtils.ShaderID.SG_StackLit;
        protected override FieldDescriptor subShaderField => HDFields.SubShader.StackLit;
        protected override string postDecalsInclude => "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLitDecalData.hlsl";
        protected override string subShaderInclude => CoreIncludes.kStackLit;

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
            context.AddField(HDFields.Anisotropy,                   stackLitData.anisotropy);
            context.AddField(HDFields.Coat,                         stackLitData.coat);
            context.AddField(HDFields.CoatMask,                     stackLitData.coat && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.CoatMask) &&
                                                                        descs.Contains(HDBlockFields.SurfaceDescription.CoatMask));
            // context.AddField(HDFields.CoatMaskZero,                 coat.isOn && pass.pixelBlocks.Contains(CoatMaskSlotId) &&
            //                                                                 FindSlot<Vector1MaterialSlot>(CoatMaskSlotId).value == 0.0f),
            // context.AddField(HDFields.CoatMaskOne,                  coat.isOn && pass.pixelBlocks.Contains(CoatMaskSlotId) &&
            //                                                                 FindSlot<Vector1MaterialSlot>(CoatMaskSlotId).value == 1.0f),
            context.AddField(HDFields.CoatNormal,                   stackLitData.coatNormal && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.CoatNormal));
            context.AddField(HDFields.Iridescence,                  stackLitData.iridescence);
            context.AddField(HDFields.SubsurfaceScattering,         stackLitData.subsurfaceScattering && systemData.surfaceType != SurfaceType.Transparent);
            context.AddField(HDFields.Transmission,                 stackLitData.transmission);
            context.AddField(HDFields.DualSpecularLobe,             stackLitData.dualSpecularLobe);

            // Base Parametrization
            // Even though we can just always transfer the present (check with $SurfaceDescription.*) fields like specularcolor
            // and metallic, we still need to know the baseParametrization in the template to translate into the
            // _MATERIAL_FEATURE_SPECULAR_COLOR define:
            context.AddField(HDFields.BaseParamSpecularColor,       stackLitData.baseParametrization == StackLit.BaseParametrization.SpecularColor);

            // Dual Specular Lobe Parametrization
            context.AddField(HDFields.HazyGloss,                    stackLitData.dualSpecularLobe &&
                                                                            stackLitData.dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.HazyGloss);

            // Misc
            context.AddField(HDFields.DoAlphaTest,                  systemData.alphaTest && context.pass.validPixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold));
            context.AddField(HDFields.EnergyConservingSpecular,     stackLitData.energyConservingSpecular);
            context.AddField(HDFields.Tangent,                      descs.Contains(HDBlockFields.SurfaceDescription.Tangent) &&
                                                                            context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.Tangent));
            // Option for baseParametrization == Metallic && DualSpecularLobeParametrization == HazyGloss:
            // Again we assume masternode has HazyGlossMaxDielectricF0 which should always be the case
            // if capHazinessWrtMetallic.isOn.
            context.AddField(HDFields.CapHazinessIfNotMetallic,     stackLitData.dualSpecularLobe &&
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
            context.AddField(HDFields.GeometricSpecularAA,          lightingData.specularAA &&
                                                                            context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance) &&
                                                                            context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAThreshold));
            context.AddField(HDFields.SpecularAA,                   lightingData.specularAA &&
                                                                            context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance) &&
                                                                            context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAThreshold));
            context.AddField(HDFields.SpecularOcclusion,            stackLitData.screenSpaceSpecularOcclusionBaseMode != StackLitData.SpecularOcclusionBaseMode.Off ||
                                                                            stackLitData.dataBasedSpecularOcclusionBaseMode != StackLitData.SpecularOcclusionBaseMode.Off);

            // Advanced
            context.AddField(HDFields.AnisotropyForAreaLights,      stackLitData.anisotropyForAreaLights);
            context.AddField(HDFields.RecomputeStackPerLight,       stackLitData.recomputeStackPerLight);
            context.AddField(HDFields.HonorPerLightMinRoughness,    stackLitData.honorPerLightMinRoughness);
            context.AddField(HDFields.ShadeBaseUsingRefractedAngles, stackLitData.shadeBaseUsingRefractedAngles);
            context.AddField(HDFields.StackLitDebug,                stackLitData.debug);

            // Screen Space Specular Occlusion Base Mode
            context.AddField(HDFields.SSSpecularOcclusionBaseModeOff, stackLitData.screenSpaceSpecularOcclusionBaseMode == StackLitData.SpecularOcclusionBaseMode.Off);
            context.AddField(HDFields.SSSpecularOcclusionBaseModeDirectFromAO, stackLitData.screenSpaceSpecularOcclusionBaseMode == StackLitData.SpecularOcclusionBaseMode.DirectFromAO);
            context.AddField(HDFields.SSSpecularOcclusionBaseModeConeConeFromBentAO, stackLitData.screenSpaceSpecularOcclusionBaseMode == StackLitData.SpecularOcclusionBaseMode.ConeConeFromBentAO);
            context.AddField(HDFields.SSSpecularOcclusionBaseModeSPTDIntegrationOfBentAO, stackLitData.screenSpaceSpecularOcclusionBaseMode == StackLitData.SpecularOcclusionBaseMode.SPTDIntegrationOfBentAO);
            context.AddField(HDFields.SSSpecularOcclusionBaseModeCustom, stackLitData.screenSpaceSpecularOcclusionBaseMode == StackLitData.SpecularOcclusionBaseMode.Custom);

            // Screen Space Specular Occlusion AO Cone Size
            context.AddField(HDFields.SSSpecularOcclusionAOConeSizeUniformAO, SpecularOcclusionModeUsesVisibilityCone(stackLitData.screenSpaceSpecularOcclusionBaseMode) &&
                                                                            stackLitData.screenSpaceSpecularOcclusionAOConeSize == StackLitData.SpecularOcclusionAOConeSize.UniformAO);
            context.AddField(HDFields.SSSpecularOcclusionAOConeSizeCosWeightedAO, SpecularOcclusionModeUsesVisibilityCone(stackLitData.screenSpaceSpecularOcclusionBaseMode) &&
                                                                            stackLitData.screenSpaceSpecularOcclusionAOConeSize == StackLitData.SpecularOcclusionAOConeSize.CosWeightedAO);
            context.AddField(HDFields.SSSpecularOcclusionAOConeSizeCosWeightedBentCorrectAO, SpecularOcclusionModeUsesVisibilityCone(stackLitData.screenSpaceSpecularOcclusionBaseMode) &&
                                                                            stackLitData.screenSpaceSpecularOcclusionAOConeSize == StackLitData.SpecularOcclusionAOConeSize.CosWeightedBentCorrectAO);

            // Screen Space Specular Occlusion AO Cone Dir
            context.AddField(HDFields.SSSpecularOcclusionAOConeDirGeomNormal, SpecularOcclusionModeUsesVisibilityCone(stackLitData.screenSpaceSpecularOcclusionBaseMode) &&
                                                                            stackLitData.screenSpaceSpecularOcclusionAOConeDir == StackLitData.SpecularOcclusionAOConeDir.GeomNormal);
            context.AddField(HDFields.SSSpecularOcclusionAOConeDirBentNormal, SpecularOcclusionModeUsesVisibilityCone(stackLitData.screenSpaceSpecularOcclusionBaseMode) &&
                                                                            stackLitData.screenSpaceSpecularOcclusionAOConeDir == StackLitData.SpecularOcclusionAOConeDir.BentNormal);
            context.AddField(HDFields.SSSpecularOcclusionAOConeDirShadingNormal, SpecularOcclusionModeUsesVisibilityCone(stackLitData.screenSpaceSpecularOcclusionBaseMode) &&
                                                                            stackLitData.screenSpaceSpecularOcclusionAOConeDir == StackLitData.SpecularOcclusionAOConeDir.ShadingNormal);

            // Data Based Specular Occlusion Base Mode
            context.AddField(HDFields.DataBasedSpecularOcclusionBaseModeOff, stackLitData.dataBasedSpecularOcclusionBaseMode == StackLitData.SpecularOcclusionBaseMode.Off);
            context.AddField(HDFields.DataBasedSpecularOcclusionBaseModeDirectFromAO, stackLitData.dataBasedSpecularOcclusionBaseMode == StackLitData.SpecularOcclusionBaseMode.DirectFromAO);
            context.AddField(HDFields.DataBasedSpecularOcclusionBaseModeConeConeFromBentAO, stackLitData.dataBasedSpecularOcclusionBaseMode == StackLitData.SpecularOcclusionBaseMode.ConeConeFromBentAO);
            context.AddField(HDFields.DataBasedSpecularOcclusionBaseModeSPTDIntegrationOfBentAO, stackLitData.dataBasedSpecularOcclusionBaseMode == StackLitData.SpecularOcclusionBaseMode.SPTDIntegrationOfBentAO);
            context.AddField(HDFields.DataBasedSpecularOcclusionBaseModeCustom, stackLitData.dataBasedSpecularOcclusionBaseMode == StackLitData.SpecularOcclusionBaseMode.Custom);

            // Data Based Specular Occlusion AO Cone Size
            context.AddField(HDFields.DataBasedSpecularOcclusionAOConeSizeUniformAO, SpecularOcclusionModeUsesVisibilityCone(stackLitData.dataBasedSpecularOcclusionBaseMode) &&
                                                                            stackLitData.dataBasedSpecularOcclusionAOConeSize == StackLitData.SpecularOcclusionAOConeSize.UniformAO);
            context.AddField(HDFields.DataBasedSpecularOcclusionAOConeSizeCosWeightedAO, SpecularOcclusionModeUsesVisibilityCone(stackLitData.dataBasedSpecularOcclusionBaseMode) &&
                                                                            stackLitData.dataBasedSpecularOcclusionAOConeSize == StackLitData.SpecularOcclusionAOConeSize.CosWeightedAO);
            context.AddField(HDFields.DataBasedSpecularOcclusionAOConeSizeCosWeightedBentCorrectAO, SpecularOcclusionModeUsesVisibilityCone(stackLitData.dataBasedSpecularOcclusionBaseMode) &&
                                                                            stackLitData.dataBasedSpecularOcclusionAOConeSize == StackLitData.SpecularOcclusionAOConeSize.CosWeightedBentCorrectAO);

            // Specular Occlusion Cone Fixup Method
            context.AddField(HDFields.SpecularOcclusionConeFixupMethodOff, SpecularOcclusionUsesBentNormal(stackLitData) &&
                                                                            stackLitData.specularOcclusionConeFixupMethod == StackLitData.SpecularOcclusionConeFixupMethod.Off);
            context.AddField(HDFields.SpecularOcclusionConeFixupMethodBoostBSDFRoughness, SpecularOcclusionUsesBentNormal(stackLitData) &&
                                                                            stackLitData.specularOcclusionConeFixupMethod == StackLitData.SpecularOcclusionConeFixupMethod.BoostBSDFRoughness);
            context.AddField(HDFields.SpecularOcclusionConeFixupMethodTiltDirectionToGeomNormal, SpecularOcclusionUsesBentNormal(stackLitData) &&
                                                                            stackLitData.specularOcclusionConeFixupMethod == StackLitData.SpecularOcclusionConeFixupMethod.TiltDirectionToGeomNormal);
            context.AddField(HDFields.SpecularOcclusionConeFixupMethodBoostAndTilt, SpecularOcclusionUsesBentNormal(stackLitData) &&
                                                                            stackLitData.specularOcclusionConeFixupMethod == StackLitData.SpecularOcclusionConeFixupMethod.BoostAndTilt);
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            base.GetActiveBlocks(ref context);

            // Common
            context.AddBlock(HDBlockFields.SurfaceDescription.Tangent);
            context.AddBlock(HDBlockFields.SurfaceDescription.Anisotropy,           stackLitData.anisotropy);
            context.AddBlock(HDBlockFields.SurfaceDescription.SubsurfaceMask,       stackLitData.subsurfaceScattering);
            context.AddBlock(HDBlockFields.SurfaceDescription.Thickness,            stackLitData.transmission);
            context.AddBlock(HDBlockFields.SurfaceDescription.DiffusionProfileHash, stackLitData.subsurfaceScattering || stackLitData.transmission);

            // Base Metallic
            context.AddBlock(BlockFields.SurfaceDescription.Metallic,               stackLitData.baseParametrization == StackLit.BaseParametrization.BaseMetallic);
            context.AddBlock(HDBlockFields.SurfaceDescription.DielectricIor,        stackLitData.baseParametrization == StackLit.BaseParametrization.BaseMetallic);

            // Base Specular
            context.AddBlock(BlockFields.SurfaceDescription.Specular,               stackLitData.baseParametrization == StackLit.BaseParametrization.SpecularColor);

            // Specular Occlusion
            // for custom (external) SO replacing data based SO (which normally comes from some func of DataBasedSOMode(dataAO, optional bent normal))
            // TODO: we would ideally need one value per lobe
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularOcclusion,    DataBasedSpecularOcclusionIsCustom());
            context.AddBlock(HDBlockFields.SurfaceDescription.SOFixupVisibilityRatioThreshold, SpecularOcclusionUsesBentNormal(stackLitData) && 
                                                                                        stackLitData.specularOcclusionConeFixupMethod != StackLitData.SpecularOcclusionConeFixupMethod.Off);
            context.AddBlock(HDBlockFields.SurfaceDescription.SOFixupStrengthFactor, SpecularOcclusionUsesBentNormal(stackLitData) && 
                                                                                        stackLitData.specularOcclusionConeFixupMethod != StackLitData.SpecularOcclusionConeFixupMethod.Off);
            context.AddBlock(HDBlockFields.SurfaceDescription.SOFixupMaxAddedRoughness, SpecularOcclusionUsesBentNormal(stackLitData) && SpecularOcclusionConeFixupMethodModifiesRoughness(stackLitData.specularOcclusionConeFixupMethod) &&
                                                                                        stackLitData.specularOcclusionConeFixupMethod != StackLitData.SpecularOcclusionConeFixupMethod.Off);

            // Coat
            context.AddBlock(HDBlockFields.SurfaceDescription.CoatSmoothness,       stackLitData.coat);
            context.AddBlock(HDBlockFields.SurfaceDescription.CoatIor,              stackLitData.coat);
            context.AddBlock(HDBlockFields.SurfaceDescription.CoatThickness,        stackLitData.coat);
            context.AddBlock(HDBlockFields.SurfaceDescription.CoatExtinction,       stackLitData.coat);
            context.AddBlock(HDBlockFields.SurfaceDescription.CoatNormal,           stackLitData.coat && stackLitData.coatNormal);
            context.AddBlock(HDBlockFields.SurfaceDescription.CoatMask,             stackLitData.coat);

            // Dual Specular Lobe
            context.AddBlock(HDBlockFields.SurfaceDescription.SmoothnessB,          stackLitData.dualSpecularLobe && stackLitData.dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.Direct);
            context.AddBlock(HDBlockFields.SurfaceDescription.LobeMix,              stackLitData.dualSpecularLobe && stackLitData.dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.Direct);

            context.AddBlock(HDBlockFields.SurfaceDescription.Haziness,             stackLitData.dualSpecularLobe && stackLitData.dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.HazyGloss);
            context.AddBlock(HDBlockFields.SurfaceDescription.HazeExtent,           stackLitData.dualSpecularLobe && stackLitData.dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.HazyGloss);
            context.AddBlock(HDBlockFields.SurfaceDescription.HazyGlossMaxDielectricF0, stackLitData.dualSpecularLobe && stackLitData.dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.HazyGloss &&
                                                                                        stackLitData.capHazinessWrtMetallic && stackLitData.baseParametrization == StackLit.BaseParametrization.BaseMetallic);
            context.AddBlock(HDBlockFields.SurfaceDescription.AnisotropyB,          stackLitData.dualSpecularLobe && stackLitData.anisotropy);

            // Iridescence
            context.AddBlock(HDBlockFields.SurfaceDescription.IridescenceMask,      stackLitData.iridescence);
            context.AddBlock(HDBlockFields.SurfaceDescription.IridescenceThickness, stackLitData.iridescence);
            context.AddBlock(HDBlockFields.SurfaceDescription.IridescenceCoatFixupTIR, stackLitData.iridescence && stackLitData.coat);
            context.AddBlock(HDBlockFields.SurfaceDescription.IridescenceCoatFixupTIRClamp, stackLitData.iridescence && stackLitData.coat);
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new StackLitSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features.Lit, stackLitData));
            if (systemData.surfaceType == SurfaceType.Transparent)
                blockList.AddPropertyBlock(new DistortionPropertyBlock());
            blockList.AddPropertyBlock(new AdvancedOptionsPropertyBlock());
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
