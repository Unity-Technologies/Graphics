using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    struct SphereShape : ICullingShape
    {
        public float radius;

        public float SampleDistance(float3 position)
        {
            // https://www.iquilezles.org/www/articles/distfunctions/distfunctions.htm
            return math.length(position) - radius;
        }
    }
}
