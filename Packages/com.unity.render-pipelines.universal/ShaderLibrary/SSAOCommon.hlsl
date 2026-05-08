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
half4 _SSAOTemporalParams;   // x: TemporalScale (AABB variance), y: TemporalResponse (blend weight)
float4 _MotionVectorTexture_TexelSize;

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
static const half kContrast         = half(0.6);
static const half kGeometryCoeff    = half(0.8);
static const half kBeta             = half(0.004);
static const half kEpsilon          = half(0.0001);

static const float GOLDEN_RATIO     = 1.6180339887;
static const uint  R1_ALPHA_UINT    = 2654435769u;  // (golden_ratio - 1) * (1 << 32)
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
static const half  HALF_INV_NINE    = half(0.11111111111111111111);
static const half  HALF_HUNDRED     = half(100.0);

struct GTAOConfig
{
    half intensity;
    half radius;
    half downsample;
    half falloff;
    float4 depthToViewParams;
    half gtaoMaxRadiusPixels;
    half gtaoInvRadiusSq;
    half gtaoFOVCorrection;
    half2 blueNoiseScale;
    half2 blueNoiseOffset;
    half temporalRotation;
    uint temporalOffset;
};

GTAOConfig CreateGTAOConfig(half4 ssaoParams, half4 ssaoParams2, float4 depthToViewParams, half2 blueNoiseScale, half2 blueNoiseOffset, half temporalRotation, uint temporalOffset)
{
    GTAOConfig config;
    config.intensity           = ssaoParams.x;
    config.radius              = ssaoParams.y;
    config.downsample          = ssaoParams.z;
    config.falloff             = ssaoParams.w;
    config.depthToViewParams   = depthToViewParams;
    config.gtaoMaxRadiusPixels = ssaoParams2.x;
    config.gtaoInvRadiusSq     = ssaoParams2.y;
    config.gtaoFOVCorrection   = ssaoParams2.z;
    config.blueNoiseScale      = blueNoiseScale;
    config.blueNoiseOffset     = blueNoiseOffset;
    config.temporalRotation    = temporalRotation;
    config.temporalOffset      = temporalOffset;
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
bool ShouldSkipAO(float rawDepth, half halfLinearDepth, half falloff)
{
    if (rawDepth < SKY_DEPTH_VALUE)
        return true;

    return halfLinearDepth > falloff;
}


// ------------------------------------------------------------------
// Shared GTAO Functions
// ------------------------------------------------------------------

half2 GetDirectionGTAO_BlueNoise(float2 uv, int dirIdx, half rcpDirectionCount, half2 blueNoiseOffset, half2 blueNoiseScale, half temporalRotation)
{
    const half lerpVal = half(dirIdx) * rcpDirectionCount;
    half blueNoise = SSAO_COMMON_SAMPLE_BLUE_NOISE((uv + blueNoiseOffset) * blueNoiseScale + lerpVal);
#if defined(_TEMPORAL_FILTERING)
    blueNoise = frac(blueNoise + temporalRotation);
#endif
    // Randomized slice angle in [0, PI]
    const float sliceAngle = (lerpVal + blueNoise * rcpDirectionCount) * PI;
    float sinAngle, cosAngle;
    sincos(sliceAngle, sinAngle, cosAngle);
    return half2(cosAngle, sinAngle);
}

half2 GetDirectionGTAO_IGN(float2 positionSS, int dirIdx, half temporalRotation)
{
    float noise = InterleavedGradientNoise(positionSS, 0);

#if defined(_TEMPORAL_FILTERING)
    half rotation = temporalRotation;
#else
    static const half rotations[6] = { 60.0, 300.0, 180.0, 240.0, 120.0, 0.0 };
    half rotation = (rotations[dirIdx] / 360.0);
#endif

    // Randomized slice angle in [0, PI]
    noise = (noise + rotation) * PI;
    float sinAngle, cosAngle;
    sincos(noise, sinAngle, cosAngle);
    return half2(cosAngle, sinAngle);
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
    // 4 evenly-spaced offsets from screen position parity
    float offset = 0.25 * ((positionSS.y - positionSS.x) & 0x3);
#if defined(_TEMPORAL_FILTERING)
    static const float offsets[4] = { 0.0, 0.5, 0.25, 0.75 };
    offset += offsets[temporalOffset];
#endif
    return frac(offset);
}

float GetHorizonAngle(float maxH, float candidateH, float distSq, half invRadiusSq)
{
    // Quadratic falloff to zero at radius boundary
    half falloff = saturate(1.0 - (distSq * invRadiusSq));
    // Raise horizon blended by falloff
    return (candidateH > maxH) ? lerp(maxH, candidateH, falloff) : lerp(maxH, candidateH, 0.03);
}

float2 GetDepthSamplePos(int stepIdx, half2 rayStart, half2 rayDir, half2 screenSize, half rayOffset, half maxRadiusPixels, float minS, int sliceIdx, int stepCount)
{
    // R1 sequence using integer arithmetic for bit-exact frac()
    uint stepSeed = uint(sliceIdx + stepIdx * stepCount) * R1_ALPHA_UINT;
    float stepNoise = frac(rayOffset + UintToFloat01(stepSeed));

    float rayStep = (float(stepIdx) + stepNoise) / float(stepCount);
    rayStep = rayStep * rayStep;
    rayStep += minS;

    // Final sample position in pixels
    float offset = rayStep * maxRadiusPixels;

    return clamp(rayStart + offset * rayDir, 2.0, screenSize - 2.0);
}

void UpdateHorizon(inout float maxHorizon, float2 samplePos, half3 V, float3 positionVS, float sampleDepth, float4 depthToViewParams, half invRadiusSq)
{
    float3 samplePosVS = GetPositionVS(samplePos, sampleDepth, depthToViewParams);
    float3 deltaPos = samplePosVS - positionVS;
    float deltaLenSq = dot(deltaPos, deltaPos);
    float currHorizon = dot(deltaPos, V) * rsqrt(deltaLenSq);

    maxHorizon = GetHorizonAngle(maxHorizon, currHorizon, deltaLenSq, invRadiusSq);
}

float HorizonLoop(GTAOConfig config, float3 positionVS, half3 V, float2 rayStart, half2 rayDir,
                  half rayOffset, half maxRadiusPixels, float initialHorizon, int sliceIdx)
{
    float maxHorizon = initialHorizon;
    const half2 screenSize = SCREEN_PARAMS.xy * config.downsample;

    // Min distance to start sampling from to avoid sampling from the center pixel
    const float pixelTooCloseThreshold = 1.3;
    const float minS = pixelTooCloseThreshold / maxRadiusPixels;

    // Unroll for performance on the fragment path. On the compute path, keep the loop dynamic to support runtime quality settings.
#ifndef GTAO_COMPUTE_PATH
    UNITY_UNROLL
#endif
    for (int stepIdx = 0; stepIdx < GTAO_STEP_COUNT; stepIdx++)
    {
        float2 samplePos = GetDepthSamplePos(stepIdx, rayStart, rayDir, screenSize, rayOffset, maxRadiusPixels, minS, sliceIdx, GTAO_STEP_COUNT);
        float sampleDepth = SSAO_COMMON_FETCH_DEPTH(samplePos, screenSize, config.downsample);
        UpdateHorizon(maxHorizon, samplePos, V, positionVS, sampleDepth, config.depthToViewParams, config.gtaoInvRadiusSq);
    }

    return maxHorizon;
}

float IntegrateArcCosWeighted(float2 horizonAngles, float n, float cosN)
{
    // Double the horizon angles for the double-angle cosine terms
    float h1_2 = horizonAngles.x * 2.0;
    float h2_2 = horizonAngles.y * 2.0;
    float sinN = sin(n);
    // Analytical cosine-weighted arc integral (GTAO paper)
    return 0.25 * ((-cos(h1_2 - n) + cosN + h1_2 * sinN) + (-cos(h2_2 - n) + cosN + h2_2 * sinN));
}

half IntegrateSlice(GTAOConfig config, int dirIdx, float2 uv, float2 positionSS, float3 positionVS, half3 V, half3 normalVS, half fovCorrectedRadiusSS, half rayOffset, half rcpDirectionCount)
{
#if defined(_BLUE_NOISE)
    half2 dir = GetDirectionGTAO_BlueNoise(uv, dirIdx, rcpDirectionCount, config.blueNoiseOffset, config.blueNoiseScale, config.temporalRotation);
#else
    half2 dir = GetDirectionGTAO_IGN(positionSS, dirIdx, config.temporalRotation);
#endif
    half2 negDir = -dir + 1e-30;

    half3 sliceN = normalize(cross(half3(dir.xy, 0.0), V));
    half3 projN = normalVS - sliceN * dot(normalVS, sliceN);
    half projNLen = length(projN);
    if (projNLen < half(1e-4))
    {
        return 1.0;
    }
    half cosN = dot(projN / projNLen, V);

    half3 T = cross(V, sliceN);
    float N = -sign(dot(projN, T)) * FastACos(saturate(cosN));

    float sinN = sin(N);
    float initialHorizon0 = sinN;   // positive direction
    float initialHorizon1 = -sinN;  // negative direction

    // Find horizons (pass dirIdx for R1 sequence distribution)
    float2 maxHorizons;
    maxHorizons.x = HorizonLoop(config, positionVS, V, positionSS, dir, rayOffset, fovCorrectedRadiusSS, initialHorizon0, dirIdx);
    maxHorizons.y = HorizonLoop(config, positionVS, V, positionSS, negDir, rayOffset, fovCorrectedRadiusSS, initialHorizon1, dirIdx);

    // Now we find the actual horizon angles
    maxHorizons.x = -FastACos(maxHorizons.x);
    maxHorizons.y = FastACos(maxHorizons.y);
    maxHorizons.x = N + max(maxHorizons.x - N, -HALF_PI);
    maxHorizons.y = N + min(maxHorizons.y - N, HALF_PI);

    return AnyIsNaN(maxHorizons) ? 1.0 : IntegrateArcCosWeighted(maxHorizons, N, cosN);
}

half4 EvaluateGTAO(GTAOConfig config, float2 uv, float2 positionSS, float3 positionVS, half3 V, half3 normal, float rawDepth, float linearDepth, half halfLinearDepth)
{
    // Invalid depth check
    if (rawDepth == UNITY_RAW_FAR_CLIP_VALUE)
        return PackAONormal(HALF_ZERO, normal);

    half3 normalVS = TransformWorldToViewNormal(normal);
    normalVS = half3(normalVS.xy, -normalVS.z);

#if defined(_ORTHOGRAPHIC)
    half fovCorrectedRadiusSS = clamp(config.radius * config.gtaoFOVCorrection, GTAO_STEP_COUNT, config.gtaoMaxRadiusPixels);
#else
    half fovCorrectedRadiusSS = clamp(config.radius * config.gtaoFOVCorrection * rcp(linearDepth), GTAO_STEP_COUNT, config.gtaoMaxRadiusPixels);
#endif
#if defined(_BLUE_NOISE)
    half rayOffset = GetOffsetGTAO_BlueNoise(uv, config.blueNoiseOffset, config.blueNoiseScale, config.temporalOffset);
#else
    half rayOffset = GetOffsetGTAO_IGN((uint2)positionSS, config.temporalOffset);
#endif

    const half rcpDirectionCount = half(rcp(GTAO_DIRECTION_COUNT));
    half integral = 0.0;

    // Unroll for performance on the fragment path. On the compute path, keep the loop dynamic to support runtime quality settings
    // except when temporal filtering forces direction count to 1, where unrolling is safe.
#if !defined(GTAO_COMPUTE_PATH) || defined(_TEMPORAL_FILTERING)
    UNITY_UNROLL
#endif
    for (int dirIdx = 0; dirIdx < GTAO_DIRECTION_COUNT; dirIdx++)
    {
        integral += IntegrateSlice(config, dirIdx, uv, positionSS, positionVS, V, normalVS, fovCorrectedRadiusSS, rayOffset, rcpDirectionCount);
    }
    integral *= rcpDirectionCount;

    half falloff = HALF_ONE - halfLinearDepth * half(rcp(config.falloff));
    falloff = falloff * falloff;
    half ao = HALF_ONE - saturate(integral);
    ao = PositivePow(saturate(ao * config.intensity * falloff), kContrast);

    // Return the packed ao + normals
    return PackAONormal(ao, normal);
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
