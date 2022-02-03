using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [SRPFilter(typeof(HDRenderPipeline))] // TODO: move to ShaderGraph and add the renderer bounds in URP as well
    [Title("Utility", "High Definition Render Pipeline", "Bounds")]
    class BoundsNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public BoundsNode()
        {
            name = "Bounds";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("SGNode-Bounds");

        // TODO: space option: world, object, view, etc.

        const int kBoundsMinOutputSlotId = 0;
        const int kBoundsMaxOutputSlotId = 1;
        const string kBoundsMinOutputSlotName = "Min";
        const string kBoundsMaxOutputSlotName = "Max";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(kBoundsMinOutputSlotId, kBoundsMinOutputSlotName, kBoundsMinOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(kBoundsMaxOutputSlotId, kBoundsMaxOutputSlotName, kBoundsMaxOutputSlotName, SlotType.Output, Vector3.zero));

            RemoveSlotsNameNotMatching(new[]
            {
                kBoundsMinOutputSlotId, kBoundsMaxOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine($"$precision3 {GetSlotValue(kBoundsMinOutputSlotId, generationMode)} = unity_RendererBounds_Min;");
            sb.AppendLine($"$precision3 {GetSlotValue(kBoundsMaxOutputSlotId, generationMode)} = unity_RendererBounds_Max;");
        }
    }
}
