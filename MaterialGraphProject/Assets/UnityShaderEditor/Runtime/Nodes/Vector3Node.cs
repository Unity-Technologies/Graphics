using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Vector 3 Node")]
    public class Vector3Node : PropertyNode, IGeneratesBodyCode
    {
        private const string kOutputSlotName = "Value";

        [SerializeField]
        private Vector3 m_Value;

        public Vector3Node(IGraph owner) : base(owner)
        {
            name = "V3Node";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(kOutputSlotName, kOutputSlotName, SlotType.Output, 0, SlotValueType.Vector3, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotName });
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Vector3; }
        }

        public Vector3 value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            if (exposed)
                visitor.AddShaderProperty(new VectorPropertyChunk(propertyName, description, m_Value, false));
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode, ConcreteSlotValueType valueType)
        {
            if (exposed || generationMode.IsPreview())
                visitor.AddShaderChunk("float3 " + propertyName + ";", true);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (exposed || generationMode.IsPreview())
                return;

            visitor.AddShaderChunk(precision + "3 " +  propertyName + " = " + precision + "3 (" + m_Value.x + ", " + m_Value.y + ", " + m_Value.z + ");", true);
        }

        public override PreviewProperty GetPreviewProperty()
        {
            return new PreviewProperty
            {
                m_Name = propertyName,
                m_PropType = PropertyType.Vector3,
                m_Vector4 = m_Value
            };
        }
    }
}
