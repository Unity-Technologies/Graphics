#ifndef CAPSULE_OCCLUSION_DATA
#define CAPSULE_OCCLUSION_DATA
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/CapsuleShadows/EllipsoidOccluder.cs.hlsl"

// StructuredBuffer<OrientedBBox> _CapsuleOccludersBounds;
StructuredBuffer<EllipsoidOccluderData> _CapsuleOccludersDatas;

EllipsoidOccluderData FetchEllipsoidOccluderData(uint index)
{
    return _CapsuleOccludersDatas[index];
}

// --------------------------------------------
// Occluder data helpers
// --------------------------------------------
float3 GetOccluderPositionRWS(EllipsoidOccluderData data)
{
    return data.positionRWS_radius.xyz;
}

float GetOccluderRadius(EllipsoidOccluderData data)
{
    return data.positionRWS_radius.w;
}

float3 GetOccluderDirectionWS(EllipsoidOccluderData data)
{
    return normalize(data.directionWS_influence.xyz);
}

float GetOccluderScaling(EllipsoidOccluderData data)
{
    return length(data.directionWS_influence.xyz);
}

float GetOccluderInfluenceRadiusWS(EllipsoidOccluderData data)
{
    return data.directionWS_influence.w;
}


// --------------------------------------------
// Data preparation functions
// --------------------------------------------
float3x3 GetRelativeMatrix(EllipsoidOccluderData data)
{
    float zMagnitude = data.positionRWS_radius.w / length(data.directionWS_influence.xyz);
    float3 centerSphere = data.positionRWS_radius.xyz;

    float3 zAxis = normalize(data.directionWS_influence.xyz) * zMagnitude;
    float3 yAxis = normalize(cross(normalize(zAxis), float3(0, 1, 0)));
    float3 xAxis = normalize(cross(normalize(zAxis), yAxis));

    return float3x3(xAxis, yAxis, zAxis);
}

float4 GetDataForSphereIntersection(EllipsoidOccluderData data, float3 positionWS)
{
    // TODO : Fill with transformations needed so the rest of the code deals with simple spheres.
    // xyz should be un-normalized direction, w should contain the length.
    float3 dir = positionWS - data.positionRWS_radius.xyz;
    float3 TransformVec = mul(GetRelativeMatrix(data), dir);
    float len = length(TransformVec);
    return float4(dir.x, dir.y, dir.z, len);
}

void ComputeDirectionAndDistanceFromStartAndEnd(float3 start, float3 end, out float3 direction, out float dist)
{
    direction = end - start;
    dist = length(direction);
    direction = (dist > 1e-5f) ? (direction / dist) : float3(0.0f, 1.0f, 0.0f);
}

float ComputeInfluenceFalloff(float dist, float influenceRadius)
{
    return smoothstep(1.0f, 0.5f, dist / influenceRadius);
}

float ApplyInfluenceFalloff(float occlusion, float influenceFalloff)
{
    return lerp(1.0f, occlusion, influenceFalloff);
}

// Returns pos/radius of occluder as sphere relative to positionWS
float4 TransformOccluder(float3 positionWS, EllipsoidOccluderData data)
{
    float3 dir = GetOccluderDirectionWS(data);
    float3 toOccluder = GetOccluderPositionRWS(data) - positionWS;
    float proj = dot(toOccluder, dir);
    float3 toOccluderCS = toOccluder - (proj * dir) + proj * dir * (GetOccluderRadius(data) * 2.0 / GetOccluderScaling(data));
    return float4(toOccluderCS, GetOccluderRadius(data));
}


#endif
