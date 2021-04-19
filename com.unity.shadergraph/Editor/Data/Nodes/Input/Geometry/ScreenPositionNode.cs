using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Geometry", "Screen Position")]
    class ScreenPositionNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireScreenPosition
    {
        public ScreenPositionNode()
        {
            name = "Screen Position";
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        private ScreenSpaceType m_ScreenSpaceType = ScreenSpaceType.Default;

        [EnumControl("Mode")]
        public ScreenSpaceType screenSpaceType
        {
            get { return m_ScreenSpaceType; }
            set
            {
                if (m_ScreenSpaceType == value)
                    return;

                m_ScreenSpaceType = value;
                Dirty(ModificationScope.Graph);
            }
        }

        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public override bool hasPreview { get { return true; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine(string.Format("$precision4 {0} = {1};", GetVariableNameForSlot(kOutputSlotId), m_ScreenSpaceType.ToValueAsVariable()));
        }

        public bool RequiresScreenPosition(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
