using Unity.Collections;
using UnityEngine.Rendering;
using Unity.Burst;

namespace UnityEngine.Rendering
{
    [BurstCompile]
    internal static class GPUResidentDrawerBurst
    {
        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static void ClassifyMaterials(in NativeArray<int> materialIDs, in NativeParallelHashMap<int, BatchMaterialID>.ReadOnly batchMaterialHash,
                                             ref NativeList<int> supportedMaterialIDs, ref NativeList<int> unsupportedMaterialIDs, ref NativeList<GPUDrivenPackedMaterialData> supportedPackedMaterialDatas)
        {
            var usedMaterialIDs = new NativeList<int>(4, Allocator.Temp);

            foreach (var materialID in materialIDs)
            {
                if (batchMaterialHash.ContainsKey(materialID))
                    usedMaterialIDs.Add(materialID);
            }

            if (usedMaterialIDs.IsEmpty)
            {
                usedMaterialIDs.Dispose();
                return;
            }

            unsupportedMaterialIDs.Resize(usedMaterialIDs.Length, NativeArrayOptions.UninitializedMemory);
            supportedMaterialIDs.Resize(usedMaterialIDs.Length, NativeArrayOptions.UninitializedMemory);
            supportedPackedMaterialDatas.Resize(usedMaterialIDs.Length, NativeArrayOptions.UninitializedMemory);

            int unsupportedMaterialCount = GPUDrivenProcessor.ClassifyMaterials(usedMaterialIDs.AsArray(), unsupportedMaterialIDs.AsArray(), supportedMaterialIDs.AsArray(), supportedPackedMaterialDatas.AsArray());

            unsupportedMaterialIDs.Resize(unsupportedMaterialCount, NativeArrayOptions.ClearMemory);
            supportedMaterialIDs.Resize(usedMaterialIDs.Length - unsupportedMaterialCount, NativeArrayOptions.ClearMemory);
            supportedPackedMaterialDatas.Resize(supportedMaterialIDs.Length, NativeArrayOptions.ClearMemory);

            usedMaterialIDs.Dispose();
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static void FindUnsupportedRenderers(in NativeArray<int> unsupportedMaterials, in NativeArray<SmallIntegerArray>.ReadOnly materialIDArrays, in NativeArray<int>.ReadOnly rendererGroups,
                                                    ref NativeList<int> unsupportedRenderers)
        {
            for (int arrayIndex = 0; arrayIndex < materialIDArrays.Length; arrayIndex++)
            {
                var materialIDs = materialIDArrays[arrayIndex];
                int rendererID = rendererGroups[arrayIndex];

                for (int i = 0; i < materialIDs.Length; i++)
                {
                    int materialID = materialIDs[i];

                    if (unsupportedMaterials.Contains(materialID))
                    {
                        unsupportedRenderers.Add(rendererID);
                        break;
                    }
                }
            }
        }

        [BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
        public static void GetMaterialsWithChangedPackedMaterial(in NativeArray<int> materialIDs, in NativeArray<GPUDrivenPackedMaterialData> packedMaterialDatas,
            in NativeParallelHashMap<int, GPUDrivenPackedMaterialData>.ReadOnly packedMaterialHash, ref NativeHashSet<int> filteredMaterials)
        {
            for (int index = 0; index < materialIDs.Length ; index++)
            {
                var materialID = materialIDs[index];
                var newPackedMaterialData = packedMaterialDatas[index];

                // Has its packed material changed? If the material isn't in the packed material cache, consider the material has changed.
                if (packedMaterialHash.TryGetValue(materialID, out var packedMaterial) && packedMaterial.Equals(newPackedMaterialData))
                    continue;

                filteredMaterials.Add(materialID);
            }
        }
    }
}
