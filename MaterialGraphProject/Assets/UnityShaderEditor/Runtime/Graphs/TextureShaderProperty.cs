using System;
using System.Text;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class TextureShaderProperty : AbstractShaderProperty<SerializableTexture>
    {
        [SerializeField]
        private bool m_Modifiable;

        public TextureShaderProperty()
        {
            value = new SerializableTexture();
        }

        public override PropertyType propertyType
        {
            get { return PropertyType.Texture; }
        }

        public bool modifiable
        {
            get { return m_Modifiable; }
            set { m_Modifiable = value; }
        }

        public override string GetPropertyBlockString()
        {
            var result = new StringBuilder();
            if (!m_Modifiable)
            {
                result.Append("[HideInInspector] ");
                result.Append("[NonModifiableTextureData] ");
            }
            result.Append("[NoScaleOffset] ");

            result.Append(name);
            result.Append("(\"");
            result.Append(description);
            result.Append("\", 2D) = \"white\" {}");
            return result.ToString();
        }

        public override string GetPropertyDeclarationString()
        {
            return "UNITY_DECLARE_TEX2D(" + name + ");";
        }


        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty()
            {
                m_Name = name,
                m_PropType = PropertyType.Texture,
                m_Texture = value.texture
            };
        }
    }
}
