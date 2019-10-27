using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.Texture2DArrayShaderProperty")]
    public sealed class Texture2DArrayShaderProperty : AbstractShaderProperty<SerializableTextureArray>
    {
        internal Texture2DArrayShaderProperty()
        {
            displayName = "Texture2D Array";
            value = new SerializableTextureArray();
        }

        public override PropertyType propertyType => PropertyType.Texture2DArray;

        internal override bool isBatchable => false;
        internal override bool isExposable => true;
        internal override bool isRenamable => true;

        internal string modifiableTagString => modifiable ? "" : "[NonModifiableTextureData]";

        internal override string GetPropertyBlockString()
        {
            return $"{hideTagString}{modifiableTagString}[NoScaleOffset]{referenceName}(\"{displayName}\", 2DArray) = \"white\" {{}}";
        }

        internal override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return $"TEXTURE2D_ARRAY({referenceName}){delimiter} SAMPLER(sampler{referenceName}){delimiter}";
        }

        internal override string GetPropertyAsArgumentString()
        {
            return $"TEXTURE2D_ARRAY_PARAM({referenceName}, sampler{referenceName})";
        }

        [SerializeField]
        bool m_Modifiable = true;

        internal bool modifiable
        {
            get => m_Modifiable;
            set => m_Modifiable = value;
        }

        internal override AbstractMaterialNode ToConcreteNode()
        {
            return new Texture2DArrayAssetNode { texture = value.textureArray };
        }

        internal override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                textureValue = value.textureArray
            };
        }

        internal override ShaderInput Copy()
        {
            return new Texture2DArrayShaderProperty()
            {
                displayName = displayName,
                hidden = hidden,
                value = value
            };
        }
    }
}
