using UnityEditor.Rendering.HighDefinition;

using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEngine.Rendering.HighDefinition
{
    internal static class FogVolumeAPI
    {
        internal static readonly string k_BlendModeProperty = "_FogVolumeBlendMode";
        internal static readonly string k_SrcColorBlendProperty = "_FogVolumeSrcColorBlend";
        internal static readonly string k_DstColorBlendProperty = "_FogVolumeDstColorBlend";
        internal static readonly string k_SrcAlphaBlendProperty = "_FogVolumeSrcAlphaBlend";
        internal static readonly string k_DstAlphaBlendProperty = "_FogVolumeDstAlphaBlend";
        internal static readonly string k_ColorBlendOpProperty = "_FogVolumeColorBlendOp";
        internal static readonly string k_AlphaBlendOpProperty = "_FogVolumeAlphaBlendOp";

        internal static readonly string k_SingleScatteringAlbedoProperty = "_FogVolumeSingleScatteringAlbedo";
        internal static readonly string k_FogDistanceProperty = "_FogVolumeFogDistanceProperty";

        internal static void ComputeBlendParameters(LocalVolumetricFogBlendingMode mode, out Rendering.BlendMode srcColorBlend,
            out Rendering.BlendMode srcAlphaBlend, out Rendering.BlendMode dstColorBlend, out Rendering.BlendMode dstAlphaBlend,
            out BlendOp colorBlendOp, out BlendOp alphaBlendOp)
        {
            colorBlendOp = BlendOp.Add;
            alphaBlendOp = BlendOp.Add;

            switch (mode)
            {
                default:
                case LocalVolumetricFogBlendingMode.Additive:
                    srcColorBlend = Rendering.BlendMode.One;
                    dstColorBlend = Rendering.BlendMode.One;
                    srcAlphaBlend = Rendering.BlendMode.One;
                    dstAlphaBlend = Rendering.BlendMode.One;
                    break;
                case LocalVolumetricFogBlendingMode.Multiply:
                    srcColorBlend = Rendering.BlendMode.DstColor;
                    dstColorBlend = Rendering.BlendMode.Zero;
                    srcAlphaBlend = Rendering.BlendMode.DstAlpha;
                    dstAlphaBlend = Rendering.BlendMode.Zero;
                    break;
                case LocalVolumetricFogBlendingMode.Overwrite:
                    srcColorBlend = Rendering.BlendMode.One;
                    dstColorBlend = Rendering.BlendMode.Zero;
                    srcAlphaBlend = Rendering.BlendMode.One;
                    dstAlphaBlend = Rendering.BlendMode.Zero;
                    break;
                case LocalVolumetricFogBlendingMode.Max:
                    srcColorBlend = Rendering.BlendMode.One;
                    dstColorBlend = Rendering.BlendMode.One;
                    srcAlphaBlend = Rendering.BlendMode.One;
                    dstAlphaBlend = Rendering.BlendMode.One;
                    alphaBlendOp = BlendOp.Max;
                    colorBlendOp = BlendOp.Max;
                    break;
                case LocalVolumetricFogBlendingMode.Min:
                    srcColorBlend = Rendering.BlendMode.One;
                    dstColorBlend = Rendering.BlendMode.One;
                    srcAlphaBlend = Rendering.BlendMode.One;
                    dstAlphaBlend = Rendering.BlendMode.One;
                    alphaBlendOp = BlendOp.Min;
                    colorBlendOp = BlendOp.Min;
                    break;
            }
        }

        internal static void SetupFogVolumeKeywordsAndProperties(Material material)
        {
            if (material.HasProperty(k_BlendModeProperty))
            {
                LocalVolumetricFogBlendingMode mode = (LocalVolumetricFogBlendingMode)material.GetFloat(k_BlendModeProperty);
                SetupFogVolumeBlendMode(material, mode);
            }
        }

        internal static int GetPassIndexFromBlendingMode(LocalVolumetricFogBlendingMode mode) => (int)mode;

        internal static void SetupFogVolumeBlendMode(Material material, LocalVolumetricFogBlendingMode mode)
        {
            ComputeBlendParameters(mode, out var srcColorBlend, out var srcAlphaBlend, out var dstColorBlend, out var dstAlphaBlend, out var colorBlendOp, out var alphaBlendOp);

            material.SetFloat(k_SrcColorBlendProperty, (float)srcColorBlend);
            material.SetFloat(k_DstColorBlendProperty, (float)dstColorBlend);
            material.SetFloat(k_SrcAlphaBlendProperty, (float)srcAlphaBlend);
            material.SetFloat(k_DstAlphaBlendProperty, (float)dstAlphaBlend);
            material.SetFloat(k_ColorBlendOpProperty, (float)colorBlendOp);
            material.SetFloat(k_AlphaBlendOpProperty, (float)alphaBlendOp);
            material.SetFloat(k_BlendModeProperty, (float)mode);
        }
    }
}
