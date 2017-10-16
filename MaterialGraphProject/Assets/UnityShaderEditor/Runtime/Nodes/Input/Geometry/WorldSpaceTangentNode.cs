using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public interface IMayRequireTangent
    {
        NeededCoordinateSpace RequiresTangent();
    }

    [Title("Input/Geometry/World Tangent")]
    public class TangentNode : AbstractMaterialNode, IMayRequireTangent
    {
        public const int kOutputSlotId = 0;
        public const string kOutputSlotName = "Tangent";

        public TangentNode()
        {
            name = "Tangent";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, new Vector4(0, 0, 1, 1)));
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
            return ShaderGeneratorNames.ObjectSpaceTangent;
        }

        public NeededCoordinateSpace RequiresTangent()
        {
            return NeededCoordinateSpace.Object;
        }
    }
}
