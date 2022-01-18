#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"

// Structure that defines the current state of the intersection
struct RayIntersectionDynGI
{
    float t;
    float3 albedo;
    float3 normalWS;
};
