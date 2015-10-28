using UnityEditor.Graphs;

namespace UnityEditor.MaterialGraph
{
    [Title("Time/Time Node")]
    public class TimeNode : BaseMaterialNode, IRequiresTime
    {
        private const string kOutputSlotName = "Time";

        public override void OnCreate()
        {
            base.OnCreate();
            name = "Time";
        }

        public override void OnEnable()
        {
            base.OnEnable();
            AddSlot(new MaterialGraphSlot(new Slot(SlotType.OutputSlot, kOutputSlotName), null));
        }

        public override string GetOutputVariableNameForSlot(Slot s, GenerationMode generationMode)
        {
            return "_Time";
        }
    }
}
