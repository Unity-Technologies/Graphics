#ifndef UNITY_PATH_TRACING_SAMPLING_INCLUDED
#define UNITY_PATH_TRACING_SAMPLING_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingSampling.hlsl"

float GetSample(uint2 coord, uint index, uint dim)
{
    // If we go past the number of stored samples per dim, just shift all to the next pair of dimensions
    dim += (index / 256) * 2;

    return GetBNDSequenceSample(coord, index, dim);
}

float4 GetSample4D(uint2 coord, uint index, uint dim)
{
    // If we go past the number of stored samples per dim, just shift all to the next pair of dimensions
    dim += (index / 256) * 2;

    float4 randomSample;
    randomSample.x = GetBNDSequenceSample(coord, index, dim);
    randomSample.y = GetBNDSequenceSample(coord, index, dim + 1);
    randomSample.z = GetBNDSequenceSample(coord, index, dim + 2);
    randomSample.w = GetBNDSequenceSample(coord, index, dim + 3);

    return randomSample;
}

bool RussianRouletteTest(float threshold, float value, float rand, out float factor, bool skip = false)
{
    if (skip || value >= threshold)
    {
        factor = 1.0;
        return true;
    }

    if (rand * threshold >= value)
    {
        factor = 1.0;
        return false;
    }

    factor = threshold / value;

    return true;
}

#endif // UNITY_PATH_TRACING_SAMPLING_INCLUDED
