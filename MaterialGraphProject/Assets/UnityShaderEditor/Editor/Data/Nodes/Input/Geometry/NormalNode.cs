using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Geometry/World Normal")]
    public class NormalNode : AbstractMaterialNode, IMayRequireNormal
    {
        public const int kOutputSlotId = 0;
        public const string kOutputSlotName = "Normal";

        public NormalNode()
        {
            name = "Normal";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, new Vector4(0, 0, 1)));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return CoordinateSpace.World.ToVariableName(InterpolatorType.Normal);
        }

        public NeededCoordinateSpace RequiresNormal()
        {
            return NeededCoordinateSpace.World;
        }
    }
}
