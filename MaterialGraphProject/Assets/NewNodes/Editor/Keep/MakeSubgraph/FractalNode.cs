using System.Reflection;
using UnityEngine;

/*namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "MakeSubgraph", "Fractal")]
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
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.5f, 0, 0, 0)] Vector2 Pan,
            [Slot(2, Binding.None, 3, 0, 0, 0)] Vector1 Zoom,
            [Slot(3, Binding.None, 0.9f, 0, 0, 0)] Vector1 Aspect,
            [Slot(4, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    const int Iterations = 128;
    float2 c = (UV - 0.5) * Zoom * float2(1, Aspect) - Pan;
    float2 v = 0;
    for (int n = 0; n < Iterations && dot(v,v) < 4; n++)
    {
        v = float2(v.x * v.x - v.y * v.y, v.x * v.y * 2) + c;
    }
    Out = (dot(v, v) > 4) ? (float)n / (float)Iterations : 0;
}
";
        }
    }
}*/
