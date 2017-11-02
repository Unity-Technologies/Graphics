using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public interface IMayRequireScreenPosition
    {
        bool RequiresScreenPosition();
    }

    [Title("Input/Scene Data/Screen Position")]
    public class ScreenPosNode : AbstractMaterialNode, IMayRequireScreenPosition
    {
        public ScreenPosNode()
        {
            name = "ScreenPosition";
            UpdateNodeAfterDeserialization();
        }

        private const int kOutputSlot1Id = 0;
        private const string kOutputSlot1Name = "Raw ScreenPos";
        private const int kOutputSlot2Id = 1;
        private const string kOutputSlot2Name = "Normalized";

        public override bool hasPreview { get { return true; } }
        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview2D; }
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(kOutputSlot1Id, kOutputSlot1Name, kOutputSlot1Name, SlotType.Output, Vector4.zero));
            AddSlot(new Vector3MaterialSlot(kOutputSlot2Id, kOutputSlot2Name, kOutputSlot2Name, SlotType.Output, Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlot1Id, kOutputSlot2Id });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            string returnString = "";
            switch (slotId)
            {
                case 0:
                    returnString = ShaderGeneratorNames.ScreenPosition;
                    break;
                case 1:
                    returnString = "float3(" + ShaderGeneratorNames.ScreenPosition + ".xy / " + ShaderGeneratorNames.ScreenPosition + ".w, 0)";
                    break;
            }
            return returnString;
        }

        public bool RequiresScreenPosition()
        {
            return true;
        }
    }
}
