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


static const uint kCommittedTriangleHit = 1;
static const uint kCommittedProceduralHit = 2;

float3 _WorldRayOrigin() { return WorldRayOrigin(); }
float3 _WorldRayDirection() { return WorldRayDirection(); }
float3 _LocalRayOrigin() { return ObjectRayOrigin(); }
float3 _LocalRayDirection() { return ObjectRayDirection(); }
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

    float3 LocalRayOrigin()
    {
        return _LocalRayOrigin();
    }

    float3 LocalRayDirection()
    {
        return _LocalRayDirection();
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

    uint PrimitiveType()
    {
        return HitKind() >= HIT_KIND_TRIANGLE_FRONT_FACE ? kCommittedTriangleHit : kCommittedProceduralHit;
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
    float3 localRayOrigin;
    float3 localRayDirection;
    float tmin;
    float tcurrent;
    uint instanceID;
    uint primitiveIndex;
    float2 barycentrics;
    bool isFrontFace;
    uint primitiveType;

    float3 WorldRayOrigin()
    {
        return worldRayOrigin;
    }

    float3 WorldRayDirection()
    {
        return worldRayDirection;
    }

    float3 LocalRayOrigin()
    {
        return localRayOrigin;
    }

    float3 LocalRayDirection()
    {
        return localRayDirection;
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

    uint PrimitiveType()
    {
        return primitiveType;
    }
};

} // namespace UnifiedRT

#ifdef UNIFIED_RT_ANYHIT_FUNC
    uint UNIFIED_RT_ANYHIT_FUNC(UnifiedRT::HitContext hitContext, inout UNIFIED_RT_PAYLOAD payload);
#endif

#ifdef UNIFIED_RT_CLOSESTHIT_FUNC
    #ifdef UNIFIED_RT_ADDITIONAL_INTERSECTION_ATTRIBS
        void UNIFIED_RT_CLOSESTHIT_FUNC(UnifiedRT::HitContext hitContext, inout UNIFIED_RT_PAYLOAD payload, UNIFIED_RT_ADDITIONAL_INTERSECTION_ATTRIBS attributes);
#else
        void UNIFIED_RT_CLOSESTHIT_FUNC(UnifiedRT::HitContext hitContext, inout UNIFIED_RT_PAYLOAD payload);
    #endif
#endif

#ifdef UNIFIED_RT_INTERSECTION_FUNC
    #ifdef UNIFIED_RT_ADDITIONAL_INTERSECTION_ATTRIBS
        bool UNIFIED_RT_INTERSECTION_FUNC(UnifiedRT::HitContext hitContext, out float hitT, out float2 uvAttributes, out UNIFIED_RT_ADDITIONAL_INTERSECTION_ATTRIBS additionalAttributes);
    #else
        bool UNIFIED_RT_INTERSECTION_FUNC(UnifiedRT::HitContext hitContext, out float hitT, out float2 uvAttributes);
    #endif
#endif

namespace UnifiedRT {

#pragma warning(disable : 3557) // prevent warning when the "while (rayQuery.Proceed())" loop is unrolled
#pragma warning(disable : 4000) // suppress FXC warnings about potentially uninitialized variables

void TraceRay(DispatchInfo dispatchInfo, RayTracingAccelStruct accelStruct, uint instanceMask, Ray ray, uint rayFlags, inout UNIFIED_RT_PAYLOAD payload)
{
    float2 proceduralHitUvAttributes = 0;
#ifdef UNIFIED_RT_ADDITIONAL_INTERSECTION_ATTRIBS
    UNIFIED_RT_ADDITIONAL_INTERSECTION_ATTRIBS proceduralAddtionalAttributes = (UNIFIED_RT_ADDITIONAL_INTERSECTION_ATTRIBS)0;
#endif

#if defined(UNIFIED_RT_ANYHIT_FUNC) || defined(UNIFIED_RT_INTERSECTION_FUNC)
    RayQuery rayQuery;
    rayQuery.Init(dispatchInfo.globalThreadIndex, dispatchInfo.localThreadIndex, accelStruct, rayFlags, instanceMask, ray);
    while (rayQuery.Proceed())
    {
        #ifndef UNIFIED_RT_INTERSECTION_FUNC
        // not necessary but makes sure the compiler optimizes the loop out when one of these flags is set
        if (rayFlags & (UnifiedRT::kRayFlagForceOpaque | UnifiedRT::kRayFlagCullNonOpaque))
            break;
        #endif

        HitContext hitContext;
        hitContext.worldRayOrigin = rayQuery.WorldRayOrigin();
        hitContext.worldRayDirection = rayQuery.WorldRayDirection();
        hitContext.localRayOrigin = rayQuery.CandidateLocalRayOrigin();
        hitContext.localRayDirection = rayQuery.CandidateLocalRayDirection();
        hitContext.tmin = rayQuery.RayTMin();
        hitContext.instanceID = rayQuery.CandidateInstanceID();
        hitContext.primitiveIndex = rayQuery.CandidatePrimitiveIndex();
        hitContext.barycentrics = rayQuery.CandidateTriangleBarycentrics();
        hitContext.isFrontFace = rayQuery.CandidateTriangleFrontFace();
        hitContext.primitiveType = rayQuery.CandidateType();

        if (rayQuery.CandidateType() == kCandidateNonOpaqueTriangle)
        {
            #if defined(UNIFIED_RT_ANYHIT_FUNC)
            hitContext.tcurrent = rayQuery.CandidateTriangleRayT();
            uint res = UNIFIED_RT_ANYHIT_FUNC(hitContext, payload);

            if (res != UnifiedRT::kIgnoreHit)
                rayQuery.CommitNonOpaqueTriangleHit();

            if (res == UnifiedRT::kAcceptHitAndEndSearch)
                rayQuery.Abort();
            #endif
        }
        else // kCandidateProceduralPrimitive
        {
            hitContext.tcurrent = rayQuery.CommittedRayT();
            float hitT;
            float2 hitUv;

            #if defined(UNIFIED_RT_INTERSECTION_FUNC) && defined(UNIFIED_RT_ADDITIONAL_INTERSECTION_ATTRIBS)
            UNIFIED_RT_ADDITIONAL_INTERSECTION_ATTRIBS hitAttribs;
            if (UNIFIED_RT_INTERSECTION_FUNC(hitContext, hitT, hitUv, hitAttribs))
            {
                proceduralAddtionalAttributes = hitAttribs;
                proceduralHitUvAttributes = hitUv;
                rayQuery.CommitProceduralPrimitiveHit(hitT);
            }
            #elif defined(UNIFIED_RT_INTERSECTION_FUNC)
            if (UNIFIED_RT_INTERSECTION_FUNC(hitContext, hitT, hitUv))
            {
                proceduralHitUvAttributes = hitUv;
                rayQuery.CommitProceduralPrimitiveHit(hitT);
            }
            #endif
        }
    }
 #else
    RayQuery rayQuery;
    rayQuery.Init(dispatchInfo.globalThreadIndex, dispatchInfo.localThreadIndex, accelStruct, rayFlags | UnifiedRT::kRayFlagForceOpaque, instanceMask, ray);
    rayQuery.Proceed();
 #endif

#ifdef UNIFIED_RT_CLOSESTHIT_FUNC
    if (!(rayFlags & kRayFlagSkipClosestHit) && rayQuery.CommittedStatus() != kCommittedNothing)
    {
        HitContext hitContext;
        hitContext.worldRayOrigin = rayQuery.WorldRayOrigin();
        hitContext.worldRayDirection = rayQuery.WorldRayDirection();
        hitContext.localRayOrigin = rayQuery.CommittedLocalRayOrigin();
        hitContext.localRayDirection = rayQuery.CommittedLocalRayDirection();
        hitContext.tmin = rayQuery.RayTMin();
        hitContext.tcurrent = rayQuery.CommittedRayT();
        hitContext.instanceID = rayQuery.CommittedInstanceID();
        hitContext.primitiveIndex = rayQuery.CommittedPrimitiveIndex();
        hitContext.barycentrics = rayQuery.CommittedStatus() == kCommittedProceduralHit ? proceduralHitUvAttributes : rayQuery.CommittedTriangleBarycentrics();
        hitContext.isFrontFace = rayQuery.CommittedTriangleFrontFace();
        hitContext.primitiveType = rayQuery.CommittedStatus();


#ifdef UNIFIED_RT_ADDITIONAL_INTERSECTION_ATTRIBS
            UNIFIED_RT_CLOSESTHIT_FUNC(hitContext, payload, proceduralAddtionalAttributes);
        #else
            UNIFIED_RT_CLOSESTHIT_FUNC(hitContext, payload);
        #endif
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
