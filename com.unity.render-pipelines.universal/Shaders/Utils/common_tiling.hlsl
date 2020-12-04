#ifndef UNIVERSAL_COMMON_TILING_INCLUDED
#define UNIVERSAL_COMMON_TILING_INCLUDED

#define PREFERRED_CBUFFER_SIZE (64 * 1024)
#define SIZEOF_VEC4_TILEDATA 1 // uint4
#define SIZEOF_VEC4_PUNCTUALLIGHTDATA 5 // 5 * float4
#define MAX_DEPTHRANGE_PER_CBUFFER_BATCH (PREFERRED_CBUFFER_SIZE / 4) // Should be ushort, but extra unpacking code is "too expensive"
#define MAX_TILES_PER_CBUFFER_PATCH (PREFERRED_CBUFFER_SIZE / (16 * SIZEOF_VEC4_TILEDATA))
#define MAX_PUNCTUALLIGHT_PER_CBUFFER_BATCH (PREFERRED_CBUFFER_SIZE / (16 * SIZEOF_VEC4_PUNCTUALLIGHTDATA))
#define MAX_REL_LIGHT_INDICES_PER_CBUFFER_BATCH (PREFERRED_CBUFFER_SIZE / 4) // Should be ushort, but extra unpacking code is "too expensive"

// If we choose to perform light culling into tiles using compute shaders, then tile related information is stored in structured buffers.
#if defined(_DEFERRED_GPU_TILING)
#define FORCE_STRUCTBUFFER_FOR_TILING 1
#else
#define FORCE_STRUCTBUFFER_FOR_TILING 0
#endif

// Keep in sync with UseCBufferForDepthRange.ion
// Keep in sync with UseCBufferForTileData.
// Keep in sync with UseCBufferForLightData.
// Keep in sync with UseCBufferForLightList.
#if defined(SHADER_API_SWITCH)
#define USE_CBUFFER_FOR_DEPTHRANGE  0
#define USE_CBUFFER_FOR_TILELIST   (0 && !FORCE_STRUCTBUFFER_FOR_TILING)
#define USE_CBUFFER_FOR_LIGHTLIST  (0 && !FORCE_STRUCTBUFFER_FOR_TILING)
#define USE_CBUFFER_FOR_LIGHTDATA   1
#elif defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)
#define USE_CBUFFER_FOR_DEPTHRANGE  1
#define USE_CBUFFER_FOR_TILELIST   (1 && !FORCE_STRUCTBUFFER_FOR_TILING)
#define USE_CBUFFER_FOR_LIGHTLIST  (1 && !FORCE_STRUCTBUFFER_FOR_TILING)
#define USE_CBUFFER_FOR_LIGHTDATA   1
#else
#define USE_CBUFFER_FOR_DEPTHRANGE  0
#define USE_CBUFFER_FOR_TILELIST   (0 && !FORCE_STRUCTBUFFER_FOR_TILING)
#define USE_CBUFFER_FOR_LIGHTLIST  (0 && !FORCE_STRUCTBUFFER_FOR_TILING)
#define USE_CBUFFER_FOR_LIGHTDATA   1
#endif

struct CPUTile
{
    uint tileID;                 // 2 ushorts
    uint listBitMask;            // 1 uint
    uint relLightOffsetAndCount; // 2 ushorts
    uint unused;
};

struct GPUTile
{
    uint tileID;                 // 2 ushorts
    uint listBitMask;            // 1 uint
    uint relLightOffset;         // needs to be 32 bits
    uint relLightCount;
};

uint PackTileID(uint2 tileCoord)
{
    return tileCoord.x | (tileCoord.y << 16);
}

uint2 UnpackTileID(uint tileID)
{
    return uint2(tileID & 0xFFFF, (tileID >> 16) & 0xFFFF);
}

uint StoreLightBitmask(uint lightIndex, uint firstBit, uint bitCount)
{
    return lightIndex | (firstBit << 16) | (bitCount << 24);
}

uint LoadLightBitmask(uint lightIndexAndRange)
{
    uint firstBit = (lightIndexAndRange >> 16) & 0xFF;
    uint bitCount = lightIndexAndRange >> 24;
    uint lightBitmask = (0xFFFFFFFF >> (32 - bitCount)) << firstBit;
    return lightBitmask;
}

#endif
