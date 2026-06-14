#ifndef UNIVERSAL_SSAO_COMMON_INCLUDED
#define UNIVERSAL_SSAO_COMMON_INCLUDED

// Shared SSAO/GTAO library used by both the fragment path (SSAO.hlsl) and the compute path (GTAO.compute).
// Callers must define the following macros before including this file:
//   SSAO_COMMON_SAMPLE_BASEMAP(uv)                      - sample the AO accumulation texture (rgba)
//   SSAO_COMMON_SAMPLE_BASEMAP_R(uv)                    - sample the AO accumulation texture (r only)
//   SSAO_COMMON_SAMPLE_BLUE_NOISE(uv)                   - sample the blue noise texture
//   SSAO_COMMON_FETCH_DEPTH(samplePos, screenSize, ds)  - fetch scene depth at a sample position
//
// The compute path (GTAO.compute) additionally defines:
//   GTAO_COMPUTE_PATH        - guards fragment-only code (SampleDepth, UNITY_UNROLL)
//   GTAO_STEP_COUNT          - runtime uniform (vs compile-time constant in fragment path)
//   GTAO_DIRECTION_COUNT     - runtime uniform (vs compile-time constant in fragment path)

#include "Packages/com.unity.render-pipelines.core/Runtime/Sampling/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

#define SCREEN_PARAMS               GetScaledScreenParams()

// Shared uniform declarations — activated when SSAO_COMMON_DECLARE_UNIFORMS is defined by the caller.
// Functions in this file never read these globals directly; all inputs are passed as explicit parameters.
#ifdef SSAO_COMMON_DECLARE_UNIFORMS
half4 _SSAOParams;
half4 _SSAOParams2;
float4 _AODepthToViewParams;
float4 _SourceSize;

#if defined(_TEMPORAL_FILTERING)
half _SSAOTemporalRotation;
uint _SSAOTemporalOffset;    // Valid range: [0,3]
#define TemporalRotation            _SSAOTemporalRotation
#define TemporalOffset              _SSAOTemporalOffset
#else
static const half TemporalRotation  = 0;
static const uint TemporalOffset    = 0;
#endif
half _SSAOHistoryLength;
half _SSAOGhostingMitigation;

#if defined(_BLUE_NOISE)
half4 _SSAOBlueNoiseParams;
#define BlueNoiseScale              _SSAOBlueNoiseParams.xy
#define BlueNoiseOffset             _SSAOBlueNoiseParams.zw
#else
static const half2 BlueNoiseScale   = 0;
static const half2 BlueNoiseOffset  = 0;
#endif

#endif // SSAO_COMMON_DECLARE_UNIFORMS

// Constants
static const half kContrast = half(0.6);
static const half kGeometryCoeff = half(0.8);
static const half kBeta = half(0.004);
static const half kEpsilon = half(0.0001);
static const float kFalloffFadeStartScale = 0.75;

static const float GOLDEN_RATIO = 1.6180339887;
static const uint R1_ALPHA_UINT = 2654435769u;  // (golden_ratio - 1) * (1 << 32)
static const float SKY_DEPTH_VALUE = 0.00001;
static const half HALF_POINT_ONE = half(0.1);
static const half HALF_MINUS_ONE = half(-1.0);
static const half HALF_ZERO = half(0.0);
static const half HALF_HALF = half(0.5);
static const half HALF_ONE = half(1.0);
static const half4 HALF4_ONE = half4(1.0, 1.0, 1.0, 1.0);
static const half HALF_TWO = half(2.0);
static const half HALF_TWO_PI = half(6.28318530717958647693);
static const half HALF_FOUR = half(4.0);
static const half HALF_INV_NINE = half(0.11111111111111111111);
static const half HALF_HUNDRED = half(100.0);

struct GTAOConfig
{
    half intensity;
    half radius;
    half downsample;
    half falloff;
    float4 depthToViewParams;
    half gtaoMinimumRadiusInPixels;
    half gtaoFOVCorrection;
    half2 blueNoiseScale;
    half2 blueNoiseOffset;
    half temporalRotation;
    uint temporalOffset;
};

GTAOConfig CreateGTAOConfig(half4 ssaoParams, half4 ssaoParams2, float4 depthToViewParams, half2 blueNoiseScale, half2 blueNoiseOffset, half temporalRotation, uint temporalOffset)
{
    GTAOConfig config;
    config.intensity = ssaoParams.x;
    config.radius = ssaoParams.y;
    config.downsample = ssaoParams.z;
    config.falloff = ssaoParams.w;
    config.depthToViewParams = depthToViewParams;
    config.gtaoMinimumRadiusInPixels = ssaoParams2.x;
    config.gtaoFOVCorrection = ssaoParams2.z;
    config.blueNoiseScale = blueNoiseScale;
    config.blueNoiseOffset = blueNoiseOffset;
    config.temporalRotation = temporalRotation;
    config.temporalOffset = temporalOffset;
    return config;
}

#ifndef GTAO_COMPUTE_PATH
// For Downsampled SSAO we need to adjust the UV coordinates
// so it hits the center of the pixel inside the depth texture.
// The texelSize multiplier is 1.0 when DOWNSAMPLE is enabled, otherwise 0.0
#define ADJUSTED_DEPTH_UV(uv, downsample) uv.xy + ((_CameraDepthTexture_TexelSize.xy * 0.5) * (1.0 - (downsample - 0.5) * 2.0))
float SampleDepth(float2 uv, half downsample)
{
    return SampleSceneDepth(ADJUSTED_DEPTH_UV(uv.xy, downsample));
}
#endif

// ------------------------------------------------------------------
// Shared Helper Functions
// ------------------------------------------------------------------
half4 PackAOAndNormal(half ao, half3 n)
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

half CompareNormal(half3 d1, half3 d2)
{
    return smoothstep(kGeometryCoeff, HALF_ONE, dot(d1, d2));
}

float2 GetScreenSpacePosition(float2 uv, half downsample)
{
    return float2(uv * SCREEN_PARAMS.xy * downsample);
}

float GetLinearEyeDepth(float rawDepth)
{
#if defined(_ORTHOGRAPHIC)
    return LinearDepthToEyeDepth(rawDepth);
#else
    return LinearEyeDepth(rawDepth, _ZBufferParams);
#endif
}

float3 GetPositionVS(float2 positionSS, float depth, float4 depthToViewParams)
{
#if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    {
        positionSS = RemapFoveatedRenderingNonUniformToLinear(positionSS);
    }
#endif

    float linearDepth = GetLinearEyeDepth(depth);
#if defined(_ORTHOGRAPHIC)
    return float3(positionSS * depthToViewParams.xy - depthToViewParams.zw, linearDepth);
#else
    return float3((positionSS * depthToViewParams.xy - depthToViewParams.zw) * linearDepth, linearDepth);
#endif
}

// View vector in view space. Orthographic cameras have a constant view direction along -Z.
half3 GetViewVectorVS(float3 positionVS)
{
#if defined(_ORTHOGRAPHIC)
    return half3(0, 0, -1);
#else
    return half3(normalize(-positionVS));
#endif
}

// Checks if the fragment should skip AO (sky or beyond falloff).
inline bool ShouldSkipAO(float rawDepth, half halfLinearDepth, half falloff)
{
    return rawDepth == UNITY_RAW_FAR_CLIP_VALUE || rawDepth < SKY_DEPTH_VALUE || halfLinearDepth > falloff;
}

// Packing.hlsl uses `real`, which can resolve to min16float in this compute path.
// Use float math for history depth packing to avoid precision/overflow issues near 1.0.
float2 PackFloatToR8G8Safe(float value)
{
    uint packedBits = (uint)round(saturate(value) * 65535.0);
    return float2((packedBits & 0xFFu) / 255.0, ((packedBits >> 8) & 0xFFu) / 255.0);
}

float UnpackFloatFromR8G8Safe(float2 value)
{
    uint lo = (uint)round(saturate(value.x) * 255.0);
    uint hi = (uint)round(saturate(value.y) * 255.0);
    return ((hi << 8) | lo) / 65535.0;
}

float4 CreateHistoryData(float ao, float depth, float reciprocalHistoryFrameCount)
{
    float4 historyData;
    historyData.xy = PackFloatToR8G8Safe(depth);
    historyData.z = saturate(ao);
    historyData.w = saturate(reciprocalHistoryFrameCount);
    return historyData;
}

// ------------------------------------------------------------------
// Shared GTAO Functions
// ------------------------------------------------------------------

float2 FastAcosGTAO(float2 x)
{
    float2 outVal = -0.156583 * abs(x) + HALF_PI;
    outVal *= sqrt(saturate(1.0 - abs(x)));
    return lerp(PI - outVal, outVal, step(0.0, x));
}

float2 GetDirectionGTAO_BlueNoise(float2 uv, int dirIdx, float rcpDirectionCount, float2 blueNoiseOffset, float2 blueNoiseScale, float temporalRotation)
{
    const float lerpVal = float(dirIdx) * rcpDirectionCount;
    float noise = SSAO_COMMON_SAMPLE_BLUE_NOISE((uv + blueNoiseOffset) * blueNoiseScale + lerpVal);
#if defined(_TEMPORAL_FILTERING)
    noise = frac(noise + temporalRotation);
#endif
    const float sliceAngle = (float(dirIdx) + noise) * PI * rcpDirectionCount;
    float sinAngle, cosAngle;
    sincos(sliceAngle, sinAngle, cosAngle);
    return float2(cosAngle, sinAngle);
}

float2 GetDirectionGTAO_IGN(float2 positionSS, int dirIdx, float temporalRotation, half rcpDirectionCount)
{
    float noise = InterleavedGradientNoise(positionSS, 0);
#if defined(_TEMPORAL_FILTERING)
    static const float rotations[6] = { 60.0, 300.0, 180.0, 240.0, 120.0, 0.0 };
    noise = frac(noise + temporalRotation + (rotations[(uint)dirIdx % 6] / 360.0));
#endif
    const float sliceAngle = (float(dirIdx) + noise) * PI * rcpDirectionCount;
    float sinAngle, cosAngle;
    sincos(sliceAngle, sinAngle, cosAngle);
    return float2(cosAngle, sinAngle);
}

half GetOffsetGTAO_BlueNoise(float2 uv, half2 blueNoiseOffset, half2 blueNoiseScale, uint temporalOffset)
{
    const half blueNoise = SSAO_COMMON_SAMPLE_BLUE_NOISE((uv + blueNoiseOffset) * blueNoiseScale);
    // Low-discrepancy step offset via golden ratio
    float offset = blueNoise * GOLDEN_RATIO;
#if defined(_TEMPORAL_FILTERING)
    static const float offsets[4] = { 0.0, 0.5, 0.25, 0.75 };
    offset += offsets[temporalOffset];
#endif
    return frac(offset);
}

half GetOffsetGTAO_IGN(uint2 positionSS, uint temporalOffset)
{
#if defined(_TEMPORAL_FILTERING)
    // Use a stable 4-phase pattern for temporal accumulation.
    float offset = 0.25 * ((positionSS.y - positionSS.x) & 0x3);
    static const float offsets[4] = { 0.0, 0.5, 0.25, 0.75 };
    offset = frac(offset + offsets[temporalOffset]);
#else
    // Different seed than slice angle's IGN to decorrelate X/Y noise.
    float offset = InterleavedGradientNoise(float2(positionSS), 1);
#endif
    return offset;
}

float GetHorizonAngle(float maxH, float candidateH, float distSq, half invRadiusSq)
{
    // Quadratic falloff to zero at radius boundary
    half falloff = saturate(1.0 - (distSq * invRadiusSq));
    // Raise horizon blended by falloff
    return max(maxH, lerp(maxH, candidateH, falloff));
}

void UpdateHorizon(inout float maxHorizon, float2 samplePos, float3 V, float3 positionVS, float sampleDepth, float4 depthToViewParams, half invRadiusSq)
{
    float3 samplePosVS = GetPositionVS(samplePos, sampleDepth, depthToViewParams);
    float3 deltaPos = samplePosVS - positionVS;
    float deltaLenSq = dot(deltaPos, deltaPos);
    float currHorizon = dot(deltaPos, V) * rsqrt(deltaLenSq);

    maxHorizon = GetHorizonAngle(maxHorizon, currHorizon, deltaLenSq, invRadiusSq);
}

half IntegrateArcCosWeighted(float2 horizonAngles, float n, float sinN, float cosN)
{
    // Double the horizon angles for the double-angle cosine terms
    float doubledHorizon0 = horizonAngles.x * 2.0;
    float doubledHorizon1 = horizonAngles.y * 2.0;
    // Analytical cosine-weighted arc integral (GTAO paper)
    return 0.25 * ((-cos(doubledHorizon0 - n) + cosN + doubledHorizon0 * sinN) + (-cos(doubledHorizon1 - n) + cosN + doubledHorizon1 * sinN));
}

float2 EstimateSliceVisibility(GTAOConfig config, int dirIdx, float2 uv, float2 positionSS, float3 positionVS, half3 V, float3 normalVS, float fovCorrectedRadiusSS, half invRadiusSq, half rayOffset, half rcpDirectionCount)
{
#if defined(_BLUE_NOISE)
    float2 dir = GetDirectionGTAO_BlueNoise(uv, dirIdx, rcpDirectionCount, config.blueNoiseOffset, config.blueNoiseScale, config.temporalRotation);
#else
    float2 dir = GetDirectionGTAO_IGN(positionSS, dirIdx, config.temporalRotation, rcpDirectionCount);
#endif

    float3 sliceN = normalize(cross(float3(dir.xy, 0.0), V));
    float3 projN = normalVS - sliceN * dot(normalVS, sliceN);
    float projNLen = length(projN);
    float cosN = saturate(dot(projN / projNLen, V));

    float3 T = cross(V, sliceN);
    float N = -sign(dot(projN, T)) * acos(cosN);

    // Per-slice horizon accumulator: x = positive direction, y = negative direction.
    float sinN = sin(N);
    float2 maxHorizons = float2(sinN, -sinN);

    const half2 screenSize              = SCREEN_PARAMS.xy * config.downsample;
    const float pixelTooCloseThreshold  = 1.3;
    const float minStepFraction         = pixelTooCloseThreshold / fovCorrectedRadiusSS;
    const float rcpStepCount            = rcp(GTAO_STEP_COUNT);

    // Single step loop driving both directions. Step stride/noise computed once and reused.
    // Unroll for performance on the fragment path. On the compute path, keep the loop dynamic to support runtime quality settings.
#ifndef GTAO_COMPUTE_PATH
    UNITY_UNROLL
#endif
    for (int stepIdx = 0; stepIdx < GTAO_STEP_COUNT; stepIdx++)
    {
        // R1 sequence using integer arithmetic for bit-exact frac()
        uint  stepSeed  = uint(dirIdx + stepIdx * GTAO_STEP_COUNT) * R1_ALPHA_UINT;
        float stepNoise = frac(rayOffset + UintToFloat01(stepSeed));
        float rayStep   = (float(stepIdx) + stepNoise) * rcpStepCount;
        rayStep         = rayStep * rayStep + minStepFraction;
        float2 stepVec  = round(rayStep * fovCorrectedRadiusSS * dir);

        // positive direction
        float2 samplePos = positionSS + stepVec;
        float sampleDepth = SSAO_COMMON_FETCH_DEPTH(samplePos, screenSize, config.downsample);
        UpdateHorizon(maxHorizons.x, samplePos, V, positionVS, sampleDepth, config.depthToViewParams, invRadiusSq);

        // negative direction
        samplePos = positionSS - stepVec;
        sampleDepth = SSAO_COMMON_FETCH_DEPTH(samplePos, screenSize, config.downsample);
        UpdateHorizon(maxHorizons.y, samplePos, V, positionVS, sampleDepth, config.depthToViewParams, invRadiusSq);
    }

    // Convert horizon cosines to signed angles relative to slice normal N.
    float2 horizonAcos = FastAcosGTAO(maxHorizons);
    maxHorizons.x = N + max(-horizonAcos.x - N, -HALF_PI);
    maxHorizons.y = N + min( horizonAcos.y - N,  HALF_PI);

    // (visibility, maxVisibility) summed across slices and divided once by the caller.
    // maxVisibility = the integral with horizons fully open, normalizing visibility into [0, 1].
    return float2(projNLen * IntegrateArcCosWeighted(maxHorizons, N, sinN, cosN), projNLen * (N * sinN + cosN));
}

half EvaluateGTAOValue(GTAOConfig config, float2 uv, float2 positionSS, float3 positionVS, half3 V, half3 normal, float linearDepth)
{
    float3 normalVS = TransformWorldToViewNormal(normal);
    normalVS = float3(normalVS.xy, -normalVS.z);

    // Shrink origin toward camera at grazing angles to avoid the surface itself being a horizon.
    positionVS *= lerp(0.997, 1.0, abs(dot(normalVS, V)));

#if defined(_ORTHOGRAPHIC)
    float fovCorrectedRadiusSS = max(config.radius * config.gtaoFOVCorrection, config.gtaoMinimumRadiusInPixels);
    float invEffectiveRadius   = config.gtaoFOVCorrection / fovCorrectedRadiusSS;
#else
    float fovCorrectedRadiusSS = max(config.radius * config.gtaoFOVCorrection * rcp(linearDepth), config.gtaoMinimumRadiusInPixels);
    float invEffectiveRadius   = config.gtaoFOVCorrection / (fovCorrectedRadiusSS * linearDepth);
#endif
    half invRadiusSq = invEffectiveRadius * invEffectiveRadius;

#if defined(_BLUE_NOISE)
        half rayOffset = GetOffsetGTAO_BlueNoise(uv, config.blueNoiseOffset, config.blueNoiseScale, config.temporalOffset);
#else
        half rayOffset = GetOffsetGTAO_IGN((uint2)positionSS, config.temporalOffset);
#endif

    const half rcpDirectionCount = half(rcp(GTAO_DIRECTION_COUNT));
    float2 acc = 0;

    // Unroll for performance on the fragment path. On the compute path, keep the loop dynamic to support runtime quality settings.
#if !defined(GTAO_COMPUTE_PATH)
    UNITY_UNROLL
#endif
    for (int dirIdx = 0; dirIdx < GTAO_DIRECTION_COUNT; dirIdx++)
    {
        acc += EstimateSliceVisibility(config, dirIdx, uv, positionSS, positionVS, V, normalVS, fovCorrectedRadiusSS, invRadiusSq, rayOffset, rcpDirectionCount);
    }

    half integral = acc.y > half(1e-5) ? saturate(acc.x / acc.y) : HALF_ONE;
    half ao = HALF_ONE - PositivePow(integral, config.intensity);

    half fadeFactor = smoothstep(config.falloff * kFalloffFadeStartScale, config.falloff, linearDepth);
    ao *= (HALF_ONE - fadeFactor);

    return ao;
}

half4 EvaluateGTAO(GTAOConfig config, float2 uv, float2 positionSS, float3 positionVS, half3 V, half3 normal, float linearDepth)
{
    half ao = EvaluateGTAOValue(config, uv, positionSS, positionVS, V, normal, linearDepth);
    // Return the packed ao + normals
    return PackAOAndNormal(ao, normal);
}


// ------------------------------------------------------------------
// Shared Temporal Filter
// ------------------------------------------------------------------

void ResolverAABB(half aabbScale, half2 uv, half2 screenSize,
    inout half minColor, inout half maxColor, inout half filterColor)
{
    half2 texelSize = rcp(screenSize);

    half s00 = SSAO_COMMON_SAMPLE_BASEMAP_R(uv + half2(-1, -1) * texelSize);
    half s10 = SSAO_COMMON_SAMPLE_BASEMAP_R(uv + half2( 0, -1) * texelSize);
    half s20 = SSAO_COMMON_SAMPLE_BASEMAP_R(uv + half2( 1, -1) * texelSize);
    half s01 = SSAO_COMMON_SAMPLE_BASEMAP_R(uv + half2(-1,  0) * texelSize);
    half s11 = SSAO_COMMON_SAMPLE_BASEMAP_R(uv + half2( 0,  0) * texelSize); // center
    half s21 = SSAO_COMMON_SAMPLE_BASEMAP_R(uv + half2( 1,  0) * texelSize);
    half s02 = SSAO_COMMON_SAMPLE_BASEMAP_R(uv + half2(-1,  1) * texelSize);
    half s12 = SSAO_COMMON_SAMPLE_BASEMAP_R(uv + half2( 0,  1) * texelSize);
    half s22 = SSAO_COMMON_SAMPLE_BASEMAP_R(uv + half2( 1,  1) * texelSize);

    // Gaussian weighted filtering (3x3 kernel)
    static const half cornerWeight = 0.0625h;  // 1.0h / 16.0h
    static const half edgeWeight   = 0.125h;   // 2.0h / 16.0h
    static const half centerWeight = 0.25h;    // 4.0h / 16.0h

    half filtered = s00 * cornerWeight + s10 * edgeWeight + s20 * cornerWeight
                  + s01 * edgeWeight   + s11 * centerWeight + s21 * edgeWeight
                  + s02 * cornerWeight + s12 * edgeWeight + s22 * cornerWeight;

    // Variance-based AABB
    half m1 = s00 + s10 + s20 + s01 + s11 + s21 + s02 + s12 + s22;
    half m2 = s00*s00 + s10*s10 + s20*s20 + s01*s01 + s11*s11 + s21*s21 + s02*s02 + s12*s12 + s22*s22;

    half mean = m1 * HALF_INV_NINE;
    half stddev = sqrt(max(0, m2 * HALF_INV_NINE - mean * mean));

    minColor = mean - aabbScale * stddev;
    maxColor = mean + aabbScale * stddev;

    filterColor = filtered;
    minColor = min(minColor, filtered);
    maxColor = max(maxColor, filtered);
}

#endif //UNIVERSAL_SSAO_COMMON_INCLUDED
