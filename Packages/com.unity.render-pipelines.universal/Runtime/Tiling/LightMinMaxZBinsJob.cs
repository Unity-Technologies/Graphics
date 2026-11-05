using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    struct LightMinMaxZBinsJob : IJobFor
    {
        public Fixed2<float4x4> worldToViews;

        [ReadOnly]
        public NativeArray<VisibleLight> lights;
        public NativeArray<int2> minMaxZBins;

        public float near;

        public ZBinMapping zBinMapping;

        public void Execute(int index)
        {
            var lightIndex = index % lights.Length;
            var light = lights[lightIndex];
            var lightToWorld = (float4x4)light.localToWorldMatrix;
            var originWS = lightToWorld.c3.xyz;
            var viewIndex = index / lights.Length;
            var worldToView = worldToViews[viewIndex];
            var originVS = math.mul(worldToView, math.float4(originWS, 1)).xyz;
            originVS.z *= -1;

            var minMax = math.float2(originVS.z - light.range, originVS.z + light.range);

            if (!TilingJob.HasNonZeroVolume(light))
            {
                minMax.x = float.MaxValue;
                minMax.y = float.MinValue;
            }
            else if (light.lightType == LightType.Spot)
            {
                // Based on https://iquilezles.org/www/articles/diskbbox/diskbbox.htm
                var angleA = math.radians(light.spotAngle) * 0.5f;
                math.sincos(angleA, out var sinAngleA, out var cosAngleA);
                float coneHeight = light.range * cosAngleA;
                float3 spotDirectionWS = lightToWorld.c2.xyz;
                var endPointWS = originWS + spotDirectionWS * coneHeight;
                var endPointVS = math.mul(worldToView, math.float4(endPointWS, 1)).xyz;
                endPointVS.z *= -1;
                var coneRadius = light.range * sinAngleA;
                var a = endPointVS - originVS;
                // Saturate: rounding can push the argument slightly below 0, where sqrt returns NaN.
                var e = math.sqrt(math.saturate(1.0f - a.z * a.z / math.dot(a, a)));

                // `-a.z` and `a.z` is `dot(a, {0, 0, -1}).z` and `dot(a, {0, 0, 1}).z` optimized
                // `cosAngleA` is multiplied by `coneHeight` to avoid normalizing `a`, which we know has length `coneHeight`
                if (-a.z < coneHeight * cosAngleA) minMax.x = math.min(originVS.z, endPointVS.z - e * coneRadius);
                if (a.z < coneHeight * cosAngleA) minMax.y = math.max(originVS.z, endPointVS.z + e * coneRadius);
            }

            // Clamp to near, not 0: an orthographic near plane can be negative. A light entirely closer than the near
            // plane keeps max < min, which ZBinMapping.ToBins turns into an empty bin span.
            minMax.x = math.max(minMax.x, near);
            minMaxZBins[index] = zBinMapping.ToBins(minMax);
        }
    }
}
