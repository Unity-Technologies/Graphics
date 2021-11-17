using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural", "Checkerboard")]
    class CheckerboardNode : CodeFunctionNode
    {
        public CheckerboardNode()
        {
            name = "Checkerboard";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Checkerboard", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Checkerboard(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.2f, 0.2f, 0.2f, 1f)] ColorRGB ColorA,
            [Slot(2, Binding.None, 0.7f, 0.7f, 0.7f, 1f)] ColorRGB ColorB,
            [Slot(3, Binding.None, 1f, 1f, 1f, 1f)] Vector2 Frequency,
            [Slot(4, Binding.None, ShaderStageCapability.Fragment)] out Vector3 Out)
        {
            Out = Vector2.zero;
            return
@"
{
    UV = (UV.xy + 0.5) * Frequency;
    $precision2 distance3 = 4.0 * abs(frac(UV + 0.25) - 0.5) - 1.0;
#if defined(SHADER_STAGE_RAY_TRACING)
    int2 alpha = saturate(distance3 * FLT_MAX);
    Out = lerp(ColorB, ColorA, alpha.x ^ alpha.y);
#else
    $precision4 derivatives = $precision4(ddx(UV), ddy(UV));
    $precision2 duv_length = sqrt($precision2(dot(derivatives.xz, derivatives.xz), dot(derivatives.yw, derivatives.yw)));
    $precision2 scale = 0.35 / duv_length.xy;
    $precision freqLimiter = sqrt(clamp(1.1f - max(duv_length.x, duv_length.y), 0.0, 1.0));
    $precision2 vector_alpha = clamp(distance3 * scale.xy, -1.0, 1.0);
    $precision alpha = saturate(0.5f + 0.5f * vector_alpha.x * vector_alpha.y * freqLimiter);
    Out = lerp(ColorA, ColorB, alpha.xxx);
#endif
}";
        }
    }
}
