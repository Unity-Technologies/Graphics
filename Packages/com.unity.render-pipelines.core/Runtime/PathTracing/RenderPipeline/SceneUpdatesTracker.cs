#if SURFACE_CACHE || ENABLE_PATH_TRACING_SRP

using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.LiveGI
{
    internal class SceneChanges
    {
        public List<MeshRenderer> addedInstances;
        public List<InstanceChanges> changedInstances;
        public List<EntityId> removedInstances;

        public List<Material> addedMaterials;
        public List<EntityId> removedMaterials;
        public List<Material> changedMaterials;

        public List<Light> addedLights;
        public List<Light> changedLights;
        public List<EntityId> removedLights;

        public SceneChanges()
        {
            addedInstances = new List<MeshRenderer>();
            changedInstances = new List<InstanceChanges>();
            removedInstances = new List<EntityId>();

            addedMaterials = new List<Material>();
            removedMaterials = new List<EntityId>();
            changedMaterials = new List<Material>();

            addedLights = new List<Light>();
            changedLights = new List<Light>();
            removedLights = new List<EntityId>();
        }

        public bool HasChanges()
        {
            return (addedInstances.Count | removedInstances.Count | changedInstances.Count
                | addedMaterials.Count | removedMaterials.Count | changedMaterials.Count
                | addedLights.Count | removedLights.Count | changedLights.Count) != 0;
        }

        public void Clear()
        {
            addedInstances.Clear();
            removedInstances.Clear();
            changedInstances.Clear();

            addedMaterials.Clear();
            removedMaterials.Clear();
            changedMaterials.Clear();

            addedLights.Clear();
            removedLights.Clear();
            changedLights.Clear();
        }
    }

    [System.Flags]
    internal enum ModifiedProperties { Transform = 1, Material = 2, IsStatic = 4, ShadowCasting = 8, Layer = 16 }
    internal struct InstanceChanges
    {
        public MeshRenderer meshRenderer;
        public ModifiedProperties changes;
    }

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
                shaderCompiled = metaPassIndex != -1 ? ShaderUtil.IsPassCompiled(material, metaPassIndex) : true;
            }

            public Material material;
            public int metaPassIndex;
            public bool shaderCompiled;

            public Timestamp timestamp;
        }

        class InstanceData
        {
            public Timestamp timestamp;
            public EntityId[] materialIDs;
            public Material[] materials;
            public MeshRenderer renderer;
            public bool isStatic;
            public ShadowCastingMode shadowCastingMode;
        }

        class LightData
        {
            public Light light;
            public Timestamp timestamp;
        }

        ObjectDispatcher m_ObjectDispatcher;
        Dictionary<EntityId, InstanceData> m_Instances;
        Dictionary<EntityId, MaterialData> m_Materials;
        Dictionary<EntityId, LightData> m_Lights;
        SceneChanges m_Changes;
        uint m_Timestamp;


        public SceneUpdatesTracker()
        {
            m_Changes = new SceneChanges();
            m_Instances = new Dictionary<EntityId, InstanceData>();
            m_Materials = new Dictionary<EntityId, MaterialData>();
            m_Lights = new Dictionary<EntityId, LightData>();

            m_ObjectDispatcher = new ObjectDispatcher();

#if UNITY_EDITOR
            m_ObjectDispatcher.maxDispatchHistoryFramesCount = int.MaxValue;
#endif
            m_ObjectDispatcher.EnableTypeTracking<MeshRenderer>(ObjectDispatcher.TypeTrackingFlags.SceneObjects);
            m_ObjectDispatcher.EnableTransformTracking<MeshRenderer>(ObjectDispatcher.TransformTrackingType.GlobalTRS);
            m_ObjectDispatcher.EnableTypeTracking<Material>(ObjectDispatcher.TypeTrackingFlags.SceneObjects | ObjectDispatcher.TypeTrackingFlags.Assets);
            m_ObjectDispatcher.EnableTypeTracking<Light>(ObjectDispatcher.TypeTrackingFlags.SceneObjects);
            m_ObjectDispatcher.EnableTransformTracking<Light>(ObjectDispatcher.TransformTrackingType.GlobalTRS);
        }

        public void Dispose()
        {
            m_ObjectDispatcher.Dispose();
            m_Changes.Clear();
        }

        public SceneChanges GetChanges(bool filterRealtimeLights, bool filterBakedLights, bool filterMixedLights)
        {
            m_Timestamp++;
            m_Changes.Clear();

            FindInstancesChanges();
            FindMaterialsChanges();
            FindLightChanges(filterRealtimeLights, filterBakedLights, filterMixedLights);

            return m_Changes;
        }

        private void FindMaterialsChanges()
        {
            using var materialChanges = m_ObjectDispatcher.GetTypeChangesAndClear<Material>(Unity.Collections.Allocator.Temp);

            // Handle added materials
            foreach (var instance in m_Instances.Values)
            {
                foreach (var material in instance.materials)
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
                MaterialData data;
                if (!m_Materials.TryGetValue(changedMaterial.GetEntityId(), out data))
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

        private void FindInstancesChanges()
        {
            // Handle changed instances
            using var meshRendererChanges = m_ObjectDispatcher.GetTypeChangesAndClear<MeshRenderer>(Unity.Collections.Allocator.Temp);
            var transformChanges = m_ObjectDispatcher.GetTransformChangesAndClear<MeshRenderer>(ObjectDispatcher.TransformTrackingType.GlobalTRS, false);
            var changedRenderers = MergeChanges<MeshRenderer>(meshRendererChanges.changed, transformChanges);

            // Handle removed instances
            foreach (var key in meshRendererChanges.destroyedID)
            {
                m_Changes.removedInstances.Add(key);
            }

            // Update the remaining timestamps of the active mesh renderers
            List<EntityId> keys = new List<EntityId>(m_Instances.Keys);
            foreach (var key in keys)
            {
                if (!m_Instances[key].renderer)
                    continue;

                if (m_Instances[key].renderer.enabled && m_Instances[key].renderer.gameObject.activeInHierarchy)
                {
                    m_Instances[key].timestamp.lastVisit = m_Timestamp;
                }
                else
                {
                    m_Changes.removedInstances.Add(key);
                }
            }

            foreach (var key in m_Changes.removedInstances)
                m_Instances.Remove(key);


            foreach (var item in changedRenderers)
            {
                var meshRenderer = item.Value.objectReference;
                bool tranformChanged = item.Value.transformChanged;

                if (!meshRenderer.enabled || !meshRenderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                InstanceData oldData;
                if (!m_Instances.TryGetValue(meshRenderer.GetEntityId(), out oldData))
                {
                    // This renderer was just added
                    var newData = CreateInstanceData(m_Timestamp, meshRenderer);
                    m_Instances.Add(meshRenderer.GetEntityId(), newData);
                    m_Changes.addedInstances.Add(meshRenderer);
                    continue;
                }

                var data = CreateInstanceData(m_Timestamp, meshRenderer);

                ModifiedProperties changes = 0;

                if (tranformChanged)
                    changes |= ModifiedProperties.Transform;

                bool IntArraySequenceEqual(EntityId[] firstArray, EntityId[] secondArray) =>
                    ((ReadOnlySpan<EntityId>)firstArray).SequenceEqual(secondArray);

                if (!IntArraySequenceEqual(oldData.materialIDs, data.materialIDs))
                    changes |= ModifiedProperties.Material;

                if (oldData.isStatic != data.isStatic)
                    changes |= ModifiedProperties.IsStatic;

                if (oldData.shadowCastingMode != data.shadowCastingMode)
                    changes |= ModifiedProperties.ShadowCasting;

                if (changes != 0)
                {
                    m_Changes.changedInstances.Add(new InstanceChanges()
                    {
                        changes = changes,
                        meshRenderer = meshRenderer
                    });

                    m_Instances[meshRenderer.GetEntityId()] = data;
                }
            }
        }

        InstanceData CreateInstanceData(uint timestamp, MeshRenderer meshRenderer)
        {
            return new InstanceData()
            {
                timestamp = new Timestamp { lastVisit = timestamp, creation = timestamp },
                isStatic = meshRenderer.gameObject.isStatic,
                materials = meshRenderer.sharedMaterials,
                materialIDs = Array.ConvertAll(meshRenderer.sharedMaterials, mat => mat != null ? mat.GetEntityId() : EntityId.None),
                shadowCastingMode = meshRenderer.shadowCastingMode,
                renderer = meshRenderer,
            };
        }

        private void FindLightChanges(bool filterRealtimeLights, bool filterBakedLights, bool filterMixedLights)
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

                bool isRealtimeLight = m_Lights[key].light.lightmapBakeType == LightmapBakeType.Realtime;
                bool isBakedLight = m_Lights[key].light.lightmapBakeType == LightmapBakeType.Baked;
                bool isMixedLight = m_Lights[key].light.lightmapBakeType == LightmapBakeType.Mixed;

                if (m_Lights[key].light.enabled && m_Lights[key].light.gameObject.activeInHierarchy && !(filterRealtimeLights && isRealtimeLight) && !(filterBakedLights && isBakedLight) && !(filterMixedLights && isMixedLight))
                {
                    m_Lights[key].timestamp.lastVisit = m_Timestamp;
                }
                else
                {
                    m_Changes.removedLights.Add(key);
                }
            }

            foreach (var key in m_Changes.removedLights)
                m_Lights.Remove(key);


            foreach (var item in changedLights)
            {
                var light = item.Value.objectReference;

                bool isRealtimeLight = light.lightmapBakeType == LightmapBakeType.Realtime;
                bool isBakedLight = light.lightmapBakeType == LightmapBakeType.Baked;
                bool isMixedLight = light.lightmapBakeType == LightmapBakeType.Mixed;

                if (!light.enabled || !light.gameObject.activeInHierarchy || (filterRealtimeLights && isRealtimeLight) && !(filterBakedLights && isBakedLight) && !(filterMixedLights && isMixedLight))
                {
                    continue;
                }

                var newData = CreateLightData(m_Timestamp, light);

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

        LightData CreateLightData(uint timestamp, Light light)
        {
            return new LightData()
            {
                timestamp = new Timestamp { lastVisit = timestamp, creation = timestamp },
                light = light,
            };
        }

        struct ChangedObject<T>
            where T : Component
        {
            public T objectReference;
            public bool transformChanged;
        }

        Dictionary<EntityId, ChangedObject<T>> MergeChanges<T>(Object[] changedRenderers, Component[] changedTransforms)
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
