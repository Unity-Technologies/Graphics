#include "Common.hlsl"
#include "PatchUtil.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/FetchGeometry.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/TraceRayAndQueryHit.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/PathTracing/MaterialPool/MaterialPool.hlsl"

struct SurfaceGeometry
{
    float3 position;
    float3 normal;
    float2 uv0;
    float2 uv1;
};

SurfaceGeometry FetchSurfaceGeometry(UnifiedRT::InstanceData instanceInfo, UnifiedRT::Hit hit)
{
    UnifiedRT::HitGeomAttributes attributes = UnifiedRT::FetchHitGeomAttributes(hit);

    SurfaceGeometry res;
    res.position = mul(float4(attributes.position, 1), instanceInfo.localToWorld);
    res.normal = normalize(mul((float3x3)instanceInfo.localToWorldNormals, attributes.faceNormal));
    res.uv0 = attributes.uv0.xy;
    res.uv1 = attributes.uv1.xy;

    return res;
}

struct MaterialPoolParamSet
{
    StructuredBuffer<MaterialPool::MaterialEntry> materialEntries;
    Texture2DArray<float4> albedoTextures;
    Texture2DArray<float4> transmissionTextures;
    Texture2DArray<float4> emissionTextures;
    SamplerState emissionSampler;
    SamplerState albedoSampler;
    SamplerState transmissionSampler;
    float atlasTexelSize; // The size of 1 texel in the atlases above
    float albedoBoost;
};

static const float3 invalidRadianceSample = float3(-1.0f, -1.0f, -1.0f);

float3 SampleOutgoingRadianceAssumingLambertianBrdf(
    float3 position,
    float3 normal,
    UnifiedRT::DispatchInfo dispatchInfo,
    UnifiedRT::RayTracingAccelStruct accelStruct,
    float3 dirLightDirection,
    float3 dirLightIntensity,
    bool multiBounce,
    IrradianceBufferType patchIrradiances,
    StructuredBuffer<uint> cellPatchIndices,
    uint volumeSpatialResolution,
    StructuredBuffer<int3> cascadeOffsets,
    float3 volumeTargetPos,
    uint cascadeCount,
    float volumeVoxelMinSize,
    float3 albedo,
    float3 emission)
{
    float3 radianceSample = 0.0f;

    if (any(dirLightIntensity != 0.0f))
    {
        const float worldHitNormalDotSunDir = dot(dirLightDirection, normal);
        if (worldHitNormalDotSunDir < 0.0f)
        {
            UnifiedRT::Ray ray2;
            ray2.origin = OffsetRayOrigin(position, normal);
            ray2.direction = -dirLightDirection;
            ray2.tMin = 0;
            ray2.tMax = FLT_MAX;

            UnifiedRT::Hit hitResult2 = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, 0xFFFFFFFF, ray2, UnifiedRT::kRayFlagNone);
            if (!hitResult2.IsValid())
            {
                radianceSample += dirLightIntensity * dot(-dirLightDirection, normal);
            }
        }
    }

    if (multiBounce)
    {
        float3 cacheRead = PatchUtil::ReadPlanarIrradiance(volumeTargetPos, patchIrradiances, cellPatchIndices, volumeSpatialResolution, cascadeOffsets, cascadeCount, volumeVoxelMinSize, position, normal);
        if (all(cacheRead != PatchUtil::invalidIrradiance))
            radianceSample += cacheRead;
    }

    radianceSample *= albedo / PI;
    radianceSample += emission;
    return radianceSample;
}

float3 SampleIncomingRadianceAssumingLambertianBrdf(
    UnifiedRT::DispatchInfo dispatchInfo,
    UnifiedRT::RayTracingAccelStruct accelStruct,
    UnifiedRT::Ray ray,
    MaterialPoolParamSet matPoolParams,
    float3 dirLightDirection,
    float3 dirLightIntensity,
    bool multiBounce,
    TextureCube<float3> envTex,
    SamplerState envSampler,
    IrradianceBufferType patchIrradiances,
    StructuredBuffer<uint> cellPatchIndices,
    uint volumeSpatialResolution,
    StructuredBuffer<int3> cascadeOffsets,
    float3 volumeTargetPos,
    uint cascadeCount,
    float volumeVoxelMinSize)
{
    UnifiedRT::Hit hitResult = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, 0xFFFFFFFF, ray, UnifiedRT::kRayFlagNone);
    float3 radianceSample;
    if (hitResult.IsValid())
    {
        if (!hitResult.isFrontFace)
        {
            radianceSample = invalidRadianceSample;
        }
        else
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

            radianceSample = SampleOutgoingRadianceAssumingLambertianBrdf(
                hitGeo.position,
                hitGeo.normal,
                dispatchInfo,
                accelStruct,
                dirLightDirection,
                dirLightIntensity,
                multiBounce,
                patchIrradiances,
                cellPatchIndices,
                volumeSpatialResolution,
                cascadeOffsets,
                volumeTargetPos,
                cascadeCount,
                volumeVoxelMinSize,
                hitMat.baseColor,
                hitMat.emissive);
        }
    }
    else
    {
        radianceSample = envTex.SampleLevel(envSampler, ray.direction, 0);
    }
    return radianceSample;
}
