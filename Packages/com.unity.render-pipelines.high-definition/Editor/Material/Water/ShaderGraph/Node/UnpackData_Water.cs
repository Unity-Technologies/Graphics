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
    [Title("Utility", "High Definition Render Pipeline", "Water", "UnpackData_Water")]
    class UnpackData_Water : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV
    {
        public UnpackData_Water()
        {
            name = "Unpack Water Data";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("UnpackData_Water");

        const int kLowFrequencyHeightOutputSlotId = 0;
        const string kLowFrequencyHeightSlotName = "LowFrequencyHeight";

        const int kDisplacementOutputSlotId = 1;
        const string kDisplacementSlotName = "Displacement";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Outputs
            AddSlot(new Vector1MaterialSlot(kLowFrequencyHeightOutputSlotId, kLowFrequencyHeightSlotName, kLowFrequencyHeightSlotName, SlotType.Output, 0));
            AddSlot(new Vector3MaterialSlot(kDisplacementOutputSlotId, kDisplacementSlotName, kDisplacementSlotName, SlotType.Output, Vector3.zero));

            RemoveSlotsNameNotMatching(new[]
            {
                // Outputs
                kLowFrequencyHeightOutputSlotId,
                kDisplacementOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            // See PackWaterVertexData

            // Low Frequency Height
            sb.AppendLine("$precision {0} = saturate(IN.{1}.z);",
                GetVariableNameForSlot(kLowFrequencyHeightOutputSlotId),
                ShaderGeneratorNames.GetUVName(UVChannel.UV0)
            );

            // Displacement
            sb.AppendLine("$precision3 {0} = float3(IN.{1}.w, IN.{1}.z, IN.{2}.w);",
                GetVariableNameForSlot(kDisplacementOutputSlotId),
                ShaderGeneratorNames.GetUVName(UVChannel.UV0), 
                ShaderGeneratorNames.GetUVName(UVChannel.UV1) 
            );
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            return channel == UVChannel.UV1;
        }
    }
}
