using System;
using System.Text;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class CubemapShaderProperty : AbstractShaderProperty<SerializableCubemap>
    {
        [SerializeField]
        private bool m_Modifiable = true;

        public CubemapShaderProperty()
        {
            value = new SerializableCubemap();
            displayName = "Cubemap";
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Cubemap; }
        }

        public bool modifiable
        {
            get { return m_Modifiable; }
            set { m_Modifiable = value; }
        }

        public override Vector4 defaultValue
        {
            get { return new Vector4(); }
        }

        public override string GetPropertyBlockString()
        {
            var result = new StringBuilder();
            if (!m_Modifiable)
            {
                result.Append("[NonModifiableTextureData] ");
            }
            result.Append("[NoScaleOffset] ");

            result.Append(referenceName);
            result.Append("(\"");
            result.Append(displayName);
            result.Append("\", CUBE) = \"\" {}");
            return result.ToString();
        }

        public override string GetPropertyDeclarationString()
        {
            return "samplerCUBE " + referenceName + ";";
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty()
            {
                m_Name = referenceName,
                m_PropType = PropertyType.Cubemap,
                m_Cubemap = value.cubemap
            };
        }
    }
}
