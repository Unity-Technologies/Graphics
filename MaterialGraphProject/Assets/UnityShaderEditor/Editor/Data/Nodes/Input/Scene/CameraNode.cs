using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Scene/Camera")]
    public class CameraNode : AbstractMaterialNode
    {
        private const string kOutputSlotName = "Pos";
        private const string kOutputSlot2Name = "Dir";

        public const int OutputSlotId = 0;
        public const int OutputSlot2Id = 1;

        public CameraNode()
        {
            name = "Camera";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(OutputSlot2Id, kOutputSlot2Name, kOutputSlot2Name, SlotType.Output, Vector3.zero));
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] { OutputSlotId, OutputSlot2Id }; }
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            switch (slotId)
            {
                case OutputSlot2Id:
                    return "mul(unity_ObjectToWorld, UNITY_MATRIX_IT_MV [2].xyz)";
                default:
                    return "_WorldSpaceCameraPos";
            }
        }
    }
}
