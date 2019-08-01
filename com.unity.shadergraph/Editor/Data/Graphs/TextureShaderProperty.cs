using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class TextureShaderProperty : AbstractShaderProperty<SerializableTexture>, ISplattableShaderProperty
    {
        public enum DefaultType { White, Black, Grey, Bump }

        public TextureShaderProperty()
        {
            displayName = "Texture2D";
            value = new SerializableTexture();
        }
        
        public override PropertyType propertyType => PropertyType.Texture2D;

        public override bool isExposable => true;
        public override bool isRenamable => true;
        
        public string modifiableTagString => modifiable ? "" : "[NonModifiableTextureData]";

        public override string GetPropertyBlockString()
        {
            return $"{hideTagString}{modifiableTagString}{this.PerSplatString()}[NoScaleOffset]{referenceName}(\"{displayName}\", 2D) = \"{defaultType.ToString().ToLower()}\" {{}}";
        }

        public override string GetPropertyAsArgumentString()
        {
            // TODO
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
        DefaultType m_DefaultType = DefaultType.White;

        public DefaultType defaultType
        {
            get => m_DefaultType;
            set => m_DefaultType = value;
        }

        [SerializeField]
        bool m_Splat = false;

        public bool splat
        {
            get => m_Splat;
            set => m_Splat = value;
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
                value = value,
                splat = splat
            };
        }
    }
}
