using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Degrees To Radians")]
    class DegreesToRadiansNode : CodeFunctionNode
    {
        public DegreesToRadiansNode()
        {
            name = "Degrees To Radians";
        }

        [HlslCodeGen]
        static void Unity_DegreesToRadians(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = radians(In);
        }
    }
}
