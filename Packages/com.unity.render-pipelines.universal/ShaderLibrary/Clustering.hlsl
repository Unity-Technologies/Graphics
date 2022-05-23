#ifndef UNIVERSAL_CLUSTERING_INCLUDED
#define UNIVERSAL_CLUSTERING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

#if USE_FORWARD_PLUS

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
    uint zBinHeaderIndex = min(4*MAX_ZBIN_VEC4S - 1, (uint)(log2(viewZ) * URP_ADDITIONAL_LIGHTS_ZBIN_SCALE + URP_ADDITIONAL_LIGHTS_ZBIN_OFFSET)) * (1 + _AdditionalLightsWordsPerTile);
    state.zBinOffset = zBinHeaderIndex + 1;

#if MAX_LIGHTS_PER_TILE > 32
    state.wordIndexMax = Select4(asuint(_AdditionalLightsZBins[zBinHeaderIndex / 4]), zBinHeaderIndex % 4);
#else
    uint tileIndex = state.tileOffset;
    uint zBinIndex = state.zBinOffset;
    if (_AdditionalLightsWordsPerTile > 0)
    {
        state.tileMask =
            Select4(asuint(_AdditionalLightsTiles[tileIndex / 4]), tileIndex % 4) &
            Select4(asuint(_AdditionalLightsZBins[zBinIndex / 4]), zBinIndex % 4);
    }
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
        state.tileMask =
            Select4(asuint(_AdditionalLightsTiles[tileIndex / 4]), tileIndex % 4) &
            Select4(asuint(_AdditionalLightsZBins[zBinIndex / 4]), zBinIndex % 4);
        state.wordIndexMax++;
    }
#endif
    return state.tileMask != 0;
}

uint ClusteredLightLoopGetLightIndex(inout ClusteredLightLoop state)
{
    uint bitIndex = FIRST_BIT_LOW(state.tileMask);
    state.tileMask ^= (1 << bitIndex);
#if MAX_LIGHTS_PER_TILE > 32
    return _AdditionalLightsDirectionalCount + ((state.wordIndexMax & 0xFFFF) - 1) * 32 + bitIndex;
#else
    return _AdditionalLightsDirectionalCount + bitIndex;
#endif
}

#endif

#endif
