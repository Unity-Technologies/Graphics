
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

        public TimeNode()
        {
            name = "Time";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(kOutputSlotName, kOutputSlotName, SlotType.Output, 0, SlotValueType.Vector4, Vector4.zero));
            AddSlot(new MaterialSlot(kOutputSlotNameX, kOutputSlotNameX, SlotType.Output, 1, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(kOutputSlotNameY, kOutputSlotNameY, SlotType.Output, 2, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(kOutputSlotNameZ, kOutputSlotNameZ, SlotType.Output, 3, SlotValueType.Vector1, Vector4.zero));
            AddSlot(new MaterialSlot(kOutputSlotNameW, kOutputSlotNameW, SlotType.Output, 4, SlotValueType.Vector1, Vector4.zero));
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected string[] validSlots
        {
            get { return new[] {kOutputSlotName, kOutputSlotNameX, kOutputSlotNameY, kOutputSlotNameZ, kOutputSlotName}; }
        }

        public override string GetOutputVariableNameForSlot(MaterialSlot s)
        {
            switch (s.name)
            {
                case kOutputSlotNameX:
                    return "_Time.x";
                case kOutputSlotNameY:
                    return "_Time.y";
                case kOutputSlotNameZ:
                    return "_Time.z";
                case kOutputSlotNameW:
                    return "_Time.w";
                default:
                    return "_Time";
            }
        }
    }
}
