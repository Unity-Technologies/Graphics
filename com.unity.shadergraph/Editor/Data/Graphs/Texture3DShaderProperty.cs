using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.Texture3DShaderProperty")]
    public sealed class Texture3DShaderProperty : AbstractShaderProperty<SerializableTexture>
    {
        internal Texture3DShaderProperty()
        {
            displayName = "Texture3D";
            value = new SerializableTexture();
        }

        public override PropertyType propertyType => PropertyType.Texture3D;

        internal override bool isBatchable => false;
        internal override bool isExposable => true;
        internal override bool isRenamable => true;

        internal string modifiableTagString => modifiable ? "" : "[NonModifiableTextureData]";

        internal override string GetPropertyBlockString()
        {
            return $"{hideTagString}{modifiableTagString}[NoScaleOffset]{referenceName}(\"{displayName}\", 3D) = \"white\" {{}}";
        }

        internal override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return $"TEXTURE3D({referenceName}){delimiter} SAMPLER(sampler{referenceName}){delimiter}";
        }

        internal override string GetPropertyAsArgumentString()
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

        internal override AbstractMaterialNode ToConcreteNode()
        {
            return new Texture3DAssetNode { texture = value.texture as Texture3D };
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
            return new Texture3DShaderProperty()
            {
                displayName = displayName,
                hidden = hidden,
                value = value
            };
        }
    }
}
