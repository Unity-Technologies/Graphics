using Unity.Collections;
using Unity.Burst;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    [BurstCompile]
    internal static class MeshRendererProcessorBurst
    {
        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static unsafe void ComputeInstanceGPUArchetypes(in NativeReference<GPUArchetypeManager> archetypeManager,
            in DefaultGPUComponents defaultGPUComponents,
            MeshRendererUpdateBatch* updateBatch,
            in GPUComponentSet overrideComponentSet,
            bool useSharedGPUArchetype,
            ref NativeArray<GPUArchetypeHandle> archetypes)
        {
            if (useSharedGPUArchetype)
            {
                Assert.IsTrue(archetypes.Length == 1);

                GPUComponentSet componentSet = MeshRendererProcessor.ComputeComponentSet(defaultGPUComponents, updateBatch->lightmapUsage, updateBatch->blendProbesUsage);
                componentSet.AddSet(overrideComponentSet);
                archetypes[0] = archetypeManager.GetRef().GetOrCreateArchetype(componentSet);
            }
            else
            {
                Assert.IsTrue(archetypes.Length == updateBatch->TotalLength);

                bool hasMeshRendererSettings = updateBatch->HasAnyComponent(MeshRendererComponentMask.RendererSettings);
                bool hasLightmap = updateBatch->HasAnyComponent(MeshRendererComponentMask.Lightmap);

                int flatIndex = 0;
                for (int sectionIndex = 0; sectionIndex < updateBatch->SectionCount; sectionIndex++)
                {
                    NativeArray<InternalMeshRendererSettings> rendererSettingsSection = updateBatch->GetRendererSettingsSectionOrDefault(sectionIndex);
                    NativeArray<short> lightmapIndexSection = updateBatch->GetLightmapIndexSectionOrDefault(sectionIndex);

                    for (int i = 0; i < updateBatch->GetSectionLength(sectionIndex); i++)
                    {
                        InternalMeshRendererSettings rendererSettings = hasMeshRendererSettings ? rendererSettingsSection[i] : RenderWorld.DefaultRendererSettings;
                        int lightmapIndex = hasLightmap ? lightmapIndexSection[i] : RenderWorld.DefaultLightmapIndex;
                        GPUComponentSet componentSet = MeshRendererProcessor.ComputeComponentSet(defaultGPUComponents, rendererSettings, lightmapIndex);
                        componentSet.AddSet(overrideComponentSet);

                        archetypes[flatIndex] = archetypeManager.GetRef().GetOrCreateArchetype(componentSet);
                        ++flatIndex;
                    }
                }
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static unsafe void BuildGPUComponentOverrideUploadSources(in NativeReference<GPUArchetypeManager> archetypeManager,
            in NativeArray<GPUComponentJaggedUpdate> componentUpdates,
            ref NativeArray<MeshRendererProcessor.GPUComponentUploadSource> uploadSources,
            GPUComponentSet* overrideComponentSet)
        {
            Assert.IsTrue(componentUpdates.Length == componentUpdates.Length);

            GPUComponentSet componentSet = default;

            for (int i = 0; i < componentUpdates.Length; i++)
            {
                ref readonly GPUComponentJaggedUpdate update = ref componentUpdates.ElementAt(i);
                GPUComponentHandle component = archetypeManager.GetRef().GetOrCreateComponent(update.PropertyID, update.StrideInBytes, perInstance: true);
                componentSet.Add(component);

                MeshRendererProcessor.GPUComponentUploadSource source = default;
                source.component = component;
                source.data = update.Data;
                source.componentSize = update.StrideInBytes;

                uploadSources[i] = source;
            }

            *overrideComponentSet = componentSet;
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static bool AnyInstanceUseBlendProbes(in NativeArray<InstanceHandle> instances, ref RenderWorld renderWorld)
        {
            for (int i = 0; i < instances.Length; i++)
            {
                InstanceHandle instance = instances[i];
                Assert.IsTrue(instance.isValid);
                if (!instance.isValid)
                    continue;

                int instanceIndex = renderWorld.HandleToIndex(instance);
                if (renderWorld.rendererSettings[instanceIndex].LightProbeUsage == LightProbeUsage.BlendProbes)
                {
                    return true;
                }
            }

            return false;
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static unsafe bool DidGPUArchetypeChange(in NativeReference<GPUArchetypeManager> archetypeManager,
            in DefaultGPUComponents defaultGPUComponents,
            in NativeArray<InstanceHandle> instances,
            MeshRendererUpdateBatch* updateBatch,
            ref RenderWorld renderWorld,
            in GPUComponentSet overrideComponentSet)
        {
            int flatIndex = 0;
            for (int sectionIndex = 0; sectionIndex < updateBatch->SectionCount; sectionIndex++)
            {
                NativeArray<InternalMeshRendererSettings> rendererSettingsSection = updateBatch->GetRendererSettingsSectionOrDefault(sectionIndex);
                NativeArray<short> lightmapIndexSection = updateBatch->GetLightmapIndexSectionOrDefault(sectionIndex);

                for (int i = 0; i < updateBatch->GetSectionLength(sectionIndex); i++)
                {
                    InstanceHandle instance = instances[flatIndex];
                    int instanceIndex = renderWorld.HandleToIndex(instance);

                    InternalMeshRendererSettings oldRendererSettings = renderWorld.rendererSettings[instanceIndex];
                    int oldLightmapIndex = renderWorld.lightmapIndices[instanceIndex];

                    InternalMeshRendererSettings newRendererSettings = updateBatch->HasAnyComponent(MeshRendererComponentMask.RendererSettings)
                        ? rendererSettingsSection[i]
                        : oldRendererSettings;

                    int newLightmapIndex = updateBatch->HasAnyComponent(MeshRendererComponentMask.Lightmap)
                        ? lightmapIndexSection[i]
                        : oldLightmapIndex;

                    ulong oldBaseComponentMask = MeshRendererProcessor.ComputeComponentSet(defaultGPUComponents, oldRendererSettings, oldLightmapIndex).componentsMask;
                    ulong newBaseComponentMask = MeshRendererProcessor.ComputeComponentSet(defaultGPUComponents, newRendererSettings, newLightmapIndex).componentsMask;

                    // First compare the "base" component mask. So the component mask excluding the shader property overrides.
                    // Those two mask must match exactly, otherwise it means the update changed the GPU archetype.
                    if (oldBaseComponentMask != newBaseComponentMask)
                    {
                        return true;
                    }

                    // Then retrieve the full component mask, which includes the shader property overrides.
                    GPUArchetypeHandle currentArchetype = renderWorld.gpuHandles[instanceIndex].archetype;
                    ulong fullComponentMask = archetypeManager.GetRefRO().FindComponentSet(currentArchetype).componentsMask;
                    ulong overrideComponentMask = overrideComponentSet.componentsMask;

                    // For shader property overrides, it is only required that the components we want to update were already part of the archetype.
                    // It is possible to just update one shader property override even if the instance has many overrides for example.
                    if ((overrideComponentMask & fullComponentMask) != overrideComponentMask)
                    {
                        return true;
                    }

                    ++flatIndex;
                }
            }

            return false;
        }
    }
}
