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
    [Title("Utility", "High Definition Render Pipeline", "Water", "UnpackData_Water (Preview)")]
    class UnpackData_Water : AbstractMaterialNode, IGeneratesBodyCode
    {
        public UnpackData_Water()
        {
            name = "Unpack Water Data (Preview)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("UnpackData_Water");

        const int kUV1InputSlotId = 0;
        const string kUV1InputSlotName = "uv1";

        const int kLowFrequencyHeightOutputSlotId = 1;
        const string kLowFrequencyHeightSlotName = "LowFrequencyHeight";

        const int kCustomFoamOutputSlotId = 2;
        const string kCustomFoamSlotName = "CustomFoam";

        const int kSSSMaskOutputSlotId = 3;
        const string kSSSMaskSlotName = "SSSMask";

        const int kHorizontalDisplacementOutputSlotId = 4;
        const string kHorizontalDisplacementSlotName = "HorizontalDisplacement";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Inputs
            AddSlot(new Vector4MaterialSlot(kUV1InputSlotId, kUV1InputSlotName, kUV1InputSlotName, SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment));

            // Outputs
            AddSlot(new Vector1MaterialSlot(kLowFrequencyHeightOutputSlotId, kLowFrequencyHeightSlotName, kLowFrequencyHeightSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(kCustomFoamOutputSlotId, kCustomFoamSlotName, kCustomFoamSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(kSSSMaskOutputSlotId, kSSSMaskSlotName, kSSSMaskSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(kHorizontalDisplacementOutputSlotId, kHorizontalDisplacementSlotName, kHorizontalDisplacementSlotName, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(new[]
            {
                // Inputs
                kUV1InputSlotId,

                // Outputs
                kLowFrequencyHeightOutputSlotId,
                kCustomFoamOutputSlotId,
                kSSSMaskOutputSlotId,
                kHorizontalDisplacementOutputSlotId
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            string uv1 = GetSlotValue(kUV1InputSlotId, generationMode);
            sb.AppendLine("$precision {1} = {0}.x; $precision {2} = {0}.y; $precision {3} = {0}.z; $precision {4} = {0}.w;",
                uv1,
                GetVariableNameForSlot(kLowFrequencyHeightOutputSlotId),
                GetVariableNameForSlot(kCustomFoamOutputSlotId),
                GetVariableNameForSlot(kSSSMaskOutputSlotId),
                GetVariableNameForSlot(kHorizontalDisplacementOutputSlotId)
            );
        }
    }
}
