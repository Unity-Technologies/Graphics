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
    [Title("Utility", "High Definition Render Pipeline", "Water", "EvaluateSimulationAdditionalData_Water (Preview)")]
    class EvaluateSimulationAdditionalData_Water : AbstractMaterialNode, IGeneratesBodyCode
    {
        public EvaluateSimulationAdditionalData_Water()
        {
            name = "Evaluate Simulation Additional Data Water (Preview)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("EvaluateSimulationAdditionalData_Water");

        const int kPositionWSInputSlotId = 0;
        const string kPositionWSInputSlotName = "PositionWS";

        const int kSurfaceGradientOutputSlotId = 1;
        const string kSurfaceGradientOutputSlotName = "SurfaceGradient";

        const int kLowFrequencySurfaceGradientOutputSlotId = 2;
        const string kLowFrequencySurfaceGradientOutputSlotName = "LowFrequencySurfaceGradient";

        const int kSimulationFoamOutputSlotId = 3;
        const string kSimulationFoamOutputSlotName = "SimulationFoam";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Input
            AddSlot(new Vector3MaterialSlot(kPositionWSInputSlotId, kPositionWSInputSlotName, kPositionWSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));

            // Output
            AddSlot(new Vector3MaterialSlot(kSurfaceGradientOutputSlotId, kSurfaceGradientOutputSlotName, kSurfaceGradientOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(kLowFrequencySurfaceGradientOutputSlotId, kLowFrequencySurfaceGradientOutputSlotName, kLowFrequencySurfaceGradientOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector2MaterialSlot(kSimulationFoamOutputSlotId, kSimulationFoamOutputSlotName, kSimulationFoamOutputSlotName, SlotType.Output, Vector2.zero));

            RemoveSlotsNameNotMatching(new[]
            {
                // Input
                kPositionWSInputSlotId,

                // Output
                kSurfaceGradientOutputSlotId,
                kLowFrequencySurfaceGradientOutputSlotId,
                kSimulationFoamOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                // Initialize the structure
                sb.AppendLine("WaterAdditionalData waterAdditionalData;");
                sb.AppendLine("ZERO_INITIALIZE(WaterAdditionalData, waterAdditionalData);");

                // Evaluate the data
                sb.AppendLine("EvaluateWaterAdditionalData({0}, waterAdditionalData);",
                    GetSlotValue(kPositionWSInputSlotId, generationMode));

                // Output the data
                sb.AppendLine("$precision3 {0} = waterAdditionalData.surfaceGradient;",
                    GetVariableNameForSlot(kSurfaceGradientOutputSlotId));
                sb.AppendLine("$precision3 {0} = waterAdditionalData.lowFrequencySurfaceGradient;",
                    GetVariableNameForSlot(kLowFrequencySurfaceGradientOutputSlotId));
                sb.AppendLine("$precision2 {0} = waterAdditionalData.simulationFoam;",
                    GetVariableNameForSlot(kSimulationFoamOutputSlotId));
            }
            else
            {
                // Output zeros
                sb.AppendLine("$precision3 {0} = 0.0",
                    GetVariableNameForSlot(kSurfaceGradientOutputSlotId));
                sb.AppendLine("$precision3 {0} = 0.0",
                    GetVariableNameForSlot(kLowFrequencySurfaceGradientOutputSlotId));
                sb.AppendLine("$precision3 {0} = 0.0",
                    GetVariableNameForSlot(kSimulationFoamOutputSlotId));
            }
        }
    }
}
