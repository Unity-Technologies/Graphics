using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Procedural/Checkerboard")]
    public class AACheckerboardNode : CodeFunctionNode
    {
        public AACheckerboardNode()
        {
            name = "Checkerboard";
        }
        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_AACheckerboard", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_AACheckerboard(
            [Slot(0, Binding.MeshUV0)] Vector2 UV,
            [Slot(1, Binding.None, 0.2f, 0.2f, 0.2f, 0.2f)] Vector4 ColorA,
            [Slot(2, Binding.None, 0.7f, 0.7f, 0.7f, 0.7f)] Vector4 ColorB,
            [Slot(3, Binding.None, 1f, 1f, 1f, 1f)] Vector2 Frequency,
            [Slot(4, Binding.None)] out Vector4 Out)
        {
            Out = Vector2.zero;
            return
                @"
{
    float4 derivatives = float4(ddx(UV), ddy(UV));
    float2 duv_length = sqrt(float2(dot(derivatives.xz, derivatives.xz), dot(derivatives.yw, derivatives.yw)));
    float width = 0.5f;
    float2 distance3 = 2.0f * abs(frac(UV.xy * Frequency) - 0.5f) - width;
    float2 scale = 0.5 / duv_length.xy;
    float2 blend_out = saturate(scale / 3);
    float2 vector_alpha = clamp(distance3 * scale.xy * blend_out.xy, -1.0f, 1.0f);
    float alpha = saturate(0.5f + 0.5f * vector_alpha.x * vector_alpha.y);
    Out= lerp(ColorA, ColorB, alpha.xxxx);
}";
        }
    }
}
