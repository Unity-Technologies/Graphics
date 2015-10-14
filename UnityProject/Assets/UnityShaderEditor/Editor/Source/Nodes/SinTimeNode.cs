using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Time/Sin Time Node")]
    public class SinTimeNode : BaseMaterialNode, IRequiresTime
    {
        private const string kOutputSlotName = "SinTime";

        public override void Init()
        {
            base.Init();
            name = "Sin Time";
            AddSlot(new Slot(SlotType.OutputSlot, kOutputSlotName));
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        public override string GetOutputVariableNameForSlot(Slot s, GenerationMode generationMode)
        {
            return "_SinTime";
        }
    }
}
