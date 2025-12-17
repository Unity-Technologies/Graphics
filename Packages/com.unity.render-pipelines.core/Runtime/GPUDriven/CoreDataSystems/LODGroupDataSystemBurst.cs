using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    [BurstCompile]
    internal static class LODGroupDataSystemBurst
    {
        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static int GetOrAllocateLODGroupDataInstances(in JaggedSpan<EntityId> jaggedLODGroups,
            ref NativeList<LODGroupData> lodGroupsData,
            ref NativeList<LODGroupCullingData> lodGroupCullingData,
            ref NativeParallelHashMap<EntityId, GPUInstanceIndex> lodGroupDataHash,
            ref NativeList<GPUInstanceIndex> freeLODGroupDataHandles,
            ref NativeArray<GPUInstanceIndex> lodGroupInstances)
        {
            int freeHandlesCount = freeLODGroupDataHandles.Length;
            int lodDataLength = lodGroupsData.Length;

            int absoluteIndex = 0;
            int previousRendererCount = 0;

            for (int sectionIndex = 0; sectionIndex < jaggedLODGroups.sectionCount; ++sectionIndex)
            {
                NativeArray<EntityId> lodGroups = jaggedLODGroups[sectionIndex];

                for (int localIndex = 0; localIndex < lodGroups.Length; localIndex++, ++absoluteIndex)
                {
                    EntityId lodGroup = lodGroups[localIndex];

                    if (!lodGroupDataHash.TryGetValue(lodGroup, out var lodGroupInstance))
                    {
                        if (freeHandlesCount == 0)
                            lodGroupInstance = GPUInstanceIndex.Create(lodDataLength++);
                        else
                            lodGroupInstance = freeLODGroupDataHandles[--freeHandlesCount];

                        lodGroupDataHash.TryAdd(lodGroup, lodGroupInstance);
                    }
                    else
                    {
                        previousRendererCount += lodGroupsData.ElementAt(lodGroupInstance.index).rendererCount;
                    }

                    lodGroupInstances[absoluteIndex] = lodGroupInstance;
                }
            }

            freeLODGroupDataHandles.ResizeUninitialized(freeHandlesCount);
            lodGroupsData.ResizeUninitialized(lodDataLength);
            lodGroupCullingData.ResizeUninitialized(lodDataLength);

            return previousRendererCount;
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static int FreeLODGroupData(in NativeArray<EntityId> destroyedLODGroups,
            ref NativeList<LODGroupData> lodGroupsData,
            ref NativeParallelHashMap<EntityId, GPUInstanceIndex> lodGroupDataHash,
            ref NativeList<GPUInstanceIndex> freeLODGroupDataHandles)
        {
            int removedRendererCount = 0;

            foreach (EntityId lodGroup in destroyedLODGroups)
            {
                if (lodGroupDataHash.TryGetValue(lodGroup, out var lodGroupInstance))
                {
                    Assert.IsTrue(lodGroupInstance.valid);

                    lodGroupDataHash.Remove(lodGroup);
                    freeLODGroupDataHandles.Add(lodGroupInstance);

                    ref LODGroupData lodGroupData = ref lodGroupsData.ElementAt(lodGroupInstance.index);
                    Assert.IsTrue(lodGroupData.valid);

                    removedRendererCount += lodGroupData.rendererCount;
                    lodGroupData.valid = false;
                }
            }

            return removedRendererCount;
        }
    }
}
