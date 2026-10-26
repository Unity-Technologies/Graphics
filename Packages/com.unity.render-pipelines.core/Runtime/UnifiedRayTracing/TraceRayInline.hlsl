#ifndef _UNIFIEDRAYTRACING_TRACERAYINLINE_HLSL_
#define _UNIFIEDRAYTRACING_TRACERAYINLINE_HLSL_


#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/RayQuery.hlsl"

#ifndef UNIFIED_RT_PAYLOAD
struct UnifiedRT_EmptyPayload
{
};
#define UNIFIED_RT_PAYLOAD UnifiedRT_EmptyPayload
#endif

namespace UnifiedRT
{

// Field-based HitContext, populated from the RayQuery accessors. Identical on both backends.
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

    float3 WorldRayOrigin()    { return worldRayOrigin; }
    float3 WorldRayDirection() { return worldRayDirection; }
    float3 LocalRayOrigin()    { return localRayOrigin; }
    float3 LocalRayDirection() { return localRayDirection; }
    float RayTMin()            { return tmin; }
    float RayTCurrent()        { return tcurrent; }
    uint InstanceID()          { return instanceID; }
    uint PrimitiveIndex()      { return primitiveIndex; }
    float2 UvBarycentrics()    { return barycentrics; }
    bool IsFrontFace()         { return isFrontFace; }
    uint PrimitiveType()       { return primitiveType; }
};

} // namespace UnifiedRT

// User-provided callbacks. Same HitContext-first convention as TraceRay.hlsl, with 'inout payload' added to the
// intersection callback so users can carry custom procedural data out through the payload.
#ifdef UNIFIED_RT_ANYHIT_FUNC
    uint UNIFIED_RT_ANYHIT_FUNC(UnifiedRT::HitContext hitContext, inout UNIFIED_RT_PAYLOAD payload);
#endif

#ifdef UNIFIED_RT_INTERSECTION_FUNC
    bool UNIFIED_RT_INTERSECTION_FUNC(UnifiedRT::HitContext hitContext, inout UNIFIED_RT_PAYLOAD payload, out float hitT, out float2 uvAttributes);
#endif

// Static (compile-time) ray flags baked into the hardware RayQuery<flags> template. When there are no candidate
// callbacks every geometry can be treated as opaque so a single Proceed() resolves the closest hit.
#if defined(UNIFIED_RT_ANYHIT_FUNC) || defined(UNIFIED_RT_INTERSECTION_FUNC)
    #define UNIFIED_RT_INLINE_STATIC_RAYFLAGS UnifiedRT::kRayFlagNone
#else
    #define UNIFIED_RT_INLINE_STATIC_RAYFLAGS UnifiedRT::kRayFlagForceOpaque
#endif

namespace UnifiedRT
{

#pragma warning(disable : 3557) // prevent warning when the "while (rayQuery.Proceed())" loop is unrolled
#pragma warning(disable : 4000) // suppress FXC warnings about potentially uninitialized variables

Hit TraceRayInline(uint globalThreadIndex, uint localThreadIndex, RayTracingAccelStruct accelStruct, uint instanceMask, Ray ray, uint rayFlags, inout UNIFIED_RT_PAYLOAD payload)
{
    float2 proceduralHitUvAttributes = 0;

    UNIFIED_RT_RAY_QUERY_TYPE(UNIFIED_RT_INLINE_STATIC_RAYFLAGS) rayQuery;
    UNIFIED_RT_INIT_RAY_QUERY(UNIFIED_RT_INLINE_STATIC_RAYFLAGS, rayQuery, globalThreadIndex, localThreadIndex, accelStruct, rayFlags, instanceMask, ray);

#if defined(UNIFIED_RT_ANYHIT_FUNC) || defined(UNIFIED_RT_INTERSECTION_FUNC)
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
        hitContext.localRayOrigin = rayQuery.CandidateObjectRayOrigin();
        hitContext.localRayDirection = rayQuery.CandidateObjectRayDirection();
        hitContext.tmin = rayQuery.RayTMin();
        hitContext.instanceID = rayQuery.CandidateInstanceID();
        hitContext.primitiveIndex = rayQuery.CandidatePrimitiveIndex();
        hitContext.barycentrics = rayQuery.CandidateTriangleBarycentrics();
        hitContext.isFrontFace = UNIFIED_RT_CANDIDATE_TRIANGLE_FRONTFACE(rayQuery);
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
            #if defined(UNIFIED_RT_INTERSECTION_FUNC)
            hitContext.tcurrent = rayQuery.CommittedRayT();
            float hitT;
            float2 hitUv;
            if (UNIFIED_RT_INTERSECTION_FUNC(hitContext, payload, hitT, hitUv))
            {
                proceduralHitUvAttributes = hitUv;
                rayQuery.CommitProceduralPrimitiveHit(hitT);
            }
            #endif
        }
    }
#else
    rayQuery.Proceed();
#endif

    Hit hit = Hit::Invalid();

    uint committedStatus = rayQuery.CommittedStatus();
    if (committedStatus != kCommittedNothing)
    {
        hit.instanceID = rayQuery.CommittedInstanceID();
        hit.primitiveIndex = rayQuery.CommittedPrimitiveIndex();
        hit.uvBarycentrics = (committedStatus == kCommittedProceduralHit) ? proceduralHitUvAttributes : rayQuery.CommittedTriangleBarycentrics();
        hit.hitDistance = rayQuery.CommittedRayT();
        hit.isFrontFace = rayQuery.CommittedTriangleFrontFace();
    }

    return hit;
}

Hit TraceRayInline(uint globalThreadIndex, uint localThreadIndex, RayTracingAccelStruct accelStruct, uint instanceMask, Ray ray, uint rayFlags)
{
    UNIFIED_RT_PAYLOAD dummyPayload = (UNIFIED_RT_PAYLOAD) 0;
    return TraceRayInline(globalThreadIndex, localThreadIndex, accelStruct, instanceMask, ray, rayFlags, dummyPayload);
}

} // namespace UnifiedRT


#endif // _UNIFIEDRAYTRACING_TRACERAYINLINE_HLSL_
