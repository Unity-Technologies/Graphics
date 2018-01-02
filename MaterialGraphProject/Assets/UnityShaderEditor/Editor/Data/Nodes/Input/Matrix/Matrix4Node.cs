using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Matrix", "Matrix 4x4")]
    public class Matrix4Node : AbstractMaterialNode, IGeneratesBodyCode
    {
        const int kOutputSlotId = 0;
        const string kOutputSlotName = "Out";

        [SerializeField]
        Vector4 m_Row0;

        [SerializeField]
        Vector4 m_Row1;

        [SerializeField]
        Vector4 m_Row2;

        [SerializeField]
        Vector4 m_Row3;

        [MultiFloatControl("", " ", " ", " ", " ")]
        public Vector4 row0
        {
            get { return m_Row0; }
            set { SetRow(ref m_Row0, value); }
        }

        [MultiFloatControl("", " ", " ", " ", " ")]
        public Vector4 row1
        {
            get { return m_Row1; }
            set { SetRow(ref m_Row1, value); }
        }

        [MultiFloatControl("", " ", " ", " ", " ")]
        public Vector4 row2
        {
            get { return m_Row2; }
            set { SetRow(ref m_Row2, value); }
        }

        [MultiFloatControl("", " ", " ", " ", " ")]
        public Vector4 row3
        {
            get { return m_Row3; }
            set { SetRow(ref m_Row3, value); }
        }

        void SetRow(ref Vector4 row, Vector4 value)
        {
            if (value == row)
                return;
            row = value;
            Dirty(ModificationScope.Graph);
        }

        public Matrix4Node()
        {
            name = "Matrix 4x4";
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

            visitor.AddShaderChunk(precision + "4x4 " + propertyName + " = " + precision + "4x4 (" + row0.x + ", " + row0.y + ", " + row0.z + ", " + row0.w + ", " + m_Row1.x + ", " + m_Row1.y + ", " + m_Row1.z + ", " + m_Row1.w + ", " + m_Row2.x + ", " + m_Row2.y + ", " + m_Row2.z + ", " + m_Row2.w + ", " + m_Row3.x + ", " + m_Row3.y + ", " + m_Row3.z + ", " + m_Row3.w + ");", true);
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
                m_Vector4 = m_Value
            };
        }*/
    }
}
