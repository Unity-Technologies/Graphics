using UnityEditor.Graphing;
using UnityEditor.Rendering.UITK.ShaderGraph;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "UI", "Element Layout UV")]
    [SubTargetFilter(typeof(IUISubTarget))]
    class ElementLayoutUV : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireUITK
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
            if (GetInputNodeFromSlot(LayoutUVSlotId) != null) sb.AppendLine(string.Format("$precision2 {0} = IN.layoutUV.xy;", GetVariableNameForSlot(LayoutUVSlotId)));
        }

        public bool RequiresUITK(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
