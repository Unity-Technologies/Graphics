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
    class ComputeVertexData_Water : AbstractMaterialNode, IGeneratesBodyCode, IMayRequirePosition, IMayRequireNormal
    {
        public ComputeVertexData_Water()
        {
            name = "Compute Water Vertex Data (Legacy)";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL => Documentation.GetPageLink("ComputeVertexData_Water");

        const int kPositionOSOutputSlotId = 0;
        const string kPositionOSOutputSlotName = "PositionOS";

        const int kNormalOSOutputSlotId = 1;
        const string kNormalOSOutputSlotName = "NormalOS";

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(kPositionOSOutputSlotId, kPositionOSOutputSlotName, kPositionOSOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(kNormalOSOutputSlotId, kNormalOSOutputSlotName, kNormalOSOutputSlotName, SlotType.Output, Vector3.zero));

            RemoveSlotsNameNotMatching(new[]
            {
                kPositionOSOutputSlotId,
                kNormalOSOutputSlotId,
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.ForReals)
            {
                sb.AppendLine("$precision3 {0} = IN.ObjectSpacePosition;",
                  GetVariableNameForSlot(kPositionOSOutputSlotId),
                  CoordinateSpace.Object.ToVariableName(InterpolatorType.Position));

                sb.AppendLine("$precision3 {0} = IN.ObjectSpaceNormal;",
                  GetVariableNameForSlot(kNormalOSOutputSlotId),
                  CoordinateSpace.Object.ToVariableName(InterpolatorType.Position));
            }
            else
            {
                sb.AppendLine("$precision3 {0} = 0.0;",
                 GetVariableNameForSlot(kPositionOSOutputSlotId));

                sb.AppendLine("$precision3 {0} = 0.0;",
                    GetVariableNameForSlot(kNormalOSOutputSlotId));
            }
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability = ShaderStageCapability.Vertex)
        {
            return NeededCoordinateSpace.World;
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability = ShaderStageCapability.Vertex)
        {
            return NeededCoordinateSpace.World;
        }

        public override void ValidateNode()
        {
            owner.messageManager?.AddOrAppendError(owner, objectId, new ShaderMessage("This node is deprecated and will be released in a future version. Please refer to the Water Samples for the new version.", ShaderCompilerMessageSeverity.Warning));
        }
    }
}
