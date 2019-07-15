using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class Texture3DShaderProperty : AbstractShaderProperty<SerializableTexture>
    {
        public Texture3DShaderProperty()
        {
            displayName = "Texture3D";
            value = new SerializableTexture();
        }
        
        public override PropertyType propertyType => PropertyType.Texture3D;
        
        public override bool isBatchable => false;
        public override bool isExposable => true;
        public override bool isRenamable => true;
        
        public string modifiableTagString => modifiable ? "" : "[NonModifiableTextureData]";

        public override string GetPropertyBlockString()
        {
            return $"{hideTagString}{modifiableTagString}[NoScaleOffset]{referenceName}(\"{displayName}\", 3D) = \"white\" {{}}";
        }
        
        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return $"TEXTURE3D({referenceName}){delimiter} SAMPLER(sampler{referenceName}){delimiter}";
        }

        public override string GetPropertyAsArgumentString()
        {
            return $"TEXTURE3D_PARAM({referenceName}, sampler{referenceName})";
        }
        
        [SerializeField]
        bool m_Modifiable = true;

        public bool modifiable
        {
            get => m_Modifiable;
            set => m_Modifiable = value;
        }
        
        public override AbstractMaterialNode ToConcreteNode()
        {
            return new Texture3DAssetNode { texture = value.texture as Texture3D };
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
            return new Texture3DShaderProperty()
            {
                displayName = displayName,
                hidden = hidden,
                value = value
            };
        }
    }
}
