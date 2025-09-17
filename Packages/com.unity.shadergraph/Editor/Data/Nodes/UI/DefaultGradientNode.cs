using UnityEditor.Rendering.UITK.ShaderGraph;
using UnityEngine;

using SlotType = UnityEditor.Graphing.SlotType;

namespace UnityEditor.ShaderGraph
{
    [Title("UI", "Default Gradient")]
    [SubTargetFilter(typeof(IUISubTarget))]
    class DefaultGradientNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireUITK
    {
        public DefaultGradientNode()
        {
            name = "Default Gradient";
            synonyms = null;
            UpdateNodeAfterDeserialization();
        }

        const int k_OutputSlotId = 0;
        const string k_OutputSlotName = "Gradient";

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new ColorRGBAMaterialSlot(k_OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, Vector4.one, ShaderStageCapability.Fragment));
            RemoveSlotsNameNotMatching(new[] { k_OutputSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            string outputVarName = GetVariableNameForSlot(k_OutputSlotId);

            sb.AppendLine("float4 {0} = float4(1, 1, 0, 1);", outputVarName);

            sb.AppendLine("[branch] if (_UIE_RENDER_TYPE_GRADIENT || _UIE_RENDER_TYPE_ANY && round(IN.typeTexSettings.x) == k_FragTypeSvgGradient)");
            using (sb.BlockScope())
            {
                sb.AppendLine("SvgGradientFragInput Unity_UIE_RenderTypeSwitchNode_SvgGradient_Input;");
                sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_SvgGradient_Input.settingIndex = round(IN.typeTexSettings.z);");
                sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_SvgGradient_Input.textureSlot = round(IN.typeTexSettings.y);");
                sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_SvgGradient_Input.uv = IN.uvClip.xy;");
                sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_SvgGradient_Input.isArc = false;");
                sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_SvgGradient_Input.outer = float2(-10000, -10000);");
                sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_SvgGradient_Input.inner = float2(-10000, -10000);");
                sb.AppendLine("CommonFragOutput Unity_UIE_RenderTypeSwitchNode_Output = uie_std_frag_svg_gradient(Unity_UIE_RenderTypeSwitchNode_SvgGradient_Input);");
                sb.AppendLine("{0} = Unity_UIE_RenderTypeSwitchNode_Output.color * IN.color;", outputVarName);
            }
        }

        public bool RequiresUITK(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
