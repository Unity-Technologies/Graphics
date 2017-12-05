using System.Reflection;
using UnityEngine;

/*namespace UnityEditor.ShaderGraph
{
    [Title("OLD", "AACheckerboard3d")]
    public class AACheckerboard3dNode : CodeFunctionNode
    {
        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_AACheckerboard3d", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_AACheckerboard3d(
            [Slot(0, Binding.MeshUV0)] Vector3 uv,
            [Slot(1, Binding.None, 0.2f, 0.2f, 0.2f, 0.2f)] Vector4 colorA,
            [Slot(2, Binding.None, 0.7f, 0.7f, 0.7f, 0.7f)] Vector4 colorB,
            [Slot(3, Binding.None, 0.5f, 3f, 0f, 0f)] Vector3 aaTweak,
            [Slot(4, Binding.None, 1f, 1f, 1f, 1f)] Vector3 frequency,
            [Slot(5, Binding.None)] out Vector4 result)
        {
            result = Vector2.zero;
            return
                @"
{
    float3 dx = ddx(uv);
    float3 dy = ddy(uv);
    float du=  sqrt(dx.x * dx.x + dy.x * dy.x);
    float dv=  sqrt(dx.y * dx.y + dy.y * dy.y);
    float dw=  sqrt(dx.z * dx.z + dy.z * dy.z);
    float3 distance3 = 2.0f * abs(frac((uv.xyz + 0.5f) * frequency.xyz) - 0.5f) - 0.5f;
    float3 scale = aaTweak.xxx / float3(du, dv, dw);
    float3 blend_out = saturate((scale - aaTweak.zzz) / (aaTweak.yyy - aaTweak.zzz));
    float3 vectorAlpha = clamp(distance3 * scale.xyz * blend_out.xyz, -1.0f, 1.0f);
    float alpha = saturate(0.5f + 0.5f * vectorAlpha.x * vectorAlpha.y * vectorAlpha.z);
    result= lerp(colorA, colorB, alpha.xxxx);
}";
        }
    }
}*/
