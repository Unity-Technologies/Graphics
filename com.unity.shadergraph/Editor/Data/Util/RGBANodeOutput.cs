using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    internal struct RGBANodeOutput
    {
        const string kRGBAName = "RGBA";
        const string kRName = "R";
        const string kGName = "G";
        const string kBName = "B";
        const string kAName = "A";

        public int rgbaOutput;
        public int rOutput;
        public int gOutput;
        public int bOutput;
        public int aOutput;

        public MaterialSlot rgba;
        public MaterialSlot r;
        public MaterialSlot g;
        public MaterialSlot b;
        public MaterialSlot a;

        public ShaderStageCapability capabilities;

        public static RGBANodeOutput NewDefault()
        {
            return new RGBANodeOutput()
            {
                rgba = null,
                r = null,
                g = null,
                b = null,
                a = null,
                capabilities = ShaderStageCapability.None
            };
        }

        public void CreateNodes(AbstractMaterialNode node, ShaderStageCapability newCapabilities, int rgbaSlot, int rSlot, int gSlot, int bSlot, int aSlot)
        {
            capabilities = newCapabilities;
            rgbaOutput = rgbaSlot;
            rOutput = rSlot;
            gOutput = gSlot;
            bOutput = bSlot;
            aOutput = aSlot;
            rgba = node.AddSlot(new Vector4MaterialSlot(rgbaOutput, kRGBAName, kRGBAName, SlotType.Output, Vector4.zero, capabilities));
            r = node.AddSlot(new Vector1MaterialSlot(rOutput, kRName, kRName, SlotType.Output, 0.0f, capabilities));
            g = node.AddSlot(new Vector1MaterialSlot(gOutput, kGName, kGName, SlotType.Output, 0.0f, capabilities));
            b = node.AddSlot(new Vector1MaterialSlot(bOutput, kBName, kBName, SlotType.Output, 0.0f, capabilities));
            a = node.AddSlot(new Vector1MaterialSlot(aOutput, kAName, kAName, SlotType.Output, 0.0f, capabilities));
        }

        public void SetCapabilities(ShaderStageCapability newCapabilities)
        {
            if (newCapabilities == capabilities)
                return;

            capabilities = newCapabilities;
            rgba.stageCapability = capabilities;
            r.stageCapability = capabilities;
            g.stageCapability = capabilities;
            b.stageCapability = capabilities;
            a.stageCapability = capabilities;
        }
    }
}
