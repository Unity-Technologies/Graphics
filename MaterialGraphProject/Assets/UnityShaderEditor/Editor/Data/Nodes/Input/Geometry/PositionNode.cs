using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Geometry", "Position")]
    public class PositionNode : GeometryNode, IMayRequirePosition
    {
        private const int kOutputSlotId = 0;
        public const string kOutputSlotName = "Out";


        public PositionNode()
        {
            name = "Position";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(
                    kOutputSlotId,
                    kOutputSlotName,
                    kOutputSlotName,
                    SlotType.Output,
                    Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return space.ToVariableName(InterpolatorType.Position);
        }

        public NeededCoordinateSpace RequiresPosition()
        {
            return space.ToNeededCoordinateSpace();
        }
    }
}
