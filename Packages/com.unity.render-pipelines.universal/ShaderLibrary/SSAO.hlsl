#ifndef UNIVERSAL_SSAO_INCLUDED
#define UNIVERSAL_SSAO_INCLUDED

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

// Textures & Samplers
TEXTURE2D_HALF(_BlueNoiseTexture);
TEXTURE2D_X_HALF(_ScreenSpaceOcclusionTexture);

SAMPLER(sampler_BlitTexture);

// Params
half4 _BlurOffset;
half _KawaseBlurIteration;
int _LastKawasePass;
half4 _SSAOParams;
float4 _CameraViewTopLeftCorner[2];
float4x4 _CameraViewProjections[2]; // This is different from UNITY_MATRIX_VP (platform-agnostic projection matrix is used). Handle both non-XR and XR modes.

float4 _SourceSize;
float4 _ProjectionParams2;
float4 _CameraViewXExtent[2];
float4 _CameraViewYExtent[2];
float4 _CameraViewZExtent[2];

// SSAO Settings
#define INTENSITY _SSAOParams.x
#define RADIUS _SSAOParams.y
#define DOWNSAMPLE _SSAOParams.z
#define FALLOFF _SSAOParams.w

#if defined(_BLUE_NOISE)
half4 _SSAOBlueNoiseParams;
#define BlueNoiseScale          _SSAOBlueNoiseParams.xy
#define BlueNoiseOffset         _SSAOBlueNoiseParams.zw
#endif

#if defined(SHADER_API_GLES) && !defined(SHADER_API_GLES3)
    static const int SAMPLE_COUNT = 3;
#elif defined(_SAMPLE_COUNT_HIGH)
    static const int SAMPLE_COUNT = 12;
#elif defined(_SAMPLE_COUNT_MEDIUM)
    static const int SAMPLE_COUNT = 8;
#else
    static const int SAMPLE_COUNT = 4;
#endif
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

// Function defines
#define SCREEN_PARAMS               GetScaledScreenParams()
#define SAMPLE_BASEMAP(uv)          half4(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, UnityStereoTransformScreenSpaceTex(uv)));
#define SAMPLE_BASEMAP_R(uv)        half(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, UnityStereoTransformScreenSpaceTex(uv)).r);
#define SAMPLE_BLUE_NOISE(uv)       SAMPLE_TEXTURE2D(_BlueNoiseTexture, sampler_PointRepeat, UnityStereoTransformScreenSpaceTex(uv)).a;

// Constants
// kContrast determines the contrast of occlusion. This allows users to control over/under
// occlusion. At the moment, this is not exposed to the editor because it's rarely useful.
// The range is between 0 and 1.
static const half kContrast = half(0.6);

// The constant below controls the geometry-awareness of the bilateral
// filter. The higher value, the more sensitive it is.
static const half kGeometryCoeff = half(0.8);

// The constants below are used in the AO estimator. Beta is mainly used for suppressing
// self-shadowing noise, and Epsilon is used to prevent calculation underflow. See the paper
// (Morgan 2011 https://casual-effects.com/research/McGuire2011AlchemyAO/index.html)
// for further details of these constants.
static const half kBeta = half(0.004);
static const half kEpsilon = half(0.0001);

static const float SKY_DEPTH_VALUE  = 0.00001;
static const half  HALF_POINT_ONE   = half(0.1);
static const half  HALF_MINUS_ONE   = half(-1.0);
static const half  HALF_ZERO        = half(0.0);
static const half  HALF_HALF        = half(0.5);
static const half  HALF_ONE         = half(1.0);
static const half4 HALF4_ONE        = half4(1.0, 1.0, 1.0, 1.0);
static const half  HALF_TWO         = half(2.0);
static const half  HALF_TWO_PI      = half(6.28318530717958647693);
static const half  HALF_FOUR        = half(4.0);
static const half  HALF_NINE        = half(9.0);
static const half  HALF_HUNDRED     = half(100.0);


#if defined(USING_STEREO_MATRICES)
    #define unity_eyeIndex unity_StereoEyeIndex
#else
    #define unity_eyeIndex 0
#endif


half4 PackAONormal(half ao, half3 n)
{
    n *= HALF_HALF;
    n += HALF_HALF;
    return half4(ao, n);
}

half3 GetPackedNormal(half4 p)
{
    return p.gba * HALF_TWO - HALF_ONE;
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
    return smoothstep(kGeometryCoeff, HALF_ONE, dot(d1, d2));
}

float2 GetScreenSpacePosition(float2 uv)
{
    return float2(uv * SCREEN_PARAMS.xy * DOWNSAMPLE);
}

// Pseudo random number generator
half GetRandomVal(half u, half sampleIndex)
{
    return SSAORandomUV[u * 20 + sampleIndex];
}

// Sample point picker
half3 PickSamplePoint(float2 uv, int sampleIndex, half sampleIndexHalf, half rcpSampleCount, half3 normal_o)
{
    #if defined(_BLUE_NOISE)
        const half lerpVal = sampleIndexHalf * rcpSampleCount;
        const half noise = SAMPLE_BLUE_NOISE(((uv + BlueNoiseOffset) * BlueNoiseScale) + lerpVal);
        const half u = frac(GetRandomVal(HALF_ZERO, sampleIndexHalf).x + noise) * HALF_TWO - HALF_ONE;
        const half theta = (GetRandomVal(HALF_ONE, sampleIndexHalf).x + noise) * HALF_TWO_PI * HALF_HUNDRED;
        const half u2 = half(sqrt(HALF_ONE - u * u));

        half3 v = half3(u2 * cos(theta), u2 * sin(theta), u);
        v *= (dot(normal_o, v) >= HALF_ZERO) * HALF_TWO - HALF_ONE;
        v *= lerp(0.1, 1.0, lerpVal * lerpVal);
    #else
        const float2 positionSS = GetScreenSpacePosition(uv);
        const half noise = half(InterleavedGradientNoise(positionSS, sampleIndex));
        const half u = frac(GetRandomVal(HALF_ZERO, sampleIndex) + noise) * HALF_TWO - HALF_ONE;
        const half theta = (GetRandomVal(HALF_ONE, sampleIndex) + noise) * HALF_TWO_PI;
        const half u2 = half(sqrt(HALF_ONE - u * u));

        half3 v = half3(u2 * cos(theta), u2 * sin(theta), u);
        v *= sqrt((sampleIndexHalf + HALF_ONE) * rcpSampleCount);
        v = faceforward(v, -normal_o, v);
    #endif

    return v * RADIUS;
}

float SampleDepth(float2 uv)
{
    return SampleSceneDepth(uv.xy);
}

float GetLinearEyeDepth(float rawDepth)
{
#if defined(_ORTHOGRAPHIC)
    return LinearDepthToEyeDepth(rawDepth);
#else
    return LinearEyeDepth(rawDepth, _ZBufferParams);
#endif
}

float SampleAndGetLinearEyeDepth(float2 uv)
{
    const float rawDepth = SampleDepth(uv);
    return GetLinearEyeDepth(rawDepth);
}

// This returns a vector in world unit (not a position), from camera to the given point described by uv screen coordinate and depth (in absolute world unit).
half3 ReconstructViewPos(float2 uv, float linearDepth)
{
    // Screen is y-inverted.
    uv.y = 1.0 - uv.y;

    // view pos in world space
    #if defined(_ORTHOGRAPHIC)
        float zScale = linearDepth * _ProjectionParams.w; // divide by far plane
        float3 viewPos = _CameraViewTopLeftCorner[unity_eyeIndex].xyz
                            + _CameraViewXExtent[unity_eyeIndex].xyz * uv.x
                            + _CameraViewYExtent[unity_eyeIndex].xyz * uv.y
                            + _CameraViewZExtent[unity_eyeIndex].xyz * zScale;
    #else
        float zScale = linearDepth * _ProjectionParams2.x; // divide by near plane
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
half3 ReconstructNormal(float2 uv, float linearDepth, float3 vpos)
{
    #if defined(_SOURCE_DEPTH_LOW)
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
        #if defined(_SOURCE_DEPTH_MEDIUM)
             uint closest_horizontal = l1.z > r1.z ? 0 : 1;
             uint closest_vertical   = d1.z > u1.z ? 0 : 1;
        #else
            float3 l2 = float3(uv + lUV * 2.0, 0.0); l2.z = SampleAndGetLinearEyeDepth(l2.xy); // Left2
            float3 r2 = float3(uv + rUV * 2.0, 0.0); r2.z = SampleAndGetLinearEyeDepth(r2.xy); // Right2
            float3 u2 = float3(uv + uUV * 2.0, 0.0); u2.z = SampleAndGetLinearEyeDepth(u2.xy); // Up2
            float3 d2 = float3(uv + dUV * 2.0, 0.0); d2.z = SampleAndGetLinearEyeDepth(d2.xy); // Down2

            const uint closest_horizontal = abs( (2.0 * l1.z - l2.z) - linearDepth) < abs( (2.0 * r1.z - r2.z) - linearDepth) ? 0 : 1;
            const uint closest_vertical   = abs( (2.0 * d1.z - d2.z) - linearDepth) < abs( (2.0 * u1.z - u2.z) - linearDepth) ? 0 : 1;
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
            P1 = half3(closest_horizontal == 0 ? l1 : d1);
            P2 = half3(closest_horizontal == 0 ? d1 : r1);
        }
        else
        {
            P1 = half3(closest_horizontal == 0 ? u1 : r1);
            P2 = half3(closest_horizontal == 0 ? l1 : u1);
        }

        // Use the cross product to calculate the normal...
        return half3(normalize(cross(ReconstructViewPos(P2.xy, P2.z) - vpos, ReconstructViewPos(P1.xy, P1.z) - vpos)));
    #endif
}

half3 SampleNormal(float2 uv, float linearDepth)
{
    #if defined(_SOURCE_DEPTH_NORMALS)
        return half3(SampleSceneNormals(uv));
    #else
        float3 vpos = ReconstructViewPos(uv, linearDepth);
        return ReconstructNormal(uv, linearDepth, vpos);
    #endif
}

// Distance-based AO estimator based on Morgan 2011
// "Alchemy screen-space ambient obscurance algorithm"
// http://graphics.cs.williams.edu/papers/AlchemyHPG11/
half4 SSAO(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = input.texcoord;

    // Early Out for Sky...
    float rawDepth_o = SampleDepth(uv);
    if (rawDepth_o < SKY_DEPTH_VALUE)
        return PackAONormal(HALF_ZERO, HALF_ZERO);

    // Early Out for Falloff
    float linearDepth_o = GetLinearEyeDepth(rawDepth_o);
    half halfLinearDepth_o = half(linearDepth_o);
    if (halfLinearDepth_o > FALLOFF)
        return PackAONormal(HALF_ZERO, HALF_ZERO);

    // Normal for this fragment
    half3 normal_o = SampleNormal(uv, linearDepth_o);

    // View position for this fragment
    float3 vpos_o = ReconstructViewPos(uv, linearDepth_o);

    // Parameters used in coordinate conversion
    half3 camTransform000102 = half3(_CameraViewProjections[unity_eyeIndex]._m00, _CameraViewProjections[unity_eyeIndex]._m01, _CameraViewProjections[unity_eyeIndex]._m02);
    half3 camTransform101112 = half3(_CameraViewProjections[unity_eyeIndex]._m10, _CameraViewProjections[unity_eyeIndex]._m11, _CameraViewProjections[unity_eyeIndex]._m12);

    const half rcpSampleCount = half(rcp(SAMPLE_COUNT));
    half ao = HALF_ZERO;
    half sHalf = HALF_MINUS_ONE;
    UNITY_UNROLL
    for (int s = 0; s < SAMPLE_COUNT; s++)
    {
        sHalf += HALF_ONE;

        // Sample point
        half3 v_s1 = PickSamplePoint(uv, s, sHalf, rcpSampleCount, normal_o);
        half3 vpos_s1 = half3(vpos_o + v_s1);
        half2 spos_s1 = half2(
            camTransform000102.x * vpos_s1.x + camTransform000102.y * vpos_s1.y + camTransform000102.z * vpos_s1.z,
            camTransform101112.x * vpos_s1.x + camTransform101112.y * vpos_s1.y + camTransform101112.z * vpos_s1.z
        );

        half zDist = HALF_ZERO;
        #if defined(_ORTHOGRAPHIC)
            zDist = halfLinearDepth_o;
            half2 uv_s1_01 = saturate((spos_s1 + HALF_ONE) * HALF_HALF);
        #else
            zDist = half(-dot(UNITY_MATRIX_V[2].xyz, vpos_s1));
            half2 uv_s1_01 = saturate(half2(spos_s1 * rcp(zDist) + HALF_ONE) * HALF_HALF);
        #endif

        // Relative depth of the sample point
        float rawDepth_s = SampleDepth(uv_s1_01);
        float linearDepth_s = GetLinearEyeDepth(rawDepth_s);

        // We need to make sure we not use the AO value if the sample point it's outside the radius or if it's the sky...
        half halfLinearDepth_s = half(linearDepth_s);
        half isInsideRadius = abs(zDist - halfLinearDepth_s) < RADIUS ? 1.0 : 0.0;
        isInsideRadius *= rawDepth_s > SKY_DEPTH_VALUE ? 1.0 : 0.0;

        // Relative postition of the sample point
        half3 v_s2 = half3(ReconstructViewPos(uv_s1_01, linearDepth_s) - vpos_o);

        // Estimate the obscurance value
        half dotVal = dot(v_s2, normal_o) - kBeta * halfLinearDepth_o;
        half a1 = max(dotVal, HALF_ZERO);
        half a2 = dot(v_s2, v_s2) + kEpsilon;
        ao += a1 * rcp(a2) * isInsideRadius;
    }

    // Intensity normalization
    ao *= RADIUS;

    // Calculate falloff...
    half falloff = HALF_ONE - halfLinearDepth_o * half(rcp(FALLOFF));
    falloff = falloff*falloff;

    // Apply contrast + intensity + falloff^2
    ao = PositivePow(saturate(ao * INTENSITY * falloff * rcpSampleCount), kContrast);

    // Return the packed ao + normals
    return PackAONormal(ao, normal_o);
}


// ------------------------------------------------------------------
// Bilateral Blur
// ------------------------------------------------------------------

// Geometry-aware separable bilateral filter
half4 Blur(const float2 uv, const float2 delta) : SV_Target
{
    half4 p0 =  SAMPLE_BASEMAP(uv                       );
    half4 p1a = SAMPLE_BASEMAP(uv - delta * 1.3846153846);
    half4 p1b = SAMPLE_BASEMAP(uv + delta * 1.3846153846);
    half4 p2a = SAMPLE_BASEMAP(uv - delta * 3.2307692308);
    half4 p2b = SAMPLE_BASEMAP(uv + delta * 3.2307692308);

    half3 n0 = GetPackedNormal(p0);

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
half BlurSmall(const float2 uv, const float2 delta)
{
    half4 p0 = SAMPLE_BASEMAP(uv                            );
    half4 p1 = SAMPLE_BASEMAP(uv + float2(-delta.x, -delta.y));
    half4 p2 = SAMPLE_BASEMAP(uv + float2( delta.x, -delta.y));
    half4 p3 = SAMPLE_BASEMAP(uv + float2(-delta.x,  delta.y));
    half4 p4 = SAMPLE_BASEMAP(uv + float2( delta.x,  delta.y));

    half3 n0 = GetPackedNormal(p0);

    half w0 = HALF_ONE;
    half w1 = CompareNormal(n0, GetPackedNormal(p1));
    half w2 = CompareNormal(n0, GetPackedNormal(p2));
    half w3 = CompareNormal(n0, GetPackedNormal(p3));
    half w4 = CompareNormal(n0, GetPackedNormal(p4));

    half s = HALF_ZERO;
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

    const float2 uv = input.texcoord;
    const float2 delta = float2(_SourceSize.z * rcp(DOWNSAMPLE), 0.0);
    return Blur(uv, delta);
}

half4 VerticalBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    const float2 uv = input.texcoord;
    const float2 delta = float2(0.0, _SourceSize.w * rcp(DOWNSAMPLE));
    return Blur(uv, delta);
}

half4 FinalBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    const float2 uv = input.texcoord;
    const float2 delta = _SourceSize.zw * rcp(DOWNSAMPLE);
    return HALF_ONE - BlurSmall(uv, delta );
}


// ------------------------------------------------------------------
// Gaussian Blur
// ------------------------------------------------------------------

// https://software.intel.com/content/www/us/en/develop/blogs/an-investigation-of-fast-real-time-gpu-based-image-blur-algorithms.html
half GaussianBlur(half2 uv, half2 pixelOffset)
{
    half colOut = 0;

    // Kernel width 7 x 7
    const int stepCount = 2;

    const half gWeights[stepCount] ={
       0.44908,
       0.05092
    };
    const half gOffsets[stepCount] ={
       0.53805,
       2.06278
    };

    UNITY_UNROLL
    for( int i = 0; i < stepCount; i++ )
    {
        half2 texCoordOffset = gOffsets[i] * pixelOffset;
        half4 p1 = SAMPLE_BASEMAP(uv + texCoordOffset);
        half4 p2 = SAMPLE_BASEMAP(uv - texCoordOffset);
        half col = p1.r + p2.r;
        colOut += gWeights[i] * col;
    }

    return colOut;
}

half HorizontalGaussianBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half2 uv = input.texcoord;
    half2 delta = half2(_SourceSize.z * rcp(DOWNSAMPLE), HALF_ZERO);

    return GaussianBlur(uv, delta);
}

half VerticalGaussianBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half2 uv = input.texcoord;
    half2 delta = half2(HALF_ZERO, _SourceSize.w * rcp(DOWNSAMPLE));

    return HALF_ONE - GaussianBlur(uv, delta);
}


// ------------------------------------------------------------------
// Kawase Blur
// ------------------------------------------------------------------

///////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Developed by Masaki Kawase, Bunkasha Games
// Used in DOUBLE-S.T.E.A.L. (aka Wreckless)
// From his GDC2003 Presentation: Frame Buffer Postprocessing Effects in  DOUBLE-S.T.E.A.L (Wreckless)
///////////////////////////////////////////////////////////////////////////////////////////////////////////////
half KawaseBlurFilter( half2 texCoord, half2 pixelSize, half iteration )
{
    half2 texCoordSample;
    half2 halfPixelSize = pixelSize * HALF_HALF;
    half2 dUV = ( pixelSize.xy * half2( iteration, iteration ) ) + halfPixelSize.xy;

    half cOut;

    // Sample top left pixel
    texCoordSample.x = texCoord.x - dUV.x;
    texCoordSample.y = texCoord.y + dUV.y;

    cOut = SAMPLE_BASEMAP_R(texCoordSample);

    // Sample top right pixel
    texCoordSample.x = texCoord.x + dUV.x;
    texCoordSample.y = texCoord.y + dUV.y;

    cOut += SAMPLE_BASEMAP_R(texCoordSample);

    // Sample bottom right pixel
    texCoordSample.x = texCoord.x + dUV.x;
    texCoordSample.y = texCoord.y - dUV.y;
    cOut += SAMPLE_BASEMAP_R(texCoordSample);

    // Sample bottom left pixel
    texCoordSample.x = texCoord.x - dUV.x;
    texCoordSample.y = texCoord.y - dUV.y;

    cOut += SAMPLE_BASEMAP_R(texCoordSample);

    // Average
    cOut *= half(0.25);

    return cOut;
}

half KawaseBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half2 uv = input.texcoord;
    half2 texelSize = _SourceSize.zw * rcp(DOWNSAMPLE);

    half col = KawaseBlurFilter(uv, texelSize, _KawaseBlurIteration);

    if (_LastKawasePass)
        col = HALF_ONE - col;

    return col;
}


#endif //UNIVERSAL_SSAO_INCLUDED
