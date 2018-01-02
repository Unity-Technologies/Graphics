using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Basic", "Vector 2")]
    public class Vector2Node : AbstractMaterialNode, IGeneratesBodyCode, IPropertyFromNode
    {
        [SerializeField]
        private Vector2 m_Value;

        public const int OutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public Vector2Node()
        {
            name = "Vector 2";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector2MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        [MultiFloatControl("")]
        public Vector2 value
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

            properties.AddShaderProperty(new Vector2ShaderProperty()
            {
                overrideReferenceName = GetVariableNameForNode(),
                generatePropertyBlock = false,
                value = value
            });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (generationMode.IsPreview())
                return;

            visitor.AddShaderChunk(precision + "2 " + GetVariableNameForNode() + " = " + precision + "2" + m_Value + ";", true);
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return GetVariableNameForNode();
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty(PropertyType.Vector2)
            {
                name = GetVariableNameForNode(),
                vector4Value = m_Value
            });
        }

        public IShaderProperty AsShaderProperty()
        {
            return new Vector2ShaderProperty { value = value };
        }

        public int outputSlotId { get { return OutputSlotId; } }
    }
}
