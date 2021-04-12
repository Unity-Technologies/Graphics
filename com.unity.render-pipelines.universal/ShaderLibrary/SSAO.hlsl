#ifndef UNIVERSAL_SSAO_INCLUDED
#define UNIVERSAL_SSAO_INCLUDED

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

// Textures & Samplers
TEXTURE2D_X(_BaseMap);
TEXTURE2D_X(_ScreenSpaceOcclusionTexture);

SAMPLER(sampler_BaseMap);
SAMPLER(sampler_ScreenSpaceOcclusionTexture);

// Params
half4 _SSAOParams;
half4 _CameraViewTopLeftCorner[2];
half4x4 _CameraViewProjections[2]; // This is different from UNITY_MATRIX_VP (platform-agnostic projection matrix is used). Handle both non-XR and XR modes.

float4 _SourceSize;
float4 _ProjectionParams2;
float4 _CameraViewXExtent[2];
float4 _CameraViewYExtent[2];
float4 _CameraViewZExtent[2];

// Hardcoded random UV values that improves performance.
// The values were taken from this function:
// r = frac(43758.5453 * sin( dot(float2(12.9898, 78.233), uv)) ));
// Indices  0 to 19 are for u = 0.0
// Indices 20 to 39 are for u = 1.0
static half SSAORandomUV[40] =
{
    0.00000000,  // 00
    0.33984375,  // 01
    0.75390625,  // 02
    0.56640625,  // 03
    0.98437500,  // 04
    0.07421875,  // 05
    0.23828125,  // 06
    0.64062500,  // 07
    0.35937500,  // 08
    0.50781250,  // 09
    0.38281250,  // 10
    0.98437500,  // 11
    0.17578125,  // 12
    0.53906250,  // 13
    0.28515625,  // 14
    0.23137260,  // 15
    0.45882360,  // 16
    0.54117650,  // 17
    0.12941180,  // 18
    0.64313730,  // 19

    0.92968750,  // 20
    0.76171875,  // 21
    0.13333330,  // 22
    0.01562500,  // 23
    0.00000000,  // 24
    0.10546875,  // 25
    0.64062500,  // 26
    0.74609375,  // 27
    0.67968750,  // 28
    0.35156250,  // 29
    0.49218750,  // 30
    0.12500000,  // 31
    0.26562500,  // 32
    0.62500000,  // 33
    0.44531250,  // 34
    0.17647060,  // 35
    0.44705890,  // 36
    0.93333340,  // 37
    0.87058830,  // 38
    0.56862750,  // 39
};

// SSAO Settings
#define INTENSITY _SSAOParams.x
#define RADIUS _SSAOParams.y
#define DOWNSAMPLE _SSAOParams.z

// GLES2: In many cases, dynamic looping is not supported.
#if defined(SHADER_API_GLES) && !defined(SHADER_API_GLES3)
    #define SAMPLE_COUNT 3
#else
    #define SAMPLE_COUNT int(_SSAOParams.w)
#endif

// Function defines
#define SCREEN_PARAMS        GetScaledScreenParams()
#define SAMPLE_BASEMAP(uv)   SAMPLE_TEXTURE2D_X(_BaseMap, sampler_BaseMap, UnityStereoTransformScreenSpaceTex(uv));

// Constants
// kContrast determines the contrast of occlusion. This allows users to control over/under
// occlusion. At the moment, this is not exposed to the editor because it's rarely useful.
// The range is between 0 and 1.
static const half kContrast = half(0.5);

// The constant below controls the geometry-awareness of the bilateral
// filter. The higher value, the more sensitive it is.
static const half kGeometryCoeff = half(0.8);

// The constants below are used in the AO estimator. Beta is mainly used for suppressing
// self-shadowing noise, and Epsilon is used to prevent calculation underflow. See the paper
// (Morgan 2011 https://casual-effects.com/research/McGuire2011AlchemyAO/index.html)
// for further details of these constants.
static const half kBeta = half(0.002);
static const half kEpsilon = half(0.0001);

#if defined(USING_STEREO_MATRICES)
    #define unity_eyeIndex unity_StereoEyeIndex
#else
    #define unity_eyeIndex 0
#endif

half4 PackAONormal(half ao, half3 n)
{
    return half4(ao, n * half(0.5) + half(0.5));
}

half3 GetPackedNormal(half4 p)
{
    return p.gba * half(2.0) - half(1.0);
}

half GetPackedAO(half4 p)
{
    return p.r;
}

half EncodeAO(half x)
{
    #if UNITY_COLORSPACE_GAMMA
        return half(1.0 - max(LinearToSRGB(1.0 - saturate(x)), 0.0));
    #else
        return x;
    #endif
}

half CompareNormal(half3 d1, half3 d2)
{
    return smoothstep(kGeometryCoeff, half(1.0), dot(d1, d2));
}

// Trigonometric function utility
half2 CosSin(half theta)
{
    half sn, cs;
    sincos(theta, sn, cs);
    return half2(cs, sn);
}

// Pseudo random number generator with 2D coordinates
half GetRandomUVForSSAO(float u, int sampleIndex)
{
    return SSAORandomUV[u * 20 + sampleIndex];
}

float2 GetScreenSpacePosition(float2 uv)
{
    return float2(uv * SCREEN_PARAMS.xy * DOWNSAMPLE);
}

// Sample point picker
half3 PickSamplePoint(float2 uv, int sampleIndex)
{
    const float2 positionSS = GetScreenSpacePosition(uv);
    const half gn = half(InterleavedGradientNoise(positionSS, sampleIndex));

    const half u = frac(GetRandomUVForSSAO(half(0.0), sampleIndex) + gn) * half(2.0) - half(1.0);
    const half theta = (GetRandomUVForSSAO(half(1.0), sampleIndex) + gn) * half(TWO_PI);

    return half3(CosSin(theta) * sqrt(half(1.0) - u * u), u);
}

float SampleAndGetLinearEyeDepth(float2 uv)
{
    float rawDepth = SampleSceneDepth(uv.xy);
    #if defined(_ORTHOGRAPHIC)
        return LinearDepthToEyeDepth(rawDepth);
    #else
        return LinearEyeDepth(rawDepth, _ZBufferParams);
    #endif
}

// This returns a vector in world unit (not a position), from camera to the given point described by uv screen coordinate and depth (in absolute world unit).
half3 ReconstructViewPos(float2 uv, float depth)
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

    return half3(viewPos);
}

// Try reconstructing normal accurately from depth buffer.
// Low:    DDX/DDY on the current pixel
// Medium: 3 taps on each direction | x | * | y |
// High:   5 taps on each direction: | z | x | * | y | w |
// https://atyuwen.github.io/posts/normal-reconstruction/
// https://wickedengine.net/2019/09/22/improved-normal-reconstruction-from-depth/
half3 ReconstructNormal(float2 uv, float depth, float3 vpos)
{
    #if defined(_RECONSTRUCT_NORMAL_LOW)
        return half3(normalize(cross(ddy(vpos), ddx(vpos))));
    #else
        float2 delta = float2(_SourceSize.zw * 2.0);

        // Sample the neighbour fragments
        float2 lUV = float2(-delta.x, 0.0);
        float2 rUV = float2( delta.x, 0.0);
        float2 uUV = float2(0.0,  delta.y);
        float2 dUV = float2(0.0, -delta.y);

        float3 l1 = float3(uv + lUV, 0.0); l1.z = SampleAndGetLinearEyeDepth(l1.xy); // Left1
        float3 r1 = float3(uv + rUV, 0.0); r1.z = SampleAndGetLinearEyeDepth(r1.xy); // Right1
        float3 u1 = float3(uv + uUV, 0.0); u1.z = SampleAndGetLinearEyeDepth(u1.xy); // Up1
        float3 d1 = float3(uv + dUV, 0.0); d1.z = SampleAndGetLinearEyeDepth(d1.xy); // Down1

        // Determine the closest horizontal and vertical pixels...
        // horizontal: left = 0.0 right = 1.0
        // vertical  : down = 0.0    up = 1.0
        #if defined(_RECONSTRUCT_NORMAL_MEDIUM)
             uint closest_horizontal = l1.z > r1.z ? 0 : 1;
             uint closest_vertical   = d1.z > u1.z ? 0 : 1;
        #else
            float3 l2 = float3(uv + lUV * 2.0, 0.0); l2.z = SampleAndGetLinearEyeDepth(l2.xy); // Left2
            float3 r2 = float3(uv + rUV * 2.0, 0.0); r2.z = SampleAndGetLinearEyeDepth(r2.xy); // Right2
            float3 u2 = float3(uv + uUV * 2.0, 0.0); u2.z = SampleAndGetLinearEyeDepth(u2.xy); // Up2
            float3 d2 = float3(uv + dUV * 2.0, 0.0); d2.z = SampleAndGetLinearEyeDepth(d2.xy); // Down2

            const uint closest_horizontal = abs( (2.0 * l1.z - l2.z) - depth) < abs( (2.0 * r1.z - r2.z) - depth) ? 0 : 1;
            const uint closest_vertical   = abs( (2.0 * d1.z - d2.z) - depth) < abs( (2.0 * u1.z - u2.z) - depth) ? 0 : 1;
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

        // Use the cross product to calculate the normal...
        return half3(normalize(cross(ReconstructViewPos(P2.xy, P2.z) - vpos, ReconstructViewPos(P1.xy, P1.z) - vpos)));
    #endif
}

// For when we don't need to output the depth or view position
// Used in the blur passes
half3 SampleNormal(float2 uv)
{
    #if defined(_SOURCE_DEPTH_NORMALS)
        return half3(SampleSceneNormals(uv));
    #else
        float depth = SampleAndGetLinearEyeDepth(uv);
        half3 vpos = ReconstructViewPos(uv, depth);
        return ReconstructNormal(uv, depth, vpos);
    #endif
}

void SampleDepthNormalView(float2 uv, out float depth, out half3 normal, out half3 vpos)
{
    depth  = SampleAndGetLinearEyeDepth(uv);
    vpos   = ReconstructViewPos(uv, depth);

    #if defined(_SOURCE_DEPTH_NORMALS)
        normal = half3(SampleSceneNormals(uv));
    #else
        normal = ReconstructNormal(uv, depth, vpos);
    #endif
}

// Distance-based AO estimator based on Morgan 2011
// "Alchemy screen-space ambient obscurance algorithm"
// http://graphics.cs.williams.edu/papers/AlchemyHPG11/
half4 SSAO(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = input.uv;

    // Parameters used in coordinate conversion
    half3x3 camTransform = (half3x3)_CameraViewProjections[unity_eyeIndex]; // camera viewProjection matrix

    // Get the depth, normal and view position for this fragment
    float depth_o;
    half3 norm_o;
    half3 vpos_o;
    SampleDepthNormalView(uv, depth_o, norm_o, vpos_o);

    // This was added to avoid a NVIDIA driver issue.
    const half rcpSampleCount = half(rcp(SAMPLE_COUNT));
    half ao = 0.0;
    for (int s = 0; s < SAMPLE_COUNT; s++)
    {
        // Sample point
        half3 v_s1 = PickSamplePoint(uv, s);

        // Make it distributed between [0, _Radius]
        v_s1 *= sqrt((half(s) + half(1.0)) * rcpSampleCount) * RADIUS;

        v_s1 = faceforward(v_s1, -norm_o, v_s1);

        half3 vpos_s1 = vpos_o + v_s1;

        // Reproject the sample point
        half3 spos_s1 = mul(camTransform, vpos_s1);

        #if defined(_ORTHOGRAPHIC)
            float2 uv_s1_01 = clamp((spos_s1.xy + float(1.0)) * float(0.5), float(0.0), float(1.0));
        #else
            float zdist = -dot(UNITY_MATRIX_V[2].xyz, vpos_s1);
            float2 uv_s1_01 = clamp((spos_s1.xy * rcp(zdist) + float(1.0)) * float(0.5), float(0.0), float(1.0));
        #endif

        // Depth at the sample point
        float depth_s1 = SampleAndGetLinearEyeDepth(uv_s1_01);

        // Relative position of the sample point
        half3 vpos_s2 = ReconstructViewPos(uv_s1_01, depth_s1);
        half3 v_s2 = vpos_s2 - vpos_o;

        // Estimate the obscurance value
        half dotVal = dot(v_s2, norm_o);
        #if defined(_ORTHOGRAPHIC)
            dotVal -= half(2.0 * kBeta * depth_o);
        #else
            dotVal -= half(kBeta * depth_o);
        #endif

        half a1 = max(dotVal, half(0.0));
        half a2 = dot(v_s2, v_s2) + kEpsilon;
        ao += a1 * rcp(a2);
    }

    // Intensity normalization
    ao *= RADIUS;

    // Apply contrast
    ao = PositivePow(ao * INTENSITY * rcpSampleCount, kContrast);
    return PackAONormal(ao, norm_o);
}

// Geometry-aware separable bilateral filter
half4 Blur(float2 uv, float2 delta) : SV_Target
{
    half4 p0 =  (half4) SAMPLE_BASEMAP(uv                 );
    half4 p1a = (half4) SAMPLE_BASEMAP(uv - delta * 1.3846153846);
    half4 p1b = (half4) SAMPLE_BASEMAP(uv + delta * 1.3846153846);
    half4 p2a = (half4) SAMPLE_BASEMAP(uv - delta * 3.2307692308);
    half4 p2b = (half4) SAMPLE_BASEMAP(uv + delta * 3.2307692308);

    #if defined(BLUR_SAMPLE_CENTER_NORMAL)
        #if defined(_SOURCE_DEPTH_NORMALS)
            half3 n0 = half3(SampleSceneNormals(uv));
        #else
            half3 n0 = SampleNormal(uv);
        #endif
    #else
        half3 n0 = GetPackedNormal(p0);
    #endif

    half w0  =                                           half(0.2270270270);
    half w1a = CompareNormal(n0, GetPackedNormal(p1a)) * half(0.3162162162);
    half w1b = CompareNormal(n0, GetPackedNormal(p1b)) * half(0.3162162162);
    half w2a = CompareNormal(n0, GetPackedNormal(p2a)) * half(0.0702702703);
    half w2b = CompareNormal(n0, GetPackedNormal(p2b)) * half(0.0702702703);

    half s = half(0.0);
    s += GetPackedAO(p0)  * w0;
    s += GetPackedAO(p1a) * w1a;
    s += GetPackedAO(p1b) * w1b;
    s += GetPackedAO(p2a) * w2a;
    s += GetPackedAO(p2b) * w2b;
    s *= rcp(w0 + w1a + w1b + w2a + w2b);

    return PackAONormal(s, n0);
}

// Geometry-aware bilateral filter (single pass/small kernel)
half BlurSmall(float2 uv, float2 delta)
{
    half4 p0 = (half4) SAMPLE_BASEMAP(uv                            );
    half4 p1 = (half4) SAMPLE_BASEMAP(uv + float2(-delta.x, -delta.y));
    half4 p2 = (half4) SAMPLE_BASEMAP(uv + float2( delta.x, -delta.y));
    half4 p3 = (half4) SAMPLE_BASEMAP(uv + float2(-delta.x,  delta.y));
    half4 p4 = (half4) SAMPLE_BASEMAP(uv + float2( delta.x,  delta.y));

    half3 n0 = GetPackedNormal(p0);

    half w0 = half(1.0);
    half w1 = CompareNormal(n0, GetPackedNormal(p1));
    half w2 = CompareNormal(n0, GetPackedNormal(p2));
    half w3 = CompareNormal(n0, GetPackedNormal(p3));
    half w4 = CompareNormal(n0, GetPackedNormal(p4));

    half s = half(0.0);
    s += GetPackedAO(p0) * w0;
    s += GetPackedAO(p1) * w1;
    s += GetPackedAO(p2) * w2;
    s += GetPackedAO(p3) * w3;
    s += GetPackedAO(p4) * w4;

    return s *= rcp(w0 + w1 + w2 + w3 + w4);
}

half4 HorizontalBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    const float2 uv = input.uv;
    const float2 delta = float2(_SourceSize.z, 0.0);
    return Blur(uv, delta);
}

half4 VerticalBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    const float2 uv = input.uv;
    const float2 delta = float2(0.0, _SourceSize.w * rcp(DOWNSAMPLE));
    return Blur(uv, delta);
}

half4 FinalBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    const float2 uv = input.uv;
    const float2 delta = _SourceSize.zw;
    return half(1.0) - BlurSmall(uv, delta );
}

#endif //UNIVERSAL_SSAO_INCLUDED
