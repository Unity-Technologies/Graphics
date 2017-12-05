using UnityEngine;
using UnityEditor.Graphing;

/*namespace UnityEditor.ShaderGraph
{
    [Title("Vertex Interpolation")]
    public class VertexInterpolatorNode : AbstractMaterialNode
    {
        const string k_InputSlotName = "In";
        const string k_OutputSlotName = "Out";

        public const int InputSlotId = 0;
        public const int OutputSlotId = 1;

        public VertexInterpolatorNode()
        {
            name = "Position";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicVectorMaterialSlot(InputSlotId, k_InputSlotName, k_InputSlotName, SlotType.Input, Vector4.zero, ShaderStage.Vertex));
            AddSlot(new DynamicVectorMaterialSlot(OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, Vector4.zero, ShaderStage.Fragment));
            RemoveSlotsNameNotMatching(k_ValidSlots);
        }

        static readonly int[] k_ValidSlots = { InputSlotId, OutputSlotId };
    }
}*/
