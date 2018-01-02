using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Basic", "Vector 4")]
    public class Vector4Node : AbstractMaterialNode, IGeneratesBodyCode, IPropertyFromNode
    {
        [SerializeField]
        private Vector4 m_Value;

        public const int OutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public Vector4Node()
        {
            name = "Vector 4";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        [MultiFloatControl("")]
        public Vector4 value
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

            properties.AddShaderProperty(new Vector4ShaderProperty()
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

            visitor.AddShaderChunk(precision + "4 " + GetVariableNameForNode() + " = " + precision + "4" + m_Value + ";", true);
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return GetVariableNameForNode();
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty(PropertyType.Vector4)
            {
                name = GetVariableNameForNode(),
                vector4Value = m_Value
            });
        }

        public IShaderProperty AsShaderProperty()
        {
            return new Vector4ShaderProperty { value = value };
        }

        public int outputSlotId { get { return OutputSlotId; } }
    }
}
