using UnityEditor.Graphing;
using UnityEditor.Rendering.UITK.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "UI", "Element Layout UV")]
    [SubTargetFilter(typeof(IUISubTarget))]
    class ElementLayoutUV : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireUITK, IMayRequireMeshUV
    {
        public const int LayoutUVSlotId = 0;

        private const string kLayoutUVSlotName = "Layout UV";

        public override bool hasPreview { get { return false; } }

        public ElementLayoutUV()
        {
            name = "Element Layout UV";
            synonyms = new string[] {};
            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector2MaterialSlot(LayoutUVSlotId, kLayoutUVSlotName, kLayoutUVSlotName, SlotType.Output, Vector2.zero));
            RemoveSlotsNameNotMatching(new[] { LayoutUVSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.Preview)
            {
                // In preview mode, use standard mesh UV0 (will visualize as color: red = u, green = v)
                sb.AppendLine("$precision2 {0} = IN.uv0.xy;", GetVariableNameForSlot(LayoutUVSlotId));
                return;
            }

            if (GetInputNodeFromSlot(LayoutUVSlotId) != null) sb.AppendLine("$precision2 {0} = IN.layoutUV.xy;", GetVariableNameForSlot(LayoutUVSlotId));
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
