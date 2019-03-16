using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Object")]
    sealed class ObjectNode : AbstractMaterialNode
    {
        const string kOutputSlotName = "Position";
        const string kOutputSlot1Name = "Scale";

        public const int OutputSlotId = 0;
        public const int OutputSlot1Id = 1;

        public ObjectNode()
        {
            name = "Object";
            UpdateNodeAfterDeserialization();
        }


        public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(OutputSlot1Id, kOutputSlot1Name, kOutputSlot1Name, SlotType.Output, Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, OutputSlot1Id });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            switch (slotId)
            {
                case OutputSlot1Id:
                    return @"float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)))";
                default:
                    return "SHADERGRAPH_OBJECT_POSITION";
            }
        }
    }
}
