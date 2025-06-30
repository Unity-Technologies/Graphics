using Unity.Collections;
using Unity.Burst;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    [BurstCompile]
    internal static class LODGroupDataPoolBurst
    {
        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static int FreeLODGroupData(in NativeArray<int> destroyedLODGroupsID, ref NativeList<LODGroupData> lodGroupsData,
            ref NativeParallelHashMap<int, GPUInstanceIndex> lodGroupDataHash, ref NativeList<GPUInstanceIndex> freeLODGroupDataHandles)
        {
            int removedRendererCount = 0;

            foreach (int lodGroupID in destroyedLODGroupsID)
            {
                if (lodGroupDataHash.TryGetValue(lodGroupID, out var lodGroupInstance))
                {
                    Assert.IsTrue(lodGroupInstance.valid);

                    lodGroupDataHash.Remove(lodGroupID);
                    freeLODGroupDataHandles.Add(lodGroupInstance);

                    ref LODGroupData lodGroupData = ref lodGroupsData.ElementAt(lodGroupInstance.index);
                    Assert.IsTrue(lodGroupData.valid);

                    removedRendererCount += lodGroupData.rendererCount;
                    lodGroupData.valid = false;
                }
            }

            return removedRendererCount;
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static int AllocateOrGetLODGroupDataInstances(in NativeArray<int> lodGroupsID, ref NativeList<LODGroupData> lodGroupsData, ref NativeList<LODGroupCullingData> lodGroupCullingData,
            ref NativeParallelHashMap<int, GPUInstanceIndex> lodGroupDataHash, ref NativeList<GPUInstanceIndex> freeLODGroupDataHandles, ref NativeArray<GPUInstanceIndex> lodGroupInstances)
        {
            int freeHandlesCount = freeLODGroupDataHandles.Length;
            int lodDataLength = lodGroupsData.Length;
            int previousRendererCount = 0;

            for (int i = 0; i < lodGroupsID.Length; ++i)
            {
                int lodGroupID = lodGroupsID[i];

                if (!lodGroupDataHash.TryGetValue(lodGroupID, out var lodGroupInstance))
                {
                    if (freeHandlesCount == 0)
                        lodGroupInstance = new GPUInstanceIndex() { index = lodDataLength++ };
                    else
                        lodGroupInstance = freeLODGroupDataHandles[--freeHandlesCount];

                    lodGroupDataHash.TryAdd(lodGroupID, lodGroupInstance);
                }
                else
                {
                    previousRendererCount += lodGroupsData.ElementAt(lodGroupInstance.index).rendererCount;
                }

                lodGroupInstances[i] = lodGroupInstance;
            }

            freeLODGroupDataHandles.ResizeUninitialized(freeHandlesCount);
            lodGroupsData.ResizeUninitialized(lodDataLength);
            lodGroupCullingData.ResizeUninitialized(lodDataLength);

            return previousRendererCount;
        }
    }
}
