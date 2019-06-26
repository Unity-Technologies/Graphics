using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Vector", "Dot Product")]
    class DotProductNode : CodeFunctionNode
    {
        public DotProductNode()
        {
            name = "Dot Product";
        }

        [HlslCodeGen]
        static void Unity_DotProduct(
            [Slot(0, Binding.None, 0, 0, 0, 0)] [AnyDimension] Float4 A,
            [Slot(1, Binding.None, 0, 1, 0, 0)] [AnyDimension] Float4 B,
            [Slot(2, Binding.None)] out Float Out)
        {
            Out = dot(A, B);
        }
    }
}
