using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Math/Range/Remap")]
    public class RemapNode : CodeFunctionNode
    {
        public RemapNode()
        {
            name = "Remap";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Remap", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Remap(
            [Slot(0, Binding.None)] DynamicDimensionVector input,
            [Slot(1, Binding.None)] Vector2 inMinMax,
            [Slot(2, Binding.None)] Vector2 outMinMax,
            [Slot(3, Binding.None)] out DynamicDimensionVector result)
        {
            return
                @"
{
    result = outMinMax.x + (input - inMinMax.x) * (outMinMax.y - outMinMax.x) / (inMinMax.y - inMinMax.x);
}
";
        }
    }
}
