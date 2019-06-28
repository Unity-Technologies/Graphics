using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Wave", "Sawtooth Wave")]
    class SawtoothWaveNode : CodeFunctionNode
    {
        public SawtoothWaveNode()
        {
            name = "Sawtooth Wave";
        }

        [HlslCodeGen]
        static void SawtoothWave(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = 2 * (In - floor(0.5 + In));
        }
    }
}
