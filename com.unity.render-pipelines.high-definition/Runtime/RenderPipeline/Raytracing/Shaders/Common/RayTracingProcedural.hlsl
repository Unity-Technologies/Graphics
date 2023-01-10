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

AABB FetchPrimitiveAABB(uint primitiveIndex, uint aabbCount, uint instanceIndex)
{
    return aabbBuffer[primitiveIndex + aabbCount * instanceIndex];
}

bool RayPlaneIntersection(float3 rayOrigin, float3 rayDir, float3 planeOrigin, float3 planeNormal, out float t)
{
    t = dot((planeOrigin - rayOrigin), planeNormal) / dot(rayDir, planeNormal);
    return t > 0.0;
}

bool RayQuadIntersection(float3 rayOrigin, float3 rayDir, float3 planeOrigin, float3 planeNormal, float4x4 localToPrimitive, out float t, out float2 uv)
{
    if(RayPlaneIntersection(rayOrigin, rayDir, planeOrigin, planeNormal, t))
    {
        // Compute the intersection point
        float3 hitPoint = rayOrigin + rayDir * t;

        // Evaluate the projected UVs
        uv = mul(localToPrimitive, float4(hitPoint, 1.0f)).xy + 0.5f;
        return !(any(uv < 0.0f) || any(uv > 1.0f));
    }
    return false;
}

float sign(float2 p1, float2 p2, float2 p3)
{
    return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
}

bool PointInTriangle(float2 pt, float2 v1, float2 v2, float2 v3)
{
    float d1, d2, d3;
    bool has_neg, has_pos;

    d1 = sign(pt, v1, v2);
    d2 = sign(pt, v2, v3);
    d3 = sign(pt, v3, v1);

    has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
    has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

    return !(has_neg && has_pos);
}

bool RayTriangleIntersection(float3 rayOrigin, float3 rayDir, float3 planeOrigin, float3 planeNormal, float2 p0, float2 p1, float2 p2, float4x4 localToPrimitive, out float t, out float2 uv)
{
    if(RayPlaneIntersection(rayOrigin, rayDir, planeOrigin, planeNormal, t))
    {
        // Compute the intersection point
        float3 hitPoint = rayOrigin + rayDir * t;

        // Evaluate the projected UVs
        float2 pos2D = mul(localToPrimitive, float4(hitPoint, 1.0f)).xy + 0.5;
        uv = pos2D;
        return PointInTriangle(pos2D, p0, p1, p2);
    }
    return false;
}

bool RayDiskIntersection(float3 rayOrigin, float3 rayDir, float3 planeOrigin, float3 planeNormal, float4x4 localToPrimitive, out float t, out float2 uv)
{
    if(RayPlaneIntersection(rayOrigin, rayDir, planeOrigin, planeNormal, t))
    {
        // Compute the intersection point
        float3 hitPoint = rayOrigin + rayDir * t;

        // Evaluate the projected UVs
        uv = mul(localToPrimitive, float4(hitPoint, 1.0f)).xy + 0.5;
        return length(uv - 0.5) < 0.5;
    }
    return false;
}

void IntersectPrimitive(RayTracingProceduralData rtProceduralData)
{
#if (SHADERPASS == SHADERPASS_RAYTRACING_DEBUG)
    AttributeData attributeData;
    ZERO_INITIALIZE(AttributeData, attributeData);
    ReportHit(1.0f, 0, attributeData);
#else

#if defined(RAY_TRACING_QUAD_PRIMTIIVE)
    float t;
    float2 uv;
    if (RayQuadIntersection(ObjectRayOrigin(), ObjectRayDirection(), rtProceduralData.position, rtProceduralData.normal, rtProceduralData.objectToPrimitive, t, uv))
    {
        AttributeData attributeData;
        attributeData.barycentrics = uv;

        if(AABBPrimitiveIsVisible(rtProceduralData, uv))
            ReportHit(t, 0, attributeData);
    }
#endif

#if defined(RAY_TRACING_TRIANGLE_PRIMTIIVE)
    float t;
    float2 uv;
    if (RayTriangleIntersection(ObjectRayOrigin(), ObjectRayDirection(), rtProceduralData.position, rtProceduralData.normal, rtProceduralData.p0, rtProceduralData.p1, rtProceduralData.p2, rtProceduralData.objectToPrimitive, t, uv))
    {
        AttributeData attributeData;
        attributeData.barycentrics = uv;

        if(AABBPrimitiveIsVisible(rtProceduralData, uv))
            ReportHit(t, 0, attributeData);
    }
#endif

#if defined(RAY_TRACING_DISK_PRIMTIIVE)
    float t;
    float2 uv;
    if (RayDiskIntersection(ObjectRayOrigin(), ObjectRayDirection(), rtProceduralData.position, rtProceduralData.normal, rtProceduralData.objectToPrimitive, t, uv))
    {
        AttributeData attributeData;
        attributeData.barycentrics = uv;

        if(AABBPrimitiveIsVisible(rtProceduralData, uv))
            ReportHit(t, 0, attributeData);
    }
#endif
#endif
}
#endif // RAY_TRACING_PROCEDURAL
