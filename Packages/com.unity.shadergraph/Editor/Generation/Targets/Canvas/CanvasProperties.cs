using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    static class CanvasProperties
    {
        public static readonly Vector1ShaderProperty StencilComp = new Vector1ShaderProperty()
        {
            overrideReferenceName = "_StencilComp",
            displayName = "Stencil Comparison",
            floatType = FloatType.Default,
            hidden = true,
            value = 8,
            generatePropertyBlock = true,
            overrideHLSLDeclaration = true,
            hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
        };

        public static readonly Vector1ShaderProperty Stencil = new Vector1ShaderProperty()
        {
            overrideReferenceName = "_Stencil",
            displayName = "Stencil ID",
            floatType = FloatType.Default,
            hidden = true,
            value = 0,
            generatePropertyBlock = true,
            overrideHLSLDeclaration = false,
        };

        public static readonly Vector1ShaderProperty StencilOp = new Vector1ShaderProperty()
        {
            overrideReferenceName = "_StencilOp",
            displayName = "Stencil Operation",
            floatType = FloatType.Default,
            hidden = true,
            value = 0,
            generatePropertyBlock = true,
            overrideHLSLDeclaration = false,
        };

        public static readonly Vector1ShaderProperty StencilWriteMask = new Vector1ShaderProperty()
        {
            overrideReferenceName = "_StencilWriteMask",
            displayName = "Stencil Write Mask",
            floatType = FloatType.Default,
            hidden = true,
            value = 255,
            generatePropertyBlock = true,
            overrideHLSLDeclaration = false,
        };

        public static readonly Vector1ShaderProperty StencilReadMask = new Vector1ShaderProperty()
        {
            overrideReferenceName = "_StencilReadMask",
            displayName = "Stencil Read Mask",
            floatType = FloatType.Default,
            hidden = true,
            value = 255,
            generatePropertyBlock = true,
            overrideHLSLDeclaration = false,
        };

        public static readonly Vector1ShaderProperty ColorMask = new Vector1ShaderProperty()
        {
            overrideReferenceName = "_ColorMask",
            displayName = "ColorMask",
            floatType = FloatType.Default,
            hidden = true,
            value = 15,
            generatePropertyBlock = true,
            overrideHLSLDeclaration = false,
        };

        public static readonly Vector1ShaderProperty UIMaskSoftnessX = new Vector1ShaderProperty()
        {
            overrideReferenceName = "_UIMaskSoftnessX",
            displayName = "UIMaskSoftnessX",
            floatType = FloatType.Default,
            hidden = true,
            value = 1.0f,
            generatePropertyBlock = true,
            overrideHLSLDeclaration = false,
        };

        public static readonly Vector1ShaderProperty UIMaskSoftnessY = new Vector1ShaderProperty()
        {
            overrideReferenceName = "_UIMaskSoftnessY",
            displayName = "UIMaskSoftnessY",
            floatType = FloatType.Default,
            hidden = true,
            value = 1.0f,
            generatePropertyBlock = true,
            overrideHLSLDeclaration = false,
        };

        public static readonly Vector4ShaderProperty ClipRect = new Vector4ShaderProperty()
        {
            overrideReferenceName = "_ClipRect",
            displayName = "ClipRect",
            hidden = true,
            generatePropertyBlock = true,
            overrideHLSLDeclaration = false,
        };

        public static readonly Vector1ShaderProperty AlphaTest = new Vector1ShaderProperty()
        {
            floatType = FloatType.Default,
            hidden = true,
            overrideHLSLDeclaration = true,
            value = 0.5f,
            hlslDeclarationOverride = HLSLDeclaration.DoNotDeclare,
            displayName = "_AlphaClip",
            overrideReferenceName = "_AlphaClip",
        };

        public static readonly Texture2DShaderProperty MainTex = new Texture2DShaderProperty()
        {
            overrideReferenceName = "_MainTex",
            displayName = "MainTex",
            generatePropertyBlock = true,
            defaultType = Texture2DShaderProperty.DefaultType.White,
            value = new SerializableTexture(),
            hidden = true,
            overrideHLSLDeclaration = false,
        };
    }
}
