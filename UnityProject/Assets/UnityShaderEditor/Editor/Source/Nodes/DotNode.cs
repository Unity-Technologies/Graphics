using UnityEditor.Graphs;

namespace UnityEditor.MaterialGraph
{
    [Title("Math/Dot Node")]
    class DotNode : Function2Input
    {
        public override void OnCreate()
        {
            name = "DotNode";
            base.OnCreate();;
        }

        protected override string GetFunctionName() { return "dot"; }
        protected override MaterialGraphSlot GetInputSlot1()
        {
            var slot = new Slot(SlotType.InputSlot, GetInputSlot1Name());
            return new MaterialGraphSlot(slot, SlotValueType.Vector3);
        }

        protected override MaterialGraphSlot GetInputSlot2()
        {
            var slot = new Slot(SlotType.InputSlot, GetInputSlot2Name());
            return new MaterialGraphSlot(slot, SlotValueType.Vector3);
        }

        protected override MaterialGraphSlot GetOutputSlot()
        {
            var slot = new Slot(SlotType.OutputSlot, GetOutputSlotName());
            return new MaterialGraphSlot(slot, SlotValueType.Vector1);
        }
    }
}
