using System;
using System.Reflection;
using Drawing.Inspector;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [SGPropertyDrawer(typeof(StackLitMasterNode.StackLitSettings))]
    public class StackLitPropertyDrawer : IPropertyDrawer
    {
        IntegerField m_SortPriorityField;
        EnumField m_screenSpaceSpecularOcclusionBaseModeVisualElement;

        public Action inspectorUpdateDelegate { get; set; }

        private VisualElement CreateGUI(StackLitMasterNode masterNode)
        {
            PropertySheet propertySheet = new PropertySheet();
            int indentLevel = 0;

            // Instantiate property drawers
            var enumPropertyDrawer = new EnumPropertyDrawer();
            var toggleDataPropertyDrawer = new ToggleDataPropertyDrawer();
            var integerPropertyDrawer = new IntegerPropertyDrawer();

            propertySheet.Add(enumPropertyDrawer.CreateGUI(
                newValue =>
                {
                    masterNode.surfaceType = (SurfaceType) newValue;
                    this.inspectorUpdateDelegate();
                },
                masterNode.surfaceType,
                "Surface Type",
                SurfaceType.Opaque,
                out var surfaceTypeVisualElement,
                indentLevel));

            if (masterNode.surfaceType == SurfaceType.Transparent)
            {
                ++indentLevel;

                // No refraction in StackLit, always show this:
                propertySheet.Add(enumPropertyDrawer.CreateGUI(
                    newValue =>
                    {
                        masterNode.alphaMode =
                            masterNode.GetAlphaMode((StackLitMasterNode.AlphaModeLit) newValue);
                    },
                    masterNode.GetAlphaModeLit(masterNode.alphaMode),
                    "Blending Mode",
                    HDLitMasterNode.AlphaModeLit.Additive,
                    out var blendModeVisualElement,
                    indentLevel));

                ++indentLevel;

                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    newValue => masterNode.blendPreserveSpecular = newValue,
                    masterNode.blendPreserveSpecular,
                    "Blend Preserves Specular",
                    out var preserveSpecularLightingToggle,
                    indentLevel));

                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    newValue => masterNode.transparencyFog = newValue,
                    masterNode.transparencyFog,
                    "Fog",
                    out var receiveFogToggle,
                    indentLevel));

                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    newValue =>
                    {
                        masterNode.distortion = newValue;
                        inspectorUpdateDelegate();
                    },
                    masterNode.distortion,
                    "Distortion",
                    out var distortionToggle,
                    indentLevel));

                if (masterNode.distortion.isOn)
                {
                    ++indentLevel;

                    propertySheet.Add(enumPropertyDrawer.CreateGUI(
                        newValue => masterNode.distortionMode = (DistortionMode) newValue,
                        masterNode.distortionMode,
                        "Distortion Blend Mode",
                        DistortionMode.Add,
                        out var distortionModeVisualElement,
                        indentLevel));

                    propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                        newValue => masterNode.distortionDepthTest = newValue,
                        masterNode.distortionDepthTest,
                        "Distortion Depth Test",
                        out var distortionDepthTestToggle,
                        indentLevel));

                    --indentLevel;
                }

                propertySheet.Add(integerPropertyDrawer.CreateGUI(newValue =>
                    {
                        m_SortPriorityField.value = masterNode.sortPriority;
                        masterNode.sortPriority = newValue;
                        inspectorUpdateDelegate();
                    },
                    masterNode.sortPriority,
                    "Sorting Priority",
                    out var sortPriorityField,
                    indentLevel));
                // Hold onto field reference for later
                m_SortPriorityField = (IntegerField) sortPriorityField;

                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    newValue => masterNode.zWrite = newValue,
                    masterNode.zWrite,
                    "Depth Write",
                    out var depthWriteToggle,
                    indentLevel));

                if (masterNode.doubleSidedMode == DoubleSidedMode.Disabled)
                {
                    propertySheet.Add(enumPropertyDrawer.CreateGUI(
                        newValue => masterNode.transparentCullMode = (TransparentCullMode) newValue,
                        masterNode.transparentCullMode,
                        "Cull Mode",
                        masterNode.transparentCullMode,
                        out var cullModeVisualElement,
                        indentLevel));
                }

                propertySheet.Add(enumPropertyDrawer.CreateGUI(
                    newValue => masterNode.zTest = (CompareFunction) newValue,
                    masterNode.zTest,
                    "Depth Test",
                    masterNode.zTest,
                    out var depthTestVisualElement,
                    indentLevel));
            }

            propertySheet.Add(enumPropertyDrawer.CreateGUI(
                newValue =>
                {
                    masterNode.doubleSidedMode = (DoubleSidedMode) newValue;
                    this.inspectorUpdateDelegate();
                },
                masterNode.doubleSidedMode,
                "Double-Sided",
                DoubleSidedMode.Disabled,
                out var doubleSidedModeVisualElement,
                indentLevel));

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

            propertySheet.Add(enumPropertyDrawer.CreateGUI(
                newValue =>
                {
                    masterNode.baseParametrization = (StackLit.BaseParametrization) newValue;
                    this.inspectorUpdateDelegate();
                },
                masterNode.baseParametrization,
                "Base Color Parametrization",
                StackLit.BaseParametrization.BaseMetallic,
                out var baseParametrizationVisualElement,
                indentLevel));


            if (masterNode.baseParametrization == StackLit.BaseParametrization.SpecularColor)
            {
                ++indentLevel;
                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    newValue => masterNode.energyConservingSpecular = newValue,
                    masterNode.energyConservingSpecular,
                    "Energy Conserving Specular",
                    out var energyConservingSpecularToggle,
                    indentLevel));
                --indentLevel;
            }

            propertySheet.Add(new PropertyRow(PropertyDrawerUtils.CreateLabel("Material Core Features", indentLevel)),
                (row) => { });
            ;
            ++indentLevel;

            propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                newValue => masterNode.anisotropy = newValue,
                masterNode.anisotropy,
                "Anisotropy",
                out var anisotropyToggle,
                indentLevel));

            propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                newValue =>
                {
                    masterNode.coat = newValue;
                    this.inspectorUpdateDelegate();
                },
                masterNode.coat,
                "Coat",
                out var coatToggle,
                indentLevel));

            if (masterNode.coat.isOn)
            {
                ++indentLevel;
                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    newValue => masterNode.coatNormal = newValue,
                    masterNode.coatNormal,
                    "Coat Normal",
                    out var coatNormalToggle,
                    indentLevel));
                --indentLevel;
            }

            propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                newValue =>
                {
                    masterNode.dualSpecularLobe = newValue;
                    this.inspectorUpdateDelegate();
                },
                masterNode.dualSpecularLobe,
                "Dual Specular Lobe",
                out var dualSpecularLobeToggle,
                indentLevel));

            if (masterNode.dualSpecularLobe.isOn)
            {
                ++indentLevel;
                propertySheet.Add(enumPropertyDrawer.CreateGUI(
                    newValue => masterNode.dualSpecularLobeParametrization =
                        (StackLit.DualSpecularLobeParametrization) newValue,
                    masterNode.dualSpecularLobeParametrization,
                    "Dual SpecularLobe Parametrization",
                    StackLit.DualSpecularLobeParametrization.HazyGloss,
                    out var dualSpecularLobeParametrization,
                    indentLevel));

                if (masterNode.baseParametrization == StackLit.BaseParametrization.BaseMetallic &&
                    masterNode.dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.HazyGloss)
                {
                    propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                        newValue => masterNode.capHazinessWrtMetallic = newValue,
                        masterNode.capHazinessWrtMetallic,
                        "Cap Haziness For Non Metallic",
                        out var capHazinessToggle,
                        indentLevel));
                }

                --indentLevel;
            }

            propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                newValue =>
                {
                    masterNode.iridescence = newValue;
                    this.inspectorUpdateDelegate();
                },
                masterNode.iridescence,
                "Iridescence",
                out var iridescenceToggle,
                indentLevel));

            if (masterNode.surfaceType != SurfaceType.Transparent)
            {
                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    newValue => masterNode.subsurfaceScattering = newValue,
                    masterNode.subsurfaceScattering,
                    "Subsurface Scattering",
                    out var subsurfaceScatteringToggle,
                    indentLevel));
            }

            propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                newValue => masterNode.transmission = newValue,
                masterNode.transmission,
                "Transmission",
                out var tranmissionToggle,
                indentLevel));
            --indentLevel;


            if (masterNode.devMode.isOn)
            {
                propertySheet.Add(enumPropertyDrawer.CreateGUI(
                    newValue =>
                    {
                        if (Equals(newValue, StackLitMasterNode.SpecularOcclusionBaseMode.Custom))
                        {
                            Debug.LogWarning("Custom input not supported for SSAO based specular occlusion.");
                            // Make sure the UI field doesn't switch and stays in sync with the master node property:
                            m_screenSpaceSpecularOcclusionBaseModeVisualElement.value =
                                masterNode.screenSpaceSpecularOcclusionBaseMode;
                            return;
                        }

                        masterNode.screenSpaceSpecularOcclusionBaseMode =
                            (StackLitMasterNode.SpecularOcclusionBaseMode) newValue;
                        this.inspectorUpdateDelegate();
                    },
                    masterNode.dualSpecularLobeParametrization,
                    "Specular Occlusion (from SSAO)",
                    StackLitMasterNode.SpecularOcclusionBaseMode.DirectFromAO,
                    out var screenSpaceSpecularOcclusionBaseModeVisualElement,
                    indentLevel));
                // Store for later use
                m_screenSpaceSpecularOcclusionBaseModeVisualElement =
                    (EnumField) screenSpaceSpecularOcclusionBaseModeVisualElement;

                if (StackLitMasterNode.SpecularOcclusionModeUsesVisibilityCone(masterNode
                    .screenSpaceSpecularOcclusionBaseMode))
                {
                    ++indentLevel;
                    propertySheet.Add(enumPropertyDrawer.CreateGUI(
                        newValue => masterNode.screenSpaceSpecularOcclusionAOConeSize =
                            (StackLitMasterNode.SpecularOcclusionAOConeSize) newValue,
                        masterNode.screenSpaceSpecularOcclusionAOConeSize,
                        "Specular Occlusion (SS) AO Cone Weight",
                        StackLitMasterNode.SpecularOcclusionAOConeSize.CosWeightedAO,
                        out var specularOcclusionAoConeSizeVisualElement,
                        indentLevel));

                    propertySheet.Add(enumPropertyDrawer.CreateGUI(
                        newValue =>
                        {
                            masterNode.screenSpaceSpecularOcclusionAOConeDir =
                                (StackLitMasterNode.SpecularOcclusionAOConeDir) newValue;
                            this.inspectorUpdateDelegate();
                        },
                        masterNode.screenSpaceSpecularOcclusionAOConeDir,
                        "Specular Occlusion (SS) AO Cone Dir",
                        StackLitMasterNode.SpecularOcclusionAOConeDir.ShadingNormal,
                        out var specularOcclusionAoConeDirVisualElement,
                        indentLevel));
                    --indentLevel;
                }
            }

            // SpecularOcclusion from input AO (baked or data-based SO)
            {
                // #TODO: Inspector - Make this less redundant
                if (masterNode.devMode.isOn)
                {
                    propertySheet.Add(enumPropertyDrawer.CreateGUI(
                        newValue =>
                        {
                            masterNode.dataBasedSpecularOcclusionBaseMode =
                                (StackLitMasterNode.SpecularOcclusionBaseMode) newValue;
                            this.inspectorUpdateDelegate();
                        },
                        masterNode.screenSpaceSpecularOcclusionAOConeDir,
                        "Specular Occlusion (from input AO)",
                        StackLitMasterNode.SpecularOcclusionBaseMode.DirectFromAO,
                        out var specularOcclusionBaseModeVisualElement,
                        indentLevel));
                }
                else
                {
                    // In non-dev mode, parse any enum value set to a method not shown in the simple UI as SPTD (highest quality) method:
                    StackLitMasterNode.SpecularOcclusionBaseModeSimple simpleUIEnumValue =
                        Enum.TryParse(masterNode.dataBasedSpecularOcclusionBaseMode.ToString(),
                            out StackLitMasterNode.SpecularOcclusionBaseModeSimple parsedValue)
                            ? parsedValue
                            : StackLitMasterNode.SpecularOcclusionBaseModeSimple.SPTDIntegrationOfBentAO;

                    propertySheet.Add(enumPropertyDrawer.CreateGUI(
                        newValue =>
                        {
                            masterNode.dataBasedSpecularOcclusionBaseMode =
                                (StackLitMasterNode.SpecularOcclusionBaseMode) newValue;
                            this.inspectorUpdateDelegate();
                        },
                        masterNode.dataBasedSpecularOcclusionBaseMode,
                        "Specular Occlusion (from input AO)",
                        simpleUIEnumValue,
                        out var specularOcclusionBaseModeVisualElement,
                        indentLevel));
                }

                if (StackLitMasterNode.SpecularOcclusionModeUsesVisibilityCone(
                    masterNode.dataBasedSpecularOcclusionBaseMode))
                {
                    ++indentLevel;
                    propertySheet.Add(enumPropertyDrawer.CreateGUI(
                        newValue => masterNode.dataBasedSpecularOcclusionAOConeSize =
                            (StackLitMasterNode.SpecularOcclusionAOConeSize) newValue,
                        masterNode.dataBasedSpecularOcclusionAOConeSize,
                        "Specular Occlusion AO Cone Weight",
                        StackLitMasterNode.SpecularOcclusionAOConeSize.CosWeightedBentCorrectAO,
                        out var specularOcclusionAoConeSizeVisualElement,
                        indentLevel));
                    --indentLevel;
                }
            }

            if (masterNode.SpecularOcclusionUsesBentNormal())
            {
                if (masterNode.devMode.isOn)
                {
                    propertySheet.Add(enumPropertyDrawer.CreateGUI(
                        newValue =>
                        {
                            masterNode.specularOcclusionConeFixupMethod =
                                (StackLitMasterNode.SpecularOcclusionConeFixupMethod) newValue;
                        },
                        masterNode.specularOcclusionConeFixupMethod,
                        "Specular Occlusion Bent Cone Fixup",
                        StackLitMasterNode.SpecularOcclusionConeFixupMethod.Off,
                        out var specularOcclusionConeFixupMethodVisualElement,
                        indentLevel));
                }
                else
                {
                    propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                        newValue =>
                        {
                            if ((newValue.isOn == false && Equals(masterNode.specularOcclusionConeFixupMethod,
                                    StackLitMasterNode.SpecularOcclusionConeFixupMethod.Off))
                                || (newValue.isOn && Equals(masterNode.specularOcclusionConeFixupMethod,
                                    StackLitMasterNode.SpecularOcclusionConeFixupMethod.BoostAndTilt)))
                                return;

                            masterNode.specularOcclusionConeFixupMethod = newValue.isOn
                                ? StackLitMasterNode.SpecularOcclusionConeFixupMethod.BoostAndTilt
                                : StackLitMasterNode.SpecularOcclusionConeFixupMethod.Off;
                        },
                        new ToggleData(masterNode.specularOcclusionConeFixupMethod !=
                                       StackLitMasterNode.SpecularOcclusionConeFixupMethod.Off),
                        "Specular Occlusion Bent Cone Fixup",
                        out var specularOcclusionConeFixupMethodVisualElement,
                        indentLevel));

                }
            }

            propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                newValue => masterNode.supportLodCrossFade = newValue,
                masterNode.supportLodCrossFade,
                "Support LOD CrossFade",
                out var LODCrossfadeToggle,
                indentLevel));

            propertySheet.Add(new PropertyRow(PropertyDrawerUtils.CreateLabel("Advanced Options", indentLevel)), (row) => {} );;
            ++indentLevel;

            propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                newValue => masterNode.anisotropyForAreaLights = newValue,
                masterNode.anisotropyForAreaLights,
                "Anisotropy for Area Lights",
                out var anisotropyForAreaLightsToggle,
                indentLevel));

            {
                propertySheet.Add(new PropertyRow(PropertyDrawerUtils.CreateLabel("Per Punctual/Directional Lights:", indentLevel)), (row) => {} );
                ++indentLevel;

                if (masterNode.coat.isOn)
                {
                    propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                        newValue => masterNode.shadeBaseUsingRefractedAngles = newValue,
                        masterNode.shadeBaseUsingRefractedAngles,
                        "Base Layer Uses Refracted Angles",
                        out var shadeBaseUsingRefractedAnglesToggle,
                        indentLevel));
                }

                if (masterNode.coat.isOn || masterNode.iridescence.isOn)
                {
                    propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                        newValue => masterNode.recomputeStackPerLight = newValue,
                        masterNode.recomputeStackPerLight,
                        "Recompute Stack & Iridescence",
                        out var shadeBaseUsingRefractedAnglesToggle,
                        indentLevel));
                }

                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    newValue => masterNode.honorPerLightMinRoughness = newValue,
                    masterNode.honorPerLightMinRoughness,
                    "Honor Per Light Max Smoothness",
                    out var honorPerLightMinRoughnessToggle,
                    indentLevel));

                --indentLevel;
            };

            // Uncomment to show the dev mode UI:
            //propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
            //     newValue => masterNode.devMode = newValue,
            //     masterNode.devMode,
            //     "Enable Dev Mode",
            //     out var devModeToggle,
            //     indentLevel));

            if (masterNode.devMode.isOn)
            {
                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    newValue => masterNode.debug = newValue,
                    masterNode.debug,
                    "Show And Enable StackLit Debugs",
                    out var debugToggle,
                    indentLevel));
            }

            propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                newValue => masterNode.overrideBakedGI = newValue,
                masterNode.overrideBakedGI,
                "Override Baked GI",
                out var overrideBakedGIToggle,
                indentLevel));

            propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                newValue => masterNode.depthOffset = newValue,
                masterNode.depthOffset,
                "Depth Offset",
                out var depthOffsetToggle,
                indentLevel));

            --indentLevel;

            return propertySheet;
        }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            return this.CreateGUI((StackLitMasterNode) actualObject);
        }
    }
}
