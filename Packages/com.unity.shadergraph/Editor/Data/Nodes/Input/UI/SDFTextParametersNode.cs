using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.Rendering.UITK.ShaderGraph;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "UI", "SDF Text Parameters")]
    [SubTargetFilter(typeof(IUISubTarget))]
    class SDFTextParametersNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireUITK
    {
        public const int OutlineColorSlotId = 0;
        public const int ShadowColorSlotId = 1;
        public const int SharpnessSlotId = 2;
        public const int DilateSlotId = 3;
        public const int OutlineDilateSlotId = 4;
        public const int ShadowOffsetSlotId = 5;
        public const int ShadowSoftnessSlotId = 6;

        const string k_OutlineColorSlotName = "Outline Color";
        const string k_ShadowColorSlotName = "Shadow Color";
        const string k_SharpnessSlotName = "Sharpness";
        const string k_DilateSlotName = "Dilate";
        const string k_OutlineDilateSlotName = "Outline Dilate";
        const string k_ShadowOffsetSlotName = "Shadow Offset";
        const string k_ShadowSoftnessSlotName = "Shadow Softness";

        static readonly int[] k_SlotOrder =
        {
            DilateSlotId, SharpnessSlotId, OutlineColorSlotId, OutlineDilateSlotId,
            ShadowColorSlotId, ShadowOffsetSlotId, ShadowSoftnessSlotId
        };

        public override bool hasPreview { get { return false; } }

        public SDFTextParametersNode()
        {
            name = "SDF Text Parameters";
            synonyms = new string[] { "element", "ui", "text", "font", "outline", "underlay", "shadow", "bold", "dilate" };
            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(DilateSlotId, k_DilateSlotName, k_DilateSlotName, SlotType.Output, 0.0f));
            AddSlot(new Vector1MaterialSlot(SharpnessSlotId, k_SharpnessSlotName, k_SharpnessSlotName, SlotType.Output, 0.0f));
            AddSlot(new ColorRGBAMaterialSlot(OutlineColorSlotId, k_OutlineColorSlotName, k_OutlineColorSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new Vector1MaterialSlot(OutlineDilateSlotId, k_OutlineDilateSlotName, k_OutlineDilateSlotName, SlotType.Output, 0.0f));
            AddSlot(new ColorRGBAMaterialSlot(ShadowColorSlotId, k_ShadowColorSlotName, k_ShadowColorSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new Vector2MaterialSlot(ShadowOffsetSlotId, k_ShadowOffsetSlotName, k_ShadowOffsetSlotName, SlotType.Output, Vector2.zero));
            AddSlot(new Vector1MaterialSlot(ShadowSoftnessSlotId, k_ShadowSoftnessSlotName, k_ShadowSoftnessSlotName, SlotType.Output, 0.0f));
            RemoveSlotsNameNotMatching(k_SlotOrder);
            SetSlotOrder(new List<int>(k_SlotOrder));
        }

        static readonly (int slotId, string dimension, string field)[] k_Outputs =
        {
            (DilateSlotId, "", "extraDilate"),
            (SharpnessSlotId, "", "sharpness"),
            (OutlineColorSlotId, "4", "outlineColor"),
            (OutlineDilateSlotId, "", "outlineDilate"),
            (ShadowColorSlotId, "4", "underlayColor"),
            (ShadowOffsetSlotId, "2", "underlayOffset"),
            (ShadowSoftnessSlotId, "", "underlaySoftness"),
        };

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            foreach (var (slotId, dimension, _) in k_Outputs)
                sb.AppendLine("$precision{0} {1} = 0;", dimension, GetVariableNameForSlot(slotId));

            if (generationMode == GenerationMode.Preview)
                return;

            sb.AppendLine("[branch] if (uie_is_sdf_text_fragment(IN.typeTexSettings))");
            using (sb.BlockScope())
            {
                sb.AppendLine("TextureInfo Unity_UIE_SdfTextParametersNode_TextureInfo = GetTextureInfo(IN.typeTexSettings.y);");
                sb.AppendLine("SdfTextParams Unity_UIE_SdfTextParametersNode_Params = uie_load_sdf_text_params((min16uint2)round(IN.textCoreLoc), Unity_UIE_SdfTextParametersNode_TextureInfo.sdfScale, Unity_UIE_SdfTextParametersNode_TextureInfo.sharpness, IN.circle.x);");
                foreach (var (slotId, _, field) in k_Outputs)
                    sb.AppendLine("{0} = Unity_UIE_SdfTextParametersNode_Params.{1};", GetVariableNameForSlot(slotId), field);

                string outlineColorVar = GetVariableNameForSlot(OutlineColorSlotId);
                sb.AppendLine("{0} = uie_unpremultiply_sdf_outline_color({0});", outlineColorVar);
                // Stored x is negated; the port uses text-shadow orientation.
                sb.AppendLine("{0}.x = -{0}.x;", GetVariableNameForSlot(ShadowOffsetSlotId));
            }
        }

        public bool RequiresUITK(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
