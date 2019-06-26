using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Round", "Round")]
    class RoundNode : CodeFunctionNode
    {
        public RoundNode()
        {
            name = "Round";
        }

        [HlslCodeGen]
        static void Unity_Round(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = round(In);
        }
    }
}
