using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Wave", "Noise Sine Wave")]
    class NoiseSineWaveNode : CodeFunctionNode
    {
        public NoiseSineWaveNode()
        {
            name = "Noise Sine Wave";
        }

        [HlslCodeGen]
        static void NoiseSineWave(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None, -0.5f, 0.5f, 1, 1)] Float2 MinMax,
            [Slot(2, Binding.None)] [AnyDimension] out Float4 Out)
        {
            var sinIn = sin(In);
            var sinInOffset = sin(In + 1.0);
            var randomno = frac(sin((sinIn - sinInOffset) * (12.9898 + 78.233)) * 43758.5453);
            var noise = lerp(MinMax.x, MinMax.y, randomno);
            Out = sinIn + noise;
        }
    }
}
