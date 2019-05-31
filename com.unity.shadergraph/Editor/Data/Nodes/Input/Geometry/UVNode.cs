using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Geometry", "UV")]
    class UVNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV
    {
        public const int OutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        [SerializeField]
        private UVChannel m_OutputChannel;

        [EnumControl("Channel")]
        public UVChannel uvChannel
        {
            get { return m_OutputChannel; }
            set
            {
                if (m_OutputChannel == value)
                    return;

                m_OutputChannel = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public override bool hasPreview { get { return true; } }

        public UVNode()
        {
            name = "UV";
            UpdateNodeAfterDeserialization();
        }


        public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector2.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode)
        {
            sb.AppendLine(string.Format("$precision4 {0} = IN.{1};", GetVariableNameForSlot(OutputSlotId), m_OutputChannel.GetUVName()));
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            return channel == uvChannel;
        }
    }
}
