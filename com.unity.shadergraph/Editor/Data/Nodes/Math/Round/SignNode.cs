using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Round", "Sign")]
    class SignNode : CodeFunctionNode
    {
        public SignNode()
        {
            name = "Sign";
        }

        [HlslCodeGen]
        static void Unity_Sign(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = sign(In);
        }
    }
}
