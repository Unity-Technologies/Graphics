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

void IntersectPrimitive(VFXAttributes attributes, float3 size3)
{
    // Grab the current particle's AABB
    AABB particleAABB = FetchPrimitiveAABB(PrimitiveIndex());

    // These parameters are only required by the quad primitive for now
#if defined(RAY_TRACING_QUAD_PRIMTIIVE)
    // Object <-> primtive matrices
    const float4x4 primtiveToObject = GetElementToVFXMatrix(attributes.axisX, attributes.axisY, attributes.axisZ,
        float3(attributes.angleX,attributes.angleY,attributes.angleZ),
        float3(attributes.pivotX,attributes.pivotY,attributes.pivotZ),
        size3, attributes.position);

    const float4x4 objectToPrimitive = GetVFXToElementMatrix(attributes.axisX, attributes.axisY, attributes.axisZ,
        float3(attributes.angleX, attributes.angleY, attributes.angleZ),
        float3(attributes.pivotX, attributes.pivotY, attributes.pivotZ),
        size3, attributes.position);
#endif

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

#if defined(RAY_TRACING_QUAD_PRIMTIIVE)
    float t;
    float2 uv;
    if (RayQuadIntersection(ObjectRayOrigin(), ObjectRayDirection(), primtiveToObject._m03_m13_m23, -primtiveToObject._m02_m12_m22, objectToPrimitive, t, uv))
    {
        AttributeData attributeData;
        attributeData.barycentrics = uv;

        float alpha = SampleTexture(VFX_SAMPLER(mainTexture), uv).a;
        if(alpha > 0.5f)
            ReportHit(t, 0, attributeData);
    }
#endif

#if defined(RAY_TRACING_SPHERE_PRIMITIVE)
    float3 sphereCenter = (particleAABB.maxPosOS + particleAABB.minPosOS) * 0.5;
    float sphereRadius = (particleAABB.maxPosOS.x - particleAABB.minPosOS.x) * 0.5;

    // Intersect the box and fill the intersection data
    float2 intersection = 0;
    float3 rayStart = ObjectRayOrigin() - sphereCenter;
    float3 rayDir = ObjectRayDirection();
    if (IntersectRaySphere(rayStart, rayDir, sphereRadius, intersection))
    {
        // Pack the normal into the attribute data
        AttributeData attributeData;
        attributeData.barycentrics = PackNormalOctQuadEncode(normalize(rayStart + intersection.x * rayDir));

        // Report the hit
        ReportHit(intersection.x, 0, attributeData);
    }
#endif
}
#endif // RAY_TRACING_PROCEDURAL
