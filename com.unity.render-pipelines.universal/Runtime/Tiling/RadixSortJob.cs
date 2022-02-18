using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.Rendering.Universal
{
    // This could be multi-threaded if profiling shows need
    [BurstCompile]
    unsafe struct RadixSortJob : IJob
    {
        public NativeArray<uint> keys;
        public NativeArray<int> indices;

        public void Execute()
        {
            var counts = new NativeArray<int>(256, Allocator.Temp);
            var n = indices.Length / 2;

            for (var i = 0; i < n; i++)
            {
                indices[i] = i;
            }

            for (var i = 0; i < 4; i++)
            {
                int currentOffset, nextOffset;

                for (var j = 0; j < 256; j++)
                {
                    counts[j] = 0;
                }

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
                    var key = keys[currentOffset + j];
                    var bucket = (key >> (8 * i)) & 0xFF;
                    counts[(int)bucket]++;
                }

                for (var j = 1; j < 256; j++)
                {
                    counts[j] += counts[j - 1];
                }

                for (var j = n - 1; j >= 0; j--)
                {
                    var key = keys[currentOffset + j];
                    var bucket = (key >> (8 * i)) & 0xFF;
                    var newIndex = counts[(int)bucket] - 1;
                    counts[(int)bucket]--;
                    keys[nextOffset + newIndex] = key;
                    indices[nextOffset + newIndex] = indices[currentOffset + j];
                }
            }

            counts.Dispose();
        }
    }
}
