using System.Reflection;
using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Hyperbolic Tangent")]
    class HyperbolicTangentNode : CodeFunctionNode
    {
        public HyperbolicTangentNode()
        {
            name = "Hyperbolic Tangent";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_HyperbolicTangent", BindingFlags.Static | BindingFlags.NonPublic);
        }

        [HlslCodeGen]
        static void Unity_HyperbolicTangent(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = tanh(In);
        }
    }
}
