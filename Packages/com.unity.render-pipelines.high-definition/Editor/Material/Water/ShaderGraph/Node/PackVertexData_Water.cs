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
    class PackVertexData_Water : AbstractMaterialNode, IGeneratesBodyCode
    {
        public PackVertexData_Water()
        {
            name = "Pack Water Vertex Data (Legacy)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("PackVertexData_Water");

        // Inputs
        const int kPositionOSInputSlotId = 0;
        const string kPositionOSInputSlotName = "PositionOS";

        // Inputs
        const int kNormalOSInputSlotId = 1;
        const string kNormalOSInputSlotName = "NormalOS";

        const int kDisplacementInputSlotId = 2;
        const string kDisplacementInputSlotName = "Displacement";

        const int kLowFrequencyHeightInputSlotId = 3;
        const string kLowFrequencyHeightInputSlotName = "LowFrequencyHeight";

        // Outputs
        const int kPositionOSOutputSlotId = 4;
        const string kPositionOSOutputSlotName = "PositionOS";

        const int kNormalOSOutputSlotId = 5;
        const string kNormalOSOutputSlotName = "NormalOS";

        const int kUV0OutputSlotId = 6;
        const string kUV0OutputSlotName = "uv0";

        const int kUV1OutputSlotId = 7;
        const string kUV1OutputSlotName = "uv1";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Inputs
            AddSlot(new Vector3MaterialSlot(kPositionOSInputSlotId, kPositionOSInputSlotName, kPositionOSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Vertex));
            AddSlot(new Vector3MaterialSlot(kNormalOSInputSlotId, kNormalOSInputSlotName, kNormalOSInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Vertex));
            AddSlot(new Vector3MaterialSlot(kDisplacementInputSlotId, kDisplacementInputSlotName, kDisplacementInputSlotName, SlotType.Input, Vector3.zero, ShaderStageCapability.Vertex));
            AddSlot(new Vector1MaterialSlot(kLowFrequencyHeightInputSlotId, kLowFrequencyHeightInputSlotName, kLowFrequencyHeightInputSlotName, SlotType.Input, 0, ShaderStageCapability.Vertex));

            // Outputs
            AddSlot(new Vector3MaterialSlot(kPositionOSOutputSlotId, kPositionOSOutputSlotName, kPositionOSOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(kNormalOSOutputSlotId, kNormalOSOutputSlotName, kNormalOSOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector4MaterialSlot(kUV0OutputSlotId, kUV0OutputSlotName, kUV0OutputSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(kUV1OutputSlotId, kUV1OutputSlotName, kUV1OutputSlotName, SlotType.Output, Vector4.zero));

            RemoveSlotsNameNotMatching(new[]
            {
                kPositionOSInputSlotId,
                kNormalOSInputSlotId,
                kDisplacementInputSlotId,
                kLowFrequencyHeightInputSlotId,

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
                string positionOS = GetSlotValue(kPositionOSInputSlotId, generationMode);
                string normalOS = GetSlotValue(kNormalOSInputSlotId, generationMode);
                string displacement = GetSlotValue(kDisplacementInputSlotId, generationMode);
                string lowFrequencyHeight = GetSlotValue(kLowFrequencyHeightInputSlotId, generationMode);

                sb.AppendLine("$precision3 {0} = {1};",
                    GetVariableNameForSlot(kPositionOSOutputSlotId),
                    positionOS
                );

                sb.AppendLine("$precision3 {0} = {1};",
                    GetVariableNameForSlot(kNormalOSOutputSlotId),
                    normalOS
                );

                sb.AppendLine("$precision4 {0} = float4({1}.xyz, 0.0);",
                    GetVariableNameForSlot(kUV0OutputSlotId),
                    displacement
                );

                sb.AppendLine("$precision4 {0} = float4({1}, 0.0, 0.0, 0.0);",
                    GetVariableNameForSlot(kUV1OutputSlotId),
                    lowFrequencyHeight
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

        public override void ValidateNode()
        {
            owner.messageManager?.AddOrAppendError(owner, objectId, new ShaderMessage("This node is deprecated and will be released in a future version. Please refer to the Water Samples for the new version.", ShaderCompilerMessageSeverity.Warning));
        }
    }
}
