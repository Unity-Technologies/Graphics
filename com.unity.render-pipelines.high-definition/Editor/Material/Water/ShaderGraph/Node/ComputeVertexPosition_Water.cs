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
    class ComputeVertexPosition_Water : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireVertexID, IMayRequirePosition
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
                sb.AppendLine("#if defined(WATER_PROCEDURAL_GEOMETRY)");
                sb.AppendLine("$precision3 {0} = GetVertexPositionFromVertexID(IN.{1});",
                  GetVariableNameForSlot(kPositionWSOutputSlotId),
                  ShaderGeneratorNames.VertexID);
                sb.AppendLine("#else");
                sb.AppendLine("$precision3 {0} = GetVertexPositionFromVertexID(IN.{1}, IN.{2});",
                  GetVariableNameForSlot(kPositionWSOutputSlotId),
                  ShaderGeneratorNames.VertexID,
                  CoordinateSpace.Object.ToVariableName(InterpolatorType.Position));
                sb.AppendLine("#endif");
            }
            else
            {
                sb.AppendLine("$precision3 {0} = 0.0;",
                 GetVariableNameForSlot(kPositionWSOutputSlotId));
            }
        }

        public bool RequiresVertexID(ShaderStageCapability stageCapability = ShaderStageCapability.Vertex)
        {
            return true;
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability = ShaderStageCapability.Vertex)
        {
            return NeededCoordinateSpace.Object;
        }
    }
}
