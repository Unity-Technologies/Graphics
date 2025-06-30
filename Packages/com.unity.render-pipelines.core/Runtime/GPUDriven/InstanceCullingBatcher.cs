using System;
using System.Threading;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering
{
    internal delegate void OnCullingCompleteCallback(JobHandle jobHandle, in BatchCullingContext cullingContext, in BatchCullingOutput cullingOutput);

    internal struct InstanceCullingBatcherDesc
    {
        public OnCullingCompleteCallback onCompleteCallback;

#if UNITY_EDITOR
        public Shader brgPicking;
        public Shader brgLoading;
        public Shader brgError;
#endif

        public static InstanceCullingBatcherDesc NewDefault()
        {
            return new InstanceCullingBatcherDesc()
            {
                onCompleteCallback = null
#if UNITY_EDITOR
                ,brgPicking = null
                ,brgLoading = null
                ,brgError = null
#endif
            };
        }
    }

    internal struct MeshProceduralInfo
    {
        public MeshTopology topology;
        public uint baseVertex;
        public uint firstIndex;
        public uint indexCount;
    }

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
        public const int k_BatchSize = 128;
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
        public const int k_BatchSize = 128;

        [ReadOnly] public NativeArray<InstanceHandle> instancesSorted;
        [NativeDisableContainerSafetyRestriction, NoAlias] [ReadOnly] public NativeList<DrawInstance> drawInstances;

        [WriteOnly] public NativeList<int>.ParallelWriter outDrawInstanceIndicesWriter;

        public void Execute(int startIndex, int count)
        {
            int* instancesToRemove = stackalloc int[k_BatchSize];
            int length = 0;

            for (int i = startIndex; i < startIndex + count; ++i)
            {
                ref DrawInstance drawInstance = ref drawInstances.ElementAt(i);

                if (instancesSorted.BinarySearch(InstanceHandle.FromInt(drawInstance.instanceIndex)) >= 0)
                    instancesToRemove[length++] = i;
            }

            outDrawInstanceIndicesWriter.AddRangeNoResize(instancesToRemove, length);
        }
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal unsafe struct FindMaterialDrawInstancesJob : IJobParallelForBatch
    {
        public const int k_BatchSize = 128;

        [ReadOnly] public NativeArray<uint> materialsSorted;
        [NativeDisableContainerSafetyRestriction, NoAlias] [ReadOnly] public NativeList<DrawInstance> drawInstances;

        [WriteOnly] public NativeList<int>.ParallelWriter outDrawInstanceIndicesWriter;

        public void Execute(int startIndex, int count)
        {
            int* instancesToRemove = stackalloc int[k_BatchSize];
            int length = 0;

            for (int i = startIndex; i < startIndex + count; ++i)
            {
                ref DrawInstance drawInstance = ref drawInstances.ElementAt(i);

                if (materialsSorted.BinarySearch(drawInstance.key.materialID.value) >= 0)
                    instancesToRemove[length++] = i;
            }

            outDrawInstanceIndicesWriter.AddRangeNoResize(instancesToRemove, length);
        }
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal struct FindNonRegisteredMeshesJob : IJobParallelForBatch
    {
        public const int k_BatchSize = 128;

        [ReadOnly] public NativeArray<int> instanceIDs;
        [ReadOnly] public NativeParallelHashMap<int, BatchMeshID> hashMap;

        [WriteOnly] public NativeList<int>.ParallelWriter outInstancesWriter;

        public unsafe void Execute(int startIndex, int count)
        {
            int* notFoundinstanceIDsPtr = stackalloc int[k_BatchSize];
            var notFoundinstanceIDs = new UnsafeList<int>(notFoundinstanceIDsPtr, k_BatchSize);

            notFoundinstanceIDs.Length = 0;

            for (int i = startIndex; i < startIndex + count; ++i)
            {
                int instanceID = instanceIDs[i];

                if (!hashMap.ContainsKey(instanceID))
                    notFoundinstanceIDs.AddNoResize(instanceID);
            }

            outInstancesWriter.AddRangeNoResize(notFoundinstanceIDsPtr, notFoundinstanceIDs.Length);
        }
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal struct FindNonRegisteredMaterialsJob : IJobParallelForBatch
    {
        public const int k_BatchSize = 128;

        [ReadOnly] public NativeArray<int> instanceIDs;
        [ReadOnly] public NativeArray<GPUDrivenPackedMaterialData> packedMaterialDatas;
        [ReadOnly] public NativeParallelHashMap<int, BatchMaterialID> hashMap;

        [WriteOnly] public NativeList<int>.ParallelWriter outInstancesWriter;
        [WriteOnly] public NativeList<GPUDrivenPackedMaterialData>.ParallelWriter outPackedMaterialDatasWriter;

        public unsafe void Execute(int startIndex, int count)
        {
            int* notFoundinstanceIDsPtr = stackalloc int[k_BatchSize];
            var notFoundinstanceIDs = new UnsafeList<int>(notFoundinstanceIDsPtr, k_BatchSize);

            GPUDrivenPackedMaterialData* notFoundPackedMaterialDatasPtr = stackalloc GPUDrivenPackedMaterialData[k_BatchSize];
            var notFoundPackedMaterialDatas = new UnsafeList<GPUDrivenPackedMaterialData>(notFoundPackedMaterialDatasPtr, k_BatchSize);

            notFoundinstanceIDs.Length = 0;
            notFoundPackedMaterialDatas.Length = 0;

            for (int i = startIndex; i < startIndex + count; ++i)
            {
                int instanceID = instanceIDs[i];

                if (!hashMap.ContainsKey(instanceID))
                {
                    notFoundinstanceIDs.AddNoResize(instanceID);
                    notFoundPackedMaterialDatas.AddNoResize(packedMaterialDatas[i]);
                }
            }

            outInstancesWriter.AddRangeNoResize(notFoundinstanceIDsPtr, notFoundinstanceIDs.Length);
            outPackedMaterialDatasWriter.AddRangeNoResize(notFoundPackedMaterialDatasPtr, notFoundPackedMaterialDatas.Length);
        }
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal struct RegisterNewMeshesJob : IJobParallelFor
    {
        public const int k_BatchSize = 128;

        [ReadOnly] public NativeArray<int> instanceIDs;
        [ReadOnly] public NativeArray<BatchMeshID> batchIDs;

        [WriteOnly] public NativeParallelHashMap<int, BatchMeshID>.ParallelWriter hashMap;

        public void Execute(int index)
        {
            hashMap.TryAdd(instanceIDs[index], batchIDs[index]);
        }
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal struct RegisterNewMaterialsJob : IJobParallelFor
    {
        public const int k_BatchSize = 128;

        [ReadOnly] public NativeArray<int> instanceIDs;
        [ReadOnly] public NativeArray<GPUDrivenPackedMaterialData> packedMaterialDatas;
        [ReadOnly] public NativeArray<BatchMaterialID> batchIDs;

        [WriteOnly] public NativeParallelHashMap<int, BatchMaterialID>.ParallelWriter batchMaterialHashMap;
        [WriteOnly] public NativeParallelHashMap<int, GPUDrivenPackedMaterialData>.ParallelWriter packedMaterialHashMap;

        public void Execute(int index)
        {
            var instanceID = instanceIDs[index];
            batchMaterialHashMap.TryAdd(instanceID, batchIDs[index]);
            packedMaterialHashMap.TryAdd(instanceID, packedMaterialDatas[index]);
        }
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
    internal struct RemoveDrawInstanceIndicesJob : IJob
    {
        [NativeDisableContainerSafetyRestriction, NoAlias] [ReadOnly] public NativeArray<int> drawInstanceIndices;

        public NativeList<DrawInstance> drawInstances;
        public NativeParallelHashMap<RangeKey, int> rangeHash;
        public NativeParallelHashMap<DrawKey, int> batchHash;
        public NativeList<DrawRange> drawRanges;
        public NativeList<DrawBatch> drawBatches;

        public void RemoveDrawRange(in RangeKey key)
        {
            int drawRangeIndex = rangeHash[key];

            ref DrawRange lastDrawRange = ref drawRanges.ElementAt(drawRanges.Length - 1);
            rangeHash[lastDrawRange.key] = drawRangeIndex;

            rangeHash.Remove(key);
            drawRanges.RemoveAtSwapBack(drawRangeIndex);
        }

        public void RemoveDrawBatch(in DrawKey key)
        {
            int drawBatchIndex = batchHash[key];

            ref DrawBatch drawBatch = ref drawBatches.ElementAt(drawBatchIndex);

            int drawRangeIndex = rangeHash[key.range];
            ref DrawRange drawRange = ref drawRanges.ElementAt(drawRangeIndex);

            Assert.IsTrue(drawRange.drawCount > 0);

            if (--drawRange.drawCount == 0)
                RemoveDrawRange(drawRange.key);

            ref DrawBatch lastDrawBatch = ref drawBatches.ElementAt(drawBatches.Length - 1);
            batchHash[lastDrawBatch.key] = drawBatchIndex;

            batchHash.Remove(key);
            drawBatches.RemoveAtSwapBack(drawBatchIndex);
        }

        public unsafe void Execute()
        {
            var drawInstancesPtr = (DrawInstance*)drawInstances.GetUnsafePtr();
            var drawInstancesNewBack = drawInstances.Length - 1;

            for (int indexRev = drawInstanceIndices.Length - 1; indexRev >= 0; --indexRev)
            {
                int indexToRemove = drawInstanceIndices[indexRev];
                DrawInstance* drawInstance = drawInstancesPtr + indexToRemove;

                int drawBatchIndex = batchHash[drawInstance->key];
                ref DrawBatch drawBatch = ref drawBatches.ElementAt(drawBatchIndex);

                Assert.IsTrue(drawBatch.instanceCount > 0);

                if (--drawBatch.instanceCount == 0)
                    RemoveDrawBatch(drawBatch.key);

                UnsafeUtility.MemCpy(drawInstance, drawInstancesPtr + drawInstancesNewBack--, sizeof(DrawInstance));
            }

            drawInstances.ResizeUninitialized(drawInstancesNewBack + 1);
        }
    }

    [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]

    internal struct UpdatePackedMaterialDataCacheJob : IJob
    {
        [ReadOnly] public NativeArray<int>.ReadOnly materialIDs;
        [ReadOnly] public NativeArray<GPUDrivenPackedMaterialData>.ReadOnly packedMaterialDatas;

        public NativeParallelHashMap<int, GPUDrivenPackedMaterialData> packedMaterialHash;

        private void ProcessMaterial(int i)
        {
            var materialID = materialIDs[i];
            var packedMaterialData = packedMaterialDatas[i];

            if (materialID == 0)
                return;

            // Cache the packed material so we can detect a change in material that would need to update the renderer data.
            packedMaterialHash[materialID] = packedMaterialData;
        }

        public void Execute()
        {
            for (int i = 0; i < materialIDs.Length; ++i)
                ProcessMaterial(i);
        }
    }

    internal class CPUDrawInstanceData
    {
        public NativeList<DrawInstance> drawInstances => m_DrawInstances;
        public NativeParallelHashMap<DrawKey, int> batchHash => m_BatchHash;
        public NativeList<DrawBatch> drawBatches => m_DrawBatches;
        public NativeParallelHashMap<RangeKey, int> rangeHash => m_RangeHash;
        public NativeList<DrawRange> drawRanges => m_DrawRanges;
        public NativeArray<int> drawBatchIndices => m_DrawBatchIndices.AsArray();
        public NativeArray<int> drawInstanceIndices => m_DrawInstanceIndices.AsArray();

        private NativeParallelHashMap<RangeKey, int> m_RangeHash;       // index in m_DrawRanges, hashes by range state
        private NativeList<DrawRange> m_DrawRanges;
        private NativeParallelHashMap<DrawKey, int> m_BatchHash;        // index in m_DrawBatches, hashed by draw state
        private NativeList<DrawBatch> m_DrawBatches;
        private NativeList<DrawInstance> m_DrawInstances;
        private NativeList<int> m_DrawInstanceIndices;          // DOTS instance index, arranged in contiguous blocks in m_DrawBatches order (see DrawBatch.instanceOffset, DrawBatch.instanceCount)
        private NativeList<int> m_DrawBatchIndices;             // index in m_DrawBatches, arranged in contiguous blocks in m_DrawRanges order (see DrawRange.drawOffset, DrawRange.drawCount)

        private bool m_NeedsRebuild;

        public bool valid => m_DrawInstances.IsCreated;

        public void Initialize()
        {
            Assert.IsTrue(!valid);
            m_RangeHash = new NativeParallelHashMap<RangeKey, int>(1024, Allocator.Persistent);
            m_DrawRanges = new NativeList<DrawRange>(Allocator.Persistent);
            m_BatchHash = new NativeParallelHashMap<DrawKey, int>(1024, Allocator.Persistent);
            m_DrawBatches = new NativeList<DrawBatch>(Allocator.Persistent);
            m_DrawInstances = new NativeList<DrawInstance>(1024, Allocator.Persistent);
            m_DrawInstanceIndices = new NativeList<int>(1024, Allocator.Persistent);
            m_DrawBatchIndices = new NativeList<int>(1024, Allocator.Persistent);
        }

        public void Dispose()
        {
            if (m_DrawBatchIndices.IsCreated)
                m_DrawBatchIndices.Dispose();

            if (m_DrawInstanceIndices.IsCreated)
                m_DrawInstanceIndices.Dispose();

            if (m_DrawInstances.IsCreated)
                m_DrawInstances.Dispose();

            if (m_DrawBatches.IsCreated)
                m_DrawBatches.Dispose();

            if (m_BatchHash.IsCreated)
                m_BatchHash.Dispose();

            if (m_DrawRanges.IsCreated)
                m_DrawRanges.Dispose();

            if (m_RangeHash.IsCreated)
                m_RangeHash.Dispose();
        }

        public void RebuildDrawListsIfNeeded()
        {
            if (!m_NeedsRebuild)
                return;

            m_NeedsRebuild = false;

            Assert.IsTrue(m_RangeHash.Count() == m_DrawRanges.Length);
            Assert.IsTrue(m_BatchHash.Count() == m_DrawBatches.Length);

            m_DrawInstanceIndices.ResizeUninitialized(m_DrawInstances.Length);
            m_DrawBatchIndices.ResizeUninitialized(m_DrawBatches.Length);

            var internalDrawIndex = new NativeArray<int>(drawBatches.Length * BuildDrawListsJob.k_IntsPerCacheLine, Allocator.TempJob, NativeArrayOptions.ClearMemory);

            var prefixSumDrawInstancesJob = new PrefixSumDrawInstancesJob()
            {
                rangeHash = m_RangeHash,
                drawRanges = m_DrawRanges,
                drawBatches = m_DrawBatches,
                drawBatchIndices = m_DrawBatchIndices.AsArray()
            };

            var prefixSumJobHandle = prefixSumDrawInstancesJob.Schedule();

            var buildDrawListsJob = new BuildDrawListsJob()
            {
                drawInstances = m_DrawInstances,
                batchHash = m_BatchHash,
                drawBatches = m_DrawBatches,
                internalDrawIndex = internalDrawIndex,
                drawInstanceIndices = m_DrawInstanceIndices.AsArray(),
            };

            buildDrawListsJob.Schedule(m_DrawInstances.Length, BuildDrawListsJob.k_BatchSize, prefixSumJobHandle).Complete();

            internalDrawIndex.Dispose();
        }

        public void DestroyDrawInstanceIndices(NativeArray<int> drawInstanceIndicesToDestroy)
        {
            Profiler.BeginSample("DestroyDrawInstanceIndices.ParallelSort");
            drawInstanceIndicesToDestroy.ParallelSort().Complete();
            Profiler.EndSample();

            Profiler.BeginSample("DestroyDrawInstanceIndices.RemoveDrawInstanceIndices");
            InstanceCullingBatcherBurst.RemoveDrawInstanceIndices(drawInstanceIndicesToDestroy, ref m_DrawInstances, ref m_RangeHash,
                ref m_BatchHash, ref m_DrawRanges, ref m_DrawBatches);
            Profiler.EndSample();
        }

        public unsafe void DestroyDrawInstances(NativeArray<InstanceHandle> destroyedInstances)
        {
            if (m_DrawInstances.IsEmpty || destroyedInstances.Length == 0)
                return;

            NeedsRebuild();

            var destroyedInstancesSorted = new NativeArray<InstanceHandle>(destroyedInstances, Allocator.TempJob);
            Assert.AreEqual(UnsafeUtility.SizeOf<InstanceHandle>(), UnsafeUtility.SizeOf<int>());

            Profiler.BeginSample("DestroyDrawInstances.ParallelSort");
            destroyedInstancesSorted.Reinterpret<int>().ParallelSort().Complete();
            Profiler.EndSample();

            var drawInstanceIndicesToDestroy = new NativeList<int>(m_DrawInstances.Length, Allocator.TempJob);

            var findDrawInstancesJobHandle = new FindDrawInstancesJob()
            {
                instancesSorted = destroyedInstancesSorted,
                drawInstances = m_DrawInstances,
                outDrawInstanceIndicesWriter = drawInstanceIndicesToDestroy.AsParallelWriter()
            };

            findDrawInstancesJobHandle.ScheduleBatch(m_DrawInstances.Length, FindDrawInstancesJob.k_BatchSize).Complete();

            DestroyDrawInstanceIndices(drawInstanceIndicesToDestroy.AsArray());

            destroyedInstancesSorted.Dispose();
            drawInstanceIndicesToDestroy.Dispose();
        }

        public unsafe void DestroyMaterialDrawInstances(NativeArray<uint> destroyedBatchMaterials)
        {
            if (m_DrawInstances.IsEmpty || destroyedBatchMaterials.Length == 0)
                return;

            NeedsRebuild();

            var destroyedBatchMaterialsSorted = new NativeArray<uint>(destroyedBatchMaterials, Allocator.TempJob);

            Profiler.BeginSample("DestroyedBatchMaterials.ParallelSort");
            destroyedBatchMaterialsSorted.Reinterpret<int>().ParallelSort().Complete();
            Profiler.EndSample();

            var drawInstanceIndicesToDestroy = new NativeList<int>(m_DrawInstances.Length, Allocator.TempJob);

            var findDrawInstancesJobHandle = new FindMaterialDrawInstancesJob()
            {
                materialsSorted = destroyedBatchMaterialsSorted,
                drawInstances = m_DrawInstances,
                outDrawInstanceIndicesWriter = drawInstanceIndicesToDestroy.AsParallelWriter()
            };

            findDrawInstancesJobHandle.ScheduleBatch(m_DrawInstances.Length, FindMaterialDrawInstancesJob.k_BatchSize).Complete();

            DestroyDrawInstanceIndices(drawInstanceIndicesToDestroy.AsArray());

            destroyedBatchMaterialsSorted.Dispose();
            drawInstanceIndicesToDestroy.Dispose();
        }

        public void NeedsRebuild()
        {
            m_NeedsRebuild = true;
        }
    }

    internal class InstanceCullingBatcher : IDisposable
    {
        private RenderersBatchersContext m_BatchersContext;
        private CPUDrawInstanceData m_DrawInstanceData;
        private BatchRendererGroup m_BRG;
        private NativeParallelHashMap<uint, BatchID> m_GlobalBatchIDs;
        private InstanceCuller m_Culler;
        private NativeParallelHashMap<int, BatchMaterialID> m_BatchMaterialHash;
        private NativeParallelHashMap<int, GPUDrivenPackedMaterialData> m_PackedMaterialHash;
        private NativeParallelHashMap<int, BatchMeshID> m_BatchMeshHash;

        private int m_CachedInstanceDataBufferLayoutVersion;

        private OnCullingCompleteCallback m_OnCompleteCallback;

        public NativeParallelHashMap<int, BatchMaterialID> batchMaterialHash => m_BatchMaterialHash;
        public NativeParallelHashMap<int, GPUDrivenPackedMaterialData> packedMaterialHash => m_PackedMaterialHash;

        public InstanceCullingBatcher(RenderersBatchersContext batcherContext, InstanceCullingBatcherDesc desc, BatchRendererGroup.OnFinishedCulling onFinishedCulling)
        {
            m_BatchersContext = batcherContext;
            m_DrawInstanceData = new CPUDrawInstanceData();
            m_DrawInstanceData.Initialize();

            m_BRG = new BatchRendererGroup(new BatchRendererGroupCreateInfo()
            {
                cullingCallback = OnPerformCulling,
                finishedCullingCallback = onFinishedCulling,
                userContext = IntPtr.Zero
            });

#if UNITY_EDITOR
            if (desc.brgPicking != null)
            {
                var mat = new Material(desc.brgPicking);
                mat.hideFlags = HideFlags.HideAndDontSave;
                m_BRG.SetPickingMaterial(mat);
            }
            if (desc.brgLoading != null)
            {
                var mat = new Material(desc.brgLoading);
                mat.hideFlags = HideFlags.HideAndDontSave;
                m_BRG.SetLoadingMaterial(mat);
            }
            if (desc.brgError != null)
            {
                var mat = new Material(desc.brgError);
                mat.hideFlags = HideFlags.HideAndDontSave;
                m_BRG.SetErrorMaterial(mat);
            }
            var viewTypes = new BatchCullingViewType[] {
                BatchCullingViewType.Light,
                BatchCullingViewType.Camera,
                BatchCullingViewType.Picking,
                BatchCullingViewType.SelectionOutline,
                BatchCullingViewType.Filtering
            };
            m_BRG.SetEnabledViewTypes(viewTypes);
#endif

            m_Culler = new InstanceCuller();
            m_Culler.Init(batcherContext.resources, batcherContext.debugStats);

            m_CachedInstanceDataBufferLayoutVersion = -1;
            m_OnCompleteCallback = desc.onCompleteCallback;
            m_BatchMaterialHash = new NativeParallelHashMap<int, BatchMaterialID>(64, Allocator.Persistent);
            m_PackedMaterialHash = new NativeParallelHashMap<int, GPUDrivenPackedMaterialData>(64, Allocator.Persistent);
            m_BatchMeshHash = new NativeParallelHashMap<int, BatchMeshID>(64, Allocator.Persistent);

            m_GlobalBatchIDs = new NativeParallelHashMap<uint, BatchID>(6, Allocator.Persistent);
            m_GlobalBatchIDs.Add((uint)InstanceComponentGroup.Default, GetBatchID(InstanceComponentGroup.Default));
            m_GlobalBatchIDs.Add((uint)InstanceComponentGroup.DefaultWind, GetBatchID(InstanceComponentGroup.DefaultWind));
            m_GlobalBatchIDs.Add((uint)InstanceComponentGroup.DefaultLightProbe, GetBatchID(InstanceComponentGroup.DefaultLightProbe));
            m_GlobalBatchIDs.Add((uint)InstanceComponentGroup.DefaultLightmap, GetBatchID(InstanceComponentGroup.DefaultLightmap));
            m_GlobalBatchIDs.Add((uint)InstanceComponentGroup.DefaultWindLightProbe, GetBatchID(InstanceComponentGroup.DefaultWindLightProbe));
            m_GlobalBatchIDs.Add((uint)InstanceComponentGroup.DefaultWindLightmap, GetBatchID(InstanceComponentGroup.DefaultWindLightmap));
        }

        internal ref InstanceCuller culler => ref m_Culler;

        public void Dispose()
        {
            m_OnCompleteCallback = null;
            m_Culler.Dispose();

            foreach (var batchID in m_GlobalBatchIDs)
            {
                if (!batchID.Value.Equals(BatchID.Null))
                    m_BRG.RemoveBatch(batchID.Value);
            }
            m_GlobalBatchIDs.Dispose();

            if (m_BRG != null)
                m_BRG.Dispose();

            m_DrawInstanceData.Dispose();
            m_DrawInstanceData = null;

            m_BatchMaterialHash.Dispose();
            m_PackedMaterialHash.Dispose();
            m_BatchMeshHash.Dispose();
        }

        private BatchID GetBatchID(InstanceComponentGroup componentsOverriden)
        {
            if (m_CachedInstanceDataBufferLayoutVersion != m_BatchersContext.instanceDataBufferLayoutVersion)
                return BatchID.Null;

            Assert.IsTrue(m_BatchersContext.defaultDescriptions.Length == m_BatchersContext.defaultMetadata.Length);

            const uint kClearIsOverriddenBit = 0x4FFFFFFF;
            var tempMetadata = new NativeList<MetadataValue>(m_BatchersContext.defaultMetadata.Length, Allocator.Temp);

            for(int i = 0; i < m_BatchersContext.defaultDescriptions.Length; ++i)
            {
                var componentGroup = m_BatchersContext.defaultDescriptions[i].componentGroup;
                var metadata = m_BatchersContext.defaultMetadata[i];
                var value = metadata.Value;

                // if instances in this batch do not override the component, clear the override bit
                if ((componentsOverriden & componentGroup) == 0)
                    value &= kClearIsOverriddenBit;

                tempMetadata.Add(new MetadataValue
                {
                    NameID = metadata.NameID,
                    Value = value
                });
            }

            return m_BRG.AddBatch(tempMetadata.AsArray(), m_BatchersContext.gpuInstanceDataBuffer.bufferHandle);
        }

        private void UpdateInstanceDataBufferLayoutVersion()
        {
            if (m_CachedInstanceDataBufferLayoutVersion != m_BatchersContext.instanceDataBufferLayoutVersion)
            {
                m_CachedInstanceDataBufferLayoutVersion = m_BatchersContext.instanceDataBufferLayoutVersion;

                foreach (var componentsToBatchID in m_GlobalBatchIDs)
                {
                    var batchID = componentsToBatchID.Value;
                    if (!batchID.Equals(BatchID.Null))
                        m_BRG.RemoveBatch(batchID);

                    var componentsOverriden = (InstanceComponentGroup)componentsToBatchID.Key;
                    componentsToBatchID.Value = GetBatchID(componentsOverriden);
                }
            }
        }

        public CPUDrawInstanceData GetDrawInstanceData()
        {
            return m_DrawInstanceData;
        }

        public unsafe JobHandle OnPerformCulling(
            BatchRendererGroup rendererGroup,
            BatchCullingContext cc,
            BatchCullingOutput cullingOutput,
            IntPtr userContext)
        {
            foreach (var batchID in m_GlobalBatchIDs)
            {
                if (batchID.Value.Equals(BatchID.Null))
                    return new JobHandle();
            }

            m_DrawInstanceData.RebuildDrawListsIfNeeded();

            bool allowOcclusionCulling = m_BatchersContext.hasBoundingSpheres;
            JobHandle jobHandle = m_Culler.CreateCullJobTree(
                cc,
                cullingOutput,
                m_BatchersContext.instanceData,
                m_BatchersContext.sharedInstanceData,
                m_BatchersContext.instanceDataBuffer,
                m_BatchersContext.lodGroupCullingData,
                m_DrawInstanceData,
                m_GlobalBatchIDs,
                m_BatchersContext.crossfadedRendererCount,
                m_BatchersContext.smallMeshScreenPercentage,
                allowOcclusionCulling ? m_BatchersContext.occlusionCullingCommon : null);

            if (m_OnCompleteCallback != null)
                m_OnCompleteCallback(jobHandle, cc, cullingOutput);

            return jobHandle;
        }

        public void OnFinishedCulling(IntPtr customCullingResult)
        {
            int viewInstanceID = (int)customCullingResult;
            m_Culler.EnsureValidOcclusionTestResults(viewInstanceID);
        }

        public void DestroyDrawInstances(NativeArray<InstanceHandle> instances)
        {
            if (instances.Length == 0)
                return;

            Profiler.BeginSample("DestroyDrawInstances");

            m_DrawInstanceData.DestroyDrawInstances(instances);

            Profiler.EndSample();
        }

        public void DestroyMaterials(NativeArray<int> destroyedMaterials)
        {
            if (destroyedMaterials.Length == 0)
                return;

            Profiler.BeginSample("DestroyMaterials");

            var destroyedBatchMaterials = new NativeList<uint>(destroyedMaterials.Length, Allocator.TempJob);

            foreach (int destroyedMaterial in destroyedMaterials)
            {
                if (m_BatchMaterialHash.TryGetValue(destroyedMaterial, out var destroyedBatchMaterial))
                {
                    destroyedBatchMaterials.Add(destroyedBatchMaterial.value);
                    m_BatchMaterialHash.Remove(destroyedMaterial);
                    m_PackedMaterialHash.Remove(destroyedMaterial);
                    m_BRG.UnregisterMaterial(destroyedBatchMaterial);
                }
            }

            m_DrawInstanceData.DestroyMaterialDrawInstances(destroyedBatchMaterials.AsArray());

            destroyedBatchMaterials.Dispose();

            Profiler.EndSample();
        }

        public void DestroyMeshes(NativeArray<int> destroyedMeshes)
        {
            if (destroyedMeshes.Length == 0)
                return;

            Profiler.BeginSample("DestroyMeshes");

            foreach (int destroyedMesh in destroyedMeshes)
            {
                if (m_BatchMeshHash.TryGetValue(destroyedMesh, out var destroyedBatchMesh))
                {
                    m_BatchMeshHash.Remove(destroyedMesh);
                    m_BRG.UnregisterMesh(destroyedBatchMesh);
                }
            }

            Profiler.EndSample();
        }

        public void PostCullBeginCameraRendering(RenderRequestBatcherContext context)
        {
        }

        private void RegisterBatchMeshes(NativeArray<int> meshIDs)
        {
            var newMeshIDs = new NativeList<int>(meshIDs.Length, Allocator.TempJob);
            new FindNonRegisteredMeshesJob
            {
                instanceIDs = meshIDs,
                hashMap = m_BatchMeshHash,
                outInstancesWriter = newMeshIDs.AsParallelWriter()
            }
            .ScheduleBatch(meshIDs.Length, FindNonRegisteredMeshesJob.k_BatchSize).Complete();
            var newBatchMeshIDs = new NativeArray<BatchMeshID>(newMeshIDs.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            m_BRG.RegisterMeshes(newMeshIDs.AsArray(), newBatchMeshIDs);

            int totalMeshesNum = m_BatchMeshHash.Count() + newBatchMeshIDs.Length;
            m_BatchMeshHash.Capacity = Math.Max(m_BatchMeshHash.Capacity, Mathf.CeilToInt(totalMeshesNum / 1023.0f) * 1024);

            new RegisterNewMeshesJob
            {
                instanceIDs = newMeshIDs.AsArray(),
                batchIDs = newBatchMeshIDs,
                hashMap = m_BatchMeshHash.AsParallelWriter()
            }
            .Schedule(newMeshIDs.Length, RegisterNewMeshesJob.k_BatchSize).Complete();

            newMeshIDs.Dispose();
            newBatchMeshIDs.Dispose();
        }

        private void RegisterBatchMaterials(in NativeArray<int> usedMaterialIDs, in NativeArray<GPUDrivenPackedMaterialData> usedPackedMaterialDatas)
        {
            Debug.Assert(usedMaterialIDs.Length == usedPackedMaterialDatas.Length, "Each material ID should correspond to one packed material data.");
            var newMaterialIDs = new NativeList<int>(usedMaterialIDs.Length, Allocator.TempJob);
            var newPackedMaterialDatas = new NativeList<GPUDrivenPackedMaterialData>(usedMaterialIDs.Length, Allocator.TempJob);
            new FindNonRegisteredMaterialsJob
            {
                instanceIDs = usedMaterialIDs,
                packedMaterialDatas = usedPackedMaterialDatas,
                hashMap = m_BatchMaterialHash,
                outInstancesWriter = newMaterialIDs.AsParallelWriter(),
                outPackedMaterialDatasWriter = newPackedMaterialDatas.AsParallelWriter()
            }
            .ScheduleBatch(usedMaterialIDs.Length, FindNonRegisteredMaterialsJob.k_BatchSize).Complete();

            var newBatchMaterialIDs = new NativeArray<BatchMaterialID>(newMaterialIDs.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            m_BRG.RegisterMaterials(newMaterialIDs.AsArray(), newBatchMaterialIDs);

            int totalMaterialsNum = m_BatchMaterialHash.Count() + newMaterialIDs.Length;
            m_BatchMaterialHash.Capacity = Math.Max(m_BatchMaterialHash.Capacity, Mathf.CeilToInt(totalMaterialsNum / 1023.0f) * 1024);
            m_PackedMaterialHash.Capacity = m_BatchMaterialHash.Capacity;

            new RegisterNewMaterialsJob
            {
                instanceIDs = newMaterialIDs.AsArray(),
                packedMaterialDatas = newPackedMaterialDatas.AsArray(),
                batchIDs = newBatchMaterialIDs,
                batchMaterialHashMap = m_BatchMaterialHash.AsParallelWriter(),
                packedMaterialHashMap = m_PackedMaterialHash.AsParallelWriter()
            }
            .Schedule(newMaterialIDs.Length, RegisterNewMaterialsJob.k_BatchSize).Complete();

            newMaterialIDs.Dispose();
            newPackedMaterialDatas.Dispose();
            newBatchMaterialIDs.Dispose();
        }

        public JobHandle SchedulePackedMaterialCacheUpdate(NativeArray<int> materialIDs, NativeArray<GPUDrivenPackedMaterialData> packedMaterialDatas)
        {
            return new UpdatePackedMaterialDataCacheJob
            {
                materialIDs = materialIDs.AsReadOnly(),
                packedMaterialDatas = packedMaterialDatas.AsReadOnly(),
                packedMaterialHash = m_PackedMaterialHash
            }.Schedule();
        }

        public void BuildBatch(
            NativeArray<InstanceHandle> instances,
            in GPUDrivenRendererGroupData rendererData,
            bool registerMaterialsAndMeshes)
        {
            if (registerMaterialsAndMeshes)
            {
                RegisterBatchMaterials(rendererData.materialID, rendererData.packedMaterialData);
                RegisterBatchMeshes(rendererData.meshID);
            }

            var rangeHash = m_DrawInstanceData.rangeHash;
            var drawRanges = m_DrawInstanceData.drawRanges;
            var batchHash = m_DrawInstanceData.batchHash;
            var drawBatches = m_DrawInstanceData.drawBatches;
            var drawInstances = m_DrawInstanceData.drawInstances;

            InstanceCullingBatcherBurst.CreateDrawBatches(rendererData.instancesCount.Length == 0, instances, rendererData,
                m_BatchMeshHash, m_BatchMaterialHash, m_PackedMaterialHash, ref rangeHash, ref drawRanges, ref batchHash, ref drawBatches, ref drawInstances);

            m_DrawInstanceData.NeedsRebuild();
            UpdateInstanceDataBufferLayoutVersion();
        }

        public void InstanceOccludersUpdated(int viewInstanceID, int subviewMask)
        {
            m_Culler.InstanceOccludersUpdated(viewInstanceID, subviewMask, m_BatchersContext);
        }

        public void UpdateFrame()
        {
            m_Culler.UpdateFrame();
        }

        public ParallelBitArray GetCompactedVisibilityMasks(bool syncCullingJobs)
        {
            return m_Culler.GetCompactedVisibilityMasks(syncCullingJobs);
        }

        public void OnEndContextRendering()
        {
            ParallelBitArray compactedVisibilityMasks = GetCompactedVisibilityMasks(syncCullingJobs: true);

            if(compactedVisibilityMasks.IsCreated)
                m_BatchersContext.UpdatePerFrameInstanceVisibility(compactedVisibilityMasks);
        }

        public void OnBeginCameraRendering(Camera camera)
        {
            m_Culler.OnBeginCameraRendering(camera);
        }

        public void OnEndCameraRendering(Camera camera)
        {
            m_Culler.OnEndCameraRendering(camera);
        }
    }
}
