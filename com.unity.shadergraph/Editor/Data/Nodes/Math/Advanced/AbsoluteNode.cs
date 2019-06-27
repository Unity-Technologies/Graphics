using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Advanced", "Absolute")]
    class AbsoluteNode : CodeFunctionNode
    {
        public AbsoluteNode()
        {
            name = "Absolute";
        }

        [HlslCodeGen]
        static void Unity_Absolute(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = abs(In);
        }
    }
}
