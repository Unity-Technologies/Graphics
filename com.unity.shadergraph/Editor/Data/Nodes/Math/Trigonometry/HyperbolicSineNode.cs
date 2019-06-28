using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Hyperbolic Sine")]
    class HyperbolicSineNode : CodeFunctionNode
    {
        public HyperbolicSineNode()
        {
            name = "Hyperbolic Sine";
        }

        [HlslCodeGen]
        static void Unity_HyperbolicSine(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = sinh(In);
        }
    }
}
