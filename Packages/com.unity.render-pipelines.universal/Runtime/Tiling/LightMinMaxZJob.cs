using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    [BurstCompile]
    struct LightMinMaxZJob : IJobFor
    {
        public Fixed2<float4x4> worldToViews;

        [ReadOnly]
        public NativeArray<VisibleLight> lights;
        public NativeArray<float2> minMaxZs;

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

            if (light.lightType == LightType.Spot)
            {
                // Based on https://iquilezles.org/www/articles/diskbbox/diskbbox.htm
                var angleA = math.radians(light.spotAngle) * 0.5f;
                float cosAngleA = math.cos(angleA);
                float coneHeight = light.range * cosAngleA;
                float3 spotDirectionWS = lightToWorld.c2.xyz;
                var endPointWS = originWS + spotDirectionWS * coneHeight;
                var endPointVS = math.mul(worldToView, math.float4(endPointWS, 1)).xyz;
                endPointVS.z *= -1;
                var angleB = math.PI * 0.5f - angleA;
                var coneRadius = light.range * cosAngleA * math.sin(angleA) / math.sin(angleB);
                var a = endPointVS - originVS;
                var e = math.sqrt(1.0f - a.z * a.z / math.dot(a, a));

                // `-a.z` and `a.z` is `dot(a, {0, 0, -1}).z` and `dot(a, {0, 0, 1}).z` optimized
                // `cosAngleA` is multiplied by `coneHeight` to avoid normalizing `a`, which we know has length `coneHeight`
                if (-a.z < coneHeight * cosAngleA) minMax.x = math.min(originVS.z, endPointVS.z - e * coneRadius);
                if (a.z < coneHeight * cosAngleA) minMax.y = math.max(originVS.z, endPointVS.z + e * coneRadius);
            }
            else if (light.lightType != LightType.Point)
            {
                // Currently we support only spot, point and directional lights in URP. This job only deals with the
                // first two. If any other light type is found, skip that light.
                minMax.x = float.MaxValue;
                minMax.y = float.MinValue;
            }

            minMax.x = math.max(minMax.x, 0);
            minMax.y = math.max(minMax.y, 0);
            minMaxZs[index] = minMax;
        }
    }
}
