using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input/Gradient/Gradient 2D")]
    public class Gradient2DNode : CodeFunctionNode
    {
        public Gradient2DNode()
        {
            name = "Gradient 2D";
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Gradient2D", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Gradient2D(
            [Slot(0, Binding.None)] Gradient g,
            [Slot(1, Binding.None)] Vector1 Time,
            [Slot(2, Binding.None)] out Vector4 Out)
        {
            Out = Vector4.zero;
            return
                @"
{
    int colorKey1 = 0;
    int colorKey2 = g.colorsLength-1;

    for(int i = 0; i < g.colorsLength; i++)
    {
        if(g.colors[i].w <= Time)
            colorKey1 = i;
        else
            break;
    }
    for(int i = g.colorsLength-1; i >= 0; i--)
    {
        if(g.colors[i].w >= Time)
            colorKey2 = i;
        else
            break;
    }

    int alphaKey1 = 0;
    int alphaKey2 = g.alphasLength-1;

    for(int i = 0; i < g.alphasLength; i++)
    {
        if(g.alphas[i].y <= Time)
            alphaKey1 = i;
        else
            break;
    }
    for(int i = g.alphasLength-1; i >= 0; i--)
    {
        if(g.alphas[i].y >= Time)
            alphaKey2 = i;
        else
            break;
    }

    float colorPos = (Time - g.colors[colorKey1].w) / (g.colors[colorKey2].w - g.colors[colorKey1].w);
    float3 color = lerp(g.colors[colorKey1].rgb, g.colors[colorKey2].rgb, colorPos);
    float alphaPos = (Time - g.alphas[alphaKey1].y) / (g.alphas[alphaKey2].y - g.alphas[alphaKey1].y);
    float alpha = lerp(g.alphas[alphaKey1].r, g.alphas[alphaKey2].r, alphaPos);
    Out = float4(color, alpha);
}
";
        }
    }
}
