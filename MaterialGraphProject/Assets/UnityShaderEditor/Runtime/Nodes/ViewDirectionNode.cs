using System.ComponentModel;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    interface IMayRequireViewDirection
    {
        bool RequiresViewDirection();
    }

    [Title("Input/View Direction Node")]
    public class ViewDirectionNode : AbstractMaterialNode, IMayRequireViewDirection
    {
        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "ViewDirection";

        public override bool hasPreview { get { return true; } }
        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        public ViewDirectionNode()
        {
            name = "View Direction";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, SlotValueType.Vector3, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return "worldViewDir";
        }

        public bool RequiresViewDirection()
        {
            return true;
        }
    }
}
