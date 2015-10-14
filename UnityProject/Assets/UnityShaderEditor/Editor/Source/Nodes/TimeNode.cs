using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Time/Time Node")]
    public class TimeNode : BaseMaterialNode, IRequiresTime
    {
        private const string kOutputSlotName = "Time";

        public override void Init()
        {
            base.Init();
            name = "Time";
            AddSlot(new Slot(SlotType.OutputSlot, kOutputSlotName));
        }

        public override string GetOutputVariableNameForSlot(Slot s, GenerationMode generationMode)
        {
            return "_Time";
        }
    }
}
