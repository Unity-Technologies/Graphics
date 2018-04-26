using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class TextureShaderProperty : AbstractShaderProperty<SerializableTexture>
    {
        [SerializeField]
        private bool m_Modifiable = true;

        public TextureShaderProperty()
        {
            value = new SerializableTexture();
            displayName = "Texture";
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
            result.Append("\", 2D) = \"white\" {}");
            return result.ToString();
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return string.Format("TEXTURE2D({0}){1} SAMPLER(sampler{0}){1}", referenceName, delimiter);
        }

        public override string GetPropertyAsArgumentString()
        {
            return string.Format("TEXTURE2D_ARGS({0}, sampler{0})", referenceName);
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(PropertyType.Texture)
            {
                name = referenceName,
                textureValue = value.texture
            };
        }

        public override INode ToConcreteNode()
        {
            return new Texture2DAssetNode { texture = value.texture };
        }

        public override IShaderProperty Copy()
        {
            var copied = new TextureShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            return copied;
        }
    }
}
