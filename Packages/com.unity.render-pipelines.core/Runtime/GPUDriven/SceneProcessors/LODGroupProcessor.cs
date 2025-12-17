using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering
{
    internal class LODGroupProcessor
    {
        private GPUDrivenProcessor m_GPUDrivenProcessor;
        private LODGroupDataSystem m_LODGroupDataSystem;

        public LODGroupProcessor(GPUDrivenProcessor gpuDrivenProcessor, GPUResidentContext context)
        {
            m_GPUDrivenProcessor = gpuDrivenProcessor;
            m_LODGroupDataSystem = context.lodGroupDataSystem;
        }

        public void DestroyInstances(NativeArray<EntityId> destroyedIDs)
        {
            Profiler.BeginSample("DestroyLODGroupInstances");
            m_LODGroupDataSystem.FreeLODGroups(destroyedIDs);
            Profiler.EndSample();
        }

        public void ProcessGameObjectChanges(NativeArray<EntityId> changedLODGroups, bool transformOnly)
        {
            m_GPUDrivenProcessor.DispatchLODGroupData(changedLODGroups, transformOnly, ProcessGameObjectUpdateBatch);
        }

        public void ProcessUpdateBatch(in LODGroupUpdateBatch updateBatch)
        {
            if (updateBatch.TotalLength == 0)
                return;

            Profiler.BeginSample("ProcessLODGroupUpdateBatch");

            if (updateBatch.updateMode == LODGroupUpdateBatchMode.MightIncludeNewInstances)
            {
                Assert.IsTrue(updateBatch.HasAnyComponent(LODGroupComponentMask.GroupSettings));
                Assert.IsTrue(updateBatch.HasAnyComponent(LODGroupComponentMask.WorldSpaceReferencePoint));
                Assert.IsTrue(updateBatch.HasAnyComponent(LODGroupComponentMask.WorldSpaceSize));
                Assert.IsTrue(updateBatch.HasAnyComponent(LODGroupComponentMask.LODBuffer));

                NativeArray<GPUInstanceIndex> instances = m_LODGroupDataSystem.GetOrAllocateInstances(updateBatch, Allocator.TempJob);
                m_LODGroupDataSystem.UpdateLODGroupData(updateBatch, instances);
                instances.Dispose();
            }
            else if (updateBatch.updateMode == LODGroupUpdateBatchMode.OnlyKnownInstances)
            {
                // This mode only support transform-only updates for now.
                Assert.IsTrue(updateBatch.HasAnyComponent(LODGroupComponentMask.WorldSpaceSize));
                Assert.IsTrue(updateBatch.HasAnyComponent(LODGroupComponentMask.WorldSpaceReferencePoint));

                m_LODGroupDataSystem.UpdateLODGroupTransforms(updateBatch);
            }

            Profiler.EndSample();
        }

        void ProcessGameObjectUpdateBatch(in GPUDrivenLODGroupData inputData)
        {
            if (inputData.invalidLODGroup.Length > 0)
                DestroyInstances(inputData.invalidLODGroup);

            if (inputData.lodGroup.Length == 0)
                return;

            var updateMode = inputData.transformOnly ? LODGroupUpdateBatchMode.OnlyKnownInstances : LODGroupUpdateBatchMode.MightIncludeNewInstances;

            var updateBatch = new LODGroupUpdateBatch(new LODGroupUpdateSection
            {
                instanceIDs = inputData.lodGroup,
                worldSpaceReferencePoints = inputData.worldSpaceReferencePoint.Reinterpret<float3>(),
                worldSpaceSizes = inputData.worldSpaceSize,
                lodGroupSettings = inputData.groupSettings,
                forceLODMask = inputData.forceLODMask,
                lodBuffers = inputData.lodBuffer
            },
            updateMode,
            Allocator.TempJob);

            updateBatch.Validate();
            ProcessUpdateBatch(updateBatch);

            updateBatch.Dispose();
        }
    }
}
