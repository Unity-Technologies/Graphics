using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Interpolation", "Smoothstep")]
    class SmoothstepNode : CodeFunctionNode
    {
        public SmoothstepNode()
        {
            name = "Smoothstep";
        }

        [HlslCodeGen]
        static void Unity_Smoothstep(
            [Slot(0, Binding.None, 0, 0, 0, 0)] [AnyDimension] Float4 Edge1,
            [Slot(1, Binding.None, 1, 1, 1, 1)] [AnyDimension] Float4 Edge2,
            [Slot(2, Binding.None, 0, 0, 0, 0)] [AnyDimension] Float4 In,
            [Slot(3, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = smoothstep(Edge1, Edge2, In);
        }
    }
}
