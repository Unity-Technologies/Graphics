using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Math/Vector/Dot Product")]
    public class DotNode : CodeFunctionNode
    {
        public DotNode()
        {
            name = "DotProduct";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_DotProduct", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_DotProduct(
            [Slot(0, Binding.None)] Vector3 first,
            [Slot(1, Binding.None)] Vector3 second,
            [Slot(2, Binding.None)] out Vector1 result)
        {
            return
                @"
{
    result = dot(first, second);
}
";
        }
    }
}
