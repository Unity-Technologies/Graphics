using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public interface IMayRequireBitangent
    {
        NeededCoordinateSpace RequiresBitangent();
    }

    [Title("Input/Geometry/World Bitangent")]
    public class BitangentNode : AbstractMaterialNode, IMayRequireBitangent
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
            AddSlot(new MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, SlotValueType.Vector3, new Vector4(0, 0, 1, 1)));
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
            return ShaderGeneratorNames.WorldSpaceBiTangent;
        }

        public NeededCoordinateSpace RequiresBitangent()
        {
            return NeededCoordinateSpace.World;
        }
    }
}
