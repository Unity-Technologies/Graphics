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
    class EvaluateRefractionData_Water : AbstractMaterialNode, IGeneratesBodyCode, IMayRequirePosition, IMayRequireNDCPosition, IMayRequireViewDirection
    {
        public EvaluateRefractionData_Water()
        {
            name = "Evaluate Refraction Data Water (Preview)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("EvaluateRefractionData_Water");

        const int kNormalWSInputSlotId = 0;
        const string kNormalWSInputSlotName = "NormalWS";

        const int kLowFrequencyNormalWSInputSlotId = 1;
        const string kLowFrequencyNormalWSInputSlotName = "LowFrequencyNormalWS";

        const int kRefractedPositionWSOutputSlotId = 2;
        const string kRefractedPositionWSOutputSlotName = "RefractedPositionWS";

        const int kDistordedWaterNDCOutputSlotId = 3;
        const string kDistordedWaterNDCOutputSlotName = "DistordedWaterNDC";

        const int kAbsorptionTintOutputSlotId = 4;
        const string kAbsorptionTintOutputSlotName = "AbsorptionTint";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Input
            AddSlot(new Vector3MaterialSlot(kNormalWSInputSlotId, kNormalWSInputSlotName, kNormalWSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector3MaterialSlot(kLowFrequencyNormalWSInputSlotId, kLowFrequencyNormalWSInputSlotName, kLowFrequencyNormalWSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));

            // Output
            AddSlot(new Vector3MaterialSlot(kRefractedPositionWSOutputSlotId, kRefractedPositionWSOutputSlotName, kRefractedPositionWSOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector2MaterialSlot(kDistordedWaterNDCOutputSlotId, kDistordedWaterNDCOutputSlotName, kDistordedWaterNDCOutputSlotName, SlotType.Output, Vector2.zero));
            AddSlot(new Vector3MaterialSlot(kAbsorptionTintOutputSlotId, kAbsorptionTintOutputSlotName, kAbsorptionTintOutputSlotName, SlotType.Output, Vector3.zero));

            RemoveSlotsNameNotMatching(new[]
            {
                // Input
                kNormalWSInputSlotId,
                kLowFrequencyNormalWSInputSlotId,

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

                string positionAWS = $"IN.{CoordinateSpace.World.ToVariableName(InterpolatorType.Position)}";
                string normalWS = GetSlotValue(kNormalWSInputSlotId, generationMode);
                string lfNormalWS = GetSlotValue(kLowFrequencyNormalWSInputSlotId, generationMode);
                string screenPos = ScreenSpaceType.Default.ToValueAsVariable();
                string viewWS = $"IN.{CoordinateSpace.World.ToVariableName(InterpolatorType.ViewDirection)}";
                string faceSign = $"IN.{StructFields.SurfaceDescriptionInputs.FaceSign.name}";

                sb.AppendLine("ComputeWaterRefractionParams({0}, {1}, {2}, {3}.xy, {4}, {5}, _MaxRefractionDistance, _TransparencyColor.xyz, _OutScatteringCoefficient, refractedPos, distordedNDC, refractedDistance, absorptionTint);",
                    positionAWS,
                    normalWS,
                    lfNormalWS,
                    screenPos,
                    viewWS,
                    faceSign
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

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            return NeededCoordinateSpace.World;
        }

        bool IMayRequireNDCPosition.RequiresNDCPosition(ShaderStageCapability stageCapability)
        {
            return true;
        }

        public NeededCoordinateSpace RequiresViewDirection(ShaderStageCapability stageCapability)
        {
            return NeededCoordinateSpace.World;
        }
    }
}
