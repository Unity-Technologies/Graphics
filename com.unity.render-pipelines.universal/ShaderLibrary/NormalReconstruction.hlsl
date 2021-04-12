#ifndef UNIVERSAL_NORMAL_RECONSTRUCTION
#define UNIVERSAL_NORMAL_RECONSTRUCTION

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

#if defined(USING_STEREO_MATRICES)
#define unity_eyeIndex unity_StereoEyeIndex
#else
#define unity_eyeIndex 0
#endif

float4x4 _NormalReconstructionMatrix[2];

float GetRawDepth(float2 uv) { return SampleSceneDepth(uv.xy).r; }

// inspired by keijiro's depth inverse projection
// https://github.com/keijiro/DepthInverseProjection
// constructs view space ray at the far clip plane from the screen uv
// then multiplies that ray by the linear 01 depth
half3 ViewSpacePosAtScreenUV(float2 uv)
{
    half3 viewSpaceRay = (half3)(mul(_NormalReconstructionMatrix[unity_eyeIndex], float4(uv * 2.0 - 1.0, 1.0, 1.0) * _ProjectionParams.z));
    float rawDepth = GetRawDepth(uv);
    return viewSpaceRay * half(Linear01Depth(rawDepth, _ZBufferParams));
}

half3 ViewSpacePosAtPixelPosition(float2 positionSS)
{
    float2 uv = positionSS * _ScreenSize.zw;
    return ViewSpacePosAtScreenUV(uv);
}

half3 ReconstructNormalDerivative(float2 positionSS)
{
    half3 viewSpacePos = ViewSpacePosAtPixelPosition(positionSS);
    return normalize(cross(ddy(viewSpacePos), ddx(viewSpacePos)));
}

// Taken from https://gist.github.com/bgolus/a07ed65602c009d5e2f753826e8078a0
// unity's compiled fragment shader stats: 33 math, 3 tex
half3 ReconstructNormalTap3(float2 positionSS)
{
    // get current pixel's view space position
    half3 viewSpacePos_c = ViewSpacePosAtPixelPosition(positionSS + float2(0.0, 0.0));

    // get view space position at 1 pixel offsets in each major direction
    half3 viewSpacePos_r = ViewSpacePosAtPixelPosition(positionSS + float2(1.0, 0.0));
    half3 viewSpacePos_u = ViewSpacePosAtPixelPosition(positionSS + float2(0.0, 1.0));

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
    half3 viewSpacePos_l = ViewSpacePosAtPixelPosition(positionSS + float2(-1.0, 0.0));
    half3 viewSpacePos_r = ViewSpacePosAtPixelPosition(positionSS + float2(1.0, 0.0));
    half3 viewSpacePos_d = ViewSpacePosAtPixelPosition(positionSS + float2(0.0, -1.0));
    half3 viewSpacePos_u = ViewSpacePosAtPixelPosition(positionSS + float2(0.0, 1.0));

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
    half3 viewSpacePos_c = ViewSpacePosAtPixelPosition(positionSS + float2(0.0, 0.0));

    // get view space position at 1 pixel offsets in each major direction
    half3 viewSpacePos_l = ViewSpacePosAtPixelPosition(positionSS + float2(-1.0, 0.0));
    half3 viewSpacePos_r = ViewSpacePosAtPixelPosition(positionSS + float2(1.0, 0.0));
    half3 viewSpacePos_d = ViewSpacePosAtPixelPosition(positionSS + float2(0.0, -1.0));
    half3 viewSpacePos_u = ViewSpacePosAtPixelPosition(positionSS + float2(0.0, 1.0));

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
    float c = GetRawDepth(uv);

    // get current pixel's view space position
    half3 viewSpacePos_c = ViewSpacePosAtScreenUV(uv);

    // get view space position at 1 pixel offsets in each major direction
    half3 viewSpacePos_l = ViewSpacePosAtScreenUV(uv + float2(-1.0, 0.0) * _ScreenSize.zw);
    half3 viewSpacePos_r = ViewSpacePosAtScreenUV(uv + float2(1.0, 0.0) * _ScreenSize.zw);
    half3 viewSpacePos_d = ViewSpacePosAtScreenUV(uv + float2(0.0, -1.0) * _ScreenSize.zw);
    half3 viewSpacePos_u = ViewSpacePosAtScreenUV(uv + float2(0.0, 1.0) * _ScreenSize.zw);

    // get the difference between the current and each offset position
    half3 l = viewSpacePos_c - viewSpacePos_l;
    half3 r = viewSpacePos_r - viewSpacePos_c;
    half3 d = viewSpacePos_c - viewSpacePos_d;
    half3 u = viewSpacePos_u - viewSpacePos_c;

    // get depth values at 1 & 2 pixels offsets from current along the horizontal axis
    half4 H = half4(
        GetRawDepth(uv + float2(-1.0, 0.0) * _ScreenSize.zw.xy),
        GetRawDepth(uv + float2(1.0, 0.0) * _ScreenSize.zw.xy),
        GetRawDepth(uv + float2(-2.0, 0.0) * _ScreenSize.zw.xy),
        GetRawDepth(uv + float2(2.0, 0.0) * _ScreenSize.zw.xy)
        );

    // get depth values at 1 & 2 pixels offsets from current along the vertical axis
    half4 V = half4(
        GetRawDepth(uv + float2(0.0, -1.0) * _ScreenSize.zw.xy),
        GetRawDepth(uv + float2(0.0, 1.0) * _ScreenSize.zw.xy),
        GetRawDepth(uv + float2(0.0, -2.0) * _ScreenSize.zw.xy),
        GetRawDepth(uv + float2(0.0, 2.0) * _ScreenSize.zw.xy)
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
