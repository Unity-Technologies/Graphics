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
    class UnpackData_Water : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV
    {
        public UnpackData_Water()
        {
            name = "Unpack Water Data (Preview)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("UnpackData_Water");

        const int kLowFrequencyHeightOutputSlotId = 0;
        const string kLowFrequencyHeightSlotName = "LowFrequencyHeight";

        const int kHorizontalDisplacementOutputSlotId = 1;
        const string kHorizontalDisplacementSlotName = "HorizontalDisplacement";

        const int kSSSMaskOutputSlotId = 2;
        const string kSSSMaskSlotName = "SSSMask";

        const int kCustomFoamOutputSlotId = 3;
        const string kCustomFoamSlotName = "CustomFoam";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Outputs
            AddSlot(new Vector1MaterialSlot(kLowFrequencyHeightOutputSlotId, kLowFrequencyHeightSlotName, kLowFrequencyHeightSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(kHorizontalDisplacementOutputSlotId, kHorizontalDisplacementSlotName, kHorizontalDisplacementSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(kSSSMaskOutputSlotId, kSSSMaskSlotName, kSSSMaskSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(kCustomFoamOutputSlotId, kCustomFoamSlotName, kCustomFoamSlotName, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(new[]
            {
                // Outputs
                kLowFrequencyHeightOutputSlotId,
                kHorizontalDisplacementOutputSlotId,
                kSSSMaskOutputSlotId,
                kCustomFoamOutputSlotId
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine("$precision {1} = saturate(IN.{0}.x); $precision {2} = IN.{0}.y; $precision {3} = IN.{0}.z; $precision {4} = IN.{0}.w;",
                ShaderGeneratorNames.GetUVName(UVChannel.UV1),
                GetVariableNameForSlot(kLowFrequencyHeightOutputSlotId),
                GetVariableNameForSlot(kCustomFoamOutputSlotId),
                GetVariableNameForSlot(kSSSMaskOutputSlotId),
                GetVariableNameForSlot(kHorizontalDisplacementOutputSlotId)
            );
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            return channel == UVChannel.UV1;
        }
    }
}
