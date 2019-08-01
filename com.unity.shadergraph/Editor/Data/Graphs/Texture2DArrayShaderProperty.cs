using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class Texture2DArrayShaderProperty : AbstractShaderProperty<SerializableTextureArray>
    {
        public Texture2DArrayShaderProperty()
        {
            displayName = "Texture2D Array";
            value = new SerializableTextureArray();
        }
        
        public override PropertyType propertyType => PropertyType.Texture2DArray;
        
        public override bool isExposable => true;
        public override bool isRenamable => true;
        
        public string modifiableTagString => modifiable ? "" : "[NonModifiableTextureData]";

        public override string GetPropertyBlockString()
        {
            return $"{hideTagString}{modifiableTagString}[NoScaleOffset]{referenceName}(\"{displayName}\", 2DArray) = \"white\" {{}}";
        }

        public override IEnumerable<(string cbName, string line)> GetPropertyDeclarationStrings()
        {
            yield return (null, $"TEXTURE2D_ARRAY({referenceName})");
        }

        public override string GetPropertyAsArgumentString()
        {
            return $"TEXTURE2D_ARRAY_PARAM({referenceName}, sampler{referenceName})";
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
            return new Texture2DArrayAssetNode { texture = value.textureArray };
        }

        
        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                textureValue = value.textureArray
            };
        }

        public override ShaderInput Copy()
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
