using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Matrix/Matrix 2")]
    public class Matrix2Node : AbstractMaterialNode, IGeneratesBodyCode
    {
        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Value";

        [SerializeField]
        private Vector2[] m_Value = new Vector2[2];

        public Vector2 this[int index]
        {
            get { return m_Value[index]; }
            set
            {
                if (m_Value[index] == value)
                    return;

                m_Value[index] = value;

                if (onModified != null)
                    onModified(this, ModificationScope.Node);
            }
        }

        public Matrix2Node()
        {
            name = "Matrix2";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Matrix2MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public string propertyName
        {
            get
            {
                return string.Format("{0}_{1}_Uniform", name, GetVariableNameForNode());
            }
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return propertyName;
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            //if (exposedState == ExposedState.Exposed || generationMode.IsPreview())
            //    return;

            visitor.AddShaderChunk(precision + "2 " +  name + " = " + precision + "2x2 (" + m_Value[0].x + ", " + m_Value[0].y + ", " + m_Value[1].x + ", " + m_Value[1].y + ");", true);
        }

        [SerializeField]
        private string m_Description = string.Empty;

        public string description
        {
            get
            {
                return string.IsNullOrEmpty(m_Description) ? name : m_Description;
            }
            set { m_Description = value; }
        }

        // TODO - Remove Property entries everywhere?
        // Matrix cant be a shader property
        /*public override PropertyType propertyType
        {
            get { return PropertyType.Matrix2; }
        }*/

        /*public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            if (exposedState == ExposedState.Exposed)
                visitor.AddShaderProperty(new VectorPropertyChunk(propertyName, description, m_Value, PropertyChunk.HideState.Visible));
        }*/

        /*public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (exposedState == ExposedState.Exposed || generationMode.IsPreview())
                visitor.AddShaderChunk(precision + "2 " + propertyName + ";", true);
        }*/

        /*public override PreviewProperty GetPreviewProperty()
        {
            return new PreviewProperty
            {
                m_Name = propertyName,
                m_PropType = PropertyType.Vector2,
                m_Vector4 = m_Value
            };
        }*/
    }
}
