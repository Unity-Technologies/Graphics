using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Basic", "HDR Color")]
    public class HDRColorNode : AbstractMaterialNode, IGeneratesBodyCode, IPropertyFromNode
    {
        [SerializeField]
        private Color m_Color;

        public const int OutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public HDRColorNode()
        {
            name = "HDR Color";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new ColorRGBAMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        [ColorControl("", ColorMode.HDR)]
        public Color color
        {
            get { return m_Color; }
            set
            {
                if (m_Color == value)
                    return;

                m_Color = value;
                Dirty(ModificationScope.Node);
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
                HDR = true
            });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (generationMode.IsPreview())
                return;

            visitor.AddShaderChunk(string.Format(
                @"{0}4 {1} = IsGammaSpace() ? {0}4({2}, {3}, {4}, {5}) : {0}4(SRGBToLinear({0}3({2}, {3}, {4})), {5});"
                , precision
                , GetVariableNameForNode()
                , color.r
                , color.g
                , color.b
                , color.a), true);
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return GetVariableNameForNode();
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty(PropertyType.Color)
            {
                name = GetVariableNameForNode(),
                colorValue = color
            });
        }

        public IShaderProperty AsShaderProperty()
        {
            return new ColorShaderProperty { value = color, HDR = true };
        }

        public int outputSlotId { get { return OutputSlotId; } }
    }
}
