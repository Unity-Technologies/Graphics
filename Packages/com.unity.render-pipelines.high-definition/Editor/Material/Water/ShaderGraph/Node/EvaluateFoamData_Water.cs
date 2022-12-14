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
    [Title("Utility", "High Definition Render Pipeline", "Water", "EvaluateFoamData_Water")]
    class EvaluateFoamData_Water : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV
    {
        public EvaluateFoamData_Water()
        {
            name = "Evaluate Foam Data Water";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("EvaluateFoamData_Water");

        const int kSimulationFoamInputSlotId = 0;
        const string kSimulationFoamInputSlotName = "SimulationFoam";

        const int kCustomFoamInputSlotId = 1;
        const string kCustomFoamInputSlotName = "CustomFoam";

        const int kFoamOutputSlotId = 2;
        const string kFoamOutputSlotName = "Foam";

        const int kSmoothnessOutputSlotId = 3;
        const string kSmoothnessOutputSlotName = "Smoothness";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Input
            AddSlot(new Vector1MaterialSlot(kSimulationFoamInputSlotId, kSimulationFoamInputSlotName, kSimulationFoamInputSlotName, SlotType.Input, 0, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(kCustomFoamInputSlotId, kCustomFoamInputSlotName, kCustomFoamInputSlotName, SlotType.Input, 0, ShaderStageCapability.Fragment));

            // Output
            AddSlot(new Vector1MaterialSlot(kFoamOutputSlotId, kFoamOutputSlotName, kFoamOutputSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(kSmoothnessOutputSlotId, kSmoothnessOutputSlotName, kSmoothnessOutputSlotName, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(new[]
            {
                // Input
                kSimulationFoamInputSlotId,
                kCustomFoamInputSlotId,

                // Output
                kFoamOutputSlotId,
                kSmoothnessOutputSlotId
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                string simulationFoam = GetSlotValue(kSimulationFoamInputSlotId, generationMode);
                string customFoam = GetSlotValue(kCustomFoamInputSlotId, generationMode);

                sb.AppendLine("FoamData foamData;");
                sb.AppendLine("ZERO_INITIALIZE(FoamData, foamData);");

                sb.AppendLine("EvaluateFoamData({0}, {1}, IN.{2}.xzy, foamData);",
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
            }
            else
            {
                sb.AppendLine("$precision {0} = 0.0;",
                    GetVariableNameForSlot(kSmoothnessOutputSlotId)
                );

                sb.AppendLine("$precision {0} = 0.0;",
                    GetVariableNameForSlot(kFoamOutputSlotId)
                );
            }
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            return channel == UVChannel.UV0;
        }
    }
}
