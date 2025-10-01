using UnityEditor.Rendering.UITK.ShaderGraph;
using UnityEngine;

using SlotType = UnityEditor.Graphing.SlotType;

namespace UnityEditor.ShaderGraph
{
    [Title("UI", "Default Solid")]
    [SubTargetFilter(typeof(IUISubTarget))]
    class DefaultSolidNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireUITK
    {
        public DefaultSolidNode()
        {
            name = "Default Solid";
            synonyms = null;
            UpdateNodeAfterDeserialization();
        }

        const int k_OutputSlotId = 0;
        const string k_OutputSlotName = "Solid";

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new ColorRGBAMaterialSlot(k_OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, Vector4.one, ShaderStageCapability.Fragment));
            RemoveSlotsNameNotMatching(new[] { k_OutputSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            string outputVarName = GetVariableNameForSlot(k_OutputSlotId);

            sb.AppendLine("float4 {0} = float4(1, 1, 0, 1);", outputVarName);

            sb.AppendLine("[branch] if (_UIE_RENDER_TYPE_SOLID || _UIE_RENDER_TYPE_ANY && round(IN.typeTexSettings.x) == k_FragTypeSolid)");
            using (sb.BlockScope())
            {
                sb.AppendLine("SolidFragInput Unity_UIE_EvaluateSolidNode_Input;");
                sb.AppendLine("Unity_UIE_EvaluateSolidNode_Input.tint = IN.color;");
                sb.AppendLine("Unity_UIE_EvaluateSolidNode_Input.isArc = false;");
                sb.AppendLine("Unity_UIE_EvaluateSolidNode_Input.outer = float2(-10000, -10000);");
                sb.AppendLine("Unity_UIE_EvaluateSolidNode_Input.inner = float2(-10000, -10000);");
                sb.AppendLine("CommonFragOutput Unity_UIE_EvaluateSolidNode_Output = uie_std_frag_solid(Unity_UIE_EvaluateSolidNode_Input);");
                sb.AppendLine("{0} = Unity_UIE_EvaluateSolidNode_Output.color;", outputVarName);
            }
        }

        public bool RequiresUITK(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
