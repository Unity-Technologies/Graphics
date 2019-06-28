using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic", "Adjustment", "Saturation")]
    class SaturationNode : CodeFunctionNode
    {
        public SaturationNode()
        {
            name = "Saturation";
        }

        [HlslCodeGen]
        static void Unity_Saturation(
            [Slot(0, Binding.None)] Float3 In,
            [Slot(1, Binding.None, 1, 1, 1, 1)] Float Saturation,
            [Slot(2, Binding.None)] out Float3 Out)
        {
            var luma = dot(In, Float3(0.2126729, 0.7151522, 0.0721750));
            Out = luma.xxx + Saturation.xxx * (In - luma.xxx);
        }
    }
}
