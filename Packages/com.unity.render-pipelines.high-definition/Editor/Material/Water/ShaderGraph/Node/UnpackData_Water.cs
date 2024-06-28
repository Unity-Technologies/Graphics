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
    [Title("Utility", "High Definition Render Pipeline", "Water", "UnpackData_Water")]
    class UnpackData_Water : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV
    {
        public UnpackData_Water()
        {
            name = "Unpack Water Data";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("UnpackData_Water");

        const int kLowFrequencyHeightOutputSlotId = 0;
        const string kLowFrequencyHeightSlotName = "LowFrequencyHeight";

        const int kHorizontalDisplacementOutputSlotId = 1;
        const string kHorizontalDisplacementSlotName = "HorizontalDisplacement";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Outputs
            AddSlot(new Vector1MaterialSlot(kLowFrequencyHeightOutputSlotId, kLowFrequencyHeightSlotName, kLowFrequencyHeightSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(kHorizontalDisplacementOutputSlotId, kHorizontalDisplacementSlotName, kHorizontalDisplacementSlotName, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(new[]
            {
                // Outputs
                kLowFrequencyHeightOutputSlotId,
                kHorizontalDisplacementOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            // See PackWaterVertexData

            // Low Frequency Height
            sb.AppendLine("$precision {0} = saturate(IN.{1}.w);",
                GetVariableNameForSlot(kLowFrequencyHeightOutputSlotId),
                ShaderGeneratorNames.GetUVName(UVChannel.UV1)
            );

            // Horizontal Displacement
            sb.AppendLine("$precision {0} = IN.{1}.w;",
                GetVariableNameForSlot(kHorizontalDisplacementOutputSlotId),
                ShaderGeneratorNames.GetUVName(UVChannel.UV0)
            );
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            return channel == UVChannel.UV1;
        }
    }
}
