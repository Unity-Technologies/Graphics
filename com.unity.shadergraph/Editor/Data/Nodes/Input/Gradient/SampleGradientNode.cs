using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Gradient", "Sample Gradient")]
    class SampleGradient : CodeFunctionNode
    {
        public override int latestVersion => 1;
        public SampleGradient()
        {
            name = "Sample Gradient";
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            switch (sgVersion)
            {
                case 0:
                    return GetType().GetMethod("Unity_SampleGradientV0", BindingFlags.Static | BindingFlags.NonPublic);
                case 1:
                default:
                    return GetType().GetMethod("Unity_SampleGradientV1", BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        static string Unity_SampleGradientV0(
            [Slot(0, Binding.None)] Gradient Gradient,
            [Slot(1, Binding.None)] Vector1 Time,
            [Slot(2, Binding.None)] out Vector4 Out)
        {
            Out = Vector4.zero;
            return
@"
{
    $precision3 color = Gradient.colors[0].rgb;
    [unroll]
    for (int c = 1; c < Gradient.colorsLength; c++)
    {
        $precision colorPos = saturate((Time - Gradient.colors[c - 1].w) / (Gradient.colors[c].w - Gradient.colors[c - 1].w)) * step(c, Gradient.colorsLength - 1);
        color = lerp(color, Gradient.colors[c].rgb, lerp(colorPos, step(0.01, colorPos), Gradient.type));
    }
#ifndef UNITY_COLORSPACE_GAMMA
    color = SRGBToLinear(color);
#endif
    $precision alpha = Gradient.alphas[0].x;
    [unroll]
    for (int a = 1; a < Gradient.alphasLength; a++)
    {
        $precision alphaPos = saturate((Time - Gradient.alphas[a - 1].y) / (Gradient.alphas[a].y - Gradient.alphas[a - 1].y)) * step(a, Gradient.alphasLength - 1);
        alpha = lerp(alpha, Gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), Gradient.type));
    }
    Out = $precision4(color, alpha);
}
";
        }

        static string Unity_SampleGradientV1(
            [Slot(0, Binding.None)] Gradient Gradient,
            [Slot(1, Binding.None)] Vector1 Time,
            [Slot(2, Binding.None)] out Vector4 Out)
        {
            Out = Vector4.zero;
            return
@"
{
    $precision3 color = Gradient.colors[0].rgb;
    [unroll]
    for (int c = 1; c < Gradient.colorsLength; c++)
    {
        $precision colorPos = saturate((Time - Gradient.colors[c - 1].w) / (Gradient.colors[c].w - Gradient.colors[c - 1].w)) * step(c, Gradient.colorsLength - 1);
        color = lerp(color, Gradient.colors[c].rgb, lerp(colorPos, step(0.01, colorPos), Gradient.type));
    }
#ifdef UNITY_COLORSPACE_GAMMA
    color = LinearToSRGB(color);
#endif
    $precision alpha = Gradient.alphas[0].x;
    [unroll]
    for (int a = 1; a < Gradient.alphasLength; a++)
    {
        $precision alphaPos = saturate((Time - Gradient.alphas[a - 1].y) / (Gradient.alphas[a].y - Gradient.alphas[a - 1].y)) * step(a, Gradient.alphasLength - 1);
        alpha = lerp(alpha, Gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), Gradient.type));
    }
    Out = $precision4(color, alpha);
}
";
        }
    }
}
