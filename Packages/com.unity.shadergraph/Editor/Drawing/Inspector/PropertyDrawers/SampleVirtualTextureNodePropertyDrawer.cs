using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(SampleVirtualTextureNode))]
    public class SampleVirtualTextureNodePropertyDrawer : IPropertyDrawer
    {
        VisualElement CreateGUI(SampleVirtualTextureNode node, InspectableAttribute attribute,
            out VisualElement propertyVisualElement)
        {
            PropertySheet propertySheet = new PropertySheet();

            var enumPropertyDrawer = new EnumPropertyDrawer();
            propertySheet.Add(enumPropertyDrawer.CreateGUI((newValue) =>
            {
                if (node.addressMode == (SampleVirtualTextureNode.AddressMode)newValue)
                    return;

                node.owner.owner.RegisterCompleteObjectUndo("Address Mode Change");
                node.addressMode = (SampleVirtualTextureNode.AddressMode)newValue;
            },
                node.addressMode,
                "Address Mode",
                SampleVirtualTextureNode.AddressMode.VtAddressMode_Wrap,
                out var addressModeVisualElement));

            propertySheet.Add(enumPropertyDrawer.CreateGUI((newValue) =>
            {
                if (node.lodCalculation == (SampleVirtualTextureNode.LodCalculation)newValue)
                    return;

                node.owner.owner.RegisterCompleteObjectUndo("Lod Mode Change");
                node.lodCalculation = (SampleVirtualTextureNode.LodCalculation)newValue;
            },
                node.lodCalculation,
                "Lod Mode",
                SampleVirtualTextureNode.LodCalculation.VtLevel_Automatic,
                out var lodCalculationVisualElement));

            propertySheet.Add(enumPropertyDrawer.CreateGUI((newValue) =>
            {
                if (node.sampleQuality == (SampleVirtualTextureNode.QualityMode)newValue)
                    return;

                node.owner.owner.RegisterCompleteObjectUndo("Quality Change");
                node.sampleQuality = (SampleVirtualTextureNode.QualityMode)newValue;
            },
                node.sampleQuality,
                "Quality",
                SampleVirtualTextureNode.QualityMode.VtSampleQuality_High,
                out var qualityVisualElement));

            var boolPropertyDrawer = new BoolPropertyDrawer();
            propertySheet.Add(boolPropertyDrawer.CreateGUI((newValue) =>
            {
                if (node.noFeedback == !newValue)
                    return;

                node.owner.owner.RegisterCompleteObjectUndo("Feedback Settings Change");
                node.noFeedback = !newValue;
            },
                !node.noFeedback,
                "Automatic Streaming",
                out var propertyToggle));

            propertySheet.Add(boolPropertyDrawer.CreateGUI((newValue) =>
            {
                if (node.enableGlobalMipBias == newValue)
                    return;

                node.owner.owner.RegisterCompleteObjectUndo("Enable Global Mip Bias VT Change");
                node.enableGlobalMipBias = newValue;
            },
                node.enableGlobalMipBias,
                "Use Global Mip Bias",
                out var enableGlobalMipBias));


            // display warning if the current master node doesn't support virtual texturing
            // TODO: Add warning when no active subTarget supports VT
            // if (!node.owner.isSubGraph)
            // {
            //     bool supportedByMasterNode =
            //         node.owner.GetNodes<IMasterNode>().FirstOrDefault()?.supportsVirtualTexturing ?? false;
            //     if (!supportedByMasterNode)
            //         propertySheet.Add(new HelpBoxRow(MessageType.Warning),
            //             (row) => row.Add(new Label(
            //                 "The current master node does not support Virtual Texturing, this node will do regular 2D sampling.")));
            // }

            // display warning if the current render pipeline doesn't support virtual texturing
            string labelText;
            IVirtualTexturingEnabledRenderPipeline vtRp =
                GraphicsSettings.currentRenderPipeline as IVirtualTexturingEnabledRenderPipeline;
            if (vtRp == null)
                labelText = "The current render pipeline does not support Virtual Texturing, this node will do regular 2D sampling.";
            else if (vtRp.virtualTexturingEnabled == false)
                labelText = "The current render pipeline has disabled Virtual Texturing, this node will do regular 2D sampling.";
            else
            {
#if !ENABLE_VIRTUALTEXTURES
                labelText = "Virtual Texturing is disabled globally (possibly by the render pipeline settings), this node will do regular 2D sampling.";
#else
                labelText = "";
#endif
            }

            if (!string.IsNullOrEmpty(labelText))
            {
                propertySheet.Add(new HelpBoxRow(labelText, MessageType.Warning));
            }

            propertyVisualElement = propertySheet;
            return propertySheet;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject,
            InspectableAttribute attribute)
        {
            return this.CreateGUI(
                (SampleVirtualTextureNode)actualObject,
                attribute,
                out var propertyVisualElement);
        }

        void IPropertyDrawer.DisposePropertyDrawer() { }
    }
}
