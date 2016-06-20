using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Dot Node")]
    public class DotNode : Function2Input
    {
        public DotNode()
        {
            name = "DotNode";
        }

        protected override string GetFunctionName()
        {
            return "dot";
        }

        protected override MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(GetInputSlot1Name(), GetInputSlot1Name(), SlotType.Input, 0, SlotValueType.Vector3, Vector4.zero);
        }

        protected override MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(GetInputSlot2Name(), GetInputSlot2Name(), SlotType.Input, 1, SlotValueType.Vector3, Vector4.zero);
        }

        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(GetOutputSlotName(), GetOutputSlotName(), SlotType.Output, 2, SlotValueType.Vector1, Vector4.zero);
        }
    }
}
