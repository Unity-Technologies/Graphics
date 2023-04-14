#ifndef DOF_GATHER_UTILS
#define DOF_GATHER_UTILS

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"

// Input textures
TEXTURE2D_X(_InputTexture);
TEXTURE2D_X(_InputCoCTexture);
TEXTURE2D_X(_TileList);

#define FAST_INFOCUS_TILE 0
#define SLOW_INFOCUS_TILE 1
#define FAST_DEFOCUS_TILE 2

#define TILE_RES  8u

// A set of Defines to fine-tune the algorithm
#define ADAPTIVE_SAMPLING
#define NON_UNIFORM_DENSITY
#define RING_OCCLUSION
//#define PER_TILE_BG_FG_CLASSIFICATION     // Disabled because of artifacts (case 1413534)
#define PHYSICAL_WEIGHTS
//#define USE_BLUE_NOISE                    // Increase quality at the cost of performance

#ifndef NON_UNIFORM_DENSITY
#define AlphaScale 1
#else
#define AlphaScale 2
#endif

// ============== RNG Utils ==================== //

#define XOR_SHIFT   1

#define RNG_METHOD XOR_SHIFT

#define BN_RAND_BOUNCE 4
#define BN_RAND_OFFSET 5

#define RngStateType uint

// Converts unsigned integer into float int range <0; 1) by using 23 most significant bits for mantissa
float UintToNormalizedFloat(uint x)
{
    return asfloat(0x3f800000 | (x >> 9)) - 1.0f;
}

RngStateType InitRNG(uint2 launchIndex, uint frameIndex, uint sampleIndex, uint sampleCount)
{
    frameIndex = frameIndex * sampleCount + sampleIndex;

    RngStateType seed = dot(launchIndex, uint2(1, 1280)) ^ JenkinsHash(frameIndex);
    return JenkinsHash(seed);
}

float RandomFloat01(inout RngStateType state, uint dimension)
{
    return UintToNormalizedFloat(XorShift(state));
}

// ================ DoF Sampling Utils =================== //


struct AccumData
{
    float4 color;
    float alpha;
    float destAlpha;
    float CoC;
};

struct DoFTile
{
    float maxRadius;
    float layerBorder;
    int numSamples;
};

struct SampleData
{
    CTYPE color;
    float CoC;
};


float GetSampleWeight(float cocRadius)
{
#ifdef PHYSICAL_WEIGHTS
    float pixelRadius = 0.7071f;
    float radius = max(pixelRadius, abs(cocRadius));
    return pixelRadius * pixelRadius * rcp(radius * radius);
#else
    return 1.0f;
#endif
}

float GetCoCRadius(int2 positionSS)
{
    float CoCRadius = LOAD_TEXTURE2D_X(_InputCoCTexture, ResScale * positionSS).x;
    return CoCRadius * OneOverResScale;
}

CTYPE GetColorSample(float2 sampleTC, float lod)
{
#ifndef FORCE_POINT_SAMPLING
    float texelsToClamp = (1u << (uint)ceil(lod)) + 1;
    float2 uv = ClampAndScaleUVPostProcessTexture(ResScale * (sampleTC + 0.5) * _PostProcessScreenSize.zw, _PostProcessScreenSize.zw, texelsToClamp);

    // Trilinear sampling can introduce some "leaking" between in-focus and out-of-focus regions, hence why we force point
    // sampling. Ideally, this choice should be per-tile (use trilinear only in tiles without in-focus pixels), but until
    // we do this, it is more safe to point sample.
    return SAMPLE_TEXTURE2D_X_LOD(_InputTexture, s_trilinear_clamp_sampler, uv, lod).CTYPE_SWIZZLE;
#else
    float texelsToClamp = (1u << (uint)ceil(ResScale - 1.0)) + 1;
    float2 uv = ClampAndScaleUVPostProcessTexture(ResScale * (sampleTC + 0.5) * _PostProcessScreenSize.zw, _PostProcessScreenSize.zw, texelsToClamp);
    return SAMPLE_TEXTURE2D_X_LOD(_InputTexture, s_point_clamp_sampler, uv, ResScale - 1.0).CTYPE_SWIZZLE;
#endif
}

void LoadTileData(float2 sampleTC, SampleData centerSample, float rings, inout DoFTile tileData)
{
    float4 cocRanges = LOAD_TEXTURE2D_X(_TileList, ResScale * sampleTC / TILE_RES) * OneOverResScale;

    // Note: for the far-field, we don't need to search further than than the central CoC.
    // If there is a larger CoC that overlaps the central pixel then it will have greater depth
    float limit = min(cocRanges.y, 2 * abs(centerSample.CoC));
    tileData.maxRadius = max(limit, -cocRanges.w);

    // Detect tiles than need more samples
    tileData.numSamples = rings;
    tileData.numSamples = tileData.maxRadius > 0 ? tileData.numSamples : 0;

#ifdef ADAPTIVE_SAMPLING
    float minRadius = min(cocRanges.x, -cocRanges.z) * OneOverResScale;
    tileData.numSamples = (int)ceil((minRadius / tileData.maxRadius < 0.1) ? tileData.numSamples * AdaptiveSamplingWeights.x : tileData.numSamples * AdaptiveSamplingWeights.y);
#endif

    // By default split the fg and bg layers at 0
    tileData.layerBorder = 0;
#ifdef PER_TILE_BG_FG_CLASSIFICATION
    if (cocRanges.w != 0 && cocRanges.y == 0)
    {
        // If there is no far field, then compute a splitting threshold that puts fg and bg in the near field
        // We do it this way becayse we don't want any layers that span both the near and far field (CoC < 0 & CoC > 0)
        tileData.layerBorder = (/*cocRanges.z*/ 0 + cocRanges.w) / 2;
    }
#endif
}

float2 PointInCircle(float angle)
{
    return float2(cos(angle), sin(angle)) * float2 (1 - Anamorphism, 1 + Anamorphism);
}

void ResolveColorAndAlpha(inout float4 outColor, inout float outAlpha, CTYPE defaultValue)
{
    outColor.xyz = outColor.w > 0 ? outColor.xyz / outColor.w : defaultValue.xyz;
#ifdef ENABLE_ALPHA
    outAlpha = outColor.w > 0 ? outAlpha / outColor.w : defaultValue.w;
#endif
}

void AccumulateSample(SampleData sampleData, float weight, inout AccumData accumData)
{
    accumData.color += float4(sampleData.color.xyz * weight, weight);
    accumData.CoC += abs(sampleData.CoC) * weight;
#ifdef ENABLE_ALPHA
    accumData.alpha += sampleData.color.w * weight;
#endif
}

CTYPE ResolveBackGroundEstimate(SampleData centerSample, float fgAlpha, float4 bgEstimate, float bgEstimateAlpha)
{
    CTYPE result;
    result.xyz = bgEstimate.w > 0 ? bgEstimate.xyz / bgEstimate.w : centerSample.color.xyz;
    result.xyz = lerp(centerSample.color.xyz, result.xyz, sqrt(fgAlpha));

#ifdef ENABLE_ALPHA
    result.w = bgEstimate.w > 0 ? bgEstimateAlpha / bgEstimate.w : centerSample.color.w;
    result.w = lerp(centerSample.color.w, result.w, sqrt(fgAlpha));
#endif

    return result;
}

void AccumulateCenterSample(SampleData centerSample, inout AccumData bgAccumData)
{
    float centerAlpha = GetSampleWeight(centerSample.CoC);
    bgAccumData.color.xyz = bgAccumData.color.xyz * (1 - centerAlpha) + centerAlpha * centerSample.color.xyz;
#ifdef ENABLE_ALPHA
    bgAccumData.alpha = bgAccumData.alpha * (1 - centerAlpha) + centerAlpha * centerSample.color.w;
#endif
}


void AccumulateSampleData(SampleData sampleData[2], float sampleRadius, float borderRadius, float layerBorder, float densityBias, const bool isForeground, inout AccumData ringAccum, inout AccumData accumData)
{
    UNITY_UNROLL
        for (int k = 0; k < 2; k++)
        {
            // saturate allows a small overlap between the layers, this helps conceal any continuity artifacts due to differences in sorting
            float w = saturate(sampleData[k].CoC - layerBorder);
            float layerWeight = isForeground ? 1.0 - w : w;

            float CoC = abs(sampleData[k].CoC);

            float sampleWeight = densityBias * GetSampleWeight(CoC);
            //float visibility = saturate(CoC - sampleRadius);
            float visibility = step(0.0, CoC - sampleRadius);

            // Check if the sample belongs to the current bucket
            float borderWeight = saturate(CoC - borderRadius);

#ifndef RING_OCCLUSION
            borderWeight = 0;
#endif
            float weight = layerWeight * visibility * sampleWeight;
            AccumulateSample(sampleData[k], borderWeight * weight, accumData);
            AccumulateSample(sampleData[k], (1.0 - borderWeight) * weight, ringAccum);
        }
}

void AccumulateRingData(float numSamples, const bool isNearField, AccumData ringData, inout AccumData accumData)
{
    if (ringData.color.w == 0)
    {
        // nothing to accumulate
        return;
    }

    float ringAvgCoC = ringData.color.w > 0 ? ringData.CoC * rcp(ringData.color.w) : 0;
    float accumAvgCoC = accumData.color.w > 0 ? accumData.CoC * rcp(accumData.color.w) : 0;

    float ringOcclusion = saturate(accumAvgCoC - ringAvgCoC);
    //float ringOpacity = 1.0 - saturate(ringData.coverage * rcp(numSamples));
    float normCoC = ringData.CoC * rcp(ringData.color.w);
    float ringOpacity = saturate(AlphaScale * ringData.color.w * rcp(GetSampleWeight(normCoC)) * rcp(numSamples));

    // Near-field is the region where CoC > 0. In this case sorting is reversed.
    if (isNearField)
    {
        const float occlusionWeight = 0.5;
        float accumOcclusion = occlusionWeight * (1 - saturate(ringAvgCoC - accumAvgCoC));

        // front-to-back blending
        float alpha = 1.0;
#ifdef  RING_OCCLUSION
        alpha = accumData.destAlpha;
#endif
        accumData.color += alpha * ringData.color;
        accumData.alpha += alpha * ringData.alpha;
        accumData.CoC += alpha * ringData.CoC;

        accumData.destAlpha *= saturate(1 - 2 * ringOpacity * accumOcclusion);
    }
    else
    {
        // back-to-front blending
        float alpha = 0.0;
#ifdef  RING_OCCLUSION
        alpha = (accumData.color.w > 0.0) ? ringOpacity * ringOcclusion : 1.0;
#endif
        accumData.color = accumData.color * (1.0 - alpha) + ringData.color;
        accumData.alpha = accumData.alpha * (1.0 - alpha) + ringData.alpha;
        accumData.CoC = accumData.CoC * (1.0 - alpha) + ringData.CoC;
    }
}


void DoFGatherRings(PositionInputs posInputs, DoFTile tileData, SampleData centerSample, out float4 color, out float alpha)
{
    AccumData bgAccumData, fgAccumData;
    ZERO_INITIALIZE(AccumData, bgAccumData);
    ZERO_INITIALIZE(AccumData, fgAccumData);

    // Layers in the near field are using front-to-back accumulation (so start with a dest alpha of 1, ignored if in the far field)
    fgAccumData.destAlpha = 1;
    bgAccumData.destAlpha = 1;

    const bool isBgLayerInNearField = tileData.layerBorder < 0;

    float dR = rcp((float)tileData.numSamples);
    int noiseOffset = _TaaFrameInfo.w != 0.0 ? _TaaFrameInfo.z : 0;
    int halfSamples = tileData.numSamples >> 1;
    float dAng = PI * rcp(halfSamples);

    // Select the appropriate mip to sample based on the amount of samples. Lower sample counts will be faster at the cost of "leaking"
    float lod = min(MaxColorMip, log2(2 * PI * tileData.maxRadius * rcp(tileData.numSamples)));

    RngStateType rngState = InitRNG(posInputs.positionSS.xy, noiseOffset, 0, tileData.numSamples);
    float4 bgEstimate = 0;
    float  bgEstimateAlpha = 0;

    // Gather the DoF samples
    for (int ring = tileData.numSamples - 1; ring >= 0; ring--)
    {
        AccumData bgRingData, fgRingData;
        ZERO_INITIALIZE(AccumData, bgRingData);
        ZERO_INITIALIZE(AccumData, fgRingData);

        for (int i = 0; i < halfSamples; i++)
        {
#ifdef USE_BLUE_NOISE
            float r1 = GetBNDSequenceSample(posInputs.positionSS.xy, ring * tileData.numSamples + i + noiseOffset, 0);
            float r2 = GetBNDSequenceSample(posInputs.positionSS.xy, ring * tileData.numSamples + i + noiseOffset, 1);
#else
            float r1 = RandomFloat01(rngState, ring * tileData.numSamples * 2 + 2 * i);
            float r2 = RandomFloat01(rngState, ring * tileData.numSamples * 2 + 2 * i + 1);
#endif

#ifndef NON_UNIFORM_DENSITY
            float densityBias = 1;
            float sampleRadius = sqrt((ring + r1) * dR) * tileData.maxRadius;
            float borderRadius = sqrt((ring + 1.5) * dR) * tileData.maxRadius;
#else
            float densityBias = ((ring + r1) * dR);
            float sampleRadius = ((ring + r1) * dR) * tileData.maxRadius;
            float borderRadius = ((ring + 1.5) * dR) * tileData.maxRadius;
#endif

            SampleData sampleData[2];
            const float offset[2] = { 0, PI };

            UNITY_UNROLL
                for (int j = 0; j < 2; j++)
                {
                    float2 sampleTC = posInputs.positionSS + sampleRadius * PointInCircle(offset[j] + (i + r2) * dAng);
                    sampleData[j].color = GetColorSample(sampleTC, lod);
                    sampleData[j].CoC = GetCoCRadius(sampleTC);
                    bgEstimate += float4(sampleData[j].color.xyz, 1);
#ifdef ENABLE_ALPHA
                    bgEstimateAlpha += sampleData[j].color.w;
#endif
                }

            float layerBorder = tileData.layerBorder;
            AccumulateSampleData(sampleData, sampleRadius, borderRadius, layerBorder, densityBias, false, bgRingData, bgAccumData);
            AccumulateSampleData(sampleData, sampleRadius, borderRadius, layerBorder, densityBias, true, fgRingData, fgAccumData);
        }

        AccumulateRingData(tileData.numSamples, isBgLayerInNearField, bgRingData, bgAccumData);
        AccumulateRingData(tileData.numSamples, true, fgRingData, fgAccumData);
    }

    // Compute the fg alpha. Needs to be normalized based on search radius.
    float normCoC = fgAccumData.CoC / fgAccumData.color.w;
    float scaleFactor = (normCoC * normCoC) / (tileData.maxRadius * tileData.maxRadius);
    float correctSamples = scaleFactor * (tileData.numSamples * tileData.numSamples);
    float fgAlpha = saturate(2 * AlphaScale * fgAccumData.color.w * rcp(GetSampleWeight(normCoC)) * rcp(correctSamples));

    // Approximate the missing background
    CTYPE bgColor = ResolveBackGroundEstimate(centerSample, fgAlpha, bgEstimate, bgEstimateAlpha);

    ResolveColorAndAlpha(bgAccumData.color, bgAccumData.alpha, bgColor);
    ResolveColorAndAlpha(fgAccumData.color, fgAccumData.alpha, bgColor);

    // Note: this extra step is used to hide some artifacts, generally it should not be needed
    AccumulateCenterSample(centerSample, bgAccumData);

    // Now blend the bg and fg layers
    color = bgAccumData.color * (1.0 - fgAlpha) + fgAlpha * fgAccumData.color;
    alpha = bgAccumData.alpha * (1.0 - fgAlpha) + fgAlpha * fgAccumData.alpha;
}

int GetTileClass(float2 sampleTC)
{
    float4 cocRanges = LOAD_TEXTURE2D_X(_TileList, ResScale * sampleTC / TILE_RES);
    float minRadius = min(abs(cocRanges.x), -cocRanges.z);
    float maxRadius = max(abs(cocRanges.y), -cocRanges.w);

    if (minRadius < 1 && maxRadius < 1)
        return FAST_INFOCUS_TILE;
    else if (minRadius > 2.5 && maxRadius > 2.5)
        return FAST_DEFOCUS_TILE;
    else
        return SLOW_INFOCUS_TILE;
}

void DebugTiles(float2 sampleTC, inout float3 output)
{
    int tileClass = GetTileClass(sampleTC);
    if (tileClass == SLOW_INFOCUS_TILE)
    {
        output.xyz = lerp(output.xyz, float3(1, 0, 0), 0.9);
    }
    else if (tileClass == FAST_DEFOCUS_TILE)
    {
        output.xyz = lerp(output.xyz, float3(0, 0, 1), 0.9);
    }
    else
    {
        output.xyz = lerp(output.xyz, float3(0, 1, 0), 0.9);
    }
}

void ComposeAlpha(inout CTYPE outColor, float3 inputColor, float alpha)
{
#ifdef ENABLE_ALPHA
    // Preserve the original value of the pixels with zero alpha.
    // The second line with the lerp+smoothstep combination avoids a hard transition in edge cases
    //outColor.xyz = outAlpha > 0 ? outColor.xyz : originalColor.xyz;
    outColor.xyz = lerp(inputColor, outColor.xyz, smoothstep(0, 0.01, alpha));
    outColor.w = alpha;
#endif
}

#endif //DOF_GATHER_UTILS
