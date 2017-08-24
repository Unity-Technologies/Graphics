using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    interface IMayRequireWorldPosition
    {
        bool RequiresWorldPosition();
    }

    [Title("Input/Geometry/World Space Position")]
    public class WorldSpacePositionNode : AbstractMaterialNode, IMayRequireWorldPosition
    {
        private const int kOutputSlotId = 0;

        public override bool hasPreview { get { return true; } }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        public WorldSpacePositionNode()
        {
            name = "WorldSpacePosition";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(
                    kOutputSlotId,
                    ShaderGeneratorNames.WorldSpacePosition,
                    ShaderGeneratorNames.WorldSpacePosition,
                    SlotType.Output,
                    SlotValueType.Vector3,
                    Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return ShaderGeneratorNames.WorldSpacePosition;
        }

        public bool RequiresWorldPosition()
        {
            return true;
        }
    }
}
