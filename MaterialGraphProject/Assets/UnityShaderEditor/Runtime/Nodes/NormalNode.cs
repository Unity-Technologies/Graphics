using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Normal Node")]
    public class NormalNode : AbstractMaterialNode
    {
        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Normal";

        public NormalNode()
        {
            name = "Normal";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, SlotValueType.Vector3, new Vector4(0, 0, 1, 1)));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }


        public override bool hasPreview
        {
            get { return true; }
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        public override string GetVariableNameForSlot(MaterialSlot s)
        {
            return "o.Normal";
        }
    }
}
