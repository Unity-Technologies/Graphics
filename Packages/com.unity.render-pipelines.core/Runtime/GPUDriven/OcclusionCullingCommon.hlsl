#ifndef _OCCLUSION_CULLING_COMMON_H
#define _OCCLUSION_CULLING_COMMON_H

// If using this the shader should add
// #pragma multi_compile _ USE_ARRAY
// #pragma multi_compile _ OCCLUSION_DEBUG
// before including this file

#include "Packages/com.unity.render-pipelines.core/Runtime/GPUDriven/OcclusionCullingCommon.cs.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/GPUDriven/OcclusionCullingCommonShaderVariables.cs.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/GPUDriven/OcclusionCullingDefine.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/GPUDriven/OcclusionTestCommon.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/GPUDriven/GeometryUtilities.hlsl"

#define OCCLUSION_ENABLE_GATHER_TRIM 1

TEXTURE2D_A(_OccluderDepthPyramid);
SAMPLER(s_linear_clamp_sampler);

#ifdef OCCLUSION_DEBUG
RWStructuredBuffer<uint> _OcclusionDebugPyramid;

uint toDebugPyramidCoord(uint2 coord)
{
    return OCCLUSIONCULLINGCOMMONCONFIG_DEBUG_PYRAMID_OFFSET + coord.x + _DebugPyramidSize.x * (coord.y + g_slice_index * _DebugPyramidSize.y);
}
#endif

bool IsOcclusionVisible(float3 frontCenterPosRWS, float2 centerPosNDC, float2 radialPosNDC)
{
    bool isVisible = true;
    float queryClosestDepth = ComputeNormalizedDeviceCoordinatesWithZ(frontCenterPosRWS, _ViewProjMatrix).z;
    bool isBehindCamera = dot(frontCenterPosRWS, _FacingDirWorldSpace.xyz) >= 0.f;

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

        if (!_DebugAlwaysPassOcclusionTest)
        {
            // gather4 occluder depths to cover this radius
            float2 gatherUv = (float2(mipBounds.xy) + clamp(centerCoordInChosenMip, .5f, float2(mipBounds.zw) - .5f)) * _OccluderTextureSize.zw;
            float4 gatherDepths = GATHER_TEXTURE2D_A(_OccluderDepthPyramid, s_linear_clamp_sampler, gatherUv);
            float occluderDepth = FarthestDepth(gatherDepths);
            isVisible = IsVisibleAfterOcclusion(occluderDepth, queryClosestDepth);
        }


#ifdef OCCLUSION_DEBUG
        // show footprint of gather4 in debug output
        int countForOverlay = _DebugOverlayCountOccluded ^ (isVisible ? 1 : 0);
        if (countForOverlay != 0)
        {
            uint2 debugCoord = mipBounds.xy + uint2(clamp(int2(centerCoordInChosenMip - .5f), 0, mipBounds.zw - 2));
            InterlockedAdd(_OcclusionDebugPyramid[toDebugPyramidCoord(debugCoord + uint2(0, 0))], 1);
            InterlockedAdd(_OcclusionDebugPyramid[toDebugPyramidCoord(debugCoord + uint2(1, 0))], 1);
            InterlockedAdd(_OcclusionDebugPyramid[toDebugPyramidCoord(debugCoord + uint2(0, 1))], 1);
            InterlockedAdd(_OcclusionDebugPyramid[toDebugPyramidCoord(debugCoord + uint2(1, 1))], 1);

            // accumulate the total in the first slot
            InterlockedAdd(_OcclusionDebugPyramid[0], 1);
        }
#endif

    }

    return isVisible;
}

bool IsOcclusionVisible(BoundingObjectData data)
{
    return IsOcclusionVisible(data.frontCenterPosRWS, data.centerPosNDC, data.radialPosNDC);
}

bool IsOcclusionVisible(SphereBound boundingSphere)
{
    return IsOcclusionVisible(CalculateBoundingObjectData(boundingSphere,
        _ViewProjMatrix, _ViewOriginWorldSpace, _RadialDirWorldSpace, _FacingDirWorldSpace));
}

bool IsOcclusionVisible(CylinderBound cylinderBound)
{
    return IsOcclusionVisible(CalculateBoundingObjectData(cylinderBound,
        _ViewProjMatrix, _ViewOriginWorldSpace, _RadialDirWorldSpace, _FacingDirWorldSpace));
}
#endif
