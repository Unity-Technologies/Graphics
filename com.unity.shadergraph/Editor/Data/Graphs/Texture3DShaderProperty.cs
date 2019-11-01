using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.Texture3DShaderProperty")]
    public sealed class Texture3DShaderProperty : AbstractShaderProperty<Texture3D>
    {
        [SerializeField]
        int m_Version;

        internal Texture3DShaderProperty()
        {
            displayName = "Texture3D";
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
            return new Texture3DAssetNode { texture = value };
        }

        internal override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                textureValue = value
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

        internal override void OnDeserialized(string json)
        {
            if (m_Version == 0)
            {
                m_Version = 1;
                var v0 = JsonUtility.FromJson<TextureShaderPropertyV0>(json);
                value = (Texture3D)v0.value.texture;
            }
        }
    }
}
