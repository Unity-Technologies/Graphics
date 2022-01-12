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
    [Title("Utility", "High Definition Render Pipeline", "Water", "EvaluateRefractionData_Water (Preview)")]
    class EvaluateRefractionData_Water : AbstractMaterialNode, IGeneratesBodyCode
    {
        public EvaluateRefractionData_Water()
        {
            name = "Evaluate Refraction Data Water (Preview)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("EvaluateRefractionData_Water");

        const int kPositionWSInputSlotId = 0;
        const string kPositionWSInputSlotName = "PositionWS";

        const int kNormalWSInputSlotId = 1;
        const string kNormalWSInputSlotName = "NormalWS";

        const int kLowFrequencyNormalWSInputSlotId = 2;
        const string kLowFrequencyNormalWSInputSlotName = "LowFrequencyNormalWS";

        const int kScreenPositionInputSlotId = 3;
        const string kScreenPositionInputSlotName = "ScreenPosition";

        const int kViewWSInputSlotId = 4;
        const string kViewWSInputSlotName = "ViewWS";

        const int kRefractedPositionWSOutputSlotId = 5;
        const string kRefractedPositionWSOutputSlotName = "RefractedPositionWS";

        const int kDistordedWaterNDCOutputSlotId = 6;
        const string kDistordedWaterNDCOutputSlotName = "DistordedWaterNDC";

        const int kAbsorptionTintOutputSlotId = 7;
        const string kAbsorptionTintOutputSlotName = "AbsorptionTint";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Input
            AddSlot(new Vector3MaterialSlot(kPositionWSInputSlotId, kPositionWSInputSlotName, kPositionWSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector3MaterialSlot(kNormalWSInputSlotId, kNormalWSInputSlotName, kNormalWSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector3MaterialSlot(kLowFrequencyNormalWSInputSlotId, kLowFrequencyNormalWSInputSlotName, kLowFrequencyNormalWSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector2MaterialSlot(kScreenPositionInputSlotId, kScreenPositionInputSlotName, kScreenPositionInputSlotName, SlotType.Input, Vector2.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector3MaterialSlot(kViewWSInputSlotId, kViewWSInputSlotName, kViewWSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));

            // Output
            AddSlot(new Vector3MaterialSlot(kRefractedPositionWSOutputSlotId, kRefractedPositionWSOutputSlotName, kRefractedPositionWSOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector2MaterialSlot(kDistordedWaterNDCOutputSlotId, kDistordedWaterNDCOutputSlotName, kDistordedWaterNDCOutputSlotName, SlotType.Output, Vector2.zero));
            AddSlot(new Vector3MaterialSlot(kAbsorptionTintOutputSlotId, kAbsorptionTintOutputSlotName, kAbsorptionTintOutputSlotName, SlotType.Output, Vector3.zero));

            RemoveSlotsNameNotMatching(new[]
            {
                // Input
                kPositionWSInputSlotId,
                kNormalWSInputSlotId,
                kLowFrequencyNormalWSInputSlotId,
                kScreenPositionInputSlotId,
                kViewWSInputSlotId,

                // Output
                kRefractedPositionWSOutputSlotId,
                kDistordedWaterNDCOutputSlotId,
                kAbsorptionTintOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                // Declare the variables that will hold the value
                sb.AppendLine("$precision3 refractedPos;");
                sb.AppendLine("$precision2 distordedNDC;");
                sb.AppendLine("$precision refractedDistance;");
                sb.AppendLine("$precision3 absorptionTint;");

                // Evaluate the refraction parameters
                string positionWS = GetSlotValue(kPositionWSInputSlotId, generationMode);
                string normalWS = GetSlotValue(kNormalWSInputSlotId, generationMode);
                string lfNormalWS = GetSlotValue(kLowFrequencyNormalWSInputSlotId, generationMode);
                string screenPos = GetSlotValue(kScreenPositionInputSlotId, generationMode);
                string viewWS = GetSlotValue(kViewWSInputSlotId, generationMode);
                sb.AppendLine("ComputeWaterRefractionParams({0}, {1}, {2}, {3}, {4}, _MaxRefractionDistance, _TransparencyColor.xyz, _OutScatteringCoefficient, refractedPos, distordedNDC, refractedDistance, absorptionTint);",
                    positionWS,
                    normalWS,
                    lfNormalWS,
                    screenPos,
                    viewWS
                );

                // Output the refraction params
                sb.AppendLine("$precision3 {0} = refractedPos;", GetVariableNameForSlot(kRefractedPositionWSOutputSlotId));
                sb.AppendLine("$precision2 {0} = distordedNDC;", GetVariableNameForSlot(kDistordedWaterNDCOutputSlotId));
                sb.AppendLine("$precision3 {0} = absorptionTint;", GetVariableNameForSlot(kAbsorptionTintOutputSlotId));
            }
            else
            {
                sb.AppendLine("$precision3 {0} = 0.0;", GetVariableNameForSlot(kRefractedPositionWSOutputSlotId));
                sb.AppendLine("$precision2 {0} = 0.0;", GetVariableNameForSlot(kDistordedWaterNDCOutputSlotId));
                sb.AppendLine("$precision3 {0} = 0.0;", GetVariableNameForSlot(kAbsorptionTintOutputSlotId));
            }
        }
    }
}
