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

        private const string kOutputSlotName = "SinTime";

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(kOutputSlotName, kOutputSlotName, SlotType.Output, 0, SlotValueType.Vector4, Vector4.one));
            RemoveSlotsNameNotMatching(new[] {kOutputSlotName});
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
