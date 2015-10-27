using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public class FloatProperty : ShaderProperty
    {
        [SerializeField] private float m_DefaultValue;

        public override object value
        {
            get { return m_DefaultValue; }
            set { m_DefaultValue = (float)value; }
        }

        public float defaultValue
        {
            get { return m_DefaultValue; }
            set { m_DefaultValue = value; }
        }

        public override void PropertyOnGUI(out bool nameChanged, out bool valuesChanged)
        {
            base.PropertyOnGUI(out nameChanged, out valuesChanged);
            EditorGUI.BeginChangeCheck();
            m_DefaultValue = EditorGUILayout.FloatField("Vector", m_DefaultValue);
            valuesChanged |= EditorGUI.EndChangeCheck();
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Float; }
        }

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderProperty(new FloatPropertyChunk(name, m_PropertyDescription, m_DefaultValue, false));
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderChunk("float4 " + name + ";", true);
        }

        public override string GenerateDefaultValue()
        {
            return "half4 (" + m_DefaultValue + "," + m_DefaultValue + "," + m_DefaultValue + "," + m_DefaultValue + ")";
        }
    }
}
