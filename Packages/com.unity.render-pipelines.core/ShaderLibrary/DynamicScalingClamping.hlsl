#ifndef UNITY_DYNAMIC_SCALING_CLAMPING_INCLUDED
#define UNITY_DYNAMIC_SCALING_CLAMPING_INCLUDED

// Functions to clamp UVs to use when RTHandle system is used.

float2 ClampUV(float2 UV, float2 texelSize, float numberOfTexels, float2 scale)
{
    float2 maxCoord = scale - numberOfTexels * texelSize;
    return min(UV, maxCoord);
}

float2 ClampUV(float2 UV, float2 texelSize, float numberOfTexels)
{
    return ClampUV(UV, texelSize, numberOfTexels, _RTHandleScale.xy);
}

float2 ClampAndScaleUV(float2 UV, float2 texelSize, float numberOfTexels, float2 scale)
{
    float2 maxCoord = 1.0f - numberOfTexels * texelSize;
    return min(UV, maxCoord) * scale;
}

float2 ClampAndScaleUV(float2 UV, float2 texelSize, float numberOfTexels)
{
    return ClampAndScaleUV(UV, texelSize, numberOfTexels, _RTHandleScale.xy);
}

// This is assuming half a texel offset in the clamp.
float2 ClampUVForBilinear(float2 UV, float2 texelSize)
{
    return ClampUV(UV, texelSize, 0.5f);
}

float2 ClampUVForBilinear(float2 UV)
{
    return ClampUV(UV, _ScreenSize.zw, 0.5f);
}

float2 ClampAndScaleUVForBilinear(float2 UV, float2 texelSize)
{
    return ClampAndScaleUV(UV, texelSize, 0.5f);
}

// This is assuming full screen buffer and half a texel offset for the clamping.
float2 ClampAndScaleUVForBilinear(float2 UV)
{
    return ClampAndScaleUV(UV, _ScreenSize.zw, 0.5f);
}

float2 ClampAndScaleUVForPoint(float2 UV)
{
    return min(UV, 1.0f) * _RTHandleScale.xy;
}

#endif
