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
        public enum DefaultType { White, Black, Grey, NormalMap, LinearGrey, Red }

        static readonly string[] k_DefaultTypeNames = new string[]
        {
            "white",
            "black",
            "grey",
            "bump",
            "linearGrey",
            "red",
        };

        internal static string ToShaderLabString(DefaultType defaultType)
        {
            int index = (int)defaultType;
            if ((index >= 0) && (index < k_DefaultTypeNames.Length))
                return k_DefaultTypeNames[index];
            return string.Empty;
        }

        internal Texture2DShaderProperty()
        {
            displayName = "Texture2D";
            value = new SerializableTexture();
        }

        public override PropertyType propertyType => PropertyType.Texture2D;

        [SerializeField]
        internal bool isMainTexture = false;

        internal override bool isExposable => true;
        internal override bool isRenamable => true;

        internal string modifiableTagString => modifiable ? "" : "[NonModifiableTextureData]";

        [SerializeField]
        internal bool useTilingAndOffset = false;

        internal string useSTString => useTilingAndOffset ? "" : "[NoScaleOffset]";
        internal string mainTextureString => isMainTexture ? "[MainTexture]" : "";

        internal override string GetPropertyBlockString()
        {
            var normalTagString = (defaultType == DefaultType.NormalMap) ? "[Normal]" : "";
            return $"{hideTagString}{modifiableTagString}{normalTagString}{mainTextureString}{useSTString}{referenceName}(\"{displayName}\", 2D) = \"{ToShaderLabString(defaultType)}\" {{}}";
        }

        // Texture2D properties cannot be set via Hybrid path at the moment; disallow that choice
        internal override bool AllowHLSLDeclaration(HLSLDeclaration decl) => (decl != HLSLDeclaration.HybridPerInstance) && (decl != HLSLDeclaration.DoNotDeclare);

        internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            HLSLDeclaration decl = (generatePropertyBlock ? HLSLDeclaration.UnityPerMaterial : HLSLDeclaration.Global);

            action(new HLSLProperty(HLSLType._Texture2D, referenceName, HLSLDeclaration.Global));
            action(new HLSLProperty(HLSLType._SamplerState, "sampler" + referenceName, HLSLDeclaration.Global));
            action(new HLSLProperty(HLSLType._float4, referenceName + "_TexelSize", decl));
            if (useTilingAndOffset)
            {
                action(new HLSLProperty(HLSLType._float4, referenceName + "_ST", decl));
            }
        }

        internal override string GetPropertyAsArgumentString(string precisionString)
        {
            return "UnityTexture2D " + referenceName;
        }

        internal override string GetPropertyAsArgumentStringForVFX(string precisionString)
        {
            return "TEXTURE2D(" + referenceName + ")";
        }

        internal override string GetHLSLVariableName(bool isSubgraphProperty, GenerationMode mode)
        {
            if (isSubgraphProperty)
                return referenceName;
            else
            {
                if (useTilingAndOffset)
                {
                    return $"UnityBuildTexture2DStruct({referenceName})";
                }
                else
                {
                    return $"UnityBuildTexture2DStructNoScale({referenceName})";
                }
            }
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
                value = value,
                defaultType = defaultType,
                useTilingAndOffset = useTilingAndOffset,
                isMainTexture = isMainTexture
            };
        }

        internal override void OnBeforePasteIntoGraph(GraphData graph)
        {
            if (isMainTexture)
            {
                Texture2DShaderProperty existingMain = graph.GetMainTexture();
                if (existingMain != null && existingMain != this)
                {
                    isMainTexture = false;
                }
            }
            base.OnBeforePasteIntoGraph(graph);
        }
    }
}
