using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public class VectorProperty : ShaderProperty
    {
        [SerializeField] private Vector4 m_DefaultVector;

        public override object value
        {
            get { return m_DefaultVector; }
            set { m_DefaultVector = (Vector4)value; }
        }

        public Color defaultVector
        {
            get { return m_DefaultVector; }
            set { m_DefaultVector = value; }
        }

        public override void PropertyOnGUI(out bool nameChanged, out bool valuesChanged)
        {
            base.PropertyOnGUI(out nameChanged, out valuesChanged);
            EditorGUI.BeginChangeCheck();
            m_DefaultVector = EditorGUILayout.Vector4Field("Vector", m_DefaultVector);
            valuesChanged |= EditorGUI.EndChangeCheck();
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Vector4; }
        }

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderProperty(new VectorPropertyChunk(name, m_PropertyDescription, m_DefaultVector, false));
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderChunk("float4 " + name + ";", true);
        }

        public override string GenerateDefaultValue()
        {
            return "half4 (" + m_DefaultVector.x + "," + m_DefaultVector.y + "," + m_DefaultVector.z + "," + m_DefaultVector.w + ")";
        }
    }
}
