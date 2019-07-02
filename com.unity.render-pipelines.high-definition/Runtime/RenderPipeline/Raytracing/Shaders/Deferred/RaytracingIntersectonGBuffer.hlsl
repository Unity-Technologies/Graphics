#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"

#ifndef GBufferType0
#undef GBufferType0
#define GBufferType0 float4
#endif

#ifndef GBufferType1
#undef GBufferType1
#define GBufferType1 float4
#endif

#ifndef GBufferType2
#undef GBufferType2
#define GBufferType2 float4
#endif

#ifndef GBufferType3
#undef GBufferType3
#define GBufferType3 float4
#endif

#ifndef GBufferType4
#undef GBufferType4
#define GBufferType4 float4
#endif

#ifndef GBufferType5
#undef GBufferType5
#define GBufferType5 float4
#endif

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