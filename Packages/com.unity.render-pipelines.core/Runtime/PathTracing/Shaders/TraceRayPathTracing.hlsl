#ifndef _UNIFIEDRAYTRACING_TRACERAYANDQUERYHIT_HLSL_
#define _UNIFIEDRAYTRACING_TRACERAYANDQUERYHIT_HLSL_

// float3 transmission and Hit aliased to minimize payload size
struct PathTracingPayload
{
    UnifiedRT::Hit _hit;

    void Init(bool isShadowRay)
    {
        _hit = (UnifiedRT::Hit)0;
        _hit.instanceID = -1;
        _hit.isFrontFace = isShadowRay;
    }

    bool IsShadowRay()
    {
        return _hit.isFrontFace; // before invoking ClosestHitExecute, isFrontFace holds isShadowRay
    }

    UnifiedRT::Hit GetHit()
    {
        return _hit;
    }

    void SetHit(UnifiedRT::Hit hit)
    {
        _hit = hit;
    }

    void SetTransmission(float3 transmission)
    {
        _hit.uvBarycentrics = transmission.rg;
        _hit.hitDistance = transmission.b;
    }

    float3 GetTransmission()
    {
        return float3(_hit.uvBarycentrics, _hit.hitDistance);
    }

    bool HasHit()
    {
        return _hit.IsValid();
    }

    void MarkHit()
    {
        _hit.instanceID = 1;
    }
};
#ifndef UNIFIED_RT_RAYGEN_FUNC
#define UNIFIED_RT_RAYGEN_FUNC RayGenExecute
#endif
#define UNIFIED_RT_ANYHIT_FUNC AnyHitExecute
#define UNIFIED_RT_CLOSESTHIT_FUNC ClosestHitExecute
#define UNIFIED_RT_PAYLOAD PathTracingPayload
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/TraceRay.hlsl"

UnifiedRT::Hit TraceRayClosestHit(UnifiedRT::DispatchInfo dispatchInfo, UnifiedRT::RayTracingAccelStruct accelStruct, uint instanceMask, UnifiedRT::Ray ray, uint rayFlags)
{
    PathTracingPayload payload;
    payload.Init(false);

    UnifiedRT::TraceRay(dispatchInfo, accelStruct, instanceMask, ray, rayFlags | UnifiedRT::kRayFlagForceOpaque, payload);

    return payload.GetHit();
}

bool TraceShadowRay(UnifiedRT::DispatchInfo dispatchInfo, UnifiedRT::RayTracingAccelStruct accelStruct, uint instanceMask, UnifiedRT::Ray ray, uint rayFlags, out float3 transmission)
{
    PathTracingPayload payLoadShadow= (PathTracingPayload)0;
    payLoadShadow.Init(true);
    payLoadShadow.SetTransmission(1.0f);

    UnifiedRT::TraceRay(dispatchInfo, accelStruct, instanceMask, ray, rayFlags | UnifiedRT::kRayFlagAcceptFirstHitAndEndSearch, payLoadShadow);

    transmission = payLoadShadow.GetTransmission();
    return payLoadShadow.HasHit();
}

#endif // _UNIFIEDRAYTRACING_TRACERAYANDQUERYHIT_HLSL_
