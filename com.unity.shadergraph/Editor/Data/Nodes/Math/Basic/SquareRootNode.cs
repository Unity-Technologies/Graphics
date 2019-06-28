using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Basic", "Square Root")]
    class SquareRootNode : CodeFunctionNode
    {
        public SquareRootNode()
        {
            name = "Square Root";
        }

        [HlslCodeGen]
        static void Unity_SquareRoot(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = sqrt(In);
        }
    }
}
