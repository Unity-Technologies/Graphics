using System;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal enum InstancingOptions
    {
        ProceduralFunction,
        RenderingLayer,
        NoLightProbe,
        NoLodFade,
    }

    [GenerationAPI]
    internal static class InstancingOptionsExtensions
    {
        public static string ToShaderString(this InstancingOptions options)
        {
            switch (options)
            {
                case InstancingOptions.ProceduralFunction:
                    return "procedural:SetupProcedural";
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
