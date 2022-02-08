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
    // Input
    //
    uint2   pixelCoord;   // Pixel coordinates from which the path emanates
    uint    segmentID;    // Identifier for path segment (see above)

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
    float3  rayDirection; // Continuation ray direction, null means no continuation
    float   rayTHit;      // Ray parameter, used either for current or next hit
};

void SetContinuationRayOrigin(float3 origin, out PathIntersection pathIntersection)
{
    // Alias inputs we don't need at that stage
    pathIntersection.pixelCoord = asuint(origin.xy);
    pathIntersection.segmentID = asuint(origin.z);
}

float3 GetContinuationRayOrigin(PathIntersection pathIntersection)
{
    // Alias inputs we don't need at that stage
    return float3(asfloat(pathIntersection.pixelCoord),
                  asfloat(pathIntersection.segmentID));
}

void SetContinuationRay(float3 origin, float3 direction, float tHit, out PathIntersection pathIntersection)
{
    SetContinuationRayOrigin(origin, pathIntersection);
    pathIntersection.rayDirection = direction;
    pathIntersection.rayTHit = tHit;
}

void GetContinuationRay(PathIntersection pathIntersection, out RayDesc ray)
{
    ray.Origin = GetContinuationRayOrigin(pathIntersection);
    ray.Direction = pathIntersection.rayDirection;
    if (pathIntersection.rayTHit > 0.0)
    {
        ray.TMin = pathIntersection.rayTHit - _RaytracingRayBias;
        ray.TMax = pathIntersection.rayTHit + _RaytracingRayBias;
    }
    else // Use default values
    {
        ray.TMin = 0.0;
        ray.TMax = FLT_INF;
    }
}

#endif // UNITY_PATH_TRACING_INTERSECTION_INCLUDED
