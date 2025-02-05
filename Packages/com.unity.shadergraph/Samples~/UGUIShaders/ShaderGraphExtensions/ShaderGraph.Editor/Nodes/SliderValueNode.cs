using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "UGUI", "Slider Value")]
    class SliderValueNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public SliderValueNode()
        {
            name = "Slider Value";
            UpdateNodeAfterDeserialization();
        }

        //public override string documentationURL => NodeUtils.GetDocumentationString("SliderValueNode");

        public override bool hasPreview => false;

        public sealed override void UpdateNodeAfterDeserialization()
        {
            var slots = new List<int>();
            MaterialSlot slot0 = new Vector1MaterialSlot(0, "Value", "_Value", SlotType.Output, 0);
            AddSlot(slot0);
            slots.Add(0);

            MaterialSlot slot1 = new Vector2MaterialSlot(1, "Direction", "_Direction", SlotType.Output, Vector2.zero);
            AddSlot(slot1);
            slots.Add(1);

            RemoveSlotsNameNotMatching(slots, true);
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            properties.AddShaderProperty(new Vector3ShaderProperty
            {
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.UnityPerMaterial,
                value = Vector3.zero,
                overrideReferenceName = "_SliderValue"
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine($"$precision {GetVariableNameForSlot(0)} = _SliderValue.x;");
            sb.AppendLine($"$precision2 {GetVariableNameForSlot(1)} = _SliderValue.yz;");
        }
    }
}
