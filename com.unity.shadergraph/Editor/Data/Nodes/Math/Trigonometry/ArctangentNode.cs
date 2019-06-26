using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Arctangent")]
    class ArctangentNode : CodeFunctionNode
    {
        public ArctangentNode()
        {
            name = "Arctangent";
        }

        [HlslCodeGen]
        static void Unity_Arctangent(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = atan(In);
        }
    }
}
