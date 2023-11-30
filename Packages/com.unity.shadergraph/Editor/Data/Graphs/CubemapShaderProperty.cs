using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.CubemapShaderProperty")]
    [BlackboardInputInfo(53)]
    public sealed class CubemapShaderProperty : AbstractShaderProperty<SerializableCubemap>
    {
        internal CubemapShaderProperty()
        {
            displayName = "Cubemap";
            value = new SerializableCubemap();
        }

        public override PropertyType propertyType => PropertyType.Cubemap;

        internal override bool isExposable => true;
        internal override bool isRenamable => true;

        internal string modifiableTagString => modifiable ? "" : "[NonModifiableTextureData]";

        internal override string GetPropertyBlockString()
        {
            return $"{hideTagString}{modifiableTagString}[NoScaleOffset]{referenceName}(\"{displayName}\", CUBE) = \"\" {{}}";
        }

        internal override bool AllowHLSLDeclaration(HLSLDeclaration decl) => (decl != HLSLDeclaration.HybridPerInstance) && (decl != HLSLDeclaration.DoNotDeclare);

        internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            action(new HLSLProperty(HLSLType._TextureCube, referenceName, HLSLDeclaration.Global));
            action(new HLSLProperty(HLSLType._SamplerState, "sampler" + referenceName, HLSLDeclaration.Global));
        }

        internal override string GetPropertyAsArgumentString(string precisionString)
        {
            return "UnityTextureCube " + referenceName;
        }

        internal override string GetPropertyAsArgumentStringForVFX(string precisionString)
        {
            return "TEXTURECUBE(" + referenceName + ")";
        }

        internal override string GetHLSLVariableName(bool isSubgraphProperty, GenerationMode mode)
        {
            if (isSubgraphProperty)
                return referenceName;
            else
                return $"UnityBuildTextureCubeStruct({referenceName})";
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
                value = value,
            };
        }
    }
}
