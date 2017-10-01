using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public class SubGraphOutputNode : AbstractSubGraphIONode
    {
        public SubGraphOutputNode()
        {
            name = "SubGraphOutputs";
        }

        public override int AddSlot()
        {
            var index = GetInputSlots<ISlot>().Count() + 1;
            AddSlot(new MaterialSlot(index, "Output " + index, "Output" + index, SlotType.Input, SlotValueType.Vector4, Vector4.zero));
            return index;
        }

        public override void RemoveSlot()
        {
            var index = GetInputSlots<ISlot>().Count();
            if (index == 0)
                return;

            RemoveSlot(index);
        }

        public override bool allowedInRemapGraph { get; } = false;
    }
}
