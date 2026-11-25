using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    struct ZBinMapping
    {
        public float scale;
        public float offset;
        public int binCount;
        public bool isOrthographic;

        // Widens ranges slightly so CPU/GPU log2 rounding at a bin edge cannot drop the edge bin.
        const float k_EdgeBias = 1e-3f;

        // Clamped to [-1, binCount] so depths outside the bins yield empty bin spans.
        public int ToBin(float z, float bias)
        {
            var bin = (isOrthographic ? z : math.log2(z)) * scale + offset + bias;
            return (int)math.clamp(bin, -1f, binCount);
        }

        // An empty or non-finite depth range, e.g. an item entirely closer than the near plane or a light with a broken transform,
        // gives an empty bin span without evaluating log2.
        public int2 ToBins(float2 minMaxZ) =>
            !math.all(math.isfinite(minMaxZ)) || minMaxZ.x > minMaxZ.y ? math.int2(0, -1) : math.int2(ToBin(minMaxZ.x, -k_EdgeBias), ToBin(minMaxZ.y, k_EdgeBias));
    }

    [BurstCompile(FloatMode = FloatMode.Fast, DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    struct ZBinningJob : IJobFor
    {
        // Do not use this for the innerloopBatchCount (use 1 for that). Use for dividing the arrayLength when scheduling.
        public const int batchSize = 128;

        public const int headerLength = 2;

        // A header whose min index is above its max index, which the shader iterates as empty.
        public const uint emptyHeader = ushort.MaxValue;

        [NativeDisableParallelForRestriction]
        public NativeArray<uint> bins;

        [ReadOnly]
        public NativeArray<int2> minMaxZBins;

        public int binCount;

        public int wordsPerTile;

        public int lightCount;

        public int reflectionProbeCount;

        public int batchCount;

        public int viewCount;

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
            var batchIndex = jobIndex % batchCount;
            var viewIndex = jobIndex / batchCount;

            var binStart = batchSize * batchIndex;
            var binEnd = math.min(binStart + batchSize, binCount) - 1;

            var binOffset = viewIndex * binCount;

            for (var binIndex = binStart; binIndex <= binEnd; binIndex++)
            {
                var baseIndex = (binOffset + binIndex) * (headerLength + wordsPerTile);
                bins[baseIndex + 0] = emptyHeader;
                bins[baseIndex + 1] = emptyHeader;
                for (var wordIndex = 0; wordIndex < wordsPerTile; wordIndex++)
                    bins[baseIndex + headerLength + wordIndex] = 0;
            }

            // Regarding itemOffset: minMaxZBins contains [lights view 0, lights view 1, probes view 0, probes view 1] when
            // using XR single pass instanced, and otherwise [lights, probes]. So we figure out what the offset is based
            // on the view count and index.

            // Fill ZBins for lights.
            FillZBins(binStart, binEnd, 0, lightCount, 0, viewIndex * lightCount, binOffset);

            // Fill ZBins for reflection probes.
            FillZBins(binStart, binEnd, lightCount, lightCount + reflectionProbeCount, 1, lightCount * (viewCount - 1) + viewIndex * reflectionProbeCount, binOffset);
        }

        void FillZBins(int binStart, int binEnd, int itemStart, int itemEnd, int headerIndex, int itemOffset, int binOffset)
        {
            for (var index = itemStart; index < itemEnd; index++)
            {
                var minMax = minMaxZBins[itemOffset + index];
                var minBin = math.max(minMax.x, binStart);
                var maxBin = math.min(minMax.y, binEnd);

                var wordIndex = index / 32;
                var bitMask = 1u << (index % 32);

                for (var binIndex = minBin; binIndex <= maxBin; binIndex++)
                {
                    var baseIndex = (binOffset + binIndex) * (headerLength + wordsPerTile);
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
