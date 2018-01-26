using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public enum FloatType
    {
        Float,
        Slider,
        Integer
    }

    [Serializable]
    public class FloatShaderProperty : AbstractShaderProperty<float>
    {
        public FloatShaderProperty()
        {
            displayName = "Float";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Float; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(value, value, value, value); }
        }

        private FloatType m_FloatType = FloatType.Float;

        public FloatType floatType
        {
            get { return m_FloatType; }
            set
            {
                if (m_FloatType == value)
                    return;
                m_FloatType = value;
            }
        }

        private Vector2 m_RangeValues = new Vector2(0, 1);

        public Vector2 rangeValues
        {
            get { return m_RangeValues; }
            set
            {
                if (m_RangeValues == value)
                    return;
                m_RangeValues = value;
            }
        }

        public override string GetPropertyBlockString()
        {
            var result = new StringBuilder();
            result.Append(referenceName);
            result.Append("(\"");
            result.Append(displayName);
            switch(m_FloatType)
            {
                case FloatType.Slider:
                    result.Append("\", Range(");
                    result.Append(m_RangeValues.x + ", " + m_RangeValues.y);
                    result.Append(")) = ");
                    break;
                case FloatType.Integer:
                    result.Append("\", Int) = ");
                    break;
                default:
                    result.Append("\", Float) = ");
                    break;
            }
            result.Append(value);
            return result.ToString();
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return string.Format("float {0}{1}", referenceName, delimiter);
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(PropertyType.Float)
            {
                name = referenceName,
                floatValue = value
            };
        }

        public override INode ToConcreteNode()
        {
            switch(m_FloatType)
            {
                case FloatType.Slider:
                    return new SliderNode { value = new Vector3(value, m_RangeValues.x, m_RangeValues.y) };
                case FloatType.Integer:
                    return new IntegerNode { value = (int)value };
                default:
                    return new Vector1Node { value = value };
            }
        }
    }
}
