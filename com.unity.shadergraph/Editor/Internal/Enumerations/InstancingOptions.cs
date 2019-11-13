using System;

namespace UnityEditor.ShaderGraph.Internal
{
    public enum InstancingOptions
    {
        RenderingLayer,
        NoLightProbe,
        NoLodFade,
    }

    static class InstancingOptionsExtensions
    {
        public static string ToShaderString(this InstancingOptions options)
        {
            switch(options)
            {
                case InstancingOptions.RenderingLayer:
                    return "renderinglayer";
                case InstancingOptions.NoLightProbe:
                    return "nolightprobe";
                case InstancingOptions.NoLodFade:
                    return "nolodfade";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
