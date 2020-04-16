using System;
using System.Reflection;
using Drawing.Inspector;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.UIElements;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.HighDefinition
{
    [SGPropertyDrawer(typeof(HDLitMasterNode.HDLitSettings))]
    public class HDLitPropertyDrawer : IPropertyDrawer
    {
        IntegerField m_SortPriorityField;

        public Action inspectorUpdateDelegate { get; set; }

        internal VisualElement CreateGUI(HDLitMasterNode masterNode)
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

            indentLevel++;

            switch (masterNode.surfaceType)
            {
                case SurfaceType.Opaque:
                    propertySheet.Add(new PropertyRow(PropertyDrawerUtils.CreateLabel("Rendering Pass", indentLevel)), (row) =>
                    {
                        var valueList = HDSubShaderUtilities.GetRenderingPassList(true, true);

                        // #TODO: Inspector - Harvest these PopupFields for a GenericListPropertyDrawer
                        row.Add(new PopupField<HDRenderQueue.RenderQueueType>(valueList, HDRenderQueue.RenderQueueType.Opaque, HDSubShaderUtilities.RenderQueueName, HDSubShaderUtilities.RenderQueueName), (field) =>
                        {
                            field.value = HDRenderQueue.GetOpaqueEquivalent(masterNode.renderingPass);
                            field.RegisterValueChangedCallback(evt =>
                            {
                                masterNode.renderingPass = evt.newValue;
                                inspectorUpdateDelegate();
                            });
                        });
                    });
                    break;
                case SurfaceType.Transparent:
                    propertySheet.Add(new PropertyRow(PropertyDrawerUtils.CreateLabel("Rendering Pass", indentLevel)), (row) =>
                    {
                        Enum defaultValue;
                        switch (masterNode.renderingPass) // Migration
                        {
                            default: //when deserializing without issue, we still need to init the default to something even if not used.
                            case HDRenderQueue.RenderQueueType.Transparent:
                                defaultValue = HDRenderQueue.TransparentRenderQueue.Default;
                                break;
                            case HDRenderQueue.RenderQueueType.PreRefraction:
                                defaultValue = HDRenderQueue.TransparentRenderQueue.BeforeRefraction;
                                break;
                        }

                        var valueList = HDSubShaderUtilities.GetRenderingPassList(false, true);

                        row.Add(new PopupField<HDRenderQueue.RenderQueueType>(valueList, HDRenderQueue.RenderQueueType.Transparent, HDSubShaderUtilities.RenderQueueName, HDSubShaderUtilities.RenderQueueName), (field) =>
                        {
                            field.value = HDRenderQueue.GetTransparentEquivalent(masterNode.renderingPass);
                            field.RegisterValueChangedCallback(evt =>
                            {
                                masterNode.renderingPass = evt.newValue;
                                inspectorUpdateDelegate();
                            });
                        });
                    });
                    break;
                default:
                    throw new ArgumentException("Unknown SurfaceType");
            }
            --indentLevel;

            if (masterNode.surfaceType == SurfaceType.Transparent)
            {
                ++indentLevel;
                if (!masterNode.HasRefraction())
                {
                    propertySheet.Add(enumPropertyDrawer.CreateGUI(
                        newValue =>
                        {
                            masterNode.alphaMode =
                                masterNode.GetAlphaMode((HDLitMasterNode.AlphaModeLit) newValue);
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
                        "Preserve Specular Lighting",
                        out var preserveSpecularLightingToggle,
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
                m_SortPriorityField = (IntegerField)sortPriorityField;

                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    newValue => masterNode.transparencyFog = newValue,
                    masterNode.transparencyFog,
                    "Receive Fog",
                    out var receiveFogToggle,
                    indentLevel));

                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    newValue => masterNode.backThenFrontRendering = newValue,
                    masterNode.backThenFrontRendering,
                    "Back Then Front Rendering",
                    out var backThenFrontToggle,
                    indentLevel));

                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    newValue => masterNode.alphaTestDepthPrepass = newValue,
                    masterNode.alphaTestDepthPrepass,
                    "Transparent Depth Prepass",
                    out var transparentDepthPrepassToggle,
                    indentLevel));

                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    newValue => masterNode.alphaTestDepthPostpass = newValue,
                    masterNode.alphaTestDepthPostpass,
                    "Transparent Depth Postpass",
                    out var transparentDepthPostpassToggle,
                    indentLevel));

                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    newValue => masterNode.transparentWritesMotionVec = newValue,
                    masterNode.transparentWritesMotionVec,
                    "Transparent Writes Motion Vector",
                    out var transparentWritesMotionVectorToggle,
                    indentLevel));

                if (masterNode.renderingPass != HDRenderQueue.RenderQueueType.PreRefraction)
                {
                    propertySheet.Add(enumPropertyDrawer.CreateGUI(
                        newValue =>
                        {
                            masterNode.refractionModel = (ScreenSpaceRefraction.RefractionModel) newValue;
                            this.inspectorUpdateDelegate();
                        },
                        masterNode.refractionModel,
                        "Refraction Model",
                        ScreenSpaceRefraction.RefractionModel.None,
                        out var refractionModelVisualElement,
                        indentLevel));
                }

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
                        newValue => masterNode.distortionMode = (DistortionMode)newValue,
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

                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    newValue => masterNode.zWrite = newValue,
                    masterNode.zWrite,
                    "Depth Write",
                    out var depthWriteToggle,
                    indentLevel));

                if (masterNode.doubleSidedMode == DoubleSidedMode.Disabled)
                {
                    propertySheet.Add(enumPropertyDrawer.CreateGUI(
                        newValue => masterNode.transparentCullMode = (TransparentCullMode)newValue,
                        masterNode.transparentCullMode,
                        "Cull Mode",
                        masterNode.transparentCullMode,
                        out var cullModeVisualElement,
                        indentLevel));
                }

                propertySheet.Add(enumPropertyDrawer.CreateGUI(
                    newValue => masterNode.zTest = (CompareFunction)newValue,
                    masterNode.zTest,
                    "Depth Test",
                    masterNode.zTest,
                    out var depthTestVisualElement,
                    indentLevel));

                --indentLevel;
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

            propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                newValue =>
                {
                    masterNode.alphaTest = newValue;
                    this.inspectorUpdateDelegate();
                },
                masterNode.alphaTest,
                "Alpha Clipping",
                out var alphaClippingToggle,
                indentLevel));

            if (masterNode.alphaTest.isOn)
            {
                ++indentLevel;
                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    newValue => masterNode.alphaTestShadow = newValue,
                    masterNode.alphaTestShadow,
                    "Use Shadow Threshold",
                    out var shadowThresholdVisualElement,
                    indentLevel));
                --indentLevel;
            }

            propertySheet.Add(enumPropertyDrawer.CreateGUI(
                newValue =>
                {
                    masterNode.materialType = (HDLitMasterNode.MaterialType) newValue;
                    this.inspectorUpdateDelegate();
                },
                masterNode.materialType,
                "Material Type",
                HDLitMasterNode.MaterialType.Standard,
                out var materialTypeVisualElement,
                indentLevel));

            ++indentLevel;
            if (masterNode.materialType == HDLitMasterNode.MaterialType.SubsurfaceScattering)
            {
                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    newValue => masterNode.sssTransmission = newValue,
                    masterNode.sssTransmission,
                    "Transmission",
                    out var transmissionToggle,
                    indentLevel));
            }

            if (masterNode.materialType == HDLitMasterNode.MaterialType.SpecularColor)
            {
                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    newValue => masterNode.energyConservingSpecular = newValue,
                    masterNode.energyConservingSpecular,
                    "Energy Conserving Specular",
                    out var energyConservingSpecularToggle,
                    indentLevel));
            }
            --indentLevel;

            propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                newValue =>
                {
                    if (masterNode.surfaceType == SurfaceType.Transparent)
                        masterNode.receiveSSRTransparent = newValue;
                    else
                        masterNode.receiveSSR = newValue;
                },
                masterNode.surfaceType == SurfaceType.Transparent ? masterNode.receiveSSRTransparent : masterNode.receiveSSR,
                "Receive SSR",
                out var receiveSSRToggle,
                indentLevel));

            return propertySheet;
        }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            return this.CreateGUI((HDLitMasterNode) actualObject);
        }
    }
}
