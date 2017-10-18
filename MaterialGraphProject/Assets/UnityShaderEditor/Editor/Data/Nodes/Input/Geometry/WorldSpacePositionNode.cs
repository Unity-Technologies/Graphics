using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    interface IMayRequirePosition
    {
        NeededCoordinateSpace RequiresPosition();
    }

    [Title("Input/Geometry/World Space Position")]
    public class WorldSpacePositionNode : AbstractMaterialNode, IMayRequirePosition
    {
        private const int kOutputSlotId = 0;

        public override bool hasPreview { get { return true; } }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        public WorldSpacePositionNode()
        {
            name = "Position";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(
                    kOutputSlotId,
                    ShaderGeneratorNames.WorldSpacePosition,
                    ShaderGeneratorNames.WorldSpacePosition,
                    SlotType.Output,
                    Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return ShaderGeneratorNames.WorldSpacePosition;
        }

        public NeededCoordinateSpace RequiresPosition()
        {
            return NeededCoordinateSpace.World;
        }
    }
}
