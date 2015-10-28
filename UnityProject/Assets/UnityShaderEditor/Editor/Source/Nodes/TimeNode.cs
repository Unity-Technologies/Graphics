using UnityEditor.Graphs;

namespace UnityEditor.MaterialGraph
{
    [Title("Time/Time Node")]
    public class TimeNode : BaseMaterialNode, IRequiresTime
    {
        private const string kOutputSlotName = "Time";
        private const string kOutputSlotNameX = "Time.x";
        private const string kOutputSlotNameY = "Time.y";
        private const string kOutputSlotNameZ = "Time.z";
        private const string kOutputSlotNameW = "Time.w";

        public override void OnCreate()
        {
            base.OnCreate();
            name = "Time";
        }

        public override void OnEnable()
        {
            base.OnEnable();
            AddSlot(new MaterialGraphSlot(new Slot(SlotType.OutputSlot, kOutputSlotName), SlotValueType.Vector4));
            AddSlot(new MaterialGraphSlot(new Slot(SlotType.OutputSlot, kOutputSlotNameX), SlotValueType.Vector1));
            AddSlot(new MaterialGraphSlot(new Slot(SlotType.OutputSlot, kOutputSlotNameY), SlotValueType.Vector1));
            AddSlot(new MaterialGraphSlot(new Slot(SlotType.OutputSlot, kOutputSlotNameZ), SlotValueType.Vector1));
            AddSlot(new MaterialGraphSlot(new Slot(SlotType.OutputSlot, kOutputSlotNameW), SlotValueType.Vector1));
        }

        public override string GetOutputVariableNameForSlot(Slot s, GenerationMode generationMode)
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
