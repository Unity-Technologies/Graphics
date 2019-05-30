using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Basic", "Slider")]
    class SliderNode : AbstractMaterialNode, IGeneratesBodyCode, IPropertyFromNode
    {
        [SerializeField]
        private Vector3 m_Value = new Vector3(0f, 0f, 1f);

        public const int OutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public SliderNode()
        {
            name = "Slider";
            UpdateNodeAfterDeserialization();
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, 0));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        [SliderControl("", true)]
        public Vector3 value
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
                value = value.x,
                rangeValues = new Vector2(value.y, value.z),
                floatType = FloatType.Slider
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode)
        {
            if (generationMode.IsPreview())
                return;

            sb.AppendLine(string.Format("$precision {0} = {1};", GetVariableNameForNode(), m_Value.x));
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return GetVariableNameForNode();
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty(PropertyType.Vector1)
            {
                name = GetVariableNameForNode(),
                floatValue = m_Value.x
            });
        }

        public AbstractShaderProperty AsShaderProperty()
        {
            return new Vector1ShaderProperty
            {
                value = value.x,
                rangeValues = new Vector2(value.y, value.z),
                floatType = FloatType.Slider
            };
        }

        public int outputSlotId { get { return OutputSlotId; } }
    }
}
