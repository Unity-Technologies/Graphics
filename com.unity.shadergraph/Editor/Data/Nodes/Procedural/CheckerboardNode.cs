using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Checkerboard")]
    class CheckerboardNode : CodeFunctionNode
    {
        public CheckerboardNode()
        {
            name = "Checkerboard";
        }

        [HlslCodeGen]
        static void Unity_Checkerboard(
            [Slot(0, Binding.MeshUV0)] Float2 UV,
            [Slot(1, Binding.None, 0.2f, 0.2f, 0.2f, 1f)] [AnyDimension] Float4 ColorA,
            [Slot(2, Binding.None, 0.7f, 0.7f, 0.7f, 1f)] [AnyDimension] Float4 ColorB,
            [Slot(3, Binding.None, 1f, 1f, 1f, 1f)] Float2 Frequency,
            [Slot(4, Binding.None, ShaderStageCapability.Fragment)] [AnyDimension] out Float4 Out)
        {
            UV = (UV.xy + 0.5) * Frequency;
            var derivatives = Float4(ddx(UV), ddy(UV));
            var duv_length = sqrt(Float2(dot(derivatives.xz, derivatives.xz), dot(derivatives.yw, derivatives.yw)));
            var width = 1.0;
            var distance3 = 4.0 * abs(frac(UV + 0.25) - 0.5) - width;
            var scale = 0.35 / duv_length.xy;
            var freqLimiter = sqrt(clamp(1.1f - max(duv_length.x, duv_length.y), 0.0, 1.0));
            var vector_alpha = clamp(distance3 * scale.xy, -1.0, 1.0);
            var alpha = saturate(0.5f + 0.5f * vector_alpha.x * vector_alpha.y * freqLimiter);
            Out = lerp(ColorA, ColorB, alpha.xxxx);
        }
    }
}
