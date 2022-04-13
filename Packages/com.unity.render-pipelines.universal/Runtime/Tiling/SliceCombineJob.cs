using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    [BurstCompile]
    unsafe struct SliceCombineJob : IJobFor
    {
        public int2 tileResolution;

        public int wordsPerTile;

        [ReadOnly]
        public NativeArray<uint> sliceLightMasksH;

        [ReadOnly]
        public NativeArray<uint> sliceLightMasksV;

        [NativeDisableParallelForRestriction]
        public NativeArray<uint> lightMasks;

        public void Execute(int idY)
        {
            var baseIndexH = idY * wordsPerTile;
            var baseIndexRow = baseIndexH * tileResolution.x;
            for (var idX = 0; idX < tileResolution.x; idX++)
            {
                var baseIndexV = idX * wordsPerTile;
                var baseIndexTile = baseIndexRow + baseIndexV;
                for (var wordIndex = 0; wordIndex < wordsPerTile; wordIndex++)
                {
                    lightMasks[baseIndexTile + wordIndex] = sliceLightMasksH[baseIndexH + wordIndex] & sliceLightMasksV[baseIndexV + wordIndex];
                }
            }
        }
    }
}
