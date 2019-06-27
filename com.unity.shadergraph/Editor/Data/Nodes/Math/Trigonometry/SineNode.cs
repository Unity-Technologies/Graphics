using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Sine")]
    class SineNode : CodeFunctionNode
    {
        public SineNode()
        {
            name = "Sine";
        }

        [HlslCodeGen]
        static void Unity_Sine(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = sin(In);
        }
    }
}
