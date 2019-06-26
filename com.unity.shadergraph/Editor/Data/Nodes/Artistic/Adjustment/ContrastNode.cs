using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic", "Adjustment", "Contrast")]
    class ContrastNode : CodeFunctionNode
    {
        public ContrastNode()
        {
            name = "Contrast";
        }

        [HlslCodeGen]
        static void Unity_Contrast(
            [Slot(0, Binding.None)] Float3 In,
            [Slot(1, Binding.None, 1, 1, 1, 1)] Float Contrast,
            [Slot(2, Binding.None)] out Float3 Out)
        {
            var midpoint = pow(0.5, 2.2);
            Out = (In - midpoint) * Contrast + midpoint;
        }
    }
}
