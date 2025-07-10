#ifndef _SAMPLING_QUASIRANDOM_HLSL_
#define _SAMPLING_QUASIRANDOM_HLSL_

#include "Common.hlsl"
#include "SobolSampling.hlsl"
#include "SobolBluenoiseSampling.hlsl"

static const uint kMaxSobolDim = SOBOL_MATRICES_COUNT;

// Sobol sampler with Owen scrambling, from paper: Practical Hash-based Owen Scrambling by Burley
// infinite dims, 2097151 max samples, pixel tiling wraps at 65536
// Define QRNG_SOBOL_02 to only use first 2 sobol dims and rely on scrambling for the others, this
// effectively makes every pair of dims a perfect (0,2) sequence
struct QrngSobol
{
    uint pixelSeed;
    uint sampleIndex;

    void Init(uint2 pixelCoord, uint startSampleIndex)
    {
        Init(PixelHash(pixelCoord), startSampleIndex);
    }

    void Init(uint seed, uint startSampleIndex)
    {
        pixelSeed = seed;
        sampleIndex = startSampleIndex;
    }

    float GetFloat(uint dimension)
    {
    #ifdef QRNG_SOBOL_02
        uint index = NestedUniformOwenScramble(sampleIndex, pixelSeed ^ ( dimension / 2));
        return GetOwenScrambledSobolSample(index, dimension & 1, LowBiasHash32(dimension, pixelSeed));
    #else
        uint scrambleSeed = LowBiasHash32(pixelSeed, dimension);
        uint shuffleSeed = pixelSeed;
        return GetOwenScrambledSobolSample(sampleIndex ^ shuffleSeed, dimension % kMaxSobolDim, scrambleSeed);
    #endif
    }

    void NextSample()
    {
        sampleIndex++;
    }
};

// From paper: "A Low-Discrepancy Sampler that Distributes Monte Carlo Errors as a Blue Noise in Screen Space" by Heitz and Belcour
// 256 max dims, 256 max samples (beyond 256, the sequence keeps going with another set of 256 samples belonging to another dim, and so on every 256 samples), pixel tiling wraps at 128
struct QrngSobolBlueNoise
{
    uint2 pixelCoord;
    uint sampleIndex;

    void Init(uint2 pixelCoord_, uint startSampleIndex)
    {
        pixelCoord = pixelCoord_;
        sampleIndex = startSampleIndex;
    }

    void Init(uint seed, uint startSampleIndex)
    {
        Init(seed / 256, seed % 256);
    }

    float GetFloat(uint dimension)
    {
        // If we go past the number of stored samples per dim, just shift all to the next pair of dimensions
        dimension += (sampleIndex / 256) * 2;
        return GetBNDSequenceSample(pixelCoord, sampleIndex, dimension);
    }

    void NextSample()
    {
        sampleIndex++;
    }
};

// From paper: "Screen-Space Blue-Noise Diffusion of Monte Carlo Sampling Error via Hierarchical Ordering of Pixels" by Ahmed and Wonka
// infinite dims and samples, pixel tiling depends on target sample count. The more samples, the smaller the tile (ex: for 256 samples, tiling size is 4096)
// define QRNG_GLOBAL_SOBOL_ENHANCED_TILING to get tiling to always wrap at 65536
// Define QRNG_SOBOL_02 to only use first 2 sobol dims and rely on scrambling for the others
struct QrngGlobalSobolBlueNoise
{
    uint pixelMortonCode;
    uint log2SamplesPerPixel;
    uint sampleIndex;

    void Init(uint2 pixelCoord, uint startSampleIndex, uint perPixelSampleCount = 256)
    {
        pixelMortonCode = EncodeMorton2D(pixelCoord);
        log2SamplesPerPixel = Log2IntUp(perPixelSampleCount);
        sampleIndex = startSampleIndex;
    }

    void Init(uint seed, uint startSampleIndex, uint perPixelSampleCount = 256)
    {
        Init(uint2(seed & 0xFFFF, seed >> 16), startSampleIndex, perPixelSampleCount);
    }

    float GetFloat(uint dimension)
    {
    #ifdef QRNG_SOBOL_02
        uint index = NestedUniformOwenScramble(sampleIndex, LowBiasHash32(dimension/2, 0xe0aaaf75)) & ((1U << log2SamplesPerPixel) - 1U);
        return GetOwenScrambledZShuffledSobolSample(index, dimension, 2, pixelMortonCode, log2SamplesPerPixel);
    #else
        return GetOwenScrambledZShuffledSobolSample(sampleIndex, dimension, kMaxSobolDim, pixelMortonCode, log2SamplesPerPixel);
    #endif
    }

    void NextSample()
    {
        sampleIndex++;
    }
};

// Kronecker sequence from paper "Optimizing Kronecker Sequences for Multidimensional Sampling"
//fast but lower quality than Sobol, infinite dims and samples, pixel tiling wraps at 65536
//define QRNG_KRONECKER_ENHANCED_QUALITY to add small scale jitter
struct QrngKronecker
{
    uint cranleyPattersonSeed;
    uint shuffledSampleIndex;
#ifdef QRNG_KRONECKER_ENHANCED_QUALITY
    int sampleIndex;
#endif

    void Init(uint2 pixelCoord, uint startSampleIndex)
    {
        Init(PixelHash(pixelCoord), startSampleIndex);
    }

    void Init(uint seed, uint startSampleIndex)
    {
        uint hash = seed;
        cranleyPattersonSeed = hash;
        uint shuffledStartIndex = (startSampleIndex + hash) % (1 << 20);
        shuffledSampleIndex = shuffledStartIndex;
#ifdef QRNG_KRONECKER_ENHANCED_QUALITY
        sampleIndex = startSampleIndex+1;
#endif
    }

    float GetFloat(uint dimension)
    {
        const uint alphas[]= { // values are stored multiplied by (1 << 32)
            // R2 from http://extremelearning.com.au/unreasonable-effectiveness-of-quasirandom-sequences/
            3242174889, 2447445414,
            // K21_2 from Optimizing Kronecker Sequences for Multidimensional Sampling
            3316612456, 1538627358,
        };

        // compute random offset to apply to the sequence (using another Kronecker sequence)
        uint cranleyPattersonRot = cranleyPattersonSeed + 3646589397 * (dimension / 4);

#ifdef QRNG_KRONECKER_ENHANCED_QUALITY // add small scale jitter as explained in paper
        const float alphaJitter[] = { 2681146017, 685201898 };

        uint jitter = alphaJitter[dimension % 2] * shuffledSampleIndex;
        float amplitude = 0.05 * 0.78 / sqrt(2) * rsqrt(float(sampleIndex));
        cranleyPattersonRot += jitter * uint(amplitude);
#endif
        // Kronecker sequence evaluation
        return UintToFloat01(cranleyPattersonRot + alphas[dimension % 4] * shuffledSampleIndex);
    }

    void NextSample()
    {
        // shuffledSampleIndex modulo 1048576 to avoid numerical precision issues when evaluating the Kronecker sequence
        shuffledSampleIndex = (shuffledSampleIndex + 1) % (1 << 20);
#ifdef QRNG_KRONECKER_ENHANCED_QUALITY
        sampleIndex++;
#endif
    }
};


#endif // _SAMPLING_QUASIRANDOM_HLSL_
