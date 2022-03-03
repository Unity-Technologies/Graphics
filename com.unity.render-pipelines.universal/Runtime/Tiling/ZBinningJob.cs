using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    [StructLayout(LayoutKind.Sequential)]
    struct ZBin
    {
        public ushort minIndex;
        public ushort maxIndex;

        public static explicit operator uint(ZBin bin)
        {
            return UnsafeUtility.As<ZBin, uint>(ref bin);
        }

        public static explicit operator ZBin(uint bin)
        {
            return UnsafeUtility.As<uint, ZBin>(ref bin);
        }
    }

    [BurstCompile(FloatMode = FloatMode.Fast, DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    unsafe struct ZBinningJob : IJobFor
    {
        // Do not use this for the innerloopBatchCount (use 1 for that). Use for dividing the arrayLength when scheduling.
        public const int batchCount = 128;

        [NativeDisableParallelForRestriction]
        public NativeArray<uint> bins;

        [ReadOnly]
        public NativeArray<LightMinMaxZ> minMaxZs;

        public float zBinMul;

        public float zBinAdd;

        public int binCount;

        public int wordsPerTile;

        public void Execute(int index)
        {
            var binsStart = batchCount * index;
            var binsEnd = math.min(binsStart + batchCount, binCount) - 1;

            for (var binIndex = binsStart; binIndex <= binsEnd; binIndex++)
            {
                var bin = new ZBin { minIndex = ushort.MaxValue, maxIndex = ushort.MaxValue };
                bins[binIndex * (1 + wordsPerTile)] = (uint)bin;
            }
            for (var lightIndex = 0; lightIndex < minMaxZs.Length; lightIndex++)
            {
                var ushortLightIndex = (ushort)lightIndex;
                var minMax = minMaxZs[lightIndex];
                var minBin = math.max((int)(math.log2(minMax.minZ) * zBinMul + zBinAdd), binsStart);
                var maxBin = math.min((int)(math.log2(minMax.maxZ) * zBinMul + zBinAdd), binsEnd);

                var wordIndex = lightIndex / 32;
                var bitMask = 1u << (lightIndex % 32);

                for (var binIndex = minBin; binIndex <= maxBin; binIndex++)
                {
                    var baseIndex = binIndex * (1 + wordsPerTile);
                    var bin = (ZBin)bins[baseIndex];
                    bin.minIndex = Math.Min(bin.minIndex, ushortLightIndex);
                    // This will always be the largest light index this bin has seen due to light iteration order.
                    bin.maxIndex = ushortLightIndex;
                    bins[baseIndex] = (uint)bin;
                    bins[baseIndex + 1 + wordIndex] |= bitMask;
                }
            }
        }
    }
}
