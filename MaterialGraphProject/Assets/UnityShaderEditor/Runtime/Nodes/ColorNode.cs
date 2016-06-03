using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Color Node")]
    public class ColorNode : PropertyNode, IGeneratesBodyCode
    {
        [SerializeField]
        private Color m_Color;

        private const string kOutputSlotName = "Color";

        public ColorNode(IGraph owner) : base(owner)
        {
            name = "ColorNode";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(kOutputSlotName, kOutputSlotName, SlotType.Output, 0, SlotValueType.Vector4, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] {kOutputSlotName});
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Color; }
        }

        public Color color
        {
            get { return m_Color; }
            set { m_Color = value; }
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode, ConcreteSlotValueType slotValueType)
        {
            if (exposed || generationMode.IsPreview())
                visitor.AddShaderChunk("float4 " + propertyName + ";", true);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            // we only need to generate node code if we are using a constant... otherwise we can just refer to the property :)
            if (exposed || generationMode.IsPreview())
                return;

            visitor.AddShaderChunk(precision + "4 " + propertyName + " = " + precision + "4 (" + m_Color.r + ", " + m_Color.g + ", " + m_Color.b + ", " + m_Color.a + ");", true);
        }
        
        public override PreviewProperty GetPreviewProperty()
        {
            return new PreviewProperty
            {
                m_Name = propertyName,
                m_PropType = PropertyType.Color,
                m_Color = m_Color
            };
        }

    }
}
