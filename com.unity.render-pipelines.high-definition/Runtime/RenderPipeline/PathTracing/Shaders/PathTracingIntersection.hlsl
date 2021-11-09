#ifndef UNITY_PATH_TRACING_INTERSECTION_INCLUDED
#define UNITY_PATH_TRACING_INTERSECTION_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"

// Structure that defines the current state of the intersection, for path tracing
struct PathIntersection
{
    // t as in: O + t*D = H (i.e. distance between O and H, if D is normalized)
    float t;
    // Resulting value (often color) of the ray
    float3 value;
    // Resulting alpha (camera rays only)
    float alpha;
    // Cone representation of the ray
    RayCone cone;
    // The remaining available depth for the current ray
    uint remainingDepth;
    // Pixel coordinate from which the initial ray was launched
    uint2 pixelCoord;
    // Max roughness encountered along the path
    float maxRoughness;
};

int GetCurrentDepth(PathIntersection pathIntersection)
{
    return _RaytracingMaxRecursion - pathIntersection.remainingDepth;
}

// Helper functions to read and write AOV values in the payload, hijacking other existing member variables
void SetAlbedo(inout PathIntersection payload, float3 albedo)
{
    payload.cone.width = albedo.x;
    payload.cone.spreadAngle = albedo.y;
    payload.remainingDepth = asint(albedo.z);
}

float3 GetAlbedo(in PathIntersection payload)
{
    return float3(payload.cone.width, payload.cone.spreadAngle, asfloat(payload.remainingDepth));
}

void SetNormal(inout PathIntersection payload, float3 normal)
{
    payload.pixelCoord.x = asint(normal.x);
    payload.pixelCoord.y = asint(normal.y);
    payload.maxRoughness = normal.z;
}

float3 GetNormal(inout PathIntersection payload)
{
    return float3(asfloat(payload.pixelCoord.x), asfloat(payload.pixelCoord.y), payload.maxRoughness);
}

void ClearAOVData(inout PathIntersection payload)
{
    SetAlbedo(payload, 0.0);
    SetNormal(payload, 0.0);
}

#endif // UNITY_PATH_TRACING_INTERSECTION_INCLUDED
