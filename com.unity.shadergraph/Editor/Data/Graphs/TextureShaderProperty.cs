using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    class TextureShaderProperty : AbstractShaderProperty<Texture>
    {
        [SerializeField]
        int m_Version;

        public enum DefaultType { White, Black, Grey, Bump }

        public TextureShaderProperty()
        {
            displayName = "Texture2D";
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
            return new Texture2DAssetNode { texture = value };
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
            return new TextureShaderProperty()
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
                value = v0.value.texture;
            }
        }
    }
}
