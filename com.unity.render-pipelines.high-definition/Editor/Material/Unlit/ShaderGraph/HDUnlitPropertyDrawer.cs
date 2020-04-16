using System;
using System.Reflection;
using Drawing.Inspector;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.UIElements;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [SGPropertyDrawer(typeof(HDUnlitMasterNode.HDUnlitSettings))]
    class HDUnlitPropertyDrawer : IPropertyDrawer
    {
        IntegerField m_SortPriorityField;

        // All property views explicitly defined here are because there are other properties that depend on them when they change
        // We need to be able to detect that and respond to it
        private VisualElement CreateGUI(HDUnlitMasterNode masterNode)
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
                propertySheet.Add(enumPropertyDrawer.CreateGUI(
                    newValue =>
                    {
                        masterNode.alphaMode =
                            masterNode.GetAlphaMode((HDUnlitMasterNode.AlphaModeLit) newValue);
                    },
                    masterNode.GetAlphaModeLit(masterNode.alphaMode),
                    "Blending Mode",
                    HDUnlitMasterNode.AlphaModeLit.Additive,
                    out var blendModeVisualElement,
                    indentLevel));

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
                        newValue => masterNode.distortionOnly = newValue,
                        masterNode.distortionOnly,
                        "Distortion Only",
                        out var distortionOnlyToggle,
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
                    out var zWriteTestToggle,
                    indentLevel));

                if (!masterNode.doubleSided.isOn)
                {
                    propertySheet.Add(enumPropertyDrawer.CreateGUI(
                        newValue => masterNode.transparentCullMode = (TransparentCullMode)newValue,
                        masterNode.transparentCullMode,
                        "Cull Mode",
                        masterNode.transparentCullMode,
                        out var transparentCullModeVisualElement,
                        indentLevel));
                }

                propertySheet.Add(enumPropertyDrawer.CreateGUI(
                    newValue => masterNode.zTest = (CompareFunction)newValue,
                    masterNode.zTest,
                    "Depth Test",
                    masterNode.zTest,
                    out var zTestVisualElement,
                    indentLevel));

                --indentLevel;
            }

            propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                newValue =>
                {
                    masterNode.doubleSided = newValue;
                    this.inspectorUpdateDelegate();
                },
                masterNode.doubleSided,
                "Double-Sided",
                out var doubleSidedToggle,
                indentLevel));

            return propertySheet;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            return this.CreateGUI((HDUnlitMasterNode) actualObject);
        }
    }
}
