using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.Rendering.Fullscreen.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "UGUI", "Toggle State")]
    class ToggleStateNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public ToggleStateNode()
        {
            name = "Toggle State";
            UpdateNodeAfterDeserialization();
        }

        //public override string documentationURL => NodeUtils.GetDocumentationString("ToggleStateNode");

        public override bool hasPreview => false;

        public sealed override void UpdateNodeAfterDeserialization()
        {
            var slots = new List<int>();
            MaterialSlot slot = new BooleanMaterialSlot(0, "State", "_isOn", SlotType.Output, false);
            AddSlot(slot);
            slots.Add(0);
            RemoveSlotsNameNotMatching(slots, true);
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            properties.AddBoolProperty("_isOn", false, HLSLDeclaration.UnityPerMaterial);
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine($"$precision {GetVariableNameForSlot(0)} = _isOn;");
        }
    }
}
