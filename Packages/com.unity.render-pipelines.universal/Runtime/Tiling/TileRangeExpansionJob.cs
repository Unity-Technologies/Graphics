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

        public int itemsPerLight;
        public int lightCount;
        public int wordsPerTile;
        public int2 tileResolution;

        public void Execute(int rowIndex)
        {
            var compactCount = 0;
            var lightIndices = new NativeArray<short>(lightCount, Allocator.Temp);
            var lightRanges = new NativeArray<InclusiveRange>(lightCount, Allocator.Temp);

            // Compact the light ranges for the current row.
            for (var lightIndex = 0; lightIndex < lightCount; lightIndex++)
            {
                var range = tileRanges[lightIndex * itemsPerLight + 1 + rowIndex];
                if (!range.isEmpty)
                {
                    lightIndices[compactCount] = (short)lightIndex;
                    lightRanges[compactCount] = range;
                    compactCount++;
                }
            }

            var rowBaseMaskIndex = rowIndex * wordsPerTile * tileResolution.x;
            for (var tileIndex = 0; tileIndex < tileResolution.x; tileIndex++)
            {
                var tileBaseIndex = rowBaseMaskIndex + tileIndex * wordsPerTile;
                for (var i = 0; i < compactCount; i++)
                {
                    var lightIndex = (int)lightIndices[i];
                    var wordIndex = lightIndex / 32;
                    var lightMask = 1u << (lightIndex % 32);
                    var range = lightRanges[i];
                    if (range.Contains((short)tileIndex))
                    {
                        tileMasks[tileBaseIndex + wordIndex] |= lightMask;
                    }
                }
            }

            lightIndices.Dispose();
            lightRanges.Dispose();
        }
    }
}
