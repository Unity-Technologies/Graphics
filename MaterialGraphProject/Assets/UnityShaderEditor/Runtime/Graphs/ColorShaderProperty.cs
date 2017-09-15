using System;
using System.Text;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class ColorShaderProperty : AbstractShaderProperty<Color>
    {
        [SerializeField]
        private bool m_HDR;

        public bool HDR
        {
            get { return m_HDR; }
            set
            {
                if (m_HDR == value)
                    return;

                m_HDR = value;
            }
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Color; }
        }

        public override string GetPropertyBlockString()
        {
            if (!generatePropertyBlock)
                return string.Empty;

            var result = new StringBuilder();
            if (HDR)
                result.Append("[HDR]");
            result.Append(name);
            result.Append("(\"");
            result.Append(description);
            result.Append("\", Color) = (");
            result.Append(value.r);
            result.Append(",");
            result.Append(value.g);
            result.Append(",");
            result.Append(value.b);
            result.Append(",");
            result.Append(value.a);
            result.Append(")");
            return result.ToString();
        }

        public override string GetPropertyDeclarationString()
        {
            return "float4 " + name + ";";
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty
            {
                m_Name = name,
                m_PropType = PropertyType.Color,
                m_Color = value
            };
        }
    }
}
