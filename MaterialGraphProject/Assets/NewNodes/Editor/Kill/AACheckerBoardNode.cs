using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Procedural/AACheckerboard")]
    public class AACheckerboardNode : CodeFunctionNode
    {
        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_AACheckerboard", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_AACheckerboard(
            [Slot(0, Binding.MeshUV0)] Vector2 uv,
            [Slot(1, Binding.None, 0.2f, 0.2f, 0.2f, 0.2f)] Vector4 colorA,
            [Slot(2, Binding.None, 0.7f, 0.7f, 0.7f, 0.7f)] Vector4 colorB,
            [Slot(3, Binding.None, 0.5f, 3f, 0f, 0f)] Vector3 aaTweak,
            [Slot(4, Binding.None, 1f, 1f, 1f, 1f)] Vector2 frequency,
            [Slot(5, Binding.None)] out Vector4 result)
        {
            result = Vector2.zero;
            return
                @"
{
    float4 derivatives = float4(ddx(uv), ddy(uv));
    float2 duv_length = sqrt(float2(dot(derivatives.xz, derivatives.xz), dot(derivatives.yw, derivatives.yw)));
    float width = 0.5f;
    float2 distance3 = 2.0f * abs(frac(uv.xy * frequency) - 0.5f) - width;
    float2 scale = aaTweak.x / duv_length.xy;
    float2 blend_out = saturate((scale - aaTweak.zz) / (aaTweak.yy - aaTweak.zz));
    float2 vector_alpha = clamp(distance3 * scale.xy * blend_out.xy, -1.0f, 1.0f);
    float alpha = saturate(0.5f + 0.5f * vector_alpha.x * vector_alpha.y);
    result= lerp(colorA, colorB, alpha.xxxx);
}";
        }
    }
}
