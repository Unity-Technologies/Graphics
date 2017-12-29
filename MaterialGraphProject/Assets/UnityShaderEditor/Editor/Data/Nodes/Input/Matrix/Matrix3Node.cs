using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Matrix", "Matrix 3x3")]
    public class Matrix3Node : AbstractMaterialNode, IGeneratesBodyCode
    {
        const int kOutputSlotId = 0;
        const string kOutputSlotName = "Out";

        [SerializeField]
        Vector3 m_Row0;

        [SerializeField]
        Vector3 m_Row1;

        [SerializeField]
        Vector3 m_Row2;

        [MultiFloatControl("", " ", " ", " ", " ")]
        public Vector3 row0
        {
            get { return m_Row0; }
            set { SetRow(ref m_Row0, value); }
        }

        [MultiFloatControl("", " ", " ", " ", " ")]
        public Vector3 row1
        {
            get { return m_Row1; }
            set { SetRow(ref m_Row1, value); }
        }

        [MultiFloatControl("", " ", " ", " ", " ")]
        public Vector3 row2
        {
            get { return m_Row2; }
            set { SetRow(ref m_Row2, value); }
        }

        void SetRow(ref Vector3 row, Vector3 value)
        {
            if (value == row)
                return;
            row = value;
            Dirty(ModificationScope.Graph);
        }

        public Matrix3Node()
        {
            name = "Matrix 3x3";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Matrix3MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public string propertyName
        {
            get { return string.Format("{0}_{1}_Uniform", name, GetVariableNameForNode()); }
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return propertyName;
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            //if (exposedState == ExposedState.Exposed || generationMode.IsPreview())
            //    return;

            visitor.AddShaderChunk(precision + "3x3 " + propertyName + " = " + precision + "3x3 (" + m_Row0.x + ", " + m_Row0.y + ", " + m_Row0.z + ", " + m_Row1.x + ", " + m_Row1.y + ", " + m_Row1.z + ", " + m_Row2.x + ", " + m_Row2.y + ", " + m_Row2.z + ");", true);
        }

        [SerializeField]
        string m_Description = string.Empty;

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
                m_Vector3 = m_Value
            };
        }*/
    }
}
