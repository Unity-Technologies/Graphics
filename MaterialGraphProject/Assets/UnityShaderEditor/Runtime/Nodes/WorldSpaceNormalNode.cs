using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public interface IMayRequireNormal
    {
        bool RequiresNormal();
    }

    [Title("Input/World Normal Node")]
    public class WorldSpaceNormalNode : AbstractMaterialNode, IMayRequireNormal
    {
        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Normal";

        public WorldSpaceNormalNode()
        {
            name = "World Normal";
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
            return ShaderGeneratorNames.WorldSpaceNormal;
        }

        public bool RequiresNormal()
        {
            return true;
        }
    }
}
