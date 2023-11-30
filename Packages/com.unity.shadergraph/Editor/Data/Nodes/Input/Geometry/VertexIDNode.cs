using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Geometry", "Vertex ID")]
    class VertexIDNode : AbstractMaterialNode, IMayRequireVertexID
    {
        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public override bool hasPreview { get { return false; } }

        public VertexIDNode()
        {
            name = "Vertex ID";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, (int)0, ShaderStageCapability.Vertex));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return string.Format("IN.{0}", ShaderGeneratorNames.VertexID);
        }

        public bool RequiresVertexID(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
