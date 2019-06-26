using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Derivative", "DDY")]
    class DDYNode : CodeFunctionNode
    {
        public DDYNode()
        {
            name = "DDY";
        }

        [HlslCodeGen]
        static void Unity_DDY(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = ddy(In);
        }
    }
}
