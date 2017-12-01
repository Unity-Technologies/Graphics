using System;
using System.Text;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class SamplerShaderProperty : AbstractShaderProperty<float>
    {
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
            result.Append(referenceName);
            result.Append("(\"");
            result.Append(displayName);
            result.Append("\", Float) = ");
            result.Append(value);
            return result.ToString();
        }

        public override string GetPropertyDeclarationString()
        {
            return "sampler2D " + referenceName + ";";
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty()
            {
                m_Name = referenceName,
                m_PropType = PropertyType.Float,
                m_Float = value
            };
        }
    }
}
