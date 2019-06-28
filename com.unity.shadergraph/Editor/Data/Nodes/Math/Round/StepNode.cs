using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Round", "Step")]
    class StepNode : CodeFunctionNode
    {
        public StepNode()
        {
            name = "Step";
        }

        [HlslCodeGen]
        static void Unity_Step(
            [Slot(0, Binding.None, 1, 1, 1, 1)] [AnyDimension] Float4 Edge,
            [Slot(1, Binding.None, 0, 0, 0, 0)] [AnyDimension] Float4 In,
            [Slot(2, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = step(Edge, In);
        }
    }
}
