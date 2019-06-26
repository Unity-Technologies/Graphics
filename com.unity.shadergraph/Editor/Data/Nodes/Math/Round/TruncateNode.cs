using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Round", "Truncate")]
    class TruncateNode : CodeFunctionNode
    {
        public TruncateNode()
        {
            name = "Truncate";
        }

        [HlslCodeGen]
        static void Unity_Truncate(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = trunc(In);
        }
    }
}
