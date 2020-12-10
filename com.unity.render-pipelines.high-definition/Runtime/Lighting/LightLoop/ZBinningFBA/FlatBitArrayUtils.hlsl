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
//      uint wordMask = LoadWordMask(tileIndex, word);
//      while (wordMask != 0)
//      {
//          uint entityIndex = GetNextEntityIndex(word, wordMask);
//          Entity e;
//          if(LoadEntity(entityIndex, e))           // This function needs to check validity of the index.
//              ProcessEntity(e);
//      }
//  }

#define USE_Z_BINNING 1

uint2 GetWordRangeForCategory(uint category)
{
    // TMP: For now just a single list for punctual
    // eventually this will need to either have an offset inside the list per-category.
    // The offset will likely be just the max count.

    const uint offsetInMasks = 0;
    const uint maxEntityCount = 0;
    if (category == BOUNDEDENTITYCATEGORY_PUNCTUAL_LIGHT)
    {
        offsetInMasks = 0;
        // TODO: Some clamping needed here if _PunctualLightCount is more than what we allow through the system.
        maxEntityCount = _PunctualLightCount;
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

uint2 GetWordRangeForCategory(uint category, uint2 zBinRange)
{
    uint2 categoryWordRange = GetWordRangeForCategory(category);

    uint zBinMin = zBinRange.x;
    uint zBinMax = zBinRange.y;

    // Scalarize the zBin range.
#if PLATFORM_SUPPORTS_WAVE_INTRINSICS
    zBinMin = WaveReadLaneFirst(WaveActiveMin(zBinMin));
    zBinMax = WaveReadLaneFirst(WaveActiveMax(zBinMax));
#endif

    uint2 wordRange;
    // Important! This works because indices are sorted.
    wordRange.x = max(categoryWordRange.x, zBinMax.x / WORD_SIZE);
    wordRange.y = min(categoryWordRange.y, zBinMax.y / WORD_SIZE);

    return wordRange;
}

uint LoadWordMask(uint category, uint tileIndex, uint wordIndex, uint2 zBinRange)
{
    // Note: zbin range must contain light index and not light index / WORD_SIZE
    uint zBinMin = zBinRange.x;
    uint zBinMax = zBinRange.y;

    // Create a local zbin mask with range of light index
    // for example if we have 2 to 7, we want to generate the mask 01111110 00000000 00000000 00000000
    // For 1 to 35 range
    // a) 01111111 11111111 11111111 11111111
    // b) 11100000 00000000 00000000 00000000
    // For 35 to 48
    // a) 00000000 00000000 00000000 00000000
    // b) 00111111 11111111 00000000 00000000



    uint maskWidth = ClampToWordSize((int)zBinMax - (int)(zBinMin + 1));
    // Min index within this word.
    uint wordMin = wordIndex * 32;
    uint localMin = clamp((int)zBinMin - (int)wordMin, 0, 31);

    // Question Francesco, I am not sure to understand the (maskWidth == 32) ? 0xffffffff as in my example of 1 to 35, it don't make sense...
    uint localZBinMask = (maskWidth == 32) ? 0xffffffff : GetBitFieldMask(maskWidth, localMin);

    // TODO: of course this is not the right function as we need to use the size of the _TileEntityMasks but it is for the example
    const uint tileBufferIndex = ComputeTileBufferHeaderIndex(tile, category, unity_StereoEyeIndex);
    uint outMask = _TileEntityMasks[tileBufferIndex + wordIndex];
    // Combine the zbin mask and the tile mask
    outMask = outMask & localZBinMask;

#if PLATFORM_SUPPORTS_WAVE_INTRINSICS
    // Scalarize the bit mask
    outMask = WaveReadLaneFirst(WaveActiveBitOr(outMask));
#endif

    return outMask;
}

#else

uint LoadWordMask(uint category, uint tileIndex, uint wordIndex)
{
    uint outMask = _TileEntityMasks[tileIndex + wordIndex];
#if PLATFORM_SUPPORTS_WAVE_INTRINSICS
    // Scalarize the bit mask
    outMask = WaveReadLaneFirst(WaveActiveBitOr(outMask));
#endif

    return outMask;
}

#endif


#endif
