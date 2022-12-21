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
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Utility", "High Definition Render Pipeline", "Water", "BlendNormal_Water")]
    class BlendNormal_Water : AbstractMaterialNode, IGeneratesBodyCode
    {
        public BlendNormal_Water()
        {
            name = "Blend Normal Water";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("BlendNormal_Water");

        const int kNormalTSInputSlotId = 0;
        const string kNormalTSInputSlotName = "NormalTS";

        const int kNormalWSInputSlotId = 1;
        const string kNormalWSInputSlotName = "NormalWS";

        const int kNormalWSOutputSlotId = 2;
        const string kNormalWSOutputSlotName = "NormalWS";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Input
            AddSlot(new Vector3MaterialSlot(kNormalTSInputSlotId, kNormalTSInputSlotName, kNormalTSInputSlotName, SlotType.Input, Vector3.one, ShaderStageCapability.Fragment));
            AddSlot(new Vector3MaterialSlot(kNormalWSInputSlotId, kNormalWSInputSlotName, kNormalWSInputSlotName, SlotType.Input, Vector3.one, ShaderStageCapability.Fragment));

            // Output
            AddSlot(new Vector3MaterialSlot(kNormalWSOutputSlotId, kNormalWSOutputSlotName, kNormalWSOutputSlotName, SlotType.Output, Vector3.zero));

            RemoveSlotsNameNotMatching(new[]
            {
                // Input
                kNormalTSInputSlotId,
                kNormalWSInputSlotId,

                // Output
                kNormalWSOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                sb.AppendLine("$precision3 {2} = BlendWaterNormal({0}, {1});",
                    GetSlotValue(kNormalTSInputSlotId, generationMode),
                    GetSlotValue(kNormalWSInputSlotId, generationMode),
                    GetVariableNameForSlot(kNormalWSOutputSlotId));
            }
            else
            {
                // Output zeros
                sb.AppendLine("$precision3 {0} = 0.0;",
                    GetVariableNameForSlot(kNormalWSOutputSlotId));
            }
        }
    }
}
