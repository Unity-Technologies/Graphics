using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class StackLitSettingsView
    {
        HDSystemData systemData;
        HDBuiltinData builtinData;
        HDLightingData lightingData;
        StackLitData stackLitData;

        IntegerField m_SortPriorityField;

        public StackLitSettingsView(HDStackLitSubTarget subTarget)
        {
            systemData = subTarget.systemData;
            builtinData = subTarget.builtinData;
            lightingData = subTarget.lightingData;
            stackLitData = subTarget.stackLitData;
        }

        public void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange)
        {
            // Render State
            DoRenderStateArea(ref context, 0, onChange);

            // Distortion
            if(systemData.surfaceType == SurfaceType.Transparent)
            {
                DoDistortionArea(ref context, 1, onChange);
            }

            // Alpha Test
            // TODO: AlphaTest is in SystemData but Alpha to Mask is in BuiltinData?
            context.AddProperty("Alpha Clipping", 0, new Toggle() { value = systemData.alphaTest }, (evt) =>
            {
                if (Equals(systemData.alphaTest, evt.newValue))
                    return;

                systemData.alphaTest = evt.newValue;
                onChange();
            });
            context.AddProperty("Alpha to Mask", 1, new Toggle() { value = builtinData.alphaToMask }, systemData.alphaTest, (evt) =>
            {
                if (Equals(builtinData.alphaToMask, evt.newValue))
                    return;

                builtinData.alphaToMask = evt.newValue;
                onChange();
            });

            // Misc
            context.AddProperty("Double-Sided Mode", 0, new EnumField(DoubleSidedMode.Disabled) { value = systemData.doubleSidedMode }, (evt) =>
            {
                if (Equals(systemData.doubleSidedMode, evt.newValue))
                    return;

                systemData.doubleSidedMode = (DoubleSidedMode)evt.newValue;
                onChange();
            });
            context.AddProperty("Fragment Normal Space", 0, new EnumField(NormalDropOffSpace.Tangent) { value = lightingData.normalDropOffSpace }, (evt) =>
            {
                if (Equals(lightingData.normalDropOffSpace, evt.newValue))
                    return;

                lightingData.normalDropOffSpace = (NormalDropOffSpace)evt.newValue;
                onChange();
            });

            // Rest of UI looks like this:
            //
            //  baseParametrization
            //    energyConservingSpecular
            //
            //  anisotropy
            //  coat
            //  coatNormal
            //  dualSpecularLobe
            //    dualSpecularLobeParametrization
            //    capHazinessWrtMetallic
            //  iridescence
            //  subsurfaceScattering
            //  transmission
            //
            //  receiveDecals
            //  receiveSSR
            //  addPrecomputedVelocity
            //  geometricSpecularAA
            //  specularOcclusion
            //
            //  anisotropyForAreaLights
            //  recomputeStackPerLight
            //  shadeBaseUsingRefractedAngles

            // Base parametrization:
            context.AddProperty("Base Color Parametrization", 0, new EnumField(StackLit.BaseParametrization.BaseMetallic) { value = stackLitData.baseParametrization }, (evt) =>
            {
                if (Equals(stackLitData.baseParametrization, evt.newValue))
                    return;

                stackLitData.baseParametrization = (StackLit.BaseParametrization)evt.newValue;
                onChange();
            });
            context.AddProperty("Energy Conserving Specular", 1, new Toggle() { value = lightingData.energyConservingSpecular }, stackLitData.baseParametrization == StackLit.BaseParametrization.SpecularColor, (evt) =>
            {
                if (Equals(lightingData.energyConservingSpecular, evt.newValue))
                    return;

                lightingData.energyConservingSpecular = evt.newValue;
                onChange();
            });

            // Material type enables:
            context.AddLabel("Material Core Features", 0);
            context.AddProperty("Anisotropy", 1, new Toggle() { value = stackLitData.anisotropy }, (evt) =>
            {
                if (Equals(stackLitData.anisotropy, evt.newValue))
                    return;

                stackLitData.anisotropy = evt.newValue;
                onChange();
            });
            context.AddProperty("Coat", 1, new Toggle() { value = stackLitData.coat }, (evt) =>
            {
                if (Equals(stackLitData.coat, evt.newValue))
                    return;

                stackLitData.coat = evt.newValue;
                onChange();
            });
            context.AddProperty("Coat Normal", 2, new Toggle() { value = stackLitData.coatNormal }, stackLitData.coat, (evt) =>
            {
                if (Equals(stackLitData.coatNormal, evt.newValue))
                    return;

                stackLitData.coatNormal = evt.newValue;
                onChange();
            });
            context.AddProperty("Dual Specular Lobe", 1, new Toggle() { value = stackLitData.dualSpecularLobe }, (evt) =>
            {
                if (Equals(stackLitData.dualSpecularLobe, evt.newValue))
                    return;

                stackLitData.dualSpecularLobe = evt.newValue;
                onChange();
            });
            context.AddProperty("Dual SpecularLobe Parametrization", 2, new EnumField(StackLit.DualSpecularLobeParametrization.HazyGloss) { value = stackLitData.dualSpecularLobeParametrization }, stackLitData.dualSpecularLobe, (evt) =>
            {
                if (Equals(stackLitData.dualSpecularLobeParametrization, evt.newValue))
                    return;

                stackLitData.dualSpecularLobeParametrization = (StackLit.DualSpecularLobeParametrization)evt.newValue;
                onChange();
            });
            var capHazinessForNonMetallic = stackLitData.dualSpecularLobe && (stackLitData.baseParametrization == StackLit.BaseParametrization.BaseMetallic) && (stackLitData.dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.HazyGloss);
            context.AddProperty("Cap Haziness For Non Metallic", 2, new Toggle() { value = stackLitData.capHazinessWrtMetallic }, capHazinessForNonMetallic, (evt) =>
            {
                if (Equals(stackLitData.capHazinessWrtMetallic, evt.newValue))
                    return;

                stackLitData.capHazinessWrtMetallic = evt.newValue;
                onChange();
            });
            context.AddProperty("Iridescence", 1, new Toggle() { value = stackLitData.iridescence }, (evt) =>
            {
                if (Equals(stackLitData.iridescence, evt.newValue))
                    return;

                stackLitData.iridescence = evt.newValue;
                onChange();
            });
            context.AddProperty("Subsurface Scattering", 1, new Toggle() { value = lightingData.subsurfaceScattering }, systemData.surfaceType != SurfaceType.Transparent, (evt) =>
            {
                if (Equals(lightingData.subsurfaceScattering, evt.newValue))
                    return;

                lightingData.subsurfaceScattering = evt.newValue;
                onChange();
            });
            context.AddProperty("Transmission", 1, new Toggle() { value = lightingData.transmission }, (evt) =>
            {
                if (Equals(lightingData.transmission, evt.newValue))
                    return;

                lightingData.transmission = evt.newValue;
                onChange();
            });

            // Misc
            context.AddProperty("Receive Decals", 0, new Toggle() { value = lightingData.receiveDecals }, (evt) =>
            {
                if (Equals(lightingData.receiveDecals, evt.newValue))
                    return;

                lightingData.receiveDecals = evt.newValue;
                onChange();
            });
            context.AddProperty("Receive SSR", 0, new Toggle() { value = lightingData.receiveSSR }, (evt) =>
            {
                if (Equals(lightingData.receiveSSR, evt.newValue))
                    return;

                lightingData.receiveSSR = evt.newValue;
                onChange();
            });
            context.AddProperty("Add Precomputed Velocity", 0, new Toggle() { value = builtinData.addPrecomputedVelocity }, (evt) =>
            {
                if (Equals(builtinData.addPrecomputedVelocity, evt.newValue))
                    return;

                builtinData.addPrecomputedVelocity = evt.newValue;
                onChange();
            });
            // TODO: Can this use lightingData.specularAA?
            context.AddProperty("Geometric Specular AA", 0, new Toggle() { value = stackLitData.geometricSpecularAA }, (evt) =>
            {
                if (Equals(stackLitData.geometricSpecularAA, evt.newValue))
                    return;

                stackLitData.geometricSpecularAA = evt.newValue;
                onChange();
            });

            // SpecularOcclusion from SSAO
            context.AddProperty("Specular Occlusion (from SSAO)", 0, new EnumField(StackLitData.SpecularOcclusionBaseMode.DirectFromAO) { value = stackLitData.screenSpaceSpecularOcclusionBaseMode }, stackLitData.devMode, (evt) =>
            {
                if (Equals(stackLitData.screenSpaceSpecularOcclusionBaseMode, evt.newValue))
                    return;

                stackLitData.screenSpaceSpecularOcclusionBaseMode = (StackLitData.SpecularOcclusionBaseMode)evt.newValue;
                onChange();
            });
            var specularOcclusionSSUsesVisibilityCone = stackLitData.devMode && HDStackLitSubTarget.SpecularOcclusionModeUsesVisibilityCone(stackLitData.screenSpaceSpecularOcclusionBaseMode);
            context.AddProperty("Specular Occlusion (SS) AO Cone Weight", 1, new EnumField(StackLitData.SpecularOcclusionAOConeSize.CosWeightedAO) { value = stackLitData.screenSpaceSpecularOcclusionAOConeSize }, specularOcclusionSSUsesVisibilityCone, (evt) =>
            {
                if (Equals(stackLitData.screenSpaceSpecularOcclusionAOConeSize, evt.newValue))
                    return;

                stackLitData.screenSpaceSpecularOcclusionAOConeSize = (StackLitData.SpecularOcclusionAOConeSize)evt.newValue;
                onChange();
            });
            context.AddProperty("Specular Occlusion (SS) AO Cone Dir", 1, new EnumField(StackLitData.SpecularOcclusionAOConeDir.ShadingNormal) { value = stackLitData.screenSpaceSpecularOcclusionAOConeDir }, specularOcclusionSSUsesVisibilityCone, (evt) =>
            {
                if (Equals(stackLitData.screenSpaceSpecularOcclusionAOConeDir, evt.newValue))
                    return;

                stackLitData.screenSpaceSpecularOcclusionAOConeDir = (StackLitData.SpecularOcclusionAOConeDir)evt.newValue;
                onChange();
            });

            // SpecularOcclusion from input AO (baked or data-based SO)
            EnumField specularOcclusionFromInputAOField;
            if(stackLitData.devMode)
            {
                specularOcclusionFromInputAOField = new EnumField(StackLitData.SpecularOcclusionBaseMode.DirectFromAO);
                specularOcclusionFromInputAOField.value = stackLitData.dataBasedSpecularOcclusionBaseMode;
            }
            else
            {
                specularOcclusionFromInputAOField = new EnumField(StackLitData.SpecularOcclusionBaseModeSimple.DirectFromAO);
                specularOcclusionFromInputAOField.value = Enum.TryParse(stackLitData.dataBasedSpecularOcclusionBaseMode.ToString(), out StackLitData.SpecularOcclusionBaseModeSimple parsedValue) ?
                    parsedValue : StackLitData.SpecularOcclusionBaseModeSimple.SPTDIntegrationOfBentAO;
            }
            context.AddProperty("Specular Occlusion (from input AO)", 0, specularOcclusionFromInputAOField, (evt) =>
            {
                if (Equals(stackLitData.dataBasedSpecularOcclusionBaseMode, evt.newValue))
                    return;

                stackLitData.dataBasedSpecularOcclusionBaseMode = (StackLitData.SpecularOcclusionBaseMode)evt.newValue;
                onChange();
            });
            var specularOcclusionUsesVisibilityCone = HDStackLitSubTarget.SpecularOcclusionModeUsesVisibilityCone(stackLitData.dataBasedSpecularOcclusionBaseMode);
            context.AddProperty("Specular Occlusion AO Cone Weight", 1, new EnumField(StackLitData.SpecularOcclusionAOConeSize.CosWeightedBentCorrectAO) { value = stackLitData.dataBasedSpecularOcclusionAOConeSize }, specularOcclusionUsesVisibilityCone, (evt) =>
            {
                if (Equals(stackLitData.dataBasedSpecularOcclusionAOConeSize, evt.newValue))
                    return;

                stackLitData.dataBasedSpecularOcclusionAOConeSize = (StackLitData.SpecularOcclusionAOConeSize)evt.newValue;
                onChange();
            });

            // Specular Occlusion Bent Normal
            var useBentConeFixup = HDStackLitSubTarget.SpecularOcclusionUsesBentNormal(stackLitData);
            context.AddProperty("Specular Occlusion Bent Cone Fixup", 0, new EnumField(StackLitData.SpecularOcclusionConeFixupMethod.Off) { value = stackLitData.specularOcclusionConeFixupMethod }, useBentConeFixup && stackLitData.devMode, (evt) =>
            {
                if (Equals(stackLitData.specularOcclusionConeFixupMethod, evt.newValue))
                    return;

                stackLitData.specularOcclusionConeFixupMethod = (StackLitData.SpecularOcclusionConeFixupMethod)evt.newValue;
                onChange();
            });
            context.AddProperty("Specular Occlusion Bent Cone Fixup", 0, new Toggle() { value = stackLitData.specularOcclusionConeFixupMethod != StackLitData.SpecularOcclusionConeFixupMethod.Off }, useBentConeFixup && !stackLitData.devMode, (evt) =>
            {
                if ( (evt.newValue == false && Equals(stackLitData.specularOcclusionConeFixupMethod, StackLitData.SpecularOcclusionConeFixupMethod.Off))
                    || (evt.newValue == true && Equals(stackLitData.specularOcclusionConeFixupMethod, StackLitData.SpecularOcclusionConeFixupMethod.BoostAndTilt)) )
                    return;

                stackLitData.specularOcclusionConeFixupMethod = evt.newValue ? StackLitData.SpecularOcclusionConeFixupMethod.BoostAndTilt
                                                                        : StackLitData.SpecularOcclusionConeFixupMethod.Off;
                onChange();
            });
            
            // Misc Cont.
            context.AddProperty("Support LOD CrossFade", 0, new Toggle() { value = systemData.supportLodCrossFade }, (evt) =>
            {
                if (Equals(systemData.supportLodCrossFade, evt.newValue))
                    return;

                systemData.supportLodCrossFade = evt.newValue;
                onChange();
            });

            // Advanced Options
            context.AddLabel("Advanced Options", 0);
            context.AddProperty("Anisotropy For Area Lights", 1, new Toggle() { value = stackLitData.anisotropyForAreaLights }, (evt) =>
            {
                if (Equals(stackLitData.anisotropyForAreaLights, evt.newValue))
                    return;

                stackLitData.anisotropyForAreaLights = evt.newValue;
                onChange();
            });

            // Per Punctual/Directional Lights
            context.AddLabel("Per Punctual/Directional Lights:", 1);
            context.AddProperty("Base Layer Uses Refracted Angles", 2, new Toggle() { value = stackLitData.anisotropyForAreaLights }, stackLitData.coat, (evt) =>
            {
                if (Equals(stackLitData.anisotropyForAreaLights, evt.newValue))
                    return;

                stackLitData.anisotropyForAreaLights = evt.newValue;
                onChange();
            });
            context.AddProperty("Recompute Stack & Iridescence", 2, new Toggle() { value = stackLitData.recomputeStackPerLight }, stackLitData.coat || stackLitData.iridescence, (evt) =>
            {
                if (Equals(stackLitData.recomputeStackPerLight, evt.newValue))
                    return;

                stackLitData.recomputeStackPerLight = evt.newValue;
                onChange();
            });
            context.AddProperty("Honor Per Light Max Smoothness", 2, new Toggle() { value = stackLitData.honorPerLightMinRoughness }, (evt) =>
            {
                if (Equals(stackLitData.honorPerLightMinRoughness, evt.newValue))
                    return;

                stackLitData.honorPerLightMinRoughness = evt.newValue;
                onChange();
            });

            // Debug
            // Uncomment to show the dev mode UI:
            // context.AddProperty("Enable Dev Mode", 1, new Toggle() { value = stackLitData.devMode }, (evt) =>
            // {
            //     if (Equals(stackLitData.devMode, evt.newValue))
            //         return;

            //     stackLitData.devMode = evt.newValue;
            //     onChange();
            // });
            context.AddProperty("Show And Enable StackLit Debugs", 1, new Toggle() { value = stackLitData.debug }, (evt) =>
            {
                if (Equals(stackLitData.debug, evt.newValue))
                    return;

                stackLitData.debug = evt.newValue;
                onChange();
            });

            // Misc Cont.
            context.AddProperty("Override Baked GI", 1, new Toggle() { value = lightingData.overrideBakedGI }, (evt) =>
            {
                if (Equals(lightingData.overrideBakedGI, evt.newValue))
                    return;

                lightingData.overrideBakedGI = evt.newValue;
                onChange();
            });
            context.AddProperty("Depth Offset", 1, new Toggle() { value = builtinData.depthOffset }, (evt) =>
            {
                if (Equals(builtinData.depthOffset, evt.newValue))
                    return;

                builtinData.depthOffset = evt.newValue;
                onChange();
            });
        }

        void DoRenderStateArea(ref TargetPropertyGUIContext context, int indentLevel, Action onChange)
        {
            context.AddProperty("Surface Type", indentLevel, new EnumField(SurfaceType.Opaque) { value = systemData.surfaceType }, (evt) =>
            {
                if (Equals(systemData.surfaceType, evt.newValue))
                    return;

                systemData.surfaceType = (SurfaceType)evt.newValue;
                systemData.TryChangeRenderingPass(systemData.renderingPass);
                onChange();
            });

            context.AddProperty("Blending Mode", indentLevel + 1, new EnumField(BlendMode.Alpha) { value = systemData.blendMode }, systemData.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(systemData.blendMode, evt.newValue))
                    return;

                systemData.blendMode = (BlendMode)evt.newValue;
                onChange();
            });

            context.AddProperty("Blend Preserves Specular", indentLevel + 1, new Toggle() { value = lightingData.blendPreserveSpecular }, systemData.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(lightingData.blendPreserveSpecular, evt.newValue))
                    return;

                lightingData.blendPreserveSpecular = evt.newValue;
                onChange();
            });

            context.AddProperty("Fog", indentLevel + 1, new Toggle() { value = builtinData.transparencyFog }, systemData.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(builtinData.transparencyFog, evt.newValue))
                    return;

                builtinData.transparencyFog = evt.newValue;
                onChange();
            });

            context.AddProperty("Depth Test", indentLevel + 1, new EnumField(systemData.zTest) { value = systemData.zTest }, systemData.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(systemData.zTest, evt.newValue))
                    return;

                systemData.zTest = (CompareFunction)evt.newValue;
                onChange();
            });

            context.AddProperty("Depth Write", indentLevel + 1, new Toggle() { value = systemData.zWrite }, systemData.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(systemData.zWrite, evt.newValue))
                    return;

                systemData.zWrite = evt.newValue;
                onChange();
            });

            context.AddProperty("Cull Mode", indentLevel + 1, new EnumField(systemData.transparentCullMode) { value = systemData.transparentCullMode }, systemData.surfaceType == SurfaceType.Transparent && systemData.doubleSidedMode != DoubleSidedMode.Disabled, (evt) =>
            {
                if (Equals(systemData.transparentCullMode, evt.newValue))
                    return;

                systemData.transparentCullMode = (TransparentCullMode)evt.newValue;
                onChange();
            });

            m_SortPriorityField = new IntegerField() { value = systemData.sortPriority };
            context.AddProperty("Sorting Priority", indentLevel + 1, m_SortPriorityField, systemData.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                var newValue = HDRenderQueue.ClampsTransparentRangePriority(evt.newValue);
                if (Equals(systemData.sortPriority, newValue))
                    return;
                
                m_SortPriorityField.value = newValue;
                systemData.sortPriority = evt.newValue;
                onChange();
            });
        }

        void DoDistortionArea(ref TargetPropertyGUIContext context, int indentLevel, Action onChange)
        {
            context.AddProperty("Distortion", indentLevel, new Toggle() { value = builtinData.distortion }, (evt) =>
            {
                if (Equals(builtinData.distortion, evt.newValue))
                    return;

                builtinData.distortion = evt.newValue;
                onChange();
            });

            context.AddProperty("Distortion Blend Mode", indentLevel + 1, new EnumField(DistortionMode.Add) { value = builtinData.distortionMode }, builtinData.distortion, (evt) =>
            {
                if (Equals(builtinData.distortionMode, evt.newValue))
                    return;

                builtinData.distortionMode = (DistortionMode)evt.newValue;
                onChange();
            });

            context.AddProperty("Distortion Depth Test", indentLevel + 1, new Toggle() { value = builtinData.distortionDepthTest }, builtinData.distortion, (evt) =>
            {
                if (Equals(builtinData.distortionDepthTest, evt.newValue))
                    return;

                builtinData.distortionDepthTest = evt.newValue;
                onChange();
            });
        }
    }
}
