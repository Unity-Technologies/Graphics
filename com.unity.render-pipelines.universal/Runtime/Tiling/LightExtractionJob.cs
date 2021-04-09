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
            data.viewOriginL = math.mul(worldToLightMatrix, math.float4(viewOrigin, 1f)).xyz;
            data.screenRect = light.screenRect;
            data.lightType = light.lightType;
            if (light.lightType == LightType.Point)
            {
                data.shape.sphere = new SphereShape { radius = light.range };
            }
            else if (light.lightType == LightType.Spot)
            {
                data.shape.cone = new ConeShape(light.spotAngle, light.range);
            }
            tilingLights[index] = data;
        }
    }
}
