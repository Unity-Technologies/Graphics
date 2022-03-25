using UnityEngine;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    // These functions were taken from hlsl code
    internal static class ColorExtensions
    {
        public static Color UnpackNormalRGB(this Color packedColor, float scale = 1.0f)
        {
            Color normal;
            normal.r = packedColor.r * 2.0f - 1.0f;
            normal.g = packedColor.g * 2.0f - 1.0f;
            normal.b = packedColor.b * 2.0f - 1.0f;
            normal.r *= scale;
            normal.b *= scale;
            normal.a = packedColor.a;
            return normal;
        }

        public static Color UnpackNormalAG(this Color packedNormal, float scale = 1.0f)
        {
            Color normal;
            normal.r = packedNormal.a * 2.0f - 1.0f;
            normal.g = packedNormal.g * 2.0f - 1.0f;
            normal.b = Mathf.Max(1.0e-16f, Mathf.Sqrt(1.0f - Mathf.Clamp01(Vector2.SqrMagnitude(new Vector2(normal.r, normal.g)))));
            normal.r *= scale;
            normal.g *= scale;
            normal.a = packedNormal.a;
            return normal;
        }

        public static Color UnpackNormalmapRGorAG(this Color packedNormal, float scale = 1.0f)
        {
            float alpha = packedNormal.a;
            packedNormal.a *= packedNormal.r;
            Color result = UnpackNormalAG(packedNormal, scale);
            result.a = alpha;
            return result;
        }
    }
}
