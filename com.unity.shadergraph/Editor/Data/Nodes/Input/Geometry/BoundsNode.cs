using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    // TODO: disable this node for bultin
    [Title("Input", "Geometry", "Bounds")]
    class BoundsNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public BoundsNode()
        {
            name = "Bounds";
            UpdateNodeAfterDeserialization();
        }

        // public override string documentationURL => Documentation.GetPageLink("SGNode-Bounds");

        public CoordinateSpace space;
        // TODO: space option: world, object, view, etc.

        const int kBoundsMinOutputSlotId = 0;
        const int kBoundsMaxOutputSlotId = 1;
        const int kBoundsSizeOutputSlotId = 2;
        const string kBoundsMinOutputSlotName = "Min";
        const string kBoundsMaxOutputSlotName = "Max";
        const string kBoundsSizeOutputSlotName = "Size";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(kBoundsMinOutputSlotId, kBoundsMinOutputSlotName, kBoundsMinOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(kBoundsMaxOutputSlotId, kBoundsMaxOutputSlotName, kBoundsMaxOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(kBoundsSizeOutputSlotId, kBoundsSizeOutputSlotName, kBoundsSizeOutputSlotName, SlotType.Output, Vector3.zero));

            RemoveSlotsNameNotMatching(new[]
            {
                kBoundsMinOutputSlotId, kBoundsMaxOutputSlotId, kBoundsSizeOutputSlotId
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (IsSlotConnected(kBoundsMinOutputSlotId))
                sb.AppendLine($"$precision3 {GetSlotValue(kBoundsMinOutputSlotId, generationMode)} = unity_RendererBounds_Min;");
            if (IsSlotConnected(kBoundsMaxOutputSlotId))
                sb.AppendLine($"$precision3 {GetSlotValue(kBoundsMaxOutputSlotId, generationMode)} = unity_RendererBounds_Max;");
            if (IsSlotConnected(kBoundsSizeOutputSlotId))
                sb.AppendLine($"$precision3 {GetSlotValue(kBoundsSizeOutputSlotId, generationMode)} = unity_RendererBounds_Max - unity_RendererBounds_Min;");
        }
    }
}
