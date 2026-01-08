using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering
{
    internal struct JaggedJobRange
    {
        public int sectionIndex;
        public int localStart;
        public int absoluteStart;
        public int length;

        public int localEnd => localStart + length;
        public int absoluteEnd => absoluteStart + length;

        public static NativeList<JaggedJobRange> FromSpanWithRelaxedBatchSize<T>(JaggedSpan<T> jaggedSpan, int batchSizeHint, Allocator allocator) where T : unmanaged
        {
            return ComputeRanges(jaggedSpan, batchSizeHint, canExceedBatchSizeHint: true, allocator);
        }

        public static NativeList<JaggedJobRange> FromSpanWithMaxBatchSize<T>(JaggedSpan<T> jaggedSpan, int maxBatchSize, Allocator allocator) where T : unmanaged
        {
            return ComputeRanges(jaggedSpan, maxBatchSize, canExceedBatchSizeHint: false, allocator);
        }

        private static NativeList<JaggedJobRange> ComputeRanges<T>(JaggedSpan<T> jaggedSpan, int batchSizeHint, bool canExceedBatchSizeHint, Allocator allocator) where T : unmanaged
        {
            Assert.IsTrue(batchSizeHint > 0);
            Assert.IsTrue(allocator == Allocator.TempJob || allocator == Allocator.Persistent,
                "Allocator must be either TempJob or Persistent");

            if (jaggedSpan.sectionCount == 0)
                return default;

            var jobRanges = new NativeList<JaggedJobRange>(allocator);

            JaggedJobRangeBurst.ComputeRanges(JobsUtility.JobWorkerCount, batchSizeHint, jaggedSpan.totalLength, canExceedBatchSizeHint,
                jaggedSpan.untypedSections, ref jobRanges);

            return jobRanges;
        }
    }

    internal static class JaggedJobRangeExtensions
    {
        public static JobHandle Schedule<T>(this T job, in NativeList<JaggedJobRange> jobRanges, JobHandle dependsOn = default) where T : unmanaged, IJobParallelFor
        {
            return jobRanges.IsEmpty ? dependsOn : job.ScheduleByRef(jobRanges.Length, 1, dependsOn);
        }

        public static JobHandle ScheduleByRef<T>(ref this T job, in NativeList<JaggedJobRange> jobRanges, JobHandle dependsOn = default) where T : unmanaged, IJobParallelFor
        {
            return jobRanges.IsEmpty ? dependsOn : job.ScheduleByRef(jobRanges.Length, 1, dependsOn);
        }

        public static void RunParallel<T>(this T job, in NativeList<JaggedJobRange> jobRanges, JobHandle dependsOn = default) where T : unmanaged, IJobParallelFor
        {
            if (jobRanges.Length == 1)
            {
                dependsOn.Complete();
                job.RunByRef(1);
            }
            else
            {
                job.ScheduleByRef(jobRanges, dependsOn).Complete();
            }
        }
    }
}
