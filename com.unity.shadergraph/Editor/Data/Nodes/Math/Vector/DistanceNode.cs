using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Vector", "Distance")]
    class DistanceNode : CodeFunctionNode
    {
        public DistanceNode()
        {
            name = "Distance";
        }

        [HlslCodeGen]
        static void Unity_Distance(
            [Slot(0, Binding.None)] [AnyDimension] Float4 A,
            [Slot(1, Binding.None)] [AnyDimension] Float4 B,
            [Slot(2, Binding.None)] out Float Out)
        {
            Out = distance(A, B);
        }
    }
}
