#ifndef CAPSULE_SHADOWs_COMMON_HLSL
#define CAPSULE_SHADOWs_COMMON_HLSL

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/CapsuleShadows/CapsuleShadowsCommon.cs.hlsl"

// store in gamma 2 to increase precision at the low end
float PackCapsuleVisibility(float visibility)   { return 1.f - sqrt(max(0.f, visibility)); }
float UnpackCapsuleVisibility(float texel)      { return Sq(1.f - texel); }

CapsuleShadowFilterTile makeCapsuleShadowFilterTile(uint2 tileCoord, uint viewIndex, uint activeBits, uint clearBits)
{
    CapsuleShadowFilterTile tile;
    tile.coord = (viewIndex << 30) | (tileCoord.y << 15) | tileCoord.x;
    tile.bits = (clearBits << 8) | activeBits;
    return tile;
}
uint GetFilterViewIndex(CapsuleShadowFilterTile tile)  { return tile.coord >> 30; }
uint2 GetFilterTileCoord(CapsuleShadowFilterTile tile) { return uint2(tile.coord & 0x7fffU, (tile.coord >> 15) & 0x7fffU); }
uint GetFilterActiveBits(CapsuleShadowFilterTile tile) { return tile.bits & 0xffU; }
uint GetFilterClearBits(CapsuleShadowFilterTile tile) { return tile.bits >> 8; }

#endif // ndef CAPSULE_SHADOWs_COMMON_HLSL
