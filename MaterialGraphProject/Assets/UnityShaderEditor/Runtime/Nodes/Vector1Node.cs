using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Vector 1 Node")]
    public class Vector1Node : PropertyNode, IGeneratesBodyCode
    {
        [SerializeField]
        private float m_Value;

        private const string kOutputSlotName = "Value";

        public Vector1Node()
        {
            name = "V1Node";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(kOutputSlotName, kOutputSlotName, SlotType.Output, 0, SlotValueType.Vector1, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotName });
        }
        
        public override PropertyType propertyType
        {
            get { return PropertyType.Float; }
        }

        public float value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            if (exposed)
                visitor.AddShaderProperty(new FloatPropertyChunk(propertyName, description, m_Value, false));
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode, ConcreteSlotValueType valueType)
        {
            if (exposed || generationMode.IsPreview())
               visitor.AddShaderChunk("float " + propertyName + ";", true);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (exposed || generationMode.IsPreview())
                return;
            
            visitor.AddShaderChunk(precision + " " + propertyName + " = " + m_Value + ";", true);
        }
        
        public override PreviewProperty GetPreviewProperty()
        {
            return new PreviewProperty
            {
                m_Name = propertyName,
                m_PropType = PropertyType.Float,
                m_Float = m_Value
            };
        }
    }
}
