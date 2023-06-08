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
    [Title("Utility", "High Definition Render Pipeline", "Water", "EvaluateDisplacement_Water")]
    class EvaluateDisplacement_Water : AbstractMaterialNode, IGeneratesBodyCode
    {
        public EvaluateDisplacement_Water()
        {
            name = "Evaluate Water  Displacement";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("EvaluateDisplacement_Water");

        const int kPositionOSInputSlotId = 0;
        const string kPositionWSInputSlotName = "PositionOS";

        const int kDisplacementOutputSlotId = 1;
        const string kDisplacementOutputSlotName = "Displacement";

        const int kLowFrequencyHeightOutputSlotId = 2;
        const string kLowFrequencyHeightOutputSlotName = "LowFrequencyHeight";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Inputs
            AddSlot(new PositionMaterialSlot(kPositionOSInputSlotId, kPositionWSInputSlotName, kPositionWSInputSlotName, CoordinateSpace.Object, ShaderStageCapability.Vertex));

            // Outputs
            AddSlot(new Vector3MaterialSlot(kDisplacementOutputSlotId, kDisplacementOutputSlotName, kDisplacementOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector1MaterialSlot(kLowFrequencyHeightOutputSlotId, kLowFrequencyHeightOutputSlotName, kLowFrequencyHeightOutputSlotName, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(new[]
            {
                // Inputs
                kPositionOSInputSlotId,
                // Outputs
                kDisplacementOutputSlotId,
                kLowFrequencyHeightOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                sb.AppendLine("WaterDisplacementData displacementData;");
                sb.AppendLine("ZERO_INITIALIZE(WaterDisplacementData, displacementData);");

                string positionOS = GetSlotValue(kPositionOSInputSlotId, generationMode);
                sb.AppendLine("EvaluateWaterDisplacement({0}, displacementData);",
                    positionOS
                );

                sb.AppendLine("$precision3 {0} = displacementData.displacement;",
                    GetVariableNameForSlot(kDisplacementOutputSlotId)
                );

                sb.AppendLine("$precision {0} = displacementData.lowFrequencyHeight;",
                    GetVariableNameForSlot(kLowFrequencyHeightOutputSlotId)
                );
            }
            else
            {
                sb.AppendLine("$precision3 {0} = 0.0;",
                    GetVariableNameForSlot(kDisplacementOutputSlotId)
                );

                sb.AppendLine("$precision {0} = 0.0;",
                    GetVariableNameForSlot(kLowFrequencyHeightOutputSlotId)
                );
            }
        }
    }
}
