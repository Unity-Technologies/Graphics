#ifndef GEO_UTILITIES_H
#define GEO_UTILITIES_H

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

struct CylinderBound
{
    float3 center;
    float3 axis;
    float halfHeight;
    float capRadius;
};

struct SphereBound
{
    float3 center;
    float radius;
};

float CompleteSinCos(float sinOrCos)
{
    return sqrt(max(1.0f - sinOrCos*sinOrCos, 0.0f));
}

float2 ProjectedHalfLengths(CylinderBound cylinder, float3 planeNormal)
{
    float absCosTheta = abs(dot(planeNormal, cylinder.axis));
    float sinTheta = CompleteSinCos(absCosTheta);

    float h = cylinder.halfHeight;
    float r = cylinder.capRadius;

    float halfLengthAlongNormal = absCosTheta * h + sinTheta * r;
    float halfLengthInPlane = max(sinTheta * h + absCosTheta * r, r); // ellipse, so use max of two axis lengths

    return float2(halfLengthInPlane, halfLengthAlongNormal);
}

struct BoundingObjectData
{
    float3 frontCenterPosRWS;
    float2 centerPosNDC;
    float2 radialPosNDC;
};

BoundingObjectData CalculateBoundingObjectData(SphereBound boundingSphere,
    float4x4 viewProjMatrix,
    float4 viewOriginWorldSpace,
    float4 radialDirWorldSpace,
    float4 facingDirWorldSpace)
{
    const float3 centerPosRWS = boundingSphere.center - viewOriginWorldSpace.xyz;

    const float3 radialVec = abs(boundingSphere.radius) * radialDirWorldSpace.xyz;
    const float3 facingVec = abs(boundingSphere.radius) * facingDirWorldSpace.xyz;

    BoundingObjectData data;
    data.centerPosNDC = ComputeNormalizedDeviceCoordinates(centerPosRWS, viewProjMatrix);
    data.radialPosNDC = ComputeNormalizedDeviceCoordinates(centerPosRWS + radialVec, viewProjMatrix);
    data.frontCenterPosRWS = centerPosRWS + facingVec;
    return data;
}

BoundingObjectData CalculateBoundingObjectData(CylinderBound cylinderBound,
    float4x4 viewProjMatrix,
    float4 viewOriginWorldSpace,
    float4 radialDirWorldSpace,
    float4 facingDirWorldSpace)
{
    const float3 centerPosRWS = cylinderBound.center - viewOriginWorldSpace.xyz;

    const float2 halfLengths = ProjectedHalfLengths(cylinderBound, facingDirWorldSpace.xyz);
    const float3 radialVec = halfLengths.x * radialDirWorldSpace.xyz;

    BoundingObjectData data;
    data.centerPosNDC = ComputeNormalizedDeviceCoordinates(centerPosRWS, viewProjMatrix);
    data.radialPosNDC = ComputeNormalizedDeviceCoordinates(centerPosRWS + radialVec, viewProjMatrix);
    data.frontCenterPosRWS = centerPosRWS + halfLengths.y * facingDirWorldSpace.xyz;
    return data;
}

#endif
