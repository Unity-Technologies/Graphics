using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Geometry", "Screen Position")]
    class ScreenPositionNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireScreenPosition, IMayRequireNDCPosition, IMayRequirePixelPosition
    {
        public ScreenPositionNode()
        {
            name = "Screen Position";
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        private ScreenSpaceType m_ScreenSpaceType = ScreenSpaceType.Default;

        // Note that this enum is truly public, in a sense, as it is used to drive the dropdown control
        // on the node.
        public enum Mode
        {
            Default = (int)ScreenSpaceType.Default,
            Raw = (int)ScreenSpaceType.Raw,
            Center = (int)ScreenSpaceType.Center,
            Tiled = (int)ScreenSpaceType.Tiled,
            Pixel = (int)ScreenSpaceType.Pixel,
        }

        [EnumControl("Mode")]
        public Mode mode
        {
            get { return ScreenPositionNodeModeExtensions.AsMode(m_ScreenSpaceType); }
            set
            {
                var screenSpaceValue = value.AsScreenSpaceType();
                if (m_ScreenSpaceType == screenSpaceValue)
                    return;

                m_ScreenSpaceType = screenSpaceValue;
                Dirty(ModificationScope.Graph);
            }
        }

        public ScreenSpaceType screenSpaceType => m_ScreenSpaceType;

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

        bool IMayRequireScreenPosition.RequiresScreenPosition(ShaderStageCapability stageCapability)
        {
            return m_ScreenSpaceType.RequiresScreenPosition();
        }

        bool IMayRequireNDCPosition.RequiresNDCPosition(ShaderStageCapability stageCapability)
        {
            return m_ScreenSpaceType.RequiresNDCPosition();
        }

        bool IMayRequirePixelPosition.RequiresPixelPosition(ShaderStageCapability stageCapability)
        {
            return m_ScreenSpaceType.RequiresPixelPosition();
        }
    }

    static class ScreenPositionNodeModeExtensions
    {
        public static ScreenSpaceType AsScreenSpaceType(this ScreenPositionNode.Mode mode)
        {
            return (ScreenSpaceType)mode;
        }

        public static ScreenPositionNode.Mode AsMode(ScreenSpaceType type)
        {
            if (System.Enum.IsDefined(typeof(ScreenPositionNode.Mode), (int)type))
                return (ScreenPositionNode.Mode)type;
            else
                return ScreenPositionNode.Mode.Default;
        }
    }
}
