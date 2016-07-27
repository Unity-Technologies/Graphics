using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Sine Time Node")]
    public class SinTimeNode : AbstractMaterialNode, IRequiresTime
    {
        public SinTimeNode()
        {
            name = "Sine Time";
            UpdateNodeAfterDeserialization();
        }

        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "SinTime";

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, SlotValueType.Vector4, Vector4.one));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }
        
        public override bool hasPreview
        {
            get { return true; }
        }

        public override string GetVariableNameForSlot(MaterialSlot s)
        {
            return "_SinTime";
        }
    }
}
