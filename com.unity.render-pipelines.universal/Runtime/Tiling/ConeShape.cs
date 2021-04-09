using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    struct ConeShape : ICullingShape
    {
        float2 m_SinCos;
        float m_Height;

        public ConeShape(float spotAngle, float height)
        {
            var halfAngleRadians = math.radians(spotAngle * 0.5f);
            m_SinCos = math.float2(math.sin(halfAngleRadians), math.cos(halfAngleRadians));
            m_Height = height;
        }

        public float SampleDistance(float3 position)
        {
            // https://www.iquilezles.org/www/articles/distfunctions/distfunctions.htm
            float2 q = math.float2(math.length(position.xy), position.z);
            float l = math.length(q) - m_Height;
            float m = math.length(q - m_SinCos * math.clamp(math.dot(q, m_SinCos), 0f, m_Height));
            return math.max(l, m * math.sign(m_SinCos.y * q.x - m_SinCos.x * q.y));
        }
    }
}
