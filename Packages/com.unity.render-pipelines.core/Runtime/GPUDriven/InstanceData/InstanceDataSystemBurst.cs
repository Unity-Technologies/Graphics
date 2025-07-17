using Unity.Collections;
using Unity.Burst;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    [BurstCompile]
    internal static class InstanceDataSystemBurst
    {
        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static void ReallocateInstances(bool implicitInstanceIndices, in NativeArray<int> rendererGroupIDs, in NativeArray<GPUDrivenPackedRendererData> packedRendererData,
            in NativeArray<int> instanceOffsets, in NativeArray<int> instanceCounts, ref InstanceAllocators instanceAllocators, ref CPUInstanceData instanceData,
            ref CPUSharedInstanceData sharedInstanceData, ref NativeArray<InstanceHandle> instances,
            ref NativeParallelMultiHashMap<int, InstanceHandle> rendererGroupInstanceMultiHash)
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
                            var idx = instanceData.InstanceToIndex(instance);
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

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static void FreeRendererGroupInstances(in NativeArray<int>.ReadOnly rendererGroupsID, ref InstanceAllocators instanceAllocators, ref CPUInstanceData instanceData,
            ref CPUSharedInstanceData sharedInstanceData, ref NativeParallelMultiHashMap<int, InstanceHandle> rendererGroupInstanceMultiHash)
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

                    var idx = instanceData.InstanceToIndex(instance);
                    instanceData.Remove(instance);
                    instanceAllocators.FreeInstance(instance);

                    success = rendererGroupInstanceMultiHash.TryGetNextValue(out instance, ref it);
                }

                rendererGroupInstanceMultiHash.Remove(rendererGroupID);
            }
        }


        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static void FreeInstances(in NativeArray<InstanceHandle>.ReadOnly instances, ref InstanceAllocators instanceAllocators, ref CPUInstanceData instanceData,
            ref CPUSharedInstanceData sharedInstanceData, ref NativeParallelMultiHashMap<int, InstanceHandle> rendererGroupInstanceMultiHash)
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
}
