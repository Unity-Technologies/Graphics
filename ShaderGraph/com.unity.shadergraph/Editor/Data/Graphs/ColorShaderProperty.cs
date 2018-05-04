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
        private ColorMode m_ColorMode;

        public ColorMode colorMode
        {
            get { return m_ColorMode; }
            set
            {
                if (m_ColorMode == value)
                    return;

                m_ColorMode = value;
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
            if (colorMode == ColorMode.HDR)
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

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return string.Format("float4 {0}{1}", referenceName, delimiter);
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
            return new ColorNode { color = new ColorNode.Color(value, colorMode) };
        }

        public override IShaderProperty Copy()
        {
            var copied = new ColorShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
    }
}
