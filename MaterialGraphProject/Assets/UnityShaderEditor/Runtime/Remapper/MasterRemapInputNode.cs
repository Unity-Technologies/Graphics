using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Remapper/Remap Input Node")]
    public class MasterRemapInputNode : AbstractSubGraphIONode
    {
        public MasterRemapInputNode()
        {
            name = "Inputs";
        }

        public override int AddSlot()
        {
            var index = GetInputSlots<ISlot>().Count() + 1;
            AddSlot(new MaterialSlot(index, "Output " + index, "Output" + index, SlotType.Output, SlotValueType.Vector4, Vector4.zero));
            return index;
        }

        public override void RemoveSlot()
        {
            var index = GetInputSlots<ISlot>().Count();
            if (index == 0)
                return;

            RemoveSlot(index);
        }
    }
}
