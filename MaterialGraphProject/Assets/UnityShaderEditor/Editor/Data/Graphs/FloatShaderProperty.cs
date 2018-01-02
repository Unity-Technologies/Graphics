using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
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

        public override string GetPropertyBlockString()
        {
            var result = new StringBuilder();
            //  if (m_FloatType == FloatPropertyChunk.FloatType.Toggle)
            //     result.Append("[Toggle]");
            //  else if (m_FloatType == FloatPropertyChunk.FloatType.PowerSlider)
            //      result.Append("[PowerSlider(" + m_rangeValues.z + ")]");
            result.Append(referenceName);
            result.Append("(\"");
            result.Append(displayName);

            //if (m_FloatType == FloatPropertyChunk.FloatType.Float || m_FloatType == FloatPropertyChunk.FloatType.Toggle)
            //{
            result.Append("\", Float) = ");
            /* }
             else if (m_FloatType == FloatPropertyChunk.FloatType.Range || m_FloatType == FloatPropertyChunk.FloatType.PowerSlider)
             {
                 result.Append("\", Range(");
                 result.Append(m_rangeValues.x + ", " + m_rangeValues.y);
                 result.Append(")) = ");
             }*/
            result.Append(value);
            return result.ToString();
        }

        public override string GetPropertyDeclarationString()
        {
            return "float " + referenceName + ";";
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
            return new Vector1Node { value = value };
        }
    }
}
