
#ifndef INPUT_DATA_2D_INCLUDED
#define INPUT_DATA_2D_INCLUDED

struct InputData2D
{
    float2 uv;
    half2 lightingUV;

    #if defined(_DEBUG_SHADER)
    float3 positionWS;
    float4 texelSize;
    float4 mipInfo;
    uint mipCount;
    #endif
};

InputData2D CreateInputData(float2 uv, half2 lightingUV)
{
    InputData2D inputData = (InputData2D)0;

    inputData.uv = uv;
    inputData.lightingUV = lightingUV;

    return inputData;
}

InputData2D CreateInputData(float2 uv)
{
    return CreateInputData(uv, 0);
}

#endif
