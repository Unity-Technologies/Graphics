using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [SRPFilter(typeof(HDRenderPipeline))]
    [Title("Utility", "High Definition Render Pipeline", "Water", "EvaluateFoamData_Water (Preview)")]
    class EvaluateFoamData_Water : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV
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

        const int kCustomFoamInputSlotId = 3;
        const string kCustomFoamInputSlotName = "CustomFoam";

        const int kSurfaceGradientOutputSlotId = 4;
        const string kSurfaceGradientOutputSlotName = "SurfaceGradient";

        const int kFoamOutputSlotId = 5;
        const string kFoamOutputSlotName = "Foam";

        const int kSmoothnessOutputSlotId = 6;
        const string kSmoothnessOutputSlotName = "Smoothness";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Input
            AddSlot(new Vector3MaterialSlot(kSurfaceGradientInputSlotId, kSurfaceGradientInputSlotName, kSurfaceGradientInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector3MaterialSlot(kLowFrequencySurfaceGradientInputSlotId, kLowFrequencySurfaceGradientInputSlotName, kLowFrequencySurfaceGradientInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(kSimulationFoamInputSlotId, kSimulationFoamInputSlotName, kSimulationFoamInputSlotName, SlotType.Input, 0, ShaderStageCapability.Fragment));
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

                sb.AppendLine("FoamData foamData;");
                sb.AppendLine("ZERO_INITIALIZE(FoamData, foamData);");

                sb.AppendLine("EvaluateFoamData({0}, {1}, {2}, {3}, IN.{4}.xyz, foamData);",
                    surfaceGradient,
                    lowFrequencySG,
                    simulationFoam,
                    customFoam,
                    ShaderGeneratorNames.GetUVName(UVChannel.UV0)
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

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            return channel == UVChannel.UV0;
        }
    }
}
