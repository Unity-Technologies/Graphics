#ifndef UNITY_RAY_TRACING_INTERSECTION_SUBSURFACE_INCLUDED
#define UNITY_RAY_TRACING_INTERSECTION_SUBSURFACE_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"

// Structure that defines the current state of the intersection
struct RayIntersectionSubSurface
{
    // Distance of the intersection
    float t;
    // Origin of the current ray
    float3 outNormal;
    // Indirect diffuse at the intersected point
    float3 outIndirectDiffuse;
    // Pixel coordinate matching this ray path
    uint2 pixelCoord;
    // Cone representation of the ray
    RayCone cone;
};

#endif // UNITY_RAY_TRACING_INTERSECTION_SUBSURFACE_INCLUDED
