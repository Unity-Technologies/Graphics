//--------------------------------------------------------------------------------------------------
// Definitions
//--------------------------------------------------------------------------------------------------

#define GROUP_SIZE_1D     8

#define EXPONENTIAL_FALLOFF_EXPONENT 2.2

//--------------------------------------------------------------------------------------------------
// Included headers
//--------------------------------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Core/Utilities/GeometryUtils.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/VolumetricLighting/HDRenderPipeline.VolumetricLighting.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"

//--------------------------------------------------------------------------------------------------
// Inputs & outputs
//--------------------------------------------------------------------------------------------------

RW_TEXTURE3D(float4, _VBufferDensity); // RGB = sqrt(scattering), A = sqrt(extinction)

StructuredBuffer<OrientedBBox>            _VolumeBounds;
StructuredBuffer<LocalVolumetricFogEngineData> _VolumeData;

//--------------------------------------------------------------------------------------------------
// Implementation
//--------------------------------------------------------------------------------------------------

float ComputeFadeFactor(float3 coordNDC, float dist,
    float3 rcpPosFaceFade, float3 rcpNegFaceFade, bool invertFade,
    float rcpDistFadeLen, float endTimesRcpDistFadeLen, int falloffMode)
{
    float3 posF = Remap10(coordNDC, rcpPosFaceFade, rcpPosFaceFade);
    float3 negF = Remap01(coordNDC, rcpNegFaceFade, 0);
    float  dstF = Remap10(dist, rcpDistFadeLen, endTimesRcpDistFadeLen);
    float  fade = posF.x * posF.y * posF.z * negF.x * negF.y * negF.z;

    // We only apply exponential falloff on the Blend Distance and not Distance Fade
    if (falloffMode == LOCALVOLUMETRICFOGFALLOFFMODE_EXPONENTIAL)
        fade = PositivePow(fade, EXPONENTIAL_FALLOFF_EXPONENT);

    fade = dstF * (invertFade ? (1 - fade) : fade);

    return fade;
}

// Jittered ray with screen-space derivatives.
struct JitteredRay
{
    float3 originWS;
    float3 centerDirWS;
    float3 jitterDirWS;
    float3 xDirDerivWS;
    float3 yDirDerivWS;
};

void PrepareVoxelization(
    uint3 dispatchThreadId, uint2 groupId, uint2 groupThreadId,
    out PositionInputs posInput, out uint tileIndex, out JitteredRay ray
)
{
    UNITY_XR_ASSIGN_VIEW_INDEX(dispatchThreadId.z);

    uint2 groupOffset = groupId * GROUP_SIZE_1D;
    uint2 voxelCoord = groupOffset + groupThreadId;

#ifdef VL_PRESET_OPTIMAL
    // The entire thread group is within the same light tile.
    uint2 tileCoord = groupOffset * VBUFFER_VOXEL_SIZE / TILE_SIZE_BIG_TILE;
#else
    // No compile-time optimizations, no scalarization.
    // If _VBufferVoxelSize is not a power of 2 or > TILE_SIZE_BIG_TILE, a voxel may straddle
    // a tile boundary. This means different voxel subsamples may belong to different tiles.
    // We accept this error, and simply use the coordinates of the center of the voxel.
    uint2 tileCoord = (uint2)((voxelCoord + 0.5) * _VBufferVoxelSize) / TILE_SIZE_BIG_TILE;
#endif
    tileIndex = tileCoord.x + _NumTileBigTileX * tileCoord.y;
    // This clamp is important as _VBufferVoxelSize can have float value which can cause en overflow (Crash on Vulkan and Metal)
    tileIndex = min(tileIndex, _NumTileBigTileX * _NumTileBigTileY);

    // Reminder: our voxels are sphere-capped right frustums (truncated right pyramids).
    // The curvature of the front and back faces is quite gentle, so we can use
    // the right frustum approximation (thus the front and the back faces are squares).
    // Note, that since we still rely on the perspective camera model, pixels at the center
    // of the screen correspond to larger solid angles than those at the edges.
    // Basically, sizes of front and back faces depend on the XY coordinate.
    // https://www.desmos.com/calculator/i3rkesvidk

    float3 F = GetViewForwardDir();
    float3 U = GetViewUpDir();

    float2 centerCoord = voxelCoord + float2(0.5, 0.5);

    // Compute a ray direction s.t. ViewSpace(rayDirWS).z = 1.
    float3 rayDirWS = mul(-float4(centerCoord, 1, 1), _VBufferCoordToViewDirWS[unity_StereoEyeIndex]).xyz;
    float3 rightDirWS = cross(rayDirWS, U);
    float  rcpLenRayDir = rsqrt(dot(rayDirWS, rayDirWS));
    float  rcpLenRightDir = rsqrt(dot(rightDirWS, rightDirWS));

    ray;
    ray.originWS = GetCurrentViewPosition();
    ray.centerDirWS = rayDirWS * rcpLenRayDir; // Normalize

    float FdotD = dot(F, ray.centerDirWS);
    float unitDistFaceSize = _VBufferUnitDepthTexelSpacing * FdotD * rcpLenRayDir;

    ray.xDirDerivWS = rightDirWS * (rcpLenRightDir * unitDistFaceSize); // Normalize & rescale
    ray.yDirDerivWS = cross(ray.xDirDerivWS, ray.centerDirWS); // Will have the length of 'unitDistFaceSize' by construction
    ray.jitterDirWS = ray.centerDirWS; // TODO

    posInput = GetPositionInput(voxelCoord, _VBufferViewportSize.zw, tileCoord);

    ApplyCameraRelativeXR(ray.originWS);
}

float3 ComputeVoxelCenterWS(
    PositionInputs posInput, JitteredRay ray, uint _VBufferSliceCount, uint slice, float t0, float de,
    out uint3 voxelCoord, out float t1, out float dt, out float t
)
{
    voxelCoord = uint3(posInput.positionSS, slice + _VBufferSliceCount * unity_StereoEyeIndex);

    float e1 = slice * de + de; // (slice + 1) / sliceCount
    t1 = DecodeLogarithmicDepthGeneralized(e1, _VBufferDistanceDecodingParams);
    dt = t1 - t0;
    t = t0 + 0.5 * dt;

    return ray.originWS + t * ray.centerDirWS;
}
