using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using UnityEngine.Jobs;
using Unity.Mathematics;
using System.Threading;

namespace UnityEngine.Rendering
{
    internal struct InstanceHandle : IComparable<InstanceHandle>
    {
        public int index;
        public static readonly InstanceHandle Invalid = new InstanceHandle() { index = -1 };
        public bool valid => index != -1;
        public bool Equals(InstanceHandle other) => index == other.index;
        public int CompareTo(InstanceHandle other) { return index.CompareTo(other.index); }
    }

    internal class GPURendererInstancePool : IDisposable
    {
        private unsafe static void IncrementCounter(NativeArray<int> counter)
        {
            Interlocked.Increment(ref UnsafeUtility.AsRef<int>((int*)counter.GetUnsafePtr()));
        }

        private unsafe static void AddCounter(NativeArray<int> counter, int value)
        {
            Interlocked.Add(ref UnsafeUtility.AsRef<int>((int*)counter.GetUnsafePtr()), value);
        }

        internal struct GPURendererInstanceDataArrays : IDisposable
        {
            public NativeList<bool> valid;
            //@ transformIndices probably should be moved to TransformUpdater.
            public NativeList<TransformIndex> transformIndices;
            public NativeList<int> rendererIDs;
            public NativeList<int> meshIDs;

            public int Length => valid.Length;

            public GPURendererInstanceDataArrays(int instanceCount, Allocator allocator)
            {
                valid = new NativeList<bool>(instanceCount, allocator);
                transformIndices = new NativeList<TransformIndex>(instanceCount, allocator);
                rendererIDs = new NativeList<int>(instanceCount, allocator);
                meshIDs = new NativeList<int>(instanceCount, allocator);
            }

            public void ResizeArrays(int instanceCount)
            {
                if (Length != instanceCount)
                {
                    valid.Resize(instanceCount, NativeArrayOptions.ClearMemory);
                    transformIndices.Resize(instanceCount, NativeArrayOptions.ClearMemory);
                    rendererIDs.Resize(instanceCount, NativeArrayOptions.ClearMemory);
                    meshIDs.Resize(instanceCount, NativeArrayOptions.ClearMemory);
                }
            }

            public void Dispose()
            {
                valid.Dispose();
                transformIndices.Dispose();
                rendererIDs.Dispose();
                meshIDs.Dispose();
            }
        }

        internal struct InstanceIndexPair : IComparable<InstanceIndexPair>
        {
            public int instance;
            public int index;
            public bool Equals(InstanceIndexPair other) => instance == other.instance;
            public int CompareTo(InstanceIndexPair other) { return instance.CompareTo(other.instance); }
        }

        [BurstCompile(DisableSafetyChecks = true)]
        internal unsafe struct InitializeInstanceIndexPairsArrayJob : IJobParallelFor
        {
            public const int k_BatchSize = 256;

            [ReadOnly] public NativeArray<int> instances;

            [WriteOnly] public NativeArray<InstanceIndexPair> instanceIndexPairs;

            public unsafe void Execute(int index)
            {
                instanceIndexPairs[index] = new InstanceIndexPair { instance = instances[index], index = index };
            }
        }

        [BurstCompile(DisableSafetyChecks = true)]
        internal unsafe struct QueryRendererInstancesJob : IJobParallelFor
        {
            public const int k_BatchSize = 128;

            [ReadOnly] public NativeParallelHashMap<int, InstanceHandle> rendererInstanceHash;
            [NativeDisableContainerSafetyRestriction] [ReadOnly] public NativeArray<int> renderers;

            [NativeDisableContainerSafetyRestriction] [WriteOnly] public NativeArray<InstanceHandle> instances;
            [NativeDisableContainerSafetyRestriction] [WriteOnly] public NativeArray<int> numNewInstances;

            public void Execute(int index)
            {
                int beginIndex = index * k_BatchSize;
                int endIndex = math.min(beginIndex + k_BatchSize, renderers.Length);

                int numNewInstancesJob = 0;

                for (int i = beginIndex; i < endIndex; ++i)
                {
                    if (rendererInstanceHash.TryGetValue(renderers[i], out var instance))
                    {
                        instances[i] = instance;
                    }
                    else
                    {
                        numNewInstancesJob += 1;
                        instances[i] = InstanceHandle.Invalid;
                    }
                }

                if (numNewInstances.IsCreated && numNewInstancesJob > 0)
                    AddCounter(numNewInstances, numNewInstancesJob);
            }
        }

        [BurstCompile(DisableSafetyChecks = true)]
        internal unsafe struct QueryMeshInstancesJob : IJobParallelFor
        {
            public const int k_BatchSize = 64;

            [ReadOnly] public NativeArray<int> aliveInstanceIndices;
            [NativeDisableContainerSafetyRestriction] [ReadOnly] public GPURendererInstanceDataArrays instanceDataArrays;
            [ReadOnly] public NativeArray<InstanceIndexPair> changedMeshes;
            [ReadOnly] public NativeArray<int> destroyedMeshes;

            [WriteOnly] public NativeList<KeyValuePair<InstanceHandle, int>>.ParallelWriter changedMeshInstanceIndexPairsWriter;
            [WriteOnly] public NativeList<InstanceHandle>.ParallelWriter destroyedMeshInstancesWriter;

            unsafe public void Execute(int index)
            {
                //@ Compute upper bound of possible instances referenced by assets and do early out.

                int beginIndex = index * k_BatchSize;
                int endIndex = math.min(beginIndex + k_BatchSize, aliveInstanceIndices.Length);

                for (int i = beginIndex; i < endIndex; ++i)
                {
                    int instanceIndex = aliveInstanceIndices[i];

#if DEBUG
                    Assert.IsTrue(instanceDataArrays.valid[instanceIndex]);
#endif

                    var instance = new InstanceHandle() { index = instanceIndex };

                    var meshIDPtr = (int*)instanceDataArrays.meshIDs.GetUnsafePtr() + instanceIndex;
                    int changedMeshIndex = changedMeshes.BinarySearch(new InstanceIndexPair { instance = *meshIDPtr });

                    if (changedMeshIndex >= 0)
                    {
                        changedMeshInstanceIndexPairsWriter.AddNoResize(new KeyValuePair<InstanceHandle, int>(instance, changedMeshes[changedMeshIndex].index));
                    }
                    else
                    {
                        int destroyedMeshIndex = destroyedMeshes.BinarySearch(*meshIDPtr);

                        if (destroyedMeshIndex >= 0)
                            destroyedMeshInstancesWriter.AddNoResize(instance);
                    }
                }
            }
        }

        [BurstCompile(DisableSafetyChecks = true)]
        internal struct AllocateInstancesJob : IJob
        {
            [ReadOnly] public NativeArray<int> renderersID;

            public NativeArray<InstanceHandle> newInstances;
            public NativeArray<InstanceHandle> instances;

            public NativeArray<int> nextInstanceIndex;
            public NativeList<InstanceHandle> freeInstances;
            public GPURendererInstanceDataArrays instanceData;
            public NativeParallelHashMap<int, InstanceHandle> rendererInstanceHash;

            public void Execute()
            {
                int newInstanceIndex = 0;

                for (int i = 0; i < renderersID.Length; ++i)
                {
                    var instance = instances[i];

                    // Allocate only invalid instances.
                    if (!instance.valid)
                    {
                        int rendererID = renderersID[i];

                        if (freeInstances.IsEmpty)
                        {
                            Assert.IsTrue(nextInstanceIndex[0] < instanceData.Length, "Exceeded maximum number of instances. Cannot add any more.");
                            instance = new InstanceHandle() { index = nextInstanceIndex[0] };
                            ++nextInstanceIndex[0];
                        }
                        else
                        {
                            instance = freeInstances[freeInstances.Length - 1];
                            freeInstances.RemoveAt(freeInstances.Length - 1);
                        }

                        newInstances[newInstanceIndex++] = instance;
                        instances[i] = instance;

                        instanceData.valid[instance.index] = true;
                        instanceData.rendererIDs[instance.index] = rendererID;
                        rendererInstanceHash.Add(rendererID, instance);
                    }
                }

                Assert.AreEqual(newInstanceIndex, newInstances.Length);
            }
        }

        [BurstCompile(DisableSafetyChecks = true)]
        internal struct CopyNewTransformIndicesJob : IJob
        {
            [ReadOnly] public NativeArray<InstanceHandle> newInstances;
            [ReadOnly] public NativeArray<TransformIndex> newTransformIndices;

            [WriteOnly] public GPURendererInstanceDataArrays instanceData;

            public void Execute()
            {
                for (int i = 0; i < newInstances.Length; ++i)
                {
                    instanceData.transformIndices[newInstances[i].index] = newTransformIndices[i];
                }
            }
        }

        [BurstCompile(DisableSafetyChecks = true)]
        internal struct FreeInstancesJob : IJob
        {
            [ReadOnly] public NativeArray<InstanceHandle> instances;

            [WriteOnly] public NativeList<InstanceHandle> freeInstances;
            [WriteOnly] public NativeParallelHashMap<int, InstanceHandle> rendererInstanceHash;
            public GPURendererInstanceDataArrays instanceData;

            public void Execute()
            {
                foreach (var instance in instances)
                {
                    if (instance.valid && instanceData.valid[instance.index])
                    {
                        freeInstances.Add(instance);
                        instanceData.valid[instance.index] = false;
                        rendererInstanceHash.Remove(instanceData.rendererIDs[instance.index]);
                        instanceData.rendererIDs[instance.index] = 0;
                    }
                }
            }
        }

        [BurstCompile(DisableSafetyChecks = true)]
        internal struct UpdateInstanceDataJob : IJob
        {
            [ReadOnly] public GPUDrivenRendererData rendererData;
            [ReadOnly] public NativeArray<InstanceHandle> instances;

            [WriteOnly] public GPURendererInstanceDataArrays instanceData;

            public void Execute()
            {
                for (int i = 0; i < instances.Length; ++i)
                {
                    var instance = instances[i];
                    Assert.IsTrue(instance.valid);
                    instanceData.meshIDs[instance.index] = rendererData.meshID[rendererData.meshIndex[i]];
                }
            }
        }

        TransformUpdater m_TransformUpdater;
        GPURendererInstanceDataArrays m_InstanceData;
        NativeParallelHashMap<int, InstanceHandle> m_RendererInstanceHash;
        NativeList<InstanceHandle> m_FreeInstances;

        private int m_NextInstanceIndex;

        public NativeArray<int> aliveInstanceIndices => m_TransformUpdater.indices.GetSubArray(0, m_TransformUpdater.length);
        public TransformUpdater transformUpdater => m_TransformUpdater;
        public GPURendererInstanceDataArrays instanceDataArrays => m_InstanceData;
        public int maxInstanceCount => m_InstanceData.Length;

        public GPURendererInstancePool(int maxInstances, bool enableBoundingSpheres, GPUResidentDrawerResources resources)
        {
            m_InstanceData = new GPURendererInstanceDataArrays(maxInstances, Allocator.Persistent);
            m_RendererInstanceHash = new NativeParallelHashMap<int, InstanceHandle>(maxInstances, Allocator.Persistent);
            m_FreeInstances = new NativeList<InstanceHandle>(maxInstances, Allocator.Persistent);
            m_TransformUpdater = new TransformUpdater(maxInstances, enableBoundingSpheres, resources);
            m_NextInstanceIndex = 0;
        }

        public void Dispose()
        {
            m_InstanceData.Dispose();
            m_RendererInstanceHash.Dispose();
            m_FreeInstances.Dispose();
            m_TransformUpdater.Dispose();
        }

        private static JobHandle MakeInstanceIndexPairArrayJob(NativeArray<int> instances, out NativeArray<InstanceIndexPair> instanceIndexPairs)
        {
            instanceIndexPairs = new NativeArray<InstanceIndexPair>(instances.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            var job = new InitializeInstanceIndexPairsArrayJob()
            {
                instances = instances,
                instanceIndexPairs = instanceIndexPairs
            };

            if (instances.Length == 0)
                return new JobHandle();

            return job.Schedule(instances.Length, InitializeInstanceIndexPairsArrayJob.k_BatchSize);
        }

        public void Resize(int newLength)
        {
            m_InstanceData.ResizeArrays(newLength);
            m_RendererInstanceHash.Capacity = Math.Max(m_RendererInstanceHash.Capacity, newLength);
            m_TransformUpdater.GrowBuffers(newLength);
        }

        public void ReinitializeInstanceTransforms(NativeArray<InstanceHandle> instances, NativeArray<Matrix4x4> localToWorldMatrices, NativeArray<Matrix4x4> prevLocalToWorldMatrices, in RenderersParameters renderersParameters, GraphicsBuffer gpuBuffer)
        {
            m_TransformUpdater.ReinitializeTransforms(instances, m_InstanceData.transformIndices.AsArray(), localToWorldMatrices, prevLocalToWorldMatrices, renderersParameters, gpuBuffer);
        }

        public void UpdateInstanceTransforms(NativeArray<InstanceHandle> instances, NativeArray<Matrix4x4> localToWorldMatrices, in RenderersParameters renderersParameters, GraphicsBuffer gpuBuffer)
        {
            m_TransformUpdater.UpdateTransforms(instances, m_InstanceData.transformIndices.AsArray(), localToWorldMatrices, renderersParameters, gpuBuffer);
        }

        public void UpdateAllInstanceProbes(in RenderersParameters renderersParameters, GraphicsBuffer gpuBuffer)
        {
            m_TransformUpdater.UpdateAllProbes(renderersParameters, m_InstanceData.transformIndices.AsArray(), gpuBuffer);
        }

        public void AllocateInstances(NativeArray<int> renderersID, NativeArray<InstanceHandle> instances, int numNewInstances)
        {
            var newInstances = new NativeArray<InstanceHandle>(numNewInstances, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var newTransformIndices = new NativeArray<TransformIndex>(numNewInstances, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var nextInstanceIndex = new NativeArray<int>(1, Allocator.TempJob);
            nextInstanceIndex[0] = m_NextInstanceIndex;

            new AllocateInstancesJob
            {
                renderersID = renderersID,
                instances = instances,
                newInstances = newInstances,
                instanceData = m_InstanceData,
                nextInstanceIndex = nextInstanceIndex,
                freeInstances = m_FreeInstances,
                rendererInstanceHash = m_RendererInstanceHash
            }.Run();

            m_NextInstanceIndex = nextInstanceIndex[0];

            m_TransformUpdater.AllocateTransformObjects(newInstances, newTransformIndices);

            new CopyNewTransformIndicesJob
            {
                newInstances = newInstances,
                newTransformIndices = newTransformIndices,
                instanceData = m_InstanceData
            }.Run();

            nextInstanceIndex.Dispose();
            newTransformIndices.Dispose();
            newInstances.Dispose();
        }

        public void UpdateInstanceData(NativeArray<InstanceHandle> instances, in GPUDrivenRendererData rendererData, NativeParallelHashMap<int, InstanceHandle> lodGroupDataMap)
        {
            new UpdateInstanceDataJob
            {
                instances = instances,
                rendererData = rendererData,
                instanceData = m_InstanceData
            }.Run();

            m_TransformUpdater.UpdateTransformObjects(instances, m_InstanceData.transformIndices.AsArray(), rendererData, lodGroupDataMap);
        }

        public void FreeInstances(NativeArray<InstanceHandle> instances)
        {
            m_TransformUpdater.DestroyTransformObjects(m_InstanceData, instances);

            new FreeInstancesJob
            {
                instances = instances,
                freeInstances = m_FreeInstances,
                rendererInstanceHash = m_RendererInstanceHash,
                instanceData = m_InstanceData
            }.Run();
        }

        public void QueryInstanceData(NativeArray<int> changedRenderers, NativeArray<int> destroyedRenderers, NativeArray<int> transformedRenderers, NativeArray<int> changedMeshesSorted, NativeArray<int> destroyedMeshesSorted,
            NativeArray<InstanceHandle> outChangedRendererInstances, NativeArray<InstanceHandle> outDestroyedRendererInstances, NativeArray<InstanceHandle> outTransformedInstances,
            NativeList<KeyValuePair<InstanceHandle, int>> outChangedMeshInstanceIndexPairs, NativeList<InstanceHandle> outDestroyedMeshInstances)
        {
            Assert.AreEqual(changedRenderers.Length, outChangedRendererInstances.Length);
            Assert.AreEqual(destroyedRenderers.Length, outDestroyedRendererInstances.Length);
            Assert.AreEqual(transformedRenderers.Length, outTransformedInstances.Length);
            Assert.IsTrue(outChangedMeshInstanceIndexPairs.IsEmpty);
            Assert.IsTrue(outDestroyedMeshInstances.IsEmpty);

            var queryJobHandle = new JobHandle();

            if (changedRenderers.Length != 0)
            {
                var queryRendererInstancesJobData = new QueryRendererInstancesJob()
                {
                    rendererInstanceHash = m_RendererInstanceHash,
                    renderers = changedRenderers,
                    instances = outChangedRendererInstances
                };

                var jobHandle = queryRendererInstancesJobData.Schedule(Mathf.CeilToInt((float)changedRenderers.Length / QueryRendererInstancesJob.k_BatchSize), 1);

                queryJobHandle = JobHandle.CombineDependencies(queryJobHandle, jobHandle);
            }

            if (destroyedRenderers.Length != 0)
            {
                var queryRendererInstancesJobData = new QueryRendererInstancesJob()
                {
                    rendererInstanceHash = m_RendererInstanceHash,
                    renderers = destroyedRenderers,
                    instances = outDestroyedRendererInstances
                };

                var jobHandle = queryRendererInstancesJobData.Schedule(Mathf.CeilToInt((float)destroyedRenderers.Length / QueryRendererInstancesJob.k_BatchSize), 1);

                queryJobHandle = JobHandle.CombineDependencies(queryJobHandle, jobHandle);
            }

            if (transformedRenderers.Length != 0)
            {
                var queryRendererInstancesJobData = new QueryRendererInstancesJob()
                {
                    rendererInstanceHash = m_RendererInstanceHash,
                    renderers = transformedRenderers,
                    instances = outTransformedInstances
                };

                var jobHandle = queryRendererInstancesJobData.Schedule(Mathf.CeilToInt((float)transformedRenderers.Length / QueryRendererInstancesJob.k_BatchSize), 1);

                queryJobHandle = JobHandle.CombineDependencies(queryJobHandle, jobHandle);
            }

            if (changedMeshesSorted.Length != 0 || destroyedMeshesSorted.Length != 0)
            {
                var initJobHandle = new JobHandle();
                initJobHandle = JobHandle.CombineDependencies(initJobHandle, MakeInstanceIndexPairArrayJob(changedMeshesSorted, out var changedMeshPairs));

                outChangedMeshInstanceIndexPairs.Capacity = aliveInstanceIndices.Length;
                outDestroyedMeshInstances.Capacity = aliveInstanceIndices.Length;

                var queryAssetInstancesJobData = new QueryMeshInstancesJob()
                {
                    aliveInstanceIndices = aliveInstanceIndices,
                    instanceDataArrays = m_InstanceData,
                    changedMeshes = changedMeshPairs,
                    destroyedMeshes = destroyedMeshesSorted,
                    changedMeshInstanceIndexPairsWriter = outChangedMeshInstanceIndexPairs.AsParallelWriter(),
                    destroyedMeshInstancesWriter = outDestroyedMeshInstances.AsParallelWriter()
                };

                var jobHandle = queryAssetInstancesJobData.Schedule(Mathf.CeilToInt((float)aliveInstanceIndices.Length / QueryMeshInstancesJob.k_BatchSize), 1, initJobHandle);

                changedMeshPairs.Dispose(jobHandle);

                queryJobHandle = JobHandle.CombineDependencies(queryJobHandle, jobHandle);
            }

            queryJobHandle.Complete();
        }

        public int QueryInstances(NativeArray<int> renderersID, NativeArray<InstanceHandle> instances)
        {
            Assert.AreEqual(renderersID.Length, instances.Length);

            var numNewInstancesCounter = new NativeArray<int>(1, Allocator.TempJob);

            var queryRendererInstancesJobData = new QueryRendererInstancesJob()
            {
                rendererInstanceHash = m_RendererInstanceHash,
                renderers = renderersID,
                instances = instances,
                numNewInstances = numNewInstancesCounter
            };

            queryRendererInstancesJobData.Schedule(Mathf.CeilToInt((float)renderersID.Length / QueryRendererInstancesJob.k_BatchSize), 1).Complete();

            int numNewInstances = numNewInstancesCounter[0];
            numNewInstancesCounter.Dispose();

            return numNewInstances;
        }

        public int GetRendererInstanceID(InstanceHandle instance)
        {
            Assert.IsTrue(instance.valid);

            return m_InstanceData.rendererIDs[instance.index];
        }

        public InstanceHandle GetInstanceHandle(int instanceID)
        {
            if (m_RendererInstanceHash.TryGetValue(instanceID, out var instance))
                return instance;

            return InstanceHandle.Invalid;
        }

        public TransformIndex GetTransformIndex(InstanceHandle instance)
        {
            Assert.IsTrue(instance.valid);

            return m_InstanceData.transformIndices[instance.index];
        }

        public Matrix4x4 GetInstanceLocalToWorldMatrix(InstanceHandle instance)
        {
            Assert.IsTrue(instance.valid);

            return m_TransformUpdater.drawData.localToWorldMatrices[instance.index];
        }

        public bool InternalSanityCheckStates()
        {
            Dictionary<int, int> usedInstances = new Dictionary<int, int>();

            for (int i = 0; i < m_InstanceData.Length; ++i)
            {
                if (!m_InstanceData.valid[i])
                    continue;

                usedInstances.Add(i, 1);
            }

            var aliveIndicesArray = aliveInstanceIndices;

            for (int i = 0; i < aliveIndicesArray.Length; ++i)
            {
                if (!usedInstances.TryGetValue(aliveIndicesArray[i], out var counter))
                {
                    return false;
                }

                if (counter != 1)
                    return false;

                usedInstances[aliveIndicesArray[i]] = counter + 1;
            }

            return true;
        }
    }
}
