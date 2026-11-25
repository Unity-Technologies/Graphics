using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    struct ReflectionProbeMinMaxZBinsJob : IJobFor
    {
        public Fixed2<float4x4> worldToViews;

        [ReadOnly]
        public NativeArray<VisibleReflectionProbe> reflectionProbes;
        [ReadOnly]
        public bool reflectionProbeRotation;
        public NativeArray<int2> minMaxZBins;

        public ZBinMapping zBinMapping;

        public float near;

        public void Execute(int index)
        {
            var minMax = math.float2(float.MaxValue, float.MinValue);
            var reflectionProbeIndex = index % reflectionProbes.Length;
            var reflectionProbe = reflectionProbes[reflectionProbeIndex];
            if (!TilingJob.HasNonZeroVolume(reflectionProbe))
            {
                minMaxZBins[index] = zBinMapping.ToBins(minMax);
                return;
            }

            var viewIndex = index / reflectionProbes.Length;
            var worldToView = worldToViews[viewIndex];
            var centerWS = (float3)reflectionProbe.bounds.center;
            var extentsWS = (float3)reflectionProbe.bounds.extents;
            quaternion rotation;
            if (reflectionProbeRotation)
                rotation = (quaternion)reflectionProbe.localToWorldMatrix.rotation;
            else
                rotation = quaternion.identity;

            for (var i = 0; i < 8; i++)
            {
                // Convert index to x, y, and z in [-1, 1]
                var x = ((i << 1) & 2) - 1;
                var y = (i & 2) - 1;
                var z = ((i >> 1) & 2) - 1;
                var localCorner = extentsWS * math.float3(x, y, z);
                var rotatedCorner = math.rotate(rotation, localCorner);
                var cornerVS = math.mul(worldToView, math.float4(rotatedCorner + centerWS, 1));
                cornerVS.z *= -1;
                minMax.x = math.min(minMax.x, cornerVS.z);
                minMax.y = math.max(minMax.y, cornerVS.z);
            }

            // As in LightMinMaxZBinsJob: a probe entirely closer than the near plane keeps max < min.
            minMax.x = math.max(minMax.x, near);
            minMaxZBins[index] = zBinMapping.ToBins(minMax);
        }
    }
}
