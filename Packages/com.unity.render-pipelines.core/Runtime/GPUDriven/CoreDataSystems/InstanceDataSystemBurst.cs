using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Burst;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    internal enum InstanceAllocatorVariant
    {
        Null,
        AllocOnly,
        GPUReallocOnly,
        AllocOrGPURealloc
    }

    [BurstCompile]
    internal static class InstanceDataSystemBurst
    {

        // Length == 1 means the archetype is shared over all the instances
        private static GPUArchetypeHandle FetchArchetype(in NativeArray<GPUArchetypeHandle> archetypes, int index) => archetypes.Length == 1 ? archetypes[0] : archetypes[index];

        private static unsafe void AllocOnlyIteration(NativeArray<EntityId> instanceIDSection,
            int absoluteIndex,
            int localIndex,
            in NativeArray<GPUArchetypeHandle> archetypes,
            InstanceAllocators* instanceAllocators,
            ref RenderWorld renderWorld,
            ref NativeArray<InstanceHandle> instances,
            ref NativeParallelHashMap<EntityId, InstanceHandle> rendererToInstanceMap)
        {
            EntityId instanceID = instanceIDSection[localIndex];
            GPUArchetypeHandle archetype = FetchArchetype(archetypes, absoluteIndex);
            InstanceHandle newInstance = instanceAllocators->AllocateInstance();
            InstanceGPUHandle newGPUHandle = instanceAllocators->AllocateInstanceGPUHandle(archetype);
            int instanceIndex = renderWorld.AddInstanceNoGrow(newInstance);

            renderWorld.gpuHandles.ElementAtRW(instanceIndex) = newGPUHandle;
            rendererToInstanceMap.Add(instanceID, newInstance);
            instances[absoluteIndex] = newInstance;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe void AllocOnly(in JaggedSpan<EntityId> jaggedInstanceIDs,
            in NativeArray<GPUArchetypeHandle> archetypes,
            InstanceAllocators* instanceAllocators,
            ref RenderWorld renderWorld,
            ref NativeArray<InstanceHandle> instances,
            ref NativeParallelHashMap<EntityId, InstanceHandle> rendererToInstanceMap)
        {
            int absoluteIndex = 0;

            for (int sectionIndex = 0; sectionIndex < jaggedInstanceIDs.sectionCount; sectionIndex++)
            {
                NativeArray<EntityId> instanceIDs = jaggedInstanceIDs[sectionIndex];

                for (int localIndex = 0; localIndex < instanceIDs.Length; localIndex++, absoluteIndex++)
                {
                    AllocOnlyIteration(instanceIDs, absoluteIndex, localIndex, archetypes, instanceAllocators, ref renderWorld, ref instances, ref rendererToInstanceMap);
                }
            }
        }

        private static unsafe void GPUReallocOnlyIteration(InstanceHandle instance,
            GPUArchetypeHandle archetype,
            InstanceAllocators* instanceAllocators,
            ref RenderWorld renderWorld)
        {
            int instanceIndex = renderWorld.HandleToIndex(instance);
            InstanceGPUHandle gpuHandle = renderWorld.gpuHandles[instanceIndex];
            Assert.IsTrue(gpuHandle.isValid);

            if (archetype.Equals(gpuHandle.archetype))
                return;

            instanceAllocators->FreeInstanceGPUHandle(gpuHandle);
            renderWorld.gpuHandles.ElementAtRW(instanceIndex) = instanceAllocators->AllocateInstanceGPUHandle(archetype);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe void GPUReallocOnly(in NativeArray<GPUArchetypeHandle> archetypes,
            InstanceAllocators* instanceAllocators,
            ref RenderWorld renderWorld,
            ref NativeArray<InstanceHandle> instances)
        {
            for (int i = 0; i < instances.Length; i++)
            {
                InstanceHandle instance = instances[i];
                Assert.IsTrue(instance.isValid, "Invalid Instance");
                if (!instance.isValid)
                    continue;

                GPUArchetypeHandle archetype = FetchArchetype(archetypes, i);
                GPUReallocOnlyIteration(instance, archetype, instanceAllocators, ref renderWorld);
            }
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe void AllocOrGPURealloc(in JaggedSpan<EntityId> jaggedInstanceIDs,
            in NativeArray<GPUArchetypeHandle> archetypes,
            InstanceAllocators* instanceAllocators,
            ref RenderWorld renderWorld,
            ref NativeArray<InstanceHandle> instances,
            ref NativeParallelHashMap<EntityId, InstanceHandle> rendererToInstanceMap)
        {
            int absoluteIndex = 0;

            for (int sectionIndex = 0; sectionIndex < jaggedInstanceIDs.sectionCount; sectionIndex++)
            {
                NativeArray<EntityId> instanceIDs = jaggedInstanceIDs[sectionIndex];

                for (int localIndex = 0; localIndex < instanceIDs.Length; localIndex++, absoluteIndex++)
                {
                    GPUArchetypeHandle archetype = FetchArchetype(archetypes, absoluteIndex);
                    InstanceHandle instance = instances[absoluteIndex];

                    if (instance.isValid)
                    {
                        GPUReallocOnlyIteration(instance, archetype, instanceAllocators, ref renderWorld);
                    }
                    else
                    {
                        AllocOnlyIteration(instanceIDs, absoluteIndex, localIndex, archetypes, instanceAllocators, ref renderWorld, ref instances, ref rendererToInstanceMap);
                    }
                }
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        // Without using InstanceAllocators as a pointer there is a crash inside native containers allocator. Same everywhere.
        public static unsafe void AllocateInstances(InstanceAllocatorVariant allocVariant,
            in JaggedSpan<EntityId> jaggedInstanceIDs,
            in NativeArray<GPUArchetypeHandle> archetypes,
            InstanceAllocators* instanceAllocators,
            ref RenderWorld renderWorld,
            ref NativeArray<InstanceHandle> instances,
            ref NativeParallelHashMap<EntityId, InstanceHandle> rendererToInstanceMap)
        {
            switch (allocVariant)
            {
                case InstanceAllocatorVariant.AllocOnly:
                    AllocOnly(jaggedInstanceIDs, archetypes, instanceAllocators, ref renderWorld, ref instances, ref rendererToInstanceMap);
                    break;

                case InstanceAllocatorVariant.GPUReallocOnly:
                    GPUReallocOnly(archetypes, instanceAllocators, ref renderWorld, ref instances);
                    break;

                case InstanceAllocatorVariant.AllocOrGPURealloc:
                    AllocOrGPURealloc(jaggedInstanceIDs, archetypes, instanceAllocators, ref renderWorld, ref instances, ref rendererToInstanceMap);
                    break;

                default:
                    Assert.IsTrue(false, "Invalid InstanceAllocatorVariant");
                    break;
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static unsafe void FreeInstances(in NativeArray<InstanceHandle> instances,
            InstanceAllocators* instanceAllocators,
            ref RenderWorld renderWorld,
            ref NativeParallelHashMap<EntityId, InstanceHandle> rendererToInstanceMap)
        {
            foreach (var instance in instances)
            {
                if (!renderWorld.IsValidInstance(instance))
                    continue;

                int instanceIndex = renderWorld.HandleToIndex(instance);
                EntityId renderer = renderWorld.instanceIDs[instanceIndex];
                InstanceGPUHandle gpuHandle = renderWorld.gpuHandles[instanceIndex];

                Assert.IsTrue(rendererToInstanceMap[renderer].Equals(instance));
                rendererToInstanceMap.Remove(renderer);
                renderWorld.RemoveInstance(instance);

                instanceAllocators->FreeInstance(instance);
                instanceAllocators->FreeInstanceGPUHandle(gpuHandle);
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static int ComputeTotalTreeCount(in NativeArray<InternalMeshRendererSettings> rendererSettings)
        {
            int totalTreeCount = 0;

            foreach (var settings in rendererSettings)
            {
                totalTreeCount += settings.HasTree ? 1 : 0;
            }

            return totalTreeCount;
        }
    }
}
