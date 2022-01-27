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
    [Title("Utility", "High Definition Render Pipeline", "Water", "EvaluateSimulationDisplacement_Water (Preview)")]
    class EvaluateSimulationDisplacement_Water : AbstractMaterialNode, IGeneratesBodyCode
    {
        public EvaluateSimulationDisplacement_Water()
        {
            name = "Evaluate Water Simulation Displacement (Preview)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("EvaluateSimulationDisplacement_Water");

        const int kPositionWSInputSlotId = 0;
        const string kPositionWSInputSlotName = "PositionWS";

        const int kBandsMultiplierInputSlotId = 1;
        const string kBandsMultiplierInputSlotName = "BandsMultiplier";

        const int kDisplacementOutputSlotId = 2;
        const string kDisplacementOutputSlotName = "Displacement";

        const int kLowFrequencyHeightOutputSlotId = 3;
        const string kLowFrequencyHeightOutputSlotName = "LowFrequencyHeight";

        const int kSSSMaskOutputSlotId = 4;
        const string kSSSMaskOutputSlotName = "SSSMask";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Inputs
            AddSlot(new Vector3MaterialSlot(kPositionWSInputSlotId, kPositionWSInputSlotName, kPositionWSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Vertex));
            AddSlot(new Vector4MaterialSlot(kBandsMultiplierInputSlotId, kBandsMultiplierInputSlotName, kBandsMultiplierInputSlotName, SlotType.Input, Vector4.one, ShaderStageCapability.Vertex));

            // Outputs
            AddSlot(new Vector3MaterialSlot(kDisplacementOutputSlotId, kDisplacementOutputSlotName, kDisplacementOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector1MaterialSlot(kLowFrequencyHeightOutputSlotId, kLowFrequencyHeightOutputSlotName, kLowFrequencyHeightOutputSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(kSSSMaskOutputSlotId, kSSSMaskOutputSlotName, kSSSMaskOutputSlotName, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(new[]
            {
                // Inputs
                kPositionWSInputSlotId,
                kBandsMultiplierInputSlotId,
                // Outputs
                kDisplacementOutputSlotId,
                kLowFrequencyHeightOutputSlotId,
                kSSSMaskOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                sb.AppendLine("WaterDisplacementData displacementData;");
                sb.AppendLine("ZERO_INITIALIZE(WaterDisplacementData, displacementData);");

                string positionWS = GetSlotValue(kPositionWSInputSlotId, generationMode);
                string bandsMutliplier = GetSlotValue(kBandsMultiplierInputSlotId, generationMode);
                sb.AppendLine("EvaluateWaterDisplacement({0}, {1}, displacementData);",
                    positionWS,
                    bandsMutliplier
                );

                sb.AppendLine("$precision3 {0} = displacementData.displacement;",
                    GetVariableNameForSlot(kDisplacementOutputSlotId)
                );

                sb.AppendLine("$precision {0} = displacementData.lowFrequencyHeight;",
                    GetVariableNameForSlot(kLowFrequencyHeightOutputSlotId)
                );

                sb.AppendLine("$precision {0} = displacementData.sssMask;",
                    GetVariableNameForSlot(kSSSMaskOutputSlotId)
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

                sb.AppendLine("$precision {0} = 0.0;",
                    GetVariableNameForSlot(kSSSMaskOutputSlotId)
                );
            }
        }
    }
}
