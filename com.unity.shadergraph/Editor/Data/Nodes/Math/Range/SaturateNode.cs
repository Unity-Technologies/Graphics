using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Range", "Saturate")]
    class SaturateNode : CodeFunctionNode
    {
        public SaturateNode()
        {
            name = "Saturate";
        }

        [HlslCodeGen]
        static void Unity_Saturate(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = saturate(In);
        }
    }
}
