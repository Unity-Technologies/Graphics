using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine.Assertions;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;

[assembly: RegisterGenericJobType(typeof(UnityEngine.Rendering.ParallelSortExtensions.RadixSortBucketCountJob<int>))]
[assembly: RegisterGenericJobType(typeof(UnityEngine.Rendering.ParallelSortExtensions.RadixSortBucketCountJob<ulong>))]

[assembly: RegisterGenericJobType(typeof(UnityEngine.Rendering.ParallelSortExtensions.RadixSortBatchPrefixSumJob<int>))]
[assembly: RegisterGenericJobType(typeof(UnityEngine.Rendering.ParallelSortExtensions.RadixSortBatchPrefixSumJob<ulong>))]

[assembly: RegisterGenericJobType(typeof(UnityEngine.Rendering.ParallelSortExtensions.RadixSortBucketSortJob<int>))]
[assembly: RegisterGenericJobType(typeof(UnityEngine.Rendering.ParallelSortExtensions.RadixSortBucketSortJob<ulong>))]

[assembly: RegisterGenericJobType(typeof(SortJob<int, NativeSortExtension.DefaultComparer<int>>))]
[assembly: RegisterGenericJobType(typeof(SortJob<ulong, NativeSortExtension.DefaultComparer<ulong>>))]

namespace UnityEngine.Rendering
{
    internal static class ParallelSortExtensions
    {
        // This constant is used in the ParallelSort unit test to make sure we're hitting the parallel code path.
        internal const int kMinRadixSortArraySize = 2048;
        const int kMinRadixSortBatchSize = 256;

        internal enum ParallelSortValueType
        {
            Int,
            ULong
        }

        private static int GetBucketIndex(int value, int radix)
        {
            return (value >> radix * 8) & 0xFF;
        }

        private static int GetBucketIndex(ulong value, int radix)
        {
            return (int)((value >> radix * 8) & 0xFF);
        }

        private static void Swap<T>(ref NativeArray<T> a, ref NativeArray<T> b) where T : unmanaged
        {
            NativeArray<T> temp = a;
            a = b;
            b = temp;
        }

        // The method supports for the moment only keys of type int or ulong.
        internal static JobHandle ParallelSort<T>(this NativeArray<T> array) where T : unmanaged, IComparable<T>
        {
            // Only these two integer types are supported
            Assert.IsTrue(typeof(T) == typeof(ulong) || typeof(T) == typeof(int));

            if (array.Length <= 1)
                return new JobHandle();

            var jobHandle = new JobHandle();

            if (array.Length >= kMinRadixSortArraySize)
            {
                int workersCount = Mathf.Max(JobsUtility.JobWorkerCount + 1, 1);
                int batchSize = Mathf.Max(kMinRadixSortBatchSize, Mathf.CeilToInt((float)array.Length / workersCount));
                int jobsCount = Mathf.CeilToInt((float)array.Length / batchSize);
                int keyByteSize = Marshal.SizeOf<T>();
                int signBitRadixIndex = keyByteSize - 1;

                Assert.IsTrue(jobsCount * batchSize >= array.Length);

                var supportArray = new NativeArray<T>(array.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var counter = new NativeArray<int>(1, Allocator.TempJob);
                var buckets = new NativeArray<int>(jobsCount * 256, Allocator.TempJob);
                var indices = new NativeArray<int>(jobsCount * 256, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var indicesSum = new NativeArray<int>(16, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                var arraySource = array;
                var arrayDest = supportArray;

                ParallelSortValueType valueType = typeof(T) == typeof(int) ? ParallelSortValueType.Int : ParallelSortValueType.ULong;

                // Add any unsigned value type to this condition
                if (valueType == ParallelSortValueType.ULong)
                {
                    // There are no sign bits in this type.
                    signBitRadixIndex = -1;
                }

                for (int radix = 0; radix < keyByteSize; ++radix)
                {
                    var bucketCountJobData = new RadixSortBucketCountJob<T>
                    {
                        radix = radix,
                        batchSize = batchSize,
                        buckets = buckets,
                        array = arraySource,
                        valueType = valueType
                    };

                    var batchPrefixSumJobData = new RadixSortBatchPrefixSumJob<T>
                    {
                        radix = radix,
                        jobsCount = jobsCount,
                        array = arraySource,
                        counter = counter,
                        buckets = buckets,
                        indices = indices,
                        indicesSum = indicesSum,
                        signBitRadixIndex = signBitRadixIndex
                    };

                    var prefixSumJobData = new RadixSortPrefixSumJob
                    {
                        jobsCount = jobsCount,
                        indices = indices,
                        indicesSum = indicesSum
                    };

                    var bucketSortJobData = new RadixSortBucketSortJob<T>
                    {
                        radix = radix,
                        batchSize = batchSize,
                        indices = indices,
                        array = arraySource,
                        arraySorted = arrayDest,
                        valueType = valueType
                    };

                    jobHandle = bucketCountJobData.ScheduleParallel(jobsCount, 1, jobHandle);
                    jobHandle = batchPrefixSumJobData.ScheduleParallel(16, 1, jobHandle);
                    jobHandle = prefixSumJobData.ScheduleParallel(16, 1, jobHandle);
                    jobHandle = bucketSortJobData.ScheduleParallel(jobsCount, 1, jobHandle);

                    JobHandle.ScheduleBatchedJobs();

                    Swap(ref arraySource, ref arrayDest);
                }

                supportArray.Dispose(jobHandle);
                counter.Dispose(jobHandle);
                buckets.Dispose(jobHandle);
                indices.Dispose(jobHandle);
                indicesSum.Dispose(jobHandle);
            }
            else
            {
                jobHandle = NativeSortExtension.SortJob(array).Schedule();
            }

            return jobHandle;
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        internal struct RadixSortBucketCountJob<T> : IJobFor where T : unmanaged
        {
            [ReadOnly] public int radix;
            [ReadOnly] public int batchSize;
            [ReadOnly] public ParallelSortValueType valueType;
            [ReadOnly] [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<T> array;

            [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<int> buckets;

            public void Execute(int index)
            {
                int start = index * batchSize;
                int end = math.min(start + batchSize, array.Length);

                int jobBuckets = index * 256;

                // This hacky system instead of relying solely on C# generics is because our current version of C# doesn't support IBinaryWriter
                // which would let us restrain the generic type to types that accept the binary shift and "and" operations
                // used in GetBucketIndex.
                if (valueType == ParallelSortValueType.Int)
                {
                    NativeArray<int> intArray = array.Reinterpret<int>(4);
                    for (int i = start; i < end; ++i)
                    {
                        int value = intArray[i];
                        int bucket = GetBucketIndex(value, radix);
                        buckets[jobBuckets + bucket] += 1;
                    }
                }
                else
                {
                    NativeArray<ulong> ulongArray = array.Reinterpret<ulong>(8);
                    for (int i = start; i < end; ++i)
                    {
                        ulong value = ulongArray[i];
                        int bucket = GetBucketIndex(value, radix);
                        buckets[jobBuckets + bucket] += 1;
                    }
                }
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        internal struct RadixSortBatchPrefixSumJob<T> : IJobFor where T : unmanaged
        {
            [ReadOnly] public int radix;
            [ReadOnly] public int jobsCount;
            [ReadOnly] public int signBitRadixIndex;
            [ReadOnly] [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<T> array;

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

                    if (radix != signBitRadixIndex)
                    {
                        for (int i = 0; i < 16; ++i)
                        {
                            int indexSum = indicesSum[i];
                            indicesSum[i] = sum;
                            sum += indexSum;
                        }
                    }
                    // The radix contains the sign bit so might be negative
                    else
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
        internal struct RadixSortBucketSortJob<T> : IJobFor where T : unmanaged
        {
            [ReadOnly] public int radix;
            [ReadOnly] public int batchSize;
            [ReadOnly] public ParallelSortValueType valueType;
            [ReadOnly] [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<T> array;

            [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<int> indices;
            [NativeDisableContainerSafetyRestriction, NoAlias] public NativeArray<T> arraySorted;

            public void Execute(int index)
            {
                int start = index * batchSize;
                int end = math.min(start + batchSize, array.Length);

                int jobIndices = index * 256;

                // This hacky system instead of relying solely on C# generics is because our current version of C# doesn't support IBinaryWriter
                // which would let us restrain the generic type to types that accept the binary shift and "and" operations
                // used in GetBucketIndex.
                if (valueType == ParallelSortValueType.Int)
                {
                    NativeArray<int> inArray = array.Reinterpret<int>(4);
                    NativeArray<int> outArray = arraySorted.Reinterpret<int>(4);
                    for (int i = start; i < end; ++i)
                    {
                        int value = inArray[i];
                        int bucket = GetBucketIndex(value, radix);
                        int sortedIndex = indices[jobIndices + bucket]++;
                        outArray[sortedIndex] = value;
                    }
                }
                else
                {
                    NativeArray<ulong> inArray = array.Reinterpret<ulong>(8);
                    NativeArray<ulong> outArray = arraySorted.Reinterpret<ulong>(8);
                    for (int i = start; i < end; ++i)
                    {
                        ulong value = inArray[i];
                        int bucket = GetBucketIndex(value, radix);
                        int sortedIndex = indices[jobIndices + bucket]++;
                        outArray[sortedIndex] = value;
                    }
                }
            }
        }
    }
}
