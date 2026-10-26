#ifndef _UNIFIEDRAYTRACING_RAYQUERYHARDWARE_HLSL_
#define _UNIFIEDRAYTRACING_RAYQUERYHARDWARE_HLSL_

#pragma require inlineraytracing

#include <UnityRayQuery.cginc>
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Bindings.hlsl"

#define UNIFIED_RT_RAY_QUERY_TYPE(flags) UnityRayQuery<flags>
#define UNIFIED_RT_INIT_RAY_QUERY(staticRayFlags, rayQuery, globalThreadIndex, localThreadIndex, accelStruct, dynamicRayFlags, instanceMask, ray) rayQuery.TraceRayInline(accelStruct.accelStruct, dynamicRayFlags, instanceMask, UnifiedRT::ConvertToRayDesc(ray))

#if defined(SHADER_API_PS5)
// PS5's inline RayQuery does not implement CandidateTriangleFrontFace() (part of the DXR 1.1 spec), so fall back to a
// default value. Inline ray tracing on PS5 is not feature complete and its Ray Query tests are disabled.
#define UNIFIED_RT_CANDIDATE_TRIANGLE_FRONTFACE(rayQuery) (false)
#endif

namespace UnifiedRT {

static const uint kCommittedNothing = COMMITTED_NOTHING;
static const uint kCommittedTriangleHit = COMMITTED_TRIANGLE_HIT;
static const uint kCommittedProceduralHit = COMMITTED_PROCEDURAL_PRIMITIVE_HIT;

static const uint kCandidateNonOpaqueTriangle = CANDIDATE_NON_OPAQUE_TRIANGLE;
static const uint kCandidateProceduralPrimitive = CANDIDATE_PROCEDURAL_PRIMITIVE;

RayDesc ConvertToRayDesc(Ray ray)
{
    RayDesc rayDesc;
    rayDesc.Origin = ray.origin;
    rayDesc.TMin = ray.tMin;
    rayDesc.Direction = ray.direction;
    rayDesc.TMax = ray.tMax;

    return rayDesc;
}

} // namespace UnifiedRT

#endif  // _UNIFIEDRAYTRACING_RAYQUERYHARDWARE_HLSL_
