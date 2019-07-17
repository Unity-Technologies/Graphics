using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class TextureShaderProperty : AbstractShaderProperty<SerializableTexture>
    {
        public enum DefaultType { White, Black, Grey, Bump }

        public TextureShaderProperty()
        {
            displayName = "Texture2D";
            value = new SerializableTexture();
        }
        
        public override PropertyType propertyType => PropertyType.Texture2D;
        
        public override bool isBatchable => false;
        public override bool isExposable => true;
        public override bool isRenamable => true;
        
        public string modifiableTagString => modifiable ? "" : "[NonModifiableTextureData]";

        public override string GetPropertyBlockString()
        {
            return $"{hideTagString}{modifiableTagString}[NoScaleOffset]{referenceName}(\"{displayName}\", 2D) = \"{defaultType.ToString().ToLower()}\" {{}}";
        }
        
        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return $"TEXTURE2D({referenceName}){delimiter} SAMPLER(sampler{referenceName}); {concretePrecision.ToShaderString()}4 {referenceName}_TexelSize{delimiter}";
        }

        public override string GetPropertyAsArgumentString()
        {
            return $"TEXTURE2D_PARAM({referenceName}, sampler{referenceName})";
        }
        
        [SerializeField]
        bool m_Modifiable = true;

        public bool modifiable
        {
            get => m_Modifiable;
            set => m_Modifiable = value;
        }

        [SerializeField]
        DefaultType m_DefaultType = TextureShaderProperty.DefaultType.White;

        public DefaultType defaultType
        {
            get { return m_DefaultType; }
            set { m_DefaultType = value; }
        }
        
        public override AbstractMaterialNode ToConcreteNode()
        {
            return new Texture2DAssetNode { texture = value.texture };
        }
        
        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                textureValue = value.texture
            };
        }

        public override ShaderInput Copy()
        {
            return new TextureShaderProperty()
            {
                displayName = displayName,
                hidden = hidden,
                value = value
            };
        }
    }
}
