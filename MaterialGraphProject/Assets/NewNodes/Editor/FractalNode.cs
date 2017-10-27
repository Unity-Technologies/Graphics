using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural/Fractal")]
    public class FractalNode : CodeFunctionNode
    {
        public FractalNode()
        {
            name = "Fractal";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Fractal", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Fractal(
            [Slot(0, Binding.MeshUV0)] Vector2 uv,
            [Slot(1, Binding.None, 0.5f, 0, 0, 0)] Vector2 pan,
            [Slot(2, Binding.None, 3, 0, 0, 0)] Vector1 zoom,
            [Slot(3, Binding.None, 0.9f, 0, 0, 0)] Vector1 aspect,
            [Slot(4, Binding.None)] out Vector1 result)
        {
            return
                @"
{
    const int Iterations = 128;
    float2 c = (uv - 0.5) * zoom * float2(1, aspect) - pan;
    float2 v = 0;
    for (int n = 0; n < Iterations && dot(v,v) < 4; n++)
    {
        v = float2(v.x * v.x - v.y * v.y, v.x * v.y * 2) + c;
    }
    result = (dot(v, v) > 4) ? (float)n / (float)Iterations : 0;
}
";
        }
    }
}
