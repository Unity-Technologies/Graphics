using System;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal enum InstancingOptions
    {
        AssumeUniformScaling,
        RenderingLayer,
        NoMatrices,
        NoLightProbe,
        NoLightmap,
        NoLodFade,
    }

    [GenerationAPI]
    internal static class InstancingOptionsExtensions
    {
        public static string ToShaderString(this InstancingOptions options)
        {
            switch (options)
            {
                case InstancingOptions.AssumeUniformScaling:
                    return "assumeuniformscaling";
                case InstancingOptions.RenderingLayer:
                    return "renderinglayer";
                case InstancingOptions.NoMatrices:
                    return "nomatrices";
                case InstancingOptions.NoLightProbe:
                    return "nolightprobe";
                case InstancingOptions.NoLightmap:
                    return "nolightmap";
                case InstancingOptions.NoLodFade:
                    return "nolodfade";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
