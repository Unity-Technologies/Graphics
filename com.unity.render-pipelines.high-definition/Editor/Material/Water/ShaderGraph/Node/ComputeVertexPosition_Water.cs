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
    [Title("Utility", "High Definition Render Pipeline", "Water", "ComputeVertexPosition_Water (Preview)")]
    class ComputeVertexPosition_Water : AbstractMaterialNode, IGeneratesBodyCode, IMayRequirePosition
    {
        public ComputeVertexPosition_Water()
        {
            name = "Compute Water Vertex Position (Preview)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("ComputeVertexPosition_Water");

        const int kPositionWSOutputSlotId = 0;
        const string kPositionWSOutputSlotName = "PositionWS";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(kPositionWSOutputSlotId, kPositionWSOutputSlotName, kPositionWSOutputSlotName, SlotType.Output, Vector3.zero));

            RemoveSlotsNameNotMatching(new[]
            {
                kPositionWSOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                sb.AppendLine("$precision3 {0} = GetWaterVertexPosition(IN.WorldSpacePosition);",
                  GetVariableNameForSlot(kPositionWSOutputSlotId),
                  CoordinateSpace.Object.ToVariableName(InterpolatorType.Position));
            }
            else
            {
                sb.AppendLine("$precision3 {0} = 0.0;",
                 GetVariableNameForSlot(kPositionWSOutputSlotId));
            }
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability = ShaderStageCapability.Vertex)
        {
            return NeededCoordinateSpace.World;
        }
    }
}
