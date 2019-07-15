using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class CubemapShaderProperty : AbstractShaderProperty<SerializableCubemap>
    {
        public CubemapShaderProperty()
        {
            displayName = "Cubemap";
            value = new SerializableCubemap();
        }
        
        public override PropertyType propertyType => PropertyType.Cubemap;
        
        public override bool isBatchable => false;
        public override bool isExposable => true;
        public override bool isRenamable => true;
        
        public string modifiableTagString => modifiable ? "" : "[NonModifiableTextureData]";

        public override string GetPropertyBlockString()
        {
            return $"{hideTagString}{modifiableTagString}[NoScaleOffset]{referenceName}(\"{displayName}\", CUBE) = \"\" {{}}";
        }
        
        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return $"TEXTURECUBE({referenceName}){delimiter} SAMPLER(sampler{referenceName}){delimiter}";
        }

        public override string GetPropertyAsArgumentString()
        {
            return $"TEXTURECUBE_PARAM({referenceName}, sampler{referenceName})";
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
            return new CubemapAssetNode { cubemap = value.cubemap };
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                cubemapValue = value.cubemap
            };
        }

        public override ShaderInput Copy()
        {
            return new CubemapShaderProperty()
            {
                displayName = displayName,
                hidden = hidden,
                value = value
            };
        }
    }
}
