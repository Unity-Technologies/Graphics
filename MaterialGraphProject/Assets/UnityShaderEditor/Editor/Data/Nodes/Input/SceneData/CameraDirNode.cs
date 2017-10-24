using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Scene Data/Camera Direction")]
    public class CamDirNode : AbstractMaterialNode
    {
        public CamDirNode()
        {
            name = "CameraDirection";
            UpdateNodeAfterDeserialization();
        }

        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Output";

        public override bool hasPreview { get { return true; } }
        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview2D; }
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return "UNITY_MATRIX_IT_MV [2].xyz";
        }
    }
}
