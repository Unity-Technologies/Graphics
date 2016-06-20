using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Normal Node")]
    public class NormalNode : AbstractMaterialNode
    {
        public NormalNode()
        {
            name = "Normal";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(kOutputSlotName, kOutputSlotName, SlotType.Output, 0, SlotValueType.Vector3, new Vector4(0,0,1,1)));
            RemoveSlotsNameNotMatching(new[] {kOutputSlotName});
        }

        private const string kOutputSlotName = "Normal";

        public override bool hasPreview
        {
            get { return true; }
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        public override string GetOutputVariableNameForSlot(MaterialSlot s)
        {
            return "o.Normal";
        }
    }
}
