using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    [BurstCompile(FloatMode = FloatMode.Fast, DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    struct TileRangeExpansionJob : IJobFor
    {
        [ReadOnly]
        public NativeArray<InclusiveRange> tileRanges;

        [NativeDisableParallelForRestriction]
        public NativeArray<uint> tileMasks;

        public int rangesPerItem;
        public int itemsPerTile;
        public int wordsPerTile;
        public int2 tileResolution;

        public void Execute(int jobIndex)
        {
            var rowIndex = jobIndex % tileResolution.y;
            var viewIndex = jobIndex / tileResolution.y;

            var rowBaseMaskIndex = viewIndex * wordsPerTile * tileResolution.x * tileResolution.y + rowIndex * wordsPerTile * tileResolution.x;
            var rowRangeBase = viewIndex * rangesPerItem * itemsPerTile + 1 + rowIndex;

            var rowWordCount = tileResolution.x * wordsPerTile;
            for (var i = 0; i < rowWordCount; i++)
                tileMasks[rowBaseMaskIndex + i] = 0;

            // Toggle each item's bit at its first column and one past its last, then XOR left to right so the bit is set
            // exactly inside the range. With one word per tile, setting the bits directly is a contiguous loop and cheaper.
            // Clip to on-screen columns; the tiling job leaves one tile of off-screen slack.
            var sweep = wordsPerTile > 1;
            for (var itemIndex = 0; itemIndex < itemsPerTile; itemIndex++)
            {
                var range = tileRanges[rowRangeBase + itemIndex * rangesPerItem];
                var start = math.max((int)range.start, 0);
                var end = math.min((int)range.end, tileResolution.x - 1);
                if (start > end) continue;

                var wordIndex = itemIndex / 32;
                var itemMask = 1u << (itemIndex % 32);
                if (sweep)
                {
                    tileMasks[rowBaseMaskIndex + start * wordsPerTile + wordIndex] ^= itemMask;
                    if (end + 1 < tileResolution.x)
                        tileMasks[rowBaseMaskIndex + (end + 1) * wordsPerTile + wordIndex] ^= itemMask;
                }
                else
                {
                    for (var tileIndex = start; tileIndex <= end; tileIndex++)
                        tileMasks[rowBaseMaskIndex + tileIndex * wordsPerTile + wordIndex] |= itemMask;
                }
            }

            if (sweep)
            {
                // The running value stays in a register; XOR-ing through tileMasks would serialize on store-to-load latency.
                for (var wordIndex = 0; wordIndex < wordsPerTile; wordIndex++)
                {
                    var running = 0u;
                    for (var i = rowBaseMaskIndex + wordIndex; i < rowBaseMaskIndex + rowWordCount; i += wordsPerTile)
                    {
                        running ^= tileMasks[i];
                        tileMasks[i] = running;
                    }
                }
            }
        }
    }
}
