#ifndef REBLUR_BILATERAL_UTILITIES_H_
#define REBLUR_BILATERAL_UTILITIES_H_

TEXTURE2D_X(_LightingDistanceHistoryBuffer);
TEXTURE2D_X_UINT(_AccumulationHistoryBuffer);
TEXTURE2D_X(_HistoryDepthTexture);

// Function that holds everything relative to the bilateral tap region
struct ReBlurBilateralData
{
    // Data of the top left pixel
    float4 signal0;
    float acc0;
    float linearDepth0;
    float w0;

    // Data of the top right pixel
    float4 signal1;
    float acc1;
    float linearDepth1;
    float w1;

    // Data of the bottom left pixel
    float4 signal2;
    float acc2;
    float linearDepth2;
    float w2;

    // Data of the bottom right pixel
    float4 signal3;
    float acc3;
    float linearDepth3;
    float w3;

    // Linear interpolation factors
    float2 interpolationFactors;
};

void EvaluateBilateralData(float2 tapCoords, out ReBlurBilateralData data)
{
    ZERO_INITIALIZE(ReBlurBilateralData, data);

    // Read the history signal
    int2 tap0 = floor(tapCoords);
    if (all(tap0 > 0) && all(tap0 <_ScreenSize.xy))
    {
        data.signal0 = LOAD_TEXTURE2D_X(_LightingDistanceHistoryBuffer, tap0);
        data.acc0 = LOAD_TEXTURE2D_X(_AccumulationHistoryBuffer, tap0).x;
        data.linearDepth0 = Linear01Depth(LOAD_TEXTURE2D_X(_HistoryDepthTexture, tap0).x, _ZBufferParams);
    }
    else
    {
        data.linearDepth0 = 1.0;
    }

    int2 tap1 = tap0 + int2(1, 0);
    if (all(tap1 > 0) && all(tap1 <_ScreenSize.xy))
    {
        data.signal1 = LOAD_TEXTURE2D_X(_LightingDistanceHistoryBuffer, tap1);
        data.acc1 = LOAD_TEXTURE2D_X(_AccumulationHistoryBuffer, tap1).x;
        data.linearDepth1 = Linear01Depth(LOAD_TEXTURE2D_X(_HistoryDepthTexture, tap1).x, _ZBufferParams);
    }
    else
    {
        data.linearDepth1 = 1.0;
    }

    int2 tap2 = tap0 + int2(0, 1);
    if (all(tap2 > 0) && all(tap2 <_ScreenSize.xy))
    {
        data.signal2 = LOAD_TEXTURE2D_X(_LightingDistanceHistoryBuffer, tap2);
        data.acc2 = LOAD_TEXTURE2D_X(_AccumulationHistoryBuffer, tap2).x;
        data.linearDepth2 = Linear01Depth(LOAD_TEXTURE2D_X(_HistoryDepthTexture, tap2).x, _ZBufferParams);
    }
    else
    {
        data.linearDepth2 = 1.0;
    }

    int2 tap3 = tap0 + int2(1, 1);
    if (all(tap3 > 0) && all(tap3 <_ScreenSize.xy))
    {
        data.signal3 = LOAD_TEXTURE2D_X(_LightingDistanceHistoryBuffer, tap3);
        data.acc3 = LOAD_TEXTURE2D_X(_AccumulationHistoryBuffer, tap3).x;
        data.linearDepth3 = Linear01Depth(LOAD_TEXTURE2D_X(_HistoryDepthTexture, tap3).x, _ZBufferParams);
    }
    else
    {
        data.linearDepth3 = 1.0;
    }

    // Evaluate the interpolation factors
    data.interpolationFactors = tapCoords - tap0;
}

// Function that evaluates 
float4 ComputeDisOcclusion(float linearDepth, ReBlurBilateralData data)
{
    float4 disocc = 0;
    disocc.x = data.linearDepth0 != 1.0 ? max(0.0, 1.0 - abs(data.linearDepth0 - linearDepth)) : 0.0;
    disocc.y = data.linearDepth1 != 1.0 ? max(0.0, 1.0 - abs(data.linearDepth1 - linearDepth)) : 0.0;
    disocc.z = data.linearDepth2 != 1.0 ? max(0.0, 1.0 - abs(data.linearDepth2 - linearDepth)) : 0.0;
    disocc.w = data.linearDepth3 != 1.0 ? max(0.0, 1.0 - abs(data.linearDepth3 - linearDepth)) : 0.0;
    return disocc;
}


float3 GetVirtualPosition(float3 positionWS, float3 viewWS, float NoV, float roughness, float hitDist)
{
    float f = GetSpecularDominantFactor(NoV, roughness);
    return positionWS - viewWS * hitDist * f;
}

#endif // REBLUR_BILATERAL_UTILITIES_H_
