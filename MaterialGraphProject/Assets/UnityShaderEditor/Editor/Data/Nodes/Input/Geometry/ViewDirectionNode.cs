using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Geometry/View Direction")]
    public class ViewDirectionNode : GeometryNode, IMayRequireViewDirection
    {
        private const int kOutputSlotId = 0;
        public const string kOutputSlotName = "ViewDirection";

        public ViewDirectionNode()
        {
            name = "ViewDirection";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(
                    kOutputSlotId,
                    kOutputSlotName,
                    kOutputSlotName,
                    SlotType.Output,
                    Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return space.ToVariableName(InterpolatorType.ViewDirection);
        }

        public NeededCoordinateSpace RequiresViewDirection()
        {
            return space.ToNeededCoordinateSpace();
        }
    }
}
