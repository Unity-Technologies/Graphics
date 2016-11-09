using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public interface IMayRequireScreenPosition
    {
        bool RequiresScreenPosition();
    }

    [Title("Input/Screen Pos Node")]
    public class ScreenPosNode : AbstractMaterialNode, IMayRequireScreenPosition
    {
        public ScreenPosNode()
        {
            name = "ScreenPos";
            UpdateNodeAfterDeserialization();
        }

        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "ScreenPos";

        public override bool hasPreview { get { return true; } }
        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview2D; }
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, SlotValueType.Vector4, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return ShaderGeneratorNames.ScreenPosition;
        }

        public bool RequiresScreenPosition()
        {
            return true;
        }
    }
}
