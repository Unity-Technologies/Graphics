using UnityEditor.Graphing;
using UnityEditor.Rendering.UITK.ShaderGraph;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("UI", "Is Forced Gamma")]
    [SubTargetFilter(typeof(IUISubTarget))]
    class IsForcedGammaNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireUITK
    {
        const int k_OutputSlotId = 0;
        const string k_OutputSlotName = "Out";

        public IsForcedGammaNode()
        {
            name = "Is Forced Gamma";
            synonyms = new string[] { };
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new BooleanMaterialSlot(k_OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, true));
            RemoveSlotsNameNotMatching(new[] { k_OutputSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine("bool {0} = _UIE_FORCE_GAMMA;", GetVariableNameForSlot(k_OutputSlotId));
        }

        public bool RequiresUITK(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
