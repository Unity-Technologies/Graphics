using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    // This could be multi-threaded if profiling shows need
    [BurstCompile]
    unsafe struct ZSortJob : IJob
    {
        fixed int count[256];

        public NativeArray<LightMinMaxZ> minMaxZs;
        public NativeArray<float> meanZs;
        public NativeArray<int> indices;

        public ZSortJob(NativeArray<LightMinMaxZ> minMaxZs)
        {
            this.minMaxZs = minMaxZs;
            this.meanZs = new NativeArray<float>(minMaxZs.Length * 2, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            this.indices = new NativeArray<int>(minMaxZs.Length * 2, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        }

        public void Execute()
        {
            for (var i = 0; i < minMaxZs.Length; i++)
            {
                meanZs[i] = (minMaxZs[i].minZ + minMaxZs[i].maxZ) * 0.5f;
                indices[i] = i;
            }

            var n = minMaxZs.Length;

            for (var i = 0; i < 4; i++)
            {
                int currentOffset, nextOffset;

                if (i % 2 == 0)
                {
                    currentOffset = 0;
                    nextOffset = n;
                }
                else
                {
                    currentOffset = n;
                    nextOffset = 0;
                }

                for (var j = 0; j < n; j++)
                {
                    var key = math.asuint(meanZs[currentOffset + j]);
                    var countIndex = (key >> (8 * i)) & 0xFF;
                    count[countIndex]++;
                }

                for (var j = 1; j < 256; j++)
                {
                    count[j] += count[j - 1];
                }

                for (var j = n - 1; j >= 0; j--)
                {
                    var mean = meanZs[currentOffset + j];
                    var key = math.asuint(mean);
                    var countIndex = (key >> (8 * i)) & 0xFF;
                    var newIndex = count[countIndex] - 1;
                    count[countIndex]--;
                    meanZs[nextOffset + newIndex] = mean;
                    indices[nextOffset + newIndex] = indices[currentOffset + j];
                }
            }
        }
    }
}
