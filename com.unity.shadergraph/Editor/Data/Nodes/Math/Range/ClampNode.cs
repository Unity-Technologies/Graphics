using System.Reflection;
using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Range", "Clamp")]
    class ClampNode : CodeFunctionNode
    {
        public ClampNode()
        {
            name = "Clamp";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Clamp", BindingFlags.Static | BindingFlags.NonPublic);
        }

        [HlslCodeGen]
        static void Unity_Clamp(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] Float4 Min,
            [Slot(2, Binding.None, 1, 1, 1, 1)] [AnyDimension] Float4 Max,
            [Slot(3, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = clamp(In, Min, Max);
        }
    }
}
