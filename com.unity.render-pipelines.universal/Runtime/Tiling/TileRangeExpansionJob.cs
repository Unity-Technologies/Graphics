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
        public NativeArray<uint> lightMasks;

        public int itemsPerLight;
        public int lightCount;
        public int wordsPerTile;
        public int2 tileResolution;

        public void Execute(int rowIndex)
        {
            var rowBaseMaskIndex = rowIndex * wordsPerTile * tileResolution.x;
            for (var lightIndex = 0; lightIndex < lightCount; lightIndex++)
            {
                var wordIndex = lightIndex / 32;
                var lightMask = 1u << (lightIndex % 32);
                var range = tileRanges[lightIndex * itemsPerLight + 1 + rowIndex];

                // if (!range.isEmpty) Debug.Log($"light{lightIndex}@row{rowIndex}: {range.start}..={range.end}");
                for (var tileIndex = range.start; tileIndex <= range.end; tileIndex++)
                {
                    // if (rowBaseMaskIndex + tileIndex * wordsPerTile + wordIndex >= lightMasks.Length) Debug.Log(tileIndex);
                    lightMasks[rowBaseMaskIndex + tileIndex * wordsPerTile + wordIndex] |= lightMask;
                }
            }
        }
    }
}
