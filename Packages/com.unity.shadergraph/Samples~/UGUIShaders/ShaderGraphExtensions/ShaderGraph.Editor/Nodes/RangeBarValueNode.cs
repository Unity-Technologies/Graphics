using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "UGUI", "RangeBar")]
    class RangeBarNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public RangeBarNode()
        {
            name = "RangeBar";
            UpdateNodeAfterDeserialization();
        }

        //public override string documentationURL => NodeUtils.GetDocumentationString("SliderValueNode");

        public override bool hasPreview => false;

        public sealed override void UpdateNodeAfterDeserialization()
        {
            var slots = new List<int>();
            MaterialSlot slot0 = new Vector1MaterialSlot(0, "Min", "_RangeValueMin", SlotType.Output, 0);
            AddSlot(slot0);
            slots.Add(0);

            MaterialSlot slot1 = new Vector1MaterialSlot(1, "Max", "_RangeValueMax", SlotType.Output, 1);
            AddSlot(slot1);
            slots.Add(1);

            MaterialSlot slot2 = new Vector2MaterialSlot(2, "Direction", "_RangeDirection", SlotType.Output, Vector2.zero);
            AddSlot(slot2);
            slots.Add(2);

            RemoveSlotsNameNotMatching(slots, true);
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            properties.AddShaderProperty(new Vector4ShaderProperty
            {
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.UnityPerMaterial,
                value = Vector4.zero,
                overrideReferenceName = "_RangeBarValue"
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine($"$precision {GetVariableNameForSlot(0)} = _RangeBarValue.x;");
            sb.AppendLine($"$precision {GetVariableNameForSlot(1)} = _RangeBarValue.y;");
            sb.AppendLine($"$precision2 {GetVariableNameForSlot(2)} = _RangeBarValue.zw;");
        }
    }
}
