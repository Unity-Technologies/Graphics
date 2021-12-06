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
    [Title("Utility", "High Definition Render Pipeline", "Water", "PackVertexData_Water (Preview)")]
    class PackVertexData_Water : AbstractMaterialNode, IGeneratesBodyCode
    {
        public PackVertexData_Water()
        {
            name = "Pack Water Vertex Data (Preview)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("PackVertexData_Water");

        // Inputs
        const int kPositionWSInputSlotId = 0;
        const string kPositionWSInputSlotName = "PositionWS";

        const int kDisplacementInputSlotId = 1;
        const string kDisplacementInputSlotName = "Displacement";

        const int kDisplacementNoChopinessInputSlotId = 2;
        const string kDisplacementNoChopinessInputSlotName = "DisplacementNoChopiness";

        const int kLowFrequencyHeightInputSlotId = 3;
        const string kLowFrequencyHeightInputSlotName = "LowFrequencyHeight";

        const int kSSSMaskInputSlotId = 4;
        const string kSSSMaskInputSlotName = "SSSMask";

        const int kCustomFoamInputSlotId = 5;
        const string kCustomFoamInputSlotName = "CustomFoam";

        // Outputs
        const int kPositionOSOutputSlotId = 6;
        const string kPositionOSOutputSlotName = "PositionOS";

        const int kNormalOSOutputSlotId = 7;
        const string kNormalOSOutputSlotName = "NormalOS";

        const int kUV0OutputSlotId = 8;
        const string kUV0OutputSlotName = "uv0";

        const int kUV1OutputSlotId = 9;
        const string kUV1OutputSlotName = "uv1";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Inputs
            AddSlot(new Vector3MaterialSlot(kPositionWSInputSlotId, kPositionWSInputSlotName, kPositionWSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Vertex));
            AddSlot(new Vector3MaterialSlot(kDisplacementInputSlotId, kDisplacementInputSlotName, kDisplacementInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Vertex));
            AddSlot(new Vector3MaterialSlot(kDisplacementNoChopinessInputSlotId, kDisplacementNoChopinessInputSlotName, kDisplacementNoChopinessInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Vertex));
            AddSlot(new Vector1MaterialSlot(kLowFrequencyHeightInputSlotId, kLowFrequencyHeightInputSlotName, kLowFrequencyHeightInputSlotName, SlotType.Input, 0, ShaderStageCapability.Vertex));
            AddSlot(new Vector1MaterialSlot(kSSSMaskInputSlotId, kSSSMaskInputSlotName, kSSSMaskInputSlotName, SlotType.Input, 0, ShaderStageCapability.Vertex));
            AddSlot(new Vector1MaterialSlot(kCustomFoamInputSlotId, kCustomFoamInputSlotName, kCustomFoamInputSlotName, SlotType.Input, 0, ShaderStageCapability.Vertex));

            // Outputs
            AddSlot(new Vector3MaterialSlot(kPositionOSOutputSlotId, kPositionOSOutputSlotName, kPositionOSOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(kNormalOSOutputSlotId, kNormalOSOutputSlotName, kNormalOSOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector4MaterialSlot(kUV0OutputSlotId, kUV0OutputSlotName, kUV0OutputSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(kUV1OutputSlotId, kUV1OutputSlotName, kUV1OutputSlotName, SlotType.Output, Vector4.zero));

            RemoveSlotsNameNotMatching(new[]
            {
                kPositionWSInputSlotId,
                kDisplacementInputSlotId,
                kDisplacementNoChopinessInputSlotId,
                kLowFrequencyHeightInputSlotId,
                kSSSMaskInputSlotId,
                kCustomFoamInputSlotId,

                kPositionOSOutputSlotId,
                kNormalOSOutputSlotId,
                kUV0OutputSlotId,
                kUV1OutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                sb.AppendLine("PackedWaterData packedWaterData;");
                sb.AppendLine("ZERO_INITIALIZE(PackedWaterData, packedWaterData);");

                string positionWS = GetSlotValue(kPositionWSInputSlotId, generationMode);
                string displacement = GetSlotValue(kDisplacementInputSlotId, generationMode);
                string displacementNoChopiness = GetSlotValue(kDisplacementNoChopinessInputSlotId, generationMode);
                string lowFrequencyHeight = GetSlotValue(kLowFrequencyHeightInputSlotId, generationMode);
                string customFoam = GetSlotValue(kCustomFoamInputSlotId, generationMode);
                string sssMask = GetSlotValue(kSSSMaskInputSlotId, generationMode);

                sb.AppendLine("PackWaterVertexData({0}, {1}, {2}, {3}, {4}, {5}, packedWaterData);",
                    positionWS,
                    displacement,
                    displacementNoChopiness,
                    lowFrequencyHeight,
                    customFoam,
                    sssMask
                );

                sb.AppendLine("$precision3 {0} = packedWaterData.positionOS;",
                    GetVariableNameForSlot(kPositionOSOutputSlotId)
                );

                sb.AppendLine("$precision3 {0} = packedWaterData.normalOS;",
                    GetVariableNameForSlot(kNormalOSOutputSlotId)
                );

                sb.AppendLine("$precision4 {0} = packedWaterData.uv0;",
                    GetVariableNameForSlot(kUV0OutputSlotId)
                );

                sb.AppendLine("$precision4 {0} = packedWaterData.uv1;",
                    GetVariableNameForSlot(kUV1OutputSlotId)
                );
            }
            else
            {
                sb.AppendLine("$precision3 {0} = 0.0;",
                    GetVariableNameForSlot(kPositionOSOutputSlotId)
                );

                sb.AppendLine("$precision3 {0} = 0.0;",
                    GetVariableNameForSlot(kNormalOSOutputSlotId)
                );

                sb.AppendLine("$precision4 {0} = 0.0;",
                    GetVariableNameForSlot(kUV0OutputSlotId)
                );

                sb.AppendLine("$precision4 {0} = 0.0;",
                    GetVariableNameForSlot(kUV1OutputSlotId)
                );
            }
        }
    }
}
