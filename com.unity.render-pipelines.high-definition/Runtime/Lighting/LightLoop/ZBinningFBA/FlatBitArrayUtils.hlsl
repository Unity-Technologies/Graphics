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

uint2 UnpackZBinData(uint zbin, uint category)
{
    const uint zBinBufferIndex = ComputeZBinBufferIndex(zBinRange, category, unity_StereoEyeIndex);
    const uint zBinRangeData = _zBinBuffer[zBinBufferIndex]; // {last << 16 | first}
    return uint2(zBinRangeData0 & UINT16_MAX, zBinRangeData1 >> 16);
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

uint GetNextEntityIndex(uint wordIndex, inout uint mask)
{
    uint currBitIndex = firstbitlow(mask);
    mask = mask ^ (1 << currBitIndex); // Mark the bit in the mask as processed.
    return WORD_SIZE * wordIndex + currBitIndex;
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
    wordRange.x = max(categoryWordRange.x, zBinMax.x);
    wordRange.y = min(categoryWordRange.y, zBinMax.y);

    return wordRange;
}

uint LoadWordMask(uint tileIndex, uint wordIndex, uint2 zBinRange)
{
    uint zBinMin = zBinRange.x;
    uint zBinMax = zBinRange.y;

    uint maskWidth = ClampToWordSize((int)zBinMax - (int)(zBinMin + 1));
    // Min index within this word.
    uint wordMin = wordIndex * 32;
    uint localMin = clamp((int)zBinMin - (int)wordMin, 0, 31);

    uint localZBinMask = (maskWidth == 32) ? 0xffffffff : GetBitFieldMask(maskWidth, localMin);
    uint outMask = _TileEntityMasks[tileIndex + wordIndex];
    // Combine the zbin mask and the tile mask
    outMask = outMask & localZBinMask;

#if PLATFORM_SUPPORTS_WAVE_INTRINSICS
    // Scalarize the bit mask
    outMask = WaveReadLaneFirst(WaveActiveBitOr(outMask));
#endif

    return outMask;
}

#else

uint LoadWordMask(uint tileIndex, uint wordIndex)
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
