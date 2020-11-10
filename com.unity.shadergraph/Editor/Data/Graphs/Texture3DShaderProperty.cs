using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.Texture3DShaderProperty")]
    [BlackboardInputInfo(52)]
    public sealed class Texture3DShaderProperty : AbstractShaderProperty<SerializableTexture>
    {
        internal Texture3DShaderProperty()
        {
            displayName = "Texture3D";
            value = new SerializableTexture();
        }

        public override PropertyType propertyType => PropertyType.Texture3D;

        internal override bool isExposable => true;
        internal override bool isRenamable => true;

        internal string modifiableTagString => modifiable ? "" : "[NonModifiableTextureData]";

        internal override string GetPropertyBlockString()
        {
            return $"{hideTagString}{modifiableTagString}[NoScaleOffset]{referenceName}(\"{displayName}\", 3D) = \"white\" {{}}";
        }

        internal override bool AllowHLSLDeclaration(HLSLDeclaration decl) => false; // disable UI, nothing to choose

        internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            action(new HLSLProperty(HLSLType._Texture3D, referenceName, HLSLDeclaration.Global));
            action(new HLSLProperty(HLSLType._SamplerState, "sampler" + referenceName, HLSLDeclaration.Global));

            // add struct macro
            Action<ShaderStringBuilder> structDecl = (builder) =>
            {
                builder.AppendIndentation();
                builder.Append("#define ");
                builder.Append(referenceName);
                builder.Append("_struct UnityBuildTexture3DStruct(TEXTURE3D_ARGS(");
                builder.Append(referenceName);
                builder.Append(", sampler"); builder.Append(referenceName);
                builder.Append("))");
//                 builder.Append(referenceName); builder.Append("_TexelSize, ");
//                 builder.Append(referenceName); builder.Append("_ST)");
                builder.AppendNewLine();
            };

            action(new HLSLProperty(HLSLType._CUSTOM, referenceName + "_struct", HLSLDeclaration.Global, concretePrecision)
            {
                customDeclaration = structDecl
            });
        }

        internal override string GetPropertyAsArgumentString()
        {
            return "UnityTexture3D " + referenceName + "_struct";
        }

        internal override string GetHLSLVariableName()
        {
            return referenceName + "_struct";
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
                value = value,
                precision = precision
            };
        }
    }
}
