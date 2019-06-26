using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Tangent")]
    class TangentNode : CodeFunctionNode
    {
        public TangentNode()
        {
            name = "Tangent";
        }

        [HlslCodeGen]
        static void Unity_Tangent(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = tan(In);
        }
    }
}
