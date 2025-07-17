#ifndef _SAMPLING_PSEUDORANDOM_HLSL_
#define _SAMPLING_PSEUDORANDOM_HLSL_

#include "Common.hlsl"
#include "Hashes.hlsl"

// Xor shift PRNG
struct QrngXorShift
{
    uint state;

    void Init(uint2 pixelCoord, uint startSampleIndex)
    {
        state = PixelHash(pixelCoord, startSampleIndex);
    }

    void Init(uint seed, uint startSampleIndex)
    {
        state = seed;
    }

    float GetFloat(uint dimension)
    {
        state = XorShift32(state);
        return UintToFloat01(state);
    }

    void NextSample()
    {
    }
};

// From paper: "Hash Functions for GPU Rendering" by Jarzynski & Olano)
struct QrngPcg4D
{
    uint4 state;

    void Init(uint2 pixelCoord, uint startSampleIndex)
    {
        // Seed for PCG uses a sequential sample number in 4th channel, which increments on every RNG call and starts from 0
        state = uint4(pixelCoord, startSampleIndex, 0);
    }

    void Init(uint seed, uint startSampleIndex)
    {
        state = uint4(seed, 1, startSampleIndex, 0);
    }

    float GetFloat(int dimension)
    {
        state.w++;
        return UintToFloat01(Pcg4d(state).x);
    }

    void NextSample()
    {
        state.z++;
    }
};

#endif // _SAMPLING_PSEUDORANDOM_HLSL_
