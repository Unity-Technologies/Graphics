#ifndef _PATHTRACING_PATHTRACINGCOMMON_HLSL_
#define _PATHTRACING_PATHTRACINGCOMMON_HLSL_

#pragma warning(error: 3206) // Implicit truncation of vector type

#include "Packages/com.unity.render-pipelines.core/Runtime/Sampling/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/CommonStructs.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/FetchGeometry.hlsl"

#define LIGHT_SAMPLING 0
#define BRDF_SAMPLING 1
#define MIS 2

// Force uniform sampling of the skybox for debugging / ground truth generation
//#define UNIFORM_ENVSAMPLING

// Emissive mesh sampling: we combine explicit light and brdf sampling using MIS.
// We can disable MIS and use exclusively one of the two sampling techniques with the EMISSIVE_SAMPLING define.
#ifndef EMISSIVE_SAMPLING
#define EMISSIVE_SAMPLING BRDF_SAMPLING
#endif

#define SPOT_LIGHT 0
#define DIRECTIONAL_LIGHT 1
#define POINT_LIGHT 2
#define RECTANGULAR_LIGHT 3
#define DISC_LIGHT 4
#define PYRAMID_LIGHT 5
#define BOX_LIGHT 6
#define EMISSIVE_MESH 8 // Must match the variable in UnityEngine.PathTracing.Core.World
#define ENVIRONMENT_LIGHT 9 // Must match the variable in UnityEngine.PathTracing.Core.World

#define LIGHT_PICKING_METHOD_UNIFORM 0
#define LIGHT_PICKING_METHOD_RESERVOIR_GRID 1
#define LIGHT_PICKING_METHOD_LIGHT_GRID 2

#define DIRECT_RAY_VIS_MASK 1u
#define INDIRECT_RAY_VIS_MASK 2u
#define SHADOW_RAY_VIS_MASK 4u
// warning: bits 8u and 16u are used for lightmap LODs

#define MAX_TRANSMISSION_BOUNCES 6


struct HitEntry
{
    uint instanceID;
    uint primitiveIndex;
    float2 barycentrics;
    bool IsValid()
    {
        return instanceID != -1;
    }
};

struct PixelState
{
    float3 radiance;
    float3 avgLightingDir;
    float2 coord;
    float2 motionVector;
    float3 normal;
    uint2  launchIndex;
    uint2  launchDim;
    float  depth;
    int    bounces;
};

struct PTLight
{
    float3  position;
    int     type;
    float3  intensity;
    int     castsShadows;
    float3  forward;
    int     contributesToDirectLighting;
    float4  attenuation;
    float3  up;
    float   width;
    float3  right;
    float   height;
    uint    layerMask;
    float   indirectScale;
    float2  spotAngle;
    float   range;
    int     shadowMaskChannel;
    int     falloffIndex;
    float   shadowRadius;
    int     cookieIndex;
};

int GetMeshLightInstanceID(PTLight light)
{
    return light.height;
}

float4 GetColumn(float4x4 mat, int colIndex)
{
    return float4(mat[0][3], mat[1][3], mat[2][3], mat[3][3]);
}

// Photometric RGB -> (Perceived) Luminance / digital ITU BT.709 (https://www.itu.int/rec/R-REC-BT.709)
float Luminance(float3 color)
{
    const float3 weights = float3(0.2126f, 0.7152f, 0.0722f);
    return dot(color, weights);
}

float3 EvalDiffuseBrdf(float3 albedo)
{
    return albedo / PI;
}

float ClampedCosine(float3 normal, float3 direction)
{
    return saturate(dot(normal, direction));
}

struct PTHitGeom
{
    float3 worldPosition;
    float3 lastWorldPosition;
    float3 worldNormal;
    float3 worldFaceNormal;
    float2 uv0;
    float2 uv1;
    float  triangleArea;
    uint   renderingLayerMask;

    void FixNormals(float3 rayDirection)
    {
        worldFaceNormal = dot(rayDirection, worldFaceNormal) >= 0 ? -worldFaceNormal : worldFaceNormal;
        worldNormal = dot(worldNormal, worldFaceNormal) < 0 ? -worldNormal : worldNormal;
    }

    float3 NextRayOrigin()
    {
        return OffsetRayOrigin(worldPosition, worldFaceNormal);
    }

    float3 NextTransmissionRayOrigin()
    {
        return OffsetRayOrigin(worldPosition, -worldFaceNormal);
    }
};

PTHitGeom GetHitGeomInfo(UnifiedRT::InstanceData instanceInfo, UnifiedRT::Hit hit)
{
    UnifiedRT::HitGeomAttributes attributes = UnifiedRT::FetchHitGeomAttributes(hit);

    PTHitGeom res;
    res.worldPosition = mul(float4(attributes.position, 1), instanceInfo.localToWorld).xyz;
    res.lastWorldPosition = mul(float4(attributes.position, 1), instanceInfo.previousLocalToWorld);
    res.worldNormal = normalize(mul((float3x3)instanceInfo.localToWorldNormals, attributes.normal));
    res.worldFaceNormal = mul((float3x3)instanceInfo.localToWorldNormals, attributes.faceNormal);
    res.uv0 = attributes.uv0.xy;
    res.uv1 = attributes.uv1.xy;
    res.renderingLayerMask = instanceInfo.renderingLayerMask;

    // compute the area of the hit point (used for MIS)
    float l = length(res.worldFaceNormal);
    res.triangleArea = 0.5 * instanceInfo.localToWorldDeterminant * l;

    // normalize the face normal
    if (l > 0)
        res.worldFaceNormal /= l;

    return res;
}

void FetchGeomAttributes(UnifiedRT::Hit hit, int geometryIndex, out float3 position, out float3 normal, out float3 faceNormal)
{
    position = normal = faceNormal = 0.0f;
    // hit.instanceID is set to be the same as the submeshIndex
    // geometryIndex always points to the first submesh, the others submeshes follow in order in the geometry array:
    // submeshGeomIndex = geometryIndex + randomUVHit.instanceID
    if (!hit.IsValid())
        return;
    UnifiedRT::HitGeomAttributes hitAttribs = UnifiedRT::FetchHitGeomAttributes(geometryIndex + hit.instanceID, hit.primitiveIndex, hit.uvBarycentrics);
    position = hitAttribs.position;
    normal = hitAttribs.normal;
    faceNormal = hitAttribs.faceNormal;
}

void FetchGeomAttributes(UnifiedRT::Hit hit, int geometryIndex, out float3 position, out float3 normal, out float3 faceNormal, out float2 uv1)
{
    position = normal = faceNormal = 0.0f;
    uv1 = 0.0f;
    // hit.instanceID is set to be the same as the submeshIndex
    // geometryIndex always points to the first submesh, the others submeshes follow in order in the geometry array:
    // submeshGeomIndex = geometryIndex + randomUVHit.instanceID
    if (!hit.IsValid())
        return;
    UnifiedRT::HitGeomAttributes hitAttribs = UnifiedRT::FetchHitGeomAttributes(geometryIndex + hit.instanceID, hit.primitiveIndex, hit.uvBarycentrics);
    position = hitAttribs.position;
    normal = hitAttribs.normal;
    faceNormal = hitAttribs.faceNormal;
    uv1 = hitAttribs.uv1;
}

#endif
