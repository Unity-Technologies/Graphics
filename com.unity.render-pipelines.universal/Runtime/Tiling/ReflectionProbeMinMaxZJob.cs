using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    [BurstCompile]
    struct ReflectionProbeMinMaxZJob : IJobFor
    {
        public float4x4 worldToViewMatrix;

        [ReadOnly]
        public NativeArray<VisibleReflectionProbe> reflectionProbes;
        public NativeArray<float2> minMaxZs;

        public void Execute(int index)
        {
            var minMax = math.float2(float.MaxValue, float.MinValue);
            var reflectionProbe = reflectionProbes[index];
            var localToWorld = (float4x4)reflectionProbe.localToWorldMatrix;
            var centerWS = (float3)reflectionProbe.bounds.center;
            var extentsWS = (float3)reflectionProbe.bounds.extents;
            for (var x = -1; x <= 1; x += 2)
            for (var y = -1; y <= 1; y += 2)
            for (var z = -1; z <= 1; z += 2)
            {
                var cornerVS = math.mul(worldToViewMatrix, math.float4(centerWS + extentsWS * math.float3(x, y, z), 1));
                cornerVS.z *= -1;
                minMax.x = math.min(minMax.x, cornerVS.z);
                minMax.y = math.max(minMax.y, cornerVS.z);
            }

            minMaxZs[index] = minMax;
        }
    }
}
