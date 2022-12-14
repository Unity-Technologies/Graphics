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
    [Title("Utility", "High Definition Render Pipeline", "Water", "EvaluateSimulationAdditionalData_Water")]
    class EvaluateSimulationAdditionalData_Water : AbstractMaterialNode, IGeneratesBodyCode, IMayRequirePosition, IMayRequireMeshUV, IMayRequireNormal
    {
        public EvaluateSimulationAdditionalData_Water()
        {
            name = "Evaluate Simulation Additional Data Water";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("EvaluateSimulationAdditionalData_Water");

        const int kNormalWSOutputSlotId = 1;
        const string kNormalWSOutputSlotName = "NormalWS";

        const int kLowFrequencyNormalWSOutputSlotId = 2;
        const string kLowFrequencyNormalWSOutputSlotName = "LowFrequencyNormalWS";

        const int kDeepFoamOutputSlotId = 3;
        const string kDeepFoamOutputSlotName = "DeepFoam";

        const int kSurfaceFoamOutputSlotId = 4;
        const string kSurfaceFoamOutputSlotName = "SurfaceFoam";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Output
            AddSlot(new Vector3MaterialSlot(kNormalWSOutputSlotId, kNormalWSOutputSlotName, kNormalWSOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(kLowFrequencyNormalWSOutputSlotId, kLowFrequencyNormalWSOutputSlotName, kLowFrequencyNormalWSOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector1MaterialSlot(kDeepFoamOutputSlotId, kDeepFoamOutputSlotName, kDeepFoamOutputSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(kSurfaceFoamOutputSlotId, kSurfaceFoamOutputSlotName, kSurfaceFoamOutputSlotName, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(new[]
            {
                // Output
                kNormalWSOutputSlotId,
                kLowFrequencyNormalWSOutputSlotId,
                kDeepFoamOutputSlotId,
                kSurfaceFoamOutputSlotId,
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
                sb.AppendLine("EvaluateWaterAdditionalData(IN.{0}.xzy, IN.WorldSpacePosition, IN.WorldSpaceNormal, waterAdditionalData);",
                    ShaderGeneratorNames.GetUVName(UVChannel.UV0));

                // Output the data
                sb.AppendLine("$precision3 {0} = waterAdditionalData.normalWS;",
                    GetVariableNameForSlot(kNormalWSOutputSlotId));
                sb.AppendLine("$precision3 {0} = waterAdditionalData.lowFrequencyNormalWS;",
                    GetVariableNameForSlot(kLowFrequencyNormalWSOutputSlotId));
                sb.AppendLine("$precision {0} = waterAdditionalData.surfaceFoam;",
                    GetVariableNameForSlot(kSurfaceFoamOutputSlotId));
                sb.AppendLine("$precision {0} = waterAdditionalData.deepFoam;",
                    GetVariableNameForSlot(kDeepFoamOutputSlotId));
            }
            else
            {
                // Output zeros
                sb.AppendLine("$precision3 {0} = 0.0;",
                    GetVariableNameForSlot(kNormalWSOutputSlotId));
                sb.AppendLine("$precision3 {0} = 0.0;",
                    GetVariableNameForSlot(kLowFrequencyNormalWSOutputSlotId));
                sb.AppendLine("$precision {0} = 0.0;",
                    GetVariableNameForSlot(kSurfaceFoamOutputSlotId));
                sb.AppendLine("$precision {0} = 0.0;",
                    GetVariableNameForSlot(kDeepFoamOutputSlotId));
            }
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            return channel == UVChannel.UV0;
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability = ShaderStageCapability.Vertex)
        {
            return NeededCoordinateSpace.World;
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability = ShaderStageCapability.Vertex)
        {
            return NeededCoordinateSpace.World;
        }
    }
}
