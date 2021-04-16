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

        public NativeArray<TilingLightData> tilingLights;

        public void Execute(int index)
        {
            var light = lights[index];
            var data = new TilingLightData();
            var localToWorldMatrix = (float4x4)light.localToWorldMatrix;
            var worldToLightMatrix = math.inverse(localToWorldMatrix);
            data.worldToLightMatrix = worldToLightMatrix;
            float4 p = math.mul(worldToLightMatrix, math.float4(viewOrigin, 1f));
            data.viewOriginL = p.xyz / p.w;
            data.screenRect = light.screenRect;
            data.lightType = light.lightType;
            // if (light.lightType == LightType.Point)
            // {
            //     data.shape.sphere = new SphereShape { radius = light.range };
            // }
            // else if (light.lightType == LightType.Spot)
            // {
            //     data.shape.cone = new ConeShape(light.spotAngle, light.range);
            // }
            data.radius = light.range;
            data.coneAngle = math.radians(light.spotAngle * 0.5f);
            data.directionW = localToWorldMatrix.c2.xyz;
            data.originW = localToWorldMatrix.c3.xyz;
            var angleB = math.PI * 0.5f - data.coneAngle;
            data.coneHeight = math.sin(angleB) * light.range;
            data.S = math.tan(data.coneAngle);
            data.C = (float)(1.0 / (1.0 + data.S));
            tilingLights[index] = data;
        }
    }
}
