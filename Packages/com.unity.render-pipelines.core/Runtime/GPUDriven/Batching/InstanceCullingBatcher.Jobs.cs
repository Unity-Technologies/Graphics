using System;
using System.Threading;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Mathematics;

[assembly: RegisterGenericJobType(typeof(UnityEngine.Rendering.FindNonRegisteredInstanceIDsJob<UnityEngine.Rendering.MeshInfo>))]
[assembly: RegisterGenericJobType(typeof(UnityEngine.Rendering.FindNonRegisteredInstanceIDsJob<UnityEngine.Rendering.GPUDrivenMaterial>))]

namespace UnityEngine.Rendering
{
    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal struct PrefixSumDrawInstancesJob : IJob
    {
        [ReadOnly] public NativeParallelHashMap<RangeKey, int> rangeHash;

        public NativeList<DrawRange> drawRanges;
        public NativeList<DrawBatch> drawBatches;
        public NativeArray<int> drawBatchIndices;

        public void Execute()
        {
            Assert.AreEqual(rangeHash.Count(), drawRanges.Length);
            Assert.AreEqual(drawBatchIndices.Length, drawBatches.Length);

            // Prefix sum to calculate draw offsets for each DrawRange
            int drawPrefixSum = 0;

            for (int i = 0; i < drawRanges.Length; ++i)
            {
                ref DrawRange drawRange = ref drawRanges.ElementAt(i);
                drawRange.drawOffset = drawPrefixSum;
                drawPrefixSum += drawRange.drawCount;
            }

            // Generate DrawBatch index ranges for each DrawRange
            var internalRangeIndex = new NativeArray<int>(drawRanges.Length, Allocator.Temp);

            for (int i = 0; i < drawBatches.Length; ++i)
            {
                ref DrawBatch drawBatch = ref drawBatches.ElementAt(i);
                Assert.IsTrue(drawBatch.instanceCount > 0);

                if (rangeHash.TryGetValue(drawBatch.key.range, out int drawRangeIndex))
                {
                    ref DrawRange drawRange = ref drawRanges.ElementAt(drawRangeIndex);
                    drawBatchIndices[drawRange.drawOffset + internalRangeIndex[drawRangeIndex]] = i;
                    internalRangeIndex[drawRangeIndex]++;
                }
            }

            // Prefix sum to calculate instance offsets for each DrawCommand
            int drawInstancesPrefixSum = 0;

            for (int i = 0; i < drawBatchIndices.Length; ++i)
            {
                // DrawIndices remap to get DrawCommands ordered by DrawRange
                var drawBatchIndex = drawBatchIndices[i];
                ref DrawBatch drawBatch = ref drawBatches.ElementAt(drawBatchIndex);
                drawBatch.instanceOffset = drawInstancesPrefixSum;
                drawInstancesPrefixSum += drawBatch.instanceCount;
            }

            internalRangeIndex.Dispose();
        }
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal unsafe struct BuildDrawListsJob : IJobParallelFor
    {
        public const int k_IntsPerCacheLine = JobsUtility.CacheLineSize / sizeof(int);

        [ReadOnly] public NativeParallelHashMap<DrawKey, int> batchHash;
        [NativeDisableContainerSafetyRestriction, NoAlias] [ReadOnly] public NativeList<DrawInstance> drawInstances;
        [NativeDisableContainerSafetyRestriction, NoAlias] [ReadOnly] public NativeList<DrawBatch> drawBatches;

        [NativeDisableContainerSafetyRestriction, NoAlias] [WriteOnly] public NativeArray<int> internalDrawIndex;
        [NativeDisableContainerSafetyRestriction, NoAlias] [WriteOnly] public NativeArray<int> drawInstanceIndices;

        private unsafe static int IncrementCounter(int* counter)
        {
            return Interlocked.Increment(ref UnsafeUtility.AsRef<int>(counter)) - 1;
        }

        public void Execute(int index)
        {
            // Generate instance index ranges for each DrawCommand
            ref DrawInstance drawInstance = ref drawInstances.ElementAt(index);
            int drawBatchIndex = batchHash[drawInstance.key];

            ref DrawBatch drawBatch = ref drawBatches.ElementAt(drawBatchIndex);
            var offset = IncrementCounter((int*)internalDrawIndex.GetUnsafePtr() + drawBatchIndex * k_IntsPerCacheLine);
            var writeIndex = drawBatch.instanceOffset + offset;
            drawInstanceIndices[writeIndex] = drawInstance.instanceIndex;
        }
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal unsafe struct FindDrawInstancesJob : IJobParallelForBatch
    {
        public const int k_MaxBatchSize = 128;

        [ReadOnly] public NativeArray<InstanceHandle> instancesSorted;
        [NativeDisableContainerSafetyRestriction, NoAlias] [ReadOnly] public NativeList<DrawInstance> drawInstances;

        [WriteOnly] public NativeList<int>.ParallelWriter outDrawInstanceIndicesWriter;

        public void Execute(int startIndex, int count)
        {
            int* instancesToRemovePtr = stackalloc int[k_MaxBatchSize];
            var instancesToRemove = new UnsafeList<int>(instancesToRemovePtr, k_MaxBatchSize);
            instancesToRemove.Length = 0;

            for (int i = startIndex; i < startIndex + count; ++i)
            {
                ref DrawInstance drawInstance = ref drawInstances.ElementAt(i);

                if (instancesSorted.BinarySearch(InstanceHandle.Create(drawInstance.instanceIndex)) >= 0)
                    instancesToRemove.AddNoResize(i);
            }

            outDrawInstanceIndicesWriter.AddRangeNoResize(instancesToRemove);
        }
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal unsafe struct FindMaterialDrawInstancesJob : IJobParallelForBatch
    {
        public const int k_MaxBatchSize = 128;

        [ReadOnly] public NativeArray<uint> materialsSorted;
        [NativeDisableContainerSafetyRestriction, NoAlias] [ReadOnly] public NativeList<DrawInstance> drawInstances;

        [WriteOnly] public NativeList<int>.ParallelWriter outDrawInstanceIndicesWriter;

        public void Execute(int startIndex, int count)
        {
            int* instancesToRemovePtr = stackalloc int[k_MaxBatchSize];
            var instancesToRemove = new UnsafeList<int>(instancesToRemovePtr, k_MaxBatchSize);
            instancesToRemove.Length = 0;

            for (int i = startIndex; i < startIndex + count; ++i)
            {
                ref DrawInstance drawInstance = ref drawInstances.ElementAt(i);

                if (materialsSorted.BinarySearch(drawInstance.key.materialID.value) >= 0)
                    instancesToRemove.AddNoResize(i);
            }

            outDrawInstanceIndicesWriter.AddRangeNoResize(instancesToRemove);
        }
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal struct FindNonRegisteredInstanceIDsJob<T> : IJobParallelFor where T : unmanaged
    {
        public const int MaxBatchSize = 128;

        [ReadOnly] public NativeArray<JaggedJobRange> jobRanges;
        [ReadOnly] public JaggedSpan<EntityId> jaggedInstanceIDs;
        [ReadOnly] public NativeParallelHashMap<EntityId, T> hashMap;

        [WriteOnly] public NativeParallelHashSet<EntityId>.ParallelWriter outInstanceIDWriter;

        public unsafe void Execute(int jobIndex)
        {
            JaggedJobRange jobRange = jobRanges[jobIndex];
            NativeArray<EntityId> instanceIDs = jaggedInstanceIDs[jobRange.sectionIndex];

            for (int i = jobRange.localStart; i < jobRange.localEnd; ++i)
            {
                EntityId instanceID = instanceIDs[i];

                if (!hashMap.ContainsKey(instanceID))
                    outInstanceIDWriter.Add(instanceID);
            }
        }
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal struct RegisterNewMaterialsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<EntityId> instanceIDs;
        [ReadOnly] public NativeArray<GPUDrivenMaterial> materials;

        [WriteOnly] public NativeParallelHashMap<EntityId, GPUDrivenMaterial>.ParallelWriter materialMap;

        public unsafe void Execute(int index)
        {
            bool success = materialMap.TryAdd(instanceIDs[index], materials.ElementAt(index));
            Assert.IsTrue(success);
        }
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal struct RegisterNewMeshesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<EntityId> instanceIDs;
        [ReadOnly] public NativeArray<BatchMeshID> batchMeshIDs;
        [ReadOnly] public NativeArray<GPUDrivenMeshData> meshDatas;
        [ReadOnly] public NativeArray<int> subMeshOffsets;
        [ReadOnly] public NativeArray<GPUDrivenSubMesh> subMeshBuffer;

        [WriteOnly] public NativeParallelHashMap<EntityId, MeshInfo>.ParallelWriter meshMap;

        public unsafe void Execute(int index)
        {
            Assert.IsTrue(instanceIDs.Length == meshDatas.Length);
            Assert.IsTrue(instanceIDs.Length == subMeshOffsets.Length);

            EntityId instanceID = instanceIDs[index];
            int subMeshOffset = subMeshOffsets[index];
            GPUDrivenMeshData meshData = meshDatas[index];
            BatchMeshID batchMeshID = batchMeshIDs[index];

            int totalSubMeshCount = meshData.subMeshCount * math.max(meshData.meshLodCount, 1);
            NativeArray<GPUDrivenSubMesh> subMeshes = subMeshBuffer.GetSubArray(subMeshOffset, totalSubMeshCount);
            var embeddedSubMeshes = new EmbeddedArray64<GPUDrivenSubMesh>(subMeshes, Allocator.Persistent);

            MeshInfo meshInfo = default;
            meshInfo.meshID = batchMeshID;
            meshInfo.meshLodCount = meshData.meshLodCount;
            meshInfo.meshLodSelectionCurve = meshData.meshLodSelectionCurve;
            meshInfo.subMeshes = embeddedSubMeshes;

            bool success = meshMap.TryAdd(instanceID, meshInfo);
            Assert.IsTrue(success);
        }
    }
}
