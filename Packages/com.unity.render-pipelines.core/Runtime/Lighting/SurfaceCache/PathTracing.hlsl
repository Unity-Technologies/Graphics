#ifndef SURFACE_CACHE_PATH_TRACING
#define SURFACE_CACHE_PATH_TRACING

#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/FetchGeometry.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/TraceRayAndQueryHit.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/PathTracing/MaterialPool/MaterialPool.hlsl"
#include "Common.hlsl"
#include "PatchUtil.hlsl"
#include "PunctualLightSample.hlsl"

struct SurfaceGeometry
{
    float3 position;
    float3 normal;
    float2 uv0;
    float2 uv1;
};

bool IsValidSample(bool isFrontFace)
{
    // If we hit backface geometry then we assume that a patch is inside geometry. In this case we
    // effectively pause the update process by skipping samples to prevent accumulating "irrelevant"
    // darkness which can give artifacts if/when a patch reappears after temporarily being inside
    // moving geometry.
    return isFrontFace;
}

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
    Texture2DArray albedoTextures;
    Texture2DArray transmissionTextures;
    Texture2DArray emissionTextures;
    SamplerState emissionSampler;
    SamplerState albedoSampler;
    SamplerState transmissionSampler;
    float atlasTexelSize; // The size of 1 texel in the atlases above
    float albedoBoost;
};

static const float3 invalidRadiance = float3(-1.0f, -1.0f, -1.0f);

struct PunctualLightBounceRadianceSample
{
    float3 direction;
    float3 radianceOverDensity; // L_i(X_i) / p(X_i)

    void MarkInvalid()
    {
        radianceOverDensity = -1.0f;
    }

    bool IsValid()
    {
        return !all(radianceOverDensity == -1.0f);
    }
};

PunctualLightBounceRadianceSample SamplePunctualLightBounceRadiance(
    UnifiedRT::DispatchInfo dispatchInfo,
    UnifiedRT::RayTracingAccelStruct accelStruct,
    StructuredBuffer<PunctualLightSample> punctualLightSamples,
    uint punctualLightSampleCount,
    float uniformRand,
    float3 spotLightIntensity,
    float3 position,
    float3 normal)
{
    PunctualLightBounceRadianceSample result = (PunctualLightBounceRadianceSample)0;

    PunctualLightSample punctualLightSample = punctualLightSamples[min(punctualLightSampleCount, uniformRand * punctualLightSampleCount)];
    if (punctualLightSample.HasHit())
    {
        const float epsilon = 0.01f;
        const float planeDistance = dot(normal, punctualLightSample.hitPos - position);
        if (epsilon < planeDistance) // Light sample hit point must be "in front" of the patch.
        {
            UnifiedRT::Ray reconnectionRay;
            reconnectionRay.origin = OffsetRayOrigin(position, normal);
            reconnectionRay.direction = normalize(punctualLightSample.hitPos - position);
            reconnectionRay.tMin = 0;
            reconnectionRay.tMax = FLT_MAX;
            UnifiedRT::Hit reconnectionResult = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, 0xFFFFFFFF, reconnectionRay, UnifiedRT::kRayFlagNone);

            if (!IsValidSample(reconnectionResult.isFrontFace))
            {
                result.MarkInvalid();
            }
            else
            {
                if (reconnectionResult.IsValid() &&
                    reconnectionResult.instanceID == punctualLightSample.hitInstanceId &&
                    reconnectionResult.primitiveIndex == punctualLightSample.hitPrimitiveIndex)
                {
                    result.direction = reconnectionRay.direction;
                    #if 0 // readable version
                    const float bounceCosTerm = dot(-punctualLightSample.dir, punctualLightSample.hitNormal);
                    const float bounceSolidAngleToAreaJacobian = 1.0f / (punctualLightSample.distance * punctualLightSample.distance); // To integrate over punctual light we must switch to area measure.
                    const float3 brdf = punctualLightSample.hitAlbedo * INV_PI;
                    const float3 punctualLightBouncedRadiance = bounceCosTerm * bounceSolidAngleToAreaJacobian * spotLightIntensity * brdf;

                    // We transform from patch solid angle measure to (common) surface area measure to punctual light solid angle measure.
                    const float patchSolidAngleToBounceAreaJacobian = dot(-reconnectionRay.direction, punctualLightSample.hitNormal) / (reconnectionResult.hitDistance * reconnectionResult.hitDistance);
                    const float bounceAreaToLightSolidAngleJacobian = punctualLightSample.distance * punctualLightSample.distance / dot(-punctualLightSample.dir, punctualLightSample.hitNormal);
                    const float patchSolidAngleToLightSolidAngleJacbian = patchSolidAngleToBounceAreaJacobian * bounceAreaToLightSolidAngleJacobian;

                    result.radianceOverDensity = punctualLightBouncedRadiance * patchSolidAngleToLightSolidAngleJacbian * punctualLightSample.reciprocalDensity;
                    #else // optimized version
                    result.radianceOverDensity =
                        INV_PI * dot(-reconnectionRay.direction, punctualLightSample.hitNormal) *
                        punctualLightSample.reciprocalDensity *
                        rcp(reconnectionResult.hitDistance * reconnectionResult.hitDistance) *
                        spotLightIntensity * punctualLightSample.hitAlbedo;
                    #endif
                }
            }
        }
    }

    return result;
}

float3 OutgoingDirectionalBounceAndMultiBounceRadiance(
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
    float3 radiance = 0.0f;

    if (any(dirLightIntensity != 0.0f))
    {
        const float worldHitNormalDotSunDir = dot(dirLightDirection, normal);
        if (worldHitNormalDotSunDir < 0.0f)
        {
            UnifiedRT::Ray shadowRay;
            shadowRay.origin = OffsetRayOrigin(position, normal);
            shadowRay.direction = -dirLightDirection;
            shadowRay.tMin = 0;
            shadowRay.tMax = FLT_MAX;

            UnifiedRT::Hit hitResult2 = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, 0xFFFFFFFF, shadowRay, UnifiedRT::kRayFlagNone);
            if (!hitResult2.IsValid())
            {
                radiance += dirLightIntensity * dot(-dirLightDirection, normal);
            }
        }
    }

    if (multiBounce)
    {
        float3 cacheRead = PatchUtil::ReadPlanarIrradiance(volumeTargetPos, patchIrradiances, cellPatchIndices, volumeSpatialResolution, cascadeOffsets, cascadeCount, volumeVoxelMinSize, position, normal);
        if (all(cacheRead != PatchUtil::invalidIrradiance))
            radiance += cacheRead;
    }

    radiance *= albedo * INV_PI;
    radiance += emission;
    return radiance;
}

float3 IncomingEnviromentAndDirectionalBounceAndMultiBounceRadiance(
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
    float3 radiance;
    if (hitResult.IsValid())
    {
        if (!IsValidSample(hitResult.isFrontFace))
        {
            radiance = invalidRadiance;
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

            radiance = OutgoingDirectionalBounceAndMultiBounceRadiance(
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
        radiance = envTex.SampleLevel(envSampler, ray.direction, 0);
    }
    return radiance;
}

#endif
