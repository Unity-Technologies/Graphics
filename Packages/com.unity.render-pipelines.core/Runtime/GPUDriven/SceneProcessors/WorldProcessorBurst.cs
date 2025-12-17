using Unity.Collections;
using Unity.Burst;

namespace UnityEngine.Rendering
{
    [BurstCompile]
    internal static class WorldProcessorBurst
    {
        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static void ClassifyMaterials(in NativeParallelHashMap<EntityId, GPUDrivenMaterial> materialMap,
            in NativeArray<EntityId> allChangedMaterials,
            in NativeArray<EntityId> allDestroyedMaterials,
            out NativeList<EntityId> supportedChangedMaterials,
            out NativeList<EntityId> unsupportedChangedMaterials,
            out NativeList<EntityId> destroyedMaterials,
            out NativeList<GPUDrivenMaterialData> supportedChangedMaterialDatas,
            Allocator allocator)
        {
            var usedChangedMaterials = new NativeList<EntityId>(16, Allocator.Temp);

            foreach (EntityId material in allChangedMaterials)
            {
                if (materialMap.ContainsKey(material))
                    usedChangedMaterials.Add(material);
            }

            supportedChangedMaterials = new NativeList<EntityId>(allChangedMaterials.Length, allocator);
            unsupportedChangedMaterials = new NativeList<EntityId>(allChangedMaterials.Length, allocator);
            supportedChangedMaterialDatas = new NativeList<GPUDrivenMaterialData>(allChangedMaterials.Length, allocator);

            if (!usedChangedMaterials.IsEmpty)
            {
                unsupportedChangedMaterials.Resize(usedChangedMaterials.Length, NativeArrayOptions.UninitializedMemory);
                supportedChangedMaterials.Resize(usedChangedMaterials.Length, NativeArrayOptions.UninitializedMemory);
                supportedChangedMaterialDatas.Resize(usedChangedMaterials.Length, NativeArrayOptions.UninitializedMemory);

                int unsupportedMaterialCount = GPUDrivenProcessor.ClassifyMaterials(usedChangedMaterials.AsArray(),
                    unsupportedChangedMaterials.AsArray(),
                    supportedChangedMaterials.AsArray(),
                    supportedChangedMaterialDatas.AsArray());

                unsupportedChangedMaterials.Resize(unsupportedMaterialCount, NativeArrayOptions.ClearMemory);
                supportedChangedMaterials.Resize(usedChangedMaterials.Length - unsupportedMaterialCount, NativeArrayOptions.ClearMemory);
                supportedChangedMaterialDatas.Resize(supportedChangedMaterials.Length, NativeArrayOptions.ClearMemory);
            }

            destroyedMaterials = new NativeList<EntityId>(allDestroyedMaterials.Length, allocator);

            foreach (var destroyedMaterial in allDestroyedMaterials)
            {
                // Unused material, don't add to the list
                if (!materialMap.ContainsKey(destroyedMaterial))
                    continue;

                // Edge case: If the material has been both changed and destroyed, we can't know for sure what should be done with it.
                // If it's in the supported list though, it means it still exists and it was possible to fetch material data.
                // So in this case assume it was changed and don't append to the destroy list.
                if (supportedChangedMaterials.Contains(destroyedMaterial))
                    continue;

                // If the material is unsupported don't also add to the destroy list.
                if (unsupportedChangedMaterials.Contains(destroyedMaterial))
                    continue;

                destroyedMaterials.Add(destroyedMaterial);
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static void FindOnlyUsedMeshes(in NativeParallelHashMap<EntityId, MeshInfo> meshMap,
            in NativeArray<EntityId> changedMeshes,
            Allocator allocator,
            out NativeList<EntityId> usedMeshes)
        {
            usedMeshes = new NativeList<EntityId>(16, allocator);

            foreach (EntityId mesh in changedMeshes)
            {
                if (meshMap.ContainsKey(mesh))
                    usedMeshes.Add(mesh);
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static void FindUnsupportedRenderers(in NativeArray<EntityId> unsupportedMaterials,
            in NativeArray<EmbeddedArray32<EntityId>> materialArrays,
            in NativeArray<EntityId> renderers,
            ref NativeList<EntityId> unsupportedRenderers)
        {
            for (int arrayIndex = 0; arrayIndex < materialArrays.Length; arrayIndex++)
            {
                EmbeddedArray32<EntityId> materials = materialArrays[arrayIndex];
                EntityId renderer = renderers[arrayIndex];

                for (int i = 0; i < materials.Length; i++)
                {
                    EntityId material = materials[i];

                    if (unsupportedMaterials.Contains(material))
                    {
                        unsupportedRenderers.Add(renderer);
                        break;
                    }
                }
            }
        }
    }
}
