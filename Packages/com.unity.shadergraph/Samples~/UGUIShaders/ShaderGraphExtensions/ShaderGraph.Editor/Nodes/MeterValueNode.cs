using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "UGUI", "Meter Value")]
    class MeterValueNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public MeterValueNode()
        {
            name = "Meter Value";
            UpdateNodeAfterDeserialization();
        }

        //public override string documentationURL => NodeUtils.GetDocumentationString("MeterValueNode");

        public override bool hasPreview => false;

        public sealed override void UpdateNodeAfterDeserialization()
        {
            var slots = new List<int>();
            MaterialSlot slot0 = new Vector1MaterialSlot(0, "Value", "_MeterValue", SlotType.Output, 0);
            AddSlot(slot0);
            slots.Add(0);

            RemoveSlotsNameNotMatching(slots, true);
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            properties.AddShaderProperty(new Vector1ShaderProperty
            {
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.UnityPerMaterial,
                value = 0.5f,
                overrideReferenceName = "_MeterValue"
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine($"$precision {GetVariableNameForSlot(0)} = _MeterValue;");
        }
    }
}
