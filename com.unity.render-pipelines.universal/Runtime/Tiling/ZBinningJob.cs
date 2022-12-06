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

        public const int headerLength = 2;

        [NativeDisableParallelForRestriction]
        public NativeArray<uint> bins;

        [ReadOnly]
        public NativeArray<float2> minMaxZs;

        public float zBinScale;

        public float zBinOffset;

        public int binCount;

        public int wordsPerTile;

        public int lightCount;

        public int reflectionProbeCount;

        static uint EncodeHeader(uint min, uint max)
        {
            return (min & 0xFFFF) | ((max & 0xFFFF) << 16);
        }

        static (uint, uint) DecodeHeader(uint zBin)
        {
            return (zBin & 0xFFFF, (zBin >> 16) & 0xFFFF);
        }

        public void Execute(int jobIndex)
        {
            var binsStart = batchCount * jobIndex;
            var binsEnd = math.min(binsStart + batchCount, binCount) - 1;

            var emptyHeader = EncodeHeader(ushort.MaxValue, ushort.MinValue);
            for (var binIndex = binsStart; binIndex <= binsEnd; binIndex++)
            {
                bins[binIndex * (headerLength + wordsPerTile) + 0] = emptyHeader;
                bins[binIndex * (headerLength + wordsPerTile) + 1] = emptyHeader;
            }

            FillZBins(binsStart, binsEnd, 0, lightCount, 0);
            FillZBins(binsStart, binsEnd, lightCount, lightCount + reflectionProbeCount, 1);
        }

        void FillZBins(int binsStart, int binsEnd, int itemsStart, int itemsEnd, int headerIndex)
        {
            for (var index = itemsStart; index < itemsEnd; index++)
            {
                var minMax = minMaxZs[index];
                var minBin = math.max((int)(math.log2(minMax.x) * zBinScale + zBinOffset), binsStart);
                var maxBin = math.min((int)(math.log2(minMax.y) * zBinScale + zBinOffset), binsEnd);

                var wordIndex = index / 32;
                var bitMask = 1u << (index % 32);

                for (var binIndex = minBin; binIndex <= maxBin; binIndex++)
                {
                    var baseIndex = binIndex * (headerLength + wordsPerTile);
                    var (minIndex, maxIndex) = DecodeHeader(bins[baseIndex + headerIndex]);
                    minIndex = math.min(minIndex, (uint)index);
                    maxIndex = math.max(maxIndex, (uint)index);
                    bins[baseIndex + headerIndex] = EncodeHeader(minIndex, maxIndex);
                    bins[baseIndex + headerLength + wordIndex] |= bitMask;
                }
            }
        }
    }
}
