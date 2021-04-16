using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    [BurstCompile]
    unsafe struct ActiveTilesJob : IJobFor
    {
        [ReadOnly]
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
                var hit = tilesHit[groupTilesIndex];
                var active = 0u;
                if (math.any(tileId <= 1) || math.any(tileId >= (tileResolution - 1)))
                {
                    active = hit;
                }
                else
                {
                    for (var x = tileId.x - 1; x <= tileId.x + 1; x++)
                    {
                        for (var y = tileId.y - 1; y <= tileId.y + 1; y++)
                        {
                            var neighborGroupIndex = (y * tileResolution.x + x);
                            var neighborGroupOffset = neighborGroupIndex * (lightsPerTile / 32);
                            active |= (hit ^ tilesHit[neighborGroupOffset + wordIndex]);
                        }
                    }
                }
                tilesActive[groupTilesIndex] = active & hit;
            }
        }
    }
}
