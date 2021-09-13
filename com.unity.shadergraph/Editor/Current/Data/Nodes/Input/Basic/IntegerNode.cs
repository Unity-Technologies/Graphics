using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Basic", "Integer")]
    class IntegerNode : AbstractMaterialNode, IGeneratesBodyCode, IPropertyFromNode
    {
        [SerializeField]
        private int m_Value;

        public const int OutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public IntegerNode()
        {
            name = "Integer";
            synonyms = new string[] { "whole number" };
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, 0));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        [IntegerControl("")]
        public int value
        {
            get { return m_Value; }
            set
            {
                if (m_Value == value)
                    return;

                m_Value = value;

                Dirty(ModificationScope.Node);
            }
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            properties.AddShaderProperty(new Vector1ShaderProperty()
            {
                overrideReferenceName = GetVariableNameForNode(),
                generatePropertyBlock = false,
                value = value,
                floatType = FloatType.Integer
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (generationMode.IsPreview())
                return;

            sb.AppendLine(string.Format("$precision {0} = {1};", GetVariableNameForNode(), m_Value));
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return GetVariableNameForNode();
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty(PropertyType.Float)
            {
                name = GetVariableNameForNode(),
                floatValue = m_Value
            });
        }

        public AbstractShaderProperty AsShaderProperty()
        {
            return new Vector1ShaderProperty { value = value, floatType = FloatType.Integer };
        }

        public int outputSlotId { get { return OutputSlotId; } }
    }
}
