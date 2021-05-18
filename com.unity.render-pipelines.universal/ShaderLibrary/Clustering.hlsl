#ifndef UNIVERSAL_CLUSTERING_INCLUDED
#define UNIVERSAL_CLUSTERING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

#if USE_CLUSTERED_LIGHTING

// TODO: Remove after PR #4039 is merged
// Select uint4 component by index.
// Helper to improve codegen for 2d indexing (data[x][y])
// Replace:
// data[i / 4][i % 4];
// with:
// select4(data[i / 4], i % 4);
uint ClusteringSelect4(uint4 v, uint i)
{
    // x = 0 = 00
    // y = 1 = 01
    // z = 2 = 10
    // w = 3 = 11
    uint mask0 = uint(int(i << 31) >> 31);
    uint mask1 = uint(int(i << 30) >> 31);
    return
        (((v.w & mask0) | (v.z & ~mask0)) & mask1) |
        (((v.y & mask0) | (v.x & ~mask0)) & ~mask1);
}

struct ClusteredLightLoop
{
    uint baseIndex;
    uint tileMask;
    uint wordIndex;
    uint bitIndex;
    uint zBinMinMask;
    uint zBinMaxMask;
#if LIGHTS_PER_TILE > 32
    uint wordMin;
    uint wordMax;
#endif
};

ClusteredLightLoop ClusteredLightLoopInit(float2 normalizedScreenSpaceUV, float3 positionWS)
{
    ClusteredLightLoop state = (ClusteredLightLoop)0;
    uint2 tileId = uint2(normalizedScreenSpaceUV * _AdditionalLightsTileScale);
    state.baseIndex = (tileId.y * _AdditionalLightsTileCountX + tileId.x) * (LIGHTS_PER_TILE / 32);
    float viewZ = dot(GetViewForwardDir(), positionWS - GetCameraPositionWS());
    uint zBinIndex = min(4*MAX_ZBIN_VEC4S - 1, (uint)(sqrt(viewZ) * _AdditionalLightsZBinScale) - _AdditionalLightsZBinOffset);
    uint zBinData = ClusteringSelect4(asuint(_AdditionalLightsZBins[zBinIndex / 4]), zBinIndex % 4);
    uint2 zBin = min(uint2(zBinData & 0xFFFF, (zBinData >> 16) & 0xFFFF), MAX_VISIBLE_LIGHTS - 1);
    uint2 zBinWords = zBin / 32;
    state.zBinMinMask = 0xFFFFFFFF << (zBin.x & 0x1F);
    state.zBinMaxMask = 0xFFFFFFFF >> (31 - (zBin.y & 0x1F));
#if LIGHTS_PER_TILE > 32
    state.wordMin = zBinWords.x;
    state.wordMax = zBinWords.y;
    state.wordIndex = zBinWords.x;
#endif
#if SHADER_TARGET < 45
    state.bitIndex = zBin.x & 0x1F;
#endif
    return state;
}

bool ClusteredLightLoopNextWord(inout ClusteredLightLoop state)
{
#if LIGHTS_PER_TILE > 32
    uint wordMin = state.wordMin;
    uint wordMax = state.wordMax;
#else
    uint wordMin = 0;
    uint wordMax = 0;
#endif
    if (state.wordIndex > wordMax) return false;
    uint index = state.baseIndex + state.wordIndex;
    state.tileMask = ClusteringSelect4(asuint(_AdditionalLightsTiles[index / 4]), index % 4);
    if (state.wordIndex == wordMin) state.tileMask &= state.zBinMinMask;
    if (state.wordIndex == wordMax) state.tileMask &= state.zBinMaxMask;
    state.wordIndex++;
#if SHADER_TARGET < 45
    state.bitIndex = 0;
#endif
    return true;
}

bool ClusteredLightLoopNextLight(inout ClusteredLightLoop state)
{
    if (state.tileMask == 0) return false;
#if SHADER_TARGET < 45
    while ((state.tileMask & (1 << state.bitIndex)) == 0) state.bitIndex++;
#else
    state.bitIndex = firstbitlow(state.tileMask);
#endif
    state.tileMask ^= (1 << state.bitIndex);
    return true;
}

uint ClusteredLightLoopGetLightIndex(ClusteredLightLoop state)
{
    return _AdditionalLightsDirectionalCount + (state.wordIndex - 1) * 32 + state.bitIndex;
}

#endif

#endif
