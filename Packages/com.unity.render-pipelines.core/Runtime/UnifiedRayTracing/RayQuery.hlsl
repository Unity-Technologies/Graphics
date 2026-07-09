#ifndef _UNIFIEDRAYTRACING_RAYQUERY_HLSL_
#define _UNIFIEDRAYTRACING_RAYQUERY_HLSL_

#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Bindings.hlsl"

#if defined(UNIFIED_RT_BACKEND_HARDWARE)
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Hardware/RayQueryHardware.hlsl"
#elif defined(UNIFIED_RT_BACKEND_COMPUTE)
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Compute/RayQuerySoftware.hlsl"
#endif

// Default candidate front face accessor. Backends that don't support querying the triangle front face during
// traversal (see RayQueryHardware.hlsl for PS5) override this macro before this point.
#if !defined(UNIFIED_RT_CANDIDATE_TRIANGLE_FRONTFACE)
#define UNIFIED_RT_CANDIDATE_TRIANGLE_FRONTFACE(rayQuery) (rayQuery.CandidateTriangleFrontFace())
#endif

#endif  // _UNIFIEDRAYTRACING_RAYQUERY_HLSL_
