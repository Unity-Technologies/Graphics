using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;

// We share the name of the properties in the UI to avoid duplication
using static UnityEditor.Rendering.HighDefinition.LitSurfaceInputsUIBlock.Styles;
using static UnityEditor.Rendering.HighDefinition.SurfaceOptionUIBlock.Styles;
using static UnityEditor.Rendering.HighDefinition.RefractionUIBlock.Styles;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class StackLitSurfaceOptionPropertyBlock : SurfaceOptionPropertyBlock
    {
        class Styles
        {
            public static GUIContent materialType = new GUIContent("Material Type", "Allow to select the lighting model to used with this Material.");
        }

        StackLitData stackLitData;

        public StackLitSurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features features, StackLitData stackLitData) : base(features)
            => this.stackLitData = stackLitData;

        protected override void CreatePropertyGUI()
        {
            base.CreatePropertyGUI();

            // StackLit specific properties:

            AddProperty("Base Color Parametrization", () => stackLitData.baseParametrization, (newValue) => stackLitData.baseParametrization = newValue);
            if (stackLitData.baseParametrization == StackLit.BaseParametrization.SpecularColor)
            {
                AddProperty("Energy Conserving Specular", () => stackLitData.energyConservingSpecular, (newValue) => stackLitData.energyConservingSpecular = newValue, 1);
            }

            // Material type enables:
            context.AddLabel("Material Core Features", 0);
            AddProperty("Anisotropy", () => stackLitData.anisotropy, (newValue) => stackLitData.anisotropy = newValue, 1);
            AddProperty("Coat", () => stackLitData.coat, (newValue) => stackLitData.coat = newValue, 1);
            AddProperty("Coat Normal", () => stackLitData.coatNormal, (newValue) => stackLitData.coatNormal = newValue, 2);
            AddProperty("Dual Specular Lobe", () => stackLitData.dualSpecularLobe, (newValue) => stackLitData.dualSpecularLobe = newValue, 1);
            AddProperty("Dual SpecularLobe Parametrization", () => stackLitData.dualSpecularLobeParametrization, (newValue) => stackLitData.dualSpecularLobeParametrization = newValue, 2);
            if (stackLitData.dualSpecularLobe && (stackLitData.baseParametrization == StackLit.BaseParametrization.BaseMetallic) && (stackLitData.dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.HazyGloss))
                AddProperty("Cap Haziness For Non Metallic", () => stackLitData.capHazinessWrtMetallic, (newValue) => stackLitData.capHazinessWrtMetallic = newValue, 2);
            AddProperty("Iridescence", () => stackLitData.iridescence, (newValue) => stackLitData.iridescence = newValue, 1);
            if (systemData.surfaceType != SurfaceType.Transparent)
                AddProperty("Subsurface Scattering", () => stackLitData.subsurfaceScattering, (newValue) => stackLitData.subsurfaceScattering = newValue, 1);
            AddProperty("Transmission", () => stackLitData.transmission, (newValue) => stackLitData.transmission = newValue, 1);

            // SpecularOcclusion from SSAO
            if (stackLitData.devMode)
                AddProperty("Specular Occlusion (from SSAO)", () => stackLitData.screenSpaceSpecularOcclusionBaseMode, (newValue) => stackLitData.screenSpaceSpecularOcclusionBaseMode = newValue, 0);
            var specularOcclusionSSUsesVisibilityCone = stackLitData.devMode && StackLitSubTarget.SpecularOcclusionModeUsesVisibilityCone(stackLitData.screenSpaceSpecularOcclusionBaseMode);
            if (specularOcclusionSSUsesVisibilityCone)
            {
                AddProperty("Specular Occlusion (SS) AO Cone Weight", () => stackLitData.screenSpaceSpecularOcclusionAOConeSize, (newValue) => stackLitData.screenSpaceSpecularOcclusionAOConeSize = newValue, 1);
                AddProperty("Specular Occlusion (SS) AO Cone Dir", () => stackLitData.screenSpaceSpecularOcclusionAOConeDir, (newValue) => stackLitData.screenSpaceSpecularOcclusionAOConeDir = newValue, 1);
            }

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

                registerUndo("Specular Occlusion (from input AO)");
                stackLitData.dataBasedSpecularOcclusionBaseMode = (StackLitData.SpecularOcclusionBaseMode)evt.newValue;
                onChange();
            });
            var specularOcclusionUsesVisibilityCone = StackLitSubTarget.SpecularOcclusionModeUsesVisibilityCone(stackLitData.dataBasedSpecularOcclusionBaseMode);
            if (specularOcclusionUsesVisibilityCone)
                AddProperty("Specular Occlusion AO Cone Weight", () => stackLitData.dataBasedSpecularOcclusionAOConeSize, (newValue) => stackLitData.dataBasedSpecularOcclusionAOConeSize = newValue, 1);

            // Specular Occlusion Bent Normal
            var useBentConeFixup = StackLitSubTarget.SpecularOcclusionUsesBentNormal(stackLitData);
            if (useBentConeFixup)
            {
                AddProperty("Specular Occlusion Bent Cone Fixup", () => stackLitData.specularOcclusionConeFixupMethod, (newValue) => stackLitData.specularOcclusionConeFixupMethod = newValue, 0);
                AddProperty("Specular Occlusion Bent Cone Fixup", () => stackLitData.specularOcclusionConeFixupMethod != StackLitData.SpecularOcclusionConeFixupMethod.Off, (newValue) =>
                {
                    stackLitData.specularOcclusionConeFixupMethod = newValue ? StackLitData.SpecularOcclusionConeFixupMethod.BoostAndTilt
                                                        : StackLitData.SpecularOcclusionConeFixupMethod.Off;
                }, 0);
            }
            
            // Misc Cont.
            // Advanced Options
            context.AddLabel("Advanced Options", 0);
            AddProperty("Anisotropy For Area Lights", () => stackLitData.anisotropyForAreaLights, (newValue) => stackLitData.anisotropyForAreaLights = newValue, 1);

            // Per Punctual/Directional Lights
            context.AddLabel("Per Punctual/Directional Lights:", 1);
            if (stackLitData.coat)
                AddProperty("Base Layer Uses Refracted Angles", () => stackLitData.shadeBaseUsingRefractedAngles, (newValue) => stackLitData.shadeBaseUsingRefractedAngles = newValue, 2);
            if (stackLitData.coat || stackLitData.iridescence)
                AddProperty("Recompute Stack & Iridescence", () => stackLitData.recomputeStackPerLight, (newValue) => stackLitData.recomputeStackPerLight = newValue, 2);
            AddProperty("Honor Per Light Max Smoothness", () => stackLitData.honorPerLightMinRoughness, (newValue) => stackLitData.honorPerLightMinRoughness = newValue, 2);

            // Debug
            // Uncomment to show the dev mode UI:
            // if (stackLitData.devMode)
            //     AddProperty("Enable Dev Mode", () => stackLitData.devMode, (newValue) => stackLitData.devMode = newValue, 1);
            if (stackLitData.devMode)
                AddProperty("Show And Enable StackLit Debugs", () => stackLitData.debug, (newValue) => stackLitData.debug = newValue, 1);
        }
    }
}
