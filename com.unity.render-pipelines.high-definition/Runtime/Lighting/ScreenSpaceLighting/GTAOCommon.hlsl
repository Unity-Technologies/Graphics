#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

CBUFFER_START(GTAOUniformBuffer)
float4 _AOBufferSize;
float4 _AOParams0;     
float4 _AOParams1;     
float4 _AOParams2;
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
#define _AORTHandleSize _AOParams2.xy

// If this is set to 0 best quality is achieved when full res, but performance is significantly lower.
// If set to 1, when full res, it may lead to extra aliasing and loss of detail, but still significant higher quality than half res.
#define HALF_RES_DEPTH_WHEN_FULL_RES 1 // Make this an option.
#define HALF_RES_DEPTH_WHEN_FULL_RES_FOR_CENTRAL 0 

// This increases the quality when running with half resolution buffer, however it adds a bit of cost. Note that it will not have artifact as we already don't allow samples to be at the edge of the depth buffer.
#define MIN_DEPTH_GATHERED_FOR_CENTRAL 1

#define CENTRAL_AND_SAMPLE_DEPTH_FETCH_SAME_METHOD 0

#define LOWER_RES_SAMPLE 1

float GetMinDepth(float2 localUVs)
{
    localUVs = ClampAndScaleUVForBilinear(localUVs, _AOBufferSize.zw);
    localUVs.x = localUVs.x * 0.5f;
    localUVs.y = localUVs.y * (1.0f / 3.0f) + (2.0f / 3.0f);

    float4 gatheredDepth = GATHER_TEXTURE2D_X(_DepthPyramidTexture, s_point_clamp_sampler, localUVs);
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
    return LOAD_TEXTURE2D_X(_DepthPyramidTexture, float2(0.0f, _AORTHandleSize.y) + positionSS / 2).r;
#endif 

#else  // HALF_RES_DEPTH_WHEN_FULL_RES
    return LOAD_TEXTURE2D_X(_DepthPyramidTexture, positionSS).r;
#endif

#else // FULL_RES

#if MIN_DEPTH_GATHERED_FOR_CENTRAL

    float2 localUVs = positionSS.xy * _AOBufferSize.zw;
    return GetMinDepth(localUVs);
#else

    return LOAD_TEXTURE2D_X(_DepthPyramidTexture, float2(0.0f, _AORTHandleSize.y) + (uint2)positionSS.xy).r;
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
    return LOAD_TEXTURE2D_X(_DepthPyramidTexture, float2(0.0f, _AORTHandleSize.y) + positionSS / 2).r;
#endif 

    return LOAD_TEXTURE2D_X(_DepthPyramidTexture, positionSS).r;


#else // FULL_RES

#if LOWER_RES_SAMPLE
    if (lowerRes)
    {
        return LOAD_TEXTURE2D_X(_DepthPyramidTexture, float2(_AORTHandleSize.x * 0.5f, _AORTHandleSize.y) + (uint2)positionSS.xy / 2).r;
    }
    else
#endif
    {
        return LOAD_TEXTURE2D_X(_DepthPyramidTexture, float2(0.0f, _AORTHandleSize.y) + (uint2)positionSS.xy).r;
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
uint PackAOOutput(float AO, float depth)
{
     // 24 depth,  8 bit AO
    uint packedVal = 0;
    packedVal = BitFieldInsert(0x000000ff, UnpackInt(AO, 8), packedVal);
    packedVal = BitFieldInsert(0xffffff00, UnpackInt(depth, 24) << 8, packedVal);
    return packedVal;
}

void UnpackData(uint data, out float AO, out float depth)
{
    AO = UnpackUIntToFloat(data, 0, 8);
    depth = UnpackUIntToFloat(data, 8, 24);
}

void UnpackGatheredData(uint4 data, out float4 AOs, out float4 depths)
{
    UnpackData(data.x, AOs.x, depths.x);
    UnpackData(data.y, AOs.y, depths.y);
    UnpackData(data.z, AOs.z, depths.z);
    UnpackData(data.w, AOs.w, depths.w);
}
