using System.Collections.Generic;
using UnityEditor.MaterialGraph.Drawing.Controls;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Color")]
    public class ColorNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        [SerializeField]
        private Color m_Color;

        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Color";

        public ColorNode()
        {
            name = "Color";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        [ColorControl("")]
        public Color color
        {
            get { return m_Color; }
            set
            {
                if (m_Color == value)
                    return;

                m_Color = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Node);
                }
            }
        }


        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            properties.AddShaderProperty(new ColorShaderProperty()
            {
                overrideReferenceName = GetVariableNameForNode(),
                generatePropertyBlock = false,
                value = color,
                HDR = false
            });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (generationMode.IsPreview())
                return;

            visitor.AddShaderChunk(precision + "4 " + GetVariableNameForNode() + " = " + precision + "4 (" + color.r + ", " + color.g + ", " + color.b + ", " + color.a + ");", true);
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return GetVariableNameForNode();
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty
            {
                m_Name = GetVariableNameForNode(),
                m_PropType = PropertyType.Color,
                m_Color = color
            });
        }
    }
}
