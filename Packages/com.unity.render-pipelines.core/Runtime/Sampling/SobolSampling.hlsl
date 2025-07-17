#ifndef _SAMPLING_SOBOLSAMPLING_HLSL_
#define _SAMPLING_SOBOLSAMPLING_HLSL_

#define SOBOL_MATRIX_SIZE 52
#define SOBOL_MATRICES_COUNT 1024

#include "SamplingResources.hlsl"
#include "Hashes.hlsl"
#include "Common.hlsl"

// HLSLcc cannot correctly translate `reversebits(x)` for large unsigned integers.
// Therefore, when using HLSLcc, we use our own implementation. https://jira.unity3d.com/browse/GFXFEAT-629
#ifdef UNITY_COMPILER_HLSLCC
uint ReverseBitsSafe(uint x)
{
    x = ((x >> 1) & 0x55555555u) | ((x & 0x55555555u) << 1);
    x = ((x >> 2) & 0x33333333u) | ((x & 0x33333333u) << 2);
    x = ((x >> 4) & 0x0f0f0f0fu) | ((x & 0x0f0f0f0fu) << 4);
    x = ((x >> 8) & 0x00ff00ffu) | ((x & 0x00ff00ffu) << 8);
    x = ((x >> 16) & 0xffffu) | ((x & 0xffffu) << 16);
    return x;
}
#else
#define ReverseBitsSafe reversebits
#endif

// See https://psychopath.io/post/2021_01_30_building_a_better_lk_hash
uint LaineKarrasPermutation(uint x, uint seed)
{
    x ^= x * 0x3d20adea;
    x += seed;
    x *= (seed >> 16) | 1;
    x ^= x * 0x05526c56;
    x ^= x * 0x53a22864;
    return x;
}

uint NestedUniformOwenScramble(uint x, uint seed)
{
    x = ReverseBitsSafe(x);
    x = LaineKarrasPermutation(x, seed);
    x = ReverseBitsSafe(x);
    return x;
}

//See https://psychopath.io/post/2022_08_14_a_fast_hash_for_base_4_owen_scrambling
uint LaineKarrasStylePermutationBase4(uint x, uint seed)
{
    x ^= x * 0x3d20adeau;
    x ^= (x >> 1) & (x << 1) & 0x55555555u;
    x += seed;
    x *= (seed >> 16) | 1;
    x ^= (x >> 1) & (x << 1) & 0x55555555u;
    x ^= x * 0x05526c56u;
    x ^= x * 0x53a22864u;
    return x;
}

uint NestedUniformScrambleBase4(uint x, uint seed)
{
    x = ReverseBitsSafe(x);
    x = LaineKarrasStylePermutationBase4(x, seed);
    x = ReverseBitsSafe(x);
    return x;
}

// "Insert" a 0 bit after each of the 16 low bits of x.
// Ref: https://fgiesen.wordpress.com/2009/12/13/decoding-morton-codes/
uint Part1By1(uint x)
{
    x &= 0x0000ffff;                  // x = ---- ---- ---- ---- fedc ba98 7654 3210
    x = (x ^ (x << 8)) & 0x00ff00ff; // x = ---- ---- fedc ba98 ---- ---- 7654 3210
    x = (x ^ (x << 4)) & 0x0f0f0f0f; // x = ---- fedc ---- ba98 ---- 7654 ---- 3210
    x = (x ^ (x << 2)) & 0x33333333; // x = --fe --dc --ba --98 --76 --54 --32 --10
    x = (x ^ (x << 1)) & 0x55555555; // x = -f-e -d-c -b-a -9-8 -7-6 -5-4 -3-2 -1-0
    return x;
}

uint EncodeMorton2D(uint2 coord)
{
    return (Part1By1(coord.y) << 1) + Part1By1(coord.x);
}

uint SobolSampleUint(uint index, int dimension, uint indexMaxBitCount)
{
    uint result = 0;

#ifdef UNIFIED_RT_BACKEND_HARDWARE
    // Raytracing tracing shaders compile only with unrolled loop, but unrolling fails in the compute path.
    // TODO: test again when Unity updates the shader compiler
    [unroll]
#endif
    for (uint i = dimension * SOBOL_MATRIX_SIZE; i < dimension * SOBOL_MATRIX_SIZE + indexMaxBitCount; index >>= 1, ++i)
    {
        result ^= _SobolMatricesBuffer[i] * (index & 1);
    }

    return result;
}

uint SobolSampleUint(uint2 index64, uint dimension)
{
    uint result = 0;
    uint i = dimension * SOBOL_MATRIX_SIZE;

    for (; i < dimension * SOBOL_MATRIX_SIZE + 32; index64.x >>= 1, ++i)
    {
        result ^= _SobolMatricesBuffer[i] * (index64.x & 1);
    }

    for (; i < dimension * SOBOL_MATRIX_SIZE + 32+20; index64.y >>= 1, ++i)
    {
        result ^= _SobolMatricesBuffer[i] * (index64.y & 1);
    }

    return result;
}

uint GetMortonShuffledSampleIndex(uint index, uint pixelMortonIndex, uint mortonShuffleSeed, uint log2SamplesPerPixel)
{
    uint mortonIndexShuffled = NestedUniformScrambleBase4(pixelMortonIndex, mortonShuffleSeed);

    uint sampleIndexForPixel = (mortonIndexShuffled << log2SamplesPerPixel) | index;
    return sampleIndexForPixel;
}

uint2 GetMortonShuffledSampleIndex64(uint index, uint pixelMortonIndex, uint mortonShuffleSeed, uint log2SamplesPerPixel)
{
    uint mortonIndexShuffled = NestedUniformScrambleBase4(pixelMortonIndex, mortonShuffleSeed);

    uint2 sampleIndex64;
    sampleIndex64.x = (mortonIndexShuffled << log2SamplesPerPixel) | index;
    sampleIndex64.y = mortonIndexShuffled >> (32 - log2SamplesPerPixel);
    return sampleIndex64;
}

float GetSobolSample(uint index, int dimension)
{
    uint result = SobolSampleUint(index, dimension, 20);
    return UintToFloat01(result);
}

float GetOwenScrambledSobolSample(uint sampleIndex, uint dim, uint valueScrambleSeed)
{
    uint sobolUInt = SobolSampleUint(sampleIndex, dim, 20);
    uint result = NestedUniformOwenScramble(sobolUInt, valueScrambleSeed);
    return UintToFloat01(result);
}

float GetOwenScrambledZShuffledSobolSample(uint sampleIndex, uint dim, uint maxDim, uint pixelMortonCode, uint log2SamplesPerPixel)
{
    const uint kSeedValueScramble = 0xab773au;
    uint mortonShuffleSeed = LowBiasHash32(dim, 0);
    uint valueScrambleSeed = LowBiasHash32(dim, kSeedValueScramble);

#ifdef QRNG_GLOBAL_SOBOL_ENHANCED_TILING
    uint2 shuffledSampleIndex = GetMortonShuffledSampleIndex64(sampleIndex, pixelMortonCode, mortonShuffleSeed, log2SamplesPerPixel);
    uint sobolUInt = SobolSampleUint(shuffledSampleIndex, dim % maxDim);
#else
    uint shuffledSampleIndex = GetMortonShuffledSampleIndex(sampleIndex, pixelMortonCode, mortonShuffleSeed, log2SamplesPerPixel);
    uint sobolUInt = SobolSampleUint(shuffledSampleIndex, dim % maxDim, 32);
#endif

    uint result = NestedUniformOwenScramble(sobolUInt, valueScrambleSeed);
    return UintToFloat01(result);
}


#endif // UNITY_SOBOL_SAMPLING_INCLUDED
