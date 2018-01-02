using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    public enum ScreenSpaceType
    {
        Default,
        Raw,
        Center,
        Tiled
    };

    [Title("Input", "Geometry", "Screen Position")]
    public class ScreenPositionNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireScreenPosition
    {
        public ScreenPositionNode()
        {
            name = "Screen Position";
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        private ScreenSpaceType m_ScreenSpaceType = ScreenSpaceType.Default;

        [EnumControl("")]
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
        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview2D; }
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            switch (m_ScreenSpaceType)
            {
                case ScreenSpaceType.Raw:
                    visitor.AddShaderChunk(string.Format("{0}4 {1} = {2};", precision, GetVariableNameForSlot(kOutputSlotId),
                        ShaderGeneratorNames.ScreenPosition), true);
                    break;
                case ScreenSpaceType.Center:
                    visitor.AddShaderChunk(string.Format("{0}4 {1} = {2};", precision, GetVariableNameForSlot(kOutputSlotId),
                        "float4((" + ShaderGeneratorNames.ScreenPosition + ".xy / " + ShaderGeneratorNames.ScreenPosition + ".w) * 2 - 1, 0, 0)"), true);
                    break;
                case ScreenSpaceType.Tiled:
                    visitor.AddShaderChunk(string.Format("{0}4 {1} = {2};", precision, GetVariableNameForSlot(kOutputSlotId),
                        "float4((" + ShaderGeneratorNames.ScreenPosition + ".xy / " + ShaderGeneratorNames.ScreenPosition + ".w) * 2 - 1, 0, 0)"), true);
                    visitor.AddShaderChunk(string.Format("{0} = {1};", GetVariableNameForSlot(kOutputSlotId),
                        "float4(" + ShaderGeneratorNames.ScreenPosition + ".x * _ScreenParams.x / _ScreenParams.y, " + ShaderGeneratorNames.ScreenPosition + ".y, 0, 0)"), true);
                    break;
                default:
                    visitor.AddShaderChunk(string.Format("{0}4 {1} = {2};", precision, GetVariableNameForSlot(kOutputSlotId),
                        "float4(" + ShaderGeneratorNames.ScreenPosition + ".xy / " + ShaderGeneratorNames.ScreenPosition + ".w, 0, 0)"), true);
                    break;
            }
        }

        public bool RequiresScreenPosition()
        {
            return true;
        }
    }
}
