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
        
        public override void AddSlot()
        {
            var index = GetInputSlots<ISlot>().Count();
            AddSlot(new MaterialSlot("Output" + index, "Output" + index, SlotType.Input, index, SlotValueType.Vector4, Vector4.zero));
        }

        public override void RemoveSlot()
        {
            var index = GetInputSlots<ISlot>().Count();
            if (index == 0)
                return;

            RemoveSlot("Output" + (index - 1));
        }
    }
}
