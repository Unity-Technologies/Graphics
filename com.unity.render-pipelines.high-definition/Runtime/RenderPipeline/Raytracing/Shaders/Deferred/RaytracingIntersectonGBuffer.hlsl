#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialGBufferMacros.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/RaytracingMaterialGBufferMacros.hlsl"

struct RaytracingGBuffer
{
    GBufferType0 gbuffer0;
    GBufferType1 gbuffer1;
    GBufferType2 gbuffer2;
    GBufferType3 gbuffer3;
    GBufferType4 gbuffer4;
    GBufferType5 gbuffer5;
};

// Structure that defines the current state of the intersection
struct RayIntersectionGBuffer
{
	// Origin of the current ray
	float3 origin;
	// Direction of the current ray
	float3 incidentDirection;
	// Distance of the intersection
	float t;
	// Cone representation of the ray
	RayCone cone;
	// Gbuffer packed
	RaytracingGBuffer gBufferData;
};
