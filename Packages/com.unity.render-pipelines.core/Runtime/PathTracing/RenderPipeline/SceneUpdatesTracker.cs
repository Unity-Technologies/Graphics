#if SURFACE_CACHE || ENABLE_PATH_TRACING_SRP

using System;
using System.Collections.Generic;
using ObjectDispatcher = UnityEngine.InternalBridge.ObjectDispatcher;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.LiveGI
{
    internal class SceneChanges
    {
        public List<MeshRenderer> addedMeshRenderers;
        public List<MeshRendererChanges> changedMeshRenderers;
        public List<EntityId> removedMeshRenderers;

#if ENABLE_TERRAIN_MODULE
        public List<Terrain> addedTerrains;
        public List<TerrainChanges> changedTerrains;
        public List<EntityId> removedTerrains;

        public List<TerrainData> addedTerrainData;
        public List<TerrainDataChanges> changedTerrainData;
        public List<EntityId> removedTerrainData;
#endif

        public List<Material> addedMaterials;
        public List<EntityId> removedMaterials;
        public List<Material> changedMaterials;

        public List<Light> addedLights;
        public List<Light> changedLights;
        public List<EntityId> removedLights;

        public SceneChanges()
        {
            addedMeshRenderers = new List<MeshRenderer>();
            changedMeshRenderers = new List<MeshRendererChanges>();
            removedMeshRenderers = new List<EntityId>();

#if ENABLE_TERRAIN_MODULE
            addedTerrains = new List<Terrain>();
            changedTerrains = new List<TerrainChanges>();
            removedTerrains = new List<EntityId>();

            addedTerrainData = new List<TerrainData>();
            changedTerrainData = new List<TerrainDataChanges>();
            removedTerrainData = new List<EntityId>();
#endif

            addedMaterials = new List<Material>();
            removedMaterials = new List<EntityId>();
            changedMaterials = new List<Material>();

            addedLights = new List<Light>();
            changedLights = new List<Light>();
            removedLights = new List<EntityId>();
        }

        public bool HasChanges()
        {
            return (addedMeshRenderers.Count | removedMeshRenderers.Count | changedMeshRenderers.Count
#if ENABLE_TERRAIN_MODULE
                | addedTerrains.Count | changedTerrains.Count | removedTerrains.Count
                | addedTerrainData.Count | changedTerrainData.Count | removedTerrainData.Count
#endif
                | addedMaterials.Count | removedMaterials.Count | changedMaterials.Count
                | addedLights.Count | removedLights.Count | changedLights.Count) != 0;
        }

        public void Clear()
        {
            addedMeshRenderers.Clear();
            removedMeshRenderers.Clear();
            changedMeshRenderers.Clear();

#if ENABLE_TERRAIN_MODULE
            addedTerrains.Clear();
            removedTerrains.Clear();
            changedTerrains.Clear();

            addedTerrainData.Clear();
            removedTerrainData.Clear();
            changedTerrainData.Clear();
#endif

            addedMaterials.Clear();
            removedMaterials.Clear();
            changedMaterials.Clear();

            addedLights.Clear();
            removedLights.Clear();
            changedLights.Clear();
        }
    }

    [System.Flags]
    internal enum ModifiedProperties { Transform = 1, Material = 2, IsStatic = 4, ShadowCasting = 8, Layer = 16, Heightmap = 32, TreeInstances = 64, Holes = 128 }

    internal struct MeshRendererChanges
    {
        public MeshRenderer meshRenderer;
        public ModifiedProperties changes;
    }

#if ENABLE_TERRAIN_MODULE
    internal struct TerrainChanges
    {
        public Terrain terrain;
        public ModifiedProperties changes;
    }

    internal struct TerrainDataChanges
    {
        public TerrainData terrainData;
        public ModifiedProperties changes;
    }
#endif

    internal class SceneUpdatesTracker : IDisposable
    {
        struct Timestamp
        {
            public uint lastVisit;
            public uint creation;
        }

        class MaterialData
        {
            public MaterialData(Material material, uint timestamp)
            {
                this.timestamp.creation = timestamp;
                this.timestamp.lastVisit = timestamp;
                this.material = material;
                metaPassIndex = material.FindPass("Meta");
#if UNITY_EDITOR
                shaderCompiled = metaPassIndex != -1 ? ShaderUtil.IsPassCompiled(material, metaPassIndex) : true;
#endif
            }

            public Material material;
            public int metaPassIndex;
#if UNITY_EDITOR
            public bool shaderCompiled;
#endif
            public Timestamp timestamp;
        }

        class MeshRendererDescriptor
        {
            public Timestamp timestamp;
            public EntityId[] materialIDs;
            public Material[] materials;
            public MeshRenderer renderer;
            public bool isStatic;
            public ShadowCastingMode shadowCastingMode;
        }

#if ENABLE_TERRAIN_MODULE
        class TerrainDescriptor
        {
            public Timestamp timestamp;
            public EntityId materialID;
            public Material material;
            public Terrain terrain;
            public bool isStatic;
            public ShadowCastingMode shadowCastingMode;
        }

        class TerrainDataDescriptor
        {
            public Timestamp timestamp;
            public TerrainData terrainData;
            public int treeInstanceCount;
            public Hash128 heightmapImageContentsHash;
            public Hash128 holesContentsHash;
        }
#endif

        class LightDescriptor
        {
            public Light light;
            public Timestamp timestamp;
        }

        ObjectDispatcher m_ObjectDispatcher;
        Dictionary<EntityId, MeshRendererDescriptor> m_MeshRenderers;
#if ENABLE_TERRAIN_MODULE
        Dictionary<EntityId, TerrainDescriptor> m_Terrains;
        Dictionary<EntityId, TerrainDataDescriptor> m_TerrainData;
#endif
        Dictionary<EntityId, MaterialData> m_Materials;
        Dictionary<EntityId, LightDescriptor> m_Lights;
        SceneChanges m_Changes;
        uint m_Timestamp;

        public SceneUpdatesTracker()
        {
            m_Changes = new SceneChanges();
            m_MeshRenderers = new Dictionary<EntityId, MeshRendererDescriptor>();
#if ENABLE_TERRAIN_MODULE
            m_Terrains = new Dictionary<EntityId, TerrainDescriptor>();
            m_TerrainData = new Dictionary<EntityId, TerrainDataDescriptor>();
#endif
            m_Materials = new Dictionary<EntityId, MaterialData>();
            m_Lights = new Dictionary<EntityId, LightDescriptor>();

            m_ObjectDispatcher = new();

#if UNITY_EDITOR
            m_ObjectDispatcher.maxDispatchHistoryFramesCount = int.MaxValue;
#endif
            m_ObjectDispatcher.EnableTypeTracking<MeshRenderer>(ObjectDispatcher.TypeTrackingFlags.SceneObjects);
            m_ObjectDispatcher.EnableTransformTracking<MeshRenderer>(ObjectDispatcher.TransformTrackingType.GlobalTRS);
#if ENABLE_TERRAIN_MODULE
            m_ObjectDispatcher.EnableTypeTracking<Terrain>(ObjectDispatcher.TypeTrackingFlags.SceneObjects);
            m_ObjectDispatcher.EnableTransformTracking<Terrain>(ObjectDispatcher.TransformTrackingType.GlobalTRS);
            m_ObjectDispatcher.EnableTypeTracking<TerrainData>(ObjectDispatcher.TypeTrackingFlags.SceneObjects | ObjectDispatcher.TypeTrackingFlags.Assets);
#endif
            m_ObjectDispatcher.EnableTypeTracking<Material>(ObjectDispatcher.TypeTrackingFlags.SceneObjects | ObjectDispatcher.TypeTrackingFlags.Assets);
            m_ObjectDispatcher.EnableTypeTracking<Light>(ObjectDispatcher.TypeTrackingFlags.SceneObjects);
            m_ObjectDispatcher.EnableTransformTracking<Light>(ObjectDispatcher.TransformTrackingType.GlobalTRS);
        }

        public void Dispose()
        {
            m_ObjectDispatcher.Dispose();
            m_Changes.Clear();
        }

        public SceneChanges GetChanges(bool filterBakedLights)
        {
            m_Timestamp++;
            m_Changes.Clear();

            FindMeshRendererChanges();
#if ENABLE_TERRAIN_MODULE
            FindTerrainChanges();
            FindTerrainDataChanges();
#endif
            FindMaterialsChanges();
            FindLightChanges(filterBakedLights);

            return m_Changes;
        }

        private void FindMaterialsChanges()
        {
            using var materialChanges = m_ObjectDispatcher.GetTypeChangesAndClear<Material>(Unity.Collections.Allocator.Temp);

            // Handle added materials in mesh renderers
            foreach (var meshRenderer in m_MeshRenderers.Values)
            {
                foreach (var material in meshRenderer.materials)
                {
                    MaterialData data = null;
                    if (material == null)
                        continue;
                    if (m_Materials.TryGetValue(material.GetEntityId(), out data))
                    {
                        data.timestamp.lastVisit = m_Timestamp;
                    }
                    else
                    {
                        m_Changes.addedMaterials.Add(material);
                        m_Materials.Add(material.GetEntityId(), new MaterialData(material, m_Timestamp));
                    }
                }
            }

#if ENABLE_TERRAIN_MODULE
            // Handle added materials in terrains
            foreach (var terrain in m_Terrains.Values)
            {
                var material = terrain.material;
                MaterialData data = null;
                if (material == null)
                    continue;
                if (m_Materials.TryGetValue(material.GetEntityId(), out data))
                {
                    data.timestamp.lastVisit = m_Timestamp;
                }
                else
                {
                    m_Changes.addedMaterials.Add(material);
                    m_Materials.Add(material.GetEntityId(), new MaterialData(material, m_Timestamp));
                }
            }
#endif

            var justCompiledMaterials = new HashSet<Material>();

            // Handle removed and uncompiled materials
            foreach (var materialKeyValue in m_Materials)
            {
                var materialID = materialKeyValue.Key;
                var matData = materialKeyValue.Value;

                if (matData.timestamp.lastVisit != m_Timestamp)
                    m_Changes.removedMaterials.Add(materialID);
#if UNITY_EDITOR
                else if (!matData.shaderCompiled)
                {
                    if (ShaderUtil.IsPassCompiled(matData.material, matData.metaPassIndex))
                    {
                        matData.shaderCompiled = true;
                        justCompiledMaterials.Add(matData.material);
                    }
                }
#endif
            }

            foreach (var key in m_Changes.removedMaterials)
                m_Materials.Remove(key);

            // Handle changed materials
            foreach (Material changedMaterial in materialChanges.changed)
            {
                if (!m_Materials.TryGetValue(changedMaterial.GetEntityId(), out var data))
                    continue;

                if (data.timestamp.creation == m_Timestamp) // if this is an instance that has just been added
                    continue;

                m_Changes.changedMaterials.Add(changedMaterial);
            }

#if UNITY_EDITOR
            if (justCompiledMaterials.Count != 0)
            {
                foreach (var m in m_Changes.changedMaterials)
                    justCompiledMaterials.Add(m);

                m_Changes.changedMaterials = new List<Material>(justCompiledMaterials);
            }
#endif
        }

        private static bool IntArraySequenceEqual(EntityId[] firstArray, EntityId[] secondArray) => ((ReadOnlySpan<EntityId>)firstArray).SequenceEqual(secondArray);

        private void FindMeshRendererChanges()
        {
            // Handle changed mesh renderers
            using var meshRendererChanges = m_ObjectDispatcher.GetTypeChangesAndClear<MeshRenderer>(Unity.Collections.Allocator.Temp);
            var transformChanges = m_ObjectDispatcher.GetTransformChangesAndClear<MeshRenderer>(ObjectDispatcher.TransformTrackingType.GlobalTRS, false);
            var changedRenderers = MergeChanges<MeshRenderer>(meshRendererChanges.changed, transformChanges);

            // Handle removed mesh renderers
            foreach (var key in meshRendererChanges.destroyedID)
            {
                m_Changes.removedMeshRenderers.Add(key);
            }

            // Update the remaining timestamps of the active mesh renderers
            List<EntityId> keys = new List<EntityId>(m_MeshRenderers.Keys);
            foreach (var key in keys)
            {
                if (!m_MeshRenderers[key].renderer)
                    continue;

                if (m_MeshRenderers[key].renderer.enabled && m_MeshRenderers[key].renderer.gameObject.activeInHierarchy)
                {
                    m_MeshRenderers[key].timestamp.lastVisit = m_Timestamp;
                }
                else
                {
                    m_Changes.removedMeshRenderers.Add(key);
                }
            }

            foreach (var key in m_Changes.removedMeshRenderers)
                m_MeshRenderers.Remove(key);


            foreach (var item in changedRenderers)
            {
                var meshRenderer = item.Value.objectReference;
                bool transformChanged = item.Value.transformChanged;

                if (!meshRenderer.enabled || !meshRenderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!m_MeshRenderers.TryGetValue(meshRenderer.GetEntityId(), out var oldData))
                {
                    // This renderer was just added
                    var newData = CreateMeshRendererDescriptor(m_Timestamp, meshRenderer);
                    m_MeshRenderers.Add(meshRenderer.GetEntityId(), newData);
                    m_Changes.addedMeshRenderers.Add(meshRenderer);
                    continue;
                }

                var data = CreateMeshRendererDescriptor(m_Timestamp, meshRenderer);

                ModifiedProperties changes = 0;

                if (transformChanged)
                    changes |= ModifiedProperties.Transform;

                if (!IntArraySequenceEqual(oldData.materialIDs, data.materialIDs))
                    changes |= ModifiedProperties.Material;

                if (oldData.isStatic != data.isStatic)
                    changes |= ModifiedProperties.IsStatic;

                if (oldData.shadowCastingMode != data.shadowCastingMode)
                    changes |= ModifiedProperties.ShadowCasting;

                if (changes != 0)
                {
                    m_Changes.changedMeshRenderers.Add(new MeshRendererChanges()
                    {
                        changes = changes,
                        meshRenderer = meshRenderer
                    });

                    m_MeshRenderers[meshRenderer.GetEntityId()] = data;
                }
            }
        }

        static MeshRendererDescriptor CreateMeshRendererDescriptor(uint timestamp, MeshRenderer meshRenderer)
        {
            return new MeshRendererDescriptor()
            {
                timestamp = new Timestamp { lastVisit = timestamp, creation = timestamp },
                isStatic = meshRenderer.gameObject.isStatic,
                materials = meshRenderer.sharedMaterials,
                materialIDs = Array.ConvertAll(meshRenderer.sharedMaterials, mat => mat != null ? mat.GetEntityId() : EntityId.None),
                shadowCastingMode = meshRenderer.shadowCastingMode,
                renderer = meshRenderer,
            };
        }

#if ENABLE_TERRAIN_MODULE
        private void FindTerrainChanges()
        {
            // Handle changed terrains
            using var terrainChanges = m_ObjectDispatcher.GetTypeChangesAndClear<Terrain>(Unity.Collections.Allocator.Temp);
            var terrainTransformChanges = m_ObjectDispatcher.GetTransformChangesAndClear<Terrain>(ObjectDispatcher.TransformTrackingType.GlobalTRS, false);
            var changedTerrains = MergeChanges<Terrain>(terrainChanges.changed, terrainTransformChanges);

            // Handle removed terrains
            foreach (var key in terrainChanges.destroyedID)
            {
                m_Changes.removedTerrains.Add(key);
            }

            // Update the remaining timestamps of the active terrains
            List<EntityId> keys = new List<EntityId>(m_Terrains.Keys);
            foreach (var key in keys)
            {
                if (!m_Terrains[key].terrain)
                    continue;

                if (m_Terrains[key].terrain.enabled && m_Terrains[key].terrain.gameObject.activeInHierarchy)
                {
                    m_Terrains[key].timestamp.lastVisit = m_Timestamp;
                }
                else
                {
                    m_Changes.removedTerrains.Add(key);
                }
            }

            foreach (var key in m_Changes.removedTerrains)
                m_Terrains.Remove(key);

            foreach (var item in changedTerrains)
            {
                var terrain = item.Value.objectReference;
                bool transformChanged = item.Value.transformChanged;

                if (!terrain.enabled || !terrain.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!m_Terrains.TryGetValue(terrain.GetEntityId(), out var oldData))
                {
                    // This terrain was just added
                    var newData = CreateTerrainDescriptor(m_Timestamp, terrain);
                    m_Terrains.Add(terrain.GetEntityId(), newData);
                    m_Changes.addedTerrains.Add(terrain);
                    continue;
                }

                var data = CreateTerrainDescriptor(m_Timestamp, terrain);

                ModifiedProperties changes = 0;

                if (transformChanged)
                    changes |= ModifiedProperties.Transform;

                if (oldData.materialID != data.materialID)
                    changes |= ModifiedProperties.Material;

                if (oldData.isStatic != data.isStatic)
                    changes |= ModifiedProperties.IsStatic;

                if (oldData.shadowCastingMode != data.shadowCastingMode)
                    changes |= ModifiedProperties.ShadowCasting;

                if (changes != 0)
                {
                    m_Changes.changedTerrains.Add(new TerrainChanges()
                    {
                        changes = changes,
                        terrain = terrain
                    });

                    m_Terrains[terrain.GetEntityId()] = data;
                }
            }
        }
        static TerrainDescriptor CreateTerrainDescriptor(uint timestamp, Terrain terrain)
        {
            return new TerrainDescriptor()
            {
                timestamp = new Timestamp { lastVisit = timestamp, creation = timestamp },
                isStatic = terrain.gameObject.isStatic,
                material = terrain.splatBaseMaterial,
                materialID = terrain.splatBaseMaterial != null ? terrain.splatBaseMaterial.GetEntityId() : EntityId.None,
                shadowCastingMode = terrain.shadowCastingMode,
                terrain = terrain,
            };
        }

        private void FindTerrainDataChanges()
        {
            var terrainDataChanges = m_ObjectDispatcher.GetTypeChangesAndClear<TerrainData>(Unity.Collections.Allocator.Temp);

            // Handle removed terrain data
            foreach (var key in terrainDataChanges.destroyedID)
            {
                m_TerrainData.Remove(key);
                m_Changes.removedTerrainData.Add(key);
            }

            // Update the remaining timestamps of the active terrain data
            List<EntityId> keys = new List<EntityId>(m_TerrainData.Keys);
            foreach (var key in keys)
            {
                if (!m_TerrainData[key].terrainData)
                    continue;

                m_TerrainData[key].timestamp.lastVisit = m_Timestamp;
            }

            foreach (var obj in terrainDataChanges.changed)
            {
                var terrainData = obj as TerrainData;
                if (terrainData == null)
                    continue;

                if (!m_TerrainData.TryGetValue(terrainData.GetEntityId(), out var oldData))
                {
                    // This terrain data was just added
                    var newData = CreateTerrainDataDescriptor(m_Timestamp, terrainData);
                    m_TerrainData.Add(terrainData.GetEntityId(), newData);
                    m_Changes.addedTerrainData.Add(terrainData);
                    continue;
                }

                var data = CreateTerrainDataDescriptor(m_Timestamp, terrainData);

                ModifiedProperties changes = 0;

                if (oldData.treeInstanceCount != data.treeInstanceCount)
                    changes |= ModifiedProperties.TreeInstances;

                if (oldData.heightmapImageContentsHash != data.heightmapImageContentsHash)
                    changes |= ModifiedProperties.Heightmap;

                if (oldData.holesContentsHash != data.holesContentsHash)
                    changes |= ModifiedProperties.Holes;

                if (changes != 0)
                {
                    m_Changes.changedTerrainData.Add(new TerrainDataChanges()
                    {
                        terrainData = terrainData,
                        changes = changes
                    });

                    m_TerrainData[terrainData.GetEntityId()] = data;
                }

                // When holes change, the terrain material may have changed, so we need to refresh the terrain descriptors
                if ((changes & ModifiedProperties.Holes) != 0)
                    RefreshTerrainDescriptorsForTerrainData(terrainData);
            }
        }

        static TerrainDataDescriptor CreateTerrainDataDescriptor(uint timestamp, TerrainData terrainData)
        {
            return new TerrainDataDescriptor()
            {
                timestamp = new Timestamp { lastVisit = timestamp, creation = timestamp },
                terrainData = terrainData,
                treeInstanceCount = terrainData.treeInstanceCount,
                heightmapImageContentsHash = terrainData.heightmapTexture != null ? GetTerrainDataHeightmapHash(terrainData) : new Hash128(),
                holesContentsHash = terrainData.holesTexture != null ?  GetTerrainDataHolesHash(terrainData) : new Hash128()
            };
        }

        static void RefreshTerrainDescriptorAndEmitChanges(
            Terrain terrain,
            Dictionary<EntityId, TerrainDescriptor> terrains,
            List<TerrainChanges> changedTerrains,
            Dictionary<EntityId, TerrainDataDescriptor> terrainData,
            uint timestamp)
        {
            var entityId = terrain.GetEntityId();
            Debug.Assert(terrains.ContainsKey(entityId));
            var oldDescriptor = terrains[entityId];

            var newDescriptor = CreateTerrainDescriptor(timestamp, terrain);
            newDescriptor.timestamp = oldDescriptor.timestamp;

            if (oldDescriptor.materialID != newDescriptor.materialID)
            {
                changedTerrains.Add(new TerrainChanges()
                {
                    changes = ModifiedProperties.Material,
                    terrain = terrain
                });
            }

            terrains[entityId] = newDescriptor;
        }

        void RefreshTerrainDescriptorsForTerrainData(TerrainData terrainData)
        {
            var terrainsToRefresh = new List<Terrain>();
            foreach (var kvp in m_Terrains)
            {
                var descriptor = kvp.Value;
                if (descriptor.terrain != null && descriptor.terrain.terrainData == terrainData)
                    terrainsToRefresh.Add(descriptor.terrain);
            }

            foreach (var terrain in terrainsToRefresh)
                RefreshTerrainDescriptorAndEmitChanges(terrain, m_Terrains, m_Changes.changedTerrains, m_TerrainData, m_Timestamp);
        }
#endif

        static private bool ShouldIncludeLight(Light light, bool filterBakedLights)
        {
            return light.enabled &&
                   light.gameObject.activeInHierarchy &&
                   !(filterBakedLights && light.bakingOutput.isBaked);
        }

        private void FindLightChanges(bool filterBakedLights)
        {
            // Handle changed lights
            using var lightChanges = m_ObjectDispatcher.GetTypeChangesAndClear<Light>(Unity.Collections.Allocator.Temp);
            var lightTransformChanges = m_ObjectDispatcher.GetTransformChangesAndClear<Light>(ObjectDispatcher.TransformTrackingType.GlobalTRS, false);
            var changedLights = MergeChanges<Light>(lightChanges.changed, lightTransformChanges);

            // Handle removed lights
            foreach (var key in lightChanges.destroyedID)
            {
                if (m_Lights.ContainsKey(key))
                    m_Changes.removedLights.Add(key);
            }

            // Update the remaining timestamps of the active lights
            List<EntityId> keys = new List<EntityId>(m_Lights.Keys);
            foreach (var key in keys)
            {
                if (!m_Lights[key].light)
                    continue;

                if (ShouldIncludeLight(m_Lights[key].light, filterBakedLights))
                    m_Lights[key].timestamp.lastVisit = m_Timestamp;
                else
                    m_Changes.removedLights.Add(key);
            }

            foreach (var key in m_Changes.removedLights)
                m_Lights.Remove(key);

            foreach (var item in changedLights)
            {
                var light = item.Value.objectReference;
                if (!ShouldIncludeLight(light, filterBakedLights))
                    continue;

                var newData = CreateLightDescriptor(m_Timestamp, light);

                // Newly added lights
                if (!m_Lights.ContainsKey(light.GetEntityId()))
                {
                    m_Lights.Add(light.GetEntityId(), newData);
                    m_Changes.addedLights.Add(light);
                    continue;
                }

                // Changed lights
                m_Changes.changedLights.Add(light);
                m_Lights[light.GetEntityId()] = newData;
            }
        }

        static LightDescriptor CreateLightDescriptor(uint timestamp, Light light)
        {
            return new LightDescriptor()
            {
                timestamp = new Timestamp { lastVisit = timestamp, creation = timestamp },
                light = light,
            };
        }

#if ENABLE_TERRAIN_MODULE
        static Hash128 GetTerrainDataHeightmapHash(TerrainData terrainData)
        {
            Hash128 hash = new Hash128();

            var resolution = terrainData.heightmapResolution;
            if (resolution == 0)
                return hash;

            var heights = terrainData.GetHeights(0, 0, resolution, resolution);
            for (var y = 0; y < resolution; y++)
            {
                var row = new float[resolution];
                for (var x = 0; x < resolution; x++)
                {
                    row[x] = heights[y, x];
                }
                hash.Append(row);
            }

            var scale = terrainData.heightmapScale;
            hash.Append(scale.x);
            hash.Append(scale.y);
            hash.Append(scale.z);

            return hash;
        }

        static Hash128 GetTerrainDataHolesHash(TerrainData terrainData)
        {
            Hash128 hash = new Hash128();

            var holesResolution = terrainData.holesResolution;
            if (holesResolution == 0)
                return hash;

            var holes = terrainData.GetHoles(0, 0, holesResolution, holesResolution);
            for (var y = 0; y < holesResolution; y++)
            {
                var row = new byte[holesResolution];
                for (var x = 0; x < holesResolution; x++)
                {
                    row[x] = holes[y, x] ? (byte)1 : (byte)0;
                }
                hash.Append(row);
            }
            return hash;
        }
#endif

        struct ChangedObject<T>
            where T : Component
        {
            public T objectReference;
            public bool transformChanged;
        }

        static Dictionary<EntityId, ChangedObject<T>> MergeChanges<T>(Object[] changedRenderers, Component[] changedTransforms)
            where T : Component
        {
            var map = new Dictionary<EntityId, ChangedObject<T>>();

            foreach (Component component in changedTransforms)
            {
                map.TryAdd(component.GetEntityId(), new ChangedObject<T>() { objectReference = (T)component, transformChanged = true });
            }

            foreach (Component component in changedRenderers)
            {
                map.TryAdd(component.GetEntityId(), new ChangedObject<T>() { objectReference = (T)component, transformChanged = false });
            }

            return map;
        }
    }
}

#endif
