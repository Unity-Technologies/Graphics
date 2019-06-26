using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Trigonometry", "Hyperbolic Cosine")]
    class HyperbolicCosineNode : CodeFunctionNode
    {
        public HyperbolicCosineNode()
        {
            name = "Hyperbolic Cosine";
        }

        [HlslCodeGen]
        static void Unity_HyperbolicCosine(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = sinh(In);
        }
    }
}
