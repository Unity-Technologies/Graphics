using System;
using System.Reflection;
using Drawing.Inspector;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.UIElements;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.HighDefinition
{
    [SGPropertyDrawer(typeof(HairMasterNode.HairSettings))]
    public class HairPropertyDrawer : IPropertyDrawer
    {
        IntegerField m_SortPriorityField;

        public Action inspectorUpdateDelegate { get; set; }

        private VisualElement CreateGUI(HairMasterNode masterNode)
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

                propertySheet.Add(toggleDataPropertyDrawer.CreateGUI(
                    newValue => masterNode.blendPreserveSpecular = newValue,
                    masterNode.blendPreserveSpecular,
                    "Preserve Specular Lighting",
                    out var preserveSpecularLightingToggle,
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
                m_SortPriorityField = (IntegerField) sortPriorityField;

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

            return propertySheet;
        }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            return this.CreateGUI((HairMasterNode) actualObject);
        }
    }
}
