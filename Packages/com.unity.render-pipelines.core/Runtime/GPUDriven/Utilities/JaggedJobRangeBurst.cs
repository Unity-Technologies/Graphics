using Unity.Collections;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    [BurstCompile]
    internal static class JaggedJobRangeBurst
    {
        private static int ComputeIdealJobCount(int totalLength, int batchSizeHint, int workerThreadCount)
        {
            Assert.IsTrue(totalLength > 0);
            Assert.IsTrue(batchSizeHint > 0);

            // If there are no worker threads then it should run on the main thread.
            if (workerThreadCount == 0)
                return 1;

            int jobCountBasedOnSizeHint = CoreUtils.DivRoundUp(totalLength, batchSizeHint);

            // Most jobs are waited for on the thread that schedules them. So to even it out (workerThreadCount + 1).
            // Then * 2 because often jobs are not executed perfectly even so if some jobs are longer at least it distributes a bit more.
            // This is no perfect math, just empirically tweaked on profiling data.
            int jobCountBasedOnWorkerThreads = (workerThreadCount + 1) * 2;

            return math.min(jobCountBasedOnSizeHint, jobCountBasedOnWorkerThreads);
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static void ComputeRanges(int workerThreadCount, int batchSizeHint, int totalLength, bool canExceedBatchSizeHint,
            in NativeArray<UntypedUnsafeList> sections, ref NativeList<JaggedJobRange> jobRanges)
        {
            int idealJobCount = ComputeIdealJobCount(totalLength, batchSizeHint, workerThreadCount);
            Assert.IsTrue(idealJobCount > 0);

            int idealBatchSize = CoreUtils.DivRoundUp(totalLength, idealJobCount);
            Assert.IsTrue(idealBatchSize > 0);

            // Try to account for the jobs being possibly split when they would otherwise span over more than one chunk
            int jobRangesInitialCapacity = idealJobCount * 2;
            jobRanges.SetCapacity(jobRangesInitialCapacity);

            int absoluteStart = 0;
            for (int sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++)
            {
                int localStart = 0;
                int remainingLength = sections.ElementAt(sectionIndex).m_length;
                while (remainingLength > 0)
                {
                    // Check for idealBatchSize * 1.5 so that if there is small batch left a the end it is merged with the previous job.
                    int batchSizeUncapped = remainingLength >= (int)(idealBatchSize * 1.5) ? idealBatchSize : remainingLength;

                    int batchSize = batchSizeUncapped;
                    if (!canExceedBatchSizeHint && batchSize > batchSizeHint)
                        batchSize = batchSizeHint;

                    JaggedJobRange jobRange = default;
                    jobRange.sectionIndex = sectionIndex;
                    jobRange.absoluteStart = absoluteStart;
                    jobRange.localStart = localStart;
                    jobRange.length = batchSize;
                    jobRanges.Add(jobRange);

                    localStart += batchSize;
                    absoluteStart += batchSize;
                    remainingLength -= batchSize;
                }

                Assert.AreEqual(remainingLength, 0);
            }
        }
    }
}
