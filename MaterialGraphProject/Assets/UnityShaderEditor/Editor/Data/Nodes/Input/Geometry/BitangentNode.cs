using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Geometry/Bitangent")]
    public class BitangentNode : GeometryNode, IMayRequireBitangent
    {
        public const int kOutputSlotId = 0;
        public const string kOutputSlotName = "Bitangent";

        public BitangentNode()
        {
            name = "Bitangent";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, new Vector4(0, 0, 1)));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }
        
        public override string GetVariableNameForSlot(int slotId)
        {
            return space.ToVariableName(InterpolatorType.BiTangent);
        }

        public NeededCoordinateSpace RequiresBitangent()
        {
            return space.ToNeededCoordinateSpace();
        }
    }
}
