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
float4 _SourceSize;
half4 _ProjectionParams2;
half4x4 _CameraViewProjections[2]; // This is different from UNITY_MATRIX_VP (platform-agnostic projection matrix is used). Handle both non-XR and XR modes.
half4 _CameraViewTopLeftCorner[2];
half4 _CameraViewXExtent[2];
half4 _CameraViewYExtent[2];
half4 _CameraViewZExtent[2];

// SSAO Settings
#define INTENSITY _SSAOParams.x
#define RADIUS _SSAOParams.y
#define DOWNSAMPLE _SSAOParams.z

// GLES2: In many cases, dynamic looping is not supported.
#if defined(SHADER_API_GLES) && !defined(SHADER_API_GLES3)
    #define SAMPLE_COUNT 3
#else
    #define SAMPLE_COUNT _SSAOParams.w
#endif

// Function defines
#define SCREEN_PARAMS        GetScaledScreenParams()
#define SAMPLE_BASEMAP(uv)   SAMPLE_TEXTURE2D_X(_BaseMap, sampler_BaseMap, UnityStereoTransformScreenSpaceTex(uv));
#define SAMPLE_BASEMAP_R(uv) SAMPLE_TEXTURE2D_X(_BaseMap, sampler_BaseMap, UnityStereoTransformScreenSpaceTex(uv)).r;


// Constants
// kContrast determines the contrast of occlusion. This allows users to control over/under
// occlusion. At the moment, this is not exposed to the editor because it's rarely useful.
static const half kContrast = half(0.6);

// The constant below controls the geometry-awareness of the bilateral
// filter. The higher value, the more sensitive it is.
static const half kGeometryCoeff = half(0.8);

// The constants below are used in the AO estimator. Beta is mainly used for suppressing
// self-shadowing noise, and Epsilon is used to prevent calculation underflow. See the
// paper (Morgan 2011 https://bit.ly/3uAPRgz) for further details of these constants.
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

half2 GetScreenSpacePosition(half2 uv)
{
    return uv * SCREEN_PARAMS.xy * DOWNSAMPLE;
}

// Trigonometric function utility
half2 CosSin(half theta)
{
    half sn, cs;
    sincos(theta, sn, cs);
    return half2(cs, sn);
}

// Pseudo random number generator with 2D coordinates
half UVRandom(half u, half v)
{
    half f = dot(half2(12.9898, 78.233), half2(u, v));
    return frac(half(43758.5453) * sin(f));
}

// Sample point picker
half3 PickSamplePoint(float2 uv, half randAddon, half index)
{
    half2 positionSS = GetScreenSpacePosition(uv);
    half gn = InterleavedGradientNoise(positionSS, index);
    half u = frac(UVRandom(half(0.0), index + randAddon) + gn) * half(2.0) - half(1.0);
    half theta = (UVRandom(half(1.0), index + randAddon) + gn) * TWO_PI;
    return half3(CosSin(theta) * sqrt(1.0 - u * u), u);
}

half RawToLinearDepth(half rawDepth)
{
    #if defined(_ORTHOGRAPHIC)
        #if UNITY_REVERSED_Z
            return ((_ProjectionParams.z - _ProjectionParams.y) * (half(1.0) - rawDepth) + _ProjectionParams.y);
        #else
            return ((_ProjectionParams.z - _ProjectionParams.y) * (rawDepth) + _ProjectionParams.y);
        #endif
    #else
        return LinearEyeDepth(rawDepth, _ZBufferParams);
    #endif
}

half SampleAndGetLinearDepth(float2 uv)
{
    const half rawDepth = half(SampleSceneDepth(uv.xy).r);
    return RawToLinearDepth(rawDepth);
}

// This returns a vector in world unit (not a position), from camera to the given point described by uv screen coordinate and depth (in absolute world unit).
half3 ReconstructViewPos(float2 uv, half depth)
{
    // Screen is y-inverted.
    uv.y = 1.0 - uv.y;

    // view pos in world space
    #if defined(_ORTHOGRAPHIC)
        half zScale = depth * half(_ProjectionParams.w); // divide by far plane
        half3 viewPos = _CameraViewTopLeftCorner[unity_eyeIndex].xyz
                            + _CameraViewXExtent[unity_eyeIndex].xyz * half(uv.x)
                            + _CameraViewYExtent[unity_eyeIndex].xyz * half(uv.y)
                            + _CameraViewZExtent[unity_eyeIndex].xyz * zScale;
    #else
        half zScale = depth * _ProjectionParams2.x; // divide by near plane
        half3 viewPos = _CameraViewTopLeftCorner[unity_eyeIndex].xyz
                            + _CameraViewXExtent[unity_eyeIndex].xyz * half(uv.x)
                            + _CameraViewYExtent[unity_eyeIndex].xyz * half(uv.y);
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
half3 ReconstructNormal(float2 uv, half depth, half3 vpos)
{
    #if defined(_RECONSTRUCT_NORMAL_LOW)
        return normalize(cross(ddy(vpos), ddx(vpos)));
    #else
        half2 delta = half2(_SourceSize.zw * 2.0);

        // Sample the neighbour fragments
        half2 lUV = half2(-delta.x, 0.0);
        half2 rUV = half2( delta.x, 0.0);
        half2 uUV = half2(0.0,  delta.y);
        half2 dUV = half2(0.0, -delta.y);

        half3 l1 = half3(uv + lUV, 0.0); l1.z = SampleAndGetLinearDepth(l1.xy); // Left1
        half3 r1 = half3(uv + rUV, 0.0); r1.z = SampleAndGetLinearDepth(r1.xy); // Right1
        half3 u1 = half3(uv + uUV, 0.0); u1.z = SampleAndGetLinearDepth(u1.xy); // Up1
        half3 d1 = half3(uv + dUV, 0.0); d1.z = SampleAndGetLinearDepth(d1.xy); // Down1

        // Determine the closest horizontal and vertical pixels...
        // horizontal: left = 0.0 right = 1.0
        // vertical  : down = 0.0    up = 1.0
        #if defined(_RECONSTRUCT_NORMAL_MEDIUM)
             uint closest_horizontal = l1.z > r1.z ? 0 : 1;
             uint closest_vertical   = d1.z > u1.z ? 0 : 1;
        #else
            half3 l2 = half3(uv + lUV * 2.0, 0.0); l2.z = SampleAndGetLinearDepth(l2.xy); // Left2
            half3 r2 = half3(uv + rUV * 2.0, 0.0); r2.z = SampleAndGetLinearDepth(r2.xy); // Right2
            half3 u2 = half3(uv + uUV * 2.0, 0.0); u2.z = SampleAndGetLinearDepth(u2.xy); // Up2
            half3 d2 = half3(uv + dUV * 2.0, 0.0); d2.z = SampleAndGetLinearDepth(d2.xy); // Down2

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
        half3 P1;
        half3 P2;
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

void SampleDepthNormalView(float2 uv, out half depth, out half3 normal, out half3 vpos)
{
    depth  = SampleAndGetLinearDepth(uv);
    vpos = ReconstructViewPos(uv, depth);

    #if defined(_SOURCE_DEPTH_NORMALS)
        normal = SampleSceneNormals(uv);
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
    half depth_o;
    half3 norm_o;
    half3 vpos_o;
    SampleDepthNormalView(uv, depth_o, norm_o, vpos_o);

    // This was added to avoid a NVIDIA driver issue.
    const half randAddon = half(uv.x * 1e-10);
    const half rcpSampleCount = rcp(SAMPLE_COUNT);
    half ao = 0.0;
    for (int s = 0; s < int(SAMPLE_COUNT); s++)
    {
        // Sample point
        half3 v_s1 = PickSamplePoint(uv, randAddon, half(s)); // (kchang) should we rotate this "random" vector to world space?

        // Make it distributed between [0, _Radius]
        v_s1 *= sqrt((s + 1.0) * rcpSampleCount ) * RADIUS;

        v_s1 = faceforward(v_s1, -norm_o, v_s1);
        half3 vpos_s1 = vpos_o + v_s1;

        // Reproject the sample point
        half3 spos_s1 = mul(camTransform, vpos_s1);

        #if defined(_ORTHOGRAPHIC)
            half2 uv_s1_01 = clamp((spos_s1.xy + half(1.0)) * half(0.5), half(0.0), half(1.0));
        #else
            half zdist = -dot(UNITY_MATRIX_V[2].xyz, vpos_s1);
            half2 uv_s1_01 = clamp((spos_s1.xy * rcp(zdist) + half(1.0)) * half(0.5), half(0.0), half(1.0));
        #endif

        // Depth at the sample point
        half depth_s1 = SampleAndGetLinearDepth(uv_s1_01);

        // Relative position of the sample point
        half3 vpos_s2 = ReconstructViewPos(uv_s1_01, depth_s1);
        half3 v_s2 = vpos_s2 - vpos_o;

        // Estimate the obscurance value
        half dottt = dot(v_s2, norm_o) - kBeta * depth_o;
        half a1 = max(dottt, half(0.0));
        half a2 = dot(v_s2, v_s2) + kEpsilon;
        ao += a1 / a2;//* rcp(a2);
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
            half3 n0 = SampleSceneNormals(uv);
        #else
            // Get the depth, normal and view position for this fragment
            half depth_o;
            half3 n0;
            half3 vpos_o;
            SampleDepthNormalView(uv, depth_o, n0, vpos_o);
        #endif
    #else
        half3 n0 = GetPackedNormal(p0);
    #endif

    half w0  =                                           0.2270270270;
    half w1a = CompareNormal(n0, GetPackedNormal(p1a)) * 0.3162162162;
    half w1b = CompareNormal(n0, GetPackedNormal(p1b)) * 0.3162162162;
    half w2a = CompareNormal(n0, GetPackedNormal(p2a)) * 0.0702702703;
    half w2b = CompareNormal(n0, GetPackedNormal(p2b)) * 0.0702702703;

    half s;
    s  = GetPackedAO(p0)  * w0;
    s += GetPackedAO(p1a) * w1a;
    s += GetPackedAO(p1b) * w1b;
    s += GetPackedAO(p2a) * w2a;
    s += GetPackedAO(p2b) * w2b;

    s *= rcp(w0 + w1a + w1b + w2a + w2b);

    return PackAONormal(s, n0);
}

// Geometry-aware bilateral filter (single pass/small kernel)
half BlurSmall(float2 uv, half2 delta)
{
    half4 p0 = (half4) SAMPLE_BASEMAP(uv                            );
    half4 p1 = (half4) SAMPLE_BASEMAP(uv + half2(-delta.x, -delta.y));
    half4 p2 = (half4) SAMPLE_BASEMAP(uv + half2( delta.x, -delta.y));
    half4 p3 = (half4) SAMPLE_BASEMAP(uv + half2(-delta.x,  delta.y));
    half4 p4 = (half4) SAMPLE_BASEMAP(uv + half2( delta.x,  delta.y));

    half3 n0 = GetPackedNormal(p0);

    half w0 = half(1.0);
    half w1 = CompareNormal(n0, GetPackedNormal(p1));
    half w2 = CompareNormal(n0, GetPackedNormal(p2));
    half w3 = CompareNormal(n0, GetPackedNormal(p3));
    half w4 = CompareNormal(n0, GetPackedNormal(p4));

    half s;
    s  = GetPackedAO(p0) * w0;
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
