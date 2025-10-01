#include "Common.hlsl"
#include "PatchUtil.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/FetchGeometry.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/TraceRayAndQueryHit.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Common.hlsl"
#include "PathTracingCommon.hlsl"
#include "PathTracingMaterials.hlsl"

static const float3 invalidRadianceSample = float3(-1.0f, -1.0f, -1.0f);

float3 SampleOutgoingRadianceAssumingLambertianBrdf(
    float3 position,
    float3 normal,
    UnifiedRT::DispatchInfo dispatchInfo,
    UnifiedRT::RayTracingAccelStruct accelStruct,
    StructuredBuffer<PTLight> lightList,
    bool multiBounce,
    IrradianceBufferType patchIrradiances,
    StructuredBuffer<uint> cellPatchIndices,
    uint gridSize,
    StructuredBuffer<int3> cascadeOffsets,
    float3 gridTargetPos,
    uint cascadeCount,
    float voxelMinSize,
    float3 albedo,
    float3 emission)
{
    float3 radianceSample = 0.0f;

    // For now we assume only a single directional light has been added.
    // Support for more lights will be added.
    PTLight sun = lightList[0];

    const float worldHitNormalDotSunDir = dot(sun.forward, normal);
    if (worldHitNormalDotSunDir < 0.0f)
    {
        UnifiedRT::Ray ray2;
        ray2.origin = OffsetRayOrigin(position, normal);
        ray2.direction = -sun.forward;
        ray2.tMin = 0;
        ray2.tMax = FLT_MAX;

        UnifiedRT::Hit hitResult2 = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, 0xFFFFFFFF, ray2, UnifiedRT::kRayFlagNone);
        if (!hitResult2.IsValid())
        {
            radianceSample += sun.intensity * dot(-sun.forward, normal);
        }
    }

    if (multiBounce)
    {
        float3 cacheRead = PatchUtil::ReadPlanarIrradiance(gridTargetPos, patchIrradiances, cellPatchIndices, gridSize, cascadeOffsets, cascadeCount, voxelMinSize, position, normal);
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
    StructuredBuffer<PTLight> lightList,
    bool multiBounce,
    TextureCube<float3> envTex,
    SamplerState envSampler,
    IrradianceBufferType patchIrradiances,
    StructuredBuffer<uint> cellPatchIndices,
    uint gridSize,
    StructuredBuffer<int3> cascadeOffsets,
    float3 gridTargetPos,
    uint cascadeCount,
    float voxelMinSize)
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
            const PTHitGeom hitGeo = GetHitGeomInfo(hitInstance, hitResult);
            const MaterialProperties hitMat = LoadMaterialProperties(hitInstance, false, hitGeo);

            radianceSample = SampleOutgoingRadianceAssumingLambertianBrdf(
                hitGeo.worldPosition,
                hitGeo.worldFaceNormal,
                dispatchInfo,
                accelStruct,
                lightList,
                multiBounce,
                patchIrradiances,
                cellPatchIndices,
                gridSize,
                cascadeOffsets,
                gridTargetPos,
                cascadeCount,
                voxelMinSize,
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
