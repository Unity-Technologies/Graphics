using UnityEngine;

namespace UnityEditor.Graphs.Material
{
    public class ColorProperty : ShaderProperty
    {
        [SerializeField] private Color m_DefaultColor;

        public override object value
        {
            get { return m_DefaultColor; }
            set { m_DefaultColor = (Color)value; }
        }

        public Color defaultColor
        {
            get { return m_DefaultColor; }
            set { m_DefaultColor = value; }
        }

        public override void PropertyOnGUI(out bool nameChanged, out bool valuesChanged)
        {
            base.PropertyOnGUI(out nameChanged, out valuesChanged);
            EditorGUI.BeginChangeCheck();
            m_DefaultColor = EditorGUILayout.ColorField("Color", m_DefaultColor);
            valuesChanged |= EditorGUI.EndChangeCheck();
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Color; }
        }

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderProperty(new ColorPropertyChunk(name, m_PropertyDescription, m_DefaultColor, false));
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderChunk("float4 " + name + ";", true);
        }

        public override string GenerateDefaultValue()
        {
            return "half4 (" + m_DefaultColor.r + "," + m_DefaultColor.g + "," + m_DefaultColor.b + "," + m_DefaultColor.a + ")";
        }
    }
}
