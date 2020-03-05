#ifndef UNIVERSAL_SSAO_INCLUDED
#define UNIVERSAL_SSAO_INCLUDED

#define SOURCE_DEPTH

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"

#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
TEXTURE2D_ARRAY_FLOAT(_CameraDepthTexture);
#else
TEXTURE2D_FLOAT(_CameraDepthTexture);
#endif

SAMPLER(sampler_CameraDepthTexture);
TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
TEXTURE2D(_TempTarget); SAMPLER(sampler_TempTarget);
TEXTURE2D(_TempTarget2); SAMPLER(sampler_TempTarget2);
TEXTURE2D(_ScreenSpaceAOTexture); SAMPLER(sampler_ScreenSpaceAOTexture);
TEXTURE2D(_CameraGBufferTexture2); SAMPLER(sampler_CameraGBufferTexture2);
TEXTURE2D(_CameraDepthNormalsTexture); SAMPLER(sampler_CameraDepthNormalsTexture);

#define SAMPLE_DEPTH(uvScreenSpace) SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uvScreenSpace);
#define STEREO_UV(uv) UnityStereoTransformScreenSpaceTex(uv)


//float4 _ScreenParams;
float4x4 ProjectionMatrix;
float4 _BaseMap_TexelSize;
float4 _CameraDepthTexture_TexelSize;
float4 _ScreenSpaceAOTexture_TexelSize;

// SSAO Settings
half _SSAO_Intensity;
half _SSAO_Radius;
int _SSAO_Samples;
float _SSAO_DownScale;


// Sample count
#if !defined(SHADER_API_GLES)
    #define SAMPLE_COUNT _SSAO_Samples
#else
    // GLES2: In many cases, dynamic looping is not supported.
    #define SAMPLE_COUNT 3
#endif

// Other parameters
#define INTENSITY _SSAO_Intensity
#define RADIUS _SSAO_Radius
#define DOWNSAMPLE _SSAO_DownScale



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

// The constant below determines the contrast of occlusion. This allows
// users to control over/under occlusion. At the moment, this is not exposed
// to the editor because it's rarely useful.
static const float kContrast = 0.6;

// The constant below controls the geometry-awareness of the bilateral
// filter. The higher value, the more sensitive it is.
static const float kGeometryCoeff = 0.8;

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
    float f = dot(float2(12.9898, 78.233), float2(u, v));
    return frac(43758.5453 * sin(f));
}

// Check if the camera is perspective.
// (returns 1.0 when orthographic)
float CheckPerspective(float x)
{
    return lerp(x, 1.0, unity_OrthoParams.w);
}

// Reconstruct view-space position
float3 ReconstructViewPos(float2 uv, float depth)
{
    float isFlipped = _ProjectionParams.x;
    uv.y = lerp(1.0 - uv.y, uv.y, saturate(isFlipped));

#if UNITY_REVERSED_Z
    depth = 1.0 - depth;
#endif

    depth = 2.0 * depth - 1.0;
    return ComputeViewSpacePosition(uv, depth, unity_CameraInvProjection);
}

// Interleaved gradient function from Jimenez 2014
// http://www.iryoku.com/next-generation-post-processing-in-call-of-duty-advanced-warfare
float GradientNoise(float2 uv)
{
    uv = floor(uv * _ScreenParams.xy);
    float f = dot(float2(0.06711056, 0.00583715), uv);
    return frac(52.9829189 * frac(f));
}

#define FIX_SAMPLING_PATTERN
// Sample point picker
float3 PickSamplePoint(float2 uv, float index)
{
    // Uniformaly distributed points on a unit sphere
    // http://mathworld.wolfram.com/SpherePointPicking.html
#if defined(FIX_SAMPLING_PATTERN)
    float gn = GradientNoise(uv * DOWNSAMPLE);
    // FIXEME: This was added to avoid a NVIDIA driver issue.
    //                                   vvvvvvvvvvvv
    float u = frac(UVRandom(0.0, index + uv.x * 1e-10) + gn) * 2.0 - 1.0;
    float theta = (UVRandom(1.0, index + uv.x * 1e-10) + gn) * TWO_PI;
#else
    float u = UVRandom(uv.x + _Time.x, uv.y + index) * 2.0 - 1.0;
    float theta = UVRandom(-uv.x - _Time.x, uv.y + index) * TWO_PI;
#endif
    float3 v = float3(CosSin(theta) * sqrt(1.0 - u * u), u);

    // Make them distributed between [0, _Radius]
    float l = sqrt((index + 1.0) / SAMPLE_COUNT) * RADIUS;
    return v * l;
}

// Normal vector comparer (for geometry-aware weighting)
half CompareNormal(half3 d1, half3 d2)
{
    return smoothstep(kGeometryCoeff, 1.0, dot(d1, d2));
}

// Try reconstructing normal accurately from depth buffer.
// input DepthBuffer: stores linearized depth in range (0, 1).
// 5 taps on each direction: | z | x | * | y | w |, '*' denotes the center sample.
// https://atyuwen.github.io/posts/normal-reconstruction/
float3 ReconstructNormal(float4 uv)
{
    float isFlipped = _ProjectionParams.x;
    float2 texSize = _ScreenSpaceAOTexture_TexelSize.xy;
    float2 uvScreenSpace = uv.xy;
    float2 uvProj = uv.zw;

    // Projection position xy and depth
    float3 c  = float3( uvProj                                                       , 0.0 ); // Center
    float3 l2 = float3( uvProj - float2(texSize.x * 2.0,                         0.0), 0.0 ); // Left2
    float3 l1 = float3( uvProj - float2(texSize.x      ,                         0.0), 0.0 ); // Left1
    float3 r1 = float3( uvProj + float2(texSize.x      ,                         0.0), 0.0 ); // Right
    float3 r2 = float3( uvProj + float2(texSize.x * 2.0,                         0.0), 0.0 ); // Right
    float3 u2 = float3( uvProj + float2(            0.0, texSize.y * 2.0 * isFlipped), 0.0 ); // Up2
    float3 u1 = float3( uvProj + float2(            0.0, texSize.y       * isFlipped), 0.0 ); // Up1
    float3 d1 = float3( uvProj - float2(            0.0, texSize.y       * isFlipped), 0.0 ); // Down1
    float3 d2 = float3( uvProj - float2(            0.0, texSize.y * 2.0 * isFlipped), 0.0 ); // Down2

    c.z  = SAMPLE_DEPTH( uvScreenSpace                                );
    l2.z = SAMPLE_DEPTH( uvScreenSpace - float2(texSize.x * 2.0, 0.0) );
    l1.z = SAMPLE_DEPTH( uvScreenSpace - float2(texSize.x      , 0.0) );
    r1.z = SAMPLE_DEPTH( uvScreenSpace + float2(texSize.x      , 0.0) );
    r2.z = SAMPLE_DEPTH( uvScreenSpace + float2(texSize.x * 2.0, 0.0) );
    u2.z = SAMPLE_DEPTH( uvScreenSpace + float2(0.0, texSize.y * 2.0) );
    u1.z = SAMPLE_DEPTH( uvScreenSpace + float2(0.0, texSize.y      ) );
    d1.z = SAMPLE_DEPTH( uvScreenSpace - float2(0.0, texSize.y      ) );
    d2.z = SAMPLE_DEPTH( uvScreenSpace - float2(0.0, texSize.y * 2.0) );

    // 5 taps on each direction: | z | x | * | y | w |, '*' denotes the center sample.
    // If the depth is stored as is (not linearized) there is no need for this kind of interpolation and abs(2 * H.x - H.z) - depth) is sufficient,
    float2 he = float2( abs( (2.0 * l1.z - l2.z) - c.z), abs( (2.0 * r2.z - r1.z) - c.z) );
    float2 ve = float2( abs( (2.0 * d2.z - d1.z) - c.z), abs( (2.0 * u1.z - u2.z) - c.z) );
    
    // Determine the closest horizontal and vertical pixels...
    // horizontal: left = 0.0 right = 1.0
    // vertical  : down = 0.0    up = 1.0
    const uint closest_horizontal = step(he.y, he.x);
    const uint closest_vertical   = step(ve.y, ve.x);

    // Calculate the triangle, in a counter-clockwize order, to use based on the
    // closest horizontal and vertical depths. Using lerps rather than if statements.
    // h == 0.0 && v == 0.0: p1 = left,  p2 = down
    // h == 0.0 && v == 1.0: p1 = up,    p2 = left
    // h == 1.0 && v == 0.0: p1 = down,  p2 = right
    // h == 1.0 && v == 1.0: p1 = right, p2 = up
    float3 p1 = lerp( lerp(l1, d1, closest_horizontal), lerp(u1, r1, closest_horizontal), closest_vertical );
    float3 p2 = lerp( lerp(d1, r1, closest_horizontal), lerp(l1, u1, closest_horizontal), closest_vertical );

    // Calculate the view space positions for the three points...
    float3 P  = ReconstructViewPos( c.xy,  c.z); 
    float3 P1 = ReconstructViewPos(p1.xy, p1.z);
    float3 P2 = ReconstructViewPos(p2.xy, p2.z);
    
    // Use the cross product to calculate the normal...
    return normalize(cross(P1 - P, P2 - P)) * float3(-1 * isFlipped, -1, -1 * isFlipped);
}

// Calculates the normals from a texture sample
float3 CalculateNormalFromTextureSample(float4 textureValue, float4 uv)
{
    // Deferred...
    #if defined(SOURCE_GBUFFER)
        float3 normal = textureValue.xyz;
        normal = normal * 2 - any(normal); // gets (0,0,0) when norm == 0
        normal = mul((float3x3)unity_WorldToCamera, normal);

        #if defined(VALIDATE_NORMALS)
            normal = normalize(normal);
        #endif

        return normal;

    // Forward with DepthNormals texture...
    #elif defined(SOURCE_DEPTH_NORMALS)
        return UnpackNormalOctRectEncode(textureValue.xy) * float3(1.0, 1.0, -1.0);

    // Forward with Depth texture
    #else
        return ReconstructNormal(uv);
    #endif
}

// Calculates the depth from a texture sample
float CalculateDepthFromTextureSample(float4 textureValue, float2 uv)
{
    // Deferred
    #if defined(SOURCE_GBUFFER)
        float depth = UnpackFloatFromR8G8(textureValue.zw);

    // Forward with DepthNormals texture...
    #elif defined(SOURCE_DEPTH_NORMALS)
        float depth = UnpackFloatFromR8G8(textureValue.zw);

    // Forward with Depth texture
    #else
        float depth = textureValue.x;
    #endif

    return depth;
}

float4 SampleTexture(float2 uv)
{
    // Deferred
    #if defined(SOURCE_GBUFFER)
        return SAMPLE_TEXTURE2D(_CameraGBufferTexture2, sampler_CameraGBufferTexture2, uv);

    // Forward with DepthNormals texture...
    #elif defined(SOURCE_DEPTH_NORMALS)
        return SAMPLE_TEXTURE2D_LOD(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, uv, 0);

    // Forward with Depth texture
    #else
        return SAMPLE_DEPTH(uv);
    #endif
}

// Samples a texture for depth
float SampleDepth(float2 uv)
{
    float4 cdn = SampleTexture(uv);
    return CalculateDepthFromTextureSample(cdn, uv);
}

// Samples a texture for normals
float3 SampleNormal(float4 uv)
{
    // Deferred
    #if defined(SOURCE_GBUFFER)
        return CalculateNormalFromTextureSample(SampleTexture(uv.xy), float4(0.0,0.0, 0.0, 0.0));

    // Forward with DepthNormals texture...
    #elif defined(SOURCE_DEPTH_NORMALS)
        return CalculateNormalFromTextureSample(SampleTexture(uv.xy), float4(0.0, 0.0, 0.0, 0.0));

    // Forward with Depth texture
    #else
        return ReconstructNormal(uv);
    #endif
}

// Distance-based AO estimator based on Morgan 2011
// "Alchemy screen-space ambient obscurance algorithm"
// http://graphics.cs.williams.edu/papers/AlchemyHPG11/
float4 SSAO(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float4 uv = input.uv;

    // View space normal and depth
    float4 textureVal = SampleTexture(uv);
    float3 norm_o = CalculateNormalFromTextureSample(textureVal, uv);
    float depth_o = CalculateDepthFromTextureSample(textureVal, uv.xy);
    
    // Reconstruct the view-space position.
    float3 vpos_o = ReconstructViewPos(uv.xy, depth_o);

    float ao = 0.0;
    for (int s = 0; s < int(SAMPLE_COUNT); s++)
    {
        #if defined(SHADER_API_D3D11)
            // This 'floor(1.0001 * s)' operation is needed to avoid a DX11 NVidia shader issue.
            s = floor(1.0001 * s);
        #endif

        // Sample point
        float3 v_s1 = PickSamplePoint(uv, s);

        v_s1 = faceforward(v_s1, -norm_o, v_s1);
        float3 vpos_s1 = vpos_o + v_s1;

        // Reproject the sample point
        float3 spos_s1 = mul(unity_CameraProjection, vpos_s1);
        float2 uv_s1_01 = (spos_s1.xy / CheckPerspective(vpos_s1.z) + 1.0) * 0.5;

        // Depth at the sample point
        float depth_s1 = SampleDepth(uv_s1_01);

        // Relative position of the sample point
        float3 vpos_s2 = ReconstructViewPos(uv_s1_01, depth_s1);
        float3 v_s2 = vpos_s2 - vpos_o;

        // Estimate the obscurance value
        float a1 = max(dot(v_s2, norm_o) - kBeta * LinearEyeDepth(depth_o, _ZBufferParams), 0.00);
        float a2 = dot(v_s2, v_s2) + EPSILON;
        ao += a1 / a2;
    }

    // Intensity normalization
    ao *= RADIUS;

    // Apply contrast
    ao = PositivePow(ao * INTENSITY / SAMPLE_COUNT, kContrast);

    return PackAONormal(ao, norm_o);
}

// Geometry-aware separable bilateral filter
float4 FragBlur(Varyings input) : SV_Target
{
    float4 uv = input.uv;
    uv.xy = STEREO_UV(uv.xy);

    #if defined(BLUR_HORIZONTAL)
        // Horizontal pass: Always use 2 texels interval to match to the dither pattern.
        float2 delta = float2(_BaseMap_TexelSize.x * 2.0, 0.0);
    #else
        // Vertical pass: Apply _Downsample to match to the dither pattern in the original occlusion buffer.
        float2 delta = float2(0.0, _BaseMap_TexelSize.y / DOWNSAMPLE * 2.0);
    #endif

    #if defined(BLUR_HIGH_QUALITY)
        // High quality 7-tap Gaussian with adaptive sampling
        half4 p0  = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv.xy                       );
        half4 p1a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv.xy - delta               );
        half4 p1b = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv.xy + delta               );
        half4 p2a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv.xy - delta * 2.0         );
        half4 p2b = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv.xy + delta * 2.0         );
        half4 p3a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv.xy - delta * 3.2307692308);
        half4 p3b = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv.xy + delta * 3.2307692308);

        #if defined(BLUR_SAMPLE_CENTER_NORMAL)
            half3 n0 = SampleNormal(uv);
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
        s = GetPackedAO(p0)  * w0;
        s += GetPackedAO(p1a) * w1a;
        s += GetPackedAO(p1b) * w1b;
        s += GetPackedAO(p2a) * w2a;
        s += GetPackedAO(p2b) * w2b;
        s += GetPackedAO(p3a) * w3a;
        s += GetPackedAO(p3b) * w3b;

        s /= w0 + w1a + w1b + w2a + w2b + w3a + w3b;
    #else
        // Fater 5-tap Gaussian with linear sampling
        half4 p0  = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv.xy                       );
        half4 p1a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv.xy - delta * 1.3846153846);
        half4 p1b = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv.xy + delta * 1.3846153846);
        half4 p2a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv.xy - delta * 3.2307692308);
        half4 p2b = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv.xy + delta * 3.2307692308);

        #if defined(BLUR_SAMPLE_CENTER_NORMAL)
            half3 n0 = SampleNormal(uv);
        #else
            half3 n0 = GetPackedNormal(p0);
        #endif

        half w0 = 0.2270270270;
        half w1a = CompareNormal(n0, GetPackedNormal(p1a)) * 0.3162162162;
        half w1b = CompareNormal(n0, GetPackedNormal(p1b)) * 0.3162162162;
        half w2a = CompareNormal(n0, GetPackedNormal(p2a)) * 0.0702702703;
        half w2b = CompareNormal(n0, GetPackedNormal(p2b)) * 0.0702702703;

        half s;
        s = GetPackedAO(p0)  * w0;
        s += GetPackedAO(p1a) * w1a;
        s += GetPackedAO(p1b) * w1b;
        s += GetPackedAO(p2a) * w2a;
        s += GetPackedAO(p2b) * w2b;

        s /= w0 + w1a + w1b + w2a + w2b;
#endif

    return PackAONormal(s, n0);
}


// Geometry-aware bilateral filter (single pass/small kernel)
half BlurSmall(TEXTURE2D_PARAM(tex, samp), float2 uv, float2 delta)
{
    uv = STEREO_UV(uv);

    half4 p0 = SAMPLE_TEXTURE2D(tex, samp, uv                             );
    half4 p1 = SAMPLE_TEXTURE2D(tex, samp, uv + float2(-delta.x, -delta.y));
    half4 p2 = SAMPLE_TEXTURE2D(tex, samp, uv + float2( delta.x, -delta.y));
    half4 p3 = SAMPLE_TEXTURE2D(tex, samp, uv + float2(-delta.x,  delta.y));
    half4 p4 = SAMPLE_TEXTURE2D(tex, samp, uv + float2( delta.x,  delta.y));

    half3 n0 = GetPackedNormal(p0);

    half w0 = 1.0;
    half w1 = CompareNormal(n0, GetPackedNormal(p1));
    half w2 = CompareNormal(n0, GetPackedNormal(p2));
    half w3 = CompareNormal(n0, GetPackedNormal(p3));
    half w4 = CompareNormal(n0, GetPackedNormal(p4));

    half s;
    s = GetPackedAO(p0) * w0;
    s += GetPackedAO(p1) * w1;
    s += GetPackedAO(p2) * w2;
    s += GetPackedAO(p3) * w3;
    s += GetPackedAO(p4) * w4;

    return s / (w0 + w1 + w2 + w3 + w4);
}

// Final composition shader
float4 FragComposition(Varyings input) : SV_Target
{
    float2 uv = input.uv.xy;

    float2 delta = _BaseMap_TexelSize.xy / DOWNSAMPLE;
    half ao = BlurSmall(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), uv, delta);

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
        // Workaround: _ScreenSpaceAOTexture_Texelsize hasn't been set properly
        // for some reasons. Use _ScreenParams instead.
        float2 delta = (_ScreenParams.zw - 1.0) / DOWNSAMPLE;
        half ao = BlurSmall(TEXTURE2D_ARGS(_ScreenSpaceAOTexture, sampler_ScreenSpaceAOTexture), i.uv, delta);

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

float4 FragDebugOverlay(Varyings i) : SV_Target
{
    float2 delta = _ScreenSpaceAOTexture_TexelSize.xy / DOWNSAMPLE;
    half ao = BlurSmall(TEXTURE2D_ARGS(_ScreenSpaceAOTexture, sampler_ScreenSpaceAOTexture), i.uv, delta);
    ao = EncodeAO(ao);
    return float4(1.0 - ao.xxx, 1.0);
}

#endif //UNIVERSAL_SSAO_INCLUDED
