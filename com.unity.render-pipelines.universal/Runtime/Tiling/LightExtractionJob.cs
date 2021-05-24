using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    [BurstCompile]
    struct LightExtractionJob : IJobFor
    {
        [ReadOnly]
        public NativeArray<VisibleLight> lights;

        public NativeArray<LightType> lightTypes;

        public NativeArray<float> radiuses;

        public NativeArray<float3> directions;

        public NativeArray<float3> positions;

        public NativeArray<float> coneRadiuses;

        public void Execute(int index)
        {
            var light = lights[index];
            var localToWorldMatrix = (float4x4)light.localToWorldMatrix;
            lightTypes[index] = light.lightType;
            radiuses[index] = light.range;
            directions[index] = localToWorldMatrix.c2.xyz;
            positions[index] = localToWorldMatrix.c3.xyz;
            coneRadiuses[index] = math.tan(math.radians(light.spotAngle * 0.5f)) * light.range;
        }
    }
}
