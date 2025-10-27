using UnityEditor.Graphing;
using UnityEditor.Rendering.UITK.ShaderGraph;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("UI", "Render Type")]
    [SubTargetFilter(typeof(IUISubTarget))]
    class RenderTypeNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireUITK
    {
        const int k_OutputSlotIdSolid = 0;
        const int k_OutputSlotIdTexture = 1;
        const int k_OutputSlotIdSDFText = 2;
        const int k_OutputSlotIdBitmapText = 3;
        const int k_OutputSlotIdGradient = 4;

        const string k_OutputSlotNameSolid = "Solid";
        const string k_OutputSlotNameTexture = "Texture";
        const string k_OutputSlotNameSDFText = "SDF Text";
        const string k_OutputSlotNameBitmapText = "Bitmap Text";
        const string k_OutputSlotNameGradient = "Gradient";

        public RenderTypeNode()
        {
            name = "Render Type";
            synonyms = new string[] { };
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new BooleanMaterialSlot(k_OutputSlotIdSolid, k_OutputSlotNameSolid, k_OutputSlotNameSolid, SlotType.Output, true));
            AddSlot(new BooleanMaterialSlot(k_OutputSlotIdTexture, k_OutputSlotNameTexture, k_OutputSlotNameTexture, SlotType.Output, false));
            AddSlot(new BooleanMaterialSlot(k_OutputSlotIdSDFText, k_OutputSlotNameSDFText, k_OutputSlotNameSDFText, SlotType.Output, false));
            AddSlot(new BooleanMaterialSlot(k_OutputSlotIdBitmapText, k_OutputSlotNameBitmapText, k_OutputSlotNameBitmapText, SlotType.Output, false));
            AddSlot(new BooleanMaterialSlot(k_OutputSlotIdGradient, k_OutputSlotNameGradient, k_OutputSlotNameGradient, SlotType.Output, false));
            RemoveSlotsNameNotMatching(new[] { k_OutputSlotIdSolid, k_OutputSlotIdTexture, k_OutputSlotIdSDFText, k_OutputSlotIdBitmapText, k_OutputSlotIdGradient });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine("bool {0} = _UIE_RENDER_TYPE_SOLID || _UIE_RENDER_TYPE_ANY && round(IN.typeTexSettings.x) == k_FragTypeSolid;", GetVariableNameForSlot(k_OutputSlotIdSolid));
            sb.AppendLine("bool {0} = _UIE_RENDER_TYPE_TEXTURE || _UIE_RENDER_TYPE_ANY && round(IN.typeTexSettings.x) == k_FragTypeTexture;", GetVariableNameForSlot(k_OutputSlotIdTexture));
            sb.AppendLine("bool {0} = (_UIE_RENDER_TYPE_TEXT || _UIE_RENDER_TYPE_ANY) && round(IN.typeTexSettings.x) == k_FragTypeText && (GetTextureInfo(IN.typeTexSettings.y).sdfScale > 0.0);", GetVariableNameForSlot(k_OutputSlotIdSDFText));
            sb.AppendLine("bool {0} = (_UIE_RENDER_TYPE_TEXT || _UIE_RENDER_TYPE_ANY) && round(IN.typeTexSettings.x) == k_FragTypeText && (!(GetTextureInfo(IN.typeTexSettings.y).sdfScale > 0.0));", GetVariableNameForSlot(k_OutputSlotIdBitmapText));
            sb.AppendLine("bool {0} = _UIE_RENDER_TYPE_GRADIENT || _UIE_RENDER_TYPE_ANY && round(IN.typeTexSettings.x) == k_FragTypeSvgGradient;", GetVariableNameForSlot(k_OutputSlotIdGradient));
        }

        public bool RequiresUITK(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
