using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Camera")]
    public class CameraNode : AbstractMaterialNode
    {
        private const string kOutputSlotName = "Pos";
        private const string kOutputSlot1Name = "Dir";
        private const string kOutputSlot2Name = "Near";
        private const string kOutputSlot3Name = "Far";
        private const string kOutputSlot4Name = "Sign";

        public const int OutputSlotId = 0;
        public const int OutputSlot1Id = 1;
        public const int OutputSlot2Id = 2;
        public const int OutputSlot3Id = 3;
        public const int OutputSlot4Id = 4;

        public CameraNode()
        {
            name = "Camera";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(OutputSlot1Id, kOutputSlot1Name, kOutputSlot1Name, SlotType.Output, Vector3.zero));
            AddSlot(new Vector1MaterialSlot(OutputSlot2Id, kOutputSlot2Name, kOutputSlot2Name, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSlot3Id, kOutputSlot3Name, kOutputSlot3Name, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSlot4Id, kOutputSlot4Name, kOutputSlot4Name, SlotType.Output, 1));
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] { OutputSlotId, OutputSlot1Id, OutputSlot2Id, OutputSlot3Id, OutputSlot4Id }; }
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            switch (slotId)
            {
                case OutputSlot1Id:
                    return "mul(unity_ObjectToWorld, UNITY_MATRIX_IT_MV [2].xyz)";
                case OutputSlot2Id:
                    return "_ProjectionParams.y";
                case OutputSlot3Id:
                    return "_ProjectionParams.z";
                case OutputSlot4Id:
                    return "_ProjectionParams.x";
                default:
                    return "_WorldSpaceCameraPos";
            }
        }
    }
}
