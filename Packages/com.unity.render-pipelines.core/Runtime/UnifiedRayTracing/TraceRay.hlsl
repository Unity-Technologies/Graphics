#ifndef _UNIFIEDRAYTRACING_TRACERAY_HLSL_
#define _UNIFIEDRAYTRACING_TRACERAY_HLSL_

#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Bindings.hlsl"
#if defined(UNIFIED_RT_BACKEND_COMPUTE)
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Compute/RayQuerySoftware.hlsl"
#endif

namespace UnifiedRT
{

#ifndef UNIFIED_RT_PAYLOAD
    #pragma message("Error, you must define UNIFIED_RT_PAYLOAD before including TraceRay.hlsl")
#endif

#if defined(UNIFIED_RT_BACKEND_HARDWARE)

float3 _WorldRayOrigin() { return WorldRayOrigin(); }
float3 _WorldRayDirection() { return WorldRayDirection(); }
float _RayTMin() { return RayTMin(); }
float _RayTCurrent() { return RayTCurrent(); }
uint _InstanceID() { return InstanceID(); }
uint _InstanceIndex() { return InstanceIndex(); }
uint _PrimitiveIndex() { return PrimitiveIndex(); }

struct HitContext
{
    float2 barycentrics;

    float3 WorldRayOrigin()
    {
        return _WorldRayOrigin();
    }

    float3 WorldRayDirection()
    {
        return _WorldRayDirection();
    }

    float RayTMin()
    {
        return _RayTMin();
    }

    float RayTCurrent()
    {
        return _RayTCurrent();
    }

    uint InstanceIndex()
    {
        return _InstanceIndex();
    }

    uint InstanceID()
    {
        return _InstanceID();
    }

    uint PrimitiveIndex()
    {
        return _PrimitiveIndex();
    }

    float2 UvBarycentrics()
    {
        return barycentrics;
    }

    bool IsFrontFace()
    {
       return (HitKind() == HIT_KIND_TRIANGLE_FRONT_FACE);
    }
};

void TraceRay(DispatchInfo dispatchInfo, RayTracingAccelStruct accelStruct, uint instanceMask, Ray ray, uint rayFlags, inout UNIFIED_RT_PAYLOAD payload)
{
    RayDesc rayDesc;
    rayDesc.Origin = ray.origin;
    rayDesc.TMin = ray.tMin;
    rayDesc.Direction = ray.direction;
    rayDesc.TMax = ray.tMax;

	TraceRay(accelStruct.accelStruct, rayFlags, instanceMask, 0, 1, 0, rayDesc, payload);
}

#elif defined(UNIFIED_RT_BACKEND_COMPUTE)

struct HitContext
{
    float3 worldRayOrigin;
    float3 worldRayDirection;
    float tmin;
    float tcurrent;
    uint instanceID;
    uint primitiveIndex;
    float2 barycentrics;
    bool isFrontFace;

    float3 WorldRayOrigin()
    {
        return worldRayOrigin;
    }

    float3 WorldRayDirection()
    {
        return worldRayDirection;
    }

    float RayTMin()
    {
        return tmin;
    }

    float RayTCurrent()
    {
        return tcurrent;
    }

    uint InstanceID()
    {
        return instanceID;
    }

    uint PrimitiveIndex()
    {
        return primitiveIndex;
    }

    float2 UvBarycentrics()
    {
        return barycentrics;
    }

    bool IsFrontFace()
    {
        return isFrontFace;
    }
};

} // namespace UnifiedRT

#ifdef UNIFIED_RT_ANYHIT_FUNC
    uint UNIFIED_RT_ANYHIT_FUNC(UnifiedRT::HitContext hitContext, inout UNIFIED_RT_PAYLOAD payload);
#endif

#ifdef UNIFIED_RT_CLOSESTHIT_FUNC
    void UNIFIED_RT_CLOSESTHIT_FUNC(UnifiedRT::HitContext hitContext, inout UNIFIED_RT_PAYLOAD payload);
#endif

namespace UnifiedRT {

#pragma warning(disable : 3557) // prevent warning when the "while (rayQuery.Proceed())" loop is unrolled

void TraceRay(DispatchInfo dispatchInfo, RayTracingAccelStruct accelStruct, uint instanceMask, Ray ray, uint rayFlags, inout UNIFIED_RT_PAYLOAD payload)
{
 #ifdef UNIFIED_RT_ANYHIT_FUNC
    RayQuery rayQuery;
    rayQuery.Init(dispatchInfo.globalThreadIndex, dispatchInfo.localThreadIndex, accelStruct, rayFlags, instanceMask, ray);
    while (rayQuery.Proceed())
    {
        // not necessary but makes sure the compiler optimizes the loop out when one of these flags is set
        if (rayFlags & (UnifiedRT::kRayFlagForceOpaque | UnifiedRT::kRayFlagCullNonOpaque))
            break;

        HitContext hitContext;
        hitContext.worldRayOrigin = rayQuery.WorldRayOrigin();
        hitContext.worldRayDirection = rayQuery.WorldRayDirection();
        hitContext.tmin = rayQuery.RayTMin();
        hitContext.tcurrent = rayQuery.CandidateTriangleRayT();
        hitContext.instanceID = rayQuery.CandidateInstanceID();
        hitContext.primitiveIndex = rayQuery.CandidatePrimitiveIndex();
        hitContext.barycentrics = rayQuery.CandidateTriangleBarycentrics();
        hitContext.isFrontFace = rayQuery.CandidateTriangleFrontFace();

        uint res = UNIFIED_RT_ANYHIT_FUNC(hitContext, payload);

        if (res != UnifiedRT::kIgnoreHit)
            rayQuery.CommitNonOpaqueTriangleHit();

        if (res == UnifiedRT::kAcceptHitAndEndSearch)
            rayQuery.Abort();

    }
 #else
    RayQuery rayQuery;
    rayQuery.Init(dispatchInfo.globalThreadIndex, dispatchInfo.localThreadIndex, accelStruct, rayFlags | UnifiedRT::kRayFlagForceOpaque, instanceMask, ray);
    rayQuery.Proceed();
 #endif

#ifdef UNIFIED_RT_CLOSESTHIT_FUNC
    if (!(rayFlags & kRayFlagSkipClosestHit) && rayQuery.CommittedStatus() == kCommittedTriangleHit)
    {
        HitContext hitContext;
        hitContext.worldRayOrigin = rayQuery.WorldRayOrigin();
        hitContext.worldRayDirection = rayQuery.WorldRayDirection();
        hitContext.tmin = rayQuery.RayTMin();
        hitContext.tcurrent = rayQuery.CommittedRayT();
        hitContext.instanceID = rayQuery.CommittedInstanceID();
        hitContext.primitiveIndex = rayQuery.CommittedPrimitiveIndex();
        hitContext.barycentrics = rayQuery.CommittedTriangleBarycentrics();
        hitContext.isFrontFace = rayQuery.CommittedTriangleFrontFace();

        UNIFIED_RT_CLOSESTHIT_FUNC(hitContext, payload);
    }
#endif

#ifdef UNIFIED_RT_MISS_FUNC
    if (rayQuery.CommittedStatus() == kCommittedNothing)
    {
        HitContext hitContext = (HitContext)0;
        hitContext.worldRayOrigin = rayQuery.WorldRayOrigin();
        hitContext.worldRayDirection = rayQuery.WorldRayDirection();
        hitContext.tmin = rayQuery.RayTMin();

        UNIFIED_RT_MISS_FUNC(hitContext, payload);
    }
#endif

}

#endif

} // namespace UnifiedRT

#endif // UNIFIEDRAYTRACING_TRACERAY_HLSL
