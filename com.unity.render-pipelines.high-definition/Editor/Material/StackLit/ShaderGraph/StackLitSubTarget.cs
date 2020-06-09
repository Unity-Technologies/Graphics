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
        const string kAssetGuid = "5f7ba34a143e67647b202a662748dae3";
        static string passTemplatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/StackLit/ShaderGraph/StackLitPass.template";
        protected override string customInspector => "Rendering.HighDefinition.StackLitGUI";
        protected override string subTargetAssetGuid => "5f7ba34a143e67647b202a662748dae3"; // StackLitSubTarget.cs
        protected override ShaderID shaderID => HDShaderUtils.ShaderID.SG_StackLit;

        public StackLitSubTarget() => displayName = "StackLit";

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

        protected override IEnumerable<SubShaderDescriptor> EnumerateSubShaders()
        {
            yield return SubShaders.StackLit;
            yield return SubShaders.StackLitRaytracing;
        }

        // Reference for GetFields
        // -------------------------------------------
        //
        // Properties (enables etc):
        //
        //  ok+MFD -> material feature define: means we need a predicate, because we will transform it into a #define that match the material feature, shader_feature-defined, that the rest of the shader code uses.
        //
        //  ok+MFD masterNode.baseParametrization    --> even though we can just always transfer present fields (check with $SurfaceDescription.*) like specularcolor and metallic,
        //                                               we need to translate this into the _MATERIAL_FEATURE_SPECULAR_COLOR define.
        //
        //  ok masterNode.energyConservingSpecular
        //
        //  ~~~~ ok+MFD: these are almost all material features:
        //  masterNode.anisotropy
        //  masterNode.coat
        //  masterNode.coatNormal
        //  masterNode.dualSpecularLobe
        //  masterNode.dualSpecularLobeParametrization
        //  masterNode.capHazinessWrtMetallic           -> not a material feature define, as such, we will create a combined predicate for the HazyGlossMaxDielectricF0 slot dependency
        //                                                 instead of adding a #define in the template...
        //  masterNode.iridescence
        //  masterNode.subsurfaceScattering
        //  masterNode.transmission
        //
        //  ~~~~ ...ok+MFD: these are all material features
        //
        //  ok masterNode.receiveDecals
        //  ok masterNode.receiveSSR
        //  ok masterNode.geometricSpecularAA    --> check, a way to combine predicates and/or exclude passes: TODOTODO What about WRITE_NORMAL_BUFFER passes ? (ie smoothness)
        //  ok masterNode.specularOcclusion      --> no use for it though! see comments.
        //
        //  ~~~~ ok+D: these require translation to defines also...
        //
        //  masterNode.anisotropyForAreaLights
        //  masterNode.recomputeStackPerLight
        //  masterNode.shadeBaseUsingRefractedAngles
        //  masterNode.debug

        // Inputs: Most inputs don't need a specific predicate in addition to the "present field predicate", ie the $SurfaceDescription.*,
        //         but in some special cases we check connectivity to avoid processing the default value for nothing...
        //         (see specular occlusion with _MASKMAP and _BENTNORMALMAP in LitData, or _TANGENTMAP, _BENTNORMALMAP, etc. which act a bit like that
        //         although they also avoid sampling in that case, but default tiny texture map sampling isn't a big hit since they are all cached once
        //         a default "unityTexWhite" is sampled, it is cached for everyone defaulting to white...)
        //
        // ok+ means there's a specific additional predicate
        //
        // ok masterNode.BaseColorSlotId
        // ok masterNode.NormalSlotId
        //
        // ok+ masterNode.BentNormalSlotId     --> Dependency of the predicate on IsSlotConnected avoids processing even if the slots
        // ok+ masterNode.TangentSlotId            are always there so any pass that declares its use in PixelShaderSlots will have the field in SurfaceDescription,
        //                                         but it's not necessarily useful (if slot isnt connected, waste processing on potentially static expressions if
        //                                         shader compiler cant optimize...and even then, useless to have static override value for those.)
        //
        //                                         TODOTODO: Note you could have the same argument for NormalSlot (which we dont exclude with a predicate).
        //                                         Also and anyways, the compiler is smart enough not to do the TS to WS matrix multiply on a (0,0,1) vector.
        //
        // ok+ masterNode.CoatNormalSlotId       -> we already have a "material feature" coat normal map so can use that instead, although using that former, we assume the coat normal slot
        //                                         will be there, but it's ok, we can #ifdef the code on the material feature define, and use the $SurfaceDescription.CoatNormal predicate
        //                                         for the actual assignment,
        //                                         although for that one we could again
        //                                         use the "connected" condition like for tangent and bentnormal
        //
        // The following are all ok, no need beyond present field predicate, ie $SurfaceDescription.*,
        // except special cases where noted
        //
        // ok masterNode.SubsurfaceMaskSlotId
        // ok masterNode.ThicknessSlotId
        // ok masterNode.DiffusionProfileHashSlotId
        // ok masterNode.IridescenceMaskSlotId
        // ok masterNode.IridescenceThicknessSlotId
        // ok masterNode.SpecularColorSlotId
        // ok masterNode.DielectricIorSlotId
        // ok masterNode.MetallicSlotId
        // ok masterNode.EmissionSlotId
        // ok masterNode.SmoothnessASlotId
        // ok masterNode.SmoothnessBSlotId
        // ok+ masterNode.AmbientOcclusionSlotId    -> defined a specific predicate, but not used, see StackLitData.
        // ok masterNode.AlphaSlotId
        // ok masterNode.AlphaClipThresholdSlotId
        // ok masterNode.AnisotropyASlotId
        // ok masterNode.AnisotropyBSlotId
        // ok masterNode.SpecularAAScreenSpaceVarianceSlotId
        // ok masterNode.SpecularAAThresholdSlotId
        // ok masterNode.CoatSmoothnessSlotId
        // ok masterNode.CoatIorSlotId
        // ok masterNode.CoatThicknessSlotId
        // ok masterNode.CoatExtinctionSlotId
        // ok masterNode.LobeMixSlotId
        // ok masterNode.HazinessSlotId
        // ok masterNode.HazeExtentSlotId
        // ok masterNode.HazyGlossMaxDielectricF0SlotId     -> No need for a predicate, the needed predicate is the combined (capHazinessWrtMetallic + HazyGlossMaxDielectricF0)
        //                                                     "leaking case": if the 2 are true, but we're not in metallic mode, the capHazinessWrtMetallic property is wrong,
        //                                                     that means the master node is really misconfigured, spew an error, should never happen...
        //                                                     If it happens, it's because we forgot UpdateNodeAfterDeserialization() call when modifying the capHazinessWrtMetallic or baseParametrization
        //                                                     properties, maybe through debug etc.
        //
        // ok masterNode.DistortionSlotId            -> Warning: peculiarly, instead of using $SurfaceDescription.Distortion and DistortionBlur,
        // ok masterNode.DistortionBlurSlotId           we do an #if (SHADERPASS == SHADERPASS_DISTORTION) in the template, instead of
        //                                              relying on other passed NOT to include the DistortionSlotId in their PixelShaderSlots!!

        // Other to deal with, and
        // Common between Lit and StackLit:
        //
        // doubleSidedMode, alphaTest, receiveDecals,
        // surfaceType, alphaMode, blendPreserveSpecular, transparencyFog,
        // distortion, distortionMode, distortionDepthTest,
        // sortPriority (int)
        // geometricSpecularAA, energyConservingSpecular, specularOcclusion

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);
            AddDistortionFields(ref context);
            var descs = context.blocks.Select(x => x.descriptor);

            // StackLit specific properties
            context.AddField(HDStructFields.FragInputs.IsFrontFace, systemData.doubleSidedMode != DoubleSidedMode.Disabled && !context.pass.Equals(StackLitSubTarget.StackLitPasses.MotionVectors));

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
            context.AddField(HDFields.SubsurfaceScattering,         lightingData.subsurfaceScattering && systemData.surfaceType != SurfaceType.Transparent);
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
            AddDistortionBlocks(ref context);

            // Common
            context.AddBlock(HDBlockFields.SurfaceDescription.BentNormal);
            context.AddBlock(HDBlockFields.SurfaceDescription.Tangent);
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness);
            context.AddBlock(BlockFields.SurfaceDescription.Occlusion);
            context.AddBlock(HDBlockFields.SurfaceDescription.Anisotropy,           stackLitData.anisotropy);
            context.AddBlock(HDBlockFields.SurfaceDescription.SubsurfaceMask,       lightingData.subsurfaceScattering);
            context.AddBlock(HDBlockFields.SurfaceDescription.Thickness,            stackLitData.transmission);
            context.AddBlock(HDBlockFields.SurfaceDescription.DiffusionProfileHash, lightingData.subsurfaceScattering || stackLitData.transmission);

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

            // Specular AA
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance, lightingData.specularAA);
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularAAThreshold,  lightingData.specularAA);
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

#region SubShaders
        static class SubShaders
        {
            public static SubShaderDescriptor StackLit = new SubShaderDescriptor()
            {
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
                generatesPreview = true,
                passes = new PassCollection
                {
                    { StackLitPasses.ShadowCaster },
                    { StackLitPasses.META },
                    { StackLitPasses.SceneSelection },
                    { StackLitPasses.DepthForwardOnly },
                    { StackLitPasses.MotionVectors },
                    { StackLitPasses.Distortion,                new FieldCondition(HDFields.TransparentDistortion, true) },
                    { StackLitPasses.TransparentDepthPrepass,   new FieldCondition[]{
                                                                new FieldCondition(HDFields.TransparentDepthPrePass, true),
                                                                new FieldCondition(HDFields.DisableSSRTransparent, true) }},
                    { StackLitPasses.TransparentDepthPrepass,   new FieldCondition[]{
                                                                new FieldCondition(HDFields.TransparentDepthPrePass, true),
                                                                new FieldCondition(HDFields.DisableSSRTransparent, false) }},
                    { StackLitPasses.TransparentDepthPrepass,   new FieldCondition[]{
                                                                new FieldCondition(HDFields.TransparentDepthPrePass, false),
                                                                new FieldCondition(HDFields.DisableSSRTransparent, false) }},
                    { StackLitPasses.ForwardOnly },
                    { StackLitPasses.TransparentDepthPostpass,  new FieldCondition(HDFields.TransparentDepthPostPass, true) },
                },
            };

            public static SubShaderDescriptor StackLitRaytracing = new SubShaderDescriptor()
            {
                pipelineTag = HDRenderPipeline.k_ShaderTagName,
                generatesPreview = false,
                passes = new PassCollection
                {
                    { StackLitPasses.RaytracingIndirect, new FieldCondition(Fields.IsPreview, false) },
                    { StackLitPasses.RaytracingVisibility, new FieldCondition(Fields.IsPreview, false) },
                    { StackLitPasses.RaytracingForward, new FieldCondition(Fields.IsPreview, false) },
                    { StackLitPasses.RaytracingGBuffer, new FieldCondition(Fields.IsPreview, false) },
                    { StackLitPasses.RaytracingSubSurface, new FieldCondition(Fields.IsPreview, false) },
                },
            };
        }
#endregion

#region Passes
        public static class StackLitPasses
        {
            public static PassDescriptor META = new PassDescriptor()
            {
                // Definition
                displayName = "META",
                referenceName = "SHADERPASS_LIGHT_TRANSPORT",
                lightMode = "META",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validPixelBlocks = StackLitBlockMasks.FragmentMETA,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.Meta,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.Meta,
                pragmas = StackLitPragmas.DotsInstancedInV2OnlyRenderingLayer,
                keywords = CoreKeywords.HDBase,
                includes = StackLitIncludes.Meta,
            };

            public static PassDescriptor ShadowCaster = new PassDescriptor()
            {
                // Definition
                displayName = "ShadowCaster",
                referenceName = "SHADERPASS_SHADOWS",
                lightMode = "ShadowCaster",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = StackLitBlockMasks.VertexPosition,
                validPixelBlocks = StackLitBlockMasks.FragmentAlphaDepth,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = StackLitRenderStates.ShadowCaster,
                pragmas = StackLitPragmas.DotsInstancedInV2OnlyRenderingLayer,
                keywords = CoreKeywords.HDBase,
                includes = StackLitIncludes.DepthOnly,
            };

            public static PassDescriptor SceneSelection = new PassDescriptor()
            {
                // Definition
                displayName = "SceneSelectionPass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "SceneSelectionPass",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = StackLitBlockMasks.FragmentAlphaDepth,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.SceneSelection,
                pragmas = StackLitPragmas.DotsInstancedInV2OnlyRenderingLayerEditorSync,
                defines = CoreDefines.SceneSelection,
                keywords = CoreKeywords.HDBase,
                includes = StackLitIncludes.DepthOnly,
            };

            public static PassDescriptor DepthForwardOnly = new PassDescriptor()
            {
                // // Code path for WRITE_NORMAL_BUFFER
                // See StackLit.hlsl:ConvertSurfaceDataToNormalData()
                // which ShaderPassDepthOnly uses: we need to add proper interpolators dependencies depending on WRITE_NORMAL_BUFFER.
                // In our case WRITE_NORMAL_BUFFER is always enabled here.
                // Also, we need to add PixelShaderSlots dependencies for everything potentially used there.
                // See AddPixelShaderSlotsForWriteNormalBufferPasses()

                // Definition
                displayName = "DepthForwardOnly",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "DepthForwardOnly",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = StackLitBlockMasks.FragmentDepthMotionVectors,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.DepthOnly,
                pragmas = StackLitPragmas.DotsInstancedInV2OnlyRenderingLayer,
                defines = CoreDefines.DepthMotionVectors,
                keywords = CoreKeywords.DepthMotionVectorsNoNormal,
                includes = StackLitIncludes.DepthOnly,
            };

            public static PassDescriptor MotionVectors = new PassDescriptor()
            {
                // Definition
                displayName = "MotionVectors",
                referenceName = "SHADERPASS_MOTION_VECTORS",
                lightMode = "MotionVectors",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = StackLitBlockMasks.FragmentDepthMotionVectors,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.MotionVectors,
                pragmas = StackLitPragmas.DotsInstancedInV2OnlyRenderingLayer,
                defines = CoreDefines.DepthMotionVectors,
                keywords = CoreKeywords.DepthMotionVectorsNoNormal,
                includes = StackLitIncludes.MotionVectors,
            };

            public static PassDescriptor Distortion = new PassDescriptor()
            {
                // Definition
                displayName = "DistortionVectors",
                referenceName = "SHADERPASS_DISTORTION",
                lightMode = "DistortionVectors",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = StackLitBlockMasks.FragmentDistortion,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = StackLitRenderStates.Distortion,
                pragmas = StackLitPragmas.DotsInstancedInV2OnlyRenderingLayer,
                keywords = CoreKeywords.HDBase,
                includes = StackLitIncludes.Distortion,
            };

            public static PassDescriptor TransparentDepthPrepass = new PassDescriptor()
            {
                // Definition
                displayName = "TransparentDepthPrepass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "TransparentDepthPrepass",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = StackLitBlockMasks.FragmentTransparentDepthPrepass,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.TransparentDepthPrePass,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.TransparentDepthPrepass,
                keywords = CoreKeywords.HDBase,
                includes = StackLitIncludes.DepthOnly,
            };

            public static PassDescriptor ForwardOnly = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardOnly",
                referenceName = "SHADERPASS_FORWARD",
                lightMode = "ForwardOnly",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = StackLitBlockMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                requiredFields = CoreRequiredFields.LitFull,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.Forward,
                pragmas = StackLitPragmas.DotsInstancedInV2OnlyRenderingLayer,
                defines = CoreDefines.Forward,
                keywords = CoreKeywords.Forward,
                includes = StackLitIncludes.ForwardOnly,
            };

            public static PassDescriptor TransparentDepthPostpass = new PassDescriptor()
            {
                // Definition
                displayName = "TransparentDepthPostpass",
                referenceName = "SHADERPASS_DEPTH_ONLY",
                lightMode = "TransparentDepthPostpass",
                useInPreview = true,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = StackLitBlockMasks.FragmentTransparentDepthPostpass,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                renderStates = CoreRenderStates.TransparentDepthPostPass,
                pragmas = CorePragmas.DotsInstancedInV2Only,
                defines = CoreDefines.ShaderGraphRaytracingHigh,
                keywords = CoreKeywords.HDBase,
                includes = StackLitIncludes.DepthOnly,
            };

            public static PassDescriptor RaytracingIndirect = new PassDescriptor()
            {
                // Definition
                displayName = "IndirectDXR",
                referenceName = "SHADERPASS_RAYTRACING_INDIRECT",
                lightMode = "IndirectDXR",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = StackLitBlockMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = StackLitDefines.RaytracingIndirect,
                keywords = CoreKeywords.RaytracingIndirect,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.StackLit, HDFields.ShaderPass.RaytracingIndirect },
            };

            public static PassDescriptor RaytracingVisibility = new PassDescriptor()
            {
                // Definition
                displayName = "VisibilityDXR",
                referenceName = "SHADERPASS_RAYTRACING_VISIBILITY",
                lightMode = "VisibilityDXR",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = StackLitBlockMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = StackLitDefines.RaytracingVisibility,
                keywords = CoreKeywords.RaytracingVisiblity,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.StackLit, HDFields.ShaderPass.RaytracingVisibility },
            };

            public static PassDescriptor RaytracingForward = new PassDescriptor()
            {
                // Definition
                displayName = "ForwardDXR",
                referenceName = "SHADERPASS_RAYTRACING_FORWARD",
                lightMode = "ForwardDXR",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = StackLitBlockMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = StackLitDefines.RaytracingForward,
                keywords = CoreKeywords.RaytracingGBufferForward,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.StackLit, HDFields.ShaderPass.RaytracingForward },
            };

            public static PassDescriptor RaytracingGBuffer = new PassDescriptor()
            {
                // Definition
                displayName = "GBufferDXR",
                referenceName = "SHADERPASS_RAYTRACING_GBUFFER",
                lightMode = "GBufferDXR",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                // Port Mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = StackLitBlockMasks.FragmentForward,

                // Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = StackLitDefines.RaytracingGBuffer,
                keywords = CoreKeywords.RaytracingGBufferForward,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.StackLit, HDFields.ShaderPass.RayTracingGBuffer },
            };

            public static PassDescriptor RaytracingSubSurface = new PassDescriptor()
            {
                //Definition
                displayName = "SubSurfaceDXR",
                referenceName = "SHADERPASS_RAYTRACING_SUB_SURFACE",
                lightMode = "SubSurfaceDXR",
                useInPreview = false,

                // Template
                passTemplatePath = passTemplatePath,
                sharedTemplateDirectory = HDTarget.sharedTemplateDirectory,

                //Port mask
                validVertexBlocks = CoreBlockMasks.Vertex,
                validPixelBlocks = StackLitBlockMasks.FragmentForward,

                //Collections
                structs = CoreStructCollections.Default,
                fieldDependencies = CoreFieldDependencies.Default,
                pragmas = CorePragmas.RaytracingBasic,
                defines = StackLitDefines.RaytracingGBuffer,
                keywords = CoreKeywords.RaytracingGBufferForward,
                includes = CoreIncludes.Raytracing,
                requiredFields = new FieldCollection(){ HDFields.SubShader.StackLit, HDFields.ShaderPass.RaytracingSubSurface },
            };
        }
#endregion

#region PortMasks
        static class StackLitBlockMasks
        {
            public static BlockFieldDescriptor[] VertexPosition = new BlockFieldDescriptor[]
            {
                BlockFields.VertexDescription.Position,
            };

            public static BlockFieldDescriptor[] FragmentMETA = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalWS,
                BlockFields.SurfaceDescription.NormalOS,
                HDBlockFields.SurfaceDescription.BentNormal,
                HDBlockFields.SurfaceDescription.Tangent,
                HDBlockFields.SurfaceDescription.SubsurfaceMask,
                HDBlockFields.SurfaceDescription.Thickness,
                HDBlockFields.SurfaceDescription.DiffusionProfileHash,
                HDBlockFields.SurfaceDescription.IridescenceMask,
                HDBlockFields.SurfaceDescription.IridescenceThickness,
                HDBlockFields.SurfaceDescription.IridescenceCoatFixupTIR,
                HDBlockFields.SurfaceDescription.IridescenceCoatFixupTIRClamp,
                BlockFields.SurfaceDescription.Specular,
                HDBlockFields.SurfaceDescription.DielectricIor,
                BlockFields.SurfaceDescription.Metallic,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Smoothness,
                HDBlockFields.SurfaceDescription.SmoothnessB,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.Anisotropy,
                HDBlockFields.SurfaceDescription.AnisotropyB,
                HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance,
                HDBlockFields.SurfaceDescription.SpecularAAThreshold,
                HDBlockFields.SurfaceDescription.CoatSmoothness,
                HDBlockFields.SurfaceDescription.CoatIor,
                HDBlockFields.SurfaceDescription.CoatThickness,
                HDBlockFields.SurfaceDescription.CoatExtinction,
                HDBlockFields.SurfaceDescription.CoatNormal,
                HDBlockFields.SurfaceDescription.CoatMask,
                HDBlockFields.SurfaceDescription.LobeMix,
                HDBlockFields.SurfaceDescription.Haziness,
                HDBlockFields.SurfaceDescription.HazeExtent,
                HDBlockFields.SurfaceDescription.HazyGlossMaxDielectricF0,
                HDBlockFields.SurfaceDescription.SpecularOcclusion,
                HDBlockFields.SurfaceDescription.SOFixupVisibilityRatioThreshold,
                HDBlockFields.SurfaceDescription.SOFixupStrengthFactor,
                HDBlockFields.SurfaceDescription.SOFixupMaxAddedRoughness,
            };

            public static BlockFieldDescriptor[] FragmentAlphaDepth = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };

            public static BlockFieldDescriptor[] FragmentDepthMotionVectors = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.DepthOffset,
                // StackLitMasterNode.coat
                HDBlockFields.SurfaceDescription.CoatSmoothness,
                HDBlockFields.SurfaceDescription.CoatNormal,
                // !StackLitMasterNode.coat
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalWS,
                BlockFields.SurfaceDescription.NormalOS,
                HDBlockFields.SurfaceDescription.LobeMix,
                BlockFields.SurfaceDescription.Smoothness,
                HDBlockFields.SurfaceDescription.SmoothnessB,
                // StackLitMasterNode.geometricSpecularAA
                HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance,
                HDBlockFields.SurfaceDescription.SpecularAAThreshold,
            };

            public static BlockFieldDescriptor[] FragmentDistortion = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.Distortion,
                HDBlockFields.SurfaceDescription.DistortionBlur,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };

            public static BlockFieldDescriptor[] FragmentTransparentDepthPrepass = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPrepass,
                HDBlockFields.SurfaceDescription.DepthOffset,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalWS,
                BlockFields.SurfaceDescription.NormalOS,
                BlockFields.SurfaceDescription.Smoothness,
            };

            public static BlockFieldDescriptor[] FragmentForward = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.NormalTS,
                BlockFields.SurfaceDescription.NormalWS,
                BlockFields.SurfaceDescription.NormalOS,
                HDBlockFields.SurfaceDescription.BentNormal,
                HDBlockFields.SurfaceDescription.Tangent,
                HDBlockFields.SurfaceDescription.SubsurfaceMask,
                HDBlockFields.SurfaceDescription.Thickness,
                HDBlockFields.SurfaceDescription.DiffusionProfileHash,
                HDBlockFields.SurfaceDescription.IridescenceMask,
                HDBlockFields.SurfaceDescription.IridescenceThickness,
                HDBlockFields.SurfaceDescription.IridescenceCoatFixupTIR,
                HDBlockFields.SurfaceDescription.IridescenceCoatFixupTIRClamp,
                BlockFields.SurfaceDescription.Specular,
                HDBlockFields.SurfaceDescription.DielectricIor,
                BlockFields.SurfaceDescription.Metallic,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Smoothness,
                HDBlockFields.SurfaceDescription.SmoothnessB,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.AlphaClipThreshold,
                HDBlockFields.SurfaceDescription.Anisotropy,
                HDBlockFields.SurfaceDescription.AnisotropyB,
                HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance,
                HDBlockFields.SurfaceDescription.SpecularAAThreshold,
                HDBlockFields.SurfaceDescription.CoatSmoothness,
                HDBlockFields.SurfaceDescription.CoatIor,
                HDBlockFields.SurfaceDescription.CoatThickness,
                HDBlockFields.SurfaceDescription.CoatExtinction,
                HDBlockFields.SurfaceDescription.CoatNormal,
                HDBlockFields.SurfaceDescription.CoatMask,
                HDBlockFields.SurfaceDescription.LobeMix,
                HDBlockFields.SurfaceDescription.Haziness,
                HDBlockFields.SurfaceDescription.HazeExtent,
                HDBlockFields.SurfaceDescription.HazyGlossMaxDielectricF0,
                HDBlockFields.SurfaceDescription.SpecularOcclusion,
                HDBlockFields.SurfaceDescription.SOFixupVisibilityRatioThreshold,
                HDBlockFields.SurfaceDescription.SOFixupStrengthFactor,
                HDBlockFields.SurfaceDescription.SOFixupMaxAddedRoughness,
                HDBlockFields.SurfaceDescription.BakedGI,
                HDBlockFields.SurfaceDescription.BakedBackGI,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };

            public static BlockFieldDescriptor[] FragmentTransparentDepthPostpass = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                HDBlockFields.SurfaceDescription.AlphaClipThresholdDepthPostpass,
                HDBlockFields.SurfaceDescription.DepthOffset,
            };
        }
#endregion

#region RenderStates
        static class StackLitRenderStates
        {
            public static RenderStateCollection ShadowCaster = new RenderStateCollection
            {
                { RenderState.Blend(Blend.One, Blend.Zero) },
                { RenderState.ZWrite(ZWrite.On) },
                { RenderState.ZClip(CoreRenderStates.Uniforms.zClip) },
                { RenderState.ColorMask("ColorMask 0") },
            };

            public static RenderStateCollection Distortion = new RenderStateCollection
            {
                { RenderState.Blend(Blend.One, Blend.One, Blend.One, Blend.One), new FieldCondition(HDFields.DistortionAdd, true) },
                { RenderState.Blend(Blend.DstColor, Blend.Zero, Blend.DstAlpha, Blend.Zero), new FieldCondition(HDFields.DistortionMultiply, true) },
                { RenderState.Blend(Blend.One, Blend.Zero, Blend.One, Blend.Zero), new FieldCondition(HDFields.DistortionReplace, true) },
                { RenderState.BlendOp(BlendOp.Add, BlendOp.Add) },
                { RenderState.Cull(CoreRenderStates.Uniforms.cullMode) },
                { RenderState.ZWrite(ZWrite.Off) },
                { RenderState.ZTest(ZTest.Always), new FieldCondition(HDFields.DistortionDepthTest, false) },
                { RenderState.ZTest(ZTest.LEqual), new FieldCondition(HDFields.DistortionDepthTest, true) },
                { RenderState.Stencil(new StencilDescriptor()
                {
                    WriteMask = $"{(int)StencilUsage.DistortionVectors}",
                    Ref = $"{(int)StencilUsage.DistortionVectors}",
                    Comp = "Always",
                    Pass = "Replace",
                }) },
            };
        }
#endregion

#region Defines
        static class StackLitDefines
        {
            public static DefineCollection RaytracingForward = new DefineCollection
            {
                { CoreKeywordDescriptors.Shadow, 0 },
                { RayTracingNode.GetRayTracingKeyword(), 0 },
                { CoreKeywordDescriptors.HasLightloop, 1 },
            };

            public static DefineCollection RaytracingIndirect = new DefineCollection
            {
                { CoreKeywordDescriptors.Shadow, 0 },
                { RayTracingNode.GetRayTracingKeyword(), 1 },
                { CoreKeywordDescriptors.HasLightloop, 1 },
            };

            public static DefineCollection RaytracingGBuffer = new DefineCollection
            {
                { CoreKeywordDescriptors.Shadow, 0 },
                { RayTracingNode.GetRayTracingKeyword(), 1 },
            };

            public static DefineCollection RaytracingVisibility = new DefineCollection
            {
                { RayTracingNode.GetRayTracingKeyword(), 1 },
            };

            public static DefineCollection RaytracingPathTracing = new DefineCollection
            {
                { CoreKeywordDescriptors.Shadow, 0 },
                { RayTracingNode.GetRayTracingKeyword(), 0 },
                { CoreKeywordDescriptors.HasLightloop, 1 },
            };
        }
#endregion

#region Pragmas
        static class StackLitPragmas
        {
            public static PragmaCollection DotsInstancedInV2OnlyRenderingLayer = new PragmaCollection
            {
                { CorePragmas.Basic },
                { Pragma.MultiCompileInstancing },
                { Pragma.InstancingOptions(InstancingOptions.RenderingLayer) },
                #if ENABLE_HYBRID_RENDERER_V2
                { Pragma.DOTSInstancing },
                { Pragma.InstancingOptions(InstancingOptions.NoLodFade) },
                #endif
            };

            public static PragmaCollection DotsInstancedInV2OnlyRenderingLayerEditorSync = new PragmaCollection
            {
                { CorePragmas.Basic },
                { Pragma.MultiCompileInstancing },
                { Pragma.InstancingOptions(InstancingOptions.RenderingLayer) },
                { Pragma.EditorSyncCompilation },
                #if ENABLE_HYBRID_RENDERER_V2
                { Pragma.DOTSInstancing },
                { Pragma.InstancingOptions(InstancingOptions.NoLodFade) },
                #endif
            };
        }
#endregion

#region Includes
        static class StackLitIncludes
        {
            const string kSpecularOcclusionDef = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SphericalCapPivot/SpecularOcclusionDef.hlsl";
            const string kStackLitDecalData = "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLitDecalData.hlsl";

            public static IncludeCollection Common = new IncludeCollection
            {
                { kSpecularOcclusionDef, IncludeLocation.Pregraph },
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
                { CoreIncludes.kStackLit, IncludeLocation.Pregraph },
                { CoreIncludes.CoreUtility },
                { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
                { kStackLitDecalData, IncludeLocation.Pregraph },
                { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
            };

            public static IncludeCollection Meta = new IncludeCollection
            {
                { Common },
                { CoreIncludes.kPassLightTransport, IncludeLocation.Postgraph },
            };

            public static IncludeCollection DepthOnly = new IncludeCollection
            {
                { Common },
                { CoreIncludes.kPassDepthOnly, IncludeLocation.Postgraph },
            };

            public static IncludeCollection MotionVectors = new IncludeCollection
            {
                { Common },
                { CoreIncludes.kPassMotionVectors, IncludeLocation.Postgraph },
            };

            public static IncludeCollection Distortion = new IncludeCollection
            {
                { Common },
                { CoreIncludes.kDisortionVectors, IncludeLocation.Postgraph },
            };

            public static IncludeCollection ForwardOnly = new IncludeCollection
            {
                { kSpecularOcclusionDef, IncludeLocation.Pregraph },
                { CoreIncludes.CorePregraph },
                { CoreIncludes.kNormalSurfaceGradient, IncludeLocation.Pregraph },
                { CoreIncludes.kLighting, IncludeLocation.Pregraph },
                { CoreIncludes.kLightLoopDef, IncludeLocation.Pregraph },
                { CoreIncludes.kStackLit, IncludeLocation.Pregraph },
                { CoreIncludes.kLightLoop, IncludeLocation.Pregraph },
                { CoreIncludes.CoreUtility },
                { CoreIncludes.kDecalUtilities, IncludeLocation.Pregraph },
                { kStackLitDecalData, IncludeLocation.Pregraph },
                { CoreIncludes.kShaderGraphFunctions, IncludeLocation.Pregraph },
                { CoreIncludes.kPassForward, IncludeLocation.Postgraph },
            };
        }
#endregion
    }
}
