using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Geometry/Vertex Position")]
    public class VertexPositionNode : AbstractMaterialNode
    {
        private const string kOutputSlotName = "XYZW";
        private const string kOutputSlotNameXYZ = "XYZ";
        private const string kOutputSlotNameX = "X";
        private const string kOutputSlotNameY = "Y";
        private const string kOutputSlotNameZ = "Z";
        private const string kOutputSlotNameW = "W";

        public const int OutputSlotId = 0;
        public const int OutputSlotIdXYZ = 1;
        public const int OutputSlotIdX = 2;
        public const int OutputSlotIdY = 3;
        public const int OutputSlotIdZ = 4;
        public const int OutputSlotIdW = 5;

        public VertexPositionNode()
        {
            name = "VertexPostion";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, SlotValueType.Vector4, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotIdXYZ, kOutputSlotNameXYZ, kOutputSlotNameXYZ, SlotType.Output, SlotValueType.Vector3, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotIdX, kOutputSlotNameX, kOutputSlotNameX, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotIdY, kOutputSlotNameY, kOutputSlotNameY, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotIdZ, kOutputSlotNameZ, kOutputSlotNameZ, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(OutputSlotIdW, kOutputSlotNameW, kOutputSlotNameW, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] { OutputSlotId, OutputSlotIdXYZ ,OutputSlotIdX, OutputSlotIdY, OutputSlotIdZ, OutputSlotIdW }; }
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            switch (slotId)
            {
                case OutputSlotIdXYZ:
                    return "v.vertex.xyz";
                case OutputSlotIdX:
                    return "v.vertex.x";
                case OutputSlotIdY:
                    return "v.vertex.y";
                case OutputSlotIdZ:
                    return "v.vertex.z";
                case OutputSlotIdW:
                    return "v.vertex.w";
                default:
                    return "v.vertex";
            }
        }
    }
}
