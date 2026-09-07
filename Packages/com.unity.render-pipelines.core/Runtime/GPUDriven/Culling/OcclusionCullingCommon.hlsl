#ifndef _OCCLUSION_CULLING_COMMON_H
#define _OCCLUSION_CULLING_COMMON_H

// If using this the shader should add
// #pragma multi_compile _ OCCLUSION_DEBUG
// before including this file

#include "Packages/com.unity.render-pipelines.core/Runtime/GPUDriven/Culling/OcclusionCullingCommon.cs.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/GPUDriven/Culling/OcclusionCullingCommonShaderVariables.cs.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/GPUDriven/Culling/OcclusionTestCommon.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/GPUDriven/Utilities/GeometryUtilities.hlsl"

#define OCCLUSION_ENABLE_GATHER_TRIM 1

TEXTURE2D(_OccluderDepthPyramid);
SAMPLER(s_linear_clamp_sampler);

#ifdef OCCLUSION_DEBUG
RWStructuredBuffer<uint> _OcclusionDebugOverlay;

uint OcclusionDebugOverlayOffset(uint2 coord)
{
    return OCCLUSIONCULLINGCOMMONCONFIG_DEBUG_PYRAMID_OFFSET + coord.x + _OccluderMipLayoutSizeX * coord.y;
}
#endif

bool IsOcclusionVisibleAtMip(float queryClosestDepth, float2 centerCoordInTopMip, int mipLevel, int subviewIndex)
{
    bool isVisible = true;

    // scale our coordinate to this mip
    float2 centerCoordInChosenMip = ldexp(centerCoordInTopMip, -mipLevel);
    int4 mipBounds = _OccluderMipBounds[mipLevel];
    mipBounds.y += subviewIndex * _OccluderMipLayoutSizeY;

    if ((_OcclusionTestDebugFlags & OCCLUSIONTESTDEBUGFLAG_ALWAYS_PASS) == 0)
    {
        // gather4 occluder depths to cover this footprint
        float2 gatherUv = (float2(mipBounds.xy) + clamp(centerCoordInChosenMip, .5f, float2(mipBounds.zw) - .5f)) * _OccluderDepthPyramidSize.zw;
        float4 gatherDepths = GATHER_TEXTURE2D(_OccluderDepthPyramid, s_linear_clamp_sampler, gatherUv);
        float occluderDepth = FarthestDepth(gatherDepths);
        isVisible = IsVisibleAfterOcclusion(occluderDepth, queryClosestDepth);
    }
#ifdef OCCLUSION_DEBUG
    // show footprint of gather4 in debug output
    bool countForOverlay = ((_OcclusionTestDebugFlags & OCCLUSIONTESTDEBUGFLAG_COUNT_VISIBLE) != 0);
    if (!isVisible)
        countForOverlay = !countForOverlay;
    if (countForOverlay)
    {
        uint2 debugCoord = mipBounds.xy + uint2(clamp(int2(centerCoordInChosenMip - .5f), 0, mipBounds.zw - 2));
        InterlockedAdd(_OcclusionDebugOverlay[OcclusionDebugOverlayOffset(debugCoord + uint2(0, 0))], 1);
        InterlockedAdd(_OcclusionDebugOverlay[OcclusionDebugOverlayOffset(debugCoord + uint2(1, 0))], 1);
        InterlockedAdd(_OcclusionDebugOverlay[OcclusionDebugOverlayOffset(debugCoord + uint2(0, 1))], 1);
        InterlockedAdd(_OcclusionDebugOverlay[OcclusionDebugOverlayOffset(debugCoord + uint2(1, 1))], 1);

        // accumulate the total in the first slot
        InterlockedAdd(_OcclusionDebugOverlay[0], 1);
    }
#endif

    return isVisible;
}

bool IsOcclusionVisibleRect(float queryClosestDepth, float2 centerPosNDC, float2 halfSizeNDC, int subviewIndex)
{
    float2 centerCoordInTopMip = centerPosNDC * _DepthSizeInOccluderPixels.xy;
    float2 halfSizeInTopMip = halfSizeNDC * _DepthSizeInOccluderPixels.xy;
    float radiusInPixels = max(halfSizeInTopMip.x, halfSizeInTopMip.y);

    // start from the exact-fit mip for the rect's dominant axis
    float mipPartExponent = 0.0f;
    frexp(radiusInPixels, mipPartExponent);
    int mipLevel = max((int) mipPartExponent, 0);

    if (mipLevel < OCCLUSIONCULLINGCOMMONCONFIG_MAX_OCCLUDER_MIPS)
    {
        int4 mipBounds = _OccluderMipBounds[mipLevel];
        float2 centerCoordInMip = ldexp(centerCoordInTopMip, -mipLevel);
        float2 halfSizeInMip = ldexp(halfSizeInTopMip, -mipLevel);
        float2 snappedCenter = clamp(centerCoordInMip, .5f, float2(mipBounds.zw) - .5f);
        float2 footprintMin = floor(snappedCenter - 0.5f);

        // a [1,2) texel rect can straddle 3 texel rows/columns;
        // when the snapped 2x2 gather cannot cover the rect at this mip, go one mip up
        if (any(centerCoordInMip - halfSizeInMip < footprintMin)
            || any(footprintMin + 2.0f < centerCoordInMip + halfSizeInMip))
            mipLevel += 1;
    }

    if (mipLevel >= OCCLUSIONCULLINGCOMMONCONFIG_MAX_OCCLUDER_MIPS)
        return true;

    return IsOcclusionVisibleAtMip(queryClosestDepth, centerCoordInTopMip, mipLevel, subviewIndex);
}

bool IsOcclusionVisible(float3 frontCenterPosRWS, float2 centerPosNDC, float2 radialPosNDC, int subviewIndex)
{
    float queryClosestDepth = ComputeNormalizedDeviceCoordinatesWithZ(frontCenterPosRWS, _ViewProjMatrix[subviewIndex]).z;
    bool isBehindCamera = dot(frontCenterPosRWS, _FacingDirWorldSpace[subviewIndex].xyz) >= 0.f;

    float2 centerCoordInTopMip = centerPosNDC * _DepthSizeInOccluderPixels.xy;
    float radiusInPixels = length((radialPosNDC - centerPosNDC) * _DepthSizeInOccluderPixels.xy);

    // log2 of the radius in pixels for the gather4 mip level
    float mipPartExponent = 0.0f;
    frexp(radiusInPixels, mipPartExponent);
    int mipLevel = max((int) mipPartExponent + 1, 0);
    if (mipLevel >= OCCLUSIONCULLINGCOMMONCONFIG_MAX_OCCLUDER_MIPS || isBehindCamera)
        return true;

    return IsOcclusionVisibleAtMip(queryClosestDepth, centerCoordInTopMip, mipLevel, subviewIndex);
}

bool IsOcclusionVisibleOBB(float3 boundsCenter, float3 boundsExtents, float4x4 localToWorld, int subviewIndex)
{
    // world-space OBB relative to the view origin, same convention as the sphere path
    float3 centerRWS = mul(localToWorld, float4(boundsCenter, 1.0f)).xyz - _ViewOriginWorldSpace[subviewIndex].xyz;
    float3 axisX = localToWorld._m00_m10_m20 * boundsExtents.x;
    float3 axisY = localToWorld._m01_m11_m21 * boundsExtents.y;
    float3 axisZ = localToWorld._m02_m12_m22 * boundsExtents.z;

    float4x4 viewProjMatrix = _ViewProjMatrix[subviewIndex];
    float4 clipC = mul(viewProjMatrix, float4(centerRWS, 1.0f));
    float4 clipX = mul(viewProjMatrix, float4(axisX, 0.0f));
    float4 clipY = mul(viewProjMatrix, float4(axisY, 0.0f));
    float4 clipZ = mul(viewProjMatrix, float4(axisZ, 0.0f));

    // conservatively visible if any corner could reach the near plane
    float minW = clipC.w - (abs(clipX.w) + abs(clipY.w) + abs(clipZ.w));
    if (minW <= 1e-6f)
        return true;

    float3 facingDir = _FacingDirWorldSpace[subviewIndex].xyz;
    float facingSupport = abs(dot(axisX, facingDir)) + abs(dot(axisY, facingDir)) + abs(dot(axisZ, facingDir));
    if ((dot(centerRWS, facingDir) + facingSupport) >= 0.f)
        return true;

    float2 ndcMin = 1.0e10f;
    float2 ndcMax = -1.0e10f;
    float queryClosestDepth = 0.0f;

    [unroll]
    for (uint i = 0; i < 8; ++i)
    {
        float4 clipPos = clipC;
        clipPos += (i & 1) ? clipX : -clipX;
        clipPos += (i & 2) ? clipY : -clipY;
        clipPos += (i & 4) ? clipZ : -clipZ;

        #if UNITY_UV_STARTS_AT_TOP
        clipPos.y = -clipPos.y;
        #endif
        float3 ndc = clipPos.xyz * rcp(clipPos.w);
        ndc.xy = ndc.xy * 0.5f + 0.5f;

        ndcMin = min(ndcMin, ndc.xy);
        ndcMax = max(ndcMax, ndc.xy);
        queryClosestDepth = (i == 0) ? ndc.z : ClosestDepth(queryClosestDepth, ndc.z);
    }

    // A flat OBB (quad, plane) projects exactly onto the occluder depth its own surface rendered, so the strict depth comparison would classify it as occluded by itself.
    // Pull the query depth slightly closer (a small fraction of eye depth) so a surface never culls itself.
    const float kSelfOcclusionDepthSlack = 1.0f / 256.0f;
    queryClosestDepth = PullDepthTowardsCamera(queryClosestDepth, kSelfOcclusionDepthSlack);

    // rect test: dominant-axis footprint with alignment-aware mip selection
    float2 centerPosNDC = 0.5f * (ndcMin + ndcMax);
    float2 halfSizeNDC = 0.5f * (ndcMax - ndcMin);
    return IsOcclusionVisibleRect(queryClosestDepth, centerPosNDC, halfSizeNDC, subviewIndex);
}

bool IsOcclusionVisible(BoundingObjectData data, int subviewIndex)
{
    return IsOcclusionVisible(data.frontCenterPosRWS, data.centerPosNDC, data.radialPosNDC, subviewIndex);
}

bool IsOcclusionVisible(SphereBound boundingSphere, int subviewIndex)
{
    BoundingObjectData data = CalculateBoundingObjectData(
        boundingSphere,
        _ViewProjMatrix[subviewIndex],
        _ViewOriginWorldSpace[subviewIndex],
        _RadialDirWorldSpace[subviewIndex],
        _FacingDirWorldSpace[subviewIndex]);
    return IsOcclusionVisible(data, subviewIndex);
}

bool IsOcclusionVisible(CylinderBound cylinderBound, int subviewIndex)
{
    BoundingObjectData data = CalculateBoundingObjectData(
        cylinderBound,
        _ViewProjMatrix[subviewIndex],
        _ViewOriginWorldSpace[subviewIndex],
        _RadialDirWorldSpace[subviewIndex],
        _FacingDirWorldSpace[subviewIndex]);
    return IsOcclusionVisible(data, subviewIndex);
}
#endif
