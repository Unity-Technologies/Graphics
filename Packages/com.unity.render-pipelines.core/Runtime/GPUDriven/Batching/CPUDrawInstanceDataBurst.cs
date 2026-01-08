using Unity.Collections;
using UnityEngine.Rendering;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    [BurstCompile]
    internal static class CPUDrawInstanceDataBurst
    {
        private static void RemoveDrawRange(in RangeKey key, ref NativeParallelHashMap<RangeKey, int> rangeHash, ref NativeList<DrawRange> drawRanges)
        {
            int drawRangeIndex = rangeHash[key];

            ref DrawRange lastDrawRange = ref drawRanges.ElementAt(drawRanges.Length - 1);
            rangeHash[lastDrawRange.key] = drawRangeIndex;

            rangeHash.Remove(key);
            drawRanges.RemoveAtSwapBack(drawRangeIndex);
        }

        private static void RemoveDrawBatch(in DrawKey key, ref NativeParallelHashMap<RangeKey, int> rangeHash, ref NativeParallelHashMap<DrawKey, int> batchHash,
            ref NativeList<DrawRange> drawRanges, ref NativeList<DrawBatch> drawBatches)
        {
            int drawBatchIndex = batchHash[key];

            ref DrawBatch drawBatch = ref drawBatches.ElementAt(drawBatchIndex);

            int drawRangeIndex = rangeHash[key.range];
            ref DrawRange drawRange = ref drawRanges.ElementAt(drawRangeIndex);

            Assert.IsTrue(drawRange.drawCount > 0);

            if (--drawRange.drawCount == 0)
                RemoveDrawRange(drawRange.key, ref rangeHash, ref drawRanges);

            ref DrawBatch lastDrawBatch = ref drawBatches.ElementAt(drawBatches.Length - 1);
            batchHash[lastDrawBatch.key] = drawBatchIndex;

            batchHash.Remove(key);
            drawBatches.RemoveAtSwapBack(drawBatchIndex);
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static unsafe void RemoveDrawInstanceIndices(in NativeArray<int> drawInstanceIndicesSorted, ref NativeList<DrawInstance> drawInstances, ref NativeParallelHashMap<RangeKey, int> rangeHash,
            ref NativeParallelHashMap<DrawKey, int> batchHash, ref NativeList<DrawRange> drawRanges, ref NativeList<DrawBatch> drawBatches)
        {
            var drawInstancesPtr = (DrawInstance*)drawInstances.GetUnsafePtr();
            var drawInstancesNewBack = drawInstances.Length - 1;

            for (int indexRev = drawInstanceIndicesSorted.Length - 1; indexRev >= 0; --indexRev)
            {
                int indexToRemove = drawInstanceIndicesSorted[indexRev];
                DrawInstance* drawInstance = drawInstancesPtr + indexToRemove;

                int drawBatchIndex = batchHash[drawInstance->key];
                ref DrawBatch drawBatch = ref drawBatches.ElementAt(drawBatchIndex);

                Assert.IsTrue(drawBatch.instanceCount > 0);

                if (--drawBatch.instanceCount == 0)
                    RemoveDrawBatch(drawBatch.key, ref rangeHash, ref batchHash, ref drawRanges, ref drawBatches);

                UnsafeUtility.MemCpy(drawInstance, drawInstancesPtr + drawInstancesNewBack--, sizeof(DrawInstance));
            }

            drawInstances.ResizeUninitialized(drawInstancesNewBack + 1);
        }
    }
}
