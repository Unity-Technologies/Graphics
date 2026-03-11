using UnityEditor.Graphing;
using UnityEditor.Rendering.UITK.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "UI", "Element Texture UV")]
    [SubTargetFilter(typeof(IUISubTarget))]
    class ElementTextureUVNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireUITK, IMayRequireMeshUV
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
            if (generationMode == GenerationMode.Preview)
            {
                // In preview mode, use standard mesh UV0 (will visualize as color: red = u, green = v)
                sb.AppendLine("$precision2 {0} = IN.uv0.xy;", GetVariableNameForSlot(TextureUVSlotId));
                return;
            }

            if (GetInputNodeFromSlot(TextureUVSlotId) != null) sb.AppendLine("$precision2 {0} = IN.uvClip.xy;", GetVariableNameForSlot(TextureUVSlotId));
        }

        public bool RequiresUITK(ShaderStageCapability stageCapability)
        {
            return true;
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            // Require UV0 in preview mode for visualization
            return channel == UVChannel.UV0;
        }
    }
}
