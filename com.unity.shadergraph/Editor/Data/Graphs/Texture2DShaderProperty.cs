using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.TextureShaderProperty")]
    public sealed class Texture2DShaderProperty : AbstractShaderProperty<SerializableTexture>
    {
        public enum DefaultType { White, Black, Grey, Bump }

        internal Texture2DShaderProperty()
        {
            displayName = "Texture2D";
            value = new SerializableTexture();
        }

        public override PropertyType propertyType => PropertyType.Texture2D;

        internal override bool isBatchable => false;
        internal override bool isExposable => true;
        internal override bool isRenamable => true;

        internal string modifiableTagString => modifiable ? "" : "[NonModifiableTextureData]";

        internal override string GetPropertyBlockString()
        {
            return $"{hideTagString}{modifiableTagString}[NoScaleOffset]{referenceName}(\"{displayName}\", 2D) = \"{defaultType.ToString().ToLower()}\" {{}}";
        }

        internal override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return $"TEXTURE2D({referenceName}){delimiter} SAMPLER(sampler{referenceName}){delimiter} {concretePrecision.ToShaderString()}4 {referenceName}_TexelSize{delimiter}";
        }

        internal override string GetPropertyAsArgumentString()
        {
            return $"TEXTURE2D_PARAM({referenceName}, sampler{referenceName}), {concretePrecision.ToShaderString()}4 {referenceName}_TexelSize";
        }

        [SerializeField]
        bool m_Modifiable = true;

        internal bool modifiable
        {
            get => m_Modifiable;
            set => m_Modifiable = value;
        }

        [SerializeField]
        DefaultType m_DefaultType = DefaultType.White;

        public DefaultType defaultType
        {
            get { return m_DefaultType; }
            set { m_DefaultType = value; }
        }

        internal override AbstractMaterialNode ToConcreteNode()
        {
            return new Texture2DAssetNode { texture = value.texture };
        }

        internal override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                textureValue = value.texture
            };
        }

        internal override ShaderInput Copy()
        {
            return new Texture2DShaderProperty()
            {
                displayName = displayName,
                hidden = hidden,
                value = value
            };
        }
    }
}
