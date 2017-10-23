using System.ComponentModel;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    interface IMayRequireViewDirection
    {
        NeededCoordinateSpace RequiresViewDirection();
    }

    [Title("Input/Geometry/View Direction")]
    public class ViewDirectionNode : AbstractMaterialNode, IMayRequireViewDirection
    {
        private const int kOutputSlotId = 0;

        public override bool hasPreview { get { return true; } }
        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        public ViewDirectionNode()
        {
            name = "ViewDirection";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(
                    kOutputSlotId,
                    CoordinateSpace.World.ToVariableName(InterpolatorType.ViewDirection),
                    CoordinateSpace.World.ToVariableName(InterpolatorType.ViewDirection),
                    SlotType.Output,
                    Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return CoordinateSpace.World.ToVariableName(InterpolatorType.ViewDirection);
        }

        public NeededCoordinateSpace RequiresViewDirection()
        {
            return NeededCoordinateSpace.World;
        }
    }
}
