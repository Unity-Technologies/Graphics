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

        internal static void ComputeBlendParameters(LocalVolumetricFogBlendingMode mode, out BlendMode srcColorBlend,
            out BlendMode srcAlphaBlend, out BlendMode dstColorBlend, out BlendMode dstAlphaBlend,
            out BlendOp colorBlendOp, out BlendOp alphaBlendOp)
        {
            colorBlendOp = BlendOp.Add;
            alphaBlendOp = BlendOp.Add;

            switch (mode)
            {
                default:
                case LocalVolumetricFogBlendingMode.Additive:
                    srcColorBlend = BlendMode.One;
                    dstColorBlend = BlendMode.One;
                    srcAlphaBlend = BlendMode.One;
                    dstAlphaBlend = BlendMode.One;
                    break;
                case LocalVolumetricFogBlendingMode.Multiply:
                    srcColorBlend = BlendMode.DstColor;
                    dstColorBlend = BlendMode.Zero;
                    srcAlphaBlend = BlendMode.DstAlpha;
                    dstAlphaBlend = BlendMode.Zero;
                    break;
                case LocalVolumetricFogBlendingMode.Overwrite:
                    srcColorBlend = BlendMode.One;
                    dstColorBlend = BlendMode.Zero;
                    srcAlphaBlend = BlendMode.One;
                    dstAlphaBlend = BlendMode.Zero;
                    break;
                case LocalVolumetricFogBlendingMode.Max:
                    srcColorBlend = BlendMode.One;
                    dstColorBlend = BlendMode.One;
                    srcAlphaBlend = BlendMode.One;
                    dstAlphaBlend = BlendMode.One;
                    alphaBlendOp = BlendOp.Max;
                    colorBlendOp = BlendOp.Max;
                    break;
                case LocalVolumetricFogBlendingMode.Min:
                    srcColorBlend = BlendMode.One;
                    dstColorBlend = BlendMode.One;
                    srcAlphaBlend = BlendMode.One;
                    dstAlphaBlend = BlendMode.One;
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
        }
    }
}
