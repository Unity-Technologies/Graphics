using UnityEditor.Graphing;
using UnityEditor.Rendering.UITK.ShaderGraph;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "UI", "Element Texture UV")]
    [SubTargetFilter(typeof(IUISubTarget))]
    class ElementTextureUVNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireUITK
    {
        public const int TextureUVSlotId = 0;

        private const string kTextureUVSlotName = "Texture UV";

        public override bool hasPreview { get { return false; } }

        public ElementTextureUVNode()
        {
            name = "Element Texture UV";
            synonyms = new string[] {};
            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector2MaterialSlot(TextureUVSlotId, kTextureUVSlotName, kTextureUVSlotName, SlotType.Output, Vector2.zero));
            RemoveSlotsNameNotMatching(new[] { TextureUVSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (GetInputNodeFromSlot(TextureUVSlotId) != null) sb.AppendLine(string.Format("$precision2 {0} = IN.uvClip.xy;", GetVariableNameForSlot(TextureUVSlotId)));
        }

        public bool RequiresUITK(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
