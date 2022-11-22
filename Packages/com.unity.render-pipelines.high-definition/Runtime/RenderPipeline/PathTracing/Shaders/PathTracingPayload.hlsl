#ifndef UNITY_PATH_TRACING_PAYLOAD_INCLUDED
#define UNITY_PATH_TRACING_PAYLOAD_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"

//
// Segment ID
//
// Identifies segments (or rays) along our path:
//   0:                        Camera ray
//   1 - SEGMENT_ID_MAX_DEPTH: Continuation ray (ID == depth)
//   SEGMENT_ID_TRANSMISSION:  Transmission (or Shadow) ray
//   SEGMENT_ID_NEAREST_HIT:   Nearest surface hit query ray
//   SEGMENT_ID_RANDOM_WALK:   Random walk ray (used in SSS)
//
#define SEGMENT_ID_TRANSMISSION (UINT_MAX - 0)
#define SEGMENT_ID_NEAREST_HIT  (UINT_MAX - 1)
#define SEGMENT_ID_RANDOM_WALK  (UINT_MAX - 2)
#define SEGMENT_ID_MAX_DEPTH    (UINT_MAX - 3)

// Path Tracing Payload
struct PathPayload
{
    //
    // Input
    //
    uint2   pixelCoord;      // Pixel coordinates from which the path emanates
    uint    segmentID;       // Identifier for path segment (see above)

    //
    // Input/output
    //
    float3  throughput;      // Current path throughput
    float   maxRoughness;    // Current maximum roughness encountered along the path
    RayCone cone;            // Ray differential information (not used currently)

    //
    // Output
    //
    float3  value;           // Main value (radiance, or normal for random walk)
    float   alpha;           // Opacity value (computed from transmittance)
    float3  rayOrigin;       // Continuation ray origin
    float3  rayDirection;    // Continuation ray direction, null means no continuation
    float   rayTHit;         // Ray parameter, used either for current or next hit

    //
    // AOV Input/output
    //
    float2  aovMotionVector; // Motion vector (also serve as on/off AOV switch)

    //
    // AOV Output
    //
    float3  aovAlbedo;       // Diffuse reflectance
    float3  aovNormal;       // Shading normal
};

void SetContinuationRay(float3 origin, float3 direction, float tHit, out PathPayload payload)
{
    payload.rayOrigin = origin;
    payload.rayDirection = direction;
    payload.rayTHit = tHit;
}

void GetContinuationRay(PathPayload payload, out RayDesc ray)
{
    ray.Origin = payload.rayOrigin;
    ray.Direction = payload.rayDirection;
    ray.TMin = max(payload.rayTHit - _RayTracingRayBias, 0.0);
    ray.TMax = payload.rayTHit + _RayTracingRayBias;
}

#endif // UNITY_PATH_TRACING_PAYLOAD_INCLUDED
