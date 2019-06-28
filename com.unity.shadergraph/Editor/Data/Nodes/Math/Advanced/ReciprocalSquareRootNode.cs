using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Advanced", "Reciprocal Square Root")]
    class ReciprocalSquareRootNode : CodeFunctionNode
    {
        public ReciprocalSquareRootNode()
        {
            name = "Reciprocal Square Root";
        }

        [HlslCodeGen]
        static void Unity_Rsqrt(
            [Slot(0, Binding.None, 1, 1, 1, 1)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = rsqrt(In);
        }
    }
}
