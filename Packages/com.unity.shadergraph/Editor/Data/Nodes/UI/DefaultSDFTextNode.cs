using System.Linq;
using UnityEditor.Rendering.UITK.ShaderGraph;
using UnityEngine;

using SlotType = UnityEditor.Graphing.SlotType;

namespace UnityEditor.ShaderGraph
{
    [Title("UI", "Default SDF Text")]
    [SubTargetFilter(typeof(IUISubTarget))]
    class DefaultSDFTextNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireUITK
    {
        public DefaultSDFTextNode()
        {
            name = "Default SDF Text";
            synonyms = null;
            UpdateNodeAfterDeserialization();
        }

        const int k_InputSlotIdTint = 0;
        const int k_OutputSlotId = 1;
        const int k_InputSlotIdOutlineColor = 2;
        const int k_InputSlotIdShadowColor = 3;
        const int k_InputSlotIdSharpness = 4;
        const int k_InputSlotIdDilate = 5;
        const int k_InputSlotIdOutlineDilate = 6;
        const int k_InputSlotIdShadowOffset = 7;
        const int k_InputSlotIdShadowSoftness = 8;

        const string k_InputSlotNameTint = "Tint";
        const string k_InputSlotNameOutlineColor = "Outline Color";
        const string k_InputSlotNameShadowColor = "Shadow Color";
        const string k_InputSlotNameSharpness = "Sharpness";
        const string k_InputSlotNameDilate = "Dilate";
        const string k_InputSlotNameOutlineDilate = "Outline Dilate";
        const string k_InputSlotNameShadowOffset = "Shadow Offset";
        const string k_InputSlotNameShadowSoftness = "Shadow Softness";
        const string k_OutputSlotName = "SDF Text";
        const string k_DefaultBinding = "Default";

        static readonly int[] k_OverrideSlotIds =
        {
            k_InputSlotIdDilate, k_InputSlotIdSharpness, k_InputSlotIdOutlineColor, k_InputSlotIdOutlineDilate,
            k_InputSlotIdShadowColor, k_InputSlotIdShadowOffset, k_InputSlotIdShadowSoftness
        };

        static readonly int[] k_SlotOrder = new[] { k_InputSlotIdTint }.Concat(k_OverrideSlotIds).Append(k_OutputSlotId).ToArray();

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DefaultVector4MaterialSlot(k_InputSlotIdTint, k_InputSlotNameTint, k_InputSlotNameTint, "Default"));
            AddSlot(new Vector1MaterialSlot(k_InputSlotIdDilate, k_InputSlotNameDilate, k_InputSlotNameDilate, SlotType.Input, 0.0f) { CustomBinding = k_DefaultBinding });
            AddSlot(new Vector1MaterialSlot(k_InputSlotIdSharpness, k_InputSlotNameSharpness, k_InputSlotNameSharpness, SlotType.Input, 0.0f) { CustomBinding = k_DefaultBinding });
            AddSlot(new Vector4MaterialSlot(k_InputSlotIdOutlineColor, k_InputSlotNameOutlineColor, k_InputSlotNameOutlineColor, SlotType.Input, Vector4.zero) { CustomBinding = k_DefaultBinding });
            AddSlot(new Vector1MaterialSlot(k_InputSlotIdOutlineDilate, k_InputSlotNameOutlineDilate, k_InputSlotNameOutlineDilate, SlotType.Input, 0.0f) { CustomBinding = k_DefaultBinding });
            AddSlot(new Vector4MaterialSlot(k_InputSlotIdShadowColor, k_InputSlotNameShadowColor, k_InputSlotNameShadowColor, SlotType.Input, Vector4.zero) { CustomBinding = k_DefaultBinding });
            AddSlot(new Vector2MaterialSlot(k_InputSlotIdShadowOffset, k_InputSlotNameShadowOffset, k_InputSlotNameShadowOffset, SlotType.Input, Vector2.zero) { CustomBinding = k_DefaultBinding });
            AddSlot(new Vector1MaterialSlot(k_InputSlotIdShadowSoftness, k_InputSlotNameShadowSoftness, k_InputSlotNameShadowSoftness, SlotType.Input, 0.0f) { CustomBinding = k_DefaultBinding });
            AddSlot(new ColorRGBAMaterialSlot(k_OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, Vector4.one, ShaderStageCapability.Fragment));
            RemoveSlotsNameNotMatching(k_SlotOrder);
            SetSlotOrder(k_SlotOrder.ToList());
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            string outputVarName = GetVariableNameForSlot(k_OutputSlotId);

            sb.AppendLine("float4 {0} = float4(1, 1, 1, 1);", outputVarName);

            if (generationMode == GenerationMode.Preview)
            {
                bool hasTint = GetInputNodeFromSlot(k_InputSlotIdTint) != null;
                if (hasTint)
                    sb.AppendLine("{0} = {1};", outputVarName, GetSlotValue(k_InputSlotIdTint, generationMode));
                return;
            }

            sb.AppendLine("[branch] if (uie_is_sdf_text_fragment(IN.typeTexSettings))");
            using (sb.BlockScope())
            {
                bool hasTint = GetInputNodeFromSlot(k_InputSlotIdTint) != null;
                sb.AppendLine("SdfTextFragInput Unity_UIE_EvaluateSdfTextNode_Input;");
                // The tint is straight (not premultiplied); SDF text needs a premultiplied face color,
                // a step that used to happen in the vertex. Opacity is applied at the end of the fragment.
                sb.AppendLine("float4 Unity_UIE_EvaluateSdfTextNode_Tint = {0};", hasTint ? GetSlotValue(k_InputSlotIdTint, generationMode) : "IN.color");
                sb.AppendLine("Unity_UIE_EvaluateSdfTextNode_Tint.rgb *= Unity_UIE_EvaluateSdfTextNode_Tint.a;");
                sb.AppendLine("Unity_UIE_EvaluateSdfTextNode_Input.tint = Unity_UIE_EvaluateSdfTextNode_Tint;");
                sb.AppendLine("Unity_UIE_EvaluateSdfTextNode_Input.textureSlot = IN.typeTexSettings.y;");
                sb.AppendLine("Unity_UIE_EvaluateSdfTextNode_Input.uv = IN.uvClip.xy;");
                sb.AppendLine("Unity_UIE_EvaluateSdfTextNode_Input.extraDilate = IN.circle.x;");
                sb.AppendLine("Unity_UIE_EvaluateSdfTextNode_Input.textCoreLoc = round(IN.textCoreLoc);");
                sb.AppendLine("Unity_UIE_EvaluateSdfTextNode_Input.opacity = 1.0;");

                if (!k_OverrideSlotIds.Any(id => GetInputNodeFromSlot(id) != null))
                {
                    sb.AppendLine("CommonFragOutput Unity_UIE_EvaluateSdfTextNode_Output = uie_std_frag_sdf_text(Unity_UIE_EvaluateSdfTextNode_Input);");
                    sb.AppendLine("{0} = Unity_UIE_EvaluateSdfTextNode_Output.color;", outputVarName);
                    return;
                }

                sb.AppendLine("TextureInfo Unity_UIE_EvaluateSdfTextNode_TextureInfo = GetTextureInfo(Unity_UIE_EvaluateSdfTextNode_Input.textureSlot);");
                sb.AppendLine("SdfTextParams Unity_UIE_EvaluateSdfTextNode_Params = uie_load_sdf_text_params(Unity_UIE_EvaluateSdfTextNode_Input.textCoreLoc, Unity_UIE_EvaluateSdfTextNode_TextureInfo.sdfScale, Unity_UIE_EvaluateSdfTextNode_TextureInfo.sharpness, Unity_UIE_EvaluateSdfTextNode_Input.extraDilate);");
                AppendParamOverride(sb, generationMode, k_InputSlotIdOutlineColor, "outlineColor", transform: "uie_premultiply_sdf_outline_color({0})");
                AppendParamOverride(sb, generationMode, k_InputSlotIdShadowColor, "underlayColor");
                AppendParamOverride(sb, generationMode, k_InputSlotIdSharpness, "sharpness");
                AppendParamOverride(sb, generationMode, k_InputSlotIdDilate, "extraDilate");
                AppendParamOverride(sb, generationMode, k_InputSlotIdOutlineDilate, "outlineDilate");
                AppendParamOverride(sb, generationMode, k_InputSlotIdShadowOffset, "underlayOffset", transform: "float2(-({0}).x, ({0}).y)");
                AppendParamOverride(sb, generationMode, k_InputSlotIdShadowSoftness, "underlaySoftness");
                sb.AppendLine("Unity_UIE_EvaluateSdfTextNode_Params = uie_clamp_sdf_text_params(Unity_UIE_EvaluateSdfTextNode_Params);");

                sb.AppendLine("CommonFragOutput Unity_UIE_EvaluateSdfTextNode_Output = uie_std_frag_sdf_text(Unity_UIE_EvaluateSdfTextNode_Input, Unity_UIE_EvaluateSdfTextNode_Params);");
                sb.AppendLine("{0} = Unity_UIE_EvaluateSdfTextNode_Output.color;", outputVarName);
            }
        }

        void AppendParamOverride(ShaderStringBuilder sb, GenerationMode generationMode, int slotId, string fieldName, string transform = null)
        {
            if (GetInputNodeFromSlot(slotId) == null)
                return;

            string value = GetSlotValue(slotId, generationMode);
            if (transform != null)
                value = string.Format(transform, value);

            sb.AppendLine("Unity_UIE_EvaluateSdfTextNode_Params.{0} = {1};", fieldName, value);
        }

        public bool RequiresUITK(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
