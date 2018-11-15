using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Lighting", "Ambient")]
    class AmbientNode : AbstractMaterialNode
    {
        const string kOutputSlotName = "Color/Sky";
        const string kOutputSlot1Name = "Equator";
        const string kOutputSlot2Name = "Ground";

        public const int OutputSlotId = 0;
        public const int OutputSlot1Id = 1;
        public const int OutputSlot2Id = 2;

        public AmbientNode()
        {
            name = "Ambient";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Ambient-Node"; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new ColorRGBMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero, ColorMode.Default));
            AddSlot(new ColorRGBMaterialSlot(OutputSlot1Id, kOutputSlot1Name, kOutputSlot1Name, SlotType.Output, Vector4.zero, ColorMode.Default));
            AddSlot(new ColorRGBMaterialSlot(OutputSlot2Id, kOutputSlot2Name, kOutputSlot2Name, SlotType.Output, Vector4.zero, ColorMode.Default));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, OutputSlot1Id, OutputSlot2Id });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            switch (slotId)
            {
                case OutputSlot1Id:
                    return "SHADERGRAPH_AMBIENT_EQUATOR";
                case OutputSlot2Id:
                    return "SHADERGRAPH_AMBIENT_GROUND";
                default:
                    return "SHADERGRAPH_AMBIENT_SKY";
            }
        }
    }
}
