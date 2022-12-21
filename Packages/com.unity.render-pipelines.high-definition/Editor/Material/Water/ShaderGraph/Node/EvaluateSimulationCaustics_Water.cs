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
    [Title("Utility", "High Definition Render Pipeline", "Water", "EvaluateSimulationCaustics_Water")]
    class EvaluateSimulationCaustics_Water : AbstractMaterialNode, IGeneratesBodyCode, IMayRequirePosition
    {
        public EvaluateSimulationCaustics_Water()
        {
            name = "Evaluate Simulation Caustics Water";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("EvaluateSimulationCaustics_Water");

        const int kRefractedPositionWSInputSlotId = 0;
        const string kRefractedPositionWSInputSlotName = "RefractedPositionWS";

        const int kDistordedWaterNDCInputSlotId = 1;
        const string kDistordedWaterNDCInputSlotName = "DistordedWaterNDC";

        const int kCausticsOutputSlotId = 2;
        const string kCausticsOutputSlotName = "Caustics";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Input
            AddSlot(new Vector3MaterialSlot(kRefractedPositionWSInputSlotId, kRefractedPositionWSInputSlotName, kRefractedPositionWSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector2MaterialSlot(kDistordedWaterNDCInputSlotId, kDistordedWaterNDCInputSlotName, kDistordedWaterNDCInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));

            // Output
            AddSlot(new Vector1MaterialSlot(kCausticsOutputSlotId, kCausticsOutputSlotName, kCausticsOutputSlotName, SlotType.Output, 0.0f));

            RemoveSlotsNameNotMatching(new[]
            {
                // Input
                kRefractedPositionWSInputSlotId,
                kDistordedWaterNDCInputSlotId,

                // Output
                kCausticsOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                // Evaluate the refraction parameters
                string refractedPosWS = GetSlotValue(kRefractedPositionWSInputSlotId, generationMode);
                string waterNDC = GetSlotValue(kDistordedWaterNDCInputSlotId, generationMode);
                string positionAWS = $"IN.{CoordinateSpace.World.ToVariableName(InterpolatorType.Position)}";

                sb.AppendLine("$precision {3} = EvaluateSimulationCaustics({0}, abs(dot({1} - {0}, _WaterUpDirection.xyz)), {2});",
                    refractedPosWS,
                    positionAWS,
                    waterNDC,
                    GetVariableNameForSlot(kCausticsOutputSlotId)
                );
            }
            else
            {
                sb.AppendLine("$precision {0} = 0.0;", GetVariableNameForSlot(kCausticsOutputSlotId));
            }
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability = ShaderStageCapability.Vertex)
        {
            return NeededCoordinateSpace.World;
        }
    }
}
