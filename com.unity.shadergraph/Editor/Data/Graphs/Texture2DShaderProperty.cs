using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.TextureShaderProperty")]
    [BlackboardInputInfo(50)]
    public sealed class Texture2DShaderProperty : AbstractShaderProperty<SerializableTexture>
    {
        public enum DefaultType { White, Black, Grey, Bump }

        internal Texture2DShaderProperty()
        {
            displayName = "Texture2D";
            value = new SerializableTexture();
        }

        public override PropertyType propertyType => PropertyType.Texture2D;

        internal override bool isExposable => true;
        internal override bool isRenamable => true;

        internal string modifiableTagString => modifiable ? "" : "[NonModifiableTextureData]";

        internal override string GetPropertyBlockString()
        {
            return $"{hideTagString}{modifiableTagString}[NoScaleOffset]{referenceName}(\"{displayName}\", 2D) = \"{defaultType.ToString().ToLower()}\" {{}}";
        }

        // Texture2D properties cannot be set via Hybrid path at the moment; disallow that choice
        internal override bool AllowHLSLDeclaration(HLSLDeclaration decl) => (decl != HLSLDeclaration.HybridPerInstance) && (decl != HLSLDeclaration.DoNotDeclare);

        internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            HLSLDeclaration decl = (generatePropertyBlock ? HLSLDeclaration.UnityPerMaterial : HLSLDeclaration.Global);

            action(new HLSLProperty(HLSLType._Texture2D, referenceName, HLSLDeclaration.Global));
            action(new HLSLProperty(HLSLType._SamplerState, "sampler" + referenceName, HLSLDeclaration.Global));
            action(new HLSLProperty(HLSLType._float4, referenceName + "_TexelSize", decl));
            // action(new HLSLProperty(HLSLType._float4, referenceName + "_ST", decl)); // TODO: allow users to make use of the ST values
        }

        internal override string GetPropertyAsArgumentString()
        {
            return "UnityTexture2D " + referenceName;
        }

        internal override string GetHLSLVariableName(bool isSubgraphProperty)
        {
            if (isSubgraphProperty)
                return referenceName;
            else
                return $"UnityBuildTexture2DStructNoScale({referenceName})";
        }

        [SerializeField]
        bool m_Modifiable = true;

        internal bool modifiable
        {
            get => m_Modifiable;
            set => m_Modifiable = value;
        }

        [SerializeField]
        DefaultType m_DefaultType = DefaultType.White;

        public DefaultType defaultType
        {
            get { return m_DefaultType; }
            set { m_DefaultType = value; }
        }

        internal override AbstractMaterialNode ToConcreteNode()
        {
            return new Texture2DAssetNode { texture = value.texture };
        }

        internal override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                textureValue = value.texture,
                texture2DDefaultType = defaultType
            };
        }

        internal override ShaderInput Copy()
        {
            return new Texture2DShaderProperty()
            {
                displayName = displayName,
                hidden = hidden,
                value = value,
                defaultType = defaultType,
                precision = precision,
            };
        }
    }
}
