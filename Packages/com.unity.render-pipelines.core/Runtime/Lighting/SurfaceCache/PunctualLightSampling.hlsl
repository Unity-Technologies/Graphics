#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Bindings.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/CommonStructs.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/TraceRayAndQueryHit.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/FetchGeometry.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Sampling/QuasiRandom.hlsl"
#include "PathTracing.hlsl"
#include "PunctualLights.hlsl"

UNIFIED_RT_DECLARE_ACCEL_STRUCT(_RayTracingAccelerationStructure);

StructuredBuffer<PunctualLight> _PunctualLights;
RWStructuredBuffer<PunctualLightSample> _Samples;

StructuredBuffer<MaterialPool::MaterialEntry> _MaterialEntries;
Texture2DArray _AlbedoTextures;
SamplerState sampler_AlbedoTextures;
float _MaterialAtlasTexelSize;
float _AlbedoBoost;
uint _FrameIdx;
uint _PunctualLightCount;

void SamplePunctualLights(UnifiedRT::DispatchInfo dispatchInfo)
{
    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(_RayTracingAccelerationStructure);

    MaterialPoolParamSet matPoolParams;
    matPoolParams.materialEntries = _MaterialEntries;
    matPoolParams.albedoTextures = _AlbedoTextures;
    matPoolParams.albedoSampler = sampler_AlbedoTextures;
    matPoolParams.atlasTexelSize = _MaterialAtlasTexelSize;
    matPoolParams.albedoBoost = _AlbedoBoost;

    QrngKronecker2D rng;
    rng.Init(dispatchInfo.globalThreadIndex.x, _FrameIdx);

    const uint punctualLightIndex = min(rng.GetSample(0).x * _PunctualLightCount, _PunctualLightCount - 1);
    const PunctualLight light = _PunctualLights[punctualLightIndex];

    UnifiedRT::Ray ray;
    ray.tMin = 0;
    ray.tMax = light.range;
    ray.origin = light.position;
    {
        float2 coneSample = rng.GetSample(1);
        float3 localDir = SampleConeUniform((real)coneSample.x, (real)coneSample.y, (real)light.cosOuterAngle);
        float3x3 spotBasis = OrthoBasisFromVector(light.direction);
        ray.direction = mul(spotBasis, localDir);
    }

    PunctualLightSample lightSample = (PunctualLightSample)0;
    lightSample.rayDirection = ray.direction;

    UnifiedRT::Hit hitResult = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, 0xFFFFFFFF, ray, UnifiedRT::kRayFlagNone);
    if (hitResult.IsValid() && hitResult.isFrontFace)
    {
        const UnifiedRT::InstanceData hitInstance = UnifiedRT::GetInstance(hitResult.instanceID);
        const SurfaceGeometry hitGeo = FetchSurfaceGeometry(hitInstance, hitResult);
        const MaterialPool::MaterialEntry matEntry = matPoolParams.materialEntries[hitInstance.userMaterialID];
        const float3 hitAlbedo = MaterialPool::LoadAlbedoWithBoost(matEntry, matPoolParams.albedoTextures, matPoolParams.albedoSampler, matPoolParams.atlasTexelSize, matPoolParams.albedoBoost, hitGeo.uv0, hitGeo.uv1);

        lightSample.hitPos = ray.origin + ray.direction * hitResult.hitDistance;
        lightSample.hitNormal = hitGeo.normal;
        lightSample.distance = hitResult.hitDistance;
        lightSample.hitAlbedo = hitAlbedo;
        lightSample.reciprocalDensity = AreaOfSphericalCapWithRadiusOne(light.cosOuterAngle) * _PunctualLightCount;
        lightSample.hitInstanceId = hitResult.instanceID;
        lightSample.hitPrimitiveIndex = hitResult.primitiveIndex;
        lightSample.lightIndex = punctualLightIndex;
    }
    else
    {
        lightSample.MarkNoHit();
    }

    _Samples[dispatchInfo.globalThreadIndex.x] = lightSample;
}
