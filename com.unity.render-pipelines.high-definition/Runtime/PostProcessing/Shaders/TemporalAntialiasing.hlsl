
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

#define CLAMP_MAX       65472.0 // HALF_MAX minus one (2 - 2^-9) * 2^15

// Legacy defines, DON'T USE IN NEW PASSES THAT USE TEMPORAL AA
#define RADIUS              0.75

#if !defined(CTYPE)
    #define CTYPE float3
    #define CTYPE_SWIZZLE xyz
#endif


#if UNITY_REVERSED_Z
    #define COMPARE_DEPTH(a, b) step(b, a)
#else
    #define COMPARE_DEPTH(a, b) step(a, b)
#endif


float3 Fetch(TEXTURE2D_X(tex), float2 coords, float2 offset, float2 scale)
{
    float2 uv = (coords + offset * _ScreenSize.zw) * scale;
    return SAMPLE_TEXTURE2D_X_LOD(tex, s_linear_clamp_sampler, uv, 0).xyz;
}

float4 Fetch4(TEXTURE2D_X(tex), float2 coords, float2 offset, float2 scale)
{
    float2 uv = (coords + offset * _ScreenSize.zw) * scale;
    return SAMPLE_TEXTURE2D_X_LOD(tex, s_linear_clamp_sampler, uv, 0);
}

float4 Fetch4Array(Texture2DArray tex, uint slot, float2 coords, float2 offset, float2 scale)
{
    float2 uv = (coords + offset * _ScreenSize.zw) * scale;
    return SAMPLE_TEXTURE2D_ARRAY_LOD(tex, s_linear_clamp_sampler, uv, slot, 0);
}

// ---------------------------------------------------
// Options
// ---------------------------------------------------

// History sampling options
#define BILINEAR 0
#define BICUBIC_5TAP 1

/// Neighbourhood sampling options
#define PLUS 0    // Faster! Can allow for read across twice (paying cost of 2 samples only)
#define CROSS 1   // Can only do one fast read diagonal 
#define SMALL_NEIGHBOURHOOD_SHAPE PLUS

// Neighbourhood AABB options
#define MINMAX 0
#define VARIANCE 1

// Central value filtering options
#define NO_FILTERING 0
#define BOX_FILTER 1
#define BLACKMAN_HARRIS 2

// Clip option
#define DIRECT_CLIP 0
#define BLEND_WITH_CLIP 1
#define SIMPLE_CLAMP 2


// Set defines in case not set outside the include
#ifndef YCOCG
    #define YCOCG 1
#endif

#ifndef HISTORY_SAMPLING_METHOD
    #define HISTORY_SAMPLING_METHOD BILINEAR
#endif

#ifndef NEIGHBOUROOD_CORNER_METHOD
    #define NEIGHBOUROOD_CORNER_METHOD VARIANCE
#endif

#ifndef WIDE_NEIGHBOURHOOD
    #define WIDE_NEIGHBOURHOOD 0
#endif

#ifndef CENTRAL_FILTERING
    #define CENTRAL_FILTERING NO_FILTERING
#endif

#ifndef CENTRAL_FILTERING
    #define CENTRAL_FILTERING DIRECT_CLIP
#endif

#ifndef ANTI_FLICKER
    #define ANTI_FLICKER 1
#endif

#ifndef VELOCITY_REJECTION
    #define VELOCITY_REJECTION 0
#endif

#ifndef PERCEPTUAL_SPACE
    #define PERCEPTUAL_SPACE 1
#endif

// ---------------------------------------------------
// Utilities functions
// ---------------------------------------------------

float3 Min3Float3(float3 a, float3 b, float3 c)
{
    return float3(Min3(a.x, b.x, c.x),
        Min3(a.y, b.y, c.y),
        Min3(a.z, b.z, c.z));
}

float3 Max3Float3(float3 a, float3 b, float3 c)
{
    return float3(Max3(a.x, b.x, c.x),
        Max3(a.y, b.y, c.y),
        Max3(a.z, b.z, c.z));
}

float4 Min3Float4(float4 a, float4 b, float4 c)
{
    return float4(Min3(a.x, b.x, c.x),
        Min3(a.y, b.y, c.y),
        Min3(a.z, b.z, c.z),
        Min3(a.w, b.w, c.w));
}

float4 Max3Float4(float4 a, float4 b, float4 c)
{
    return float4(Max3(a.x, b.x, c.x),
        Max3(a.y, b.y, c.y),
        Max3(a.z, b.z, c.z),
        Max3(a.w, b.w, c.w));
}

CTYPE Max3Color(CTYPE a, CTYPE b, CTYPE c)
{
#ifdef ENABLE_ALPHA
    return Max3Float4(a, b, c);
#else
    return Max3Float3(a, b, c);
#endif
}

CTYPE Min3Color(CTYPE a, CTYPE b, CTYPE c)
{
#ifdef ENABLE_ALPHA
    return Min3Float4(a, b, c);
#else
    return Min3Float3(a, b, c);
#endif
}

// Define Quad communication functions
CTYPE QuadReadColorAcrossX(CTYPE c, int2 positionSS)
{
#ifdef ENABLE_ALPHA
    return QuadReadFloat4AcrossX(c, positionSS);
#else
    return QuadReadFloat3AcrossX(c, positionSS);
#endif
}

CTYPE QuadReadColorAcrossY(CTYPE c, int2 positionSS)
{
#ifdef ENABLE_ALPHA
    return QuadReadFloat4AcrossY(c, positionSS);
#else
    return QuadReadFloat3AcrossY(c, positionSS);
#endif
}

CTYPE QuadReadColorAcrossDiagonal(CTYPE c, int2 positionSS)
{
#ifdef ENABLE_ALPHA
    return QuadReadFloat4AcrossDiagonal(c, positionSS);
#else
    return QuadReadFloat3AcrossDiagonal(c, positionSS);
#endif
}

// ---------------------------------------------------
// Color related utilities.
// ---------------------------------------------------


float GetLuma(CTYPE color)
{
#if YCOCG
    // We work in YCoCg hence the luminance is in the first channel.
    return color.x;
#else
    return Luminance(color.xyz);
#endif
}

CTYPE ReinhardToneMap(CTYPE c)
{
    return c * rcp(GetLuma(c) + 1.0);
}

float PerceptualWeight(CTYPE c)
{
#if PERCEPTUAL_SPACE
    return rcp(GetLuma(c) + 1.0);
#else
    return 1;
#endif
}

CTYPE InverseReinhardToneMap(CTYPE c)
{
    return c * rcp(1.0 - GetLuma(c));
}

float PerceptualInvWeight(CTYPE c)
{
#if PERCEPTUAL_SPACE
    return rcp(1.0 - GetLuma(c));
#else
    return 1;
#endif
}

CTYPE ConvertToWorkingSpace(CTYPE rgb)
{
#if YCOCG
    float3 ycocg = RGBToYCoCg(rgb.xyz);

    #if ENABLE_ALPHA
        return float4(ycocg, rgb.a);
    #else
        return ycocg;
    #endif

#else
    return rgb;
#endif

}
float3 ConvertToOutputSpace(float3 color)
{
#if YCOCG
    return YCoCgToRGB(color);
#else
    return color;
#endif
}

// ---------------------------------------------------
// Velocity related functions.
// ---------------------------------------------------

// Front most neighbourhood velocity ([Karis 2014])
float2 GetClosestFragment(TEXTURE2D_X(DepthTexture), int2 positionSS)
{
    float center = LOAD_TEXTURE2D_X_LOD(DepthTexture, positionSS, 0).r;

    int2 quadOffset = GetQuadOffset(positionSS);

    int2 fastOffset = -quadOffset;
    int2 offset1 = int2(-quadOffset.x, quadOffset.y);
    int2 offset2 = int2(quadOffset.x, -quadOffset.y);
    int2 offset3 = quadOffset;

    float s3 = LOAD_TEXTURE2D_X_LOD(DepthTexture, positionSS + offset3, 0).r;
    float s2 = LOAD_TEXTURE2D_X_LOD(DepthTexture, positionSS + offset2, 0).r;
    float s1 = LOAD_TEXTURE2D_X_LOD(DepthTexture, positionSS + offset1, 0).r;
    float s0 = QuadReadAcrossDiagonal(center, positionSS);

    float3 closest = float3(0.0, 0.0, center);
    closest = COMPARE_DEPTH(s0, closest.z) ? float3(fastOffset, s0) : closest;
    closest = COMPARE_DEPTH(s3, closest.z) ? float3(offset3, s3) : closest;
    closest = COMPARE_DEPTH(s2, closest.z) ? float3(offset2, s2) : closest;
    closest = COMPARE_DEPTH(s1, closest.z) ? float3(offset1, s1) : closest;

    return positionSS + closest.xy;
}

// Used since some compute might want to call this and we cannot use Quad reads in that case.
float2 GetClosestFragmentCompute(float2 positionSS)
{
    float center = LoadCameraDepth(positionSS);
    float nw = LoadCameraDepth(positionSS + int2(-1, -1));
    float ne = LoadCameraDepth(positionSS + int2(1, -1));
    float sw = LoadCameraDepth(positionSS + int2(-1, 1));
    float se = LoadCameraDepth(positionSS + int2(1, 1));

    float4 neighborhood = float4(nw, ne, sw, se);

    float3 closest = float3(0.0, 0.0, center);
    closest = lerp(closest, float3(-1, -1, neighborhood.x), COMPARE_DEPTH(neighborhood.x, closest.z));
    closest = lerp(closest, float3(1, -1, neighborhood.y), COMPARE_DEPTH(neighborhood.y, closest.z));
    closest = lerp(closest, float3(-1, 1, neighborhood.z), COMPARE_DEPTH(neighborhood.z, closest.z));
    closest = lerp(closest, float3(1, 1, neighborhood.w), COMPARE_DEPTH(neighborhood.w, closest.z));

    return positionSS + closest.xy;
}


float ModifyBlendWithMotionVectorRejection(TEXTURE2D_X(VelocityMagnitudeTexture), float mvLen, float2 prevUV, float blendFactor, float speedRejectionFactor)
{
    // TODO: This needs some refinement, it can lead to some annoying flickering coming back on strong camera movement.
#if VELOCITY_REJECTION

    float prevMVLen = Fetch(VelocityMagnitudeTexture, prevUV, 0, _RTHandleScaleHistory.zw).x;
    float diff = abs(mvLen - prevMVLen);

    // We don't start rejecting until we have the equivalent of around 40 texels in 1080p
    diff -= 0.015935382;
    float val = saturate(diff * speedRejectionFactor);
    return lerp(blendFactor, 1.0, val*val);

#else
    return blendFactor;
#endif
}

// ---------------------------------------------------
// History sampling 
// ---------------------------------------------------

CTYPE HistoryBilinear(TEXTURE2D_X(HistoryTexture), float2 UV)
{
    CTYPE color = Fetch4(HistoryTexture, UV, 0.0, _RTHandleScaleHistory.zw).CTYPE_SWIZZLE;
    return color;
}

// From Filmic SMAA presentation[Jimenez 2016]
// A bit more verbose that it needs to be, but makes it a bit better at latency hiding
CTYPE HistoryBicubic5Tap(TEXTURE2D_X(HistoryTexture), float2 UV, float sharpening, float4 historyBufferInfo)
{
    float2 samplePos = UV * historyBufferInfo.xy;
    float2 tc1 = floor(samplePos - 0.5) + 0.5;
    float2 f = samplePos - tc1;
    float2 f2 = f * f;
    float2 f3 = f * f2;

    const float c = sharpening;

    float2 w0 = -c         * f3 +  2.0 * c         * f2 - c * f;
    float2 w1 =  (2.0 - c) * f3 - (3.0 - c)        * f2          + 1.0;
    float2 w2 = -(2.0 - c) * f3 + (3.0 - 2.0 * c)  * f2 + c * f;
    float2 w3 = c          * f3 - c                * f2;

    float2 w12 = w1 + w2;
    float2 tc0 = historyBufferInfo.zw   * (tc1 - 1.0);
    float2 tc3 = historyBufferInfo.zw   * (tc1 + 2.0);
    float2 tc12 = historyBufferInfo.zw  * (tc1 + w2 / w12);

    CTYPE s0 = Fetch4(HistoryTexture, float2(tc12.x, tc0.y), 0.0, _RTHandleScaleHistory.zw).CTYPE_SWIZZLE;
    CTYPE s1 = Fetch4(HistoryTexture, float2(tc0.x, tc12.y), 0.0, _RTHandleScaleHistory.zw).CTYPE_SWIZZLE;
    CTYPE s2 = Fetch4(HistoryTexture, float2(tc12.x, tc12.y), 0.0, _RTHandleScaleHistory.zw).CTYPE_SWIZZLE;
    CTYPE s3 = Fetch4(HistoryTexture, float2(tc3.x, tc0.y), 0.0, _RTHandleScaleHistory.zw).CTYPE_SWIZZLE;
    CTYPE s4 = Fetch4(HistoryTexture, float2(tc12.x, tc3.y), 0.0, _RTHandleScaleHistory.zw).CTYPE_SWIZZLE;

    float cw0 = (w12.x * w0.y);
    float cw1 = (w0.x * w12.y);
    float cw2 = (w12.x * w12.y);
    float cw3 = (w3.x * w12.y);
    float cw4 = (w12.x *  w3.y);

#ifdef ANTI_RINGING
    CTYPE min = Min3Color(s0, s1, s2);
    min = Min3Color(min, s3, s4);

    CTYPE max = Max3Color(s0, s1, s2);
    max = Max3Color(max, s3, s4);
#endif

    s0 *= cw0;
    s1 *= cw1;
    s2 *= cw2;
    s3 *= cw3;
    s4 *= cw4;

    CTYPE historyFiltered = s0 + s1 + s2 + s3 + s4;
    float weightSum = cw0 + cw1 + cw2 + cw3 + cw4;

    CTYPE filteredVal = historyFiltered.CTYPE_SWIZZLE * rcp(weightSum);

#if ANTI_RINGING
    // This sortof neighbourhood clamping seems to work to avoid the appearance of overly dark outlines in case
    // sharpening of history is too strong.
    return clamp(filteredVal, min, max);
#endif

    return filteredVal;
}


CTYPE GetFilteredHistory(TEXTURE2D_X(HistoryTexture), float2 UV, float sharpening, float4 historyBufferInfo)
{
    CTYPE history = 0;

#if (HISTORY_SAMPLING_METHOD == BILINEAR || defined(FORCE_BILINEAR_HISTORY))
    history = HistoryBilinear(HistoryTexture, UV);
#elif HISTORY_SAMPLING_METHOD == BICUBIC_5TAP
    history = HistoryBicubic5Tap(HistoryTexture, UV, sharpening, historyBufferInfo);
#endif

    history = clamp(history, 0, CLAMP_MAX);

    return ConvertToWorkingSpace(history);
}

// ---------------------------------------------------
// Neighbourhood related.
// ---------------------------------------------------
#define SMALL_NEIGHBOURHOOD_SIZE 4 
#define NEIGHBOUR_COUNT ((WIDE_NEIGHBOURHOOD == 0) ? SMALL_NEIGHBOURHOOD_SIZE : 8)

struct NeighbourhoodSamples
{
#if WIDE_NEIGHBOURHOOD
    CTYPE neighbours[8];
#else
    CTYPE neighbours[4];
#endif

    CTYPE central;
    CTYPE minNeighbour;
    CTYPE maxNeighbour;
    CTYPE avgNeighbour;
};


void ConvertNeighboursToPerceptualSpace(inout NeighbourhoodSamples samples)
{
    samples.neighbours[0].xyz *= PerceptualWeight(samples.neighbours[0]);
    samples.neighbours[1].xyz *= PerceptualWeight(samples.neighbours[1]);
    samples.neighbours[2].xyz *= PerceptualWeight(samples.neighbours[2]);
    samples.neighbours[3].xyz *= PerceptualWeight(samples.neighbours[3]);
#if WIDE_NEIGHBOURHOOD
    samples.neighbours[4].xyz *= PerceptualWeight(samples.neighbours[4]);
    samples.neighbours[5].xyz *= PerceptualWeight(samples.neighbours[5]);
    samples.neighbours[6].xyz *= PerceptualWeight(samples.neighbours[6]);
    samples.neighbours[7].xyz *= PerceptualWeight(samples.neighbours[7]);
#endif
    samples.central.xyz       *= PerceptualWeight(samples.central);
}

void GatherNeighbourhood(TEXTURE2D_X(InputTexture), float2 UV, float2 positionSS, CTYPE centralColor, out NeighbourhoodSamples samples)
{
    samples = (NeighbourhoodSamples)0;

    samples.central = centralColor;

    float2 quadOffset = GetQuadOffset(positionSS);

#if WIDE_NEIGHBOURHOOD

    // Plus shape
    samples.neighbours[0] = ConvertToWorkingSpace(Fetch4(InputTexture, UV, float2(0.0f, quadOffset.y), _RTHandleScale.xy).CTYPE_SWIZZLE);
    samples.neighbours[1] = ConvertToWorkingSpace(Fetch4(InputTexture, UV, float2(quadOffset.x, 0.0f), _RTHandleScale.xy).CTYPE_SWIZZLE);
    samples.neighbours[2] = QuadReadColorAcrossX(centralColor, positionSS);
    samples.neighbours[3] = QuadReadColorAcrossY(centralColor, positionSS);

    // Cross shape
    int2 fastOffset = -quadOffset;
    int2 offset1 = int2(-quadOffset.x, quadOffset.y);
    int2 offset2 = int2(quadOffset.x, -quadOffset.y);
    int2 offset3 = quadOffset;

    samples.neighbours[4] = ConvertToWorkingSpace(Fetch4(InputTexture, UV, offset1, _RTHandleScale.xy).CTYPE_SWIZZLE);
    samples.neighbours[5] = ConvertToWorkingSpace(Fetch4(InputTexture, UV, offset2, _RTHandleScale.xy).CTYPE_SWIZZLE);
    samples.neighbours[6] = ConvertToWorkingSpace(Fetch4(InputTexture, UV, offset3, _RTHandleScale.xy).CTYPE_SWIZZLE);
    samples.neighbours[7] = QuadReadColorAcrossDiagonal(centralColor, positionSS);

#else // !WIDE_NEIGHBOURHOOD

#if SMALL_NEIGHBOURHOOD_SHAPE == PLUS

    samples.neighbours[0] = ConvertToWorkingSpace(Fetch4(InputTexture, UV, float2(0.0f, quadOffset.y), _RTHandleScale.xy).CTYPE_SWIZZLE);
    samples.neighbours[1] = ConvertToWorkingSpace(Fetch4(InputTexture, UV, float2(quadOffset.x, 0.0f), _RTHandleScale.xy).CTYPE_SWIZZLE);
    samples.neighbours[2] = QuadReadColorAcrossX(centralColor, positionSS);
    samples.neighbours[3] = QuadReadColorAcrossY(centralColor, positionSS);

#else // SMALL_NEIGHBOURHOOD_SHAPE == CROSS

    int2 fastOffset = -quadOffset;
    int2 offset1 = int2(-quadOffset.x, quadOffset.y);
    int2 offset2 = int2(quadOffset.x, -quadOffset.y);
    int2 offset3 = quadOffset;

    samples.neighbours[0] = ConvertToWorkingSpace(Fetch4(InputTexture, UV, offset1, _RTHandleScale.xy).CTYPE_SWIZZLE);
    samples.neighbours[1] = ConvertToWorkingSpace(Fetch4(InputTexture, UV, offset2, _RTHandleScale.xy).CTYPE_SWIZZLE);
    samples.neighbours[2] = ConvertToWorkingSpace(Fetch4(InputTexture, UV, offset3, _RTHandleScale.xy).CTYPE_SWIZZLE);
    samples.neighbours[3] = QuadReadColorAcrossDiagonal(centralColor, positionSS);

#endif // SMALL_NEIGHBOURHOOD_SHAPE == 4

#endif // !WIDE_NEIGHBOURHOOD

#if PERCEPTUAL_SPACE
    ConvertNeighboursToPerceptualSpace(samples);
#endif
}


void MinMaxNeighbourhood(inout NeighbourhoodSamples samples)
{
    // We always have at least the first 4 neighbours.
    samples.minNeighbour = Min3Color(samples.neighbours[0], samples.neighbours[1], samples.neighbours[2]);
    samples.minNeighbour = Min3Color(samples.minNeighbour, samples.central, samples.neighbours[3]);

    samples.maxNeighbour = Max3Color(samples.neighbours[0], samples.neighbours[1], samples.neighbours[2]);
    samples.maxNeighbour = Max3Color(samples.maxNeighbour, samples.central, samples.neighbours[3]);

#if WIDE_NEIGHBOURHOOD
    samples.minNeighbour = Min3Color(samples.minNeighbour, samples.neighbours[4], samples.neighbours[5]);
    samples.minNeighbour = Min3Color(samples.minNeighbour, samples.neighbours[6], samples.neighbours[7]);

    samples.maxNeighbour = Max3Color(samples.maxNeighbour, samples.neighbours[4], samples.neighbours[5]);
    samples.maxNeighbour = Max3Color(samples.maxNeighbour, samples.neighbours[6], samples.neighbours[7]);
#endif

    samples.avgNeighbour = 0;
    for (int i = 0; i < NEIGHBOUR_COUNT; ++i)
    {
        samples.avgNeighbour += samples.neighbours[i];
    }
    samples.avgNeighbour *= rcp(NEIGHBOUR_COUNT);
}

void VarianceNeighbourhood(inout NeighbourhoodSamples samples, float historyLuma, float colorLuma, float2 antiFlickerParams, float motionVectorLen)
{
    CTYPE moment1 = 0;
    CTYPE moment2 = 0;

    for (int i = 0; i < NEIGHBOUR_COUNT; ++i)
    {
        moment1 += samples.neighbours[i];
        moment2 += samples.neighbours[i] * samples.neighbours[i];
    }
    samples.avgNeighbour = moment1 * rcp(NEIGHBOUR_COUNT);

    moment1 += samples.central;
    moment2 += samples.central * samples.central;

    const int sampleCount = NEIGHBOUR_COUNT + 1;
    moment1 *= rcp(sampleCount);
    moment2 *= rcp(sampleCount);

    CTYPE stdDev = sqrt(abs(moment2 - moment1 * moment1));

    float stDevMultiplier = 1.5;
    // The reasoning behind the anti flicker is that if we have high spatial contrast (high standard deviation)
    // and high temporal contrast, we let the history to be closer to be unclipped. To achieve, the min/max bounds
    // are extended artificially more.
#if ANTI_FLICKER
    stDevMultiplier = 1.5;
    float temporalContrast = saturate(abs(colorLuma - historyLuma) / Max3(0.2, colorLuma, historyLuma));
#if ANTI_FLICKER_MV_DEPENDENT
    const float screenDiag = length(_ScreenSize.xy);
    const float maxFactorScale = 2.25f; // when stationary
    const float minFactorScale = 0.8f; // when moving more than slightly
    float localizedAntiFlicker = lerp(antiFlickerParams.x * minFactorScale, antiFlickerParams.x * maxFactorScale, saturate(1.0f - 2.0f * (motionVectorLen * screenDiag)));
#else
    float localizedAntiFlicker = antiFlickerParams.x;
#endif
    stDevMultiplier += lerp(0.0, localizedAntiFlicker, smoothstep(0.05, antiFlickerParams.y, temporalContrast));

#endif
    samples.minNeighbour = moment1 - stdDev * stDevMultiplier;
    samples.maxNeighbour = moment1 + stdDev * stDevMultiplier;
}

void GetNeighbourhoodCorners(inout NeighbourhoodSamples samples, float historyLuma, float colorLuma, float2 antiFlickerParams, float motionVecLen)
{
#if NEIGHBOUROOD_CORNER_METHOD == MINMAX
    MinMaxNeighbourhood(samples);
#else
    VarianceNeighbourhood(samples, historyLuma, colorLuma, antiFlickerParams, motionVecLen);
#endif
}

// ---------------------------------------------------
// Filter main color
// ---------------------------------------------------

CTYPE FilterCentralColor(NeighbourhoodSamples samples, float4 filterWeights)
{
#if CENTRAL_FILTERING == NO_FILTERING

    return samples.central;

#elif CENTRAL_FILTERING == BOX_FILTER

    CTYPE avg = samples.central;
    for (int i = 0; i < NEIGHBOUR_COUNT; ++i)
    {
        avg += samples.neighbours[i];
    }
    return avg / (1 + NEIGHBOUR_COUNT);

#elif CENTRAL_FILTERING == BLACKMAN_HARRIS

    CTYPE filtered = samples.central * filterWeights.x;
    filtered += (samples.neighbours[0] + samples.neighbours[1] + samples.neighbours[2] + samples.neighbours[3]) * filterWeights.y;
#if WIDE_NEIGHBOURHOOD
    filtered += (samples.neighbours[4] + samples.neighbours[5] + samples.neighbours[6] + samples.neighbours[7]) * filterWeights.z;
#endif
    return filtered;

#endif

}

// ---------------------------------------------------
// Blend factor calculation
// ---------------------------------------------------

float HistoryContrast(float historyLuma, float minNeighbourLuma, float maxNeighbourLuma)
{
    float lumaContrast = max(maxNeighbourLuma - minNeighbourLuma, 0) / historyLuma;
    float blendFactor = 0.125;
    return saturate(blendFactor * rcp(1.0 + lumaContrast));
}

float DistanceToClamp(float historyLuma, float minNeighbourLuma, float maxNeighbourLuma)
{
    float distToClamp = min(abs(minNeighbourLuma - historyLuma), abs(maxNeighbourLuma - historyLuma));
    return saturate((0.125 * distToClamp) / (distToClamp + maxNeighbourLuma - minNeighbourLuma));
}

float GetBlendFactor(float colorLuma, float historyLuma, float minNeighbourLuma, float maxNeighbourLuma)
{
    // TODO: Investigate factoring in the speed in this computation.

    return HistoryContrast(historyLuma, minNeighbourLuma, maxNeighbourLuma);
}

// ---------------------------------------------------
// Clip History
// ---------------------------------------------------

// From Playdead's TAA
CTYPE DirectClipToAABB(CTYPE history, CTYPE minimum, CTYPE maximum)
{
    // note: only clips towards aabb center (but fast!)
    CTYPE center  = 0.5 * (maximum + minimum);
    CTYPE extents = 0.5 * (maximum - minimum);

    // This is actually `distance`, however the keyword is reserved
    CTYPE offset = history - center;
    float3 v_unit = offset.xyz / extents.xyz;
    float3 absUnit = abs(v_unit);
    float maxUnit = Max3(absUnit.x, absUnit.y, absUnit.z);

    if (maxUnit > 1.0)
        return center + (offset / maxUnit);
    else
        return history;
}

// Here the ray referenced goes from history to (filtered) center color
float DistToAABB(CTYPE color, CTYPE history, CTYPE minimum, CTYPE maximum)
{
    CTYPE center = 0.5 * (maximum + minimum);
    CTYPE extents = 0.5 * (maximum - minimum);

    CTYPE rayDir = color - history;
    CTYPE rayPos = history - center;

    CTYPE invDir = rcp(rayDir);
    CTYPE t0 = (extents - rayPos)  * invDir;
    CTYPE t1 = -(extents + rayPos) * invDir;

    float AABBIntersection = max(max(min(t0.x, t1.x), min(t0.y, t1.y)), min(t0.z, t1.z));
    return saturate(AABBIntersection);
}

CTYPE GetClippedHistory(CTYPE filteredColor, CTYPE history, CTYPE minimum, CTYPE maximum)
{
#if HISTORY_CLIP == DIRECT_CLIP
    return DirectClipToAABB(history, minimum, maximum);
#elif HISTORY_CLIP == BLEND_WITH_CLIP
    float historyBlend = DistToAABB(filteredColor, history, minimum, maximum);
    return lerp(history, filteredColor, historyBlend);
#elif HISTORY_CLIP == SIMPLE_CLAMP
    return clamp(history, minimum, maximum);
#endif
}

// ---------------------------------------------------
// Sharpening
// ---------------------------------------------------

// TODO: This is not great and sub optimal since it really needs to be in linear and the data is already in perceptive space
CTYPE SharpenColor(NeighbourhoodSamples samples, CTYPE color, float sharpenStrength)
{
    CTYPE linearC = color * PerceptualInvWeight(color);
    CTYPE linearAvg = samples.avgNeighbour * PerceptualInvWeight(samples.avgNeighbour);
    linearC = linearC + (linearC - linearAvg) * sharpenStrength * 3;

#if YCOCG
    linearC.x = clamp(linearC.x, 0, CLAMP_MAX);
#else
    linearC = clamp(linearC, 0, CLAMP_MAX);
#endif
    return linearC * PerceptualWeight(linearC);
}
