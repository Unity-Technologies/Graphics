#ifndef RANDOM_UAV_SHARED_INCLUDED
#define RANDOM_UAV_SHARED_INCLUDED

#define ONE_THIRD float(1.0 / 3.0)
#define TWO_THIRDS float(2.0 / 3.0)

// Inputs
float4 _ImageSize; // Needed as the _CameraOpaqueTexture_TexelSize can change based on URP Asset settings

#if SHADER_API_PSSL
RWStructuredBuffer<float4> _UAVBuffer   : register(u1);
RWTexture2D<float4> _UAVTextureBuffer   : register(u0);
#else
RWStructuredBuffer<float4> _UAVBuffer   : register(u2);
RWTexture2D<float4> _UAVTextureBuffer   : register(u1);
#endif

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

// Calculate the index
int CalculateBufferIndex(float2 uv, float width, float height)
{
    int x = int(floor(uv.x * width));
    int y = int(floor(uv.y * height));
    return y * int(width) + x;
}

uint2 CalculateTextureBufferCoord(float2 uv, float width, float height)
{
    return uint2(uv.x * width, uv.y * height);
}

float EaseInOutQuad(float x)
{
    return x < 0.5 ? 2 * x * x : 1 - pow(-2 * x + 2, 2) / 2;
}

void WriteToBuffer(float2 uv, float4 value)
{
    const int index = CalculateBufferIndex(uv,_ImageSize.x, _ImageSize.y);
    _UAVBuffer[index] = value;
}

float4 ReadFromBuffer(float2 uv)
{
    const int index = CalculateBufferIndex(uv,_ImageSize.x, _ImageSize.y);
    return _UAVBuffer[index];
}

void WriteToTextureBuffer(float2 uv, float4 value)
{
    const uint2 coord = CalculateTextureBufferCoord(uv,_ImageSize.x, _ImageSize.y);
    _UAVTextureBuffer[coord] = value;
}

float4 ReadFromTextureBuffer(float2 uv)
{
    const uint2 coord = CalculateTextureBufferCoord(uv,_ImageSize.x, _ImageSize.y);
    return _UAVTextureBuffer[coord];
}

#endif //RANDOM_UAV_SHARED_INCLUDED
