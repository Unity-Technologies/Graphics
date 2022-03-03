#ifndef UNIVERSAL_CLUSTERING_INCLUDED
#define UNIVERSAL_CLUSTERING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

#if USE_CLUSTERED_LIGHTING

struct ClusteredLightLoop
{
    uint tileOffset;
    uint zBinOffset;
    uint tileMask;
    // Stores the word index in first 16 bits, and the max word index in the last 16 bits.
    uint wordIndexMax;
};

ClusteredLightLoop ClusteredLightLoopInit(float2 normalizedScreenSpaceUV, float3 positionWS)
{
    ClusteredLightLoop state = (ClusteredLightLoop)0;

    uint2 tileId = uint2(normalizedScreenSpaceUV * _AdditionalLightsTileScale);
    state.tileOffset = (tileId.y * _AdditionalLightsTileCountX + tileId.x) * _AdditionalLightsWordsPerTile;

    float viewZ = dot(GetViewForwardDir(), positionWS - GetCameraPositionWS());
    uint zBinHeaderIndex = min(4*MAX_ZBIN_VEC4S - 1, (uint)(log2(viewZ) * URP_ADDITIONAL_LIGHTS_ZBIN_MUL + URP_ADDITIONAL_LIGHTS_ZBIN_ADD)) * (1 + _AdditionalLightsWordsPerTile);
    state.zBinOffset = zBinHeaderIndex + 1;

#if MAX_LIGHTS_PER_TILE > 32
    uint zBinData = Select4(asuint(_AdditionalLightsZBins[zBinHeaderIndex >> 2]), zBinHeaderIndex & 0x3);
    uint2 zBin = min(uint2(zBinData & 0xFFFF, (zBinData >> 16) & 0xFFFF), (_AdditionalLightsWordsPerTile * 32) - 1);
    state.wordIndexMax = (zBin.x / 32) | ((zBin.y / 32) << 16);
#endif

    return state;
}

bool ClusteredLightLoopNext(inout ClusteredLightLoop state)
{
#if MAX_LIGHTS_PER_TILE > 32
    uint wordMax = state.wordIndexMax >> 16;
    while (state.tileMask == 0 && (state.wordIndexMax & 0xFFFF) <= wordMax)
    {
        uint tileIndex = state.tileOffset + (state.wordIndexMax & 0xFFFF);
        uint zBinIndex = state.zBinOffset + (state.wordIndexMax & 0xFFFF);
        state.tileMask = Select4(asuint(_AdditionalLightsTiles[tileIndex >> 2]), tileIndex & 0x3) &
            Select4(asuint(_AdditionalLightsZBins[zBinIndex >> 2]), zBinIndex & 0x3);
        state.wordIndexMax++;
    }
#endif
    return state.tileMask != 0;
}

uint ClusteredLightLoopGetLightIndex(inout ClusteredLightLoop state)
{
    uint bitIndex = FIRST_BIT_LOW(state.tileMask);
    state.tileMask ^= (1 << bitIndex);
    return _AdditionalLightsDirectionalCount + ((state.wordIndexMax & 0xFFFF) - 1) * 32 + bitIndex;
}

#endif

#endif
