using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine.Assertions;
using UnityEngine.Profiling;

#if UNITY_EDITOR
using UnityEditor.Sprites;
#endif

namespace UnityEngine.Rendering
{
    internal partial class InstanceDataSystem : IDisposable
    {
        private unsafe static int AtomicAddLengthNoResize<T>(in NativeList<T> list, int count) where T : unmanaged
        {
            UnsafeList<T>* unsafeList = list.GetUnsafeList();
            var newLength = Interlocked.Add(ref unsafeList->m_length, count);
            Assert.IsTrue(unsafeList->Capacity >= newLength);
            return newLength - count;
        }

        [BurstCompile]
        private unsafe struct QueryRendererGroupInstancesCountJob : IJobParallelForBatch
        {
            public const int k_BatchSize = 128;

            [ReadOnly] public CPUInstanceData instanceData;
            [ReadOnly] public CPUSharedInstanceData sharedInstanceData;
            [ReadOnly] public NativeParallelMultiHashMap<int, InstanceHandle> rendererGroupInstanceMultiHash;
            [NativeDisableContainerSafetyRestriction][ReadOnly] public NativeArray<int> rendererGroupIDs;

            [NativeDisableContainerSafetyRestriction][WriteOnly] public NativeArray<int> instancesCount;

            public void Execute(int startIndex, int count)
            {
                for (int i = startIndex; i < startIndex + count; ++i)
                {
                    var rendererGroupID = rendererGroupIDs[i];

                    if (rendererGroupInstanceMultiHash.TryGetFirstValue(rendererGroupID, out var instance, out var it))
                    {
                        var sharedInstance = instanceData.Get_SharedInstance(instance);
                        var refCount = sharedInstanceData.Get_RefCount(sharedInstance);
                        instancesCount[i] = refCount;
                    }
                    else
                    {
                        instancesCount[i] = 0;
                    }
                }
            }
        }

        [BurstCompile]
        private unsafe struct ComputeInstancesOffsetAndResizeInstancesArrayJob : IJob
        {
            [ReadOnly] public NativeArray<int> instancesCount;
            [WriteOnly] public NativeArray<int> instancesOffset;
            public NativeList<InstanceHandle> instances;

            public void Execute()
            {
                int totalInstancesCount = 0;

                for (int i = 0; i < instancesCount.Length; ++i)
                {
                    instancesOffset[i] = totalInstancesCount;
                    totalInstancesCount += instancesCount[i];
                }

                instances.ResizeUninitialized(totalInstancesCount);
            }
        }

        [BurstCompile]
        private unsafe struct QueryRendererGroupInstancesJob : IJobParallelForBatch
        {
            public const int k_BatchSize = 128;

            [ReadOnly] public NativeParallelMultiHashMap<int, InstanceHandle> rendererGroupInstanceMultiHash;
            [NativeDisableContainerSafetyRestriction][ReadOnly] public NativeArray<int> rendererGroupIDs;

            [NativeDisableContainerSafetyRestriction][WriteOnly] public NativeArray<InstanceHandle> instances;
            [NativeDisableUnsafePtrRestriction] public UnsafeAtomicCounter32 atomicNonFoundInstancesCount;

            public void Execute(int startIndex, int count)
            {
                int newInstancesCountJob = 0;

                for (int i = startIndex; i < startIndex + count; ++i)
                {
                    if (rendererGroupInstanceMultiHash.TryGetFirstValue(rendererGroupIDs[i], out var instance, out var it))
                    {
                        instances[i] = instance;
                    }
                    else
                    {
                        newInstancesCountJob += 1;
                        instances[i] = InstanceHandle.Invalid;
                    }
                }

                if (atomicNonFoundInstancesCount.Counter != null && newInstancesCountJob > 0)
                    atomicNonFoundInstancesCount.Add(newInstancesCountJob);
            }
        }

        [BurstCompile]
        private unsafe struct QueryRendererGroupInstancesMultiJob : IJobParallelForBatch
        {
            public const int k_BatchSize = 128;

            [ReadOnly] public NativeParallelMultiHashMap<int, InstanceHandle> rendererGroupInstanceMultiHash;
            [NativeDisableContainerSafetyRestriction][ReadOnly] public NativeArray<int> rendererGroupIDs;
            [NativeDisableContainerSafetyRestriction][ReadOnly] public NativeArray<int> instancesOffsets;
            [NativeDisableContainerSafetyRestriction][ReadOnly] public NativeArray<int> instancesCounts;

            [NativeDisableContainerSafetyRestriction][WriteOnly] public NativeArray<InstanceHandle> instances;
            [NativeDisableUnsafePtrRestriction] public UnsafeAtomicCounter32 atomicNonFoundSharedInstancesCount;
            [NativeDisableUnsafePtrRestriction] public UnsafeAtomicCounter32 atomicNonFoundInstancesCount;

            public void Execute(int startIndex, int count)
            {
                int newSharedInstancesCountJob = 0;
                int newInstancesCountJob = 0;

                for (int i = startIndex; i < startIndex + count; ++i)
                {
                    var rendererGroupID = rendererGroupIDs[i];
                    int instancesOffset = instancesOffsets[i];
                    int instancesCount = instancesCounts[i];

                    bool success = rendererGroupInstanceMultiHash.TryGetFirstValue(rendererGroupID, out var storedInstance, out var it);

                    if (!success)
                        newSharedInstancesCountJob += 1;

                    for (int j = 0; j < instancesCount; ++j)
                    {
                        int index = instancesOffset + j;

                        if (success)
                        {
                            instances[index] = storedInstance;
                            success = rendererGroupInstanceMultiHash.TryGetNextValue(out storedInstance, ref it);
                        }
                        else
                        {
                            newInstancesCountJob += 1;
                            instances[index] = InstanceHandle.Invalid;
                        }
                    }
                }

                if (atomicNonFoundSharedInstancesCount.Counter != null && newSharedInstancesCountJob > 0)
                    atomicNonFoundSharedInstancesCount.Add(newSharedInstancesCountJob);

                if (atomicNonFoundInstancesCount.Counter != null && newInstancesCountJob > 0)
                    atomicNonFoundInstancesCount.Add(newInstancesCountJob);
            }
        }

        [BurstCompile]
        private struct QuerySortedMeshInstancesJob : IJobParallelForBatch
        {
            public const int k_BatchSize = 64;

            [ReadOnly] public CPUInstanceData instanceData;
            [ReadOnly] public CPUSharedInstanceData sharedInstanceData;
            [ReadOnly] public NativeArray<int> sortedMeshID;

            [NativeDisableParallelForRestriction][WriteOnly] public NativeList<InstanceHandle> instances;

            public void Execute(int startIndex, int count)
            {
                ulong validBits = 0;

                for (int i = 0; i < count; ++i)
                {
                    int instanceIndex = startIndex + i;
                    InstanceHandle instance = instanceData.instances[instanceIndex];
                    Assert.IsTrue(instanceData.IsValidInstance(instance));
                    SharedInstanceHandle sharedInstance = instanceData.sharedInstances[instanceIndex];
                    var meshID = sharedInstanceData.Get_MeshID(sharedInstance);

                    if (sortedMeshID.BinarySearch(meshID) >= 0)
                        validBits |= 1ul << i;
                }

                int validBitCount = math.countbits(validBits);

                if (validBitCount > 0)
                {
                    int writeIndex = AtomicAddLengthNoResize(instances, validBitCount);
                    int validBitIndex = math.tzcnt(validBits);

                    while (validBits != 0)
                    {
                        int instanceIndex = startIndex + validBitIndex;
                        instances[writeIndex] = instanceData.instances[instanceIndex];

                        writeIndex += 1;
                        validBits &= ~(1ul << validBitIndex);
                        validBitIndex = math.tzcnt(validBits);
                    }
                }
            }
        }

        [BurstCompile]
        private struct CalculateInterpolatedLightAndOcclusionProbesBatchJob : IJobParallelFor
        {
            public const int k_BatchSize = 1;
            public const int k_CalculatedProbesPerBatch = 8;

            [ReadOnly] public int probesCount;
            [ReadOnly] public LightProbesQuery lightProbesQuery;

            [NativeDisableParallelForRestriction][ReadOnly] public NativeArray<Vector3> queryPostitions;
            [NativeDisableParallelForRestriction] public NativeArray<int> compactTetrahedronCache;
            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<SphericalHarmonicsL2> probesSphericalHarmonics;
            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<Vector4> probesOcclusion;

            public void Execute(int index)
            {
                var startIndex = index * k_CalculatedProbesPerBatch;
                var endIndex = math.min(probesCount, startIndex + k_CalculatedProbesPerBatch);
                var count = endIndex - startIndex;

                var compactTetrahedronCacheSubArray = compactTetrahedronCache.GetSubArray(startIndex, count);
                var queryPostitionsSubArray = queryPostitions.GetSubArray(startIndex, count);
                var probesSphericalHarmonicsSubArray = probesSphericalHarmonics.GetSubArray(startIndex, count);
                var probesOcclusionSubArray = probesOcclusion.GetSubArray(startIndex, count);
                lightProbesQuery.CalculateInterpolatedLightAndOcclusionProbes(queryPostitionsSubArray, compactTetrahedronCacheSubArray, probesSphericalHarmonicsSubArray, probesOcclusionSubArray);
            }
        }

        [BurstCompile]
        private struct ScatterTetrahedronCacheIndicesJob : IJobParallelFor
        {
            public const int k_BatchSize = 128;

            [ReadOnly] public NativeArray<InstanceHandle> probeInstances;
            [ReadOnly] public NativeArray<int> compactTetrahedronCache;

            [NativeDisableContainerSafetyRestriction][NativeDisableParallelForRestriction] public CPUInstanceData instanceData;

            public void Execute(int index)
            {
                InstanceHandle instance = probeInstances[index];
                instanceData.Set_TetrahedronCacheIndex(instance, compactTetrahedronCache[index]);
            }
        }

        [BurstCompile]
        private unsafe struct TransformUpdateJob : IJobParallelForBatch
        {
            public const int k_BatchSize = 64;

            [ReadOnly] public bool initialize;
            [ReadOnly] public bool enableBoundingSpheres;
            [ReadOnly] public NativeArray<InstanceHandle> instances;
            [ReadOnly] public NativeArray<Matrix4x4> localToWorldMatrices;
            [ReadOnly] public NativeArray<Matrix4x4> prevLocalToWorldMatrices;

            [NativeDisableUnsafePtrRestriction] public UnsafeAtomicCounter32 atomicTransformQueueCount;

            [NativeDisableParallelForRestriction] public CPUSharedInstanceData sharedInstanceData;
            [NativeDisableParallelForRestriction] public CPUInstanceData instanceData;

            [NativeDisableParallelForRestriction] public NativeArray<InstanceHandle> transformUpdateInstanceQueue;
            [NativeDisableParallelForRestriction] public NativeArray<TransformUpdatePacket> transformUpdateDataQueue;
            [NativeDisableParallelForRestriction] public NativeArray<float4> boundingSpheresDataQueue;

            public void Execute(int startIndex, int count)
            {
                ulong validBits = 0;

                for (int i = 0; i < count; ++i)
                {
                    InstanceHandle instance = instances[startIndex + i];

                    if (!instance.valid)
                        continue;

                    if (!initialize)
                    {
                        int instanceIndex = instanceData.InstanceToIndex(instance);
                        int sharedInstanceIndex = sharedInstanceData.InstanceToIndex(instanceData, instance);

                        TransformUpdateFlags flags = sharedInstanceData.flags[sharedInstanceIndex].transformUpdateFlags;
                        bool movedCurrentFrame = instanceData.movedInCurrentFrameBits.Get(instanceIndex);
                        bool isStaticObject = (flags & TransformUpdateFlags.IsPartOfStaticBatch) != 0;

                        if (isStaticObject || movedCurrentFrame)
                            continue;
                    }

                    validBits |= 1ul << i;
                }

                int validBitCount = math.countbits(validBits);

                if (validBitCount > 0)
                {
                    int writeIndex = atomicTransformQueueCount.Add(validBitCount);
                    int validBitIndex = math.tzcnt(validBits);

                    while (validBits != 0)
                    {
                        int index = startIndex + validBitIndex;
                        InstanceHandle instance = instances[index];
                        int instanceIndex = instanceData.InstanceToIndex(instance);
                        int sharedInstanceIndex = sharedInstanceData.InstanceToIndex(instanceData, instance);

                        TransformUpdateFlags flags = sharedInstanceData.flags[sharedInstanceIndex].transformUpdateFlags;
                        bool isStaticObject = (flags & TransformUpdateFlags.IsPartOfStaticBatch) != 0;

                        instanceData.movedInCurrentFrameBits.Set(instanceIndex, !isStaticObject);
                        transformUpdateInstanceQueue[writeIndex] = instance;

                        ref float4x4 l2w = ref UnsafeUtility.ArrayElementAsRef<float4x4>(localToWorldMatrices.GetUnsafeReadOnlyPtr(), index);
                        ref AABB localAABB = ref UnsafeUtility.ArrayElementAsRef<AABB>(sharedInstanceData.localAABBs.GetUnsafePtr(), sharedInstanceIndex);
                        AABB worldAABB = AABB.Transform(l2w, localAABB);
                        instanceData.worldAABBs[instanceIndex] = worldAABB;

                        if (initialize)
                        {
                            PackedMatrix l2wPacked = PackedMatrix.FromFloat4x4(l2w);
                            PackedMatrix l2wPrevPacked = PackedMatrix.FromMatrix4x4(prevLocalToWorldMatrices[index]);

                            transformUpdateDataQueue[writeIndex * 2] = new TransformUpdatePacket()
                            {
                                localToWorld0 = l2wPacked.packed0,
                                localToWorld1 = l2wPacked.packed1,
                                localToWorld2 = l2wPacked.packed2,
                            };
                            transformUpdateDataQueue[writeIndex * 2 + 1] = new TransformUpdatePacket()
                            {
                                localToWorld0 = l2wPrevPacked.packed0,
                                localToWorld1 = l2wPrevPacked.packed1,
                                localToWorld2 = l2wPrevPacked.packed2,
                            };

                            // no need to set instanceData.localToWorldMatrices or instanceData.localToWorldIsFlippedBits
                            // they have been set up already by UpdateRendererInstancesJob
                        }
                        else
                        {
                            PackedMatrix l2wPacked = PackedMatrix.FromMatrix4x4(l2w);

                            transformUpdateDataQueue[writeIndex] = new TransformUpdatePacket()
                            {
                                localToWorld0 = l2wPacked.packed0,
                                localToWorld1 = l2wPacked.packed1,
                                localToWorld2 = l2wPacked.packed2,
                            };

                            float det = math.determinant((float3x3)l2w);
                            instanceData.localToWorldIsFlippedBits.Set(instanceIndex, det < 0.0f);
                        }

                        if (enableBoundingSpheres)
                            boundingSpheresDataQueue[writeIndex] = new float4(worldAABB.center.x, worldAABB.center.y, worldAABB.center.z, math.distance(worldAABB.max, worldAABB.min) * 0.5f);

                        writeIndex += 1;
                        validBits &= ~(1ul << validBitIndex);
                        validBitIndex = math.tzcnt(validBits);
                    }
                }
            }
        }

        [BurstCompile]
        private unsafe struct ProbesUpdateJob : IJobParallelForBatch
        {
            public const int k_BatchSize = 64;

            [ReadOnly] public bool initialize;
            [NativeDisableContainerSafetyRestriction][ReadOnly] public NativeArray<InstanceHandle> instances;
            [NativeDisableParallelForRestriction][NativeDisableContainerSafetyRestriction] public CPUInstanceData instanceData;
            [ReadOnly] public CPUSharedInstanceData sharedInstanceData;

            [NativeDisableUnsafePtrRestriction] public UnsafeAtomicCounter32 atomicProbesQueueCount;

            [NativeDisableParallelForRestriction] public NativeArray<InstanceHandle> probeInstanceQueue;
            [NativeDisableParallelForRestriction] public NativeArray<int> compactTetrahedronCache;
            [NativeDisableParallelForRestriction] public NativeArray<Vector3> probeQueryPosition;

            public void Execute(int startIndex, int count)
            {
                ulong validBits = 0;

                for (int i = 0; i < count; ++i)
                {
                    InstanceHandle instance = instances[startIndex + i];

                    if (!instance.valid)
                        continue;

                    int sharedInstanceIndex = sharedInstanceData.InstanceToIndex(instanceData, instance);
                    TransformUpdateFlags flags = sharedInstanceData.flags[sharedInstanceIndex].transformUpdateFlags;
                    bool isStaticObject = (flags & TransformUpdateFlags.IsPartOfStaticBatch) != 0;

                    if (!initialize && isStaticObject)
                        continue;

                    bool hasLightProbe = (flags & TransformUpdateFlags.HasLightProbeCombined) != 0;

                    if (!hasLightProbe)
                        continue;

                    validBits |= 1ul << i;
                }

                int validBitCount = math.countbits(validBits);

                if (validBitCount > 0)
                {
                    int writeIndex = atomicProbesQueueCount.Add(validBitCount);
                    int validBitIndex = math.tzcnt(validBits);

                    while (validBits != 0)
                    {
                        InstanceHandle instance = instances[startIndex + validBitIndex];
                        int instanceIndex = instanceData.InstanceToIndex(instance);
                        ref AABB worldAABB = ref UnsafeUtility.ArrayElementAsRef<AABB>(instanceData.worldAABBs.GetUnsafePtr(), instanceIndex);

                        probeInstanceQueue[writeIndex] = instance;
                        probeQueryPosition[writeIndex] = worldAABB.center;
                        compactTetrahedronCache[writeIndex] = instanceData.tetrahedronCacheIndices[instanceIndex];

                        writeIndex += 1;
                        validBits &= ~(1ul << validBitIndex);
                        validBitIndex = math.tzcnt(validBits);
                    }
                }
            }
        }

        [BurstCompile]
        private struct MotionUpdateJob : IJobParallelFor
        {
            public const int k_BatchSize = 16;

            [ReadOnly] public int queueWriteBase;

            [NativeDisableParallelForRestriction] public CPUInstanceData instanceData;
            [NativeDisableUnsafePtrRestriction] public UnsafeAtomicCounter32 atomicUpdateQueueCount;
            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<InstanceHandle> transformUpdateInstanceQueue;

            public void Execute(int chunk_index)
            {
                int maxChunkBitCount = math.min(instanceData.instancesLength - 64 * chunk_index, 64);
                ulong chunkBitMask = ~0ul >> (64 - maxChunkBitCount);

                ulong currentChunkBits = instanceData.movedInCurrentFrameBits.GetChunk(chunk_index) & chunkBitMask;
                ulong prevChunkBits = instanceData.movedInPreviousFrameBits.GetChunk(chunk_index) & chunkBitMask;

                // update state in memory for the next frame
                instanceData.movedInCurrentFrameBits.SetChunk(chunk_index, 0);
                instanceData.movedInPreviousFrameBits.SetChunk(chunk_index, currentChunkBits);

                // ensure that objects that were moved last frame update their previous world matrix, if not already fully updated
                ulong remainingChunkBits = prevChunkBits & ~currentChunkBits;

                // allocate space for all the writes from this chunk
                int chunkBitCount = math.countbits(remainingChunkBits);
                int writeIndex = queueWriteBase;
                if (chunkBitCount > 0)
                    writeIndex += atomicUpdateQueueCount.Add(chunkBitCount);

                // loop over set bits to do the writes
                int indexInChunk = math.tzcnt(remainingChunkBits);

                while (indexInChunk < 64)
                {
                    int instanceIndex = 64 * chunk_index + indexInChunk;
                    transformUpdateInstanceQueue[writeIndex] = instanceData.IndexToInstance(instanceIndex);

                    writeIndex += 1;
                    remainingChunkBits &= ~(1ul << indexInChunk);
                    indexInChunk = math.tzcnt(remainingChunkBits);
                }
            }
        }

        [BurstCompile]
        private struct ReallocateInstancesJob : IJob
        {
            [ReadOnly] public bool implicitInstanceIndices;
            [ReadOnly] public NativeArray<int> rendererGroupIDs;
            [ReadOnly] public NativeArray<GPUDrivenPackedRendererData> packedRendererData;
            [ReadOnly] public NativeArray<int> instanceOffsets;
            [ReadOnly] public NativeArray<int> instanceCounts;

            public InstanceAllocators instanceAllocators;
            public CPUInstanceData instanceData;
            public CPUSharedInstanceData sharedInstanceData;
            public NativeArray<InstanceHandle> instances;
            public NativeParallelMultiHashMap<int, InstanceHandle> rendererGroupInstanceMultiHash;

            public void Execute()
            {
                for (int i = 0; i < rendererGroupIDs.Length; ++i)
                {
                    var rendererGroupID = rendererGroupIDs[i];
                    var hasTree = packedRendererData[i].hasTree;

                    int instanceCount;
                    int instanceOffset;

                    if (implicitInstanceIndices)
                    {
                        instanceCount = 1;
                        instanceOffset = i;
                    }
                    else
                    {
                        instanceCount = instanceCounts[i];
                        instanceOffset = instanceOffsets[i];
                    }

                    SharedInstanceHandle sharedInstance;

                    if (rendererGroupInstanceMultiHash.TryGetFirstValue(rendererGroupID, out var instance, out var it))
                    {
                        sharedInstance = instanceData.Get_SharedInstance(instance);

                        int currentInstancesCount = sharedInstanceData.Get_RefCount(sharedInstance);
                        int instancesToFreeCount = currentInstancesCount - instanceCount;

                        if (instancesToFreeCount > 0)
                        {
                            bool success = true;
                            int freedInstancesCount = 0;

                            for (int j = 0; j < instanceCount; ++j)
                                success = rendererGroupInstanceMultiHash.TryGetNextValue(out instance, ref it);

                            Assert.IsTrue(success);

                            while (success)
                            {
                                instanceData.Remove(instance);
                                instanceAllocators.FreeInstance(instance);

                                rendererGroupInstanceMultiHash.Remove(it);
                                ++freedInstancesCount;
                                success = rendererGroupInstanceMultiHash.TryGetNextValue(out instance, ref it);
                            }

                            Assert.AreEqual(instancesToFreeCount, freedInstancesCount);
                        }
                    }
                    else
                    {
                        sharedInstance = instanceAllocators.AllocateSharedInstance();
                        sharedInstanceData.AddNoGrow(sharedInstance);
                    }

                    if (instanceCount > 0)
                    {
                        sharedInstanceData.Set_RefCount(sharedInstance, instanceCount);

                        for (int j = 0; j < instanceCount; ++j)
                        {
                            int instanceIndex = instanceOffset + j;

                            if (instances[instanceIndex].valid)
                                continue;

                            InstanceHandle newInstance;

                            if (!hasTree)
                                newInstance = instanceAllocators.AllocateInstance(InstanceType.MeshRenderer);
                            else
                                newInstance = instanceAllocators.AllocateInstance(InstanceType.SpeedTree);

                            instanceData.AddNoGrow(newInstance);
                            int index = instanceData.InstanceToIndex(newInstance);
                            instanceData.sharedInstances[index] = sharedInstance;
                            instanceData.movedInCurrentFrameBits.Set(index, false);
                            instanceData.movedInPreviousFrameBits.Set(index, false);
                            instanceData.visibleInPreviousFrameBits.Set(index, false);

                            rendererGroupInstanceMultiHash.Add(rendererGroupID, newInstance);
                            instances[instanceIndex] = newInstance;
                        }
                    }
                    else
                    {
                        sharedInstanceData.Remove(sharedInstance);
                        instanceAllocators.FreeSharedInstance(sharedInstance);
                    }
                }
            }
        }

        [BurstCompile]
        private struct FreeInstancesJob : IJob
        {
            [ReadOnly] public NativeArray<InstanceHandle> instances;

            public InstanceAllocators instanceAllocators;
            public CPUInstanceData instanceData;
            public CPUSharedInstanceData sharedInstanceData;
            public NativeParallelMultiHashMap<int, InstanceHandle> rendererGroupInstanceMultiHash;

            public void Execute()
            {
                foreach (var instance in instances)
                {
                    if (!instanceData.IsValidInstance(instance))
                        continue;

                    int instanceIndex = instanceData.InstanceToIndex(instance);
                    SharedInstanceHandle sharedInstance = instanceData.sharedInstances[instanceIndex];
                    int sharedInstanceIndex = sharedInstanceData.SharedInstanceToIndex(sharedInstance);
                    int refCount = sharedInstanceData.refCounts[sharedInstanceIndex];
                    var rendererGroupID = sharedInstanceData.rendererGroupIDs[sharedInstanceIndex];

                    Assert.IsTrue(refCount > 0);

                    if (refCount > 1)
                    {
                        sharedInstanceData.refCounts[sharedInstanceIndex] = refCount - 1;
                    }
                    else
                    {
                        sharedInstanceData.Remove(sharedInstance);
                        instanceAllocators.FreeSharedInstance(sharedInstance);
                    }

                    instanceData.Remove(instance);
                    instanceAllocators.FreeInstance(instance);

                    //@ This will have quadratic cost. Optimize later.
                    for (bool success = rendererGroupInstanceMultiHash.TryGetFirstValue(rendererGroupID, out var i, out var it); success;)
                    {
                        if (instance.Equals(i))
                        {
                            rendererGroupInstanceMultiHash.Remove(it);
                            break;
                        }
                        success = rendererGroupInstanceMultiHash.TryGetNextValue(out i, ref it);
                    }
                }
            }
        }

        [BurstCompile]
        private struct FreeRendererGroupInstancesJob : IJob
        {
            [ReadOnly] public NativeArray<int> rendererGroupsID;

            public InstanceAllocators instanceAllocators;
            public CPUInstanceData instanceData;
            public CPUSharedInstanceData sharedInstanceData;
            public NativeParallelMultiHashMap<int, InstanceHandle> rendererGroupInstanceMultiHash;

            public void Execute()
            {
                foreach (var rendererGroupID in rendererGroupsID)
                {
                    for (bool success = rendererGroupInstanceMultiHash.TryGetFirstValue(rendererGroupID, out var instance, out var it); success;)
                    {
                        SharedInstanceHandle sharedInstance = instanceData.Get_SharedInstance(instance);
                        int sharedInstanceIndex = sharedInstanceData.SharedInstanceToIndex(sharedInstance);
                        int refCount = sharedInstanceData.refCounts[sharedInstanceIndex];

                        Assert.IsTrue(refCount > 0);

                        if (refCount > 1)
                        {
                            sharedInstanceData.refCounts[sharedInstanceIndex] = refCount - 1;
                        }
                        else
                        {
                            sharedInstanceData.Remove(sharedInstance);
                            instanceAllocators.FreeSharedInstance(sharedInstance);
                        }

                        instanceData.Remove(instance);
                        instanceAllocators.FreeInstance(instance);

                        success = rendererGroupInstanceMultiHash.TryGetNextValue(out instance, ref it);
                    }

                    rendererGroupInstanceMultiHash.Remove(rendererGroupID);
                }
            }
        }

        [BurstCompile]
        private unsafe struct UpdateRendererInstancesJob : IJobParallelFor
        {
            public const int k_BatchSize = 128;

            [ReadOnly] public bool implicitInstanceIndices;
            [ReadOnly] public GPUDrivenRendererGroupData rendererData;
            [ReadOnly] public NativeArray<InstanceHandle> instances;
            [ReadOnly] public NativeParallelHashMap<int, GPUInstanceIndex> lodGroupDataMap;

            [NativeDisableParallelForRestriction][NativeDisableContainerSafetyRestriction] public CPUInstanceData instanceData;
            [NativeDisableParallelForRestriction][NativeDisableContainerSafetyRestriction] public CPUSharedInstanceData sharedInstanceData;

            public void Execute(int index)
            {
                var rendererGroupID = rendererData.rendererGroupID[index];
                int meshIndex = rendererData.meshIndex[index];
                var packedRendererData = rendererData.packedRendererData[index];
                var lodGroupID = rendererData.lodGroupID[index];
                var gameObjectLayer = rendererData.gameObjectLayer[index];
                var lightmapIndex = rendererData.lightmapIndex[index];
                var localAABB = rendererData.localBounds[index].ToAABB();

                int meshID = rendererData.meshID[meshIndex];

                const int k_LightmapIndexMask = 0xFFFF;
                const int k_LightmapIndexNotLightmapped = 0xFFFF;
                const int k_LightmapIndexInfluenceOnly = 0xFFFE;
                const uint k_InvalidLODGroupAndMask = 0xFFFFFFFF;

                var instanceFlags = InstanceFlags.None;
                var transformUpdateFlags = TransformUpdateFlags.None;

                var lmIndexMasked = lightmapIndex & k_LightmapIndexMask;

                // Object doesn't have a valid lightmap Index, -> uses probes for lighting
                if (lmIndexMasked >= k_LightmapIndexInfluenceOnly)
                {
                    // Only add the component when needed to store blended results (shader will use the ambient probe when not present)
                    if (packedRendererData.lightProbeUsage == LightProbeUsage.BlendProbes)
                        transformUpdateFlags |= TransformUpdateFlags.HasLightProbeCombined;
                }

                if (packedRendererData.isPartOfStaticBatch)
                    transformUpdateFlags |= TransformUpdateFlags.IsPartOfStaticBatch;

                switch (packedRendererData.shadowCastingMode)
                {
                    case ShadowCastingMode.Off:
                        instanceFlags |= InstanceFlags.IsShadowsOff;
                        break;
                    case ShadowCastingMode.ShadowsOnly:
                        instanceFlags |= InstanceFlags.IsShadowsOnly;
                        break;
                    default:
                        break;
                }

                // If the object is light mapped, or has the special influence-only value, it affects lightmaps
                if (lmIndexMasked != k_LightmapIndexNotLightmapped)
                    instanceFlags |= InstanceFlags.AffectsLightmaps;

                // Mark if it should perform the small-mesh culling test
                if (packedRendererData.smallMeshCulling)
                    instanceFlags |= InstanceFlags.SmallMeshCulling;

                uint lodGroupAndMask = k_InvalidLODGroupAndMask;

                // Renderer's LODGroup could be disabled which means that the renderer is not managed by it.
                if (lodGroupDataMap.TryGetValue(lodGroupID, out var lodGroupHandle))
                {
                    if (packedRendererData.lodMask > 0)
                        lodGroupAndMask = (uint)lodGroupHandle.index << 8 | packedRendererData.lodMask;
                }

                int instancesCount;
                int instancesOffset;

                if (implicitInstanceIndices)
                {
                    instancesCount = 1;
                    instancesOffset = index;
                }
                else
                {
                    instancesCount = rendererData.instancesCount[index];
                    instancesOffset = rendererData.instancesOffset[index];
                }

                if (instancesCount > 0)
                {
                    InstanceHandle instance = instances[instancesOffset];
                    SharedInstanceHandle sharedInstance = instanceData.Get_SharedInstance(instance);
                    Assert.IsTrue(sharedInstance.valid);

                    sharedInstanceData.Set(sharedInstance, rendererGroupID, meshID, localAABB, transformUpdateFlags, instanceFlags, lodGroupAndMask, gameObjectLayer,
                        sharedInstanceData.Get_RefCount(sharedInstance));

                    for (int i = 0; i < instancesCount; ++i)
                    {
                        int instanceIndex = instancesOffset + i;

                        ref Matrix4x4 l2w = ref UnsafeUtility.ArrayElementAsRef<Matrix4x4>(rendererData.localToWorldMatrix.GetUnsafeReadOnlyPtr(), instanceIndex);
                        var worldAABB = AABB.Transform(l2w, localAABB);

                        instance = instances[instanceIndex];
                        Assert.IsTrue(instance.valid);

                        float det = math.determinant((float3x3)(float4x4)l2w);
                        bool isFlipped = (det < 0.0f);

                        instanceData.Set(instance, isFlipped, worldAABB, -1);
                    }
                }
            }
        }

        [BurstCompile]
        private struct CollectInstancesLODGroupsAndMasksJob : IJobParallelFor
        {
            public const int k_BatchSize = 128;

            [ReadOnly] public NativeArray<InstanceHandle> instances;
            [ReadOnly] public CPUInstanceData.ReadOnly instanceData;
            [ReadOnly] public CPUSharedInstanceData.ReadOnly sharedInstanceData;

            [WriteOnly] public NativeArray<uint> lodGroupAndMasks;

            public void Execute(int index)
            {
                var instance = instances[index];
                var sharedInstanceIndex = sharedInstanceData.InstanceToIndex(instanceData, instance);
                lodGroupAndMasks[index] = sharedInstanceData.lodGroupAndMasks[sharedInstanceIndex];
            }
        }

        [BurstCompile]
        private struct GetVisibleNonProcessedTreeInstancesJob : IJobParallelForBatch
        {
            public const int k_BatchSize = 64;

            [ReadOnly] public CPUInstanceData instanceData;
            [ReadOnly] public CPUSharedInstanceData sharedInstanceData;
            [ReadOnly][NativeDisableContainerSafetyRestriction] public ParallelBitArray compactedVisibilityMasks;
            [ReadOnly] public bool becomeVisible;

            [NativeDisableParallelForRestriction] public ParallelBitArray processedBits;

            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<int> rendererIDs;
            [NativeDisableParallelForRestriction][WriteOnly] public NativeArray<InstanceHandle> instances;

            [NativeDisableUnsafePtrRestriction] public UnsafeAtomicCounter32 atomicTreeInstancesCount;

            public void Execute(int startIndex, int count)
            {
                var chunkIndex = startIndex / 64;
                var visibleInPrevFrameChunk = instanceData.visibleInPreviousFrameBits.GetChunk(chunkIndex);
                var processedChunk = processedBits.GetChunk(chunkIndex);

                ulong validBits = 0;

                for (int i = 0; i < count; ++i)
                {
                    int instanceIndex = startIndex + i;
                    InstanceHandle instance = instanceData.IndexToInstance(instanceIndex);
                    bool hasTree = instance.type == InstanceType.SpeedTree;

                    if (hasTree && compactedVisibilityMasks.Get(instance.index))
                    {
                        var bitMask = 1ul << i;

                        var processedInCurrentFrame = (processedChunk & bitMask) != 0;

                        if (!processedInCurrentFrame)
                        {
                            bool visibleInPrevFrame = (visibleInPrevFrameChunk & bitMask) != 0;

                            if (becomeVisible)
                            {
                                if (!visibleInPrevFrame)
                                    validBits |= bitMask;
                            }
                            else
                            {
                                if (visibleInPrevFrame)
                                    validBits |= bitMask;
                            }
                        }
                    }
                }

                int validBitsCount = math.countbits(validBits);

                if (validBitsCount > 0)
                {
                    processedBits.SetChunk(chunkIndex, processedChunk | validBits);

                    int writeIndex = atomicTreeInstancesCount.Add(validBitsCount);
                    int validBitIndex = math.tzcnt(validBits);

                    while (validBits != 0)
                    {
                        int instanceIndex = startIndex + validBitIndex;
                        InstanceHandle instance = instanceData.IndexToInstance(instanceIndex);
                        SharedInstanceHandle sharedInstanceHandle = instanceData.Get_SharedInstance(instance);
                        int rendererID = sharedInstanceData.Get_RendererGroupID(sharedInstanceHandle);

                        rendererIDs[writeIndex] = rendererID;
                        instances[writeIndex] = instance;

                        writeIndex += 1;
                        validBits &= ~(1ul << validBitIndex);
                        validBitIndex = math.tzcnt(validBits);
                    }
                }
            }
        }

        [BurstCompile]
        private struct UpdateCompactedInstanceVisibilityJob : IJobParallelForBatch
        {
            public const int k_BatchSize = 64;

            [ReadOnly] public ParallelBitArray compactedVisibilityMasks;

            [NativeDisableContainerSafetyRestriction][NativeDisableParallelForRestriction] public CPUInstanceData instanceData;

            public void Execute(int startIndex, int count)
            {
                ulong visibleBits = 0;

                for (int i = 0; i < count; ++i)
                {
                    int instanceIndex = startIndex + i;
                    InstanceHandle instance = instanceData.IndexToInstance(instanceIndex);
                    bool visible = compactedVisibilityMasks.Get(instance.index);

                    if (visible)
                        visibleBits |= 1ul << i;
                }

                instanceData.visibleInPreviousFrameBits.SetChunk(startIndex / 64, visibleBits);
            }
        }
    }
}
