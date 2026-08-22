#ifndef SURFACE_CACHE_PATH_TRACING
#define SURFACE_CACHE_PATH_TRACING

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Sampling/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/FetchGeometry.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/TraceRayAndQueryHit.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/PathTracing/MaterialPool/MaterialPool.hlsl"
#include "Common.hlsl"
#include "PatchUtil.hlsl"
#include "PatchAllocationRequest.hlsl"
#include "PunctualLights.hlsl"
#include "EmissiveTriangles.hlsl"

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
    Texture2DArray<float4> albedoTextures;
    Texture2DArray<float4> emissionTextures;
    SamplerState emissionSampler;
    SamplerState albedoSampler;
    float atlasTexelSize; // The size of 1 texel in the atlases above
    float albedoBoost;
};

static const float3 invalidRadiance = float3(-1.0f, -1.0f, -1.0f);

struct RadianceSample
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

RadianceSample SamplePunctualLightBounceRadiance(
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
    RadianceSample result = (RadianceSample)0;

    PunctualLightSample punctualLightSample = punctualLightSamples[min(uniformRand * punctualLightSampleCount, punctualLightSampleCount - 1)];
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

// Represents a specific sample on an emissive triangle.
struct EmissiveTriangleSample
{
    float3 pos;
    float3 normal;
    float3 emission;
    float reciprocalDensity;

    void MarkNoHit()
    {
        emission = -1.0f;
    }

    bool HasHit()
    {
        return all(emission != -1.0f);
    }
};

struct WorldSpaceTriangle
{
    GeoPoolVertex v0;
    GeoPoolVertex v1;
    GeoPoolVertex v2;
    float3 w0;
    float3 w1;
    float3 w2;
};

WorldSpaceTriangle FetchWorldSpaceTriangle(UnifiedRT::InstanceData instance, uint primitiveIndex)
{
    const GeoPoolMeshChunk meshInfo = g_MeshList[instance.geometryIndex];
    const uint3 vertexIndices = UnifiedRT::Internal::FetchTriangleIndices(meshInfo, primitiveIndex);

    WorldSpaceTriangle tri;
    tri.v0 = UnifiedRT::Internal::FetchVertex(meshInfo, vertexIndices.x);
    tri.v1 = UnifiedRT::Internal::FetchVertex(meshInfo, vertexIndices.y);
    tri.v2 = UnifiedRT::Internal::FetchVertex(meshInfo, vertexIndices.z);
    tri.w0 = mul(float4(tri.v0.pos, 1.0f), instance.localToWorld);
    tri.w1 = mul(float4(tri.v1.pos, 1.0f), instance.localToWorld);
    tri.w2 = mul(float4(tri.v2.pos, 1.0f), instance.localToWorld);
    return tri;
}

float CalculateTriangleArea(UnifiedRT::InstanceData instance, uint primitiveIndex)
{
    const WorldSpaceTriangle tri = FetchWorldSpaceTriangle(instance, primitiveIndex);
    return 0.5f * length(cross(tri.w1 - tri.w0, tri.w2 - tri.w0));
}

float EmissiveTriangleReciprocalDensity(uint emissiveTriangleCount, float triangleArea)
{
    return emissiveTriangleCount * triangleArea;
}

EmissiveTriangleSample SampleEmissiveTriangles(
    StructuredBuffer<EmissiveTriangle> emissiveTriangles,
    uint emissiveTriangleCount,
    MaterialPoolParamSet matPoolParams,
    float triangleSelectionRand,
    float2 pointRand)
{
    EmissiveTriangleSample emissiveSample = (EmissiveTriangleSample)0;

    const uint emissiveTriangleIndex = min(triangleSelectionRand * emissiveTriangleCount, emissiveTriangleCount - 1);
    const EmissiveTriangle emissiveTriangle = emissiveTriangles[emissiveTriangleIndex];

    const UnifiedRT::InstanceData instance = UnifiedRT::GetInstance(emissiveTriangle.instanceId);
    const WorldSpaceTriangle tri = FetchWorldSpaceTriangle(instance, emissiveTriangle.primitiveIndex);
    const float3 crossProduct = cross(tri.w1 - tri.w0, tri.w2 - tri.w0);
    const float doubleArea = length(crossProduct);

    if (doubleArea > 0.0f)
    {
        const float2 samplePoint = MapUnitSquareToUnitTriangle(pointRand);
        const float bary0 = 1.0f - samplePoint.x - samplePoint.y;
        const float bary1 = samplePoint.x;
        const float bary2 = samplePoint.y;

        const float2 uv0 = bary0 * tri.v0.uv0 + bary1 * tri.v1.uv0 + bary2 * tri.v2.uv0;
        const float2 uv1 = bary0 * tri.v0.uv1 + bary1 * tri.v1.uv1 + bary2 * tri.v2.uv1;

        const MaterialPool::MaterialEntry matEntry = matPoolParams.materialEntries[instance.userMaterialID];
        const float3 emission = MaterialPool::LoadEmission(matEntry, matPoolParams.emissionTextures, matPoolParams.emissionSampler, matPoolParams.atlasTexelSize, uv0, uv1);

        emissiveSample.pos = bary0 * tri.w0 + bary1 * tri.w1 + bary2 * tri.w2;
        emissiveSample.normal = crossProduct * instance.localToWorldDetSign / doubleArea;
        emissiveSample.emission = emission;
        emissiveSample.reciprocalDensity = EmissiveTriangleReciprocalDensity(emissiveTriangleCount, 0.5f * doubleArea);
    }
    else
    {
        emissiveSample.MarkNoHit();
    }

    return emissiveSample;
}

static const float uniformHemisphereDensity = 1.0f / (2.0f * PI);

RadianceSample EstimateIncomingRadianceFromEmissiveTriangles(
    UnifiedRT::DispatchInfo dispatchInfo,
    UnifiedRT::RayTracingAccelStruct accelStruct,
    StructuredBuffer<EmissiveTriangle> emissiveTriangles,
    uint emissiveTriangleCount,
    MaterialPoolParamSet matPoolParams,
    float emissiveTriangleIntensityMultiplier,
    float3 position,
    float3 normal,
    float additionalRayOffset,
    float triangleSelectionRand,
    float2 pointRand)
{
    EmissiveTriangleSample emissiveSample = SampleEmissiveTriangles(
        emissiveTriangles,
        emissiveTriangleCount,
        matPoolParams,
        triangleSelectionRand,
        pointRand);

    RadianceSample result = (RadianceSample)0;

    // Valid sample?
    if (!emissiveSample.HasHit())
    {
        result.MarkInvalid();
        return result;
    }

    // Correct hemisphere from observer's POV?
    const float epsilon = 0.01f;
    if (dot(normal, emissiveSample.pos - position) <= epsilon)
    {
        return result;
    }

    // Is the sample occluded?
    const float3 shadowRayOrigin = OffsetRayOrigin(position, normal, additionalRayOffset);
    const float3 toLight = emissiveSample.pos - shadowRayOrigin;
    const float distanceToLight = length(toLight);
    const float3 shadowRayDirection = toLight * rcp(distanceToLight);

    UnifiedRT::Ray shadowRay;
    shadowRay.origin = shadowRayOrigin;
    shadowRay.direction = shadowRayDirection;
    shadowRay.tMin = 0;
    shadowRay.tMax = distanceToLight * (1.0f - epsilon); // Just a bit shorter than the real distance
    UnifiedRT::Hit shadowHit = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, 0xFFFFFFFF, shadowRay, UnifiedRT::kRayFlagNone);
    if (shadowHit.IsValid())
    {
        if (!IsValidSample(shadowHit.isFrontFace))
        {
            result.MarkInvalid();
        }
        return result;
    }

    // Correct hemisphere from light POV?
    const float lightCosTerm = dot(-shadowRayDirection, emissiveSample.normal);
    if (lightCosTerm <= epsilon)
    {
        return result;
    }

    // Calculate MIS weight for light ray hitting emissive triangle.
    const float areaToSolidAngleJacobian = Square(distanceToLight) / lightCosTerm; // dA/dω
    const float uniformTriangleDensity = rcp(emissiveSample.reciprocalDensity) * areaToSolidAngleJacobian; // p_A * (dA/dω) = p_ω
    const float emissiveLightRayMISWeight = PowerHeuristic(uniformTriangleDensity, uniformHemisphereDensity);

    // Get contribution.
    result.direction = shadowRayDirection;
    result.radianceOverDensity =
        emissiveSample.emission * emissiveTriangleIntensityMultiplier *
        rcp(uniformTriangleDensity) * emissiveLightRayMISWeight;
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
        PatchUtil::PatchIndexLookupResult result = PatchUtil::LookupPatchIndex(volumeParams, cellPatchIndices, position, normal);
        UNITY_OUT_OF_BOUNDS_BRANCH
        if (PatchUtil::HasValidPatchIndex(result))
        {
            bouncePatchIndex = PatchUtil::GetPatchIndex(result);
            radiance += PatchUtil::EvalIrradiance(patchIrradiances[bouncePatchIndex], normal);
        }

    }

    radiance *= albedo * INV_PI;
    return radiance;
}

float3 IncomingEnvironmentAndDirectionalBounceAndMultiBounceAndEmissionRadiance(
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
    uint emissiveTriangleCount,
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

            // Calculate MIS weight for hemisphere rays hitting emissive triangles.
            float emissiveHemisphereRayMISWeight = 0.0f;
            if (any(hitEmission > 0.0f))
            {
                const float lightArea = CalculateTriangleArea(hitInstance, hitResult.primitiveIndex);
                const float lightCosTerm = dot(-ray.direction, hitGeo.normal);
                if (lightArea > 0 && lightCosTerm > 0)
                {
                    const float areaToSolidAngleJacobian = Square(hitResult.hitDistance) / lightCosTerm; // dA/dω
                    const float uniformTriangleDensity = rcp(EmissiveTriangleReciprocalDensity(emissiveTriangleCount, lightArea)) * areaToSolidAngleJacobian; // p_A * (dA/dω) = p_ω
                    emissiveHemisphereRayMISWeight = PowerHeuristic(uniformHemisphereDensity, uniformTriangleDensity);
                }
            }

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
                bouncePatchIndex);

            radiance += hitEmission * emissiveTriangleIntensityMultiplier * emissiveHemisphereRayMISWeight;

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
                    PatchUtil::PatchRequestState reqState = patchStatistics[bouncePatchIndex].requestState;
                    if (PatchUtil::GetRank(reqState) == 1)
                    {
                        PatchUtil::SetHeartbeat(reqState, frameIndex);
                        patchStatistics[bouncePatchIndex].requestState = reqState;
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
