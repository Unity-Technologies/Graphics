using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Derivative", "DDXY")]
    class DDXYNode : CodeFunctionNode
    {
        public DDXYNode()
        {
            name = "DDXY";
        }

        [HlslCodeGen]
        static void Unity_DDXY(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = abs(ddx(In)) + abs(ddy(In));
        }
    }
}
