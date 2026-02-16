#ifndef _SAMPLING_PATHTRACINGSAMPLER_HLSL_
#define _SAMPLING_PATHTRACINGSAMPLER_HLSL_

#if defined(QRNG_METHOD_RANDOM_XOR_SHIFT) || defined(QRNG_METHOD_RANDOM_PCG_4D)
#include "Packages/com.unity.render-pipelines.core/Runtime/Sampling/PseudoRandom.hlsl"
#else
#include "Packages/com.unity.render-pipelines.core/Runtime/Sampling/QuasiRandom.hlsl"
#endif

// global dimension offset (could be used to alter the noise pattern)
#ifndef QRNG_OFFSET
#define QRNG_OFFSET 0
#endif

#ifndef QRNG_SAMPLES_PER_BOUNCE
#define QRNG_SAMPLES_PER_BOUNCE 32
#endif

struct PathTracingSampler
{
    QRNG_TYPE generator;
    int pathIndex;
    int bounceIndex;
    uint2 pixelCoord;

    void Init(uint2 startPixelCoord, uint startPathIndex, uint perPixelPathCount = 256)
    {
        #if defined(QRNG_METHOD_GLOBAL_SOBOL_BLUE_NOISE)
            generator.Init(startPixelCoord, startPathIndex, perPixelPathCount);
        #else
            generator.Init(startPixelCoord, startPathIndex);
        #endif
        pixelCoord = startPixelCoord;
        pathIndex = startPathIndex;
        bounceIndex = 0;
    }

    float GetSample1D(int dimension)
    {
        uint actualDimension = QRNG_OFFSET + QRNG_SAMPLES_PER_BOUNCE * bounceIndex + dimension;
        return generator.GetSample(actualDimension).x;
    }

    float2 GetSample2D(int dimension)
    {
        uint actualDimension = QRNG_OFFSET + QRNG_SAMPLES_PER_BOUNCE * bounceIndex + dimension;
        return generator.GetSample(actualDimension);
    }

    void NextBounce()
    {
        bounceIndex++;
    }

    void NextPath()
    {
        generator.NextSample();
        bounceIndex = 0;
    }
};

#endif // _SAMPLING_PATHTRACINGSAMPLER_HLSL_
