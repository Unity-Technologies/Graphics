#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
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

StructuredBuffer<SpotLight> _SpotLights;
RWStructuredBuffer<PunctualLightSample> _Samples;

StructuredBuffer<MaterialPool::MaterialEntry> _MaterialEntries;
Texture2DArray _AlbedoTextures;
Texture2DArray _TransmissionTextures;
Texture2DArray _EmissionTextures;
SamplerState sampler_EmissionTextures;
SamplerState sampler_AlbedoTextures;
SamplerState sampler_TransmissionTextures;
float _MaterialAtlasTexelSize;
float _AlbedoBoost;
uint _FrameIdx;
uint _SpotLightCount;

void SamplePunctualLights(UnifiedRT::DispatchInfo dispatchInfo)
{
    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(_RayTracingAccelerationStructure);

    MaterialPoolParamSet matPoolParams;
    matPoolParams.materialEntries = _MaterialEntries;
    matPoolParams.albedoTextures = _AlbedoTextures;
    matPoolParams.transmissionTextures = _TransmissionTextures;
    matPoolParams.emissionTextures = _EmissionTextures;
    matPoolParams.emissionSampler = sampler_EmissionTextures;
    matPoolParams.albedoSampler = sampler_AlbedoTextures;
    matPoolParams.transmissionSampler = sampler_TransmissionTextures;
    matPoolParams.atlasTexelSize = _MaterialAtlasTexelSize;
    matPoolParams.albedoBoost = _AlbedoBoost;

    QrngKronecker rng;
    rng.Init(dispatchInfo.globalThreadIndex.x, _FrameIdx);

    const uint spotLightIndex = min(rng.GetFloat(0) * _SpotLightCount, _SpotLightCount - 1);
    const SpotLight light = _SpotLights[spotLightIndex];

    UnifiedRT::Ray ray;
    ray.tMin = 0;
    ray.tMax = FLT_MAX;
    ray.origin = light.position;
    {
        float3 localDir = SampleConeUniform(rng.GetFloat(1), rng.GetFloat(1), light.cosAngle);
        float3x3 spotBasis = OrthoBasisFromVector(light.direction);
        ray.direction = mul(spotBasis, localDir);
    }

    PunctualLightSample lightSample = (PunctualLightSample)0;
    lightSample.dir = ray.direction;

    UnifiedRT::Hit hitResult = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, 0xFFFFFFFF, ray, UnifiedRT::kRayFlagNone);
    if (hitResult.IsValid() && hitResult.isFrontFace)
    {
        const UnifiedRT::InstanceData hitInstance = UnifiedRT::GetInstance(hitResult.instanceID);
        const SurfaceGeometry hitGeo = FetchSurfaceGeometry(hitInstance, hitResult);
        const MaterialPool::MaterialProperties hitMat = MaterialPool::LoadMaterialProperties(
            matPoolParams.materialEntries,
            matPoolParams.albedoTextures,
            matPoolParams.albedoSampler,
            matPoolParams.transmissionTextures,
            matPoolParams.transmissionSampler,
            matPoolParams.emissionTextures,
            matPoolParams.emissionSampler,
            matPoolParams.albedoBoost,
            matPoolParams.atlasTexelSize,
            hitInstance.userMaterialID,
            hitGeo.uv0,
            hitGeo.uv1);

        lightSample.hitPos = ray.origin + ray.direction * hitResult.hitDistance;
        lightSample.hitNormal = hitGeo.normal;
        lightSample.distance = hitResult.hitDistance;
        lightSample.hitAlbedo = hitMat.baseColor;
        lightSample.reciprocalDensity = AreaOfSphericalCapWithRadiusOne(light.cosAngle) * _SpotLightCount;
        lightSample.hitInstanceId = hitResult.instanceID;
        lightSample.hitPrimitiveIndex = hitResult.primitiveIndex;
        lightSample.intensity = light.intensity;
    }
    else
    {
        lightSample.MarkNoHit();
    }

    _Samples[dispatchInfo.globalThreadIndex.x] = lightSample;
}
