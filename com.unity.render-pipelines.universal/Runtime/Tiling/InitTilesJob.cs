using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    [BurstCompile]
    unsafe struct InitTilesJob : IJobFor
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<uint> tilesHit;

        [NativeDisableParallelForRestriction]
        public NativeArray<uint> tilesActive;

        public int2 tileResolution;

        public int lightCount;

        public int lightsPerTile;

        public void Execute(int tileIndex)
        {
            var tileId = math.int2(tileIndex % tileResolution.x, tileIndex / tileResolution.x);
            var tileOffset = tileIndex * (lightsPerTile / 32);
            var wordCount = (lightCount + 31) / 32;
            for (var wordIndex = 0; wordIndex < wordCount; wordIndex++)
            {
                var lightsInWord = wordIndex == wordCount - 1 ? lightCount % 32 : 32;
                var lightRangeMask = 0xFFFFFFFFu >> -lightsInWord;
                var groupTilesIndex = tileOffset + wordIndex;
                tilesHit[groupTilesIndex] = lightRangeMask;
                tilesActive[groupTilesIndex] = lightRangeMask;
            }
        }
    }
}
