
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Time Node")]
    public class TimeNode : AbstractMaterialNode, IRequiresTime
    {
        private const string kOutputSlotName = "Time";
        private const string kOutputSlotNameX = "Time.x";
        private const string kOutputSlotNameY = "Time.y";
        private const string kOutputSlotNameZ = "Time.z";
        private const string kOutputSlotNameW = "Time.w";

        private const int kOutputSlotId = 0;
        private const int kOutputSlotIdX = 1;
        private const int kOutputSlotIdY = 2;
        private const int kOutputSlotIdZ = 3;
        private const int kOutputSlotIdW = 4;

        public TimeNode()
        {
            name = "Time";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, SlotValueType.Vector4, Vector4.zero));
            AddSlot(new MaterialSlot(kOutputSlotIdX, kOutputSlotNameX, kOutputSlotNameX, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(kOutputSlotIdY, kOutputSlotNameY, kOutputSlotNameY, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(kOutputSlotIdZ, kOutputSlotNameZ, kOutputSlotNameZ, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(kOutputSlotIdW, kOutputSlotNameW, kOutputSlotNameW, SlotType.Output, SlotValueType.Vector1, Vector4.zero));
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] {kOutputSlotId, kOutputSlotIdX, kOutputSlotIdY, kOutputSlotIdZ, kOutputSlotId}; }
        }

        public override string GetVariableNameForSlot(MaterialSlot s)
        {
            switch (s.id)
            {
                case kOutputSlotIdX:
                    return "_Time.x";
                case kOutputSlotIdY:
                    return "_Time.y";
                case kOutputSlotIdZ:
                    return "_Time.z";
                case kOutputSlotIdW:
                    return "_Time.w";
                default:
                    return "_Time";
            }
        }
    }
}
