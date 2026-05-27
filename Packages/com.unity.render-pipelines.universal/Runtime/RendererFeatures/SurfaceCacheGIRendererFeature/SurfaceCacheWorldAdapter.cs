#if SURFACE_CACHE

using System;
using System.Collections.Generic;
using UnityEngine.PathTracing.Core;
using InstanceHandle = UnityEngine.PathTracing.Core.Handle<UnityEngine.Rendering.SurfaceCacheWorld.Instance>;
using LightHandle = UnityEngine.PathTracing.Core.Handle<UnityEngine.Rendering.SurfaceCacheWorld.Light>;
using MaterialHandle = UnityEngine.PathTracing.Core.Handle<UnityEngine.PathTracing.Core.MaterialPool.MaterialDescriptor>;
using ObjectDispatcher = UnityEngine.InternalBridge.ObjectDispatcher;

namespace UnityEngine.Rendering.Universal
{
    class SurfaceCacheWorldAdapter
    {
        readonly SharedMaterialSet _sharedMaterials;
        readonly LightSet _lights;
        readonly MeshRendererSet _meshRenderers;
#if ENABLE_TERRAIN_MODULE
        readonly TerrainSet _terrains;
        // Per-frame set of TerrainData EntityIds that were reported changed by ObjectDispatcher.
        readonly HashSet<EntityId> _dirtyTerrainDataThisFrame = new();
#endif

        public SurfaceCacheWorldAdapter(ObjectDispatcher objDispatcher, SurfaceCacheWorld world, Material fallbackMaterial)
        {
            _lights = new LightSet();
            _sharedMaterials = new SharedMaterialSet(fallbackMaterial);
            _meshRenderers = new MeshRendererSet(_sharedMaterials, fallbackMaterial, world);
#if ENABLE_TERRAIN_MODULE
            _terrains = new TerrainSet(_sharedMaterials, fallbackMaterial, world);
#endif

#if UNITY_EDITOR
            objDispatcher.maxDispatchHistoryFramesCount = int.MaxValue;
#endif
            objDispatcher.EnableTypeTracking<MeshRenderer>(ObjectDispatcher.TypeTrackingFlags.SceneObjects);
            objDispatcher.EnableTransformTracking<MeshRenderer>(ObjectDispatcher.TransformTrackingType.GlobalTRS);
            objDispatcher.EnableTypeTracking<Light>(ObjectDispatcher.TypeTrackingFlags.SceneObjects);
            objDispatcher.EnableTransformTracking<Light>(ObjectDispatcher.TransformTrackingType.GlobalTRS);
            objDispatcher.EnableTypeTracking<Material>(ObjectDispatcher.TypeTrackingFlags.SceneObjects | ObjectDispatcher.TypeTrackingFlags.Assets);
#if ENABLE_TERRAIN_MODULE
            objDispatcher.EnableTypeTracking<Terrain>(ObjectDispatcher.TypeTrackingFlags.SceneObjects);
            objDispatcher.EnableTransformTracking<Terrain>(ObjectDispatcher.TransformTrackingType.GlobalTRS);
            objDispatcher.EnableTypeTracking<TerrainData>(ObjectDispatcher.TypeTrackingFlags.SceneObjects | ObjectDispatcher.TypeTrackingFlags.Assets);
#endif
        }

        public void CleanUp(SurfaceCacheWorld world)
        {
            _meshRenderers.CleanUp(_sharedMaterials, world);
#if ENABLE_TERRAIN_MODULE
            _terrains.CleanUp(_sharedMaterials, world);
#endif
            _lights.CleanUp(world);
            _sharedMaterials.CleanUp(world);
        }

        public void Update(ObjectDispatcher objDispatcher, AmbientMode ambientMode, Material skyboxMaterial,
            Color ambientSkycolor, Color ambientEquatorColor, Color ambientGroundColor, float materialEnvIntensityMultiplier,
            bool divideEnvIntensityByPI, SurfaceCacheWorld world)
        {
            UpdateMeshRenderers(objDispatcher, world);
#if ENABLE_TERRAIN_MODULE
            UpdateTerrains(objDispatcher, world);
#endif
            UpdateLights(objDispatcher, world);
            UpdateMaterials(objDispatcher, world);
            UpdateEnvironment(
                ambientMode,
                skyboxMaterial,
                ambientSkycolor,
                ambientEquatorColor,
                ambientGroundColor,
                materialEnvIntensityMultiplier,
                divideEnvIntensityByPI,
                world);
        }

        void UpdateMeshRenderers(ObjectDispatcher objDispatcher, SurfaceCacheWorld world)
        {
            var transformChanges = objDispatcher.GetTransformChangesAndClear<MeshRenderer>(ObjectDispatcher.TransformTrackingType.GlobalTRS, false);
            foreach (var component in transformChanges)
            {
                var meshRenderer = (MeshRenderer)component;
                _meshRenderers.Refresh(meshRenderer, _sharedMaterials, world, true);
            }

            using (var typeChanges = objDispatcher.GetTypeChangesAndClear<MeshRenderer>(Unity.Collections.Allocator.Temp))
            {
                foreach (var component in typeChanges.changed)
                {
                    var meshRenderer = (MeshRenderer)component;
                    _meshRenderers.Refresh(meshRenderer, _sharedMaterials, world, false);
                }

                foreach (var entityId in typeChanges.destroyedID)
                {
                    if (_meshRenderers.Contains(entityId))
                        _meshRenderers.Remove(entityId, _sharedMaterials, world);
                }
            }
        }

#if ENABLE_TERRAIN_MODULE
        void UpdateTerrains(ObjectDispatcher objDispatcher, SurfaceCacheWorld world)
        {
            Debug.Assert(_dirtyTerrainDataThisFrame.Count == 0);

            using (var terrainDataChanges = objDispatcher.GetTypeChangesAndClear<TerrainData>(Unity.Collections.Allocator.Temp))
            {
                foreach (var obj in terrainDataChanges.changed)
                    _dirtyTerrainDataThisFrame.Add(((TerrainData)obj).GetEntityId());
            }

            var transformChanges = objDispatcher.GetTransformChangesAndClear<Terrain>(ObjectDispatcher.TransformTrackingType.GlobalTRS, false);
            foreach (var component in transformChanges)
                _terrains.Refresh((Terrain)component, _sharedMaterials, world, true);

            using (var typeChanges = objDispatcher.GetTypeChangesAndClear<Terrain>(Unity.Collections.Allocator.Temp))
            {
                foreach (var component in typeChanges.changed)
                    _terrains.Refresh((Terrain)component, _sharedMaterials, world, false);

                foreach (var entityId in typeChanges.destroyedID)
                {
                    if (_terrains.Contains(entityId))
                        _terrains.Remove(entityId, _sharedMaterials, world);
                }
            }

            // TerrainData changes require remove + re-add of the World instance because terrain geometry is baked at AddInstance time and currently cannot be patched in place.
            if (_dirtyTerrainDataThisFrame.Count > 0)
                _terrains.RebuildConsumers(_dirtyTerrainDataThisFrame, _sharedMaterials, world);

            _dirtyTerrainDataThisFrame.Clear();

#if UNITY_EDITOR
            _terrains.ProcessDeferredRebuilds(_sharedMaterials, world);
#endif
        }
#endif

        void UpdateEnvironment(AmbientMode ambientMode, Material skyboxMaterial,
            Color ambientSkycolor, Color ambientEquatorColor, Color ambientGroundColor, float materialEnvIntensityMultiplier,
            bool divideEnvIntensityByPI, SurfaceCacheWorld world)
        {
            float envIntensityMultiplier = divideEnvIntensityByPI ? 1.0f / Mathf.PI : 1.0f;

            switch (ambientMode)
            {
                case AmbientMode.Skybox:
                    world.SetEnvironmentMode(CubemapRender.Mode.Material);
                    world.SetEnvironmentMaterial(skyboxMaterial);
                    world.SetEnvironmentIntensityMultiplier(materialEnvIntensityMultiplier * envIntensityMultiplier);
                    break;

                case AmbientMode.Flat:
                    world.SetEnvironmentMode(CubemapRender.Mode.Color);
                    world.SetEnvironmentColor(ambientSkycolor);
                    world.SetEnvironmentIntensityMultiplier(envIntensityMultiplier);
                    break;

                case AmbientMode.Trilight:
                    world.SetEnvironmentMode(CubemapRender.Mode.Color);
                    world.SetEnvironmentGradientColors(ambientSkycolor, ambientEquatorColor, ambientGroundColor);
                    world.SetEnvironmentIntensityMultiplier(envIntensityMultiplier);
                    break;

                default:
                    world.SetEnvironmentMode(CubemapRender.Mode.Color);
                    world.SetEnvironmentColor(Color.black);
                    world.SetEnvironmentIntensityMultiplier(envIntensityMultiplier);
                    break;
            }
        }

        void UpdateLights(ObjectDispatcher objDispatcher, SurfaceCacheWorld world)
        {
            var transformChanges = objDispatcher.GetTransformChangesAndClear<Light>(ObjectDispatcher.TransformTrackingType.GlobalTRS, false);
            foreach (var component in transformChanges)
                _lights.Refresh((Light)component, world);

            using (var typeChanges = objDispatcher.GetTypeChangesAndClear<Light>(Unity.Collections.Allocator.Temp))
            {
                foreach (var component in typeChanges.changed)
                    _lights.Refresh((Light)component, world);

                foreach (var entityId in typeChanges.destroyedID)
                {
                    if (_lights.Contains(entityId))
                        _lights.Remove(entityId, world);
                }

            }
        }

        void UpdateMaterials(ObjectDispatcher objDispatcher, SurfaceCacheWorld world)
        {
#if UNITY_EDITOR
            _sharedMaterials.Update(world);
#endif

            using (var typeChanges = objDispatcher.GetTypeChangesAndClear<Material>(Unity.Collections.Allocator.Temp))
            {
                foreach (var obj in typeChanges.changed)
                {
                    var material = (Material)obj;
                    var matEntityId = material.GetEntityId();
                    if (_sharedMaterials.IsReferenced(matEntityId))
                    {
                        _sharedMaterials.Update(matEntityId, material, world);
                    }
                }

                // For now we do not explicitly handle material _removal_. This is acceptable for these reasons:
                // 1) Even without explicit handling, the user experience is decent. If a user removes a material currently
                //    being used by a mesh renderer, they can assign a new material and everything will be in sync again.
                // 2) Keeping the associated data structures consistent is not easy and requires extra complexity and
                //    and tracking. It is a lot of work and cost for little gain.
            }
        }

        class MeshRendererSet
        {
            readonly Dictionary<EntityId, InstanceHandle> _entityIdsToWorldInstanceHandles = new();
            readonly EntityId _fallbackMaterialEntityId;
            readonly MaterialHandle _fallbackMaterialHandle;

            // Whenever a MeshRenderer points to no material then the material Entity ID in this
            // dictionary must be set to EntityId.None.
            Dictionary<EntityId, EntityId[]> _entityIdsToMaterialEntityIdArrays = new();

            public MeshRendererSet(SharedMaterialSet sharedMaterials, Material fallbackMaterial, SurfaceCacheWorld world)
            {
                _fallbackMaterialEntityId = fallbackMaterial.GetEntityId();
                _fallbackMaterialHandle = sharedMaterials.Acquire(_fallbackMaterialEntityId, fallbackMaterial, world);
            }

            public void CleanUp(SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                foreach (var instanceHandle in _entityIdsToWorldInstanceHandles.Values)
                    world.RemoveInstance(instanceHandle);

                sharedMaterials.Release(_fallbackMaterialEntityId, world);
            }

            public bool Contains(EntityId meshRendererEntityId)
            {
                return _entityIdsToWorldInstanceHandles.ContainsKey(meshRendererEntityId);
            }

            public void Refresh(MeshRenderer renderer, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world, bool transformChange)
            {
                var entityId = renderer.GetEntityId();
                var exists = _entityIdsToWorldInstanceHandles.TryGetValue(entityId, out var instanceHandle);
                var meshFilter = renderer.GetComponent<MeshFilter>();
                var mesh = meshFilter?.sharedMesh;
                var shouldExist = renderer.enabled && renderer.gameObject.activeInHierarchy && mesh != null && mesh.vertexCount != 0;
                var rendererMaterials = renderer.sharedMaterials;

                if (exists)
                {
                    if (shouldExist)
                    {
                        // Mesh renderer exists and it should exist, so we must update it.
                        Debug.Assert(!renderer.isPartOfStaticBatch, "Static Batching is not supported by Surface Cache GI.");
                        Debug.Assert(mesh != null && mesh.vertexCount != 0);

                        if (transformChange)
                        {
                            world.UpdateInstanceTransform(instanceHandle, renderer.transform.localToWorldMatrix);
                        }
                        else
                        {
                            Span<EntityId> instanceMaterials = stackalloc EntityId[mesh.subMeshCount];
                            for (int i = 0; i < instanceMaterials.Length; i++)
                            {
                                Material material = i < rendererMaterials.Length ? rendererMaterials[i] : null;
                                instanceMaterials[i] = material != null ? material.GetEntityId() : EntityId.None;
                            }

                            var materialsChanged = !((ReadOnlySpan<EntityId>)instanceMaterials).SequenceEqual(_entityIdsToMaterialEntityIdArrays[entityId]);

                            if (materialsChanged)
                            {
                                Span<MaterialHandle> materialHandles = stackalloc MaterialHandle[mesh.subMeshCount];
                                for (int i = 0; i < materialHandles.Length; i++)
                                {
                                    Material material = i < rendererMaterials.Length ? rendererMaterials[i] : null;
                                    MaterialHandle handle;
                                    if (material != null)
                                        handle = sharedMaterials.Acquire(material.GetEntityId(), material, world);
                                    else
                                        handle = _fallbackMaterialHandle;
                                    materialHandles[i] = handle;
                                }
                                Span<uint> masks = stackalloc uint[mesh.subMeshCount];
                                for (int i = 0; i < masks.Length; i++)
                                {
                                    masks[i] = i < rendererMaterials.Length && rendererMaterials[i] != null ? 1u : 0u;
                                }

                                Debug.Assert(_entityIdsToMaterialEntityIdArrays.ContainsKey(entityId));
                                foreach (var matEntityId in _entityIdsToMaterialEntityIdArrays[entityId])
                                {
                                    if (matEntityId != EntityId.None)
                                        sharedMaterials.Release(matEntityId, world);
                                }
                                _entityIdsToMaterialEntityIdArrays[entityId] = instanceMaterials.ToArray();

                                world.UpdateInstanceMaterials(instanceHandle, materialHandles);
                                world.UpdateInstanceMask(instanceHandle, masks);
                            }
                        }
                    }
                    else
                    {
                        // Mesh renderer exists and it should not exist, so we must remove it.
                        Remove(entityId, sharedMaterials, world);
                    }
                }
                else
                {
                    if (shouldExist)
                    {
                        // Mesh renderer does not exist and it should exist, so we must create it.

                        var instanceMaterials = new EntityId[mesh.subMeshCount];
                        Span<MaterialHandle> materialHandles = stackalloc MaterialHandle[mesh.subMeshCount];
                        for (int i = 0; i < materialHandles.Length; i++)
                        {
                            Material material = i < rendererMaterials.Length ? rendererMaterials[i] : null;
                            MaterialHandle handle;
                            EntityId matEntityId;
                            if (material != null)
                            {
                                matEntityId = material.GetEntityId();
                                handle = sharedMaterials.Acquire(matEntityId, material, world);
                            }
                            else
                            {
                                matEntityId = EntityId.None;
                                handle = _fallbackMaterialHandle;
                            }
                            instanceMaterials[i] = matEntityId;
                            materialHandles[i] = handle;
                        }
                        Span<uint> masks = stackalloc uint[mesh.subMeshCount];
                        for (int i = 0; i < masks.Length; i++)
                        {
                            masks[i] = i < rendererMaterials.Length && rendererMaterials[i] != null ? 1u : 0u;
                        }

                        InstanceHandle instance = world.AddInstance(mesh, materialHandles, masks, renderer.transform.localToWorldMatrix);
                        Debug.Assert(!_entityIdsToWorldInstanceHandles.ContainsKey(entityId));
                        Debug.Assert(!_entityIdsToMaterialEntityIdArrays.ContainsKey(entityId));
                        _entityIdsToWorldInstanceHandles.Add(entityId, instance);
                        _entityIdsToMaterialEntityIdArrays.Add(entityId, instanceMaterials);
                    }
                }
            }

            public void Remove(EntityId renderer, SharedMaterialSet adapterMaterials, SurfaceCacheWorld world)
            {
                Debug.Assert(_entityIdsToWorldInstanceHandles.ContainsKey(renderer));
                Debug.Assert(_entityIdsToMaterialEntityIdArrays.ContainsKey(renderer));

                world.RemoveInstance(_entityIdsToWorldInstanceHandles[renderer]);

                foreach (var matEntityId in _entityIdsToMaterialEntityIdArrays[renderer])
                {
                    if (matEntityId != EntityId.None)
                        adapterMaterials.Release(matEntityId, world);
                }

                _entityIdsToWorldInstanceHandles.Remove(renderer);
                _entityIdsToMaterialEntityIdArrays.Remove(renderer);
            }
        }

#if ENABLE_TERRAIN_MODULE
        class TerrainSet
        {
            struct Entry
            {
                public Terrain Terrain;
                public InstanceHandle InstanceHandle;
                public EntityId MaterialEntityId;
                public EntityId TerrainDataEntityId;
            }

            readonly Dictionary<EntityId, Entry> _entries = new();

            // Scratch buffer reused by RebuildConsumers to snapshot ids before mutating _entries.
            readonly List<EntityId> _terrainEntityIdScratch = new();

            readonly EntityId _fallbackMaterialEntityId;
            readonly MaterialHandle _fallbackMaterialHandle;

#if UNITY_EDITOR
            struct DeferredRebuild
            {
                public uint TickAtLastChange;
            }

            const uint k_TerrainRebuildDelayInTicks = 30;
            uint _tickCounter;
            readonly Dictionary<EntityId, DeferredRebuild> _deferredRebuilds = new();
            readonly List<EntityId> _deferredRebuildScratch = new();
#endif

            public TerrainSet(SharedMaterialSet sharedMaterials, Material fallbackMaterial, SurfaceCacheWorld world)
            {
                _fallbackMaterialEntityId = fallbackMaterial.GetEntityId();
                _fallbackMaterialHandle = sharedMaterials.Acquire(_fallbackMaterialEntityId, fallbackMaterial, world);
            }

            public void CleanUp(SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                foreach (var entry in _entries.Values)
                {
                    world.RemoveInstance(entry.InstanceHandle);
                    if (entry.MaterialEntityId != EntityId.None)
                        sharedMaterials.Release(entry.MaterialEntityId, world);
                }

                sharedMaterials.Release(_fallbackMaterialEntityId, world);
            }

            public bool Contains(EntityId terrainEntityId)
            {
                return _entries.ContainsKey(terrainEntityId);
            }

            public void Refresh(Terrain terrain, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world, bool transformChange)
            {
                var entityId = terrain.GetEntityId();
                var exists = _entries.TryGetValue(entityId, out var entry);
                var terrainData = terrain.terrainData;
                var shouldExist = terrain.isActiveAndEnabled && terrainData != null;

                if (exists)
                {
                    if (shouldExist)
                    {
                        if (transformChange)
                        {
                            world.UpdateInstanceTransform(entry.InstanceHandle, terrain.transform.localToWorldMatrix);
                            return;
                        }

                        var newTerrainDataEntityId = terrainData.GetEntityId();
                        if (newTerrainDataEntityId != entry.TerrainDataEntityId)
                        {
                            RebuildInstance(terrain, entityId, sharedMaterials, world);
                            return;
                        }

                        var newMaterial = terrain.splatBaseMaterial;
                        var newMaterialEntityId = newMaterial != null ? newMaterial.GetEntityId() : EntityId.None;
                        if (newMaterialEntityId != entry.MaterialEntityId)
                        {
                            MaterialHandle newHandle;
                            if (newMaterial != null)
                                newHandle = sharedMaterials.Acquire(newMaterialEntityId, newMaterial, world);
                            else
                                newHandle = _fallbackMaterialHandle;

                            Span<MaterialHandle> materialHandles = stackalloc MaterialHandle[1] { newHandle };
                            Span<uint> masks = stackalloc uint[1] { newMaterial != null ? 1u : 0u };
                            world.UpdateInstanceMaterials(entry.InstanceHandle, materialHandles);
                            world.UpdateInstanceMask(entry.InstanceHandle, masks);

                            if (entry.MaterialEntityId != EntityId.None)
                                sharedMaterials.Release(entry.MaterialEntityId, world);

                            entry.MaterialEntityId = newMaterialEntityId;
                            _entries[entityId] = entry;
                        }
                    }
                    else
                    {
                        Remove(entityId, sharedMaterials, world);
                    }
                }
                else
                {
                    if (shouldExist)
                        AddInstance(terrain, entityId, sharedMaterials, world);
                }
            }

            // For every tracked Terrain whose TerrainData appears in the dirty set, schedule (Editor) or perform (Player) a geometry rebuild.
            public void RebuildConsumers(HashSet<EntityId> dirtyTerrainDataIds, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                _terrainEntityIdScratch.Clear();
                foreach (var entry in _entries)
                {
                    if (dirtyTerrainDataIds.Contains(entry.Value.TerrainDataEntityId))
                        _terrainEntityIdScratch.Add(entry.Key);
                }

                foreach (var terrainEntityId in _terrainEntityIdScratch)
                {
#if UNITY_EDITOR
                    _deferredRebuilds[terrainEntityId] = new DeferredRebuild
                    {
                        TickAtLastChange = _tickCounter,
                    };
#else
                    RebuildInstance(_entries[terrainEntityId].Terrain, terrainEntityId, sharedMaterials, world);
#endif
                }
            }

#if UNITY_EDITOR
            public void ProcessDeferredRebuilds(SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                _tickCounter++;

                if (_deferredRebuilds.Count == 0)
                    return;

                _deferredRebuildScratch.Clear();
                foreach (var deferredRebuild in _deferredRebuilds)
                {
                    if (_tickCounter - deferredRebuild.Value.TickAtLastChange >= k_TerrainRebuildDelayInTicks)
                        _deferredRebuildScratch.Add(deferredRebuild.Key);
                }

                foreach (var entityId in _deferredRebuildScratch)
                {
                    _deferredRebuilds.Remove(entityId);
                    RebuildInstance(_entries[entityId].Terrain, entityId, sharedMaterials, world);
                }
            }
#endif

            public void Remove(EntityId terrainEntityId, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                Debug.Assert(_entries.ContainsKey(terrainEntityId));
                var entry = _entries[terrainEntityId];
                world.RemoveInstance(entry.InstanceHandle);
                if (entry.MaterialEntityId != EntityId.None)
                    sharedMaterials.Release(entry.MaterialEntityId, world);
                _entries.Remove(terrainEntityId);
#if UNITY_EDITOR
                _deferredRebuilds.Remove(terrainEntityId);
#endif
            }

            void AddInstance(Terrain terrain, EntityId terrainEntityId, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                Debug.Assert(!_entries.ContainsKey(terrainEntityId));
                var terrainData = terrain.terrainData;
                Debug.Assert(terrainData != null);

                var material = terrain.splatBaseMaterial;
                MaterialHandle materialHandle;
                EntityId materialEntityId;
                if (material != null)
                {
                    materialEntityId = material.GetEntityId();
                    materialHandle = sharedMaterials.Acquire(materialEntityId, material, world);
                }
                else
                {
                    materialEntityId = EntityId.None;
                    materialHandle = _fallbackMaterialHandle;
                }

                uint mask = material != null ? 1u : 0u;
                var instance = world.AddInstance(terrain, materialHandle, mask, terrain.transform.localToWorldMatrix);
                _entries[terrainEntityId] = new Entry
                {
                    Terrain = terrain,
                    InstanceHandle = instance,
                    MaterialEntityId = materialEntityId,
                    TerrainDataEntityId = terrainData.GetEntityId(),
                };
            }

            void RebuildInstance(Terrain terrain, EntityId terrainEntityId, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                Debug.Assert(_entries.ContainsKey(terrainEntityId));
                Debug.Assert(terrain.terrainData != null);

                var oldMaterialEntityId = _entries[terrainEntityId].MaterialEntityId;

                world.RemoveInstance(_entries[terrainEntityId].InstanceHandle);
                _entries.Remove(terrainEntityId);

                AddInstance(terrain, terrainEntityId, sharedMaterials, world);

                if (oldMaterialEntityId != EntityId.None)
                    sharedMaterials.Release(oldMaterialEntityId, world);
            }
        }
#endif

        class LightSet
        {
            readonly Dictionary<EntityId, LightHandle> _entityIdsToWorldHandles = new();

            public void CleanUp(SurfaceCacheWorld world)
            {
                foreach (var lightHandle in _entityIdsToWorldHandles.Values)
                    world.RemoveLight(lightHandle);
            }

            public bool Contains(EntityId entityId)
            {
                return _entityIdsToWorldHandles.ContainsKey(entityId);
            }

            public void Refresh(Light light, SurfaceCacheWorld world)
            {
                const bool multiplyPunctualLightIntensityByPI = false;
                var entityId = light.GetEntityId();
                var exists = Contains(entityId);
                var shouldExist = light.isActiveAndEnabled && !light.bakingOutput.isBaked;

                if (exists)
                {
                    if (shouldExist)
                    {
                        // Light exists and it should exist, so we update.
                        var lightDesc = CreateLightDescriptor(light, multiplyPunctualLightIntensityByPI);
                        Debug.Assert(_entityIdsToWorldHandles.ContainsKey(entityId));
                        world.UpdateLight(_entityIdsToWorldHandles[entityId], lightDesc);
                    }
                    else
                    {
                        // Light exists and it should not exist, so we remove.
                        Remove(entityId, world);
                    }
                }
                else
                {
                    if (shouldExist)
                    {
                        // Light does not exist and it should exist, so we create.
                        var lightDesc = CreateLightDescriptor(light, multiplyPunctualLightIntensityByPI);
                        Debug.Assert(!_entityIdsToWorldHandles.ContainsKey(entityId));
                        _entityIdsToWorldHandles[entityId] = world.AddLight(lightDesc);
                    }
                }
            }

            public void Remove(EntityId light, SurfaceCacheWorld world)
            {
                Debug.Assert(_entityIdsToWorldHandles.ContainsKey(light));
                world.RemoveLight(_entityIdsToWorldHandles[light]);
                _entityIdsToWorldHandles.Remove(light);
            }

            static SurfaceCacheWorld.LightDescriptor CreateLightDescriptor(Light light, bool multiplyPunctualLightIntensityByPI)
            {
                var desc = new SurfaceCacheWorld.LightDescriptor();
                desc.Type = light.type;
                desc.LinearLightColor = Util.GetLinearLightColor(light, light.bounceIntensity);
                if (multiplyPunctualLightIntensityByPI && Util.IsPunctualLightType(light.type))
                    desc.LinearLightColor *= Mathf.PI;
                desc.Transform = light.transform.localToWorldMatrix;
                desc.ColorTemperature = light.colorTemperature;
                desc.OuterSpotAngle = light.spotAngle;
                desc.InnerSpotAngle = light.innerSpotAngle;
                desc.Range = light.range;
                return desc;
            }
        }

        class SharedMaterialSet
        {
            struct Entry
            {
                public Material Material;
                public MaterialHandle WorldHandle;
                public uint RefCount;
                public MaterialPool.MaterialDescriptor Descriptor;
            }

            const EmissionMode kEmissionMode = EmissionMode.Realtime;
            const UVChannel kUVChannel = UVChannel.UV0;

            readonly Dictionary<EntityId, Entry> _entries = new();
            readonly HashSet<EntityId> _pendingMetaPassEvals = new();
            readonly Material _fallbackMaterial;

            public SharedMaterialSet(Material fallbackMaterial)
            {
                _fallbackMaterial = fallbackMaterial;
            }

            public MaterialHandle Acquire(EntityId matEntityId, Material mat, SurfaceCacheWorld world)
            {
                if (_entries.TryGetValue(matEntityId, out var entry))
                {
                    entry.RefCount += 1;
                    _entries[matEntityId] = entry;
                    return entry.WorldHandle;
                }
                else
                {
                    var metaPassIndex = mat.FindPass("Meta");
                    Debug.Assert(metaPassIndex != -1, "The material has no metapass.");
                    MaterialPool.MaterialDescriptor descriptor;
#if UNITY_EDITOR
                    if (UnityEditor.ShaderUtil.IsPassCompiled(mat, metaPassIndex))
                    {
                        descriptor = MaterialPool.ConvertUnityMaterialToMaterialDescriptor(mat, kEmissionMode);
                    }
                    else
                    {
                        var oldAllowAsyncCompilation = UnityEditor.ShaderUtil.allowAsyncCompilation;
                        UnityEditor.ShaderUtil.allowAsyncCompilation = false;
                        descriptor = MaterialPool.ConvertUnityMaterialToMaterialDescriptor(_fallbackMaterial, kEmissionMode);
                        UnityEditor.ShaderUtil.allowAsyncCompilation = oldAllowAsyncCompilation;
                        _pendingMetaPassEvals.Add(matEntityId);
                        UnityEditor.ShaderUtil.CompilePass(mat, metaPassIndex);
                    }
#else
                    descriptor = MaterialPool.ConvertUnityMaterialToMaterialDescriptor(mat, EmissionMode.Realtime);
#endif

                    var newHandle = world.AddMaterial(descriptor, kUVChannel);
                    var newEntry = new Entry
                    {
                        RefCount = 1,
                        WorldHandle = newHandle,
                        Descriptor = descriptor,
                        Material = mat
                    };
                    _entries[matEntityId] = newEntry;
                    return newHandle;
                }
            }

#if UNITY_EDITOR
            public void Update(SurfaceCacheWorld world)
            {
                if (_pendingMetaPassEvals.Count != 0)
                {
                    var evaluatedMaterials = new List<EntityId>();
                    foreach (var matEntityId in _pendingMetaPassEvals)
                    {
                        var entry = _entries[matEntityId];
                        var metaPassIndex = entry.Material.FindPass("Meta");
                        Debug.Assert(metaPassIndex != -1);
                        if (UnityEditor.ShaderUtil.IsPassCompiled(entry.Material, metaPassIndex))
                        {
                            DestroyDescriptorTextures(entry.Descriptor);
                            entry.Descriptor = MaterialPool.ConvertUnityMaterialToMaterialDescriptor(entry.Material, kEmissionMode);
                            world.UpdateMaterial(entry.WorldHandle, entry.Descriptor, kUVChannel);
                            _entries[matEntityId] = entry;
                            evaluatedMaterials.Add(matEntityId);
                        }
                    }

                    foreach (var matEntityId in evaluatedMaterials)
                    {
                        _pendingMetaPassEvals.Remove(matEntityId);
                    }
                }
            }
#endif

            public void Update(EntityId matEntityId, Material material, SurfaceCacheWorld world)
            {
                Debug.Assert(_entries.ContainsKey(matEntityId));

                if (!_pendingMetaPassEvals.Contains(matEntityId))
                {
                    var entry = _entries[matEntityId];
                    DestroyDescriptorTextures(entry.Descriptor);
                    entry.Descriptor = MaterialPool.ConvertUnityMaterialToMaterialDescriptor(material, kEmissionMode);
                    _entries[matEntityId] = entry;

                    world.UpdateMaterial(entry.WorldHandle, in entry.Descriptor, kUVChannel);
                }
            }

            public bool IsReferenced(EntityId matEntityId)
            {
                return _entries.ContainsKey(matEntityId);
            }

            public void Release(EntityId matEntityId, SurfaceCacheWorld world)
            {
                Debug.Assert(_entries.ContainsKey(matEntityId));
                var entry = _entries[matEntityId];

                if (entry.RefCount == 1)
                {
                    RemoveHandle(matEntityId, world);
                }
                else
                {
                    entry.RefCount -= 1;
                    _entries[matEntityId] = entry;
                }
            }

            public void CleanUp(SurfaceCacheWorld world)
            {
                var ids = new EntityId[_entries.Count];

                int i = 0;
                foreach (var key in _entries.Keys)
                    ids[i++] = key;

                foreach (var id in ids)
                    RemoveHandle(id, world);
            }

            void RemoveHandle(EntityId matEntityId, SurfaceCacheWorld world)
            {
                _pendingMetaPassEvals.Remove(matEntityId);
                var entry = _entries[matEntityId];
                world.RemoveMaterial(entry.WorldHandle);
                _entries.Remove(matEntityId);
                DestroyDescriptorTextures(entry.Descriptor);
            }

            static void DestroyDescriptorTextures(in MaterialPool.MaterialDescriptor desc)
            {
                CoreUtils.Destroy(desc.Albedo);
                CoreUtils.Destroy(desc.Emission);
                CoreUtils.Destroy(desc.Transmission);
            }
        }
    }
}

#endif
