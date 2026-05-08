#if SURFACE_CACHE

using System;
using System.Collections.Generic;
using UnityEngine.PathTracing.Core;
using UnityEngine.Rendering.LiveGI;
using InstanceHandle = UnityEngine.PathTracing.Core.Handle<UnityEngine.Rendering.SurfaceCacheWorld.Instance>;
using LightHandle = UnityEngine.PathTracing.Core.Handle<UnityEngine.Rendering.SurfaceCacheWorld.Light>;
using MaterialHandle = UnityEngine.PathTracing.Core.Handle<UnityEngine.PathTracing.Core.MaterialPool.MaterialDescriptor>;
using ObjectDispatcher = UnityEngine.InternalBridge.ObjectDispatcher;

namespace UnityEngine.Rendering.Universal
{
    class SurfaceCacheLegacyWorldAdapter : IDisposable
    {
        // This dictionary maps from Unity EntityID for MeshRenderer or Terrain, to corresponding InstanceHandle for accessing World.
        private readonly Dictionary<EntityId, InstanceHandle> _entityIDToWorldInstanceHandles = new();

        // Same as above but for Lights
        private readonly Dictionary<EntityId, LightHandle> _entityIDToWorldLightHandles = new();

        // Same as above but for Materials
        private Dictionary<EntityId, MaterialHandle> _entityIDToWorldMaterialHandles = new();

        // We also keep track of associated material descriptors, so we can free temporary temporary textures when a material is removed
        private Dictionary<EntityId, MaterialPool.MaterialDescriptor> _entityIDToWorldMaterialDescriptors = new();

        private Material _fallbackMaterial;
        private MaterialPool.MaterialDescriptor _fallbackMaterialDescriptor;
        private MaterialHandle _fallbackMaterialHandle;

#if ENABLE_TERRAIN_MODULE
        // Maps TerrainData EntityID to list of Terrains that use that TerrainData
        private readonly Dictionary<EntityId, List<Terrain>> _terrainDataToTerrains = new();

#if UNITY_EDITOR
        private class TerrainRebuild
        {
            public Terrain terrain;
            public double timeSinceLastChange;
            public EntityId materialEntityId;
        }

        private readonly Dictionary<EntityId, TerrainRebuild> _deferredTerrainRebuilds = new();
        private const double k_TerrainRebuildDelay = 0.5;
#endif
#endif

        public SurfaceCacheLegacyWorldAdapter(SurfaceCacheWorld world, Material fallbackMaterial)
        {
            _fallbackMaterial = fallbackMaterial;
            _fallbackMaterialDescriptor = MaterialPool.ConvertUnityMaterialToMaterialDescriptor(fallbackMaterial, EmissionMode.Realtime);
            _fallbackMaterialHandle = world.AddMaterial(in _fallbackMaterialDescriptor, UVChannel.UV0);
            _entityIDToWorldMaterialHandles.Add(fallbackMaterial.GetEntityId(), _fallbackMaterialHandle);
            _entityIDToWorldMaterialDescriptors.Add(fallbackMaterial.GetEntityId(), _fallbackMaterialDescriptor);
        }

        internal void Update(SceneUpdatesTracker sceneTracker, AmbientMode ambientMode, Material skyboxMaterial,
            Color ambientSkycolor, Color ambientEquatorColor, Color ambientGroundColor, float envIntensityMultiplier,
            SurfaceCacheWorld world)
        {
            const bool filterBakedLights = true;
            var changes = sceneTracker.GetChanges(filterBakedLights);

            UpdateMaterials(world, changes.addedMaterials, changes.removedMaterials, changes.changedMaterials);
            UpdateMeshRenderers(
                world,
                changes.addedMeshRenderers,
                changes.changedMeshRenderers,
                changes.removedMeshRenderers);
#if ENABLE_TERRAIN_MODULE
            UpdateTerrains(
                world,
                changes.addedTerrains,
                changes.changedTerrains,
                changes.removedTerrains,
                changes.addedTerrainData,
                changes.changedTerrainData,
                changes.removedTerrainData);
#endif

            const bool multiplyPunctualLightIntensityByPI = false;
            UpdateLights(world, changes.addedLights, changes.removedLights, changes.changedLights, multiplyPunctualLightIntensityByPI);

            switch (ambientMode)
            {
                case AmbientMode.Skybox:
                    world.SetEnvironmentMode(CubemapRender.Mode.Material);
                    world.SetEnvironmentMaterial(skyboxMaterial);
                    world.SetEnvironmentIntensityMultiplier(envIntensityMultiplier);
                    break;

                case AmbientMode.Flat:
                    world.SetEnvironmentMode(CubemapRender.Mode.Color);
                    world.SetEnvironmentColor(ambientSkycolor);
                    world.SetEnvironmentIntensityMultiplier(1.0f);
                    break;

                case AmbientMode.Trilight:
                    world.SetEnvironmentMode(CubemapRender.Mode.Color);
                    world.SetEnvironmentGradientColors(ambientSkycolor, ambientEquatorColor, ambientGroundColor);
                    world.SetEnvironmentIntensityMultiplier(1.0f);
                    break;

                default:
                    world.SetEnvironmentMode(CubemapRender.Mode.Color);
                    world.SetEnvironmentColor(Color.black);
                    world.SetEnvironmentIntensityMultiplier(1.0f);
                    break;
            }
        }

        private void UpdateMaterials(SurfaceCacheWorld world, List<Material> addedMaterials, List<EntityId> removedMaterials, List<Material> changedMaterials)
        {
            UpdateMaterials(world, _entityIDToWorldMaterialHandles, _entityIDToWorldMaterialDescriptors, addedMaterials, removedMaterials, changedMaterials);
        }

        private static void UpdateMaterials(SurfaceCacheWorld world, Dictionary<EntityId, MaterialHandle> entityIDToHandle, Dictionary<EntityId, MaterialPool.MaterialDescriptor> entityIDToDescriptor, List<Material> addedMaterials, List<EntityId> removedMaterials, List<Material> changedMaterials)
        {
            static void DeleteTemporaryTextures(ref MaterialPool.MaterialDescriptor desc)
            {
                CoreUtils.Destroy(desc.Albedo);
                CoreUtils.Destroy(desc.Emission);
                CoreUtils.Destroy(desc.Transmission);
            }

            foreach (var entityID in removedMaterials)
            {
                // Clean up temporary textures in the descriptor
                Debug.Assert(entityIDToDescriptor.ContainsKey(entityID));
                var descriptor = entityIDToDescriptor[entityID];
                DeleteTemporaryTextures(ref descriptor);
                entityIDToDescriptor.Remove(entityID);

                // Remove the material from the world
                Debug.Assert(entityIDToHandle.ContainsKey(entityID));
                world.RemoveMaterial(entityIDToHandle[entityID]);
                entityIDToHandle.Remove(entityID);
            }

            foreach (var material in addedMaterials)
            {
                // Add material to the world
                var descriptor = MaterialPool.ConvertUnityMaterialToMaterialDescriptor(material, EmissionMode.Realtime);
                var handle = world.AddMaterial(in descriptor, UVChannel.UV0);
                entityIDToHandle.Add(material.GetEntityId(), handle);

                // Keep track of the descriptor
                entityIDToDescriptor.Add(material.GetEntityId(), descriptor);
            }

            foreach (var material in changedMaterials)
            {
                // Clean up temporary textures in the old descriptor
                Debug.Assert(entityIDToDescriptor.ContainsKey(material.GetEntityId()));
                var oldDescriptor = entityIDToDescriptor[material.GetEntityId()];
                DeleteTemporaryTextures(ref oldDescriptor);

                // Update the material in the world using the new descriptor
                Debug.Assert(entityIDToHandle.ContainsKey(material.GetEntityId()));
                var newDescriptor = MaterialPool.ConvertUnityMaterialToMaterialDescriptor(material, EmissionMode.Realtime);
                world.UpdateMaterial(entityIDToHandle[material.GetEntityId()], in newDescriptor, UVChannel.UV0);
                entityIDToDescriptor[material.GetEntityId()] = newDescriptor;
            }
        }

        private void UpdateLights(SurfaceCacheWorld world, List<Light> addedLights, List<EntityId> removedLights,
            List<Light> changedLights, bool multiplyPunctualLightIntensityByPI)
        {
            UpdateLights(world, _entityIDToWorldLightHandles, addedLights, removedLights, changedLights, multiplyPunctualLightIntensityByPI);
        }

        private static void UpdateLights(
            SurfaceCacheWorld world,
            Dictionary<EntityId, LightHandle> entityIDToHandle, List<Light> addedLights, List<EntityId> removedLights,
            List<Light> changedLights,
            bool multiplyPunctualLightIntensityByPI)
        {
            // Remove deleted lights
            LightHandle[] handlesToRemove = new LightHandle[removedLights.Count];
            for (int i = 0; i < removedLights.Count; i++)
            {
                var lightEntityID = removedLights[i];
                handlesToRemove[i] = entityIDToHandle[lightEntityID];
                entityIDToHandle.Remove(lightEntityID);
            }
            world.RemoveLights(handlesToRemove);

            // Add new lights
            var lightDescriptors = ConvertUnityLightsToLightDescriptors(addedLights.ToArray(), multiplyPunctualLightIntensityByPI);
            LightHandle[] addedHandles = world.AddLights(lightDescriptors);
            for (int i = 0; i < addedLights.Count; ++i)
                entityIDToHandle.Add(addedLights[i].GetEntityId(), addedHandles[i]);

            // Update changed lights
            LightHandle[] handlesToUpdate = new LightHandle[changedLights.Count];
            for (int i = 0; i < changedLights.Count; i++)
                handlesToUpdate[i] = entityIDToHandle[changedLights[i].GetEntityId()];

            world.UpdateLights(handlesToUpdate, ConvertUnityLightsToLightDescriptors(changedLights.ToArray(), multiplyPunctualLightIntensityByPI));
        }

        private void UpdateMeshRenderers(
            SurfaceCacheWorld world,
            List<MeshRenderer> addedMeshRenderers,
            List<MeshRendererChanges> changedMeshRenderers,
            List<EntityId> removedMeshRenderers)
        {
            UpdateMeshRenderers(world, _entityIDToWorldInstanceHandles, _entityIDToWorldMaterialHandles, addedMeshRenderers, changedMeshRenderers, removedMeshRenderers, _fallbackMaterial);
        }

        private static void UpdateMeshRenderers(
            SurfaceCacheWorld world,
            Dictionary<EntityId, InstanceHandle> entityIDToInstanceHandle,
            Dictionary<EntityId, MaterialHandle> entityIDToMaterialHandle,
            List<MeshRenderer> addedMeshRenderers,
            List<MeshRendererChanges> changedMeshRenderers,
            List<EntityId> removedMeshRenderers,
            Material fallbackMaterial)
        {
            foreach (var meshRendererEntityID in removedMeshRenderers)
            {
                if (entityIDToInstanceHandle.TryGetValue(meshRendererEntityID, out var instanceHandle))
                {
                    world.RemoveInstance(instanceHandle);
                    entityIDToInstanceHandle.Remove(meshRendererEntityID);
                }
            }

            foreach (var meshRenderer in addedMeshRenderers)
            {
                Debug.Assert(!meshRenderer.isPartOfStaticBatch, "Static Batching is not supported by Surface Cache GI.");

                var mesh = meshRenderer.GetComponent<MeshFilter>().sharedMesh;

                if (mesh == null || mesh.vertexCount == 0)
                    continue;

                var localToWorldMatrix = meshRenderer.transform.localToWorldMatrix;

                var materials = Util.GetMaterials(meshRenderer);
                var materialHandles = new MaterialHandle[materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    var matEntityId = materials[i] == null ? fallbackMaterial.GetEntityId() : materials[i].GetEntityId();
                    materialHandles[i] = entityIDToMaterialHandle[matEntityId];
                }
                uint[] masks = new uint[materials.Length];
                for (int i = 0; i < masks.Length; i++)
                {
                    masks[i] = materials[i] != null ? 1u : 0u;
                }

                InstanceHandle instance = world.AddInstance(mesh, materialHandles, masks, in localToWorldMatrix);
                var entityID = meshRenderer.GetEntityId();
                Debug.Assert(!entityIDToInstanceHandle.ContainsKey(entityID));
                entityIDToInstanceHandle.Add(entityID, instance);
            }

            foreach (var meshRendererUpdate in changedMeshRenderers)
            {
                var meshRenderer = meshRendererUpdate.meshRenderer;
                var gameObject = meshRenderer.gameObject;

                Debug.Assert(entityIDToInstanceHandle.ContainsKey(meshRenderer.GetEntityId()));
                var instanceHandle = entityIDToInstanceHandle[meshRenderer.GetEntityId()];

                if ((meshRendererUpdate.changes & ModifiedProperties.Transform) != 0)
                {
                    world.UpdateInstanceTransform(instanceHandle, gameObject.transform.localToWorldMatrix);
                }

                if ((meshRendererUpdate.changes & ModifiedProperties.Material) != 0)
                {
                    var materials = Util.GetMaterials(meshRenderer);
                    var materialHandles = new MaterialHandle[materials.Length];
                    for (int i = 0; i < materials.Length; i++)
                    {
                        var matEntityId = materials[i] == null ? fallbackMaterial.GetEntityId() : materials[i].GetEntityId();
                        materialHandles[i] = entityIDToMaterialHandle[matEntityId];
                    }

                    world.UpdateInstanceMaterials(instanceHandle, materialHandles);

                    uint[] masks = new uint[materials.Length];
                    for (int i = 0; i < masks.Length; i++)
                    {
                        masks[i] = materials[i] != null ? 1u : 0u;
                    }

                    world.UpdateInstanceMask(instanceHandle, masks);
                }
            }
        }

#if ENABLE_TERRAIN_MODULE
        private void UpdateTerrains(
            SurfaceCacheWorld world,
            List<Terrain> addedTerrains,
            List<TerrainChanges> changedTerrains,
            List<EntityId> removedTerrains,
            List<TerrainData> addedTerrainData,
            List<TerrainDataChanges> changedTerrainData,
            List<EntityId> removedTerrainData)
        {
            UpdateTerrains(world, _entityIDToWorldInstanceHandles, _entityIDToWorldMaterialHandles, addedTerrains, changedTerrains, removedTerrains, addedTerrainData, changedTerrainData, removedTerrainData, _terrainDataToTerrains
#if UNITY_EDITOR
                , _deferredTerrainRebuilds
#endif
                , _fallbackMaterial);
        }

        private static void UpdateTerrains(
            SurfaceCacheWorld world,
            Dictionary<EntityId, InstanceHandle> entityIDToInstanceHandle,
            Dictionary<EntityId, MaterialHandle> entityIDToMaterialHandle,
            List<Terrain> addedTerrains,
            List<TerrainChanges> changedTerrains,
            List<EntityId> removedTerrains,
            List<TerrainData> addedTerrainData,
            List<TerrainDataChanges> changedTerrainData,
            List<EntityId> removedTerrainData,
            Dictionary<EntityId, List<Terrain>> terrainDataToTerrains
#if UNITY_EDITOR
            , Dictionary<EntityId, TerrainRebuild> deferredTerrainRebuilds
#endif
            , Material fallbackMaterial)
        {
            foreach (var terrainEntityID in removedTerrains)
            {
#if UNITY_EDITOR
                deferredTerrainRebuilds.Remove(terrainEntityID);
#endif

                if (entityIDToInstanceHandle.TryGetValue(terrainEntityID, out var instanceHandle))
                {
                    world.RemoveInstance(instanceHandle);
                    entityIDToInstanceHandle.Remove(terrainEntityID);
                }

                foreach (var entry in terrainDataToTerrains)
                {
                    var terrainToRemove = entry.Value.Find(t => t.GetEntityId() == terrainEntityID);
                    if (terrainToRemove != null)
                    {
                        entry.Value.Remove(terrainToRemove);
                        break;
                    }
                }
            }
#if UNITY_EDITOR
            // Rebuild terrains whose heightmap/tree changes have been idle past the delay
            ProcessDeferredTerrainRebuilds(world, entityIDToInstanceHandle, entityIDToMaterialHandle, deferredTerrainRebuilds, fallbackMaterial);
#endif
            // Register existing terrains that were reassigned to this newly seen TerrainData
            foreach (var terrainData in addedTerrainData)
            {
                var terrainDataEntityID = terrainData.GetEntityId();
                if (!terrainDataToTerrains.TryGetValue(terrainDataEntityID, out var terrainList))
                {
                    terrainList = new List<Terrain>();
                    terrainDataToTerrains[terrainDataEntityID] = terrainList;
                }

                var toMove = new List<Terrain>();
                foreach (var entry in terrainDataToTerrains)
                {
                    if (entry.Key == terrainDataEntityID)
                        continue;
                    foreach (var terrain in entry.Value)
                    {
                        if (terrain.terrainData == terrainData && entityIDToInstanceHandle.ContainsKey(terrain.GetEntityId()))
                            toMove.Add(terrain);
                    }
                }

                // Remove each reassigned terrain from its old list, add to this TerrainData's list,
                // and rebuild the world instance so geometry matches the new TerrainData
                foreach (var terrain in toMove)
                {
                    foreach (var entry in terrainDataToTerrains)
                    {
                        if (entry.Value.Remove(terrain))
                            break;
                    }
                    terrainList.Add(terrain);
                    var terrainEntityID = terrain.GetEntityId();
                    if (!entityIDToInstanceHandle.TryGetValue(terrainEntityID, out var instanceHandle))
                        continue;
                    var material = terrain.splatBaseMaterial;
                    var matEntityId = material == null ? fallbackMaterial.GetEntityId() : material.GetEntityId();
                    RebuildTerrainInstance(world, entityIDToInstanceHandle, entityIDToMaterialHandle,
                        terrain, terrainEntityID, instanceHandle, matEntityId, fallbackMaterial);
                }
            }

            foreach (var terrain in addedTerrains)
            {
                var localToWorldMatrix = terrain.transform.localToWorldMatrix;

                var material = terrain.splatBaseMaterial;
                var matEntityId = material == null ? fallbackMaterial.GetEntityId() : material.GetEntityId();
                var materialHandle = entityIDToMaterialHandle[matEntityId];
                uint mask = 1u;

                InstanceHandle instance = world.AddInstance(terrain, materialHandle, mask, in localToWorldMatrix);
                var entityID = terrain.GetEntityId();
                Debug.Assert(!entityIDToInstanceHandle.ContainsKey(entityID));
                entityIDToInstanceHandle.Add(entityID, instance);

                var terrainData = terrain.terrainData;
                if (terrainData != null)
                {
                    var terrainDataEntityID = terrainData.GetEntityId();
                    if (!terrainDataToTerrains.TryGetValue(terrainDataEntityID, out var terrainList))
                    {
                        terrainList = new List<Terrain>();
                        terrainDataToTerrains[terrainDataEntityID] = terrainList;
                    }

                    if (!terrainList.Contains(terrain))
                    {
                        terrainList.Add(terrain);
                    }
                }
            }

            foreach (var terrainUpdate in changedTerrains)
            {
                var terrain = terrainUpdate.terrain;
                var gameObject = terrain.gameObject;

                Debug.Assert(entityIDToInstanceHandle.ContainsKey(terrain.GetEntityId()));
                var instanceHandle = entityIDToInstanceHandle[terrain.GetEntityId()];

                if ((terrainUpdate.changes & ModifiedProperties.Transform) != 0)
                {
                    world.UpdateInstanceTransform(instanceHandle, gameObject.transform.localToWorldMatrix);
                }

                if ((terrainUpdate.changes & ModifiedProperties.Material) != 0)
                {
                    var material = terrain.splatBaseMaterial;

                    var matEntityId = material == null ? fallbackMaterial.GetEntityId() : material.GetEntityId();
                    var materialHandle = entityIDToMaterialHandle[matEntityId];

                    world.UpdateInstanceMaterials(instanceHandle, new MaterialHandle[] { materialHandle });

                    var mask = material != null ? 1u : 0u;

                    world.UpdateInstanceMask(instanceHandle, new uint[] { mask });
                }
            }

            foreach (var terrainDataEntityID in removedTerrainData)
            {
                terrainDataToTerrains.Remove(terrainDataEntityID);
            }

            foreach (var terrainDataUpdate in changedTerrainData)
            {
                var terrainData = terrainDataUpdate.terrainData;
                var changes = terrainDataUpdate.changes;

                var terrainDataEntityID = terrainData.GetEntityId();
                if (!terrainDataToTerrains.TryGetValue(terrainDataEntityID, out var affectedTerrains))
                    continue;

                if ((changes & ModifiedProperties.Heightmap) == 0 && (changes & ModifiedProperties.Holes) == 0)
                    continue;

                foreach (var terrain in affectedTerrains)
                {
                    var terrainEntityID = terrain.GetEntityId();

                    if (!entityIDToInstanceHandle.TryGetValue(terrainEntityID, out var instanceHandle))
                        continue;

                    var material = terrain.splatBaseMaterial;
                    var matEntityId = material == null ? fallbackMaterial.GetEntityId() : material.GetEntityId();

#if UNITY_EDITOR
                    // Delay the removing and re-adding the terrain when in the editor
                    // to avoid lag when the user is actively editing the terrain
                    if (deferredTerrainRebuilds.TryGetValue(terrainEntityID, out var pending))
                    {
                        pending.timeSinceLastChange = UnityEditor.EditorApplication.timeSinceStartup;
                        pending.materialEntityId = matEntityId;
                    }
                    else
                    {
                        deferredTerrainRebuilds[terrainEntityID] = new TerrainRebuild
                        {
                            terrain = terrain,
                            timeSinceLastChange = UnityEditor.EditorApplication.timeSinceStartup,
                            materialEntityId = matEntityId
                        };
                    }
#else
                    // Immediately remove and re-add the terrain to World in a Player
                    RebuildTerrainInstance(world, entityIDToInstanceHandle, entityIDToMaterialHandle,
                        terrain, terrainEntityID, instanceHandle, matEntityId, fallbackMaterial);
#endif
                }
            }
        }

#if UNITY_EDITOR
        private static void ProcessDeferredTerrainRebuilds(
            SurfaceCacheWorld world,
            Dictionary<EntityId, InstanceHandle> entityIDToInstanceHandle,
            Dictionary<EntityId, MaterialHandle> entityIDToMaterialHandle,
            Dictionary<EntityId, TerrainRebuild> terrainRebuilds,
            Material fallbackMaterial)
        {
            // Rebuild terrains that have been idle past the delay (avoids lag while editing)
            var currentTime = UnityEditor.EditorApplication.timeSinceStartup;
            var terrainsToRebuild = new List<EntityId>();

            foreach (var entry in terrainRebuilds)
            {
                if (currentTime - entry.Value.timeSinceLastChange >= k_TerrainRebuildDelay)
                {
                    terrainsToRebuild.Add(entry.Key);
                }
            }

            foreach (var terrainEntityID in terrainsToRebuild)
            {
                var pending = terrainRebuilds[terrainEntityID];

                if (entityIDToInstanceHandle.TryGetValue(terrainEntityID, out var instanceHandle))
                {
                    RebuildTerrainInstance(world, entityIDToInstanceHandle, entityIDToMaterialHandle, pending.terrain, terrainEntityID, instanceHandle, pending.materialEntityId, fallbackMaterial);
                }

                terrainRebuilds.Remove(terrainEntityID);
            }
        }
#endif

        private static void RebuildTerrainInstance(
            SurfaceCacheWorld world,
            Dictionary<EntityId, InstanceHandle> entityIDToInstanceHandle,
            Dictionary<EntityId, MaterialHandle> entityIDToMaterialHandle,
            Terrain terrain,
            EntityId terrainEntityID,
            InstanceHandle instanceHandle,
            EntityId materialEntityId,
            Material fallbackMaterial)
        {
            world.RemoveInstance(instanceHandle);
            entityIDToInstanceHandle.Remove(terrainEntityID);

            var localToWorldMatrix = terrain.transform.localToWorldMatrix;
            var fallbackMaterialHandle = entityIDToMaterialHandle[fallbackMaterial.GetEntityId()];
            var materialHandle = entityIDToMaterialHandle.GetValueOrDefault(materialEntityId, fallbackMaterialHandle);
            uint mask = 1u;

            InstanceHandle instance = world.AddInstance(terrain, materialHandle, mask, in localToWorldMatrix);
            Debug.Assert(!entityIDToInstanceHandle.ContainsKey(terrainEntityID));
            entityIDToInstanceHandle.Add(terrainEntityID, instance);
        }
#endif // ENABLE_TERRAIN_MODULE

        public void Dispose()
        {
            CoreUtils.Destroy(_fallbackMaterialDescriptor.Albedo);
            CoreUtils.Destroy(_fallbackMaterialDescriptor.Emission);
            CoreUtils.Destroy(_fallbackMaterialDescriptor.Transmission);
        }

        internal static SurfaceCacheWorld.LightDescriptor[] ConvertUnityLightsToLightDescriptors(Light[] lights, bool multiplyPunctualLightIntensityByPI)
        {
            var descriptors = new SurfaceCacheWorld.LightDescriptor[lights.Length];
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                ref SurfaceCacheWorld.LightDescriptor descriptor = ref descriptors[i];
                descriptor.Type = light.type;
                descriptor.LinearLightColor = Util.GetLinearLightColor(light, light.bounceIntensity);
                if (multiplyPunctualLightIntensityByPI && Util.IsPunctualLightType(light.type))
                    descriptor.LinearLightColor *= Mathf.PI;
                descriptor.Transform = light.transform.localToWorldMatrix;
                descriptor.ColorTemperature = light.colorTemperature;
                descriptor.OuterSpotAngle = light.spotAngle;
                descriptor.InnerSpotAngle = light.innerSpotAngle;
                descriptor.Range = light.range;
            }
            return descriptors;
        }
    }

    class SurfaceCacheWorldAdapter
    {
        readonly SharedMaterialSet _sharedMaterials;
        readonly LightSet _lights;
        readonly MeshRendererSet _meshRenderers;

        public SurfaceCacheWorldAdapter(ObjectDispatcher objDispatcher, SurfaceCacheWorld world, Material fallbackMaterial)
        {
            _lights = new LightSet();
            _sharedMaterials = new SharedMaterialSet(fallbackMaterial);
            _meshRenderers = new MeshRendererSet(_sharedMaterials, fallbackMaterial, world);

#if UNITY_EDITOR
            objDispatcher.maxDispatchHistoryFramesCount = int.MaxValue;
#endif
            objDispatcher.EnableTypeTracking<MeshRenderer>(ObjectDispatcher.TypeTrackingFlags.SceneObjects);
            objDispatcher.EnableTransformTracking<MeshRenderer>(ObjectDispatcher.TransformTrackingType.GlobalTRS);
            objDispatcher.EnableTypeTracking<Light>(ObjectDispatcher.TypeTrackingFlags.SceneObjects);
            objDispatcher.EnableTransformTracking<Light>(ObjectDispatcher.TransformTrackingType.GlobalTRS);
            objDispatcher.EnableTypeTracking<Material>(ObjectDispatcher.TypeTrackingFlags.SceneObjects | ObjectDispatcher.TypeTrackingFlags.Assets);
        }

        public void CleanUp(SurfaceCacheWorld world)
        {
            _meshRenderers.CleanUp(_sharedMaterials, world);
            _lights.CleanUp(world);
            _sharedMaterials.CleanUp(world);
        }

        public void Update(ObjectDispatcher objDispatcher, AmbientMode ambientMode, Material skyboxMaterial,
            Color ambientSkycolor, Color ambientEquatorColor, Color ambientGroundColor, float envIntensityMultiplier,
            SurfaceCacheWorld world)
        {
            UpdateMeshRenderers(objDispatcher, world);
            UpdateLights(objDispatcher, world);
            UpdateMaterials(objDispatcher, world);
            UpdateEnvironment(
                ambientMode,
                skyboxMaterial,
                ambientSkycolor,
                ambientEquatorColor,
                ambientGroundColor,
                envIntensityMultiplier,
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

        void UpdateEnvironment(AmbientMode ambientMode, Material skyboxMaterial,
            Color ambientSkycolor, Color ambientEquatorColor, Color ambientGroundColor, float envIntensityMultiplier,
            SurfaceCacheWorld world)
        {
            switch (ambientMode)
            {
                case AmbientMode.Skybox:
                    world.SetEnvironmentMode(CubemapRender.Mode.Material);
                    world.SetEnvironmentMaterial(skyboxMaterial);
                    world.SetEnvironmentIntensityMultiplier(envIntensityMultiplier);
                    break;

                case AmbientMode.Flat:
                    world.SetEnvironmentMode(CubemapRender.Mode.Color);
                    world.SetEnvironmentColor(ambientSkycolor);
                    world.SetEnvironmentIntensityMultiplier(1.0f);
                    break;

                case AmbientMode.Trilight:
                    world.SetEnvironmentMode(CubemapRender.Mode.Color);
                    world.SetEnvironmentGradientColors(ambientSkycolor, ambientEquatorColor, ambientGroundColor);
                    world.SetEnvironmentIntensityMultiplier(1.0f);
                    break;

                default:
                    world.SetEnvironmentMode(CubemapRender.Mode.Color);
                    world.SetEnvironmentColor(Color.black);
                    world.SetEnvironmentIntensityMultiplier(1.0f);
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
                var shouldExist = light.gameObject.activeInHierarchy && light.enabled && !light.bakingOutput.isBaked;

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
