#ifndef SURFACE_CACHE_PATH_TRACING
#define SURFACE_CACHE_PATH_TRACING

#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/FetchGeometry.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/TraceRayAndQueryHit.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/PathTracing/MaterialPool/MaterialPool.hlsl"
#include "Common.hlsl"
#include "PatchUtil.hlsl"
#include "PatchAllocationRequest.hlsl"
#include "PunctualLights.hlsl"

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
    Texture2DArray emissionTextures;
    SamplerState emissionSampler;
    SamplerState albedoSampler;
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

float Square(float x)
{
    return x * x;
}

// Many distance window functions are possible. This one is the square root of
// the one currently used in URP: (1.0 - saturate((distanceSqr * 1.0 / rangeSqr)^2)).
float SharpPunctualLightRangeWindow(float range, float distanceSquared)
{
    const float lightRangeSquared = Square(range);
    const float distanceSquaredOverRangeSquared = distanceSquared / lightRangeSquared;
    const float window = 1.0f - saturate(distanceSquaredOverRangeSquared * distanceSquaredOverRangeSquared);
    return window;
}

// Many distance window functions are possible. This one matches the one currently
// used in URP: (1.0 - saturate((distanceSqr * 1.0 / rangeSqr)^2))^2.
float SmoothPunctualLightRangeWindow(float range, float distanceSquared)
{
    const float window = SharpPunctualLightRangeWindow(range, distanceSquared);
    return Square(window);
}

// Sharp spot light angle attenuation function:
// f(cosHitAngle) = saturate((cosHitAngle - cosOuterAngle) / (cosOuterAngle - cosInnerAngle)).
// This function is linear in cosHitAngle and satisfies f(cosOuterAngle) = 0 and
// f(cosInnerAngle) = 1.
// For perf reasons, the implementation assumes cosHitAngle <= cosOuterAngle.
// angleAttenuationValue1: If innerAngle != outerAngle then 1/(cosInnerAngle-cosOuterAngle), otherwise 0.
// angleAttenuationValue2: If innerAngle != outerAngle then cosOuterAngle/(cosOuterAngle-cosInnerAngle), otherwise 1.
float SharpSpotLightAngleAttenuation(float3 spotDirection, float3 hitDirection, float angleAttenuationValue1, float angleAttenuationValue2)
{
    // Note that
    // (cosHitAngle - cosOuterAngle) / (cosOuterAngle - cosInnerAngle) =
    // (cosHitAngle) * 1/(cosOuterAngle - cosInnerAngle) + cosOuterAngle / (cosInnerAngle - cosOuterAngle)
    // a * b + c
    // where
    // a = cosHitAngle,
    // b = 1/(cosOuterAngle - cosInnerAngle),
    // c = cosOuterAngle / (cosInnerAngle - cosOuterAngle).
    // We exploit this to reduce a fused multiply-add.
    const float cosHitAngle = dot(spotDirection, hitDirection);
    const float attenuation = saturate(cosHitAngle * angleAttenuationValue1 + angleAttenuationValue2);
    return attenuation;
}

// Smooth spot light angle attenuation linear in cos(hitAngle), then clamped, then squared for smoothing.
// More precisely: saturate((cosHitAngle - cosOuterAngle) / (cosOuterAngle - cosInnerAngle))^2.
// This matches URP's current behaviour (see AngleAttenuation in RealtimeLights.hlsl).
float SmoothSpotLightAngleAttenuation(float3 spotDirection, float3 hitDirection, float angleAttenuationValue1, float angleAttenuationValue2)
{
    const float attenuation = SharpSpotLightAngleAttenuation(spotDirection, hitDirection, angleAttenuationValue1, angleAttenuationValue2);
    return Square(attenuation); // Square to smoothen fade-out.
}

bool IsSpotLight(float cosOuterAngle)
{
    // Here we assume that spot lights are not allowed to have an outer angle of PI or larger.
    return cosOuterAngle != -1.0f;
}

PunctualLightBounceRadianceSample SamplePunctualLightBounceRadiance(
    UnifiedRT::DispatchInfo dispatchInfo,
    UnifiedRT::RayTracingAccelStruct accelStruct,
    StructuredBuffer<PunctualLight> lights,
    StructuredBuffer<PunctualLightSample> punctualLightSamples,
    uint punctualLightSampleCount,
    float uniformRand,
    float3 position,
    float3 normal,
    float additionalRayOffset)
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
            reconnectionRay.origin = OffsetRayOrigin(position, normal, additionalRayOffset);
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
                    const PunctualLight light = lights[punctualLightSample.lightIndex];
                    result.direction = reconnectionRay.direction;
                    #if 0 // readable version
                    const float distanceSquared = Square(punctualLightSample.distance);
                    const float rangeWindow = SmoothPunctualLightRangeWindow(light.range, distanceSquared);
                    float angularAttenuation = 1.0f;
                    if (IsSpotLight(light.cosOuterAngle))
                        angularAttenuation = SmoothSpotLightAngleAttenuation(light.direction, punctualLightSample.rayDirection, light.angleAttenuationValue1, light.angleAttenuationValue2);

                    const float bounceCosTerm = dot(-punctualLightSample.rayDirection, punctualLightSample.hitNormal);
                    const float bounceSolidAngleToAreaJacobian = 1.0f / distanceSquared; // To integrate over punctual light we must switch to area measure.
                    const float3 brdf = punctualLightSample.hitAlbedo * INV_PI;
                    const float3 punctualLightBouncedRadiance = bounceCosTerm * bounceSolidAngleToAreaJacobian * light.intensity * brdf;

                    // We transform from patch solid angle measure to (common) surface area measure to punctual light solid angle measure.
                    const float patchSolidAngleToBounceAreaJacobian = dot(-reconnectionRay.direction, punctualLightSample.hitNormal) / (reconnectionResult.hitDistance * reconnectionResult.hitDistance);
                    const float bounceAreaToLightSolidAngleJacobian = distanceSquared / dot(-punctualLightSample.rayDirection, punctualLightSample.hitNormal);
                    const float patchSolidAngleToLightSolidAngleJacobian = patchSolidAngleToBounceAreaJacobian * bounceAreaToLightSolidAngleJacobian;

                    if (isfinite(bounceSolidAngleToAreaJacobian) && isfinite(patchSolidAngleToBounceAreaJacobian))
                        result.radianceOverDensity = punctualLightBouncedRadiance * patchSolidAngleToLightSolidAngleJacobian * punctualLightSample.reciprocalDensity * rangeWindow * angularAttenuation;
                    else
                        result.MarkInvalid();
                    #else // optimized version
                    const float reciprocalReconnectionDistance = rcp(reconnectionResult.hitDistance);
                    if (isfinite(reciprocalReconnectionDistance))
                    {
                        const float distanceSquared = Square(punctualLightSample.distance);
                        const float rangeWindow = SharpPunctualLightRangeWindow(light.range, distanceSquared);

                        float angularAttenuation = 1.0f;
                        if (IsSpotLight(light.cosOuterAngle))
                            angularAttenuation = SharpSpotLightAngleAttenuation(light.direction, punctualLightSample.rayDirection, light.angleAttenuationValue1, light.angleAttenuationValue2);

                        result.radianceOverDensity =
                            INV_PI * dot(-reconnectionRay.direction, punctualLightSample.hitNormal) *
                            punctualLightSample.reciprocalDensity *
                            light.intensity * punctualLightSample.hitAlbedo *
                            Square(reciprocalReconnectionDistance * rangeWindow * angularAttenuation);
                    }
                    else
                    {
                        result.MarkInvalid();
                    }
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
    PatchIrradianceBufferType patchIrradiances,
    CellPatchIndexBufferType cellPatchIndices,
    PatchUtil::VolumeParamSet volumeParams,
    float3 albedo,
    float3 emission,
    float emissiveTriangleIntensityMultiplier,
    out uint bouncePatchIndex)
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

            UnifiedRT::Hit hitResult = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, 0xFFFFFFFF, shadowRay, UnifiedRT::kRayFlagNone);
            if (!hitResult.IsValid())
            {
                radiance += dirLightIntensity * dot(-dirLightDirection, normal);
            }
        }
    }

    bouncePatchIndex = PatchUtil::invalidPatchIndex;
    if (multiBounce)
    {
        bouncePatchIndex = PatchUtil::FindPatchIndex(volumeParams, cellPatchIndices, position, normal);
        if (bouncePatchIndex != PatchUtil::invalidPatchIndex)
            radiance += PatchUtil::EvalIrradiance(patchIrradiances[bouncePatchIndex], normal);
    }

    radiance *= albedo * INV_PI;
    radiance += emission * emissiveTriangleIntensityMultiplier;
    return radiance;
}

float3 IncomingEnvironmentAndDirectionalBounceAndMultiBounceRadiance(
    UnifiedRT::DispatchInfo dispatchInfo,
    UnifiedRT::RayTracingAccelStruct accelStruct,
    UnifiedRT::Ray ray,
    MaterialPoolParamSet matPoolParams,
    float3 dirLightDirection,
    float3 dirLightIntensity,
    bool multiBounce,
    TextureCube<float3> envTex,
    float envIntensityMultiplier,
    float emissiveTriangleIntensityMultiplier,
    SamplerState envSampler,
    PatchIrradianceBufferType patchIrradiances,
    RWStructuredBuffer<PatchUtil::PatchStatisticsSet> patchStatistics,
    RWStructuredBuffer<PatchAllocationRequest> allocationRequests,
    RWStructuredBuffer<uint> allocationRequestCount,
    CellPatchIndexBufferType cellPatchIndices,
    PatchUtil::VolumeParamSet volumeParams,
    bool enablePatchAllocation,
    uint frameIndex)
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
            const MaterialPool::MaterialEntry matEntry = matPoolParams.materialEntries[hitInstance.userMaterialID];
            const float3 hitAlbedo = MaterialPool::LoadAlbedoWithBoost(matEntry, matPoolParams.albedoTextures, matPoolParams.albedoSampler, matPoolParams.atlasTexelSize, matPoolParams.albedoBoost, hitGeo.uv0, hitGeo.uv1);
            const float3 hitEmission = MaterialPool::LoadEmission(matEntry, matPoolParams.emissionTextures, matPoolParams.emissionSampler, matPoolParams.atlasTexelSize, hitGeo.uv0, hitGeo.uv1);

            uint bouncePatchIndex;
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
                volumeParams,
                hitAlbedo,
                hitEmission,
                emissiveTriangleIntensityMultiplier,
                bouncePatchIndex);

            if (enablePatchAllocation)
            {
                if (bouncePatchIndex == PatchUtil::invalidPatchIndex)
                {
                    uint requestIdx;
                    InterlockedAdd(allocationRequestCount[0], 1, requestIdx);
                    if (requestIdx < PatchAllocationRequestMax)
                    {
                        PatchAllocationRequest req;
                        req.position = hitGeo.position;
                        req.normal = hitGeo.normal;
                        allocationRequests[requestIdx] = req;
                    }
                }
                else
                {
                    PatchUtil::PatchCounterSet counters = patchStatistics[bouncePatchIndex].counters;
                    if (PatchUtil::GetRank(counters) == 1)
                    {
                        PatchUtil::SetLastAccessFrame(counters, frameIndex);
                        patchStatistics[bouncePatchIndex].counters = counters;
                    }
                }
            }
        }
    }
    else
    {
        radiance = envIntensityMultiplier * envTex.SampleLevel(envSampler, ray.direction, 0);
    }
    return radiance;
}

#endif
