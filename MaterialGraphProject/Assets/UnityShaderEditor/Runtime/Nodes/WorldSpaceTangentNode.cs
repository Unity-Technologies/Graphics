using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public interface IMayRequireTangent
    {
        bool RequiresTangent();
    }

    [Title("Input/World Tangent Node")]
    public class WorldSpaceTangentNode : AbstractMaterialNode, IMayRequireTangent
    {
        public const int kOutputSlotId = 0;
        public const string kOutputSlotName = "Tangent";

        public WorldSpaceTangentNode()
        {
            name = "World Tangent";
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
            return ShaderGeneratorNames.WorldSpaceTangent;
        }

        public bool RequiresTangent()
        {
            return true;
        }
    }
}
