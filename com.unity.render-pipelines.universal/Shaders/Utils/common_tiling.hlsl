#ifndef UNIVERSAL_COMMON_TILING_INCLUDED
#define UNIVERSAL_COMMON_TILING_INCLUDED

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
