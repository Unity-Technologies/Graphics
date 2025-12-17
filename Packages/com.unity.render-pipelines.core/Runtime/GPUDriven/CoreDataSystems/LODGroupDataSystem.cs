using System;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace UnityEngine.Rendering
{
    internal unsafe struct LODGroupData
    {
        public const int k_MaxLODLevelsCount = 8;

        public bool valid;
        public int lodCount;
        public int rendererCount;
        public fixed float screenRelativeTransitionHeights[k_MaxLODLevelsCount];
        public fixed float fadeTransitionWidth[k_MaxLODLevelsCount];
    }

    internal unsafe struct LODGroupCullingData
    {
        public float3 worldSpaceReferencePoint;
        public int lodCount;
        public fixed float sqrDistances[LODGroupData.k_MaxLODLevelsCount]; // we use square distance to get rid of a sqrt in gpu culling..
        public fixed float transitionDistances[LODGroupData.k_MaxLODLevelsCount]; // todo - make this a separate data struct (CPUOnly, as we do not support dithering on GPU..)
        public float worldSpaceSize;// SpeedTree crossfade.
        public fixed bool percentageFlags[LODGroupData.k_MaxLODLevelsCount];// SpeedTree crossfade.
        public byte forceLODMask;
    }

    //@ Merge this class into InstanceDataSystem there is no need to have two separate classes for this.
    internal class LODGroupDataSystem : IDisposable
    {
        private NativeList<LODGroupData> m_LODGroupData;
        private NativeParallelHashMap<EntityId, GPUInstanceIndex> m_LODGroupDataHash;
        private NativeList<LODGroupCullingData> m_LODGroupCullingData;
        private NativeList<GPUInstanceIndex> m_FreeLODGroupDataHandles;

        private int m_CrossfadedRendererCount;
        private bool m_SupportDitheringCrossFade;

        public NativeParallelHashMap<EntityId, GPUInstanceIndex> lodGroupDataHash => m_LODGroupDataHash;
        public NativeList<LODGroupCullingData> lodGroupCullingData => m_LODGroupCullingData;
        public int crossfadedRendererCount => m_CrossfadedRendererCount;
        public int activeLodGroupCount => m_LODGroupData.Length;

        public LODGroupDataSystem(bool supportDitheringCrossFade)
        {
            m_LODGroupData = new NativeList<LODGroupData>(Allocator.Persistent);
            m_LODGroupDataHash = new NativeParallelHashMap<EntityId, GPUInstanceIndex>(64, Allocator.Persistent);

            m_LODGroupCullingData = new NativeList<LODGroupCullingData>(Allocator.Persistent);
            m_FreeLODGroupDataHandles = new NativeList<GPUInstanceIndex>(Allocator.Persistent);

            m_SupportDitheringCrossFade = supportDitheringCrossFade;
        }

        public void Dispose()
        {
            m_LODGroupData.Dispose();
            m_LODGroupDataHash.Dispose();

            m_LODGroupCullingData.Dispose();
            m_FreeLODGroupDataHandles.Dispose();
        }

        public NativeArray<GPUInstanceIndex> GetOrAllocateInstances(in LODGroupUpdateBatch updateBatch, Allocator allocator)
        {
            var instances = new NativeArray<GPUInstanceIndex>(updateBatch.TotalLength, allocator, NativeArrayOptions.UninitializedMemory);

            int previousRendererCount = LODGroupDataSystemBurst.GetOrAllocateLODGroupDataInstances(updateBatch.instanceIDs,
                ref m_LODGroupData,
                ref m_LODGroupCullingData,
                ref m_LODGroupDataHash,
                ref m_FreeLODGroupDataHandles,
                ref instances);

            m_CrossfadedRendererCount -= previousRendererCount;
            Assert.IsTrue(m_CrossfadedRendererCount >= 0);

            return instances;
        }

        public unsafe void UpdateLODGroupData(in LODGroupUpdateBatch updateBatch, NativeArray<GPUInstanceIndex> instances)
        {
            var jobRanges = JaggedJobRange.FromSpanWithRelaxedBatchSize(updateBatch.instanceIDs, 256, Allocator.TempJob);

            int rendererCount = 0;

            new UpdateLODGroupDataJob
            {
                jobRanges = jobRanges.AsArray(),
                lodGroupInstances = instances,
                updateBatch = updateBatch,
                supportDitheringCrossFade = m_SupportDitheringCrossFade,
                lodGroupsData = m_LODGroupData.AsArray(),
                lodGroupsCullingData = m_LODGroupCullingData.AsArray(),
                rendererCount = new UnsafeAtomicCounter32(&rendererCount),
            }
            .RunParallel(jobRanges);

            m_CrossfadedRendererCount += rendererCount;

            jobRanges.Dispose();
        }

        public unsafe void UpdateLODGroupTransforms(in LODGroupUpdateBatch updateBatch)
        {
            var jobRanges = JaggedJobRange.FromSpanWithRelaxedBatchSize(updateBatch.instanceIDs, 256, Allocator.TempJob);

            new UpdateLODGroupTransformJob()
            {
                jobRanges = jobRanges.AsArray(),
                lodGroupDataHash = m_LODGroupDataHash,
                jaggedLODGroups = updateBatch.instanceIDs,
                jaggedWorldSpaceReferencePoints = updateBatch.worldSpaceReferencePoints,
                jaggedWorldSpaceSizes = updateBatch.worldSpaceSizes,
                lodGroupDatas = m_LODGroupData,
                lodGroupCullingDatas = m_LODGroupCullingData,
                supportDitheringCrossFade = m_SupportDitheringCrossFade,
            }
            .RunParallel(jobRanges);

            jobRanges.Dispose();
        }

        public void FreeLODGroups(NativeArray<EntityId> destroyedLODGroupsID)
        {
            if (destroyedLODGroupsID.Length == 0)
                return;

            int removedRendererCount = LODGroupDataSystemBurst.FreeLODGroupData(destroyedLODGroupsID, ref m_LODGroupData, ref m_LODGroupDataHash, ref m_FreeLODGroupDataHandles);

            m_CrossfadedRendererCount -= removedRendererCount;
            Assert.IsTrue(m_CrossfadedRendererCount >= 0);
        }
    }
}
