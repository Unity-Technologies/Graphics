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
            return new MaterialSlot(InputSlot1Id, GetInputSlot1Name(), kInputSlot1ShaderName, SlotType.Input, SlotValueType.Vector3, Vector4.zero);
        }

        protected override MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(InputSlot2Id, GetInputSlot2Name(), kInputSlot2ShaderName, SlotType.Input, SlotValueType.Vector3, Vector4.zero);
        }

        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, SlotType.Output, SlotValueType.Vector1, Vector4.zero);
        }
    }
}
