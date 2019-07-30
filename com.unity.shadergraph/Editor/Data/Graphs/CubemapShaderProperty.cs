using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.CubemapShaderProperty")]
    public sealed class CubemapShaderProperty : AbstractShaderProperty<SerializableCubemap>
    {
        internal CubemapShaderProperty()
        {
            displayName = "Cubemap";
            value = new SerializableCubemap();
        }

        public override PropertyType propertyType => PropertyType.Cubemap;

        internal override bool isBatchable => false;
        internal override bool isExposable => true;
        internal override bool isRenamable => true;

        internal string modifiableTagString => modifiable ? "" : "[NonModifiableTextureData]";

        internal override string GetPropertyBlockString()
        {
            return $"{hideTagString}{modifiableTagString}[NoScaleOffset]{referenceName}(\"{displayName}\", CUBE) = \"\" {{}}";
        }

        internal override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return $"TEXTURECUBE({referenceName}){delimiter} SAMPLER(sampler{referenceName}){delimiter}";
        }

        internal override string GetPropertyAsArgumentString()
        {
            return $"TEXTURECUBE_PARAM({referenceName}, sampler{referenceName})";
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
            return new CubemapAssetNode { cubemap = value.cubemap };
        }

        internal override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                cubemapValue = value.cubemap
            };
        }

        internal override ShaderInput Copy()
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
