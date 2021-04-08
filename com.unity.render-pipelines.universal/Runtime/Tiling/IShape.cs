using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    interface ICullingShape
    {
        float SampleDistance(float3 position);
    }
}
