using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    public interface IMayRequireScreenPosition
    {
        bool RequiresScreenPosition();
    }

    public enum ScreenSpaceType
    {
        Default,
        Center,
        Tiled
    };

    [Title("Input/Geometry/Screen Position")]
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
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        string GetCurrentType()
        {
            return System.Enum.GetName(typeof(ScreenSpaceType), m_ScreenSpaceType);
        }

        private const int kOutputSlot1Id = 0;
        private const string kOutputSlot1Name = "Out";

        public override bool hasPreview { get { return true; } }
        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview2D; }
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector2MaterialSlot(kOutputSlot1Id, kOutputSlot1Name, kOutputSlot1Name, SlotType.Output, Vector2.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlot1Id });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return ShaderGeneratorNames.ScreenPosition;
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            switch (m_ScreenSpaceType)
            {
                case ScreenSpaceType.Center:
                    visitor.AddShaderChunk(string.Format("{0} = {1};", ShaderGeneratorNames.ScreenPosition, "float4((" + ShaderGeneratorNames.ScreenPosition + ".xy / " + ShaderGeneratorNames.ScreenPosition + ".w) * 2 - 1, 0, 0)"), false);
                    break;
                case ScreenSpaceType.Tiled:
                    visitor.AddShaderChunk(string.Format("{0} = {1};", ShaderGeneratorNames.ScreenPosition, "float4((" + ShaderGeneratorNames.ScreenPosition + ".xy / " + ShaderGeneratorNames.ScreenPosition + ".w) * 2 - 1, 0, 0)"), false);
                    visitor.AddShaderChunk(string.Format("{0} = {1};", ShaderGeneratorNames.ScreenPosition, "float4(" + ShaderGeneratorNames.ScreenPosition + ".x * _ScreenParams.x / _ScreenParams.y, " + ShaderGeneratorNames.ScreenPosition + ".y, 0, 0)"), false);
                    break;
                default:
                    visitor.AddShaderChunk(string.Format("{0} = {1};", ShaderGeneratorNames.ScreenPosition, "float4(" + ShaderGeneratorNames.ScreenPosition + ".xy / " + ShaderGeneratorNames.ScreenPosition + ".w, 0, 0)"), false);
                    break;
            }
        }

        public bool RequiresScreenPosition()
        {
            return true;
        }
    }
}
