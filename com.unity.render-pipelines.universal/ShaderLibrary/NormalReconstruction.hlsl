#ifndef UNIVERSAL_NORMAL_RECONSTRUCTION
#define UNIVERSAL_NORMAL_RECONSTRUCTION

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

#if defined(USING_STEREO_MATRICES)
#define unity_eyeIndex unity_StereoEyeIndex
#else
#define unity_eyeIndex 0
#endif

float4 _SourceSize;
float4 _ProjectionParams2;
float4x4 _CameraViewProjections[2]; // This is different from UNITY_MATRIX_VP (platform-agnostic projection matrix is used). Handle both non-XR and XR modes.
float4 _CameraViewTopLeftCorner[2]; // TODO: check if we can use half type
float4 _CameraViewXExtent[2];
float4 _CameraViewYExtent[2];
float4 _CameraViewZExtent[2];

#ifdef DECALS_NORMAL_BLEND_LOW
#define _RECONSTRUCT_NORMAL_LOW
#endif

#ifdef DECALS_NORMAL_BLEND_MEDIUM
#define _RECONSTRUCT_NORMAL_MEDIUM
#endif

float RawToLinearDepth(float rawDepth)
{
#if defined(_ORTHOGRAPHIC)
#if UNITY_REVERSED_Z
    return ((_ProjectionParams.z - _ProjectionParams.y) * (1.0 - rawDepth) + _ProjectionParams.y);
#else
    return ((_ProjectionParams.z - _ProjectionParams.y) * (rawDepth)+_ProjectionParams.y);
#endif
#else
    return LinearEyeDepth(rawDepth, _ZBufferParams);
#endif
}

float SampleAndGetLinearDepth(float2 uv)
{
    float rawDepth = SampleSceneDepth(uv.xy).r;
    return RawToLinearDepth(rawDepth);
}

// This returns a vector in world unit (not a position), from camera to the given point described by uv screen coordinate and depth (in absolute world unit).
float3 ReconstructViewPos(float2 uv, float depth)
{
    // Screen is y-inverted.
    uv.y = 1.0 - uv.y;

    // view pos in world space
#if defined(_ORTHOGRAPHIC)
    float zScale = depth * _ProjectionParams.w; // divide by far plane
    float3 viewPos = _CameraViewTopLeftCorner[unity_eyeIndex].xyz
        + _CameraViewXExtent[unity_eyeIndex].xyz * uv.x
        + _CameraViewYExtent[unity_eyeIndex].xyz * uv.y
        + _CameraViewZExtent[unity_eyeIndex].xyz * zScale;
#else
    float zScale = depth * _ProjectionParams2.x; // divide by near plane
    float3 viewPos = _CameraViewTopLeftCorner[unity_eyeIndex].xyz
        + _CameraViewXExtent[unity_eyeIndex].xyz * uv.x
        + _CameraViewYExtent[unity_eyeIndex].xyz * uv.y;
    viewPos *= zScale;
#endif

    return viewPos;
}

// Try reconstructing normal accurately from depth buffer.
// Low:    DDX/DDY on the current pixel
// Medium: 3 taps on each direction | x | * | y |
// High:   5 taps on each direction: | z | x | * | y | w |
// https://atyuwen.github.io/posts/normal-reconstruction/
// https://wickedengine.net/2019/09/22/improved-normal-reconstruction-from-depth/
float3 ReconstructNormal(float2 uv, float depth, float3 vpos)
{
#if defined(_RECONSTRUCT_NORMAL_LOW)
    return normalize(cross(ddy(vpos), ddx(vpos)));
#else
    float2 delta = _SourceSize.zw * 2.0;

    // Sample the neighbour fragments
    float2 lUV = float2(-delta.x, 0.0);
    float2 rUV = float2(delta.x, 0.0);
    float2 uUV = float2(0.0, delta.y);
    float2 dUV = float2(0.0, -delta.y);

    float3 l1 = float3(uv + lUV, 0.0); l1.z = SampleAndGetLinearDepth(l1.xy); // Left1
    float3 r1 = float3(uv + rUV, 0.0); r1.z = SampleAndGetLinearDepth(r1.xy); // Right1
    float3 u1 = float3(uv + uUV, 0.0); u1.z = SampleAndGetLinearDepth(u1.xy); // Up1
    float3 d1 = float3(uv + dUV, 0.0); d1.z = SampleAndGetLinearDepth(d1.xy); // Down1

    // Determine the closest horizontal and vertical pixels...
    // horizontal: left = 0.0 right = 1.0
    // vertical  : down = 0.0    up = 1.0
#if defined(_RECONSTRUCT_NORMAL_MEDIUM)
    uint closest_horizontal = l1.z > r1.z ? 0 : 1;
    uint closest_vertical = d1.z > u1.z ? 0 : 1;
#else
    float3 l2 = float3(uv + lUV * 2.0, 0.0); l2.z = SampleAndGetLinearDepth(l2.xy); // Left2
    float3 r2 = float3(uv + rUV * 2.0, 0.0); r2.z = SampleAndGetLinearDepth(r2.xy); // Right2
    float3 u2 = float3(uv + uUV * 2.0, 0.0); u2.z = SampleAndGetLinearDepth(u2.xy); // Up2
    float3 d2 = float3(uv + dUV * 2.0, 0.0); d2.z = SampleAndGetLinearDepth(d2.xy); // Down2

    const uint closest_horizontal = abs((2.0 * l1.z - l2.z) - depth) < abs((2.0 * r1.z - r2.z) - depth) ? 0 : 1;
    const uint closest_vertical = abs((2.0 * d1.z - d2.z) - depth) < abs((2.0 * u1.z - u2.z) - depth) ? 0 : 1;
#endif


    // Calculate the triangle, in a counter-clockwize order, to
    // use based on the closest horizontal and vertical depths.
    // h == 0.0 && v == 0.0: p1 = left,  p2 = down
    // h == 1.0 && v == 0.0: p1 = down,  p2 = right
    // h == 1.0 && v == 1.0: p1 = right, p2 = up
    // h == 0.0 && v == 1.0: p1 = up,    p2 = left
    // Calculate the view space positions for the three points...
    float3 P1;
    float3 P2;
    if (closest_vertical == 0)
    {
        P1 = closest_horizontal == 0 ? l1 : d1;
        P2 = closest_horizontal == 0 ? d1 : r1;
    }
    else
    {
        P1 = closest_horizontal == 0 ? u1 : r1;
        P2 = closest_horizontal == 0 ? l1 : u1;
    }

    P1 = ReconstructViewPos(P1.xy, P1.z);
    P2 = ReconstructViewPos(P2.xy, P2.z);

    // Use the cross product to calculate the normal...
    return normalize(cross(P2 - vpos, P1 - vpos));
#endif
}
#endif // UNIVERSAL_NORMAL_RECONSTRUCTION
