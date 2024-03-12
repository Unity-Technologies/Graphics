#ifndef _OCCLUSION_CULLING_COMMON_H
#define _OCCLUSION_CULLING_COMMON_H

// If using this the shader should add
// #pragma multi_compile _ OCCLUSION_DEBUG
// before including this file

#include "Packages/com.unity.render-pipelines.core/Runtime/GPUDriven/OcclusionCullingCommon.cs.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/GPUDriven/OcclusionCullingCommonShaderVariables.cs.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/GPUDriven/OcclusionTestCommon.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/GPUDriven/GeometryUtilities.hlsl"

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

bool IsOcclusionVisible(float3 frontCenterPosRWS, float2 centerPosNDC, float2 radialPosNDC, int subviewIndex)
{
    bool isVisible = true;
    float queryClosestDepth = ComputeNormalizedDeviceCoordinatesWithZ(frontCenterPosRWS, _ViewProjMatrix[subviewIndex]).z;
    bool isBehindCamera = dot(frontCenterPosRWS, _FacingDirWorldSpace[subviewIndex].xyz) >= 0.f;

    float2 centerCoordInTopMip = centerPosNDC * _DepthSizeInOccluderPixels.xy;
    float radiusInPixels = length((radialPosNDC - centerPosNDC) * _DepthSizeInOccluderPixels.xy);

    // log2 of the radius in pixels for the gather4 mip level
    int mipLevel = 0;
    float mipPartUnused = frexp(radiusInPixels, mipLevel);
    mipLevel = max(mipLevel + 1, 0);
    if (mipLevel < OCCLUSIONCULLINGCOMMONCONFIG_MAX_OCCLUDER_MIPS && !isBehindCamera)
    {
        // scale our coordinate to this mip
        float2 centerCoordInChosenMip = ldexp(centerCoordInTopMip, -mipLevel);
        int4 mipBounds = _OccluderMipBounds[mipLevel];
        mipBounds.y += subviewIndex * _OccluderMipLayoutSizeY;

        if ((_OcclusionTestDebugFlags & OCCLUSIONTESTDEBUGFLAG_ALWAYS_PASS) == 0)
        {
            // gather4 occluder depths to cover this radius
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

    }

    return isVisible;
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
