using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Object")]
    sealed class ObjectNode : AbstractMaterialNode, IMayRequireTransform
    {
        const string kOutputSlotName = "Position";
        const string kOutputSlot1Name = "Scale";
        const string kOutputSlot2Name = "World Bounds Min";
        const string kOutputSlot3Name = "World Bounds Max";
        const string kOutputSlot4Name = "Bounds Size";

        public const int OutputSlotId = 0;
        public const int OutputSlot1Id = 1;
        public const int OutputSlot2Id = 2;
        public const int OutputSlot3Id = 3;
        public const int OutputSlot4Id = 4;

        public ObjectNode()
        {
            name = "Object";
            synonyms = new string[] { "position", "scale", "bounds", "size" };
            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(OutputSlot1Id, kOutputSlot1Name, kOutputSlot1Name, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(OutputSlot2Id, kOutputSlot2Name, kOutputSlot2Name, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(OutputSlot3Id, kOutputSlot3Name, kOutputSlot3Name, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(OutputSlot4Id, kOutputSlot4Name, kOutputSlot4Name, SlotType.Output, Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, OutputSlot1Id, OutputSlot2Id, OutputSlot3Id, OutputSlot4Id });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            switch (slotId)
            {
                case OutputSlot1Id:
                    return @"$precision3(length($precision3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length($precision3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length($precision3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))";
                case OutputSlot2Id:
                    return "SHADERGRAPH_RENDERER_BOUNDS_MIN";
                case OutputSlot3Id:
                    return "SHADERGRAPH_RENDERER_BOUNDS_MAX";
                case OutputSlot4Id:
                    return "(SHADERGRAPH_RENDERER_BOUNDS_MAX - SHADERGRAPH_RENDERER_BOUNDS_MIN)";
                default:
                    return "SHADERGRAPH_OBJECT_POSITION";
            }
        }

        public NeededTransform[] RequiresTransform(ShaderStageCapability stageCapability = ShaderStageCapability.All) => new[] { NeededTransform.ObjectToWorld };
    }
}
