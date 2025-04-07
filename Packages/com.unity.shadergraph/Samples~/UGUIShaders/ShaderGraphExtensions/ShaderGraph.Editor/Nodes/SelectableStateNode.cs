using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "UGUI", "Selectable State")]
    class SelectableStateNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public SelectableStateNode()
        {
            name = "Selectable State";
            UpdateNodeAfterDeserialization();
        }

        //public override string documentationURL => NodeUtils.GetDocumentationString("SelectableStateNode");

        public override bool hasPreview => false;

        public sealed override void UpdateNodeAfterDeserialization()
        {
            var slots = new List<int>();
            MaterialSlot slot = new Vector1MaterialSlot(0, "State", "_State", SlotType.Output, 0); // 0 = out value
            AddSlot(slot);
            slots.Add(0);
            RemoveSlotsNameNotMatching(slots, true);
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            properties.AddShaderProperty(new Vector1ShaderProperty
            {
                floatType = FloatType.Default,
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.UnityPerMaterial,
                value = 0,
                overrideReferenceName = "_State"
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine($"$precision {GetVariableNameForSlot(0)} = _State;");
        }
    }
}
