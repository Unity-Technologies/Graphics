#if SURFACE_CACHE_SUPPORTED || UNITY_EDITOR

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.PathTracing.Core;
using InstanceHandle = UnityEngine.PathTracing.Core.Handle<UnityEngine.Rendering.SurfaceCacheWorld.Instance>;
using LightHandle = UnityEngine.PathTracing.Core.Handle<UnityEngine.Rendering.SurfaceCacheWorld.Light>;
using MaterialHandle = UnityEngine.PathTracing.Core.Handle<UnityEngine.PathTracing.Core.MaterialPool.MaterialDescriptor>;

namespace UnityEngine.Rendering.Universal
{
    class SurfaceCacheWorldAdapter
    {
        readonly SharedMaterialSet _sharedMaterials;
        readonly LightSet _lights;
        readonly InstanceSet<EntityId, MeshRendererSource> _meshRenderers;
        readonly EntityInstanceSet _entityInstances;
#if ENABLE_TERRAIN_MODULE
        readonly TerrainSet _terrains;
        // Per-frame set of TerrainData EntityIds that were reported changed by ObjectDispatcher.
        readonly HashSet<EntityId> _dirtyTerrainDataThisFrame = new();
#if UNITY_EDITOR
        internal const uint k_TerrainRebuildDelayInTicks = 30;
#endif
#endif

        // Defaults to Everything so that, absent a Surface Cache volume override, every instance contributes.
        uint _renderingLayerMaskFilter = 0xFFFFFFFF;

        public SurfaceCacheWorldAdapter(Material fallbackMaterial)
        {
            _lights = new LightSet();
            _sharedMaterials = new SharedMaterialSet(fallbackMaterial);
            _meshRenderers = new InstanceSet<EntityId, MeshRendererSource>(fallbackMaterial);
            _entityInstances = new EntityInstanceSet(fallbackMaterial);
#if ENABLE_TERRAIN_MODULE
            _terrains = new TerrainSet(fallbackMaterial);
#endif
        }

        public void CleanUp(SurfaceCacheWorld world)
        {
            _meshRenderers.CleanUp(_sharedMaterials, world);
            _entityInstances.CleanUp(_sharedMaterials, world);
#if ENABLE_TERRAIN_MODULE
            _terrains.CleanUp(_sharedMaterials, world);
#endif
            _lights.CleanUp(world);
            _sharedMaterials.CleanUp(world);
        }

#if ENABLE_TERRAIN_MODULE && UNITY_EDITOR
        internal int GetDeferredTerrainRebuildCount()
        {
            return _terrains.DeferredRebuildCount;
        }
#endif

#if UNITY_EDITOR
        internal bool TryGetEntityInstanceAppliedLocalToWorld(EntityId key, out Matrix4x4 localToWorld)
        {
            return _entityInstances.TryGetAppliedLocalToWorld(key, out localToWorld);
        }
#endif

        public void Update(SurfaceCacheWorldChangeSet changes, AmbientMode ambientMode, Material skyboxMaterial,
            Color ambientSkycolor, Color ambientEquatorColor, Color ambientGroundColor, float materialEnvIntensityMultiplier,
            bool divideEnvIntensityByPI, uint renderingLayerMaskFilter, SurfaceCacheWorld world)
        {
            using var _ = k_WorldAdapterUpdateMarker.Auto();

            bool filterChanged = renderingLayerMaskFilter != _renderingLayerMaskFilter;
            _renderingLayerMaskFilter = renderingLayerMaskFilter;

            UpdateMeshRenderers(changes.MeshRendererTransformChangedList, changes.MeshRendererChangedList, changes.MeshRendererDestroyedList, world);
            UpdateEntityInstances(changes.EntityInstanceTransformChangedList, changes.EntityInstanceChangedList, changes.EntityInstanceDestroyedList, world);
#if ENABLE_TERRAIN_MODULE
            UpdateTerrains(changes.TerrainTransformChangedList, changes.TerrainChangedList, changes.TerrainDestroyedList, changes.TerrainDataChangedList, world);
#endif
            UpdateLights(changes.LightTransformChangedList, changes.LightChangedList, changes.LightDestroyedList, world);

            // A change to the global rendering layer mask filter is not reported as a per-object change by the
            // ObjectDispatcher, so re-evaluate every known instance to add the newly-included and remove the
            // newly-excluded ones. Filter changes are rare (authoring), so this rescan is acceptable; the common
            // unchanged path skips it entirely.
            if (filterChanged)
            {
                _meshRenderers.ReevaluateAll(_sharedMaterials, world, _renderingLayerMaskFilter);
                _entityInstances.ReevaluateAll(_sharedMaterials, world, _renderingLayerMaskFilter);
#if ENABLE_TERRAIN_MODULE
                _terrains.ReevaluateAll(_sharedMaterials, world, _renderingLayerMaskFilter);
#endif
                _lights.ReevaluateAll(world, _renderingLayerMaskFilter);
            }

            UpdateMaterials(changes.MaterialChangedList, world);
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

        static readonly Unity.Profiling.ProfilerMarker k_WorldAdapterUpdateMarker = new("SurfaceCache.WorldAdapterUpdate");
        static readonly Unity.Profiling.ProfilerMarker k_GameObjectInstanceUpdateMarker = new("SurfaceCache.GameObjectInstanceUpdate");
        static readonly Unity.Profiling.ProfilerMarker k_EntityInstanceUpdateMarker = new("SurfaceCache.EntityInstanceUpdate");
        static readonly Unity.Profiling.ProfilerMarker k_GameObjectLightUpdateMarker = new("SurfaceCache.GameObjectLightUpdate");
        static readonly Unity.Profiling.ProfilerMarker k_MaterialUpdateMarker = new("SurfaceCache.MaterialUpdate");
        static readonly Unity.Profiling.ProfilerMarker k_EnvironmentUpdateMarker = new("SurfaceCache.EnvironmentUpdate");
#if ENABLE_TERRAIN_MODULE
        static readonly Unity.Profiling.ProfilerMarker k_GameObjectTerrainUpdateMarker = new("SurfaceCache.GameObjectTerrainUpdate");
#endif

        void UpdateMeshRenderers(IEnumerable<Component> transformChanged, IEnumerable<Object> changed, IEnumerable<EntityId> destroyed, SurfaceCacheWorld world)
        {
            using var _ = k_GameObjectInstanceUpdateMarker.Auto();

            foreach (var component in transformChanged)
            {
                var source = new MeshRendererSource((MeshRenderer)component);
                _meshRenderers.RefreshTransform(source, _sharedMaterials, world, _renderingLayerMaskFilter);
            }

            foreach (var component in changed)
            {
                var source = new MeshRendererSource((MeshRenderer)component);
                _meshRenderers.Refresh(source, _sharedMaterials, world, _renderingLayerMaskFilter);
            }

            foreach (var entityId in destroyed)
                _meshRenderers.TryRemove(entityId, _sharedMaterials, world);
        }

        void UpdateEntityInstances(NativeArray<SurfaceCacheEntityTransformRecord> transformChanged, IEnumerable<SurfaceCacheEntityInstanceRecord> changed, IEnumerable<EntityId> destroyed, SurfaceCacheWorld world)
        {
            using var _ = k_EntityInstanceUpdateMarker.Auto();

            if (transformChanged.IsCreated)
            {
                for (int i = 0; i < transformChanged.Length; i++)
                {
                    var record = transformChanged[i];
                    _entityInstances.UpdateTransform(record.Key, record.LocalToWorld, world);
                }
            }

            foreach (var record in changed)
                _entityInstances.Refresh(new EntityRecordSource(record), _sharedMaterials, world, _renderingLayerMaskFilter);

            foreach (var key in destroyed)
                _entityInstances.TryRemove(key, _sharedMaterials, world);
        }

#if ENABLE_TERRAIN_MODULE
        void UpdateTerrains(IEnumerable<Component> transformChanged, IEnumerable<Object> changed, IEnumerable<EntityId> destroyed, IEnumerable<Object> terrainDataChanged, SurfaceCacheWorld world)
        {
            using var _ = k_GameObjectTerrainUpdateMarker.Auto();

            Debug.Assert(_dirtyTerrainDataThisFrame.Count == 0);

            foreach (var obj in terrainDataChanged)
                _dirtyTerrainDataThisFrame.Add(((TerrainData)obj).GetEntityId());

            foreach (var component in transformChanged)
                _terrains.Refresh((Terrain)component, _sharedMaterials, world, true, _renderingLayerMaskFilter);

            foreach (var component in changed)
                _terrains.Refresh((Terrain)component, _sharedMaterials, world, false, _renderingLayerMaskFilter);

            foreach (var entityId in destroyed)
                _terrains.TryRemove(entityId, _sharedMaterials, world);

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
            using var _ = k_EnvironmentUpdateMarker.Auto();

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

        void UpdateLights(IEnumerable<Component> transformChanged, IEnumerable<Object> changed, IEnumerable<EntityId> destroyed, SurfaceCacheWorld world)
        {
            using var _ = k_GameObjectLightUpdateMarker.Auto();

            foreach (var component in transformChanged)
                _lights.Refresh((Light)component, world, _renderingLayerMaskFilter);

            foreach (var component in changed)
                _lights.Refresh((Light)component, world, _renderingLayerMaskFilter);

            foreach (var entityId in destroyed)
                _lights.TryRemove(entityId, world);
        }

        void UpdateMaterials(IEnumerable<Object> changed, SurfaceCacheWorld world)
        {
            using var _ = k_MaterialUpdateMarker.Auto();

#if UNITY_EDITOR
            _sharedMaterials.Update(world);
#endif

            foreach (var obj in changed)
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

        interface IInstanceSource<TKey>
        {
            TKey Key { get; }
            bool Visible { get; }
            Mesh Mesh { get; }
            uint RenderingLayerMask { get; }
            Matrix4x4 LocalToWorld { get; }
            string OwnerName { get; }
            // May fill and return the given scratch list to avoid heap allocation.
            IReadOnlyList<Material> GetMaterials(List<Material> scratch);
        }

        readonly struct MeshRendererSource : IInstanceSource<EntityId>
        {
            readonly MeshRenderer _renderer;

            public MeshRendererSource(MeshRenderer renderer)
            {
                _renderer = renderer;
            }

            public EntityId Key => _renderer.GetEntityId();

            // In an open subscene the live-converted entities are the source of truth, not the authoring renderers.
            public bool Visible => _renderer.enabled && _renderer.gameObject.activeInHierarchy && !_renderer.gameObject.scene.isSubScene;

            public Mesh Mesh
            {
                get
                {
                    Debug.Assert(!_renderer.isPartOfStaticBatch, "Static Batching is not supported by Surface Cache GI.");
                    return _renderer.TryGetComponent(out MeshFilter meshFilter) ? meshFilter.sharedMesh : null;
                }
            }

            public uint RenderingLayerMask => _renderer.renderingLayerMask;

            public Matrix4x4 LocalToWorld => _renderer.transform.localToWorldMatrix;

            public string OwnerName => _renderer.gameObject.name;

            public IReadOnlyList<Material> GetMaterials(List<Material> scratch)
            {
                _renderer.GetSharedMaterials(scratch);
                return scratch;
            }
        }

        readonly struct EntityRecordSource : IInstanceSource<EntityId>
        {
            readonly SurfaceCacheEntityInstanceRecord _record;

            public EntityRecordSource(in SurfaceCacheEntityInstanceRecord record)
            {
                _record = record;
            }

            public EntityId Key => _record.Key;
            public bool Visible => _record.Visible;
            public Mesh Mesh => _record.Mesh;
            public uint RenderingLayerMask => _record.RenderingLayerMask;
            public Matrix4x4 LocalToWorld => _record.LocalToWorld;
            public string OwnerName => _record.Key.ToString();

            public IReadOnlyList<Material> GetMaterials(List<Material> scratch)
            {
                return _record.Materials ?? Array.Empty<Material>();
            }

            public EntityRecordSource WithLocalToWorld(in Matrix4x4 localToWorld)
            {
                var record = _record;
                record.LocalToWorld = localToWorld;
                return new EntityRecordSource(record);
            }
        }

        // Ensures the right instances are present in the SurfaceCacheWorld at the right time.
        class InstanceSet<TKey, TSource>
            where TKey : IEquatable<TKey>
            where TSource : struct, IInstanceSource<TKey>
        {
            readonly Dictionary<TKey, int> _slots = new();
            readonly List<TKey> _keys = new();
            readonly List<TSource> _sources = new();
            readonly List<bool> _inWorld = new();
            readonly List<InstanceHandle> _handles = new();
            readonly List<Mesh> _meshes = new();
            // The transform last applied to the world instance.
            readonly List<Matrix4x4> _localToWorlds = new();
            // Materials provided by the source the last time we checked ("null materials" are EntityId.None).
            readonly List<EntityId[]> _inputMaterialIds = new();
            // Materials currently acquired from the shared set (may include the fallback material).
            readonly List<EntityId[]> _acquiredMaterialIds = new();

            readonly List<TSource> _reevaluateScratch = new();
            readonly Material _fallbackMaterial;
            readonly List<Material> _materialScratch = new();

            public InstanceSet(Material fallbackMaterial)
            {
                _fallbackMaterial = fallbackMaterial;
            }

            public void CleanUp(SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                for (int i = 0; i < _keys.Count; i++)
                {
                    if (_inWorld[i])
                        RemoveFromWorld(i, sharedMaterials, world);
                }
            }

#if UNITY_EDITOR
            public bool TryGetAppliedLocalToWorld(TKey key, out Matrix4x4 localToWorld)
            {
                if (_slots.TryGetValue(key, out int slot) && _inWorld[slot])
                {
                    localToWorld = _localToWorlds[slot];
                    return true;
                }

                localToWorld = default;
                return false;
            }
#endif

            // Registration is not re-evaluated here. Everything the predicate reads dispatches as a type change,
            // so it arrives on the changed list instead.
            public void RefreshTransform(in TSource source, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world, uint renderingLayerMaskFilter)
            {
                if (TryGetSlot(source.Key, out int slot))
                {
                    ApplyTransform(slot, source.LocalToWorld, world);
                    return;
                }

                Refresh(source, sharedMaterials, world, renderingLayerMaskFilter);
            }

            public void Refresh(in TSource source, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world, uint renderingLayerMaskFilter)
            {
                var key = source.Key;
                bool isRegistered = _slots.TryGetValue(key, out int slot);
                bool isInWorld = isRegistered && _inWorld[slot];

                var mesh = source.Mesh;
                bool shouldBeRegistered = source.Visible && mesh != null && mesh.vertexCount != 0;
                bool matchesWorldFilter = (source.RenderingLayerMask & renderingLayerMaskFilter) != 0;
                bool shouldBeInWorld = shouldBeRegistered && matchesWorldFilter;

                bool shouldAddToWorld = shouldBeInWorld && !isInWorld;
                bool shouldUpdateInWorld = shouldBeInWorld && isInWorld;
                bool shouldRemoveFromWorld = isInWorld && !shouldBeInWorld;
                bool shouldUnregister = isRegistered && !shouldBeRegistered;
                bool shouldRegister = shouldBeRegistered && !isRegistered;

                // Note that ordering of the following operations matter.
                if (shouldRemoveFromWorld)
                    RemoveFromWorld(slot, sharedMaterials, world);

                if (shouldUnregister)
                    Unregister(slot);
                else if (shouldRegister)
                    slot = Register(key, source);
                else if (isRegistered)
                    _sources[slot] = source;

                if (shouldAddToWorld)
                    AddToWorld(slot, source, mesh, sharedMaterials, world);
                else if (shouldUpdateInWorld)
                    UpdateInWorld(slot, source, mesh, sharedMaterials, world);
            }

            public void TryRemove(TKey key, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                if (!_slots.TryGetValue(key, out int slot))
                    return;

                if (_inWorld[slot])
                    RemoveFromWorld(slot, sharedMaterials, world);
                Unregister(slot);
            }

            public void ReevaluateAll(SharedMaterialSet sharedMaterials, SurfaceCacheWorld world, uint renderingLayerMaskFilter)
            {
                // A copy is required because we mutate below.
                Debug.Assert(_reevaluateScratch.Count == 0);
                _reevaluateScratch.AddRange(_sources);

                foreach (var source in _reevaluateScratch)
                {
                    Refresh(source, sharedMaterials, world, renderingLayerMaskFilter);
                }

                _reevaluateScratch.Clear();
            }

            protected bool TryGetSlot(TKey key, out int slot)
            {
                return _slots.TryGetValue(key, out slot);
            }

            protected TSource GetSource(int slot)
            {
                return _sources[slot];
            }

            protected void SetSource(int slot, in TSource source)
            {
                _sources[slot] = source;
            }

            protected void ApplyTransform(int slot, in Matrix4x4 localToWorld, SurfaceCacheWorld world)
            {
                if (!_inWorld[slot] || localToWorld == _localToWorlds[slot])
                    return;

                world.UpdateInstanceTransform(_handles[slot], localToWorld);
                _localToWorlds[slot] = localToWorld;
            }

            int Register(TKey key, in TSource source)
            {
                int slot = _keys.Count;
                _slots[key] = slot;
                _keys.Add(key);
                _sources.Add(source);
                _inWorld.Add(false);
                _handles.Add(default);
                _meshes.Add(null);
                _localToWorlds.Add(default);
                _inputMaterialIds.Add(null);
                _acquiredMaterialIds.Add(null);
                return slot;
            }

            void Unregister(int slot)
            {
                Debug.Assert(!_inWorld[slot]);
                int last = _keys.Count - 1;
                _slots.Remove(_keys[slot]);
                if (slot != last)
                {
                    _keys[slot] = _keys[last];
                    _sources[slot] = _sources[last];
                    _inWorld[slot] = _inWorld[last];
                    _handles[slot] = _handles[last];
                    _meshes[slot] = _meshes[last];
                    _localToWorlds[slot] = _localToWorlds[last];
                    _inputMaterialIds[slot] = _inputMaterialIds[last];
                    _acquiredMaterialIds[slot] = _acquiredMaterialIds[last];
                    _slots[_keys[slot]] = slot;
                }
                _keys.RemoveAt(last);
                _sources.RemoveAt(last);
                _inWorld.RemoveAt(last);
                _handles.RemoveAt(last);
                _meshes.RemoveAt(last);
                _localToWorlds.RemoveAt(last);
                _inputMaterialIds.RemoveAt(last);
                _acquiredMaterialIds.RemoveAt(last);
            }

            void AddToWorld(int slot, in TSource source, Mesh mesh, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                Debug.Assert(!_inWorld[slot]);
                Debug.Assert(mesh != null && mesh.vertexCount != 0);

                Debug.Assert(_materialScratch.Count == 0);
                var inputMats = source.GetMaterials(_materialScratch);

                Span<EntityId> inputMatIds = stackalloc EntityId[mesh.subMeshCount];
                ResolveMaterialIds(inputMats, inputMatIds);

                Span<MaterialHandle> acquiredMatHandles = stackalloc MaterialHandle[mesh.subMeshCount];
                Span<EntityId> acquiredMatIds = stackalloc EntityId[mesh.subMeshCount];
                Span<uint> masks = stackalloc uint[mesh.subMeshCount];
                ResolveMaterialHandlesAndMasks(world, sharedMaterials, source, inputMats, acquiredMatHandles, acquiredMatIds, masks);

                var localToWorld = source.LocalToWorld;
                _handles[slot] = world.AddInstance(mesh, acquiredMatHandles, masks, localToWorld);
                _meshes[slot] = mesh;
                _localToWorlds[slot] = localToWorld;
                _inputMaterialIds[slot] = inputMatIds.ToArray();
                _acquiredMaterialIds[slot] = acquiredMatIds.ToArray();
                _inWorld[slot] = true;

                _materialScratch.Clear();
            }

            void UpdateInWorld(int slot, in TSource source, Mesh mesh, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                if (_meshes[slot] != mesh)
                {
                    RemoveFromWorld(slot, sharedMaterials, world);
                    AddToWorld(slot, source, mesh, sharedMaterials, world);
                    return;
                }

                Debug.Assert(mesh != null && mesh.vertexCount != 0);
                Debug.Assert(_materialScratch.Count == 0);
                var inputMats = source.GetMaterials(_materialScratch);

                Span<EntityId> inputMatIds = stackalloc EntityId[mesh.subMeshCount];
                ResolveMaterialIds(inputMats, inputMatIds);

                bool materialsChanged = !((ReadOnlySpan<EntityId>)inputMatIds).SequenceEqual(_inputMaterialIds[slot]);
                if (materialsChanged)
                {
                    Span<MaterialHandle> acquiredMatHandles = stackalloc MaterialHandle[mesh.subMeshCount];
                    Span<EntityId> acquiredMatIds = stackalloc EntityId[mesh.subMeshCount];
                    Span<uint> masks = stackalloc uint[mesh.subMeshCount];
                    ResolveMaterialHandlesAndMasks(world, sharedMaterials, source, inputMats, acquiredMatHandles, acquiredMatIds, masks);

                    world.UpdateInstanceMaterials(_handles[slot], acquiredMatHandles);
                    world.UpdateInstanceMask(_handles[slot], masks);

                    foreach (var matEntityId in _acquiredMaterialIds[slot])
                        sharedMaterials.Release(matEntityId, world);

                    _inputMaterialIds[slot] = inputMatIds.ToArray();
                    _acquiredMaterialIds[slot] = acquiredMatIds.ToArray();
                }

                _materialScratch.Clear();

                ApplyTransform(slot, source.LocalToWorld, world);
            }

            void ResolveMaterialHandlesAndMasks(SurfaceCacheWorld world, SharedMaterialSet sharedMaterials, in TSource source, IReadOnlyList<Material> inputMats, Span<MaterialHandle> acquiredMaterialHandles, Span<EntityId> acquiredMaterialEntityIds, Span<uint> masks)
            {
                for (int i = 0; i < acquiredMaterialHandles.Length; i++)
                {
                    Material inputMat = i < inputMats.Count ? inputMats[i] : null;

                    masks[i] = inputMat == null ? 0u : 1u;

                    bool matIsSet = inputMat != null;
                    bool matIsSetAndHasMetaPass = matIsSet && inputMat.FindPass("Meta") != -1;
                    bool matIsUnsetOrNoMetaPass = !matIsSetAndHasMetaPass;
                    bool matIsSetAndNoMetaPass = matIsSet && matIsUnsetOrNoMetaPass;

                    if (matIsSetAndNoMetaPass)
                        Debug.LogError($"The material '{inputMat.name}' used by '{source.OwnerName}' does not have a 'Meta' shader pass and cannot be used by Surface Cache Global Illumination. A fallback material will be used for this material instead.", inputMat);

                    // Acquiring the fallback even for unset materials buys the assumption that a handle always exists.
                    var acquiredMat = inputMat;
                    if (matIsUnsetOrNoMetaPass)
                        acquiredMat = _fallbackMaterial;

                    var acquiredMatEntityId = acquiredMat.GetEntityId();
                    acquiredMaterialHandles[i] = sharedMaterials.Acquire(acquiredMatEntityId, acquiredMat, world);
                    acquiredMaterialEntityIds[i] = acquiredMatEntityId;
                }
            }

            void RemoveFromWorld(int slot, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                Debug.Assert(_inWorld[slot]);
                world.RemoveInstance(_handles[slot]);
                foreach (var matEntityId in _acquiredMaterialIds[slot])
                    sharedMaterials.Release(matEntityId, world);
                _handles[slot] = default;
                _meshes[slot] = null;
                _localToWorlds[slot] = default;
                _inputMaterialIds[slot] = null;
                _acquiredMaterialIds[slot] = null;
                _inWorld[slot] = false;
            }
        }

        // Transform records carry no mesh or material data and cannot go through Refresh.
        sealed class EntityInstanceSet : InstanceSet<EntityId, EntityRecordSource>
        {
            public EntityInstanceSet(Material fallbackMaterial)
                : base(fallbackMaterial)
            {
            }

            public void UpdateTransform(EntityId key, in Matrix4x4 localToWorld, SurfaceCacheWorld world)
            {
                // Transform records may arrive for entities that were filtered out (no mesh, hidden, masked).
                if (!TryGetSlot(key, out int slot))
                    return;

                SetSource(slot, GetSource(slot).WithLocalToWorld(localToWorld));
                ApplyTransform(slot, localToWorld, world);
            }
        }

        static void ResolveMaterialIds(IReadOnlyList<Material> materials, Span<EntityId> materialEntityIds)
        {
            for (int i = 0; i < materialEntityIds.Length; i++)
            {
                Material material = i < materials.Count ? materials[i] : null;
                materialEntityIds[i] = material != null ? material.GetEntityId() : EntityId.None;
            }
        }

#if ENABLE_TERRAIN_MODULE
        // Ensures the right Terrains are present in the SurfaceCacheWorld at the right time, according to game object
        // state (enabled/active), terrain data validity, and the rendering layer mask filter.
        class TerrainSet
        {
            readonly Dictionary<EntityId, int> _slots = new();
            readonly List<EntityId> _entityIds = new();
            readonly List<Terrain> _components = new();
            readonly List<bool> _inWorld = new();
            readonly List<InstanceHandle> _handles = new();
            // The material assigned the last time we checked. This may be EntityId.None.
            readonly List<EntityId> _inputMaterialIds = new();
            // The material currently acquired (may be the fallback material).
            readonly List<EntityId> _acquiredMaterialIds = new();
            readonly List<EntityId> _terrainDataIds = new();

            readonly List<Terrain> _reevaluateScratch = new();
            // Scratch buffer reused by RebuildConsumers to snapshot ids before mutating the world.
            readonly List<EntityId> _terrainEntityIdScratch = new();

            readonly Material _fallbackMaterial;

#if UNITY_EDITOR
            struct DeferredRebuild
            {
                public uint TickAtLastChange;
            }

            uint _tickCounter;
            readonly Dictionary<EntityId, DeferredRebuild> _deferredRebuilds = new();
            readonly List<EntityId> _deferredRebuildScratch = new();
#endif

            public TerrainSet(Material fallbackMaterial)
            {
                _fallbackMaterial = fallbackMaterial;
            }

            public void CleanUp(SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                for (int i = 0; i < _components.Count; i++)
                {
                    if (_inWorld[i])
                        RemoveFromWorld(i, sharedMaterials, world);
                }
            }

            public void Refresh(Terrain terrain, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world, bool transformChange, uint renderingLayerMaskFilter)
            {
                var id = terrain.GetEntityId();
                bool isRegistered = _slots.TryGetValue(id, out int slot);
                bool isInWorld = isRegistered && _inWorld[slot];

                bool shouldBeRegistered = terrain.isActiveAndEnabled && terrain.terrainData != null;
                bool matchesWorldFilter = (terrain.renderingLayerMask & renderingLayerMaskFilter) != 0;
                bool shouldBeInWorld = shouldBeRegistered && matchesWorldFilter;

                bool shouldAddToWorld = shouldBeInWorld && !isInWorld;
                bool shouldUpdateInWorld = shouldBeInWorld && isInWorld;
                bool shouldRemoveFromWorld = isInWorld && !shouldBeInWorld;
                bool shouldUnregister = isRegistered && !shouldBeRegistered;
                bool shouldRegister = shouldBeRegistered && !isRegistered;

                // Note that ordering of the following operations matter.
                if (shouldRemoveFromWorld)
                    RemoveFromWorld(slot, sharedMaterials, world);

                if (shouldUnregister)
                {
#if UNITY_EDITOR
                    _deferredRebuilds.Remove(id);
#endif
                    Unregister(slot);
                }
                else if (shouldRegister)
                {
                    slot = Register(id, terrain);
                }

                if (shouldAddToWorld)
                    AddToWorld(slot, terrain, sharedMaterials, world);
                else if (shouldUpdateInWorld)
                    UpdateInWorld(slot, terrain, transformChange, sharedMaterials, world);
            }

            public void TryRemove(EntityId terrainEntityId, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                if (!_slots.TryGetValue(terrainEntityId, out int slot))
                    return;

                if (_inWorld[slot])
                    RemoveFromWorld(slot, sharedMaterials, world);
                Unregister(slot);
#if UNITY_EDITOR
                _deferredRebuilds.Remove(terrainEntityId);
#endif
            }

            public void ReevaluateAll(SharedMaterialSet sharedMaterials, SurfaceCacheWorld world, uint renderingLayerMaskFilter)
            {
                // A copy is required because we mutate below.
                Debug.Assert(_reevaluateScratch.Count == 0);
                _reevaluateScratch.AddRange(_components);

                foreach (var component in _reevaluateScratch)
                {
                    Debug.Assert(component != null);
                    Refresh(component, sharedMaterials, world, false, renderingLayerMaskFilter);
                }

                _reevaluateScratch.Clear();
            }

            // For every in-world Terrain whose TerrainData appears in the dirty set, schedule (Editor) or perform (Player) a geometry rebuild.
            public void RebuildConsumers(HashSet<EntityId> dirtyTerrainDataIds, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                _terrainEntityIdScratch.Clear();
                for (int i = 0; i < _components.Count; i++)
                {
                    if (_inWorld[i] && dirtyTerrainDataIds.Contains(_terrainDataIds[i]))
                        _terrainEntityIdScratch.Add(_entityIds[i]);
                }

                foreach (var terrainEntityId in _terrainEntityIdScratch)
                {
#if UNITY_EDITOR
                    _deferredRebuilds[terrainEntityId] = new DeferredRebuild
                    {
                        TickAtLastChange = _tickCounter,
                    };
#else
                    RebuildInstance(_slots[terrainEntityId], sharedMaterials, world);
#endif
                }
            }

#if UNITY_EDITOR
            public int DeferredRebuildCount => _deferredRebuilds.Count;

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
                    if (_slots.TryGetValue(entityId, out int slot) && _inWorld[slot])
                        RebuildInstance(slot, sharedMaterials, world);
                }
            }
#endif

            int Register(EntityId id, Terrain component)
            {
                int slot = _components.Count;
                _slots[id] = slot;
                _entityIds.Add(id);
                _components.Add(component);
                _inWorld.Add(false);
                _handles.Add(default);
                _inputMaterialIds.Add(EntityId.None);
                _acquiredMaterialIds.Add(EntityId.None);
                _terrainDataIds.Add(EntityId.None);
                return slot;
            }

            void Unregister(int slot)
            {
                Debug.Assert(!_inWorld[slot]);
                int last = _components.Count - 1;
                _slots.Remove(_entityIds[slot]);
                if (slot != last)
                {
                    _entityIds[slot] = _entityIds[last];
                    _components[slot] = _components[last];
                    _inWorld[slot] = _inWorld[last];
                    _handles[slot] = _handles[last];
                    _inputMaterialIds[slot] = _inputMaterialIds[last];
                    _acquiredMaterialIds[slot] = _acquiredMaterialIds[last];
                    _terrainDataIds[slot] = _terrainDataIds[last];
                    _slots[_entityIds[slot]] = slot;
                }
                _entityIds.RemoveAt(last);
                _components.RemoveAt(last);
                _inWorld.RemoveAt(last);
                _handles.RemoveAt(last);
                _inputMaterialIds.RemoveAt(last);
                _acquiredMaterialIds.RemoveAt(last);
                _terrainDataIds.RemoveAt(last);
            }

            void AddToWorld(int slot, Terrain terrain, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                Debug.Assert(!_inWorld[slot]);
                var terrainData = terrain.terrainData;
                Debug.Assert(terrainData != null);
                var inputMaterial = terrain.splatBaseMaterial;
                EntityId inputMatEntityId = inputMaterial != null ? inputMaterial.GetEntityId() : EntityId.None;
                ResolveMaterialHandleAndMask(terrain, inputMaterial, inputMatEntityId, sharedMaterials, world, _fallbackMaterial,
                    out EntityId acquiredMatEntityId, out MaterialHandle matHandle, out uint mask);

                _handles[slot] = world.AddInstance(terrain, matHandle, mask, terrain.transform.localToWorldMatrix);
                _inputMaterialIds[slot] = inputMatEntityId;
                _acquiredMaterialIds[slot] = acquiredMatEntityId;
                _terrainDataIds[slot] = terrainData.GetEntityId();
                _inWorld[slot] = true;
            }

            void UpdateInWorld(int slot, Terrain terrain, bool transformChange, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                if (transformChange)
                {
                    world.UpdateInstanceTransform(_handles[slot], terrain.transform.localToWorldMatrix);
                    return;
                }

                var newTerrainDataEntityId = terrain.terrainData.GetEntityId();
                if (newTerrainDataEntityId != _terrainDataIds[slot])
                {
                    RebuildInstance(slot, sharedMaterials, world);
                    return;
                }

                var inputMat = terrain.splatBaseMaterial;
                var inputMatEntityId = inputMat != null ? inputMat.GetEntityId() : EntityId.None;
                if (inputMatEntityId != _inputMaterialIds[slot])
                {
                    ResolveMaterialHandleAndMask(terrain, inputMat, inputMatEntityId, sharedMaterials, world, _fallbackMaterial,
                        out EntityId acquiredMatEntityId, out MaterialHandle acquiredMatHandle, out uint mask);

                    Span<MaterialHandle> matHandles = stackalloc MaterialHandle[1] { acquiredMatHandle };
                    Span<uint> masks = stackalloc uint[1] { mask };
                    world.UpdateInstanceMaterials(_handles[slot], matHandles);
                    world.UpdateInstanceMask(_handles[slot], masks);

                    sharedMaterials.Release(_acquiredMaterialIds[slot], world);

                    _inputMaterialIds[slot] = inputMatEntityId;
                    _acquiredMaterialIds[slot] = acquiredMatEntityId;
                }
            }

            void RemoveFromWorld(int slot, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                Debug.Assert(_inWorld[slot]);
                world.RemoveInstance(_handles[slot]);
                sharedMaterials.Release(_acquiredMaterialIds[slot], world);
                _handles[slot] = default;
                _inputMaterialIds[slot] = EntityId.None;
                _acquiredMaterialIds[slot] = EntityId.None;
                _terrainDataIds[slot] = EntityId.None;
                _inWorld[slot] = false;
            }

            // Terrain geometry is baked at AddInstance time and cannot be patched in place, so a TerrainData change is
            // applied as a remove + re-add of the world instance. The instance stays registered and in the world throughout.
            void RebuildInstance(int slot, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                Debug.Assert(_inWorld[slot]);
                Debug.Assert(_components[slot] != null && _components[slot].terrainData != null);
                RemoveFromWorld(slot, sharedMaterials, world);
                AddToWorld(slot, _components[slot], sharedMaterials, world);
            }

            static void ResolveMaterialHandleAndMask(Terrain terrain, Material inputMat, EntityId inputMatEntityId, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world, Material fallbackMaterial, out EntityId acquiredMatEntityId, out MaterialHandle acquiredMatHandle, out uint mask)
            {
                mask = inputMat == null ? 0u : 1u;

                bool matIsSet = inputMat != null;
                bool matIsSetAndHasMetaPass = matIsSet && inputMat.FindPass("Meta") != -1;
                bool matIsUnsetOrNoMetaPass = !matIsSetAndHasMetaPass;
                bool matIsSetAndNoMetaPass = matIsSet && matIsUnsetOrNoMetaPass;

                if (matIsSetAndNoMetaPass)
                    Debug.LogError($"The material '{inputMat.name}' used by terrain '{terrain.gameObject.name}' does not have a 'Meta' shader pass and cannot be used by Surface Cache Global Illumination. A fallback material will be used for this material instead.", inputMat);

                // Using fallback material when there is no input material is redundant, but we do it anyway
                // because this buys us the simplifying assumption that we always have some material handle.
                Material acquiredMat;
                if (matIsUnsetOrNoMetaPass)
                {
                    acquiredMatEntityId = fallbackMaterial.GetEntityId();
                    acquiredMat = fallbackMaterial;
                }
                else
                {
                    acquiredMatEntityId = inputMatEntityId;
                    acquiredMat = inputMat;
                }
                acquiredMatHandle = sharedMaterials.Acquire(acquiredMatEntityId, acquiredMat, world);
            }
        }
#endif

        // Ensures the right Lights are present in the SurfaceCacheWorld at the right time, according to game object
        // state (enabled/active), bake state, and the rendering layer mask filter.
        class LightSet
        {
            readonly Dictionary<EntityId, int> _slots = new();
            readonly List<EntityId> _entityIds = new();
            readonly List<Light> _components = new();
            readonly List<bool> _inWorld = new();
            readonly List<LightHandle> _handles = new();

            readonly List<Light> _reevaluateScratch = new();

            public void CleanUp(SurfaceCacheWorld world)
            {
                for (int i = 0; i < _components.Count; i++)
                {
                    if (_inWorld[i])
                        RemoveFromWorld(i, world);
                }
            }

            public void Refresh(Light light, SurfaceCacheWorld world, uint renderingLayerMaskFilter)
            {
                var id = light.GetEntityId();
                bool isRegistered = _slots.TryGetValue(id, out int slot);
                bool isInWorld = isRegistered && _inWorld[slot];

                bool shouldBeRegistered = light.isActiveAndEnabled && !light.bakingOutput.isBaked;
                bool matchesWorldFilter = ((uint)light.renderingLayerMask & renderingLayerMaskFilter) != 0;
                bool shouldBeInWorld = shouldBeRegistered && matchesWorldFilter;

                bool shouldAddToWorld = shouldBeInWorld && !isInWorld;
                bool shouldUpdateInWorld = shouldBeInWorld && isInWorld;
                bool shouldRemoveFromWorld = isInWorld && !shouldBeInWorld;
                bool shouldUnregister = isRegistered && !shouldBeRegistered;
                bool shouldRegister = shouldBeRegistered && !isRegistered;

                // Note that ordering of the following operations matter.
                if (shouldRemoveFromWorld)
                    RemoveFromWorld(slot, world);

                if (shouldUnregister)
                    Unregister(slot);
                else if (shouldRegister)
                    slot = Register(id, light);

                if (shouldAddToWorld)
                    AddToWorld(slot, light, world);
                else if (shouldUpdateInWorld)
                    UpdateInWorld(slot, light, world);
            }

            public void TryRemove(EntityId lightId, SurfaceCacheWorld world)
            {
                if (!_slots.TryGetValue(lightId, out int slot))
                    return;

                if (_inWorld[slot])
                    RemoveFromWorld(slot, world);
                Unregister(slot);
            }

            public void ReevaluateAll(SurfaceCacheWorld world, uint renderingLayerMaskFilter)
            {
                // A copy is required because we mutate below.
                Debug.Assert(_reevaluateScratch.Count == 0);
                _reevaluateScratch.AddRange(_components);

                foreach (var component in _reevaluateScratch)
                {
                    Debug.Assert(component != null);
                    Refresh(component, world, renderingLayerMaskFilter);
                }

                _reevaluateScratch.Clear();
            }

            int Register(EntityId id, Light component)
            {
                int slot = _components.Count;
                _slots[id] = slot;
                _entityIds.Add(id);
                _components.Add(component);
                _inWorld.Add(false);
                _handles.Add(default);
                return slot;
            }

            void Unregister(int slot)
            {
                Debug.Assert(!_inWorld[slot]);
                int last = _components.Count - 1;
                _slots.Remove(_entityIds[slot]);
                if (slot != last)
                {
                    _entityIds[slot] = _entityIds[last];
                    _components[slot] = _components[last];
                    _inWorld[slot] = _inWorld[last];
                    _handles[slot] = _handles[last];
                    _slots[_entityIds[slot]] = slot;
                }
                _entityIds.RemoveAt(last);
                _components.RemoveAt(last);
                _inWorld.RemoveAt(last);
                _handles.RemoveAt(last);
            }

            void AddToWorld(int slot, Light light, SurfaceCacheWorld world)
            {
                Debug.Assert(!_inWorld[slot]);
                _handles[slot] = world.AddLight(CreateLightDescriptor(light));
                _inWorld[slot] = true;
            }

            void UpdateInWorld(int slot, Light light, SurfaceCacheWorld world)
            {
                world.UpdateLight(_handles[slot], CreateLightDescriptor(light));
            }

            void RemoveFromWorld(int slot, SurfaceCacheWorld world)
            {
                Debug.Assert(_inWorld[slot]);
                world.RemoveLight(_handles[slot]);
                _handles[slot] = default;
                _inWorld[slot] = false;
            }

            static SurfaceCacheWorld.LightDescriptor CreateLightDescriptor(Light light)
            {
                const bool multiplyPunctualLightIntensityByPI = false;
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
