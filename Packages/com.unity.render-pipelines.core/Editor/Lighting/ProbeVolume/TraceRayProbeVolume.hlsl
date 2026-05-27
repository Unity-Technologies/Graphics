#ifndef _PROBE_VOLUME_TRACERAY_HLSL_
#define _PROBE_VOLUME_TRACERAY_HLSL_

struct RayPayload
{
    UnifiedRT::Hit _hit;

    void Init()
    {
        _hit = (UnifiedRT::Hit)0;
        _hit.instanceID = -1;
    }

    UnifiedRT::Hit GetHit()
    {
        return _hit;
    }

    void SetHit(UnifiedRT::Hit hit)
    {
        _hit = hit;
    }

    bool HasHit()
    {
        return _hit.IsValid();
    }
};

struct ProceduralIntersectionAttribs
{
    bool isFrontFace;
};

#define UNIFIED_RT_PAYLOAD RayPayload
#ifndef UNIFIED_RT_RAYGEN_FUNC
#define UNIFIED_RT_RAYGEN_FUNC RayGenExecute
#endif
#define UNIFIED_RT_CLOSESTHIT_FUNC ClosestHitExecute

#ifdef TERRAIN_RAY_MARCHING_ENABLED
#define UNIFIED_RT_INTERSECTION_FUNC IntersectionExecute
#define UNIFIED_RT_ADDITIONAL_INTERSECTION_ATTRIBS ProceduralIntersectionAttribs
#endif
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/TraceRay.hlsl"

#ifdef TERRAIN_RAY_MARCHING_ENABLED
void ClosestHitExecute(UnifiedRT::HitContext hitContext, inout RayPayload payload, ProceduralIntersectionAttribs additionalAttribs)
{
    UnifiedRT::Hit hit;
    hit.instanceID = hitContext.InstanceID();
    hit.primitiveIndex = hitContext.PrimitiveIndex();
    hit.uvBarycentrics = hitContext.UvBarycentrics();
    hit.hitDistance = hitContext.RayTCurrent();
    hit.isFrontFace = hitContext.PrimitiveType() == UnifiedRT::kCommittedProceduralHit ? additionalAttribs.isFrontFace : hitContext.IsFrontFace();
    payload.SetHit(hit);
}


bool IntersectionExecute(UnifiedRT::HitContext hitContext, out float hitT,
                        out float2 uvAttributes, out ProceduralIntersectionAttribs additionalAttribs)
{
    additionalAttribs = (ProceduralIntersectionAttribs)0;

    bool frontFace = false;
    float2 uv = 0;
    bool hit = RayMarchTerrainTile(
        hitContext.InstanceID(),
        hitContext.PrimitiveIndex(),
        hitContext.WorldRayOrigin(),
        hitContext.WorldRayDirection(),
        hitContext.RayTCurrent(),
        hitT,
        frontFace,
        uv);

    if (hit && hitT < hitContext.RayTCurrent() && hitT >= hitContext.RayTMin())
    {
        uvAttributes = uv;
        additionalAttribs.isFrontFace = frontFace;
        return true;
    }
    return false;
}
#else
void ClosestHitExecute(UnifiedRT::HitContext hitContext, inout RayPayload payload)
{
    UnifiedRT::Hit hit;
    hit.instanceID = hitContext.InstanceID();
    hit.primitiveIndex = hitContext.PrimitiveIndex();
    hit.uvBarycentrics = hitContext.UvBarycentrics();
    hit.hitDistance = hitContext.RayTCurrent();
    hit.isFrontFace = hitContext.IsFrontFace();
    payload.SetHit(hit);
}

#endif

UnifiedRT::Hit TraceRayClosestHit(UnifiedRT::DispatchInfo dispatchInfo, UnifiedRT::RayTracingAccelStruct accelStruct, uint instanceMask, UnifiedRT::Ray ray, uint rayFlags)
{
    RayPayload payload;
    payload.Init();

    UnifiedRT::TraceRay(dispatchInfo, accelStruct, instanceMask, ray, rayFlags | UnifiedRT::kRayFlagForceOpaque, payload);

    return payload.GetHit();
}

#endif // _PROBE_VOLUME_TRACERAY_HLSL_
