using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Matrix", "Matrix 2x2")]
    public class Matrix2Node : AbstractMaterialNode, IGeneratesBodyCode
    {
        const int kOutputSlotId = 0;
        const string kOutputSlotName = "Out";

        [SerializeField]
        Vector2 m_Row0;

        [SerializeField]
        Vector2 m_Row1;

        [MultiFloatControl("", " ", " ", " ", " ")]
        public Vector2 row0
        {
            get { return m_Row0; }
            set { SetRow(ref m_Row0, value); }
        }

        [MultiFloatControl("", " ", " ", " ", " ")]
        public Vector2 row1
        {
            get { return m_Row1; }
            set { SetRow(ref m_Row1, value); }
        }

        void SetRow(ref Vector2 row, Vector2 value)
        {
            if (value == row)
                return;
            row = value;
            Dirty(ModificationScope.Graph);
        }

        public Matrix2Node()
        {
            name = "Matrix 2x2";
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

            visitor.AddShaderChunk(precision + "2 " +  name + " = " + precision + "2x2 (" + m_Row0.x + ", " + m_Row0.y + ", " + m_Row1.x + ", " + m_Row1.y + ");", true);
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
                m_Vector2 = m_Value
            };
        }*/
    }
}
