using UnityEditor.Graphs;

namespace UnityEditor.MaterialGraph
{
    [Title("Time/Sin Time Node")]
    public class SinTimeNode : BaseMaterialNode, IRequiresTime
    {
        private const string kOutputSlotName = "SinTime";

        public override void OnCreate()
        {
            base.OnCreate();
            name = "Sin Time"; 
        }

        public override void OnEnable()
        {
            base.OnEnable();
            AddSlot(new MaterialGraphSlot(new Slot(SlotType.OutputSlot, kOutputSlotName), null));
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
