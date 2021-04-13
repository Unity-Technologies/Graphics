using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    [StructLayout(LayoutKind.Sequential)]
    struct ZBin
    {
        public ushort minIndex;
        public ushort maxIndex;
    }

    [BurstCompile]
    unsafe struct ZBinningJob : IJobFor
    {
        // Do not use this for the innerloopBatchCount (use 1 for that). Use for dividing the arrayLength when scheduling.
        public const int batchCount = 64;

        [NativeDisableParallelForRestriction]
        public NativeArray<ZBin> bins;

        [ReadOnly]
        public NativeArray<LightMinMaxZ> minMaxZs;

        public int binOffset;

        public float zFactor;

        public void Execute(int index)
        {
            var binsStart = batchCount * index;
            var binsEnd = math.min(binsStart + batchCount, bins.Length) - 1;

            for (var i = binsStart; i <= binsEnd; i++)
            {
                bins[i] = new ZBin { minIndex = ushort.MaxValue, maxIndex = ushort.MaxValue };
            }

            for (var lightIndex = 0; lightIndex < minMaxZs.Length; lightIndex++)
            {
                var ushortLightIndex = (ushort)lightIndex;
                var minMax = minMaxZs[lightIndex];
                var minBin = math.max((int)(math.sqrt(minMax.minZ) * zFactor) - binOffset, binsStart);
                var maxBin = math.min((int)(math.sqrt(minMax.maxZ) * zFactor) - binOffset, binsEnd);

                for (var binIndex = minBin; binIndex <= maxBin; binIndex++)
                {
                    var bin = bins[binIndex];
                    bin.minIndex = Math.Min(bin.minIndex, ushortLightIndex);
                    // This will always be the largest light index this bin has seen due to light iteration order.
                    bin.maxIndex = ushortLightIndex;
                    bins[binIndex] = bin;
                }
            }
        }
    }
}
