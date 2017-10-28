using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural/Hex")]
    public class HexNode : CodeFunctionNode
    {
        public HexNode()
        {
            name = "Hex";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Hex", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Hex(
            [Slot(0, Binding.MeshUV0)] Vector2 uv,
            [Slot(1, Binding.None)] Vector1 thickness,
            [Slot(2, Binding.None)] out Vector1 result)
        {
            return
                @"
{
    uv.y += fmod(floor(uv.x), 2.0) * 0.5;
    uv = abs(frac(uv) - 0.5);
    result =  step(thickness, abs(max(uv.x * 1.5 + uv.y, uv.y * 2.0) - 1.0));
}
";
        }
    }
}
