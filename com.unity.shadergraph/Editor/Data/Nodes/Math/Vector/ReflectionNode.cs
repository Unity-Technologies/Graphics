using System.Reflection;
using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

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

        [HlslCodeGen]
        static void Unity_Reflection(
            [Slot(0, Binding.None, 0, 0, 0, 0)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None, 0, 1, 0, 0)] [AnyDimension] Float4 Normal,
            [Slot(2, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = reflect(In, Normal);
        }
    }
}
