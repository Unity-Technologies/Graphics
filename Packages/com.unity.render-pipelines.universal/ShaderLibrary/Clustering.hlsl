#ifndef UNIVERSAL_CLUSTERING_INCLUDED
#define UNIVERSAL_CLUSTERING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

#if USE_FORWARD_PLUS
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"


// Debug switches for disabling parts of the algorithm. Not implemented for mobile.
#define URP_FP_DISABLE_ZBINNING 0
#define URP_FP_DISABLE_TILING 0

// internal
struct ClusterIterator
{
    uint tileOffset;
    uint zBinOffset;
    uint tileMask;
    // Stores the next light index in first 16 bits, and the max light index in the last 16 bits.
    uint entityIndexNextMax;
};

// internal
ClusterIterator ClusterInit(float2 normalizedScreenSpaceUV, float3 positionWS, int headerIndex)
{
    ClusterIterator state = (ClusterIterator)0;

#if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    {
#if UNITY_UV_STARTS_AT_TOP
        // RemapFoveatedRenderingNonUniformToLinear expects the UV coordinate to be non-flipped, so we un-flip it before
        // the call, and then flip it back afterwards.
        normalizedScreenSpaceUV.y = 1.0 - normalizedScreenSpaceUV.y;
#endif
        normalizedScreenSpaceUV = RemapFoveatedRenderingNonUniformToLinear(normalizedScreenSpaceUV);
#if UNITY_UV_STARTS_AT_TOP
        normalizedScreenSpaceUV.y = 1.0 - normalizedScreenSpaceUV.y;
#endif
    }
#endif // SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER

    uint2 tileId = uint2(normalizedScreenSpaceUV * URP_FP_TILE_SCALE);
    state.tileOffset = tileId.y * URP_FP_TILE_COUNT_X + tileId.x;
#if defined(USING_STEREO_MATRICES)
    state.tileOffset += URP_FP_TILE_COUNT * unity_StereoEyeIndex;
#endif
    state.tileOffset *= URP_FP_WORDS_PER_TILE;

    float viewZ = dot(GetViewForwardDir(), positionWS - GetCameraPositionWS());
    uint zBinBaseIndex = (uint)((IsPerspectiveProjection() ? log2(viewZ) : viewZ) * URP_FP_ZBIN_SCALE + URP_FP_ZBIN_OFFSET);
#if defined(USING_STEREO_MATRICES)
    zBinBaseIndex += URP_FP_ZBIN_COUNT * unity_StereoEyeIndex;
#endif
    zBinBaseIndex = min(4*MAX_ZBIN_VEC4S - 1, zBinBaseIndex) * (2 + URP_FP_WORDS_PER_TILE);

    uint zBinHeaderIndex = zBinBaseIndex + headerIndex;
    state.zBinOffset = zBinBaseIndex + 2;

#if !URP_FP_DISABLE_ZBINNING
    uint header = Select4(asuint(URP_ZBins[zBinHeaderIndex / 4]), zBinHeaderIndex % 4);
#else
    uint header = headerIndex == 0 ? ((URP_FP_PROBES_BEGIN - 1) << 16) : (((URP_FP_WORDS_PER_TILE * 32 - 1) << 16) | URP_FP_PROBES_BEGIN);
#endif
#if MAX_LIGHTS_PER_TILE > 32 || !defined(_ENVIRONMENTREFLECTIONS_OFF)
    state.entityIndexNextMax = header;
#else
    uint tileIndex = state.tileOffset;
    uint zBinIndex = state.zBinOffset;
    if (URP_FP_WORDS_PER_TILE > 0)
    {
        state.tileMask =
            Select4(asuint(urp_Tiles[tileIndex / 4]), tileIndex % 4) &
            Select4(asuint(URP_ZBins[zBinIndex / 4]), zBinIndex % 4) &
            (0xFFFFFFFFu << (header & 0x1F)) & (0xFFFFFFFFu >> (31 - (header >> 16)));
    }
#endif

    return state;
}

// internal
bool ClusterNext(inout ClusterIterator it, out uint entityIndex)
{
#if MAX_LIGHTS_PER_TILE > 32 || !defined(_ENVIRONMENTREFLECTIONS_OFF)
    uint maxIndex = it.entityIndexNextMax >> 16;
    while (it.tileMask == 0 && (it.entityIndexNextMax & 0xFFFF) <= maxIndex)
    {
        // Extract the lower 16 bits and shift by 5 to divide by 32.
        uint wordIndex = ((it.entityIndexNextMax & 0xFFFF) >> 5);
        uint tileIndex = it.tileOffset + wordIndex;
        uint zBinIndex = it.zBinOffset + wordIndex;
        it.tileMask =
#if !URP_FP_DISABLE_TILING
            Select4(asuint(urp_Tiles[tileIndex / 4]), tileIndex % 4) &
#endif
#if !URP_FP_DISABLE_ZBINNING
            Select4(asuint(URP_ZBins[zBinIndex / 4]), zBinIndex % 4) &
#endif
            // Mask out the beginning and end of the word.
            (0xFFFFFFFFu << (it.entityIndexNextMax & 0x1F)) & (0xFFFFFFFFu >> (31 - min(31, maxIndex - wordIndex * 32)));
        // The light index can start at a non-multiple of 32, but the following iterations should always be multiples of 32.
        // So we add 32 and mask out the lower bits.
        it.entityIndexNextMax = (it.entityIndexNextMax + 32) & ~31;
    }
#endif
    bool hasNext = it.tileMask != 0;
    uint bitIndex = FIRST_BIT_LOW(it.tileMask);
    it.tileMask ^= (1 << bitIndex);
#if MAX_LIGHTS_PER_TILE > 32 || !defined(_ENVIRONMENTREFLECTIONS_OFF)
    // Subtract 32 because it stores the index of the _next_ word to fetch, but we want the current.
    // The upper 16 bits and bits representing values < 32 are masked out. The latter is due to the fact that it will be
    // included in what FIRST_BIT_LOW returns.
    entityIndex = (((it.entityIndexNextMax - 32) & (0xFFFF & ~31))) + bitIndex;
#else
    entityIndex = bitIndex;
#endif
    return hasNext;
}

#endif

#endif
