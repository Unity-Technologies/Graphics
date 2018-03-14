using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Gradient", "Sample Gradient")]
    public class SampleGradient : CodeFunctionNode, IGeneratesBodyCode
    {
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
            return GetType().GetMethod("Unity_SampleGradient", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_SampleGradient(
            [Slot(0, Binding.None)] Gradient Gradient,
            [Slot(1, Binding.None)] Vector1 Time,
            [Slot(2, Binding.None)] out Vector4 Out)
        {
            Out = Vector4.zero;
            return
                @"
{
    int colorKey1 = 0;
    int colorKey2 = Gradient.colorsLength-1;
    for(int c1 = 0; c1 < Gradient.colorsLength; c1++)
    {
        if(Gradient.colors[c1].w <= Time)
            colorKey1 = c1;
        else
            break;
    }
    for(int c2 = Gradient.colorsLength-1; c2 >= 0; c2--)
    {
        if(Gradient.colors[c2].w >= Time)
            colorKey2 = c2;
        else
            break;
    }
    int alphaKey1 = 0;
    int alphaKey2 = Gradient.alphasLength-1;
    for(int a1 = 0; a1 < Gradient.alphasLength; a1++)
    {
        if(Gradient.alphas[a1].y <= Time)
            alphaKey1 = a1;
        else
            break;
    }
    for(int a2 = Gradient.alphasLength-1; a2 >= 0; a2--)
    {
        if(Gradient.alphas[a2].y >= Time)
            alphaKey2 = a2;
        else
            break;
    }
    float colorPos = min(1, max(0, (Time - Gradient.colors[colorKey1].w) / (Gradient.colors[colorKey2].w - Gradient.colors[colorKey1].w)));
    float3 color = lerp(Gradient.colors[colorKey1].rgb, Gradient.colors[colorKey2].rgb, colorPos);
    float alphaPos = min(1, max(0, (Time - Gradient.alphas[alphaKey1].y) / (Gradient.alphas[alphaKey2].y - Gradient.alphas[alphaKey1].y)));
    float alpha = lerp(Gradient.alphas[alphaKey1].r, Gradient.alphas[alphaKey2].r, alphaPos);
    Out = float4(color, alpha);
}
";
        }
    }
}