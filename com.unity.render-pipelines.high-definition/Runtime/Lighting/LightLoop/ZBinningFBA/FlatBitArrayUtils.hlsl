#ifndef UNITY_FLATBITARRAY_INCLUDED
#define UNITY_FLATBITARRAY_INCLUDED

//
//  Data structures 
//
StructuredBuffer<uint> _TileEntityMasks;

//
// Defines
//
#define WORD_SIZE 32
#define MAX_WORDS 128   // Need to be configured from asset

uint ClampToWordSize(int val)
{
    return clamp(val, 0, WORD_SIZE);
}

// return ZBin index range
uint2 UnpackZBinData(uint zbin, uint category)
{
    const uint zBinBufferIndex = ComputeZBinBufferIndex(zbin, category, unity_StereoEyeIndex);
    const uint zBinRangeData = _zBinBuffer[zBinBufferIndex]; // {last << 16 | first}
    return uint2(zBinRangeData & UINT16_MAX, zBinRangeData >> 16);
}

//
// What a loop would look like (with Z-Binning, without ZBinning it is the same, but just don't pass zBinData to GetWordRangeForCategory)

//  uint2 zBinData = LoadZBinData(GetZBinAddress());
//  uint2 wordRange = GetWordRangeForCategory(CATEGORY, zBinData);
//  for (uint word = wordRange.x; word <= wordRange.y; ++word)
//  {
//      uint wordMask = LoadWordMask(word);
//      while (wordMask != 0)
//      {
//          uint entityIndex = GetNextEntityIndex(word, wordMask);
//          Entity e;
//          if(LoadEntity(entityIndex, e))           // This function needs to check validity of the index.
//              ProcessEntity(e);
//      }
//  }

#define USE_Z_BINNING 1

uint2 GetWordRangeForCategory(uint category, uint tile)
{
    // TMP: For now just a single list for punctual
    // eventually this will need to either have an offset inside the list per-category.
    // The offset will likely be just the max count.

    const uint tileOffset = unity_StereoEyeIndex * _PerViewOffsetInFlatEntityArray +  // Offset per view. 
                            tile * _WordCountPerTile;                                 // Offset per tile

    uint offsetInMasks = 0;
    uint maxEntityCount = 0;
    if (category == BOUNDEDENTITYCATEGORY_PUNCTUAL_LIGHT)
    {
        offsetInMasks = tileOffset + 0;             // TODO: The 0 here is because we only look at punctual lights for now for the test.
        maxEntityCount = _PunctualLightCount;       // TODO need to clamp to max entity. 
    }

    return uint2(offsetInMasks / WORD_SIZE, min(maxEntityCount / WORD_SIZE, MAX_WORDS));
}

uint GetNextEntityIndex(uint category, uint wordIndex, inout uint mask)
{
    uint currBitIndex = firstbitlow(mask);
    mask = mask ^ (1 << currBitIndex); // Mark the bit in the mask as processed.
    return WORD_SIZE * wordIndex + currBitIndex; // TODO use the category to shift the index by offset for this entity
}

#if USE_Z_BINNING

uint2 GetWordRangeForCategory(uint category, uint tile, uint2 zBinRange)
{
    uint2 categoryWordRange = GetWordRangeForCategory(category, tile);

    uint zBinMin = zBinRange.x;
    uint zBinMax = zBinRange.y;

    // Scalarize the zBin range.
#if PLATFORM_SUPPORTS_WAVE_INTRINSICS
    zBinMin = WaveReadLaneFirst(WaveActiveMin(zBinMin));
    zBinMax = WaveReadLaneFirst(WaveActiveMax(zBinMax));
#endif

    uint2 wordRange;
    // Important! This works because indices are sorted.
    wordRange.x = max(categoryWordRange.x, zBinMin / WORD_SIZE);
    wordRange.y = min(categoryWordRange.y, zBinMax / WORD_SIZE);

    return wordRange;
}

uint LoadWordMask(uint category, uint wordIndex, uint2 zBinRange)
{
    // Note: zbin range must contain light index and not light index / WORD_SIZE
    uint zBinMin = zBinRange.x;
    uint zBinMax = zBinRange.y;

    // Create a local zbin mask with range of light index
    // for example if we have 2 to 7, we want to generate the mask 00000000 00000000 00000000 11111100
    // For 1 to 35 range
    // a) 11111111 11111111 11111111 11111110
    // b) 00000000 00000000 00000000 00001111
    // For 35 to 48
    // a) 00000000 00000000 00000000 00000000
    // b) 00000000 00000000 11111111 11111000

    uint maskWidth = ClampToWordSize((int)zBinMax - (int)(zBinMin + 1));
    // Min index within this word.
    uint wordMin = wordIndex * 32;
    uint localMin = clamp((int)zBinMin - (int)wordMin, 0, 31);

    uint localZBinMask = (maskWidth == 32) ? 0xffffffff : GetBitFieldMask(maskWidth, localMin);

    uint outMask = _TileEntityMasks[wordIndex];
    // Combine the zbin mask and the tile mask
    outMask = outMask & localZBinMask;

#if PLATFORM_SUPPORTS_WAVE_INTRINSICS
    // Scalarize the bit mask
    outMask = WaveReadLaneFirst(WaveActiveBitOr(outMask));
#endif

    return outMask;
}

#else

uint LoadWordMask(uint category, uint wordIndex)
{
    uint outMask = _TileEntityMasks[wordIndex];
#if PLATFORM_SUPPORTS_WAVE_INTRINSICS
    // Scalarize the bit mask
    outMask = WaveReadLaneFirst(WaveActiveBitOr(outMask));
#endif

    return outMask;
}

#endif


#endif
