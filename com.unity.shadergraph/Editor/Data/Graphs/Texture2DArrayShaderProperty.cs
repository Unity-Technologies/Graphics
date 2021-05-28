using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.Texture2DArrayShaderProperty")]
    [BlackboardInputInfo(51)]
    public class Texture2DArrayShaderProperty : AbstractShaderProperty<SerializableTextureArray>
    {
        internal Texture2DArrayShaderProperty()
        {
            displayName = "Texture2D Array";
            value = new SerializableTextureArray();
        }

        public override PropertyType propertyType => PropertyType.Texture2DArray;

        internal override bool isExposable => true;
        internal override bool isRenamable => true;

        internal string modifiableTagString => modifiable ? "" : "[NonModifiableTextureData]";

        internal override string GetPropertyBlockString()
        {
            return $"{hideTagString}{modifiableTagString}[NoScaleOffset]{referenceName}(\"{displayName}\", 2DArray) = \"\" {{}}";
        }

        internal override bool AllowHLSLDeclaration(HLSLDeclaration decl) => false; // disable UI, nothing to choose

        internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            action(new HLSLProperty(HLSLType._Texture2DArray, referenceName, HLSLDeclaration.Global));
            action(new HLSLProperty(HLSLType._SamplerState, "sampler" + referenceName, HLSLDeclaration.Global));
        }

        internal override string GetPropertyAsArgumentString()
        {
            return "UnityTexture2DArray " + referenceName;
        }

        internal override string GetPropertyAsArgumentStringForVFX()
        {
            return "TEXTURE2D_ARRAY(" + referenceName + ")";
        }

        internal override string GetHLSLVariableName(bool isSubgraphProperty)
        {
            if (isSubgraphProperty)
                return referenceName;
            else
                return $"UnityBuildTexture2DArrayStruct({referenceName})";
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
                value = value,
                precision = precision,
            };
        }
    }
}
