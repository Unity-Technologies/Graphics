using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
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

        public ColorShaderProperty()
        {
            displayName = "Color";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Color; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(value.r, value.g, value.b, value.a); }
        }

        public override string GetPropertyBlockString()
        {
            if (!generatePropertyBlock)
                return string.Empty;

            var result = new StringBuilder();
            if (HDR)
                result.Append("[HDR]");
            result.Append(referenceName);
            result.Append("(\"");
            result.Append(displayName);
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
            return "float4 " + referenceName + ";";
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(PropertyType.Color)
            {
                name = referenceName,
                colorValue = value
            };
        }

        public override INode ToConcreteNode()
        {
            return new ColorNode { color = value };
        }
    }
}
