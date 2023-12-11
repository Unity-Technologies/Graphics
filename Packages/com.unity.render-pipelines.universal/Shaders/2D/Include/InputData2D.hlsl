
#ifndef INPUT_DATA_2D_INCLUDED
#define INPUT_DATA_2D_INCLUDED

struct InputData2D
{
    float2 uv;
    half2 lightingUV;

    #if defined(DEBUG_DISPLAY)
    float3 positionWS;
    float4 positionCS;
    
    // Mipmap Streaming Debug
    float4 texelSize;
    float4 mipInfo;
    float4 streamInfo;
    uint mipCount;
    #endif
};

void InitializeInputData(float2 uv, half2 lightingUV, out InputData2D inputData)
{
    inputData = (InputData2D)0;

    inputData.uv = uv;
    inputData.lightingUV = lightingUV;
}

void InitializeInputData(float2 uv, out InputData2D inputData)
{
    InitializeInputData(uv, 0, inputData);
}

#endif
