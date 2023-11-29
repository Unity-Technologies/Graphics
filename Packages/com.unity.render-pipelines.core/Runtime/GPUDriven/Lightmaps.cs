using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Experimental.Rendering;
using Unity.Collections;
using UnityEngine.Assertions;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Profiling;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

namespace UnityEngine.Rendering
{
    internal class LightmapManager : IDisposable
    {
        [Flags]
        enum LightmappingFlags
        {
            None = 0,
            Lightmapped = 1,
            Directional = 2,
            ShadowMask = 4
        }

        private struct MaterialLookupKey : IEquatable<MaterialLookupKey>
        {
            public int baseMaterialID;
            public LightMapKey lmKey;
            public int textureArrayIndex;
            public LightmappingFlags flags;

            public override int GetHashCode()
            {
                int hash = baseMaterialID;
                hash = hash * 31 + textureArrayIndex;
                hash = hash * 31 + lmKey.resolution;
                hash = hash * 31 + lmKey.format;
                hash = hash * 31 + (int)flags;
                return hash;
            }

            public bool Equals(MaterialLookupKey other)
            {
                return baseMaterialID == other.baseMaterialID &&
                    textureArrayIndex == other.textureArrayIndex &&
                    lmKey.Equals(other.lmKey) &&
                    flags == other.flags;
            }
        }

        public struct RendererSubmeshPair : IEquatable<RendererSubmeshPair>
        {
            int rendererGroupID;
            int materialIndex;

            public RendererSubmeshPair(int inRendererGroupID, int submeshIndex)
            {
                rendererGroupID = inRendererGroupID;
                materialIndex = submeshIndex;
            }

            public override int GetHashCode()
            {
                int hash = rendererGroupID;
                return hash * 31 + materialIndex;
            }

            public bool Equals(RendererSubmeshPair other)
            {
                return (rendererGroupID == other.rendererGroupID &&
                    materialIndex == other.materialIndex);
            }
        }

        private LightmapData[] m_CachedLightmapData;

        private Dictionary<MaterialLookupKey, Material> m_LightmappedMaterialCache;
        // This map stores all child lightmapped material keys for a given base material id.
        private NativeParallelMultiHashMap<int, MaterialLookupKey> m_BaseMaterialIDToLightmappedMaterialsHash;

        struct LightMapKey : IEquatable<LightMapKey>
        {
            public int resolution;
            public int format;

            public bool Equals(LightMapKey other)
            {
                return (resolution == other.resolution &&
                        format == other.format);
            }
        }

        private Dictionary<LightMapKey, LightmapArray> m_Lightmaps;

        private NativeParallelHashMap<RendererSubmeshPair, int> m_RendererToMaterialMap;
        // For each lightmap (defined by instanceID), we store :
        // .x : lightmap Resolution (each resolution has it's own lightmap array)
        // .y : index in the lightmapArray for said resolution (stored in .y)
        // .z : format of the data in the lighting array
        private UnsafeParallelHashMap<int, Vector3Int> m_LightmapResolutionAndIndex;

        public LightmapManager()
        {
            m_LightmappedMaterialCache = new(128);
            m_BaseMaterialIDToLightmappedMaterialsHash = new NativeParallelMultiHashMap<int, MaterialLookupKey>(64, Allocator.Persistent);
            m_Lightmaps = new(1);
            m_RendererToMaterialMap = new NativeParallelHashMap<RendererSubmeshPair, int>(1024, Allocator.Persistent);
            m_LightmapResolutionAndIndex = new(16, Allocator.Persistent);

            RecreateLightmaps();
        }

        public void Dispose()
        {
            m_CachedLightmapData = null;

            foreach(var pair in m_LightmappedMaterialCache)
                CoreUtils.Destroy(pair.Value);
            m_LightmappedMaterialCache.Clear();
            m_LightmappedMaterialCache = null;

            m_BaseMaterialIDToLightmappedMaterialsHash.Dispose();

            foreach (var lightmapArrays in m_Lightmaps)
                lightmapArrays.Value.Dispose();

            m_LightmapResolutionAndIndex.Dispose();
            m_RendererToMaterialMap.Dispose();
        }

        public void GetLightmapTextureIndices(in GPUDrivenRendererGroupData rendererData, NativeArray<float4> lightMapTextureIndices)
        {
            //@ This should be converted to burst.
            //@ But m_Lightmaps references Unity.Object so we need to do something about that later.

            for (int i = 0; i < rendererData.rendererGroupID.Length; ++i)
            {
                int lightmapIndex = rendererData.lightmapIndex[i];

                int lightmapTextureIndex = 0;

                if (lightmapIndex >= 0 && lightmapIndex < m_CachedLightmapData.Length)
                {
                    var lightmapColorTexture = m_CachedLightmapData[lightmapIndex].lightmapColor;

                    if (lightmapColorTexture != null)
                    {
                        if (m_LightmapResolutionAndIndex.TryGetValue(lightmapColorTexture.GetInstanceID(), out var resAndIndex))
                        {
                            LightMapKey key;
                            key.resolution = resAndIndex.x;
                            key.format = resAndIndex.z;
                            lightmapTextureIndex = m_Lightmaps[key].GetTextureIndex(resAndIndex.y);
                        }
                    }
                }

                lightMapTextureIndices[i] = new float4(lightmapTextureIndex, 0.0f, 0.0f, 0.0f);
            }
        }

        public void UpdateMaterials(IList<Object> changed, NativeArray<int> changedID)
        {
            Assert.AreEqual(changed.Count, changedID.Length);

            if(changed.Count == 0)
                return;

            var baseMaterialIndices = new NativeList<int>(Allocator.TempJob);
            var lightmappedMaterialKeys = new NativeList<MaterialLookupKey>(Allocator.TempJob);

            new CollectLightmappedMaterialKeysJob
            {
                baseMaterialsID = changedID,
                baseMaterialIndices = baseMaterialIndices,
                lightmappedMaterialKeys = lightmappedMaterialKeys,
                baseMaterialIDToLightmappedMaterialsHash = m_BaseMaterialIDToLightmappedMaterialsHash
            }.Run();

            Assert.AreEqual(baseMaterialIndices.Length, lightmappedMaterialKeys.Length);

            for(int i = 0; i < baseMaterialIndices.Length; ++i)
            {
                int baseMaterialIndex = baseMaterialIndices[i];
                ref MaterialLookupKey lightmappedMaterialKey = ref lightmappedMaterialKeys.ElementAt(i);

                Material baseMaterial = (Material)changed[baseMaterialIndex];
                Material lightmappedMaterial = m_LightmappedMaterialCache[lightmappedMaterialKey];

                lightmappedMaterial.CopyPropertiesFromMaterial(baseMaterial);

                var lmArray = m_Lightmaps[lightmappedMaterialKey.lmKey];
                var colorTextureArray = lmArray.colorArrays[lightmappedMaterialKey.textureArrayIndex];

                Texture2DArray dirTextureArray = null;
                if (lmArray.directionArrays != null && lmArray.directionArrays.Count > 0)
                {
                    dirTextureArray = lmArray.directionArrays[lightmappedMaterialKey.textureArrayIndex];
                }

                Texture2DArray shadowMaskTextureArray = null;
                if (lmArray.shadowMaskArrays != null && lmArray.shadowMaskArrays.Count > 0)
                {
                    shadowMaskTextureArray = lmArray.shadowMaskArrays[lightmappedMaterialKey.textureArrayIndex];
                }

                SetMaterialLightmapProperties(lightmappedMaterial, colorTextureArray, dirTextureArray, shadowMaskTextureArray);
            }

            baseMaterialIndices.Dispose();
            lightmappedMaterialKeys.Dispose();
        }

        public void DestroyMaterials(NativeArray<int> destroyedBaseMaterialsID, NativeList<int> outDestroyedLightmappedMaterialsID)
        {
            if (destroyedBaseMaterialsID.Length == 0)
                return;

            var destroyedLightmappedMaterialKeys = new NativeList<MaterialLookupKey>(Allocator.TempJob);

            new CollectAndRemoveLightmappedMaterialKeysJob
            {
                destroyedBaseMaterialsID = destroyedBaseMaterialsID,
                destroyedLightmappedMaterialKeys = destroyedLightmappedMaterialKeys,
                baseMaterialIDToLightmappedMaterialsHash = m_BaseMaterialIDToLightmappedMaterialsHash
            }.Run();

            for (int i = 0; i < destroyedLightmappedMaterialKeys.Length; ++i)
            {
                ref MaterialLookupKey lightmappedMaterialKey = ref destroyedLightmappedMaterialKeys.ElementAt(i);
                Material lightmappedMaterial = m_LightmappedMaterialCache[lightmappedMaterialKey];
                m_LightmappedMaterialCache.Remove(lightmappedMaterialKey);
                outDestroyedLightmappedMaterialsID.Add(lightmappedMaterial.GetInstanceID());
                CoreUtils.Destroy(lightmappedMaterial);
            }

            destroyedLightmappedMaterialKeys.Dispose();
        }

        private Material GetOrCreateLightmappedMaterial(Material baseMaterial, LightMapKey lmKey, int textureIndex, NativeList<int> usedMaterials)
        {
            var flags = LightmappingFlags.Lightmapped;

            if (m_Lightmaps[lmKey].HasDirectionsAtIndex(textureIndex))
                flags |= LightmappingFlags.Directional;
            if (m_Lightmaps[lmKey].HasShadowMaskAtIndex(textureIndex))
                flags |= LightmappingFlags.ShadowMask;

            var key = new MaterialLookupKey
            {
                baseMaterialID = baseMaterial.GetInstanceID(),
                textureArrayIndex = m_Lightmaps[lmKey].GetArrayIndex(textureIndex),
                lmKey = lmKey,
                flags = flags
            };

            if (m_LightmappedMaterialCache.TryGetValue(key, out var lightmappedMaterial))
            {
                return lightmappedMaterial;
            }
            else
            {
                lightmappedMaterial = CreateLightmappedMaterial(baseMaterial, lmKey, textureIndex);
                m_LightmappedMaterialCache.Add(key, lightmappedMaterial);
                m_BaseMaterialIDToLightmappedMaterialsHash.Add(baseMaterial.GetInstanceID(), key);
                usedMaterials.Add(lightmappedMaterial.GetInstanceID());
                return lightmappedMaterial;
            }
        }

        private void SetMaterialLightmapProperties(Material lightmappedMaterial, Texture2DArray lightmapArray, Texture2DArray dirArray, Texture2DArray shadowMaskArray)
        {
            lightmappedMaterial.EnableKeyword("LIGHTMAP_ON");
            lightmappedMaterial.SetTexture("unity_Lightmaps", lightmapArray);

            if (dirArray != null)
            {
                lightmappedMaterial.EnableKeyword("DIRLIGHTMAP_COMBINED");
                lightmappedMaterial.SetTexture("unity_LightmapsInd", dirArray);
            }

            if (shadowMaskArray != null)
            {
                lightmappedMaterial.EnableKeyword("SHADOWS_SHADOWMASK");
                lightmappedMaterial.SetTexture("unity_ShadowMasks", shadowMaskArray);
            }
        }

        private Material CreateLightmappedMaterial(Material material, LightMapKey key, int textureIndex)
        {
            System.Text.StringBuilder nameSuffix = new("_Lightmapped", 40);

            var lightmapArray = m_Lightmaps[key].GetColorsAtIndex(textureIndex);

            Texture2DArray dirArray = null;
            if (m_Lightmaps[key].HasDirectionsAtIndex(textureIndex))
            {
                nameSuffix.Append("_DirLightmap");
                dirArray = m_Lightmaps[key].GetDirectionsAtIndex(textureIndex);
            }

            Texture2DArray shadowMaskArray = null;
            if (m_Lightmaps[key].HasShadowMaskAtIndex(textureIndex))
            {
                nameSuffix.Append("_ShadowMasked");
                shadowMaskArray = m_Lightmaps[key].GetshadowMasksAtIndex(textureIndex);
            }

            var lightmappedMaterial = new Material(material);
            lightmappedMaterial.hideFlags = HideFlags.HideAndDontSave;
            lightmappedMaterial.name += nameSuffix;

            SetMaterialLightmapProperties(lightmappedMaterial, lightmapArray, dirArray, shadowMaskArray);

            return lightmappedMaterial;
        }

        public void RecreateLightmaps()
        {
            Profiler.BeginSample("UpdateExistingLightmaps");

            m_CachedLightmapData = LightmapSettings.lightmaps;

            // building a list of lightmaps that needs to be added or kept to our arrays
            var lightmapCount = m_CachedLightmapData.Length;
            Dictionary<LightMapKey, HashSet<int>> lightmapsArrayIndexToKeep = new(8);
            Dictionary<LightMapKey, HashSet<int>> lightmapsToAdd = new(8);
            Dictionary<int, int> instanceIDToLightmapDataIndex = new(lightmapCount);
            UnsafeParallelHashMap<int,Vector3Int> newlightmapResAndIndex = new(lightmapCount, Allocator.Persistent);

            for (var i = 0; i < lightmapCount; i++ )
            {
                var lightmapData = m_CachedLightmapData[i];

                if (lightmapData.lightmapColor == null)
                    continue;

                int instanceID = lightmapData.lightmapColor.GetInstanceID();

                if(m_LightmapResolutionAndIndex.TryGetValue(instanceID, out var resAndIndex))
                {
                    var lightMapKey = new LightMapKey
                    {
                        resolution = resAndIndex.x,
                        format = resAndIndex.z
                    };

                    if(!lightmapsArrayIndexToKeep.TryGetValue(lightMapKey, out var hashSet))
                    {
                        hashSet = new HashSet<int>(lightmapCount);
                        lightmapsArrayIndexToKeep.Add(lightMapKey, hashSet);
                    }

                    hashSet.Add(resAndIndex.y);
                    newlightmapResAndIndex.Add(instanceID, resAndIndex);
                }
                else
                {
                    var lightMapKey = new LightMapKey
                    {
                        resolution = lightmapData.lightmapColor.width,
                        format = (int)lightmapData.lightmapColor.format
                    };

                    if (!lightmapsToAdd.TryGetValue(lightMapKey, out var hashSet))
                    {
                        hashSet = new HashSet<int>(lightmapCount);
                        lightmapsToAdd.Add(lightMapKey, hashSet);
                    }

                    hashSet.Add(instanceID);
                    instanceIDToLightmapDataIndex.TryAdd(instanceID, i);
                }
            }

            m_LightmapResolutionAndIndex.Dispose();
            m_LightmapResolutionAndIndex = newlightmapResAndIndex;

            // Clear lightmapArrays for unused resolutions
            // need to do a copy here to avoid modifying the m_lightmaps dictionary
            var keys = new List<LightMapKey>(m_Lightmaps.Keys);

            foreach (var lmKey in keys)
            {
                if (!lightmapsArrayIndexToKeep.ContainsKey(lmKey))
                {
                    // remove any cached lightmapped materials using this resolution
                    var destroyedKeys = new NativeList<MaterialLookupKey>(Allocator.TempJob);
                    foreach (KeyValuePair<MaterialLookupKey, Material> kv in m_LightmappedMaterialCache)
                    {
                        if (kv.Key.lmKey.Equals(lmKey))
                        {
                            CoreUtils.Destroy(kv.Value);
                            destroyedKeys.Add(kv.Key);
                        }
                    }
                    foreach (MaterialLookupKey key in destroyedKeys)
                    {
                        m_BaseMaterialIDToLightmappedMaterialsHash.Remove(key.baseMaterialID, key);
                        m_LightmappedMaterialCache.Remove(key);
                    }
                    destroyedKeys.Dispose();

                    m_Lightmaps.Remove(lmKey);
                }
            }

            // Remove lightmaps that are no longer used by current scene
            foreach(var lightmapSet in lightmapsArrayIndexToKeep)
            {
                var resolution = lightmapSet.Key;

                if (m_Lightmaps.TryGetValue(resolution, out var lightmapArray))
                    lightmapArray.FreeLightmapsNotInSet(lightmapSet.Value);

                Assert.IsNotNull(lightmapArray);
            }

            Profiler.EndSample();

            Profiler.BeginSample("CopyLightmaps");

            var slots = new NativeArray<int>(lightmapCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            foreach (var lightmapSet in lightmapsToAdd)
            {
                var resolution = lightmapSet.Key;
                var textureToAddCount = lightmapSet.Value.Count;

                if (!m_Lightmaps.TryGetValue(resolution, out var lightmapArray))
                {
                    lightmapArray = new LightmapArray();
                    m_Lightmaps.Add(resolution, lightmapArray);
                }

                lightmapArray.GetFreeSlots(textureToAddCount, slots);

                var i = 0;

                foreach(var instanceID in lightmapSet.Value)
                {
                    m_LightmapResolutionAndIndex.Add(instanceID, new Vector3Int(resolution.resolution, slots[i], resolution.format));
                    //Currently, we copy all textures at once, which can cause a hitch. We may want to consider moving this to whenever an object uses it.
                    lightmapArray.UploadSingleTexture(m_CachedLightmapData[instanceIDToLightmapDataIndex[instanceID]], slots[i]);
                    i++;
                }
            }

            Profiler.EndSample();
        }

        // build a map of renderer / submesh -> Material.
        // create a new material if lightmapping is needed
        public NativeParallelHashMap<RendererSubmeshPair, int> GenerateLightmappingData(in GPUDrivenRendererGroupData rendererData, IList<Material> materials, NativeList<int> usedMaterials)
        {
            Profiler.BeginSample("CreateLightmappingMaterials");

            m_RendererToMaterialMap.Clear();
            m_RendererToMaterialMap.Capacity = Mathf.Max(m_RendererToMaterialMap.Capacity, rendererData.rendererGroupID.Length);

            for (int j = 0; j < rendererData.rendererGroupID.Length; ++j)
            {
                var rendererGroupID = rendererData.rendererGroupID[j];
                var lightmapIndex = rendererData.lightmapIndex[j];

                if (lightmapIndex < 0 || lightmapIndex >= 65534)
                    continue;

                var lmColorTexture = m_CachedLightmapData[lightmapIndex].lightmapColor;

                if (lmColorTexture == null)
                    continue;

                // submesh
                for (var i = 0; i < rendererData.materialsCount[j]; ++i)
                {
                    Material material;
                    var resAndIndex = m_LightmapResolutionAndIndex[lmColorTexture.GetInstanceID()];
                    int materialIndex = rendererData.materialIndex[rendererData.materialsOffset[j] + i];
                    var sharedMaterial = materials[materialIndex];


                    var lightMapKey = new LightMapKey
                    {
                        resolution = resAndIndex.x,
                        format = resAndIndex.z
                    };

                    if (!m_Lightmaps[lightMapKey].isValid || sharedMaterial == null)
                        material = sharedMaterial;
                    else
                        material = GetOrCreateLightmappedMaterial(sharedMaterial, lightMapKey, resAndIndex.y, usedMaterials);

                    var pair = new RendererSubmeshPair(rendererGroupID, i);
                    m_RendererToMaterialMap.TryAdd(pair, material.GetInstanceID());
                }
            }

            Profiler.EndSample();

            return m_RendererToMaterialMap;
        }

        [BurstCompile]
        private struct CollectLightmappedMaterialKeysJob : IJob
        {
            [ReadOnly] public NativeArray<int> baseMaterialsID;
            [ReadOnly] public NativeParallelMultiHashMap<int, MaterialLookupKey> baseMaterialIDToLightmappedMaterialsHash;

            [WriteOnly] public NativeList<int> baseMaterialIndices;
            [WriteOnly] public NativeList<MaterialLookupKey> lightmappedMaterialKeys;

            public void Execute()
            {
                for (int i = 0; i < baseMaterialsID.Length; ++i)
                {
                    int baseMaterialID = baseMaterialsID[i];

                    foreach (var key in baseMaterialIDToLightmappedMaterialsHash.GetValuesForKey(baseMaterialID))
                    {
                        baseMaterialIndices.Add(i);
                        lightmappedMaterialKeys.Add(key);
                    }
                }
            }
        }

        [BurstCompile]
        private struct CollectAndRemoveLightmappedMaterialKeysJob : IJob
        {
            [ReadOnly] public NativeArray<int> destroyedBaseMaterialsID;
            [WriteOnly] public NativeList<MaterialLookupKey> destroyedLightmappedMaterialKeys;
            public NativeParallelMultiHashMap<int, MaterialLookupKey> baseMaterialIDToLightmappedMaterialsHash;

            public void Execute()
            {
                for (int i = 0; i < destroyedBaseMaterialsID.Length; ++i)
                {
                    int baseMaterialID = destroyedBaseMaterialsID[i];

                    foreach (var key in baseMaterialIDToLightmappedMaterialsHash.GetValuesForKey(baseMaterialID))
                        destroyedLightmappedMaterialKeys.Add(key);

                    baseMaterialIDToLightmappedMaterialsHash.Remove(baseMaterialID);
                }
            }
        }
    }

    [Serializable]
    internal class LightmapArray : IEquatable<LightmapArray>
    {
        private List<Texture2DArray> m_Colors;
        private List<Texture2DArray> m_Directions;
        private bool[] m_DirectionValid;
        private List<Texture2DArray> m_ShadowMasks;
        private bool[] m_ShadowMaskValid;
        private List<int> m_PerTextureArrayCount;
        private List<int> m_FreeSlots;
        private int m_UsedSlots;

        public bool HasDirectionsAtIndex(int index) { return isDirValid ? m_DirectionValid[index] : false; }
        public bool HasShadowMaskAtIndex(int index) { return isShadowMaskValid ? m_ShadowMaskValid[index] : false; }
        public bool isColorValid => m_Colors != null ? m_Colors.Count != 0 : false;
        public bool isDirValid => m_Directions != null ? m_Directions.Count != 0 : false;
        public bool isShadowMaskValid => m_ShadowMasks != null ? m_ShadowMasks.Count != 0 : false;
        public bool isFullyCreated => isColorValid && isDirValid && isShadowMaskValid;
        public bool isValid => isColorValid; // Lightmaps can be valid with just color

        public ReadOnlyCollection<Texture2DArray> colorArrays => m_Colors?.AsReadOnly();

        public ReadOnlyCollection<Texture2DArray> directionArrays => m_Directions?.AsReadOnly();

        public ReadOnlyCollection<Texture2DArray> shadowMaskArrays => m_ShadowMasks?.AsReadOnly();

        public LightmapArray()
        {
            m_FreeSlots = new(1);
            m_PerTextureArrayCount = new(1);
            m_Colors = null;
            m_Directions = null;
            m_DirectionValid = null;
            m_ShadowMasks = null;
            m_ShadowMaskValid = null;
            m_UsedSlots = 0;
        }

        private int GetCapacity()
        {
            var sum = 0;
            foreach(var textureArraySize in m_PerTextureArrayCount)
            {
                sum += textureArraySize;
            }
            return sum;
        }

        public int GetTextureIndex(int index)
        {
            var sum = 0;
            var arrayIndex = 0;
            do
            {
                sum += m_PerTextureArrayCount[arrayIndex++];
            } while (index >= sum);

            return index - (sum - m_PerTextureArrayCount[arrayIndex - 1]);
        }

        public void GetArrayAndTextureIndices(int index, out int arrayIndex, out int textureIndex)
        {
            var sum = 0;
            arrayIndex = 0;
            do
            {
                sum += m_PerTextureArrayCount[arrayIndex++];
            } while (index >= sum);

            arrayIndex = arrayIndex - 1;
            textureIndex = index - (sum - m_PerTextureArrayCount[arrayIndex]);
        }

        public int GetArrayIndex(int index)
        {
            int arrayIndex = 0;
            var sum = m_PerTextureArrayCount[0];
            while (index >= sum)
            {
                sum += m_PerTextureArrayCount[++arrayIndex];
            }

            return arrayIndex;
        }

        private void SetIndexInvalid(int textureIndex)
        {
            if(m_DirectionValid != null)
                m_DirectionValid[textureIndex] = false;
            if(m_ShadowMaskValid != null)
                m_ShadowMaskValid[textureIndex] = false;
        }

        public int GetFreeSlot()
        {
            var freeSlotCount = m_FreeSlots.Count;
            if (freeSlotCount != 0)
            {
                int freeSlot = m_FreeSlots[freeSlotCount - 1];
                m_FreeSlots.RemoveAt(freeSlotCount - 1);
                return freeSlot;
            }
            return m_UsedSlots++;
        }

        public void ReleaseSlot(int slot)
        {
            m_FreeSlots.Add(slot);
            SetIndexInvalid(slot);
        }

        public void FreeLightmapsNotInSet(HashSet<int> lightmapsInstanceID)
        {
            for(var slot = 0; slot < m_UsedSlots; slot++)
            {
                if (!lightmapsInstanceID.Contains(slot))
                {
                    m_FreeSlots.Add(slot);
                    SetIndexInvalid(slot);
                }
            }
        }

        public void GetFreeSlots(int slotCount, NativeArray<int> slots)
        {
            for (var i = 0; i < slotCount; i++)
            {
                slots[i] = GetFreeSlot();
            }
        }

        public void UploadSingleTexture(LightmapData lightmapData, int index)
        {
            Assert.IsTrue(index < m_UsedSlots);

            if (!isFullyCreated)
                CreateTextureArrays(lightmapData);

            GrowIfNeeded();

            Update(lightmapData, index);
        }

        private void CreateTextureArrays(LightmapData lightmapData)
        {
            if (!isColorValid)
            {
                // look for the first validTexture
                var colorData = lightmapData.lightmapColor;
                Debug.Assert(colorData != null, "Trying to create a lightmap with a null color texture");

                if (m_Colors == null)
                    m_Colors = new List<Texture2DArray>(1);

                m_PerTextureArrayCount.Add(m_UsedSlots);
                bool isSrgb = GraphicsFormatUtility.IsSRGBFormat(colorData.graphicsFormat);

                m_Colors.Add(new Texture2DArray(colorData.width, colorData.height, m_UsedSlots, colorData.format, true, !isSrgb, true)
                {
                    filterMode = colorData.filterMode,
                    wrapMode = colorData.wrapMode,
                    anisoLevel = colorData.anisoLevel,
                    mipMapBias = colorData.mipMapBias,
                    hideFlags = HideFlags.DontSave
                });
            }

            if (!isDirValid)
            {
                // look if there is any valid texture
                var directionData = lightmapData.lightmapDir;

                if (directionData != null)
                {
                    m_DirectionValid = new bool[m_PerTextureArrayCount[0]];

                    if (m_Directions == null)
                        m_Directions = new List<Texture2DArray>(1);

                    bool isSrgb = GraphicsFormatUtility.IsSRGBFormat(directionData.graphicsFormat);

                    m_Directions.Add(new Texture2DArray(directionData.width, directionData.height, m_PerTextureArrayCount[0], directionData.format, true, !isSrgb, true)
                    {
                        filterMode = directionData.filterMode,
                        wrapMode = directionData.wrapMode,
                        anisoLevel = directionData.anisoLevel,
                        mipMapBias = directionData.mipMapBias,
                        hideFlags = HideFlags.DontSave
                    });
                }
            }

            if (!isShadowMaskValid)
            {
                var shadowMaskData = lightmapData.shadowMask;

                if (shadowMaskData != null)
                {
                    m_ShadowMaskValid = new bool[m_PerTextureArrayCount[0]];

                    if (m_ShadowMasks == null)
                        m_ShadowMasks = new List<Texture2DArray>(1);

                    bool isSrgb = GraphicsFormatUtility.IsSRGBFormat(shadowMaskData.graphicsFormat);

                    m_ShadowMasks.Add(new Texture2DArray(shadowMaskData.width, shadowMaskData.height, m_PerTextureArrayCount[0], shadowMaskData.format, true, !isSrgb, true)
                    {
                        filterMode = shadowMaskData.filterMode,
                        wrapMode = shadowMaskData.wrapMode,
                        anisoLevel = shadowMaskData.anisoLevel,
                        mipMapBias = shadowMaskData.mipMapBias,
                        hideFlags = HideFlags.DontSave
                    });

                }
            }
        }

        public Texture2DArray GetDirectionsAtIndex(int index)
        {
            int arrayIndex = GetArrayIndex(index);
            Assert.IsTrue(m_Directions.Count > arrayIndex);
            return m_Directions[arrayIndex];
        }

        public Texture2DArray GetColorsAtIndex(int index)
        {
            int arrayIndex = GetArrayIndex(index);
            Assert.IsTrue(m_Colors.Count > arrayIndex);
            return m_Colors[arrayIndex];
        }

        public Texture2DArray GetshadowMasksAtIndex(int index)
        {
            int arrayIndex = GetArrayIndex(index);
            Assert.IsTrue(m_ShadowMasks.Count > arrayIndex);
            return m_ShadowMasks[arrayIndex];
        }

        private void IncreaseArrayCapacity(List<Texture2DArray> inOutArray, int newCapacity)
        {
            var first = inOutArray[0];
            bool isSrgb = GraphicsFormatUtility.IsSRGBFormat(first.graphicsFormat);

            inOutArray.Add(new Texture2DArray(first.width, first.height, newCapacity, first.format, true, !isSrgb, true)
            {
                filterMode = first.filterMode,
                wrapMode = first.wrapMode,
                anisoLevel = first.anisoLevel,
                mipMapBias = first.mipMapBias,
                hideFlags = HideFlags.DontSave
            });
        }

        private void Update(LightmapData lightmapData, int index)
        {
            GetArrayAndTextureIndices(index, out var textureArrayIndex, out var indexInTextureArray);

            Assert.IsTrue(m_Colors[0].width == lightmapData.lightmapColor.width, "Trying to upload a texture in the wrong resolution lightmapArray");
            Graphics.CopyTexture(lightmapData.lightmapColor, 0, m_Colors[textureArrayIndex], indexInTextureArray);

            if (lightmapData.lightmapDir != null)
            {
                Graphics.CopyTexture(lightmapData.lightmapDir, 0, m_Directions[textureArrayIndex], indexInTextureArray);
                m_DirectionValid[index] = true;
            }

            if (lightmapData.shadowMask != null)
            {
                Graphics.CopyTexture(lightmapData.shadowMask, 0, m_ShadowMasks[textureArrayIndex], indexInTextureArray);
                m_ShadowMaskValid[index] = true;
            }
        }

        private bool GrowIfNeeded()
        {
            Assert.IsTrue(isValid, "Trying to grow invalid Lightmaps. Ensure lightmap texture arrays have been created.");
            var requiredNewTextures = m_UsedSlots - GetCapacity();

            if (requiredNewTextures <= 0)
                return false;

            m_PerTextureArrayCount.Add(requiredNewTextures);

            IncreaseArrayCapacity(m_Colors, requiredNewTextures);

            if (isDirValid)
            {
                IncreaseArrayCapacity(m_Directions, requiredNewTextures);
                Array.Resize(ref m_DirectionValid, m_UsedSlots);
            }

            if(isShadowMaskValid)
            {
                IncreaseArrayCapacity(m_ShadowMasks, requiredNewTextures);
                Array.Resize(ref m_ShadowMaskValid, m_UsedSlots);
            }

            return true;
        }

        public bool Equals(LightmapArray other)
        {
            return m_Colors == other.m_Colors &&
                m_Directions == other.m_Directions &&
                m_ShadowMasks == other.m_ShadowMasks;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            if (!ReferenceEquals(m_Colors, null)) hash ^= m_Colors.GetHashCode();
            if (!ReferenceEquals(m_Directions, null)) hash ^= m_Directions.GetHashCode();
            if (!ReferenceEquals(m_ShadowMasks, null)) hash ^= m_ShadowMasks.GetHashCode();
            return hash;
        }

        public void Dispose()
        {
            if (m_Colors != null)
            {
                foreach (var color in m_Colors)
                    CoreUtils.Destroy(color);
                m_Colors.Clear();
            }

            if (m_Directions != null)
            {
                foreach (var direction in m_Directions)
                    CoreUtils.Destroy(direction);
                m_Directions.Clear();
                m_DirectionValid = null;
            }

            if (m_ShadowMasks != null)
            {
                foreach (var shadowMask in m_ShadowMasks)
                    CoreUtils.Destroy(shadowMask);
                m_ShadowMasks.Clear();
                m_ShadowMaskValid = null;
            }
        }
    }
}
