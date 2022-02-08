#ifndef UNITY_PATH_TRACING_INTERSECTION_INCLUDED
#define UNITY_PATH_TRACING_INTERSECTION_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"

//
// Segment ID
//
// Identifies segments (or rays) along our path:
//   0:                        Camera ray
//   1 - SEGMENT_ID_MAX_DEPTH: Continuation ray (ID == depth)
//   SEGMENT_ID_TRANSMISSION:  Transmission (or Shadow) ray
//   SEGMENT_ID_RANDOM_WALK:   Random walk ray (used in SSS)
//
#define SEGMENT_ID_MAX_DEPTH    10
#define SEGMENT_ID_TRANSMISSION (~0 - 0)
#define SEGMENT_ID_RANDOM_WALK  (~0 - 1)

// Structure that defines the current state of the intersection, for path tracing
struct PathIntersection
{
    //
    // Input/output
    //
    float3  throughput;   // Current path throughput
    float   maxRoughness; // Current maximum roughness encountered along the path
    RayCone cone;         // Ray differential information (not used currently)

    //
    // Output
    //
    float3  value;        // Main value (radiance, or normal for random walk)
    float3  rayOrigin;    // Continuation ray origin (aliased with couple inputs)
    float3  rayDirection; // Continuation ray direction, null means no continuation
    float   rayTHit;      // Ray parameter, used either for current or next hit
};

void GetContinuationRay(PathIntersection pathIntersection, out RayDesc ray)
{
    ray.Origin = pathIntersection.rayOrigin;
    ray.Direction = pathIntersection.rayDirection;
    ray.TMin = pathIntersection.rayTHit - _RaytracingRayBias;
    ray.TMax = pathIntersection.rayTHit + _RaytracingRayBias;
}

void SetPixelCoordinates(uint2 pixelCoord, inout PathIntersection pathIntersection)
{
    pathIntersection.rayOrigin.xy = asfloat(pixelCoord);
}

uint2 GetPixelCoordinates(PathIntersection pathIntersection)
{
    return asuint(pathIntersection.rayOrigin.xy);
}

void SetSegmentID(uint segmentID, inout PathIntersection pathIntersection)
{
    pathIntersection.rayOrigin.z = asfloat(segmentID);
}

uint GetSegmentID(PathIntersection pathIntersection)
{
    return asuint(pathIntersection.rayOrigin.z);
}

#endif // UNITY_PATH_TRACING_INTERSECTION_INCLUDED
