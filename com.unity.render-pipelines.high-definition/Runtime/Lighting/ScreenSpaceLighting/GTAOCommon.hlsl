#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

CBUFFER_START(GTAOUniformBuffer)
float4 _AOBufferSize;
float4 _AOParams0;
float4 _AOParams1;
float4 _AOParams2;
float4 _AOParams3;
float4 _AOParams4;
float4 _FirstTwoDepthMipOffsets;
float4 _AODepthToViewParams;
CBUFFER_END

#define _AOBaseResMip  (int)_AOParams0.x
#define _AOFOVCorrection _AOParams0.y
#define _AORadius _AOParams0.z
#define _AOStepCount (uint)_AOParams0.w
#define _AOIntensity _AOParams1.x
#define _AOInvRadiusSq _AOParams1.y
#define _AOTemporalOffsetIdx _AOParams1.z
#define _AOTemporalRotationIdx _AOParams1.w
#define _AOInvStepCountPlusOne _AOParams2.z
#define _AOMaxRadiusInPixels (int)_AOParams2.w
#define _AOHistorySize _AOParams2.xy
#define _AODirectionCount _AOParams4.x
#define _FirstDepthMipOffset _FirstTwoDepthMipOffsets.xy
#define _SecondDepthMipOffset _FirstTwoDepthMipOffsets.zw

// For denoising, whether temporal or not
#define _BlurTolerance _AOParams3.x
#define _UpsampleTolerance _AOParams3.y
#define _NoiseFilterStrength _AOParams3.z
#define _StepSize _AOParams3.w
#define _AOTemporalUpperNudgeLimit _AOParams4.y
#define _AOTemporalLowerNudgeLimit _AOParams4.z


// If this is set to 0 best quality is achieved when full res, but performance is significantly lower.
// If set to 1, when full res, it may lead to extra aliasing and loss of detail, but still significant higher quality than half res.
#define HALF_RES_DEPTH_WHEN_FULL_RES 1 // Make this an option.
#define HALF_RES_DEPTH_WHEN_FULL_RES_FOR_CENTRAL 0

// This increases the quality when running with half resolution buffer, however it adds a bit of cost. Note that it will not have artifact as we already don't allow samples to be at the edge of the depth buffer.
#define MIN_DEPTH_GATHERED_FOR_CENTRAL 0

#define CENTRAL_AND_SAMPLE_DEPTH_FETCH_SAME_METHOD 0

#define LOWER_RES_SAMPLE 1

float GetMinDepth(float2 localUVs)
{
    localUVs = ClampAndScaleUVForBilinear(localUVs, _AOBufferSize.zw);
    localUVs.x = localUVs.x * 0.5f;
    localUVs.y = localUVs.y * (1.0f / 3.0f) + (2.0f / 3.0f);

    float4 gatheredDepth = GATHER_TEXTURE2D_X(_CameraDepthTexture, s_point_clamp_sampler, localUVs);
    return min(Min3(gatheredDepth.x, gatheredDepth.y, gatheredDepth.z), gatheredDepth.w);
}

float GetDepthForCentral(float2 positionSS)
{

#ifdef FULL_RES

#if HALF_RES_DEPTH_WHEN_FULL_RES_FOR_CENTRAL

#if MIN_DEPTH_GATHERED_FOR_CENTRAL

    float2 localUVs = positionSS.xy * _AOBufferSize.zw;
    return GetMinDepth(localUVs);

#else // MIN_DEPTH_GATHERED_FOR_CENTRAL
    return LOAD_TEXTURE2D_X(_CameraDepthTexture, float2(0.0f, _AORTHandleSize.y) + positionSS / 2).r;
#endif

#else  // HALF_RES_DEPTH_WHEN_FULL_RES
    return LOAD_TEXTURE2D_X(_CameraDepthTexture, positionSS).r;
#endif

#else // FULL_RES

#if MIN_DEPTH_GATHERED_FOR_CENTRAL

    float2 localUVs = positionSS.xy * _AOBufferSize.zw;
    return GetMinDepth(localUVs);
#else

    return LOAD_TEXTURE2D_X(_CameraDepthTexture, _FirstDepthMipOffset + (uint2)positionSS.xy).r;
#endif

#endif
}


float GetDepthSample(float2 positionSS, bool lowerRes)
{
#if CENTRAL_AND_SAMPLE_DEPTH_FETCH_SAME_METHOD
    return GetDepthForCentral(positionSS);
#endif

#ifdef FULL_RES

#if HALF_RES_DEPTH_WHEN_FULL_RES
    return LOAD_TEXTURE2D_X(_CameraDepthTexture, _FirstDepthMipOffset + positionSS / 2).r;
#endif

    return LOAD_TEXTURE2D_X(_CameraDepthTexture, positionSS).r;


#else // FULL_RES

#if LOWER_RES_SAMPLE
    if (lowerRes)
    {
        return LOAD_TEXTURE2D_X(_CameraDepthTexture, _SecondDepthMipOffset + (uint2)positionSS.xy / 2).r;
    }
    else
#endif
    {
        return LOAD_TEXTURE2D_X(_CameraDepthTexture, _FirstDepthMipOffset + (uint2)positionSS.xy).r;
    }
#endif
}

float GTAOFastAcos(float x)
{
    float outVal = -0.156583 * abs(x) + HALF_PI;
    outVal *= sqrt(1.0 - abs(x));
    return x >= 0 ? outVal : PI - outVal;
}

// --------------------------------------------
// Output functions
// --------------------------------------------
float PackAOOutput(float AO, float depth)
{
    uint packedDepth = PackFloatToUInt(depth, 0, 23);
    uint packedAO = PackFloatToUInt(AO, 24, 8);
    uint packedVal = packedAO | packedDepth;
    // If it is a NaN we have no guarantee the sampler will keep the bit pattern, hence we invalidate the depth, meaning that the various bilateral passes will skip the sample.
    if ((packedVal & 0x7FFFFFFF) > 0x7F800000)
    {
        packedVal = packedAO;
    }

    // We need to output as float as gather4 on an integer texture is not always supported.
    return asfloat(packedVal);
}

void UnpackData(float data, out float AO, out float depth)
{
    depth = UnpackUIntToFloat(asuint(data), 0, 23);
    AO = UnpackUIntToFloat(asuint(data), 24, 8);
}

void UnpackGatheredData(float4 data, out float4 AOs, out float4 depths)
{
    UnpackData(data.x, AOs.x, depths.x);
    UnpackData(data.y, AOs.y, depths.y);
    UnpackData(data.z, AOs.z, depths.z);
    UnpackData(data.w, AOs.w, depths.w);
}

void GatherAOData(TEXTURE2D_X_FLOAT(_AODataSource), float2 UV, out float4 AOs, out float4 depths)
{
    float4 data = GATHER_TEXTURE2D_X(_AODataSource, s_point_clamp_sampler, UV);
    UnpackGatheredData(data, AOs, depths);
}

float OutputFinalAO(float AO)
{
    return 1.0f - PositivePow(AO, _AOIntensity);
}
