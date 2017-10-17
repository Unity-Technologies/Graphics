using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Geometry/Vertex Normal")]
    public class VertexNormalNode : AbstractMaterialNode
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

        public VertexNormalNode()
        {
            name = "VertexNormal";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new Vector3MaterialSlot(OutputSlotIdXYZ, kOutputSlotNameXYZ, kOutputSlotNameXYZ, SlotType.Output,  Vector4.zero));
            AddSlot(new Vector1MaterialSlot(OutputSlotIdX, kOutputSlotNameX, kOutputSlotNameX, SlotType.Output,0));
            AddSlot(new Vector1MaterialSlot(OutputSlotIdY, kOutputSlotNameY, kOutputSlotNameY, SlotType.Output,0));
            AddSlot(new Vector1MaterialSlot(OutputSlotIdZ, kOutputSlotNameZ, kOutputSlotNameZ, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSlotIdW, kOutputSlotNameW, kOutputSlotNameW, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] { OutputSlotId, OutputSlotIdXYZ, OutputSlotIdX, OutputSlotIdY, OutputSlotIdZ, OutputSlotIdW }; }
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            switch (slotId)
            {
                case OutputSlotIdXYZ:
                    return "v.normal.xyz";
                case OutputSlotIdX:
                    return "v.normal.x";
                case OutputSlotIdY:
                    return "v.normal.y";
                case OutputSlotIdZ:
                    return "v.normal.z";
                case OutputSlotIdW:
                    return "v.normal.w";
                default:
                    return "v.normal";
            }
        }
    }
}
