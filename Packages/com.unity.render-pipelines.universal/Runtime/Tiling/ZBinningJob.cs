using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    [BurstCompile(FloatMode = FloatMode.Fast, DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    struct ZBinningJob : IJobFor
    {
        // Do not use this for the innerloopBatchCount (use 1 for that). Use for dividing the arrayLength when scheduling.
        public const int batchCount = 128;

        [NativeDisableParallelForRestriction]
        public NativeArray<uint> bins;

        [ReadOnly]
        public NativeArray<LightMinMaxZ> minMaxZs;

        public float zBinScale;

        public float zBinOffset;

        public int binCount;

        public int wordsPerTile;

        static uint EncodeHeader(uint min, uint max)
        {
            return (min & 0xFFFF) | ((max & 0xFFFF) << 16);
        }

        static (uint, uint) DecodeHeader(uint zBin)
        {
            return (zBin & 0xFFFF, (zBin >> 16) & 0xFFFF);
        }

        public void Execute(int index)
        {
            var binsStart = batchCount * index;
            var binsEnd = math.min(binsStart + batchCount, binCount) - 1;

            for (var binIndex = binsStart; binIndex <= binsEnd; binIndex++)
            {
                bins[binIndex * (1 + wordsPerTile)] = EncodeHeader(ushort.MaxValue, ushort.MinValue);
            }
            for (var lightIndex = 0; lightIndex < minMaxZs.Length; lightIndex++)
            {
                var minMax = minMaxZs[lightIndex];
                var minBin = math.max((int)(math.log2(minMax.minZ) * zBinScale + zBinOffset), binsStart);
                var maxBin = math.min((int)(math.log2(minMax.maxZ) * zBinScale + zBinOffset), binsEnd);

                var wordIndex = lightIndex / 32;
                var bitMask = 1u << (lightIndex % 32);

                for (var binIndex = minBin; binIndex <= maxBin; binIndex++)
                {
                    var baseIndex = binIndex * (1 + wordsPerTile);
                    var (minIndex, maxIndex) = DecodeHeader(bins[baseIndex]);
                    minIndex = math.min(minIndex, (uint)wordIndex);
                    // This will always be the largest light index this bin has seen due to light iteration order.
                    maxIndex = math.max(maxIndex, (uint)wordIndex);
                    bins[baseIndex] = EncodeHeader(minIndex, maxIndex);
                    bins[baseIndex + 1 + wordIndex] |= bitMask;
                }
            }
        }
    }
}
