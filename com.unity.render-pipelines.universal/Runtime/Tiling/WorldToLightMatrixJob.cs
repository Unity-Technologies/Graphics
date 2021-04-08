using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    [BurstCompile]
    struct LightExtractionJob : IJobFor
    {
        public float3 viewOrigin;

        [ReadOnly]
        public NativeArray<VisibleLight> lights;

        public NativeArray<float4x4> worldToLightMatrices;

        public NativeArray<float3> viewOriginLs;

        public NativeArray<SphereShape> sphereShapes;

        public NativeArray<ConeShape> coneShapes;

        public void Execute(int index)
        {
            var light = lights[index];
            var localToWorldMatrix = (float4x4)light.localToWorldMatrix;
            var worldToLightMatrix = math.inverse(localToWorldMatrix);
            worldToLightMatrices[index] = worldToLightMatrix;
            viewOriginLs[index] = math.mul(worldToLightMatrix, math.float4(viewOrigin, 1f)).xyz;
            if (light.lightType == LightType.Point)
            {
                sphereShapes[index] = new SphereShape { radius = light.range };
            }
            else if (light.lightType == LightType.Spot)
            {
                coneShapes[index] = new ConeShape(light.spotAngle, light.range, localToWorldMatrix);
            }
        }
    }
}
