using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Vector", "Projection")]
    class ProjectionNode : CodeFunctionNode
    {
        public ProjectionNode()
        {
            name = "Projection";
        }

        [HlslCodeGen]
        static void Unity_Projection(
            [Slot(0, Binding.None, 0, 0, 0, 0)] [AnyDimension] Float4 A,
            [Slot(1, Binding.None, 0, 1, 0, 0)] [AnyDimension] Float4 B,
            [Slot(2, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = B * dot(A, B) / dot(B, B);
        }
    }
}
