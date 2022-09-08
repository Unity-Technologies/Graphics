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
    class EvaluateSimulationAdditionalData_Water : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV
    {
        public EvaluateSimulationAdditionalData_Water()
        {
            name = "Evaluate Simulation Additional Data Water (Preview)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("EvaluateSimulationAdditionalData_Water");

        const int kBandsMultiplierInputSlotId = 0;
        const string kBandsMultiplierOutputSlotName = "BandsMutliplier";

        const int kSurfaceGradientOutputSlotId = 1;
        const string kSurfaceGradientOutputSlotName = "SurfaceGradient";

        const int kLowFrequencySurfaceGradientOutputSlotId = 2;
        const string kLowFrequencySurfaceGradientOutputSlotName = "LowFrequencySurfaceGradient";

        const int kSurfaceFoamOutputSlotId = 3;
        const string kSurfaceFoamOutputSlotName = "SurfaceFoam";

        const int kDeepFoamOutputSlotId = 4;
        const string kDeepFoamOutputSlotName = "DeepFoam";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Input
            AddSlot(new Vector4MaterialSlot(kBandsMultiplierInputSlotId, kBandsMultiplierOutputSlotName, kBandsMultiplierOutputSlotName, SlotType.Input, Vector4.one, ShaderStageCapability.Fragment));

            // Output
            AddSlot(new Vector3MaterialSlot(kSurfaceGradientOutputSlotId, kSurfaceGradientOutputSlotName, kSurfaceGradientOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(kLowFrequencySurfaceGradientOutputSlotId, kLowFrequencySurfaceGradientOutputSlotName, kLowFrequencySurfaceGradientOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector1MaterialSlot(kSurfaceFoamOutputSlotId, kSurfaceFoamOutputSlotName, kSurfaceFoamOutputSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(kDeepFoamOutputSlotId, kDeepFoamOutputSlotName, kDeepFoamOutputSlotName, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(new[]
            {
                // Input
                kBandsMultiplierInputSlotId,
                // Output
                kSurfaceGradientOutputSlotId,
                kLowFrequencySurfaceGradientOutputSlotId,
                kSurfaceFoamOutputSlotId,
                kDeepFoamOutputSlotId,
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
                string bandsMutliplier = GetSlotValue(kBandsMultiplierInputSlotId, generationMode);
                sb.AppendLine("EvaluateWaterAdditionalData(IN.{0}.xyz, {1}, waterAdditionalData);",
                    ShaderGeneratorNames.GetUVName(UVChannel.UV0),
                    bandsMutliplier);

                // Output the data
                sb.AppendLine("$precision3 {0} = waterAdditionalData.surfaceGradient;",
                    GetVariableNameForSlot(kSurfaceGradientOutputSlotId));
                sb.AppendLine("$precision3 {0} = waterAdditionalData.lowFrequencySurfaceGradient;",
                    GetVariableNameForSlot(kLowFrequencySurfaceGradientOutputSlotId));
                sb.AppendLine("$precision {0} = waterAdditionalData.surfaceFoam;",
                    GetVariableNameForSlot(kSurfaceFoamOutputSlotId));
                sb.AppendLine("$precision {0} = waterAdditionalData.deepFoam;",
                    GetVariableNameForSlot(kDeepFoamOutputSlotId));
            }
            else
            {
                // Output zeros
                sb.AppendLine("$precision3 {0} = 0.0;",
                    GetVariableNameForSlot(kSurfaceGradientOutputSlotId));
                sb.AppendLine("$precision3 {0} = 0.0;",
                    GetVariableNameForSlot(kLowFrequencySurfaceGradientOutputSlotId));
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
    }
}
