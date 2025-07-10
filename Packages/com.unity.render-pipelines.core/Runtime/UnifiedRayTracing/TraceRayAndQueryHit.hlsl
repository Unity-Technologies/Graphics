#ifndef _UNIFIEDRAYTRACING_TRACERAYANDQUERYHIT_HLSL_
#define _UNIFIEDRAYTRACING_TRACERAYANDQUERYHIT_HLSL_

#define UNIFIED_RT_PAYLOAD UnifiedRT::Hit
#ifndef UNIFIED_RT_RAYGEN_FUNC
#define UNIFIED_RT_RAYGEN_FUNC RayGenExecute
#endif
#define UNIFIED_RT_CLOSESTHIT_FUNC ClosestHitExecute
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/TraceRay.hlsl"

void ClosestHitExecute(UnifiedRT::HitContext hitContext, inout UnifiedRT::Hit payload)
{
    payload.instanceID = hitContext.InstanceID();
    payload.primitiveIndex = hitContext.PrimitiveIndex();
    payload.uvBarycentrics = hitContext.UvBarycentrics();
    payload.hitDistance = hitContext.RayTCurrent();
    payload.isFrontFace = hitContext.IsFrontFace();
}

namespace UnifiedRT
{

Hit TraceRayClosestHit(DispatchInfo dispatchInfo, RayTracingAccelStruct accelStruct, uint instanceMask, Ray ray, uint rayFlags)
{
    Hit payload= (Hit)0;
    payload.instanceID = -1;

    TraceRay(dispatchInfo, accelStruct, instanceMask, ray, rayFlags | kRayFlagForceOpaque, payload);

    return payload;
}

bool TraceRayAnyHit(DispatchInfo dispatchInfo, RayTracingAccelStruct accelStruct, uint instanceMask, Ray ray, uint rayFlags)
{
    Hit payLoadShadow = (Hit)0;
    payLoadShadow.instanceID = -1;

    TraceRay(dispatchInfo, accelStruct, instanceMask, ray, rayFlags | kRayFlagForceOpaque | kRayFlagAcceptFirstHitAndEndSearch, payLoadShadow);

    return payLoadShadow.IsValid();
}

} // namespace UnifiedRT

#endif // _UNIFIEDRAYTRACING_TRACERAYANDQUERYHIT_HLSL_
