using UnityEditor.Rendering.UITK.ShaderGraph;
using UnityEngine;

using SlotType = UnityEditor.Graphing.SlotType;

namespace UnityEditor.ShaderGraph
{
    [Title("UI", "Render Type Branch")]
    [SubTargetFilter(typeof(IUISubTarget))]
    class RenderTypeBranchNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireUITK
    {
        public RenderTypeBranchNode()
        {
            name = "Render Type Branch";
            synonyms = new [] { "toggle", "uber" };
            UpdateNodeAfterDeserialization();
        }

        const int k_InputSlotIdSolid = 0;
        const int k_InputSlotIdTexture = 1;
        const int k_InputSlotIdSdfText = 2;
        const int k_InputSlotIdBitmapText = 3;
        const int k_InputSlotIdGradient = 4;
        const int k_OutputSlotIdColor = 5;
        const int k_OutputSlotIdAlpha = 6;

        const string k_InputSlotNameSolid = "Solid";
        const string k_InputSlotNameTexture = "Texture";
        const string k_InputSlotNameSdfText = "SDF Text";
        const string k_InputSlotNameBitmapText = "Bitmap Text";
        const string k_InputSlotNameGradient = "Gradient";
        const string k_ColorOutputSlotName = "Color";
        const string k_AlphaOutputSlotName = "Alpha";

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DefaultVector4MaterialSlot(k_InputSlotIdSolid, k_InputSlotNameSolid, k_InputSlotNameSolid));
            AddSlot(new DefaultVector4MaterialSlot(k_InputSlotIdTexture, k_InputSlotNameTexture, k_InputSlotNameTexture));
            AddSlot(new DefaultVector4MaterialSlot(k_InputSlotIdSdfText, k_InputSlotNameSdfText, k_InputSlotNameSdfText));
            AddSlot(new DefaultVector4MaterialSlot(k_InputSlotIdBitmapText, k_InputSlotNameBitmapText, k_InputSlotNameBitmapText));
            AddSlot(new DefaultVector4MaterialSlot(k_InputSlotIdGradient, k_InputSlotNameGradient, k_InputSlotNameGradient));
            AddSlot(new ColorRGBMaterialSlot(k_OutputSlotIdColor, k_ColorOutputSlotName, k_ColorOutputSlotName, SlotType.Output, Color.white, Internal.ColorMode.Default, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(k_OutputSlotIdAlpha, k_AlphaOutputSlotName, k_AlphaOutputSlotName,  SlotType.Output, 1.0f, ShaderStageCapability.Fragment));
            RemoveSlotsNameNotMatching(new[] { k_InputSlotIdSolid, k_InputSlotIdTexture, k_InputSlotIdSdfText, k_InputSlotIdBitmapText, k_InputSlotIdGradient, k_OutputSlotIdColor, k_OutputSlotIdAlpha });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            string outputVarNameColor = GetVariableNameForSlot(k_OutputSlotIdColor);
            string outputVarNameAlpha = GetVariableNameForSlot(k_OutputSlotIdAlpha);

            sb.AppendLine("float3 {0} = float3(0, 0, 0);", outputVarNameColor);
            sb.AppendLine("float {0} = 1.0;", outputVarNameAlpha);

            sb.AppendLine("[branch] if (_UIE_RENDER_TYPE_SOLID || _UIE_RENDER_TYPE_ANY && TestType(IN.typeTexSettings.x, k_FragTypeSolid))");
            using (sb.BlockScope())
            {
                if (GetInputNodeFromSlot(k_InputSlotIdSolid) != null)
                {
                    // TODO: We are not switching the computation, only the result. This is a performance problem. Same story for the other render types.
                    sb.AppendLine("{0} = {1}.rgb;", outputVarNameColor, GetSlotValue(k_InputSlotIdSolid, generationMode));
                    sb.AppendLine("{0} = {1}.a;", outputVarNameAlpha, GetSlotValue(k_InputSlotIdSolid, generationMode));
                }
                else
                {
                    sb.AppendLine("SolidFragInput Unity_UIE_RenderTypeSwitchNode_Solid_Input;");
                    sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_Solid_Input.tint = IN.color;");
                    sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_Solid_Input.isArc = false;");
                    sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_Solid_Input.outer = float2(-10000, -10000);");
                    sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_Solid_Input.inner = float2(-10000, -10000);");
                    sb.AppendLine("CommonFragOutput Unity_UIE_RenderTypeSwitchNode_Output = uie_std_frag_solid(Unity_UIE_RenderTypeSwitchNode_Solid_Input);");
                    sb.AppendLine("{0} = Unity_UIE_RenderTypeSwitchNode_Output.color.rgb;", outputVarNameColor);
                    sb.AppendLine("{0} = Unity_UIE_RenderTypeSwitchNode_Output.color.a;", outputVarNameAlpha);
                }
            }
            sb.AppendLine("else [branch] if (_UIE_RENDER_TYPE_TEXTURE || _UIE_RENDER_TYPE_ANY && TestType(IN.typeTexSettings.x, k_FragTypeTexture))");
            using (sb.BlockScope())
            {
                if (GetInputNodeFromSlot(k_InputSlotIdTexture) != null)
                {
                    sb.AppendLine("{0} = {1}.rgb;", outputVarNameColor, GetSlotValue(k_InputSlotIdTexture, generationMode));
                    sb.AppendLine("{0} = {1}.a;", outputVarNameAlpha, GetSlotValue(k_InputSlotIdTexture, generationMode));
                }
                else
                {
                    sb.AppendLine("TextureFragInput Unity_UIE_RenderTypeSwitchNode_Texture_Input;");
                    sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_Texture_Input.tint = IN.color;");
                    sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_Texture_Input.textureSlot = IN.typeTexSettings.y;");
                    sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_Texture_Input.uv = IN.uvClip.xy;");
                    sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_Texture_Input.isArc = false;");
                    sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_Texture_Input.outer = float2(-10000, -10000);");
                    sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_Texture_Input.inner = float2(-10000, -10000);");
                    sb.AppendLine("CommonFragOutput Unity_UIE_RenderTypeSwitchNode_Output = uie_std_frag_texture(Unity_UIE_RenderTypeSwitchNode_Texture_Input);");
                    sb.AppendLine("{0} = Unity_UIE_RenderTypeSwitchNode_Output.color.rgb;", outputVarNameColor);
                    sb.AppendLine("{0} = Unity_UIE_RenderTypeSwitchNode_Output.color.a;", outputVarNameAlpha);
                }
            }
            sb.AppendLine("else [branch] if ((_UIE_RENDER_TYPE_TEXT || _UIE_RENDER_TYPE_ANY) && TestType(IN.typeTexSettings.x, k_FragTypeText))");
            using (sb.BlockScope())
            {
                sb.AppendLine("[branch] if (GetTextureInfo(IN.typeTexSettings.y).sdfScale > 0.0)");
                using (sb.BlockScope())
                {
                    if (GetInputNodeFromSlot(k_InputSlotIdSdfText) != null)
                    {
                        sb.AppendLine("{0} = {1}.rgb;", outputVarNameColor, GetSlotValue(k_InputSlotIdSdfText, generationMode));
                        sb.AppendLine("{0} = {1}.a;", outputVarNameAlpha, GetSlotValue(k_InputSlotIdSdfText, generationMode));
                    }
                    else
                    {
                        sb.AppendLine("SdfTextFragInput Unity_UIE_RenderTypeSwitchNode_SdfText_Input;");
                        sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_SdfText_Input.tint = IN.color;");
                        sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_SdfText_Input.textureSlot = IN.typeTexSettings.y;");
                        sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_SdfText_Input.uv = IN.uvClip.xy;");
                        sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_SdfText_Input.extraDilate = IN.circle.x;");
                        sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_SdfText_Input.textCoreLoc = round(IN.textCoreLoc);");
                        sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_SdfText_Input.opacity = IN.typeTexSettings.z;");
                        sb.AppendLine("CommonFragOutput Unity_UIE_RenderTypeSwitchNode_Output = uie_std_frag_sdf_text(Unity_UIE_RenderTypeSwitchNode_SdfText_Input);");
                        sb.AppendLine("{0} = Unity_UIE_RenderTypeSwitchNode_Output.color.rgb;", outputVarNameColor);
                        sb.AppendLine("{0} = Unity_UIE_RenderTypeSwitchNode_Output.color.a;", outputVarNameAlpha);
                    }
                }
                sb.AppendLine("else");
                using (sb.BlockScope())
                {
                    if (GetInputNodeFromSlot(k_InputSlotIdBitmapText) != null)
                    {
                        sb.AppendLine("{0} = {1}.rgb;", outputVarNameColor, GetSlotValue(k_InputSlotIdBitmapText, generationMode));
                        sb.AppendLine("{0} = {1}.a;", outputVarNameAlpha, GetSlotValue(k_InputSlotIdBitmapText, generationMode));
                    }
                    else
                    {
                        sb.AppendLine("BitmapTextFragInput Unity_UIE_RenderTypeSwitchNode_BitmapText_Input;");
                        sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_BitmapText_Input.tint = IN.color;");
                        sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_BitmapText_Input.textureSlot = IN.typeTexSettings.y;");
                        sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_BitmapText_Input.uv = IN.uvClip.xy;");
                        sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_BitmapText_Input.opacity = IN.typeTexSettings.z;");
                        sb.AppendLine("CommonFragOutput Unity_UIE_RenderTypeSwitchNode_Output = uie_std_frag_bitmap_text(Unity_UIE_RenderTypeSwitchNode_BitmapText_Input);");
                        sb.AppendLine("{0} = Unity_UIE_RenderTypeSwitchNode_Output.color.rgb;", outputVarNameColor);
                        sb.AppendLine("{0} = Unity_UIE_RenderTypeSwitchNode_Output.color.a;", outputVarNameAlpha);
                    }
                }
            }
            sb.AppendLine("else"); // k_FragTypeSvgGradient
            using (sb.BlockScope())
            {
                if (GetInputNodeFromSlot(k_InputSlotIdGradient) != null)
                {
                    sb.AppendLine("{0} = {1}.rgb;", outputVarNameColor, GetSlotValue(k_InputSlotIdGradient, generationMode));
                    sb.AppendLine("{0} = {1}.a;", outputVarNameAlpha, GetSlotValue(k_InputSlotIdGradient, generationMode));
                }
                else
                {
                    sb.AppendLine("SvgGradientFragInput Unity_UIE_RenderTypeSwitchNode_SvgGradient_Input;");
                    sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_SvgGradient_Input.settingIndex = round(IN.typeTexSettings.z);");
                    sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_SvgGradient_Input.textureSlot = round(IN.typeTexSettings.y);");
                    sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_SvgGradient_Input.uv = IN.uvClip.xy;");
                    sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_SvgGradient_Input.isArc = false;");
                    sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_SvgGradient_Input.outer = float2(-10000, -10000);");
                    sb.AppendLine("Unity_UIE_RenderTypeSwitchNode_SvgGradient_Input.inner = float2(-10000, -10000);");
                    sb.AppendLine("CommonFragOutput Unity_UIE_RenderTypeSwitchNode_Output = uie_std_frag_svg_gradient(Unity_UIE_RenderTypeSwitchNode_SvgGradient_Input);");
                    sb.AppendLine("{0} = Unity_UIE_RenderTypeSwitchNode_Output.color.rgb * IN.color.rgb;", outputVarNameColor);
                    sb.AppendLine("{0} = Unity_UIE_RenderTypeSwitchNode_Output.color.a * IN.color.a;", outputVarNameAlpha);
                }
            }
        }
        public bool RequiresUITK(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
