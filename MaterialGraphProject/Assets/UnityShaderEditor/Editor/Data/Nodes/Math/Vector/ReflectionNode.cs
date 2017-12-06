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
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None)] Vector3 Normal,
            [Slot(2, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.one;

            return @"
{
    Out = reflect(In, Normal);
}";
        }
    }
}
