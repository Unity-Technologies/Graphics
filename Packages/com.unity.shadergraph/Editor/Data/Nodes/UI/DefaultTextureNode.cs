using UnityEditor.Rendering.UITK.ShaderGraph;
using UnityEngine;

using SlotType = UnityEditor.Graphing.SlotType;

namespace UnityEditor.ShaderGraph
{
    [Title("UI", "Default Texture")]
    [SubTargetFilter(typeof(IUISubTarget))]
    class DefaultTextureNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireUITK
    {
        public DefaultTextureNode()
        {
            name = "Default Texture";
            synonyms = null;
            UpdateNodeAfterDeserialization();
        }

        const int k_InputSlotIdUV = 0;
        const int k_InputSlotIdTint = 1;
        const int k_OutputSlotId = 2;

        const string k_InputSlotNameUV = "UV";
        const string k_InputSlotNameTint = "Tint";
        const string k_OutputSlotName = "Texture";

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DefaultVector2MaterialSlot(k_InputSlotIdUV, k_InputSlotNameUV, k_InputSlotNameUV));
            AddSlot(new DefaultVector4MaterialSlot(k_InputSlotIdTint, k_InputSlotNameTint, k_InputSlotNameTint, "From Styles"));
            AddSlot(new ColorRGBAMaterialSlot(k_OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, Vector4.one, ShaderStageCapability.Fragment));
            RemoveSlotsNameNotMatching(new[] { k_InputSlotIdUV, k_InputSlotIdTint, k_OutputSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            string outputVarName = GetVariableNameForSlot(k_OutputSlotId);

            sb.AppendLine("float4 {0} = float4(1, 1, 0, 1);", outputVarName);

            sb.AppendLine("[branch] if (UIE_RENDER_TYPE_TEXTURED || UIE_RENDER_TYPE_ANY && round(IN.typeTexSettings.x) == k_FragTypeTexture)");
            using (sb.BlockScope())
            {
                bool hasTint = GetInputNodeFromSlot(k_InputSlotIdTint) != null;
                bool hasUV = GetInputNodeFromSlot(k_InputSlotIdUV) != null;

                sb.AppendLine("TextureFragInput Unity_UIE_EvaluateTextureNode_Input;");
                sb.AppendLine("Unity_UIE_EvaluateTextureNode_Input.tint = {0};", hasTint ? GetSlotValue(k_InputSlotIdTint, generationMode) : "IN.color");
                sb.AppendLine("Unity_UIE_EvaluateTextureNode_Input.textureSlot = IN.typeTexSettings.y;");
                sb.AppendLine("Unity_UIE_EvaluateTextureNode_Input.uv = {0};", hasUV ? GetSlotValue(k_InputSlotIdUV, generationMode) : "IN.uvClip.xy");
                sb.AppendLine("Unity_UIE_EvaluateTextureNode_Input.isArc = false;");
                sb.AppendLine("Unity_UIE_EvaluateTextureNode_Input.outer = float2(-10000, -10000);");
                sb.AppendLine("Unity_UIE_EvaluateTextureNode_Input.inner = float2(-10000, -10000);");
                sb.AppendLine("CommonFragOutput Unity_UIE_EvaluateTextureNode_Output = uie_std_frag_texture(Unity_UIE_EvaluateTextureNode_Input);");
                sb.AppendLine("{0} = Unity_UIE_EvaluateTextureNode_Output.color;", outputVarName);
            }
        }

        public bool RequiresUITK(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
