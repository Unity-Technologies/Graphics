using Unity.Mathematics;

namespace UnityEngine.ShaderGraph
{
    public static class MathUtils
    {
        public static float normalize(float v) => v == 0 ? float.PositiveInfinity : 1;
        public static float2 normalize(float2 v) => math.normalize(v);
        public static float3 normalize(float3 v) => math.normalize(v);
        public static float4 normalize(float4 v) => math.normalize(v);

        public static float reflect(float i, float n) => i - 2f * n * (i * n);
        public static float2 reflect(float2 i, float2 n) => math.reflect(i, n);
        public static float3 reflect(float3 i, float3 n) => math.reflect(i, n);
        public static float4 reflect(float4 i, float4 n) => math.reflect(i, n);

        public static float refract(float i, float n, float eta)
        {
            float ni = n * i;
            float k = 1.0f - eta * eta * (1.0f - ni * ni);
            return math.select(0.0f, eta * i - (eta * ni + math.sqrt(k)) * n, k >= 0);
        }

        public static float2 refract(float2 i, float2 n, float eta) => math.refract(i, n, eta);
        public static float3 refract(float3 i, float3 n, float eta) => math.refract(i, n, eta);
        public static float4 refract(float4 i, float4 n, float eta) => math.refract(i, n, eta);
    }
}
