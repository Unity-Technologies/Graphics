using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Matrix/Matrix 4")]
    public class Matrix4Node : AbstractMaterialNode, IGeneratesBodyCode
    {
        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Value";

        [SerializeField]
        private Vector4[] m_Value = new Vector4[4];

        public Vector4 this[int index]
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

        public Matrix4Node()
        {
            name = "Matrix4";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Matrix4MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
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

            visitor.AddShaderChunk(precision + "4x4 " + propertyName + " = " + precision + "4x4 (" + m_Value[0].x + ", " + m_Value[0].y + ", " + m_Value[0].z + ", " + m_Value[0].w + ", " + m_Value[1].x + ", " + m_Value[1].y + ", " + m_Value[1].z + ", " + m_Value[1].w + ", " + m_Value[2].x + ", " + m_Value[2].y + ", " + m_Value[2].z + ", " + m_Value[2].w + ", " + m_Value[3].x + ", " + m_Value[3].y + ", " + m_Value[3].z + ", " + m_Value[3].w + ");", true);
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
