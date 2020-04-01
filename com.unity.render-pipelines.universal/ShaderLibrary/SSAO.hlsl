#ifndef UNIVERSAL_SSAO_INCLUDED
#define UNIVERSAL_SSAO_INCLUDED

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

// Textures & Samplers
TEXTURE2D_X(_BaseMap);
TEXTURE2D_X(_ScreenSpaceAmbientOcclusionTexture);

SAMPLER(sampler_BaseMap);
SAMPLER(sampler_ScreenSpaceAmbientOcclusionTexture);

// SSAO Settings
int _SampleCount;
half _Intensity;
half _Radius;
float _DownSample;

#define SAMPLE_BASEMAP(uv)  SAMPLE_TEXTURE2D_X(_BaseMap, sampler_BaseMap, UnityStereoTransformScreenSpaceTex(uv));
#define SCREEN_PARAMS GetScaledScreenParams()
#define INTENSITY _Intensity
#define RADIUS _Radius
#define DOWNSAMPLE _DownSample

#if !defined(SHADER_API_GLES)
    #define SAMPLE_COUNT _SampleCount
#else
    // GLES2: In many cases, dynamic looping is not supported.
    #define SAMPLE_COUNT 3
#endif

// --------
// Options for further customization
// --------

// By default, a 5-tap Gaussian with the linear sampling technique is used
// in the bilateral noise filter. It can be replaced with a 7-tap Gaussian
// with adaptive sampling by enabling the macro below. Although the
// differences are not noticeable in most cases, it may provide preferable
// results with some special usage (e.g. NPR without textureing).
//#define BLUR_HIGH_QUALITY

// The SampleNormal function normalizes samples from G-buffer because
// they're possibly unnormalized. We can eliminate this if it can be said
// that there is no wrong shader that outputs unnormalized normals.
// #define VALIDATE_NORMALS

// Uniformly distributed points on a unit sphere
#define FIX_SAMPLING_PATTERN

// The constant below determines the contrast of occlusion. This allows
// users to control over/under occlusion. At the moment, this is not exposed
// to the editor because it's rarely useful.
static const float kContrast = 0.6;

// The constant below controls the geometry-awareness of the bilateral
// filter. The higher value, the more sensitive it is.
static const float kGeometryCoeff = -1.0;


// The constants below are used in the AO estimator. Beta is mainly used
// for suppressing self-shadowing noise, and Epsilon is used to prevent
// calculation underflow. See the paper (Morgan 2011 http://goo.gl/2iz3P)
// for further details of these constants.
static const float kBeta = 0.002;
#define EPSILON         1.0e-4



// Gamma encoding (only needed in gamma lighting mode)
half EncodeAO(half x)
{
    #if UNITY_COLORSPACE_GAMMA
        return 1.0 - max(LinearToSRGB(1.0 - saturate(x)), 0.0);
    #else
        return x;
    #endif
}

// Accessors for packed AO/normal buffer
half4 PackAONormal(half ao, half3 n)
{
    return half4(ao, n * 0.5 + 0.5);
}

half GetPackedAO(half4 p)
{
    return p.r;
}

half3 GetPackedNormal(half4 p)
{
    return p.gba * 2.0 - 1.0;
}

// Trigonometric function utility
float2 CosSin(float theta)
{
    float sn, cs;
    sincos(theta, sn, cs);
    return float2(cs, sn);
}

// Pseudo random number generator with 2D coordinates
float UVRandom(float u, float v)
{
    return frac(sin(dot(float2(u, v), float2(12.9898, 78.233)))*43758.5453);
}

// Check if the camera is perspective.
// (returns 1.0 when orthographic)
float CheckPerspective(float x)
{
    return unity_OrthoParams.w == 0 ? x : 1.0;
}

// Normal vector comparer (for geometry-aware weighting)
half CompareNormal(half3 d1, half3 d2)
{
    return smoothstep(kGeometryCoeff, 1.0, dot(d1, d2));
}

float GetLinearEyeDepth(float2 uv)
{
    float rawDepth = SampleSceneDepth(uv.xy).r;
    float persp = LinearEyeDepth(rawDepth, _ZBufferParams);
    float ortho = ((_ProjectionParams.z - _ProjectionParams.y) * (1.0-rawDepth) + _ProjectionParams.y);
    return unity_OrthoParams.w == 0 ? persp : ortho;
}

// Reconstruct view-space position from UV and depth.
// p11_22 = (unity_CameraProjection._11, unity_CameraProjection._22)
// p13_31 = (unity_CameraProjection._13, unity_CameraProjection._23)
float3 ReconstructViewPos(float3 uvDepth, float2 p11_22, float2 p13_31)
{
    return float3(((uvDepth.xy * 2.0 - 1.0 - p13_31) / p11_22) * CheckPerspective(uvDepth.z), uvDepth.z);
}

// Sample point picker
float3 PickSamplePoint(float2 uv, float index)
{
    // Uniformly distributed points on a unit sphere
    // http://mathworld.wolfram.com/SpherePointPicking.html
#if defined(FIX_SAMPLING_PATTERN)
    float gn = InterleavedGradientNoise(uv * DOWNSAMPLE * SCREEN_PARAMS.xy, index);
    // FIXME: This was added to avoid a NVIDIA driver issue.
    //                                       vvvvvvvvvvvv
    float u     = frac(UVRandom(0.0, index + uv.x * 1e-10) + gn) * 2.0 - 1.0;
    float theta =     (UVRandom(1.0, index + uv.x * 1e-10) + gn) * TWO_PI;
#else
    float u     = UVRandom( uv.x + _Time.x, uv.y + index) * 2.0 - 1.0;
    float theta = UVRandom(-uv.x - _Time.x, uv.y + index) * TWO_PI;
#endif

    float3 v = float3(CosSin(theta) * sqrt(1.0 - u * u), u);

    // Make them distributed between [0, _Radius]
    float l = sqrt((index + 1.0) / SAMPLE_COUNT) * RADIUS;
    return v * l;
}

// Try reconstructing normal accurately from depth buffer.
// input DepthBuffer: stores linearized depth in range (0, 1).
// 5 taps on each direction: | z | x | * | y | w |, '*' denotes the center sample.
// https://atyuwen.github.io/posts/normal-reconstruction/
// https://wickedengine.net/2019/09/22/improved-normal-reconstruction-from-depth/
float3 ReconstructNormal(float2 uv, float2 p11_22, float2 p13_31)
{
    #if defined(_RECONSTRUCT_NORMAL_LOW)
        float3 P0 = ReconstructViewPos(float3(uv,  GetLinearEyeDepth(uv)), p11_22, p13_31);

        // Use the cross product to calculate the normal...
        return normalize(cross(ddy(P0), ddx(P0)));
    #else
        float2 delta = SCREEN_PARAMS.zw - 1.0;

        // Sample the neighbour fragments
        float2 lUV = float2(-delta.x, 0.0);
        float2 rUV = float2( delta.x, 0.0);
        float2 uUV = float2(0.0,  delta.y);
        float2 dUV = float2(0.0, -delta.y);

        float3 c  = float3(uv,       0.0); c.z  = GetLinearEyeDepth( c.xy); // Center
        float3 l1 = float3(uv + lUV, 0.0); l1.z = GetLinearEyeDepth(l1.xy); // Left1
        float3 r1 = float3(uv + rUV, 0.0); r1.z = GetLinearEyeDepth(r1.xy); // Right1
        float3 u1 = float3(uv + uUV, 0.0); u1.z = GetLinearEyeDepth(u1.xy); // Up1
        float3 d1 = float3(uv + dUV, 0.0); d1.z = GetLinearEyeDepth(d1.xy); // Down1


        // Determine the closest horizontal and vertical pixels...
        // horizontal: left = 0.0 right = 1.0
        // vertical  : down = 0.0    up = 1.0
        #if defined(_RECONSTRUCT_NORMAL_MEDIUM)
            uint closest_horizontal = l1.z > r1.z ? 0 : 1;
            uint closest_vertical   = d1.z > u1.z ? 0 : 1;
        #else
            float3 l2 = float3(uv + lUV * 2.0, 0.0); l2.z = GetLinearEyeDepth(l2.xy); // Left2
            float3 r2 = float3(uv + rUV * 2.0, 0.0); r2.z = GetLinearEyeDepth(r2.xy); // Right2
            float3 u2 = float3(uv + uUV * 2.0, 0.0); u2.z = GetLinearEyeDepth(u2.xy); // Up2
            float3 d2 = float3(uv + dUV * 2.0, 0.0); d2.z = GetLinearEyeDepth(d2.xy); // Down2

            const uint closest_horizontal = abs( (2.0 * l1.z - l2.z) - c.z) < abs( (2.0 * r1.z - r2.z) - c.z) ? 0 : 1;
            const uint closest_vertical   = abs( (2.0 * d1.z - d2.z) - c.z) < abs( (2.0 * u1.z - u2.z) - c.z) ? 0 : 1;
        #endif


        // Calculate the triangle, in a counter-clockwize order, to
        // use based on the closest horizontal and vertical depths.
        // h == 0.0 && v == 0.0: p1 = left,  p2 = down
        // h == 1.0 && v == 0.0: p1 = down,  p2 = right
        // h == 1.0 && v == 1.0: p1 = right, p2 = up
        // h == 0.0 && v == 1.0: p1 = up,    p2 = left
        // Calculate the view space positions for the three points...
        float3 P0 = ReconstructViewPos(c, p11_22, p13_31);
        float3 P1;
        float3 P2;
        if (closest_vertical == 0)
        {
            P1 = ReconstructViewPos( (closest_horizontal == 0 ? l1 : d1), p11_22, p13_31 );
            P2 = ReconstructViewPos( (closest_horizontal == 0 ? d1 : r1), p11_22, p13_31 );
        }
        else
        {
            P1 = ReconstructViewPos( (closest_horizontal == 0 ? u1 : r1), p11_22, p13_31 );
            P2 = ReconstructViewPos( (closest_horizontal == 0 ? l1 : u1), p11_22, p13_31 );
        }

        // Use the cross product to calculate the normal...
        return normalize(cross(P2 - P0, P1 - P0));
    #endif
}

float3 SampleNormal(float2 uv, float2 p11_22, float2 p13_31)
{
    //#if defined(SOURCE_GBUFFER)
    //    return SAMPLE_TEXTURE2D(_CameraGBufferTexture2, sampler_CameraGBufferTexture2, uv.xy).xyz;
    //#elif defined(SOURCE_DEPTH_NORMALS)
    //    return SampleSceneNormals(uv.xy);
    //#else
        return ReconstructNormal(uv, p11_22, p13_31);
    //#endif
}

// Distance-based AO estimator based on Morgan 2011
// "Alchemy screen-space ambient obscurance algorithm"
// http://graphics.cs.williams.edu/papers/AlchemyHPG11/
float4 SSAO(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = input.uv;

    // Parameters used in coordinate conversion
    float3x3 camProj = (float3x3)unity_CameraProjection;
    float2 p11_22 = float2(camProj._11, camProj._22);
    float2 p13_31 = float2(camProj._13, camProj._23);

    // View space normal and depth
    float depth_o = GetLinearEyeDepth(uv.xy);
    float3 norm_o = SampleNormal(uv, p11_22, p13_31);

    // Reconstruct the view-space position.
    float3 vpos_o = ReconstructViewPos(float3(uv, depth_o), p11_22, p13_31);

    float ao = 0.0;
    for (int s = 0; s < int(SAMPLE_COUNT); s++)
    {
        #if defined(SHADER_API_D3D11)
            // This 'floor(1.0001 * s)' operation is needed to avoid a DX11 NVidia shader issue.
            s = floor(1.0001 * s);
        #endif

        // Sample point
        float3 v_s1 = PickSamplePoint(uv.xy, s).xyz;

        v_s1 = faceforward(v_s1, -norm_o, v_s1);
        float3 vpos_s1 = vpos_o + v_s1;

        // Reproject the sample point
        float3 spos_s1 = mul(camProj, vpos_s1);
        float2 uv_s1_01 = (spos_s1.xy / CheckPerspective(vpos_s1.z) + 1.0) * 0.5;

        // Depth at the sample point
        float depth_s1 = GetLinearEyeDepth(uv_s1_01);

        // Relative position of the sample point
        float3 vpos_s2 = ReconstructViewPos(float3(uv_s1_01, depth_s1), p11_22, p13_31);
        float3 v_s2 = vpos_s2 - vpos_o;

        // Estimate the obscurance value
        float a1 = max(dot(v_s2, norm_o) - kBeta * depth_o, 0.0000);
        float a2 = dot(v_s2, v_s2) + EPSILON;
        ao += a1 / a2;
    }

    // Intensity normalization
    ao *= RADIUS;

    // Apply contrast
    ao = PositivePow(ao * INTENSITY / SAMPLE_COUNT, kContrast);

    #if defined(_BLUR_ENABLED)
        return PackAONormal(ao, norm_o);
    #else
        return float4(1.0 - ao, 0.0, 0.0, 0.0);
    #endif
}

// Geometry-aware separable bilateral filter
float4 FragBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = input.uv;

    #if defined(BLUR_HORIZONTAL)
        float2 delta = float2(SCREEN_PARAMS.z - 1.0, 0.0);
    #else
        float2 delta = float2(0.0, (SCREEN_PARAMS.w - 1.0) / DOWNSAMPLE);
    #endif

    #if defined(BLUR_HIGH_QUALITY)
        // High quality 7-tap Gaussian with adaptive sampling
        half4 p0  = SAMPLE_BASEMAP(uv.xy                         );
        half4 p1a = SAMPLE_BASEMAP(uv.xy - (delta               ));
        half4 p1b = SAMPLE_BASEMAP(uv.xy + (delta               ));
        half4 p2a = SAMPLE_BASEMAP(uv.xy - (delta * 2.0         ));
        half4 p2b = SAMPLE_BASEMAP(uv.xy + (delta * 2.0         ));
        half4 p3a = SAMPLE_BASEMAP(uv.xy - (delta * 3.2307692308));
        half4 p3b = SAMPLE_BASEMAP(uv.xy + (delta * 3.2307692308));

        #if defined(BLUR_SAMPLE_CENTER_NORMAL)
            float3x3 camProj = (float3x3)unity_CameraProjection;
            float2 p11_22 = float2(camProj._11, camProj._22);
            float2 p13_31 = float2(camProj._13, camProj._23);
            half3 n0 = SampleNormal(uv, p11_22, p13_31);
        #else
            half3 n0 = GetPackedNormal(p0);
        #endif

        half w0 = 0.37004405286;
        half w1a = CompareNormal(n0, GetPackedNormal(p1a)) * 0.31718061674;
        half w1b = CompareNormal(n0, GetPackedNormal(p1b)) * 0.31718061674;
        half w2a = CompareNormal(n0, GetPackedNormal(p2a)) * 0.19823788546;
        half w2b = CompareNormal(n0, GetPackedNormal(p2b)) * 0.19823788546;
        half w3a = CompareNormal(n0, GetPackedNormal(p3a)) * 0.11453744493;
        half w3b = CompareNormal(n0, GetPackedNormal(p3b)) * 0.11453744493;

        half s;
        s  = GetPackedAO(p0)  * w0;
        s += GetPackedAO(p1a) * w1a;
        s += GetPackedAO(p1b) * w1b;
        s += GetPackedAO(p2a) * w2a;
        s += GetPackedAO(p2b) * w2b;
        s += GetPackedAO(p3a) * w3a;
        s += GetPackedAO(p3b) * w3b;

        s /= w0 + w1a + w1b + w2a + w2b + w3a + w3b;
    #else
        // Faster 5-tap Gaussian with linear sampling
        half4 p0  = SAMPLE_BASEMAP(uv.xy                         );
        half4 p1a = SAMPLE_BASEMAP(uv.xy - (delta * 1.3846153846));
        half4 p1b = SAMPLE_BASEMAP(uv.xy + (delta * 1.3846153846));
        half4 p2a = SAMPLE_BASEMAP(uv.xy - (delta * 3.2307692308));
        half4 p2b = SAMPLE_BASEMAP(uv.xy + (delta * 3.2307692308));

        #if defined(BLUR_SAMPLE_CENTER_NORMAL)
            float3x3 camProj = (float3x3)unity_CameraProjection;
            float2 p11_22 = float2(camProj._11, camProj._22);
            float2 p13_31 = float2(camProj._13, camProj._23);
            half3 n0 = SampleNormal(uv, p11_22, p13_31);
        #else
            half3 n0 = GetPackedNormal(p0);
        #endif

        half w0 = 0.2270270270;
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

        s /= w0 + w1a + w1b + w2a + w2b;
    #endif

    return PackAONormal(s, n0);
}


// Geometry-aware bilateral filter (single pass/small kernel)
half BlurSmall(TEXTURE2D_X_PARAM(tex, samp), float2 uv, float2 delta)
{
    half4 p0 = SAMPLE_TEXTURE2D_X(tex, samp, UnityStereoTransformScreenSpaceTex(uv                             ));
    half4 p1 = SAMPLE_TEXTURE2D_X(tex, samp, UnityStereoTransformScreenSpaceTex(uv + float2(-delta.x, -delta.y)));
    half4 p2 = SAMPLE_TEXTURE2D_X(tex, samp, UnityStereoTransformScreenSpaceTex(uv + float2( delta.x, -delta.y)));
    half4 p3 = SAMPLE_TEXTURE2D_X(tex, samp, UnityStereoTransformScreenSpaceTex(uv + float2(-delta.x,  delta.y)));
    half4 p4 = SAMPLE_TEXTURE2D_X(tex, samp, UnityStereoTransformScreenSpaceTex(uv + float2( delta.x,  delta.y)));

    half3 n0 = GetPackedNormal(p0);

    half w0 = 1.0;
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

    return s / (w0 + w1 + w2 + w3 + w4);
}

// Final composition shader
float4 FragComposition(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = input.uv;

    float2 delta = (SCREEN_PARAMS.zw - 1.0) / DOWNSAMPLE;
    half ao = BlurSmall(TEXTURE2D_X_ARGS(_BaseMap, sampler_BaseMap), uv, delta);

    return EncodeAO(1.0 - ao);
}

#if !SHADER_API_GLES // Excluding the MRT pass under GLES2
    struct CompositionOutput
    {
        half4 gbuffer0 : SV_Target0;
        half4 gbuffer3 : SV_Target1;
    };

    CompositionOutput FragCompositionGBuffer(Varyings i)
    {
        float2 delta = (SCREEN_PARAMS.zw - 1.0) / DOWNSAMPLE;
        half ao = BlurSmall(TEXTURE2D_X_ARGS(_ScreenSpaceAmbientOcclusionTexture, sampler_ScreenSpaceAmbientOcclusionTexture), i.uv.xy, delta);

        CompositionOutput o;
        o.gbuffer0 = half4(0.0, 0.0, 0.0, ao);
        o.gbuffer3 = half4((half3)EncodeAO(ao), 0.0);
        return o;
    }
#else
    float4 FragCompositionGBuffer(Varyings i) : SV_Target
    {
        return (0.0).xxxx;
    }
#endif

#endif //UNIVERSAL_SSAO_INCLUDED
