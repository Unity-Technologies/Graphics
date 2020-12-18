using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Geometry", "Vertex Color")]
    class VertexColorNode : AbstractMaterialNode, IMayRequireVertexColor
    {
        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public override bool hasPreview { get { return true; } }

        public VertexColorNode()
        {
            name = "Vertex Color";
            m_PreviewMode = PreviewMode.Preview3D;
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.one));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return string.Format("IN.{0}", ShaderGeneratorNames.VertexColor);
        }

        public bool RequiresVertexColor(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
