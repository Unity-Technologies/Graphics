using UnityEditor.Rendering.UITK.ShaderGraph;
using UnityEngine;

using SlotType = UnityEditor.Graphing.SlotType;

namespace UnityEditor.ShaderGraph
{
    [Title("UI", "Default Bitmap Text")]
    [SubTargetFilter(typeof(IUISubTarget))]
    class DefaultBitmapTextNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireUITK
    {
        public DefaultBitmapTextNode()
        {
            name = "Default Bitmap Text";
            synonyms = null;
            UpdateNodeAfterDeserialization();
        }

        const int k_InputSlotIdTint = 0;
        const int k_OutputSlotId = 1;

        const string k_InputSlotNameTint = "Tint";
        const string k_OutputSlotName = "Bitmap Text";

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DefaultVector4MaterialSlot(k_InputSlotIdTint, k_InputSlotNameTint, k_InputSlotNameTint, "Default"));
            AddSlot(new ColorRGBAMaterialSlot(k_OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, Vector4.one, ShaderStageCapability.Fragment));
            RemoveSlotsNameNotMatching(new[] { k_InputSlotIdTint, k_OutputSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            string outputVarName = GetVariableNameForSlot(k_OutputSlotId);

            sb.AppendLine("float4 {0} = float4(1, 1, 0, 1);", outputVarName);

            sb.AppendLine("[branch] if ((_UIE_RENDER_TYPE_TEXT || _UIE_RENDER_TYPE_ANY) && round(IN.typeTexSettings.x) == k_FragTypeText && (!(GetTextureInfo(IN.typeTexSettings.y).sdfScale > 0.0)))");
            using (sb.BlockScope())
            {
                bool hasTint = GetInputNodeFromSlot(k_InputSlotIdTint) != null;
                sb.AppendLine("BitmapTextFragInput Unity_UIE_EvaluateBitmapNode_Input;");
                sb.AppendLine("Unity_UIE_EvaluateBitmapNode_Input.tint = {0};", hasTint ? GetSlotValue(k_InputSlotIdTint, generationMode) : "IN.color");
                sb.AppendLine("Unity_UIE_EvaluateBitmapNode_Input.textureSlot = IN.typeTexSettings.y;");
                sb.AppendLine("Unity_UIE_EvaluateBitmapNode_Input.uv = IN.uvClip.xy;");
                sb.AppendLine("Unity_UIE_EvaluateBitmapNode_Input.opacity = IN.typeTexSettings.z;");
                sb.AppendLine("CommonFragOutput Unity_UIE_EvaluateBitmapNode_Output = uie_std_frag_bitmap_text(Unity_UIE_EvaluateBitmapNode_Input);");
                sb.AppendLine("{0}.rgb = Unity_UIE_EvaluateBitmapNode_Output.color.rgb;", outputVarName);
                // To correctly apply the opacity we have to multiply the alpha values by the input color's alpha if input color is not used as the tint
                if (hasTint)
                {
                    sb.AppendLine("{0} = Unity_UIE_EvaluateBitmapNode_Output.color * IN.color.a;", outputVarName);
                }
                else
                {
                    sb.AppendLine("{0}.a = Unity_UIE_EvaluateBitmapNode_Output.color.a;", outputVarName);
                }
            }
        }

        public bool RequiresUITK(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
