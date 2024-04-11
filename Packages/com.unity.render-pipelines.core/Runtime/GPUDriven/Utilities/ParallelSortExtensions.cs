using System.Threading;
using UnityEngine.Assertions;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering
{
    internal static class ParallelSortExtensions
    {
        const int kMinRadixSortArraySize = 2048;
        const int kMinRadixSortBatchSize = 256;

        internal static JobHandle ParallelSort(this NativeArray<int> array)
        {
            if (array.Length <= 1)
                return new JobHandle();

            var jobHandle = new JobHandle();

            if (array.Length >= kMinRadixSortArraySize)
            {
                int workersCount = Mathf.Max(JobsUtility.JobWorkerCount + 1, 1);
                int batchSize = Mathf.Max(kMinRadixSortBatchSize, Mathf.CeilToInt((float)array.Length / workersCount));
                int jobsCount = Mathf.CeilToInt((float)array.Length / batchSize);

                Assert.IsTrue(jobsCount * batchSize >= array.Length);

                var supportArray = new NativeArray<int>(array.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var counter = new NativeArray<int>(1, Allocator.TempJob);
                var buckets = new NativeArray<int>(jobsCount * 256, Allocator.TempJob);
                var indices = new NativeArray<int>(jobsCount * 256, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var indicesSum = new NativeArray<int>(16, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                var arraySource = array;
                var arrayDest = supportArray;

                for (int radix = 0; radix < 4; ++radix)
                {
                    var bucketCountJobData = new RadixSortBucketCountJob
                    {
                        radix = radix,
                        jobsCount = jobsCount,
                        batchSize = batchSize,
                        buckets = buckets,
                        array = arraySource
                    };

                    var batchPrefixSumJobData = new RadixSortBatchPrefixSumJob
                    {
                        radix = radix,
                        jobsCount = jobsCount,
                        array = arraySource,
                        counter = counter,
                        buckets = buckets,
                        indices = indices,
                        indicesSum = indicesSum
                    };

                    var prefixSumJobData = new RadixSortPrefixSumJob
                    {
                        jobsCount = jobsCount,
                        indices = indices,
                        indicesSum = indicesSum
                    };

                    var bucketSortJobData = new RadixSortBucketSortJob
                    {
                        radix = radix,
                        batchSize = batchSize,
                        indices = indices,
                        array = arraySource,
                        arraySorted = arrayDest
                    };

                    jobHandle = bucketCountJobData.ScheduleParallel(jobsCount, 1, jobHandle);
                    jobHandle = batchPrefixSumJobData.ScheduleParallel(16, 1, jobHandle);
                    jobHandle = prefixSumJobData.ScheduleParallel(16, 1, jobHandle);
                    jobHandle = bucketSortJobData.ScheduleParallel(jobsCount, 1, jobHandle);

                    JobHandle.ScheduleBatchedJobs();

                    static void Swap(ref NativeArray<int> a, ref NativeArray<int> b)
                    {
                        NativeArray<int> temp = a;
                        a = b;
                        b = temp;
                    }

                    Swap(ref arraySource, ref arrayDest);
                }

                supportArray.Dispose();
                counter.Dispose();
                buckets.Dispose();
                indices.Dispose();
                indicesSum.Dispose();
            }
            else
            {
                jobHandle = NativeSortExtension.SortJob(array).Schedule();
            }

            return jobHandle;
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        internal struct RadixSortBucketCountJob : IJobFor
        {
            [ReadOnly] public int radix;
            [ReadOnly] public int jobsCount;
            [ReadOnly] public int batchSize;
            [ReadOnly] [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<int> array;

            [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<int> buckets;

            public void Execute(int index)
            {
                int start = index * batchSize;
                int end = math.min(start + batchSize, array.Length);

                int jobBuckets = index * 256;

                for (int i = start; i < end; ++i)
                {
                    int value = array[i];
                    int bucket = (value >> radix * 8) & 0xFF;
                    buckets[jobBuckets + bucket] += 1;
                }
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        internal struct RadixSortBatchPrefixSumJob : IJobFor
        {
            [ReadOnly] public int radix;
            [ReadOnly] public int jobsCount;
            [ReadOnly] [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<int> array;

            [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<int> counter;
            [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<int> indicesSum;
            [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<int> buckets;
            [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<int> indices;

            private unsafe static int AtomicIncrement(NativeArray<int> counter)
            {
                return Interlocked.Increment(ref UnsafeUtility.AsRef<int>((int*)counter.GetUnsafePtr()));
            }

            private int JobIndexPrefixSum(int sum, int i)
            {
                for (int j = 0; j < jobsCount; ++j)
                {
                    int k = i + j * 256;

                    indices[k] = sum;
                    sum += buckets[k];
                    buckets[k] = 0;
                }

                return sum;
            }

            public void Execute(int index)
            {
                int start = index * 16;
                int end = start + 16;

                int jobSum = 0;

                for (int i = start; i < end; ++i)
                    jobSum = JobIndexPrefixSum(jobSum, i);

                indicesSum[index] = jobSum;

                if (AtomicIncrement(counter) == 16)
                {
                    int sum = 0;

                    if(radix < 3)
                    {
                        for (int i = 0; i < 16; ++i)
                        {
                            int indexSum = indicesSum[i];
                            indicesSum[i] = sum;
                            sum += indexSum;
                        }
                    }
                    else // Negative
                    {
                        for (int i = 8; i < 16; ++i)
                        {
                            int indexSum = indicesSum[i];
                            indicesSum[i] = sum;
                            sum += indexSum;
                        }
                        for (int i = 0; i < 8; ++i)
                        {
                            int indexSum = indicesSum[i];
                            indicesSum[i] = sum;
                            sum += indexSum;
                        }
                    }

                    Assert.AreEqual(sum, array.Length);

                    counter[0] = 0;
                }
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        internal struct RadixSortPrefixSumJob : IJobFor
        {
            [ReadOnly] public int jobsCount;

            [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<int> indicesSum;
            [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<int> indices;

            public void Execute(int index)
            {
                int start = index * 16;
                int end = start + 16;

                int jobSum = indicesSum[index];

                for (int j = 0; j < jobsCount; ++j)
                {
                    for (int i = start; i < end; ++i)
                    {
                        int k = j * 256 + i;
                        indices[k] += jobSum;
                    }
                }
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        internal struct RadixSortBucketSortJob : IJobFor
        {
            [ReadOnly] public int radix;
            [ReadOnly] public int batchSize;
            [ReadOnly] [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<int> array;

            [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<int> indices;
            [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<int> arraySorted;

            public void Execute(int index)
            {
                int start = index * batchSize;
                int end = math.min(start + batchSize, array.Length);

                int jobIndices = index * 256;

                for (int i = start; i < end; ++i)
                {
                    int value = array[i];
                    int bucket = (value >> radix * 8) & 0xFF;
                    int sortedIndex = indices[jobIndices + bucket]++;
                    arraySorted[sortedIndex] = value;
                }
            }
        }
    }
}
