using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Ambient")]
    public class AmbientNode : AbstractMaterialNode
    {
        const string kOutputSlotName = "Color";
        const string kOutputSlot1Name = "Sky";
        const string kOutputSlot2Name = "Equator";
        const string kOutputSlot3Name = "Ground";

        public const int OutputSlotId = 0;
        public const int OutputSlot1Id = 1;
        public const int OutputSlot2Id = 2;
        public const int OutputSlot3Id = 3;

        public AmbientNode()
        {
            name = "Ambient";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(OutputSlot1Id, kOutputSlot1Name, kOutputSlot1Name, SlotType.Output, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(OutputSlot2Id, kOutputSlot2Name, kOutputSlot2Name, SlotType.Output, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(OutputSlot3Id, kOutputSlot3Name, kOutputSlot3Name, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, OutputSlot1Id, OutputSlot2Id, OutputSlot3Id });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            switch (slotId)
            {
                case OutputSlot1Id:
                    return "unity_AmbientSky";
                case OutputSlot2Id:
                    return "unity_AmbientEquator";
                case OutputSlot3Id:
                    return "unity_AmbientGround";
                default:
                    return "UNITY_LIGHTMODEL_AMBIENT";
            }
        }
    }
}
