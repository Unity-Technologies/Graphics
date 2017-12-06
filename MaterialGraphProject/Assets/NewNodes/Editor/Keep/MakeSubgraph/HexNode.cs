using System.Reflection;
using UnityEngine;

/*namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "MakeSubgraph", "Hexagon")]
    public class HexNode : CodeFunctionNode
    {
        public HexNode()
        {
            name = "Hexagon";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Hex", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Hex(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.5f, 0.5f, 0.5f, 0.5f)] Vector1 Scale,
            [Slot(2, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    UV.y += fmod(floor(UV.x), 2.0) * 0.5;
    UV = abs(frac(UV) - 0.5);
    Out =  step(1-Scale,abs(max(UV.x * 1.5 + UV.y, UV.y * 2.0) - 1.0));
}
";
        }
    }
}*/
