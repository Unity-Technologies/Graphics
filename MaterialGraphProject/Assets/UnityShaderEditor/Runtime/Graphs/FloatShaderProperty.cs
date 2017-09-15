using System;
using System.Text;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class FloatShaderProperty : AbstractShaderProperty<float>
    {
        public override PropertyType propertyType
        {
            get { return PropertyType.Float; }
        }

        public override string GetPropertyBlockString()
        {
            var result = new StringBuilder();
          //  if (m_FloatType == FloatPropertyChunk.FloatType.Toggle)
           //     result.Append("[Toggle]");
          //  else if (m_FloatType == FloatPropertyChunk.FloatType.PowerSlider)
          //      result.Append("[PowerSlider(" + m_rangeValues.z + ")]");
            result.Append(name);
            result.Append("(\"");
            result.Append(description);

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
            return "float " + name + ";";
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty()
            {
                m_Name = name,
                m_PropType = PropertyType.Float,
                m_Float = value
            };
        }
    }
}
