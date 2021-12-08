#ifndef RAY_TRACING_PROCEDURAL
#define RAY_TRACING_PROCEDURAL

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl"

// Structure that defines the layout of the AABB info of each individual primitive (Object space)
struct AABB
{
    float3 minPosOS;
    float3 maxPosOS;
};

// Buffer that holds all the axis oriented bounding boxes of the current instance
StructuredBuffer<AABB> aabbBuffer;

AABB FetchPrimitiveAABB(uint primitiveIndex)
{
	return aabbBuffer[primitiveIndex];
}

[shader("intersection")]
void IntersectionShader()
{
    UNITY_XR_ASSIGN_VIEW_INDEX(DispatchRaysIndex().z);
    
    // Grab the current particle's AABB
    AABB particleAABB = FetchPrimitiveAABB(PrimitiveIndex());

    // Compute the center and extent
    float3 centerOS = (particleAABB.minPosOS + particleAABB.maxPosOS) * 0.5f;
    float3 extents = particleAABB.maxPosOS - particleAABB.minPosOS;

#if defined(RAY_TRACING_BOX_PRIMITIVE)
    // Intersect the box and fill the intersection data
    float entryP = 0;
    float exitP = 0;
    if (IntersectRayAABB(ObjectRayOrigin(), ObjectRayDirection(), particleAABB.minPosOS, particleAABB.maxPosOS, 0.0, FLT_MAX, entryP, exitP))
    {
        AttributeData attributeData;
        attributeData.barycentrics = float2(0.5, 0.5);
        ReportHit(entryP, 0, attributeData);
    }
#endif

#if defined(RAY_TRACING_SPHERE_PRIMITIVE)
    float3 sphereCenter = (particleAABB.maxPosOS + particleAABB.minPosOS) * 0.5f;
    float sphereRadius = (particleAABB.maxPosOS.x - particleAABB.minPosOS.x) * 0.5;

    // Intersect the box and fill the intersection data
    float2 intersection = 0;
    if (IntersectRaySphere(ObjectRayOrigin() - sphereCenter, ObjectRayDirection(), sphereRadius, intersection))
    {
        AttributeData attributeData;
        attributeData.barycentrics = float2(0.5, 0.5);
        ReportHit(intersection.x, 0, attributeData);
    }
#endif
}
#endif // RAY_TRACING_PROCEDURAL
