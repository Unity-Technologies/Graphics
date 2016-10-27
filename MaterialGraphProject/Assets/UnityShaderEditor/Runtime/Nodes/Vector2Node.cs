using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Vector 2 Node")]
    public class Vector2Node : PropertyNode, IGeneratesBodyCode
    {
        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Value";

        [SerializeField]
        private Vector2 m_Value;

        public Vector2Node()
        {
            name = "V2Node";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, SlotValueType.Vector2, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Vector2; }
        }

        public Vector2 value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            if (exposedState == ExposedState.Exposed)
                visitor.AddShaderProperty(new VectorPropertyChunk(propertyName, description, m_Value, PropertyChunk.HideState.Visible));
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (exposedState == ExposedState.Exposed || generationMode.IsPreview())
                visitor.AddShaderChunk(precision + "2 " + propertyName + ";", true);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (exposedState == ExposedState.Exposed || generationMode.IsPreview())
                return;

            visitor.AddShaderChunk(precision + "2 " +  propertyName + " = " + precision + "2 (" + m_Value.x + ", " + m_Value.y + ");", true);
        }

        public override PreviewProperty GetPreviewProperty()
        {
            return new PreviewProperty
                   {
                       m_Name = propertyName,
                       m_PropType = PropertyType.Vector2,
                       m_Vector4 = m_Value
                   };
        }
    }
}
