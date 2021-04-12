#ifndef UNIVERSAL_NORMAL_RECONSTRUCTION
#define UNIVERSAL_NORMAL_RECONSTRUCTION

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"

#if defined(USING_STEREO_MATRICES)
#define unity_eyeIndex unity_StereoEyeIndex
#else
#define unity_eyeIndex 0
#endif

float4 _ProjectionParams2;
float4 _CameraViewTopLeftCorner[2]; // TODO: check if we can use half type
float4 _CameraViewXExtent[2];
float4 _CameraViewYExtent[2];
float4 _CameraViewZExtent[2];

float4x4 _NormalReconstructionMatrix;

float RawToLinearDepth(float rawDepth)
{
#if defined(_ORTHOGRAPHIC)
#if UNITY_REVERSED_Z
    return ((_ProjectionParams.z - _ProjectionParams.y) * (1.0 - rawDepth) + _ProjectionParams.y);
#else
    return ((_ProjectionParams.z - _ProjectionParams.y) * (rawDepth)+_ProjectionParams.y);
#endif
#else
    return LinearEyeDepth(rawDepth, _ZBufferParams); // 1.0 / (zBufferParam.z * depth + zBufferParam.w);
#endif
    //return _NormalReconstruction_DepthScale.y + rawDepth * _NormalReconstruction_DepthScale.x
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
// https://atyuwen.github.io/posts/normal-reconstruction/
// https://wickedengine.net/2019/09/22/improved-normal-reconstruction-from-depth/
float3 ReconstructNormalTap1_OLD(float3 vpos)
{
    return normalize(cross(ddy(vpos), ddx(vpos)));
}

// Try reconstructing normal accurately from depth buffer.
// Medium: 3 taps on each direction | x | * | y |
// https://atyuwen.github.io/posts/normal-reconstruction/
// https://wickedengine.net/2019/09/22/improved-normal-reconstruction-from-depth/
float3 ReconstructNormalTap5_OLD(float2 uv, float depth, float3 vpos)
{
    float2 delta = _ScreenSize.zw * 2.0;

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
    uint closest_horizontal = l1.z > r1.z ? 0 : 1;
    uint closest_vertical = d1.z > u1.z ? 0 : 1;

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
}

// Try reconstructing normal accurately from depth buffer.
// High:   5 taps on each direction: | z | x | * | y | w |
// https://atyuwen.github.io/posts/normal-reconstruction/
// https://wickedengine.net/2019/09/22/improved-normal-reconstruction-from-depth/
float3 ReconstructNormalTap9_OLD(float2 uv, float depth, float3 vpos)
{
    float2 delta = _ScreenSize.zw * 2.0;

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
    // vertical  : down = 0.0    up = 1.
    float3 l2 = float3(uv + lUV * 2.0, 0.0); l2.z = SampleAndGetLinearDepth(l2.xy); // Left2
    float3 r2 = float3(uv + rUV * 2.0, 0.0); r2.z = SampleAndGetLinearDepth(r2.xy); // Right2
    float3 u2 = float3(uv + uUV * 2.0, 0.0); u2.z = SampleAndGetLinearDepth(u2.xy); // Up2
    float3 d2 = float3(uv + dUV * 2.0, 0.0); d2.z = SampleAndGetLinearDepth(d2.xy); // Down2

    const uint closest_horizontal = abs((2.0 * l1.z - l2.z) - depth) < abs((2.0 * r1.z - r2.z) - depth) ? 0 : 1;
    const uint closest_vertical = abs((2.0 * d1.z - d2.z) - depth) < abs((2.0 * u1.z - u2.z) - depth) ? 0 : 1;

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
}

float getRawDepth(float2 uv) { return SampleSceneDepth(uv.xy).r; }

// inspired by keijiro's depth inverse projection
// https://github.com/keijiro/DepthInverseProjection
// constructs view space ray at the far clip plane from the screen uv
// then multiplies that ray by the linear 01 depth
float3 viewSpacePosAtScreenUV(float2 uv)
{
    float3 viewSpaceRay = mul(_NormalReconstructionMatrix, float4(uv * 2.0 - 1.0, 1.0, 1.0) * _ProjectionParams.z);
    float rawDepth = getRawDepth(uv);
    return viewSpaceRay * Linear01Depth(rawDepth, _ZBufferParams);
}

float3 viewSpacePosAtPixelPosition(float2 positionSS)
{
    float2 uv = positionSS * _ScreenSize.zw;
    return viewSpacePosAtScreenUV(uv);
}

half3 ReconstructNormalTap1(float2 positionSS)
{
    half3 viewSpacePos = viewSpacePosAtPixelPosition(positionSS);
    return normalize(cross(ddy(viewSpacePos), ddx(viewSpacePos)));
}

// Taken from https://gist.github.com/bgolus/a07ed65602c009d5e2f753826e8078a0
// unity's compiled fragment shader stats: 33 math, 3 tex
half3 ReconstructNormalTap3(float2 positionSS)
{
    // get current pixel's view space position
    half3 viewSpacePos_c = viewSpacePosAtPixelPosition(positionSS + float2(0.0, 0.0));

    // get view space position at 1 pixel offsets in each major direction
    half3 viewSpacePos_r = viewSpacePosAtPixelPosition(positionSS + float2(1.0, 0.0));
    half3 viewSpacePos_u = viewSpacePosAtPixelPosition(positionSS + float2(0.0, 1.0));

    // get the difference between the current and each offset position
    half3 hDeriv = viewSpacePos_r - viewSpacePos_c;
    half3 vDeriv = viewSpacePos_u - viewSpacePos_c;

    // get view space normal from the cross product of the diffs
    half3 viewNormal = normalize(cross(vDeriv, hDeriv));

    return viewNormal;
}

// Taken from https://gist.github.com/bgolus/a07ed65602c009d5e2f753826e8078a0
// unity's compiled fragment shader stats: 50 math, 4 tex
half3 ReconstructNormalTap4(float2 positionSS)
{
    // get view space position at 1 pixel offsets in each major direction
    half3 viewSpacePos_l = viewSpacePosAtPixelPosition(positionSS + float2(-1.0, 0.0));
    half3 viewSpacePos_r = viewSpacePosAtPixelPosition(positionSS + float2(1.0, 0.0));
    half3 viewSpacePos_d = viewSpacePosAtPixelPosition(positionSS + float2(0.0, -1.0));
    half3 viewSpacePos_u = viewSpacePosAtPixelPosition(positionSS + float2(0.0, 1.0));

    // get the difference between the current and each offset position
    half3 hDeriv = viewSpacePos_r - viewSpacePos_l;
    half3 vDeriv = viewSpacePos_u - viewSpacePos_d;

    // get view space normal from the cross product of the diffs
    half3 viewNormal = normalize(cross(vDeriv, hDeriv));

    return viewNormal;
}

// Taken from https://gist.github.com/bgolus/a07ed65602c009d5e2f753826e8078a0
// unity's compiled fragment shader stats: 54 math, 5 tex
half3 ReconstructNormalTap5(float2 positionSS)
{
    // get current pixel's view space position
    half3 viewSpacePos_c = viewSpacePosAtPixelPosition(positionSS + float2(0.0, 0.0));

    // get view space position at 1 pixel offsets in each major direction
    half3 viewSpacePos_l = viewSpacePosAtPixelPosition(positionSS + float2(-1.0, 0.0));
    half3 viewSpacePos_r = viewSpacePosAtPixelPosition(positionSS + float2(1.0, 0.0));
    half3 viewSpacePos_d = viewSpacePosAtPixelPosition(positionSS + float2(0.0, -1.0));
    half3 viewSpacePos_u = viewSpacePosAtPixelPosition(positionSS + float2(0.0, 1.0));

    // get the difference between the current and each offset position
    half3 l = viewSpacePos_c - viewSpacePos_l;
    half3 r = viewSpacePos_r - viewSpacePos_c;
    half3 d = viewSpacePos_c - viewSpacePos_d;
    half3 u = viewSpacePos_u - viewSpacePos_c;

    // pick horizontal and vertical diff with the smallest z difference
    half3 hDeriv = abs(l.z) < abs(r.z) ? l : r;
    half3 vDeriv = abs(d.z) < abs(u.z) ? d : u;

    // get view space normal from the cross product of the two smallest offsets
    half3 viewNormal = normalize(cross(vDeriv, hDeriv));

    return viewNormal;
}

// Taken from https://gist.github.com/bgolus/a07ed65602c009d5e2f753826e8078a0
// unity's compiled fragment shader stats: 66 math, 9 tex
half3 ReconstructNormalTap9(float2 positionSS)
{
    // screen uv from positionSS
    float2 uv = positionSS * _ScreenSize.zw;

    // current pixel's depth
    float c = getRawDepth(uv);

    // get current pixel's view space position
    half3 viewSpacePos_c = viewSpacePosAtScreenUV(uv);

    // get view space position at 1 pixel offsets in each major direction
    half3 viewSpacePos_l = viewSpacePosAtScreenUV(uv + float2(-1.0, 0.0) * _ScreenSize.zw);
    half3 viewSpacePos_r = viewSpacePosAtScreenUV(uv + float2(1.0, 0.0) * _ScreenSize.zw);
    half3 viewSpacePos_d = viewSpacePosAtScreenUV(uv + float2(0.0, -1.0) * _ScreenSize.zw);
    half3 viewSpacePos_u = viewSpacePosAtScreenUV(uv + float2(0.0, 1.0) * _ScreenSize.zw);

    // get the difference between the current and each offset position
    half3 l = viewSpacePos_c - viewSpacePos_l;
    half3 r = viewSpacePos_r - viewSpacePos_c;
    half3 d = viewSpacePos_c - viewSpacePos_d;
    half3 u = viewSpacePos_u - viewSpacePos_c;

    // get depth values at 1 & 2 pixels offsets from current along the horizontal axis
    half4 H = half4(
        getRawDepth(uv + float2(-1.0, 0.0) * _ScreenSize.zw.xy),
        getRawDepth(uv + float2(1.0, 0.0) * _ScreenSize.zw.xy),
        getRawDepth(uv + float2(-2.0, 0.0) * _ScreenSize.zw.xy),
        getRawDepth(uv + float2(2.0, 0.0) * _ScreenSize.zw.xy)
        );

    // get depth values at 1 & 2 pixels offsets from current along the vertical axis
    half4 V = half4(
        getRawDepth(uv + float2(0.0, -1.0) * _ScreenSize.zw.xy),
        getRawDepth(uv + float2(0.0, 1.0) * _ScreenSize.zw.xy),
        getRawDepth(uv + float2(0.0, -2.0) * _ScreenSize.zw.xy),
        getRawDepth(uv + float2(0.0, 2.0) * _ScreenSize.zw.xy)
        );

    // current pixel's depth difference from slope of offset depth samples
    // differs from original article because we're using non-linear depth values
    // see article's comments
    half2 he = abs((2 * H.xy - H.zw) - c);
    half2 ve = abs((2 * V.xy - V.zw) - c);

    // pick horizontal and vertical diff with the smallest depth difference from slopes
    half3 hDeriv = he.x < he.y ? l : r;
    half3 vDeriv = ve.x < ve.y ? d : u;

    // get view space normal from the cross product of the best derivatives
    half3 viewNormal = normalize(cross(vDeriv, hDeriv));

    return viewNormal;
}
#endif // UNIVERSAL_NORMAL_RECONSTRUCTION
