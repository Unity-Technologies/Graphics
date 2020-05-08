#ifndef UNIVERSAL_SSAO_INCLUDED
#define UNIVERSAL_SSAO_INCLUDED

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.shadergraph/ShaderGraphLibrary/ShaderVariablesFunctions.hlsl"
//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

// Textures & Samplers
TEXTURE2D_X(_BaseMap);
TEXTURE2D_X(_ScreenSpaceOcclusionTexture);

SAMPLER(sampler_BaseMap);
SAMPLER(sampler_ScreenSpaceOcclusionTexture);

float4 _BaseMap_TexelSize;
float4 _CameraDepthTexture_TexelSize;

// Function defines
#define RANDOM(f) GenerateHashedRandomFloat(f)
#define SAMPLE_BASEMAP(uv)  SAMPLE_TEXTURE2D_X(_BaseMap, sampler_BaseMap, UnityStereoTransformScreenSpaceTex(uv));
#define SCREEN_PARAMS GetScaledScreenParams()

// SSAO Settings
float4 _SSAOParams;
#define DOWNSAMPLE _SSAOParams.x
#define INTENSITY _SSAOParams.y
#define RADIUS _SSAOParams.z

#if !defined(SHADER_API_GLES)
    #define SAMPLE_COUNT _SSAOParams.w
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
float EncodeAO(float x)
{
    #if UNITY_COLORSPACE_GAMMA
        return 1.0 - max(LinearToSRGB(1.0 - saturate(x)), 0.0);
    #else
        return x;
    #endif
}

// Accessors for packed AO/normal buffer
float4 PackAONormal(float ao, float3 n)
{
    return float4(ao, n * 0.5 + 0.5);
}

float GetPackedAO(float4 p)
{
    return p.r;
}

float3 GetPackedNormal(float4 p)
{
    return p.gba * 2.0 - 1.0;
}

// Normal vector comparer (for geometry-aware weighting)
float CompareNormal(float3 d1, float3 d2)
{
    return smoothstep(kGeometryCoeff, 1.0, dot(d1, d2));
}

float2 GetScreenSpacePosition(float2 uv)
{
    return uv * SCREEN_PARAMS.xy * DOWNSAMPLE;
}

// Sample point picker
float3 PickSamplePoint(float2 uv, float randAddon, float index)
{
    // Uniformly distributed points on a unit sphere
    // http://mathworld.wolfram.com/SpherePointPicking.html
    float2 positionSS = GetScreenSpacePosition(uv);
    float gn = InterleavedGradientNoise(positionSS, index);

    float u     = frac(RANDOM(      randAddon) + gn);
    float theta =     (RANDOM(1.0 + randAddon) + gn * PI);
    return SampleSphereUniform(u, theta);
}

float SampleAndGetLinearDepth(float2 uv)
{
    float rawDepth = SampleSceneDepth(uv.xy).r;
    #if defined(_ORTHOGRAPHIC)
        float linearDepth = ((_ProjectionParams.z - _ProjectionParams.y) * (1.0 - rawDepth) + _ProjectionParams.y);
    #else
        float linearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
    #endif
    return linearDepth;
}

float3 ReconstructViewPos(float2 uv, float depth, float2 p11_22, float2 p13_31)
{
    #if defined(_ORTHOGRAPHIC)
        float3 viewPos = float3(((uv.xy * 2.0 - 1.0 - p13_31) / p11_22), depth);
    #else
        float3 viewPos = float3(depth * ((uv.xy * 2.0 - 1.0 - p13_31) / p11_22), depth);
    #endif
    return viewPos;
}

float3 ReconstructViewPos(float3 uvDepth, float2 p11_22, float2 p13_31)
{
    return ReconstructViewPos(uvDepth.xy, uvDepth.z, p11_22, p13_31);
}

// Try reconstructing normal accurately from depth buffer.
// input DepthBuffer: stores linearized depth in range (0, 1).
// 5 taps on each direction: | z | x | * | y | w |, '*' denotes the center sample.
// https://atyuwen.github.io/posts/normal-reconstruction/
// https://wickedengine.net/2019/09/22/improved-normal-reconstruction-from-depth/
float3 ReconstructNormal(float2 uv, float depth, float3 vpos, float2 p11_22, float2 p13_31)
{
    #if defined(_RECONSTRUCT_NORMAL_LOW)
        // Use the cross product to calculate the normal...
        return normalize(cross(ddy(vpos), ddx(vpos)));
    #else
        float2 delta = _CameraDepthTexture_TexelSize.xy * 2.0;

        // Sample the neighbour fragments
        float2 lUV = float2(-delta.x, 0.0);
        float2 rUV = float2( delta.x, 0.0);
        float2 uUV = float2(0.0,  delta.y);
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
             uint closest_vertical   = d1.z > u1.z ? 0 : 1;
        #else
            float3 l2 = float3(uv + lUV * 2.0, 0.0); l2.z = SampleAndGetLinearDepth(l2.xy); // Left2
            float3 r2 = float3(uv + rUV * 2.0, 0.0); r2.z = SampleAndGetLinearDepth(r2.xy); // Right2
            float3 u2 = float3(uv + uUV * 2.0, 0.0); u2.z = SampleAndGetLinearDepth(u2.xy); // Up2
            float3 d2 = float3(uv + dUV * 2.0, 0.0); d2.z = SampleAndGetLinearDepth(d2.xy); // Down2

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

        P1 = ReconstructViewPos(P1, p11_22, p13_31 );
        P2 = ReconstructViewPos(P2, p11_22, p13_31 );

        // Use the cross product to calculate the normal...
        return normalize(cross(P2 - vpos, P1 - vpos));
    #endif
}

float3 SampleNormal(float2 uv, float depth, float2 p11_22, float2 p13_31, out float3 vpos)
{
    //#if defined(SOURCE_GBUFFER)
    //    return SAMPLE_TEXTURE2D(_CameraGBufferTexture2, sampler_CameraGBufferTexture2, uv.xy).xyz;
    //#elif defined(SOURCE_DEPTH_NORMALS)
    //    return SampleSceneNormals(uv.xy);
    //#else
        vpos = ReconstructViewPos(uv, depth, p11_22, p13_31);
        return ReconstructNormal(uv, depth, vpos, p11_22, p13_31);
    //#endif
}

float3 SampleNormal(float2 uv, float depth, float2 p11_22, float2 p13_31)
{
    //#if defined(SOURCE_GBUFFER)
    //    return SAMPLE_TEXTURE2D(_CameraGBufferTexture2, sampler_CameraGBufferTexture2, uv.xy).xyz;
    //#elif defined(SOURCE_DEPTH_NORMALS)
    //    return SampleSceneNormals(uv.xy);
    //#else
        float3 vpos = ReconstructViewPos(uv, depth, p11_22, p13_31);
        return ReconstructNormal(uv, depth, vpos, p11_22, p13_31);
    //#endif
}

float3 SampleNormal(float2 uv, float2 p11_22, float2 p13_31)
{
    float depth = SampleAndGetLinearDepth(uv);
    return SampleNormal(uv, depth, p11_22, p13_31);
}

void SampleDepthNormalView(float2 uv, float2 p11_22, float2 p13_31, out float depth, out float3 normal, out float3 vpos)
{
    depth = SampleAndGetLinearDepth(uv);
    normal = SampleNormal(uv, depth, p11_22, p13_31, vpos);
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

    // Get the depth, normal and view position for this fragment
    float depth_o;
    float3 norm_o;
    float3 vpos_o;
    SampleDepthNormalView(uv, p11_22, p13_31, depth_o, norm_o, vpos_o);


    // This was added to avoid a NVIDIA driver issue.
    float randAddon = uv.x * 1e-10;

    float rcpSampleCount = rcp(SAMPLE_COUNT);
    float samplePointRadiusMultiplier = RADIUS * rcpSampleCount;
    float ao = 0.0;
    for (int s = 0; s < int(SAMPLE_COUNT); s++)
    {
        #if defined(SHADER_API_D3D11)
            // This 'floor(1.0001 * s)' operation is needed to avoid a DX11 NVidia shader issue.
            s = floor(1.0001 * s);
        #endif

        // Sample point
        float3 v_s1 = PickSamplePoint(uv.xy, randAddon + s, s);

        // Make it distributed between [0, _Radius]
        v_s1 *= (s + 1.0) * samplePointRadiusMultiplier;

        v_s1 = faceforward(v_s1, -norm_o, v_s1);
        float3 vpos_s1 = vpos_o + v_s1;

        // Reproject the sample point
        float3 spos_s1 = mul(camProj, vpos_s1);
        #if defined(_ORTHOGRAPHIC)
            float2 uv_s1_01 = (spos_s1.xy + 1.0) * 0.5;
        #else
            float2 uv_s1_01 = (spos_s1.xy * rcp(vpos_s1.z) + 1.0) * 0.5;
        #endif

        // Depth at the sample point
        float depth_s1 = SampleAndGetLinearDepth(uv_s1_01);

        // Relative position of the sample point
        float3 vpos_s2 = ReconstructViewPos(uv_s1_01, depth_s1, p11_22, p13_31);
        float3 v_s2 = vpos_s2 - vpos_o;

        // Estimate the obscurance value
        float a1 = max(dot(v_s2, norm_o) - kBeta * depth_o, 0.0);
        float a2 = dot(v_s2, v_s2) + EPSILON;
        ao += a1 * rcp(a2);
    }

    // Intensity normalization
    ao *= RADIUS;

    // Apply contrast
    ao = PositivePow(ao * INTENSITY * rcpSampleCount, kContrast);

    return PackAONormal(ao, norm_o);
}

// Geometry-aware separable bilateral filter
float4 FragBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = input.uv;

    #if defined(BLUR_HORIZONTAL)
        // Horizontal pass: Always use 2 texels interval to match to
        // the dither pattern.
        float2 delta = float2(_BaseMap_TexelSize.x, 0.0);
    #else
        // Vertical pass: Apply _Downsample to match to the dither
        // pattern in the original occlusion buffer.
        float2 delta = float2(0.0, _BaseMap_TexelSize.y * rcp(DOWNSAMPLE));
    #endif

    #if defined(BLUR_HIGH_QUALITY)
        // High quality 7-tap Gaussian with adaptive sampling
        float4 p0  = SAMPLE_BASEMAP(uv.xy                         );
        float4 p1a = SAMPLE_BASEMAP(uv.xy - (delta               ));
        float4 p1b = SAMPLE_BASEMAP(uv.xy + (delta               ));
        float4 p2a = SAMPLE_BASEMAP(uv.xy - (delta * 2.0         ));
        float4 p2b = SAMPLE_BASEMAP(uv.xy + (delta * 2.0         ));
        float4 p3a = SAMPLE_BASEMAP(uv.xy - (delta * 3.2307692308));
        float4 p3b = SAMPLE_BASEMAP(uv.xy + (delta * 3.2307692308));

        #if defined(BLUR_SAMPLE_CENTER_NORMAL)
            float3x3 camProj = (float3x3)unity_CameraProjection;
            float2 p11_22 = float2(camProj._11, camProj._22);
            float2 p13_31 = float2(camProj._13, camProj._23);
            float3 n0 = SampleNormal(uv, p11_22, p13_31);
        #else
            float3 n0 = GetPackedNormal(p0);
        #endif

        float w0 = 0.37004405286;
        float w1a = CompareNormal(n0, GetPackedNormal(p1a)) * 0.31718061674;
        float w1b = CompareNormal(n0, GetPackedNormal(p1b)) * 0.31718061674;
        float w2a = CompareNormal(n0, GetPackedNormal(p2a)) * 0.19823788546;
        float w2b = CompareNormal(n0, GetPackedNormal(p2b)) * 0.19823788546;
        float w3a = CompareNormal(n0, GetPackedNormal(p3a)) * 0.11453744493;
        float w3b = CompareNormal(n0, GetPackedNormal(p3b)) * 0.11453744493;

        float s;
        s  = GetPackedAO(p0)  * w0;
        s += GetPackedAO(p1a) * w1a;
        s += GetPackedAO(p1b) * w1b;
        s += GetPackedAO(p2a) * w2a;
        s += GetPackedAO(p2b) * w2b;
        s += GetPackedAO(p3a) * w3a;
        s += GetPackedAO(p3b) * w3b;

        s *= rcp(w0 + w1a + w1b + w2a + w2b + w3a + w3b);
    #else
        // Faster 5-tap Gaussian with linear sampling
        float4 p0  = SAMPLE_BASEMAP(uv.xy                         );
        float4 p1a = SAMPLE_BASEMAP(uv.xy - (delta * 1.3846153846));
        float4 p1b = SAMPLE_BASEMAP(uv.xy + (delta * 1.3846153846));
        float4 p2a = SAMPLE_BASEMAP(uv.xy - (delta * 3.2307692308));
        float4 p2b = SAMPLE_BASEMAP(uv.xy + (delta * 3.2307692308));

        #if defined(BLUR_SAMPLE_CENTER_NORMAL)
            float3x3 camProj = (float3x3)unity_CameraProjection;
            float2 p11_22 = float2(camProj._11, camProj._22);
            float2 p13_31 = float2(camProj._13, camProj._23);
            float3 n0 = SampleNormal(uv, p11_22, p13_31);
        #else
            float3 n0 = GetPackedNormal(p0);
        #endif

        float w0 = 0.2270270270;
        float w1a = CompareNormal(n0, GetPackedNormal(p1a)) * 0.3162162162;
        float w1b = CompareNormal(n0, GetPackedNormal(p1b)) * 0.3162162162;
        float w2a = CompareNormal(n0, GetPackedNormal(p2a)) * 0.0702702703;
        float w2b = CompareNormal(n0, GetPackedNormal(p2b)) * 0.0702702703;

        float s;
        s  = GetPackedAO(p0)  * w0;
        s += GetPackedAO(p1a) * w1a;
        s += GetPackedAO(p1b) * w1b;
        s += GetPackedAO(p2a) * w2a;
        s += GetPackedAO(p2b) * w2b;

        s *= rcp(w0 + w1a + w1b + w2a + w2b);
    #endif

    return PackAONormal(s, n0);
}


// Geometry-aware bilateral filter (single pass/small kernel)
float BlurSmall(TEXTURE2D_X_PARAM(tex, samp), float2 uv, float2 delta)
{
    float4 p0 = SAMPLE_TEXTURE2D_X(tex, samp, UnityStereoTransformScreenSpaceTex(uv                             ));
    float4 p1 = SAMPLE_TEXTURE2D_X(tex, samp, UnityStereoTransformScreenSpaceTex(uv + float2(-delta.x, -delta.y)));
    float4 p2 = SAMPLE_TEXTURE2D_X(tex, samp, UnityStereoTransformScreenSpaceTex(uv + float2( delta.x, -delta.y)));
    float4 p3 = SAMPLE_TEXTURE2D_X(tex, samp, UnityStereoTransformScreenSpaceTex(uv + float2(-delta.x,  delta.y)));
    float4 p4 = SAMPLE_TEXTURE2D_X(tex, samp, UnityStereoTransformScreenSpaceTex(uv + float2( delta.x,  delta.y)));

    float3 n0 = GetPackedNormal(p0);

    float w0 = 1.0;
    float w1 = CompareNormal(n0, GetPackedNormal(p1));
    float w2 = CompareNormal(n0, GetPackedNormal(p2));
    float w3 = CompareNormal(n0, GetPackedNormal(p3));
    float w4 = CompareNormal(n0, GetPackedNormal(p4));

    float s;
    s  = GetPackedAO(p0) * w0;
    s += GetPackedAO(p1) * w1;
    s += GetPackedAO(p2) * w2;
    s += GetPackedAO(p3) * w3;
    s += GetPackedAO(p4) * w4;

    return s *= rcp(w0 + w1 + w2 + w3 + w4);
}

// Final composition shader
float4 FragComposition(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = input.uv;

    float2 delta = _BaseMap_TexelSize.xy * rcp(DOWNSAMPLE);
    float ao = BlurSmall(TEXTURE2D_X_ARGS(_BaseMap, sampler_BaseMap), uv, delta);

    return EncodeAO(1.0 - ao);
}

#if !SHADER_API_GLES // Excluding the MRT pass under GLES2
    struct CompositionOutput
    {
        float4 gbuffer0 : SV_Target0;
        float4 gbuffer3 : SV_Target1;
    };

    CompositionOutput FragCompositionGBuffer(Varyings i)
    {
        float2 delta = _BaseMap_TexelSize.xy * rcp(DOWNSAMPLE);
        float ao = BlurSmall(TEXTURE2D_X_ARGS(_ScreenSpaceOcclusionTexture, sampler_ScreenSpaceOcclusionTexture), i.uv.xy, delta);

        CompositionOutput o;
        o.gbuffer0 = float4(0.0, 0.0, 0.0, ao);
        o.gbuffer3 = float4((float3)EncodeAO(ao), 0.0);
        return o;
    }
#else
    float4 FragCompositionGBuffer(Varyings i) : SV_Target
    {
        return (0.0).xxxx;
    }
#endif

#endif //UNIVERSAL_SSAO_INCLUDED
