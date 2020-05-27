#ifndef UNITY_PATH_TRACING_INTERSECTION_INCLUDED
#define UNITY_PATH_TRACING_INTERSECTION_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"

// Structure that defines the current state of the intersection, for path tracing
struct PathIntersection
{
	float t;
	// Resulting value (often color) of the ray
	float3 value;
	// Cone representation of the ray
	RayCone cone;
	// The remaining available depth for the current Ray
	uint remainingDepth;
	// Pixel coordinate from which the initial ray was launched
	uint2 pixelCoord;
	// Max roughness encountered along the path
	float maxRoughness;
};

#endif // UNITY_PATH_TRACING_INTERSECTION_INCLUDED
