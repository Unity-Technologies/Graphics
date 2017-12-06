using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Vector", "Reflection")]
    class ReflectionNode : CodeFunctionNode
    {
        public ReflectionNode()
        {
            name = "Reflection";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Reflection", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Reflection(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] DynamicDimensionVector Normal,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {

            return @"
{
    Out = reflect(In, Normal);
}";
        }
    }
}
