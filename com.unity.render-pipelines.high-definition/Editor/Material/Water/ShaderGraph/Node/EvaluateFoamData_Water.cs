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
    [Title("Utility", "High Definition Render Pipeline", "Water", "EvaluateFoamData_Water (Preview)")]
    class EvaluateFoamData_Water : AbstractMaterialNode, IGeneratesBodyCode
    {
        public EvaluateFoamData_Water()
        {
            name = "Evaluate Foam Data Water (Preview)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("EvaluateFoamData_Water");

        const int kSurfaceGradientInputSlotId = 0;
        const string kSurfaceGradientInputSlotName = "SurfaceGradient";

        const int kLowFrequencySurfaceGradientInputSlotId = 1;
        const string kLowFrequencySurfaceGradientInputSlotName = "LowFrequencySurfaceGradient";

        const int kSimulationFoamInputSlotId = 2;
        const string kSimulationFoamInputSlotName = "SimulationFoam";

        const int kPositionWSInputSlotId = 3;
        const string kPositionWSInputSlotName = "PositionWS";

        const int kCustomFoamInputSlotId = 4;
        const string kCustomFoamInputSlotName = "CustomFoam";

        const int kSurfaceGradientOutputSlotId = 5;
        const string kSurfaceGradientOutputSlotName = "SurfaceGradient";

        const int kFoamOutputSlotId = 6;
        const string kFoamOutputSlotName = "Foam";

        const int kSmoothnessOutputSlotId = 7;
        const string kSmoothnessOutputSlotName = "Smoothness";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Input
            AddSlot(new Vector3MaterialSlot(kSurfaceGradientInputSlotId, kSurfaceGradientInputSlotName, kSurfaceGradientInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector3MaterialSlot(kLowFrequencySurfaceGradientInputSlotId, kLowFrequencySurfaceGradientInputSlotName, kLowFrequencySurfaceGradientInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(kSimulationFoamInputSlotId, kSimulationFoamInputSlotName, kSimulationFoamInputSlotName, SlotType.Input, 0, ShaderStageCapability.Fragment));
            AddSlot(new Vector3MaterialSlot(kPositionWSInputSlotId, kPositionWSInputSlotName, kPositionWSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(kCustomFoamInputSlotId, kCustomFoamInputSlotName, kCustomFoamInputSlotName, SlotType.Input, 0, ShaderStageCapability.Fragment));

            // Output
            AddSlot(new Vector3MaterialSlot(kSurfaceGradientOutputSlotId, kSurfaceGradientOutputSlotName, kSurfaceGradientOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector1MaterialSlot(kFoamOutputSlotId, kFoamOutputSlotName, kFoamOutputSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(kSmoothnessOutputSlotId, kSmoothnessOutputSlotName, kSmoothnessOutputSlotName, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(new[]
            {
                // Input
                kSurfaceGradientInputSlotId,
                kLowFrequencySurfaceGradientInputSlotId,
                kSimulationFoamInputSlotId,
                kPositionWSInputSlotId,
                kCustomFoamInputSlotId,

                // Output
                kSurfaceGradientOutputSlotId,
                kFoamOutputSlotId,
                kSmoothnessOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                string surfaceGradient = GetSlotValue(kSurfaceGradientInputSlotId, generationMode);
                string lowFrequencySG = GetSlotValue(kLowFrequencySurfaceGradientInputSlotId, generationMode);
                string simulationFoam = GetSlotValue(kSimulationFoamInputSlotId, generationMode);
                string customFoam = GetSlotValue(kCustomFoamInputSlotId, generationMode);
                string positionWS = GetSlotValue(kPositionWSInputSlotId, generationMode);

                sb.AppendLine("FoamData foamData;");
                sb.AppendLine("ZERO_INITIALIZE(FoamData, foamData);");

                sb.AppendLine("EvaluateFoamData({0}, {1}, {2}, {3}, {4}, foamData);",
                    surfaceGradient,
                    lowFrequencySG,
                    simulationFoam,
                    customFoam,
                    positionWS
                );

                sb.AppendLine("$precision {0} = foamData.smoothness;",
                    GetVariableNameForSlot(kSmoothnessOutputSlotId)
                );

                sb.AppendLine("$precision {0} = foamData.foamValue;",
                    GetVariableNameForSlot(kFoamOutputSlotId)
                );

                sb.AppendLine("$precision3 {0} = foamData.surfaceGradient;",
                    GetVariableNameForSlot(kSurfaceGradientOutputSlotId)
                );
            }
            else
            {
                sb.AppendLine("$precision {0} = 0.0;",
                    GetVariableNameForSlot(kSmoothnessOutputSlotId)
                );

                sb.AppendLine("$precision {0} = 0.0;",
                    GetVariableNameForSlot(kFoamOutputSlotId)
                );

                sb.AppendLine("$precision3 {0} = 0.0;",
                    GetVariableNameForSlot(kSurfaceGradientOutputSlotId)
                );
            }
        }
    }
}
