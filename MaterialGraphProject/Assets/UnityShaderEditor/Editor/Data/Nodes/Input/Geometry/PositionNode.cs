using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Geometry/Position")]
    public class PositionNode : AbstractMaterialNode
    {
        const string kOutputSlotName = "XYZW";

        public const int OutputSlotId = 0;

        public PositionNode()
        {
            name = "Position";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero, ShaderStage.Vertex));
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] { OutputSlotId }; }
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return "v.vertex";
        }
    }
}
