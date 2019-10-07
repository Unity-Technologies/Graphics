using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class Texture3DShaderProperty : AbstractShaderProperty<Texture3D>
    {
        [JsonUpgrade("m_Value", typeof(SerializableTextureUpgrader))]
        public override Texture3D value { get; set; }

        public Texture3DShaderProperty()
        {
            displayName = "Texture3D";
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
            return new Texture3DAssetNode { texture = value };
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                textureValue = value
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
