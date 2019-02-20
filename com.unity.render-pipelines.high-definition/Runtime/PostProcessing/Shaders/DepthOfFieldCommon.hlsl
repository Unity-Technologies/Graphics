#ifndef DEPTH_OF_FIELD_COMMON
#define DEPTH_OF_FIELD_COMMON

struct TileData
{
    uint position;
};

uint PackKernelCoord(float2 coords)
{
    return uint(f32tof16(coords.x) | f32tof16(coords.y) << 16);
}

float2 UnpackKernelCoord(StructuredBuffer<uint> kernel, uint id)
{
    uint coord = kernel[id];
    return float2(f16tof32(coord), f16tof32(coord >> 16));
}

uint PackTileCoord(uint2 coord)
{
    return (coord.x << 16u) | coord.y;
}

uint2 UnpackTileCoord(TileData tile)
{
    uint pos = tile.position;
    return uint2((pos >> 16u) & 0xffff, pos & 0xffff);
}

#endif // DEPTH_OF_FIELD_COMMON
