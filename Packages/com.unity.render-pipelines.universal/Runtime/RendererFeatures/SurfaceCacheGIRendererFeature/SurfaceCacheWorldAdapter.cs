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
        readonly HashSet<EntityId> _destroyedMeshesThisFrame = new();
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

        internal bool TryGetEntityInstanceInputMaterialIds(EntityId key, out EntityId[] materialIds)
        {
            return _entityInstances.TryGetInputMaterialIds(key, out materialIds);
        }

        internal bool TryGetEntityInstanceAppliedMeshLod(EntityId key, out int meshLod)
        {
            return _entityInstances.TryGetAppliedMeshLod(key, out meshLod);
        }

        internal bool TryGetMeshRendererAppliedMeshLod(EntityId key, out int meshLod)
        {
            return _meshRenderers.TryGetAppliedMeshLod(key, out meshLod);
        }
#endif

        public void Update(SurfaceCacheWorldChangeSet changes, AmbientMode ambientMode, Material skyboxMaterial,
            Color ambientSkycolor, Color ambientEquatorColor, Color ambientGroundColor, float materialEnvIntensityMultiplier,
            bool divideEnvIntensityByPI, uint renderingLayerMaskFilter, SurfaceCacheWorld world, int materialResolution)
        {
            using var _ = k_WorldAdapterUpdateMarker.Auto();

            bool resolutionChanged = _sharedMaterials.SetResolution(materialResolution, world);

            bool filterChanged = renderingLayerMaskFilter != _renderingLayerMaskFilter;
            _renderingLayerMaskFilter = renderingLayerMaskFilter;

            RemoveInstancesWithDestroyedMeshes(changes.MeshDestroyedList, world);

            UpdateMeshRenderers(changes.MeshRendererTransformChangedIds, changes.MeshRendererTransformChangedLocalToWorlds, changes.MeshRendererChangedList, changes.MeshRendererDestroyedList, world);
            UpdateEntityInstances(changes.EntityInstanceTransformChangedList, changes.EntityInstanceChangedList, changes.EntityInstanceMaterialList, changes.EntityInstanceMaterialOverrideList, changes.EntityInstanceDestroyedList, world);
#if ENABLE_TERRAIN_MODULE
            UpdateTerrains(changes.TerrainTransformChangedList, changes.TerrainChangedList, changes.TerrainDestroyedList, changes.TerrainDataChangedList, world);
#endif
            UpdateLights(changes.LightTransformChangedList, changes.LightChangedList, changes.LightDestroyedList, world);

            // A change to the global rendering layer mask filter is not reported as a per-object change by the
            // ObjectDispatcher, so re-evaluate every known instance to add the newly-included and remove the
            // newly-excluded ones. A material resolution change can make room for materials that were rejected, which
            // re-evaluation retries. Both changes are rare (authoring), so this rescan is acceptable; the common
            // unchanged path skips it entirely.
            if (filterChanged || resolutionChanged)
            {
                _meshRenderers.ReevaluateAll(_sharedMaterials, world, _renderingLayerMaskFilter);
                _entityInstances.ReevaluateAll(_sharedMaterials, world, _renderingLayerMaskFilter);
#if ENABLE_TERRAIN_MODULE
                _terrains.ReevaluateAll(_sharedMaterials, world, _renderingLayerMaskFilter);
#endif
            }
            if (filterChanged)
                _lights.ReevaluateAll(world, _renderingLayerMaskFilter);

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

        void RemoveInstancesWithDestroyedMeshes(NativeArray<EntityId> destroyedMeshes, SurfaceCacheWorld world)
        {
            if (destroyedMeshes.Length == 0)
                return;

            foreach (var meshId in destroyedMeshes)
                _destroyedMeshesThisFrame.Add(meshId);

            _meshRenderers.RemoveInstancesWithDestroyedMeshes(_destroyedMeshesThisFrame, _sharedMaterials, world);
            _entityInstances.RemoveInstancesWithDestroyedMeshes(_destroyedMeshesThisFrame, _sharedMaterials, world);

            _destroyedMeshesThisFrame.Clear();
        }

        void UpdateMeshRenderers(NativeArray<EntityId> transformChangedIds, NativeArray<Matrix4x4> transformChangedLocalToWorlds, IReadOnlyList<Object> changed, NativeArray<EntityId> destroyed, SurfaceCacheWorld world)
        {
            using var _ = k_GameObjectInstanceUpdateMarker.Auto();

            if (transformChangedIds.IsCreated)
            {
                Debug.Assert(transformChangedIds.Length == transformChangedLocalToWorlds.Length);

                for (int i = 0; i < transformChangedIds.Length; i++)
                {
                    // Nothing is read off the Component here. The renderer is only resolved when it is not
                    // registered yet, which needs the full record anyway.
                    if (_meshRenderers.TryApplyTransform(transformChangedIds[i], transformChangedLocalToWorlds[i], world))
                        continue;

                    if (Resources.EntityIdToObject(transformChangedIds[i]) is MeshRenderer renderer)
                        _meshRenderers.Refresh(new MeshRendererSource(renderer), _sharedMaterials, world, _renderingLayerMaskFilter);
                }
            }

            for (int i = 0; i < changed.Count; i++)
            {
                var source = new MeshRendererSource((MeshRenderer)changed[i]);
                _meshRenderers.Refresh(source, _sharedMaterials, world, _renderingLayerMaskFilter);
            }

            foreach (var entityId in destroyed)
                _meshRenderers.TryRemove(entityId, _sharedMaterials, world);
        }

        void UpdateEntityInstances(NativeArray<SurfaceCacheEntityTransformRecord> transformChanged, NativeArray<SurfaceCacheEntityInstanceRecord> changed, NativeArray<SurfaceCacheSubMeshMaterial> materials, NativeArray<SurfaceCacheMaterialOverride> overrides, NativeArray<EntityId> destroyed, SurfaceCacheWorld world)
        {
            using var _ = k_EntityInstanceUpdateMarker.Auto();

            foreach (var record in transformChanged)
                _entityInstances.UpdateTransform(record.Key, record.LocalToWorld, world);

            // Remove before adding: an entity that stopped and then resumed being tracked since the last drain has a
            // destroy and a newer record, and the record must win.
            foreach (var key in destroyed)
                _entityInstances.TryRemove(key, _sharedMaterials, world);

            foreach (var record in changed)
                _entityInstances.Refresh(_entityInstances.CreateSource(record, materials, overrides), _sharedMaterials, world, _renderingLayerMaskFilter);
        }

#if ENABLE_TERRAIN_MODULE
        void UpdateTerrains(IReadOnlyList<Component> transformChanged, IReadOnlyList<Object> changed, NativeArray<EntityId> destroyed, IReadOnlyList<Object> terrainDataChanged, SurfaceCacheWorld world)
        {
            using var _ = k_GameObjectTerrainUpdateMarker.Auto();

            Debug.Assert(_dirtyTerrainDataThisFrame.Count == 0);

            for (int i = 0; i < terrainDataChanged.Count; i++)
                _dirtyTerrainDataThisFrame.Add(((TerrainData)terrainDataChanged[i]).GetEntityId());

            for (int i = 0; i < transformChanged.Count; i++)
                _terrains.Refresh((Terrain)transformChanged[i], _sharedMaterials, world, true, _renderingLayerMaskFilter);

            for (int i = 0; i < changed.Count; i++)
                _terrains.Refresh((Terrain)changed[i], _sharedMaterials, world, false, _renderingLayerMaskFilter);

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

        void UpdateLights(IReadOnlyList<Component> transformChanged, IReadOnlyList<Object> changed, NativeArray<EntityId> destroyed, SurfaceCacheWorld world)
        {
            using var _ = k_GameObjectLightUpdateMarker.Auto();

            for (int i = 0; i < transformChanged.Count; i++)
                _lights.Refresh((Light)transformChanged[i], world, _renderingLayerMaskFilter);

            for (int i = 0; i < changed.Count; i++)
                _lights.Refresh((Light)changed[i], world, _renderingLayerMaskFilter);

            foreach (var entityId in destroyed)
                _lights.TryRemove(entityId, world);
        }

        void UpdateMaterials(IReadOnlyList<Object> changed, SurfaceCacheWorld world)
        {
            using var _ = k_MaterialUpdateMarker.Auto();

#if UNITY_EDITOR
            _sharedMaterials.Update(world);
#endif

            for (int i = 0; i < changed.Count; i++)
            {
                var material = (Material)changed[i];
                _sharedMaterials.Update(material.GetEntityId(), material, world);
            }

            // For now we do not explicitly handle material _removal_. This is acceptable for these reasons:
            // 1) Even without explicit handling, the user experience is decent. If a user removes a material currently
            //    being used by a mesh renderer, they can assign a new material and everything will be in sync again.
            // 2) Keeping the associated data structures consistent is not easy and requires extra complexity and
            //    and tracking. It is a lot of work and cost for little gain.
        }

        // Preview scene objects (e.g. Prefab Mode) dispatch transform changes but never their destruction.
        static bool IsInPreviewScene(Component component)
        {
#if UNITY_EDITOR
            return UnityEditor.SceneManagement.EditorSceneManager.IsPreviewSceneObject(component);
#else
            return false;
#endif
        }

        interface IInstanceSource<TKey>
        {
            TKey Key { get; }
            bool Visible { get; }
            Mesh Mesh { get; }
            int MeshLod { get; }
            uint RenderingLayerMask { get; }
            Matrix4x4 LocalToWorld { get; }
            // May fill and return the given scratch list. Assignments outside subMeshCount are dropped.
            IReadOnlyList<Material> GetMaterials(List<Material> scratch, int subMeshCount);
            // Adds the instance's values for the numeric properties of the material at materialIndex.
            void GetOverrides(Material material, int materialIndex, List<SurfaceCacheMaterialOverride> scratch);
        }

        // Overrides are sorted by NameId, and null when the instance overrides nothing.
        readonly struct SharedMaterialKey : IEquatable<SharedMaterialKey>
        {
            public readonly EntityId Material;
            public readonly SurfaceCacheMaterialOverride[] Overrides;

            public SharedMaterialKey(EntityId material)
            {
                Material = material;
                Overrides = null;
            }

            public SharedMaterialKey(EntityId material, SurfaceCacheMaterialOverride[] overrides)
            {
                Material = material;
                Overrides = overrides;
            }

            public bool Equals(SharedMaterialKey other)
            {
                return Material == other.Material && OverridesEqual(Overrides, other.Overrides);
            }

            public override bool Equals(object obj)
            {
                return obj is SharedMaterialKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                int hash = Material.GetHashCode();
                if (Overrides != null)
                {
                    foreach (var materialOverride in Overrides)
                        hash = HashCode.Combine(hash, materialOverride.NameId, materialOverride.Value);
                }

                return hash;
            }

            static bool OverridesEqual(SurfaceCacheMaterialOverride[] first, SurfaceCacheMaterialOverride[] second)
            {
                int length = first?.Length ?? 0;
                if (length != (second?.Length ?? 0))
                    return false;

                for (int i = 0; i < length; i++)
                {
                    if (first[i].NameId != second[i].NameId || !first[i].Value.Equals(second[i].Value))
                        return false;
                }

                return true;
            }
        }

        static bool TryFindNumericProperty(Shader shader, int nameId, out int propertyIndex)
        {
            int propertyCount = shader.GetPropertyCount();
            for (int i = 0; i < propertyCount; i++)
            {
                if (shader.GetPropertyNameId(i) != nameId)
                    continue;

                propertyIndex = i;
                return IsNumeric(shader.GetPropertyType(i));
            }

            propertyIndex = -1;
            return false;
        }

        static bool IsNumeric(ShaderPropertyType type)
        {
            return type == ShaderPropertyType.Float
                || type == ShaderPropertyType.Range
                || type == ShaderPropertyType.Color
                || type == ShaderPropertyType.Vector;
        }

        readonly struct MeshRendererSource : IInstanceSource<EntityId>
        {
            static MaterialPropertyBlock s_Properties;

            readonly MeshRenderer _renderer;

            public MeshRendererSource(MeshRenderer renderer)
            {
                _renderer = renderer;
            }

            public EntityId Key => _renderer.GetEntityId();

            // In an open subscene the live-converted entities are the source of truth, not the authoring renderers.
            public bool Visible => _renderer.enabled && _renderer.gameObject.activeInHierarchy && !_renderer.gameObject.scene.isSubScene && !IsInPreviewScene(_renderer) && IsInGlobalIlluminationLOD(_renderer);

            // A renderer outside any LODGroup contributes, matching an entity without a MeshLODComponent.
            static bool IsInGlobalIlluminationLOD(MeshRenderer renderer)
            {
                var lodGroup = renderer.LODGroup;
                if (lodGroup == null)
                    return true;

                int giLod = lodGroup.globalIlluminationLOD;
                if (giLod < 0)
                    return false;

                // LODGroup keeps the selection inside its LOD range, so the shift cannot wrap onto a valid bit.
                return (renderer.lodGroupMask & (1u << giLod)) != 0;
            }

            public Mesh Mesh
            {
                get
                {
                    Debug.Assert(!_renderer.isPartOfStaticBatch, "Static Batching is not supported by Surface Cache GI.");
                    return _renderer.TryGetComponent(out MeshFilter meshFilter) ? meshFilter.sharedMesh : null;
                }
            }

            public int MeshLod => _renderer.globalIlluminationMeshLod;

            public uint RenderingLayerMask => _renderer.renderingLayerMask;

            public Matrix4x4 LocalToWorld => _renderer.transform.localToWorldMatrix;

            public IReadOnlyList<Material> GetMaterials(List<Material> scratch, int subMeshCount)
            {
                _renderer.GetSharedMaterials(scratch);
                return scratch;
            }

            public void GetOverrides(Material material, int materialIndex, List<SurfaceCacheMaterialOverride> scratch)
            {
                if (!_renderer.HasPropertyBlock())
                    return;

                // Rendering uses a material's own block in place of the renderer's block, not merged with it.
                s_Properties ??= new MaterialPropertyBlock();
                _renderer.GetPropertyBlock(s_Properties, materialIndex);
                if (s_Properties.isEmpty)
                    _renderer.GetPropertyBlock(s_Properties);

                var shader = material.shader;
                int propertyCount = shader.GetPropertyCount();
                for (int i = 0; i < propertyCount; i++)
                {
                    var type = shader.GetPropertyType(i);
                    if (!IsNumeric(type))
                        continue;

                    int nameId = shader.GetPropertyNameId(i);
                    if (TryGetOverride(s_Properties, nameId, type, out var value))
                        scratch.Add(new SurfaceCacheMaterialOverride { NameId = nameId, Value = value });
                }
            }

            static bool TryGetOverride(MaterialPropertyBlock properties, int nameId, ShaderPropertyType type, out Vector4 value)
            {
                bool isFloat = type == ShaderPropertyType.Float || type == ShaderPropertyType.Range;
                if (isFloat && properties.HasFloat(nameId))
                {
                    value = new Vector4(properties.GetFloat(nameId), 0f, 0f, 0f);
                    return true;
                }

                if (!isFloat && properties.HasVector(nameId))
                {
                    value = properties.GetVector(nameId);
                    return true;
                }

                value = default;
                return false;
            }
        }

        readonly struct EntityRecordSource : IInstanceSource<EntityId>
        {
            readonly SurfaceCacheEntityInstanceRecord _record;
            // Copied out: a drained record only carries a range into a buffer the bridge reuses every frame.
            readonly SurfaceCacheSubMeshMaterial[] _materials;
            readonly SurfaceCacheMaterialOverride[] _overrides;

            public EntityRecordSource(in SurfaceCacheEntityInstanceRecord record, SurfaceCacheSubMeshMaterial[] materials, SurfaceCacheMaterialOverride[] overrides)
            {
                _record = record;
                _materials = materials;
                _overrides = overrides;
            }

            public SurfaceCacheSubMeshMaterial[] SubMeshMaterials => _materials;
            public SurfaceCacheMaterialOverride[] Overrides => _overrides;

            public EntityId Key => _record.Key;
            public bool Visible => _record.Visible;

            // Safe only because the live RenderMeshArrayHost keeps these meshes resident.
            public Mesh Mesh => _record.Mesh != EntityId.None ? Resources.EntityIdToObject(_record.Mesh) as Mesh : null;

            public int MeshLod => _record.MeshLod;
            public uint RenderingLayerMask => _record.RenderingLayerMask;
            public Matrix4x4 LocalToWorld => _record.LocalToWorld;

            // Pads the producer's sparse assignments to one entry per submesh; unassigned ones stay null and are
            // masked out downstream. Out-of-range assignments are dropped rather than padded to, because
            // SubMeshIndex is authored data of arbitrary size and this is the first place subMeshCount is known.
            public IReadOnlyList<Material> GetMaterials(List<Material> scratch, int subMeshCount)
            {
                scratch.Clear();
                foreach (var assignment in _materials)
                {
                    if ((uint)assignment.SubMeshIndex >= (uint)subMeshCount)
                        continue;

                    while (scratch.Count <= assignment.SubMeshIndex)
                        scratch.Add(null);

                    scratch[assignment.SubMeshIndex] = assignment.Material != EntityId.None
                        ? Resources.EntityIdToObject(assignment.Material) as Material
                        : null;
                }

                return scratch;
            }

            // An entity can carry override components for properties its material's shader does not declare.
            public void GetOverrides(Material material, int materialIndex, List<SurfaceCacheMaterialOverride> scratch)
            {
                var shader = material.shader;
                foreach (var materialOverride in _overrides)
                {
                    if (TryFindNumericProperty(shader, materialOverride.NameId, out _))
                        scratch.Add(materialOverride);
                }
            }

            public EntityRecordSource WithLocalToWorld(in Matrix4x4 localToWorld)
            {
                var record = _record;
                record.LocalToWorld = localToWorld;
                return new EntityRecordSource(record, _materials, _overrides);
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
            readonly List<EntityId> _meshIds = new();
            readonly List<int> _meshLods = new();
            // The transform last applied to the world instance.
            readonly List<Matrix4x4> _localToWorlds = new();
            // Materials provided by the source the last time we checked ("null materials" are EntityId.None).
            readonly List<SharedMaterialKey[]> _inputMaterialKeys = new();
            // Materials currently acquired from the shared set (may include the fallback material).
            readonly List<SharedMaterialKey[]> _acquiredMaterialKeys = new();

            readonly List<TSource> _reevaluateScratch = new();
            readonly Material _fallbackMaterial;
            readonly List<Material> _materialScratch = new();
            readonly List<SurfaceCacheMaterialOverride> _overrideScratch = new();
            readonly Dictionary<int, Stack<SharedMaterialKey[]>> _freeMaterialKeyArrays = new();

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

            public bool TryGetAppliedMeshLod(TKey key, out int meshLod)
            {
                if (_slots.TryGetValue(key, out int slot) && _inWorld[slot])
                {
                    meshLod = _meshLods[slot];
                    return true;
                }

                meshLod = 0;
                return false;
            }

            public bool TryGetInputMaterialIds(TKey key, out EntityId[] materialIds)
            {
                if (_slots.TryGetValue(key, out int slot) && _inWorld[slot])
                {
                    materialIds = Array.ConvertAll(_inputMaterialKeys[slot], materialKey => materialKey.Material);
                    return true;
                }

                materialIds = null;
                return false;
            }
#endif

            // Registration is not re-evaluated here. Everything the predicate reads dispatches as a type change,
            // so it arrives on the changed list instead. False when the key is unknown, which the caller resolves.
            public bool TryApplyTransform(TKey key, in Matrix4x4 localToWorld, SurfaceCacheWorld world)
            {
                if (!TryGetSlot(key, out int slot))
                    return false;

                ApplyTransform(slot, localToWorld, world);
                return true;
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
                {
                    Unregister(slot);
                }
                else if (shouldRegister)
                    slot = Register(key, source);
                else if (isRegistered)
                    ReplaceSource(slot, source);

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

            public void RemoveInstancesWithDestroyedMeshes(HashSet<EntityId> destroyedMeshes, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                for (int i = 0; i < _keys.Count; i++)
                {
                    if (_inWorld[i] && destroyedMeshes.Contains(_meshIds[i]))
                        RemoveFromWorld(i, sharedMaterials, world);
                }
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

            protected virtual void OnSourceReplaced(in TSource previous, in TSource replacement)
            {
            }

            protected virtual void OnSourceUnregistered(in TSource source)
            {
            }

            void ReplaceSource(int slot, in TSource source)
            {
                OnSourceReplaced(_sources[slot], source);
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
                _meshIds.Add(EntityId.None);
                _meshLods.Add(0);
                _localToWorlds.Add(default);
                _inputMaterialKeys.Add(null);
                _acquiredMaterialKeys.Add(null);
                return slot;
            }

            void Unregister(int slot)
            {
                Debug.Assert(!_inWorld[slot]);
                OnSourceUnregistered(_sources[slot]);
                int last = _keys.Count - 1;
                _slots.Remove(_keys[slot]);
                if (slot != last)
                {
                    _keys[slot] = _keys[last];
                    _sources[slot] = _sources[last];
                    _inWorld[slot] = _inWorld[last];
                    _handles[slot] = _handles[last];
                    _meshIds[slot] = _meshIds[last];
                    _meshLods[slot] = _meshLods[last];
                    _localToWorlds[slot] = _localToWorlds[last];
                    _inputMaterialKeys[slot] = _inputMaterialKeys[last];
                    _acquiredMaterialKeys[slot] = _acquiredMaterialKeys[last];
                    _slots[_keys[slot]] = slot;
                }
                _keys.RemoveAt(last);
                _sources.RemoveAt(last);
                _inWorld.RemoveAt(last);
                _handles.RemoveAt(last);
                _meshIds.RemoveAt(last);
                _meshLods.RemoveAt(last);
                _localToWorlds.RemoveAt(last);
                _inputMaterialKeys.RemoveAt(last);
                _acquiredMaterialKeys.RemoveAt(last);
            }

            void AddToWorld(int slot, in TSource source, Mesh mesh, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                Debug.Assert(!_inWorld[slot]);
                Debug.Assert(mesh != null && mesh.vertexCount != 0);

                Debug.Assert(_materialScratch.Count == 0);
                var inputMats = source.GetMaterials(_materialScratch, mesh.subMeshCount);

                var inputMatKeys = ReuseOrCreateMaterialKeyArray(mesh.subMeshCount);
                ResolveMaterialKeys(source, inputMats, null, inputMatKeys);

                Span<MaterialHandle> acquiredMatHandles = stackalloc MaterialHandle[mesh.subMeshCount];
                var acquiredMatKeys = ReuseOrCreateMaterialKeyArray(mesh.subMeshCount);
                Span<uint> masks = stackalloc uint[mesh.subMeshCount];
                ResolveMaterialHandlesAndMasks(world, sharedMaterials, inputMats, inputMatKeys, acquiredMatHandles, acquiredMatKeys, masks);

                var localToWorld = source.LocalToWorld;
                int meshLod = ResolveMeshLod(mesh, source.MeshLod);
                try
                {
                    _handles[slot] = world.AddInstance(mesh, acquiredMatHandles, masks, localToWorld, meshLod);
                }
                catch
                {
                    foreach (var matKey in acquiredMatKeys)
                        sharedMaterials.Release(matKey, world);
                    RecycleMaterialKeyArray(inputMatKeys);
                    RecycleMaterialKeyArray(acquiredMatKeys);
                    _materialScratch.Clear();
                    throw;
                }

                _meshIds[slot] = mesh.GetEntityId();
                _meshLods[slot] = meshLod;
                _localToWorlds[slot] = localToWorld;
                _inputMaterialKeys[slot] = inputMatKeys;
                _acquiredMaterialKeys[slot] = acquiredMatKeys;
                _inWorld[slot] = true;

                _materialScratch.Clear();
            }

            void UpdateInWorld(int slot, in TSource source, Mesh mesh, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                if (_meshIds[slot] != mesh.GetEntityId() || _meshLods[slot] != ResolveMeshLod(mesh, source.MeshLod))
                {
                    RemoveFromWorld(slot, sharedMaterials, world);
                    AddToWorld(slot, source, mesh, sharedMaterials, world);
                    return;
                }

                Debug.Assert(mesh != null && mesh.vertexCount != 0);
                Debug.Assert(_materialScratch.Count == 0);
                var inputMats = source.GetMaterials(_materialScratch, mesh.subMeshCount);

                var inputMatKeys = ReuseOrCreateMaterialKeyArray(mesh.subMeshCount);
                ResolveMaterialKeys(source, inputMats, _inputMaterialKeys[slot], inputMatKeys);

                bool materialsChanged = !((ReadOnlySpan<SharedMaterialKey>)inputMatKeys).SequenceEqual(_inputMaterialKeys[slot]);
                if (materialsChanged || HasRejectedMaterials(_inputMaterialKeys[slot], _acquiredMaterialKeys[slot]))
                {
                    Span<MaterialHandle> acquiredMatHandles = stackalloc MaterialHandle[mesh.subMeshCount];
                    var acquiredMatKeys = ReuseOrCreateMaterialKeyArray(mesh.subMeshCount);
                    Span<uint> masks = stackalloc uint[mesh.subMeshCount];
                    ResolveMaterialHandlesAndMasks(world, sharedMaterials, inputMats, inputMatKeys, acquiredMatHandles, acquiredMatKeys, masks);

                    world.UpdateInstanceMaterials(_handles[slot], acquiredMatHandles);
                    world.UpdateInstanceMask(_handles[slot], masks);

                    foreach (var matKey in _acquiredMaterialKeys[slot])
                        sharedMaterials.Release(matKey, world);

                    // Releasing the superseded materials can free the atlas space a rejected acquisition needed.
                    // A material can only be released once no instance uses it, so the rejected ones are detached first.
                    if (HasRejectedMaterials(inputMatKeys, acquiredMatKeys))
                    {
                        for (int i = 0; i < acquiredMatKeys.Length; i++)
                        {
                            if (!IsRejected(inputMatKeys[i], acquiredMatKeys[i]))
                                continue;

                            acquiredMatHandles[i] = MaterialHandle.Invalid;
                            masks[i] = 0u;
                        }

                        world.UpdateInstanceMaterials(_handles[slot], acquiredMatHandles);
                        world.UpdateInstanceMask(_handles[slot], masks);

                        for (int i = 0; i < acquiredMatKeys.Length; i++)
                        {
                            if (acquiredMatHandles[i] == MaterialHandle.Invalid)
                                sharedMaterials.Release(acquiredMatKeys[i], world);
                        }

                        for (int i = 0; i < acquiredMatKeys.Length; i++)
                        {
                            if (acquiredMatHandles[i] == MaterialHandle.Invalid)
                                AcquireMaterial(i, world, sharedMaterials, inputMats, inputMatKeys, acquiredMatHandles, acquiredMatKeys, masks);
                        }

                        world.UpdateInstanceMaterials(_handles[slot], acquiredMatHandles);
                        world.UpdateInstanceMask(_handles[slot], masks);
                    }

                    RecycleMaterialKeyArray(_inputMaterialKeys[slot]);
                    RecycleMaterialKeyArray(_acquiredMaterialKeys[slot]);
                    _inputMaterialKeys[slot] = inputMatKeys;
                    _acquiredMaterialKeys[slot] = acquiredMatKeys;
                }
                else
                {
                    RecycleMaterialKeyArray(inputMatKeys);
                }

                _materialScratch.Clear();

                ApplyTransform(slot, source.LocalToWorld, world);
            }

            static bool HasRejectedMaterials(SharedMaterialKey[] inputMatKeys, SharedMaterialKey[] acquiredMatKeys)
            {
                for (int i = 0; i < acquiredMatKeys.Length; i++)
                {
                    if (IsRejected(inputMatKeys[i], acquiredMatKeys[i]))
                        return true;
                }

                return false;
            }

            // A material acquired without the overrides that did not fit is retried like a rejected one.
            static bool IsRejected(in SharedMaterialKey inputMatKey, in SharedMaterialKey acquiredMatKey)
            {
                return acquiredMatKey.Material == EntityId.None
                    || (acquiredMatKey.Overrides == null && inputMatKey.Overrides != null && acquiredMatKey.Material == inputMatKey.Material);
            }

            void ResolveMaterialKeys(in TSource source, IReadOnlyList<Material> inputMats, SharedMaterialKey[] previousKeys, SharedMaterialKey[] keys)
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    Material inputMat = i < inputMats.Count ? inputMats[i] : null;
                    if (inputMat == null)
                    {
                        keys[i] = new SharedMaterialKey(EntityId.None);
                        continue;
                    }

                    Debug.Assert(_overrideScratch.Count == 0);
                    source.GetOverrides(inputMat, i, _overrideScratch);
                    SortByNameId(_overrideScratch);

                    var matEntityId = inputMat.GetEntityId();
                    if (_overrideScratch.Count == 0)
                        keys[i] = new SharedMaterialKey(matEntityId);
                    else if (previousKeys != null && i < previousKeys.Length && previousKeys[i].Material == matEntityId && OverridesMatch(previousKeys[i].Overrides, _overrideScratch))
                        keys[i] = previousKeys[i];
                    else
                        keys[i] = new SharedMaterialKey(matEntityId, _overrideScratch.ToArray());

                    _overrideScratch.Clear();
                }
            }

            static void SortByNameId(List<SurfaceCacheMaterialOverride> overrides)
            {
                for (int i = 1; i < overrides.Count; i++)
                {
                    var current = overrides[i];
                    int j = i - 1;
                    while (j >= 0 && overrides[j].NameId > current.NameId)
                    {
                        overrides[j + 1] = overrides[j];
                        j--;
                    }

                    overrides[j + 1] = current;
                }
            }

            static bool OverridesMatch(SurfaceCacheMaterialOverride[] overrides, List<SurfaceCacheMaterialOverride> scratch)
            {
                if (overrides == null || overrides.Length != scratch.Count)
                    return false;

                for (int i = 0; i < overrides.Length; i++)
                {
                    if (overrides[i].NameId != scratch[i].NameId || !overrides[i].Value.Equals(scratch[i].Value))
                        return false;
                }

                return true;
            }

            void ResolveMaterialHandlesAndMasks(SurfaceCacheWorld world, SharedMaterialSet sharedMaterials, IReadOnlyList<Material> inputMats, SharedMaterialKey[] inputMaterialKeys, Span<MaterialHandle> acquiredMaterialHandles, SharedMaterialKey[] acquiredMaterialKeys, Span<uint> masks)
            {
                for (int i = 0; i < acquiredMaterialHandles.Length; i++)
                    AcquireMaterial(i, world, sharedMaterials, inputMats, inputMaterialKeys, acquiredMaterialHandles, acquiredMaterialKeys, masks);
            }

            void AcquireMaterial(int subMeshIndex, SurfaceCacheWorld world, SharedMaterialSet sharedMaterials, IReadOnlyList<Material> inputMats, SharedMaterialKey[] inputMaterialKeys, Span<MaterialHandle> acquiredMaterialHandles, SharedMaterialKey[] acquiredMaterialKeys, Span<uint> masks)
            {
                Material inputMat = subMeshIndex < inputMats.Count ? inputMats[subMeshIndex] : null;

                bool matIsUnsetOrNoMetaPass = inputMat == null || inputMat.FindPass("Meta") == -1;

                masks[subMeshIndex] = matIsUnsetOrNoMetaPass ? 0u : 1u;

                // Acquiring the fallback even for unset materials buys the assumption that a handle always exists.
                var acquiredMat = inputMat;
                var acquiredMatKey = inputMaterialKeys[subMeshIndex];
                if (matIsUnsetOrNoMetaPass)
                {
                    acquiredMat = _fallbackMaterial;
                    acquiredMatKey = new SharedMaterialKey(_fallbackMaterial.GetEntityId());
                }

                acquiredMaterialHandles[subMeshIndex] = sharedMaterials.Acquire(acquiredMatKey, acquiredMat, world);

                // Per-instance values can outgrow the atlas, so such an instance keeps contributing with the shared material.
                if (acquiredMaterialHandles[subMeshIndex] == MaterialHandle.Invalid && acquiredMatKey.Overrides != null)
                {
                    acquiredMatKey = new SharedMaterialKey(acquiredMatKey.Material);
                    acquiredMaterialHandles[subMeshIndex] = sharedMaterials.Acquire(acquiredMatKey, acquiredMat, world);
                }

                acquiredMaterialKeys[subMeshIndex] = acquiredMatKey;

                if (acquiredMaterialHandles[subMeshIndex] == MaterialHandle.Invalid)
                {
                    masks[subMeshIndex] = 0u;
                    acquiredMaterialKeys[subMeshIndex] = new SharedMaterialKey(EntityId.None);
                }
            }

            void RemoveFromWorld(int slot, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                Debug.Assert(_inWorld[slot]);
                world.RemoveInstance(_handles[slot]);
                foreach (var matKey in _acquiredMaterialKeys[slot])
                    sharedMaterials.Release(matKey, world);
                RecycleMaterialKeyArray(_inputMaterialKeys[slot]);
                RecycleMaterialKeyArray(_acquiredMaterialKeys[slot]);
                _handles[slot] = default;
                _meshIds[slot] = EntityId.None;
                _meshLods[slot] = 0;
                _localToWorlds[slot] = default;
                _inputMaterialKeys[slot] = null;
                _acquiredMaterialKeys[slot] = null;
                _inWorld[slot] = false;
            }

            SharedMaterialKey[] ReuseOrCreateMaterialKeyArray(int length)
            {
                return _freeMaterialKeyArrays.TryGetValue(length, out var freeArrays) && freeArrays.Count != 0
                    ? freeArrays.Pop()
                    : new SharedMaterialKey[length];
            }

            void RecycleMaterialKeyArray(SharedMaterialKey[] array)
            {
                // Pooled arrays would otherwise keep the override arrays alive.
                Array.Clear(array, 0, array.Length);
                if (!_freeMaterialKeyArrays.TryGetValue(array.Length, out var freeArrays))
                {
                    freeArrays = new Stack<SharedMaterialKey[]>();
                    _freeMaterialKeyArrays.Add(array.Length, freeArrays);
                }

                freeArrays.Push(array);
            }
        }

        // Transform records carry no mesh or material data and cannot go through Refresh.
        sealed class EntityInstanceSet : InstanceSet<EntityId, EntityRecordSource>
        {
            readonly Dictionary<int, Stack<SurfaceCacheSubMeshMaterial[]>> _freeSubMeshMaterialArrays = new();
            readonly Dictionary<int, Stack<SurfaceCacheMaterialOverride[]>> _freeOverrideArrays = new();

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

            public EntityRecordSource CreateSource(in SurfaceCacheEntityInstanceRecord record, NativeArray<SurfaceCacheSubMeshMaterial> materials, NativeArray<SurfaceCacheMaterialOverride> overrides)
            {
                int count = record.MaterialsCount;

                // Such a record is never registered, so nothing reads its materials.
                if (count == 0 || !record.Visible || record.Mesh == EntityId.None)
                    return new EntityRecordSource(record, Array.Empty<SurfaceCacheSubMeshMaterial>(), Array.Empty<SurfaceCacheMaterialOverride>());

                // A registered key usually re-reports the same counts, so its buffers are reused rather than reallocated.
                bool registered = TryGetSlot(record.Key, out int slot);
                var buffer = registered ? GetSource(slot).SubMeshMaterials : null;
                if (buffer == null || buffer.Length != count)
                    buffer = ReuseOrCreateArray(_freeSubMeshMaterialArrays, count);

                NativeArray<SurfaceCacheSubMeshMaterial>.Copy(materials, record.MaterialsStart, buffer, 0, count);

                int overrideCount = record.OverridesCount;
                var overrideBuffer = registered ? GetSource(slot).Overrides : null;
                if (overrideBuffer == null || overrideBuffer.Length != overrideCount)
                    overrideBuffer = ReuseOrCreateArray(_freeOverrideArrays, overrideCount);

                if (overrideCount != 0)
                    NativeArray<SurfaceCacheMaterialOverride>.Copy(overrides, record.OverridesStart, overrideBuffer, 0, overrideCount);

                return new EntityRecordSource(record, buffer, overrideBuffer);
            }

            protected override void OnSourceReplaced(in EntityRecordSource previous, in EntityRecordSource replacement)
            {
                if (previous.SubMeshMaterials != replacement.SubMeshMaterials)
                    RecycleArray(_freeSubMeshMaterialArrays, previous.SubMeshMaterials);
                if (previous.Overrides != replacement.Overrides)
                    RecycleArray(_freeOverrideArrays, previous.Overrides);
            }

            protected override void OnSourceUnregistered(in EntityRecordSource source)
            {
                RecycleArray(_freeSubMeshMaterialArrays, source.SubMeshMaterials);
                RecycleArray(_freeOverrideArrays, source.Overrides);
            }

            static T[] ReuseOrCreateArray<T>(Dictionary<int, Stack<T[]>> freeArraysByLength, int length)
            {
                if (length == 0)
                    return Array.Empty<T>();

                if (freeArraysByLength.TryGetValue(length, out var freeArrays) && freeArrays.Count != 0)
                    return freeArrays.Pop();

                return new T[length];
            }

            static void RecycleArray<T>(Dictionary<int, Stack<T[]>> freeArraysByLength, T[] array)
            {
                bool isSharedEmptyArray = array.Length == 0;
                if (isSharedEmptyArray)
                    return;

                if (!freeArraysByLength.TryGetValue(array.Length, out var freeArrays))
                {
                    freeArrays = new Stack<T[]>();
                    freeArraysByLength.Add(array.Length, freeArrays);
                }

                freeArrays.Push(array);
            }
        }

        static int ResolveMeshLod(Mesh mesh, int requestedMeshLod)
        {
            return Mathf.Clamp(requestedMeshLod, 0, mesh.lodCount - 1);
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

                bool shouldBeRegistered = terrain.isActiveAndEnabled && terrain.terrainData != null && !IsInPreviewScene(terrain);
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
                ResolveMaterialHandleAndMask(inputMaterial, inputMatEntityId, sharedMaterials, world, _fallbackMaterial,
                    out EntityId acquiredMatEntityId, out MaterialHandle matHandle, out uint mask);
                if (matHandle == MaterialHandle.Invalid)
                    return;

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
                if (inputMatEntityId != _inputMaterialIds[slot] || _acquiredMaterialIds[slot] == EntityId.None)
                {
                    ResolveMaterialHandleAndMask(inputMat, inputMatEntityId, sharedMaterials, world, _fallbackMaterial,
                        out EntityId acquiredMatEntityId, out MaterialHandle acquiredMatHandle, out uint mask);

                    Span<MaterialHandle> matHandles = stackalloc MaterialHandle[1] { acquiredMatHandle };
                    Span<uint> masks = stackalloc uint[1] { mask };
                    world.UpdateInstanceMaterials(_handles[slot], matHandles);
                    world.UpdateInstanceMask(_handles[slot], masks);

                    sharedMaterials.Release(new SharedMaterialKey(_acquiredMaterialIds[slot]), world);

                    _inputMaterialIds[slot] = inputMatEntityId;
                    _acquiredMaterialIds[slot] = acquiredMatEntityId;
                }
            }

            void RemoveFromWorld(int slot, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world)
            {
                Debug.Assert(_inWorld[slot]);
                world.RemoveInstance(_handles[slot]);
                sharedMaterials.Release(new SharedMaterialKey(_acquiredMaterialIds[slot]), world);
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

            static void ResolveMaterialHandleAndMask(Material inputMat, EntityId inputMatEntityId, SharedMaterialSet sharedMaterials, SurfaceCacheWorld world, Material fallbackMaterial, out EntityId acquiredMatEntityId, out MaterialHandle acquiredMatHandle, out uint mask)
            {
                bool matIsUnsetOrNoMetaPass = inputMat == null || inputMat.FindPass("Meta") == -1;

                mask = matIsUnsetOrNoMetaPass ? 0u : 1u;

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
                acquiredMatHandle = sharedMaterials.Acquire(new SharedMaterialKey(acquiredMatEntityId), acquiredMat, world);
                if (acquiredMatHandle == MaterialHandle.Invalid)
                {
                    mask = 0u;
                    acquiredMatEntityId = EntityId.None;
                }
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

                bool shouldBeRegistered = light.isActiveAndEnabled && !light.bakingOutput.isBaked && !IsInPreviewScene(light);
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
            }

            const EmissionMode kEmissionMode = EmissionMode.Realtime;
            const UVChannel kUVChannel = UVChannel.UV0;
            static readonly int k_EmissionColorId = Shader.PropertyToID("_EmissionColor");

            readonly Dictionary<SharedMaterialKey, Entry> _entries = new();
            readonly HashSet<SharedMaterialKey> _pendingMetaPassEvals = new();
            readonly HashSet<EntityId> _reportedRejections = new();
            readonly List<SharedMaterialKey> _keyScratch = new();
            readonly Material _fallbackMaterial;
            int _resolution = 256;

            public SharedMaterialSet(Material fallbackMaterial)
            {
                _fallbackMaterial = fallbackMaterial;
            }

            public bool SetResolution(int resolution, SurfaceCacheWorld world)
            {
                if (resolution == _resolution)
                    return false;

                _resolution = resolution;
                _reportedRejections.Clear();

                _keyScratch.Clear();
                _keyScratch.AddRange(_entries.Keys);
                foreach (var matKey in _keyScratch)
                {
                    if (_pendingMetaPassEvals.Contains(matKey))
                        continue;

                    var entry = _entries[matKey];
                    if (entry.Material == null)
                        continue;

#if UNITY_EDITOR
                    var descriptor = entry.Material.FindPass("Meta") == -1 ? GetFallbackMaterialDescriptor() : CreateDescriptor(matKey, entry.Material);
#else
                    var descriptor = CreateDescriptor(matKey, entry.Material);
#endif
                    UpdateInWorld(matKey, entry, descriptor, world);
                }
                _keyScratch.Clear();
                return true;
            }

            public MaterialHandle Acquire(in SharedMaterialKey matKey, Material mat, SurfaceCacheWorld world)
            {
                if (_entries.TryGetValue(matKey, out var entry))
                {
                    entry.RefCount += 1;
                    _entries[matKey] = entry;
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
                        descriptor = CreateDescriptor(matKey, mat);
                    }
                    else
                    {
                        descriptor = GetFallbackMaterialDescriptor();
                        _pendingMetaPassEvals.Add(matKey);
                        UnityEditor.ShaderUtil.CompilePass(mat, metaPassIndex);
                    }
#else
                    descriptor = CreateDescriptor(matKey, mat);
#endif

                    var newHandle = world.AddMaterial(descriptor, kUVChannel);
                    DestroyDescriptorTextures(descriptor);
                    if (newHandle == MaterialHandle.Invalid)
                    {
                        _pendingMetaPassEvals.Remove(matKey);
                        ReportRejection(matKey.Material, mat);
                        return MaterialHandle.Invalid;
                    }

                    var newEntry = new Entry
                    {
                        RefCount = 1,
                        WorldHandle = newHandle,
                        Material = mat
                    };
                    _entries[matKey] = newEntry;
                    return newHandle;
                }
            }

#if UNITY_EDITOR
            public void Update(SurfaceCacheWorld world)
            {
                if (_pendingMetaPassEvals.Count != 0)
                {
                    var evaluatedMaterials = new List<SharedMaterialKey>();
                    foreach (var matKey in _pendingMetaPassEvals)
                    {
                        var entry = _entries[matKey];
                        var metaPassIndex = entry.Material.FindPass("Meta");
                        Debug.Assert(metaPassIndex != -1);
                        if (UnityEditor.ShaderUtil.IsPassCompiled(entry.Material, metaPassIndex))
                        {
                            UpdateInWorld(matKey, entry, CreateDescriptor(matKey, entry.Material), world);
                            evaluatedMaterials.Add(matKey);
                        }
                    }

                    foreach (var matKey in evaluatedMaterials)
                    {
                        _pendingMetaPassEvals.Remove(matKey);
                    }
                }
            }
#endif

            public void Update(EntityId matEntityId, Material material, SurfaceCacheWorld world)
            {
                _keyScratch.Clear();
                foreach (var matKey in _entries.Keys)
                {
                    if (matKey.Material == matEntityId)
                        _keyScratch.Add(matKey);
                }

                foreach (var matKey in _keyScratch)
                    Update(matKey, material, world);

                _keyScratch.Clear();
            }

            void Update(in SharedMaterialKey matKey, Material material, SurfaceCacheWorld world)
            {
                if (!_pendingMetaPassEvals.Contains(matKey))
                {
#if UNITY_EDITOR
                    // If the shader was modified, the meta pass may have been deleted, or not yet compiled.
                    var metaPassIndex = material.FindPass("Meta");
                    if (metaPassIndex == -1)
                    {
                        UpdateInWorld(matKey, _entries[matKey], GetFallbackMaterialDescriptor(), world);
                        return;
                    }
                    else if (!UnityEditor.ShaderUtil.IsPassCompiled(material, metaPassIndex))
                    {
                        _pendingMetaPassEvals.Add(matKey);
                        UnityEditor.ShaderUtil.CompilePass(material, metaPassIndex);
                        return;
                    }
#endif

                    UpdateInWorld(matKey, _entries[matKey], CreateDescriptor(matKey, material), world);
                }
            }

            public void Release(in SharedMaterialKey matKey, SurfaceCacheWorld world)
            {
                if (matKey.Material == EntityId.None)
                    return;

                Debug.Assert(_entries.ContainsKey(matKey));
                var entry = _entries[matKey];

                if (entry.RefCount == 1)
                {
                    RemoveHandle(matKey, world);
                }
                else
                {
                    entry.RefCount -= 1;
                    _entries[matKey] = entry;
                }
            }

            public void CleanUp(SurfaceCacheWorld world)
            {
                var keys = new SharedMaterialKey[_entries.Count];

                int i = 0;
                foreach (var key in _entries.Keys)
                    keys[i++] = key;

                foreach (var key in keys)
                    RemoveHandle(key, world);
            }

            MaterialPool.MaterialDescriptor CreateDescriptor(Material material)
            {
                return MaterialPool.ConvertUnityMaterialToMaterialDescriptor(material, kEmissionMode, _resolution);
            }

            // The overrides are applied to a copy, so the meta pass and the emission read them like material values.
            MaterialPool.MaterialDescriptor CreateDescriptor(in SharedMaterialKey matKey, Material material)
            {
                if (matKey.Overrides == null)
                    return CreateDescriptor(material);

                var overridden = new Material(material) { hideFlags = HideFlags.HideAndDontSave };
                try
                {
                    ApplyOverrides(overridden, matKey.Overrides);
                    return CreateDescriptor(overridden);
                }
                finally
                {
                    CoreUtils.Destroy(overridden);
                }
            }

            static void ApplyOverrides(Material material, SurfaceCacheMaterialOverride[] overrides)
            {
                var shader = material.shader;
                bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
                foreach (var materialOverride in overrides)
                {
                    if (!TryFindNumericProperty(shader, materialOverride.NameId, out int propertyIndex))
                        continue;

                    var type = shader.GetPropertyType(propertyIndex);
                    var flags = shader.GetPropertyFlags(propertyIndex);

                    // The setters convert sRGB properties from gamma, and the override is already in the active color space.
                    bool isSRGB = (type == ShaderPropertyType.Color && (flags & ShaderPropertyFlags.HDR) == 0) || (flags & ShaderPropertyFlags.Gamma) != 0;
                    bool toGamma = isSRGB && linear;

                    var value = materialOverride.Value;
                    if (type == ShaderPropertyType.Float || type == ShaderPropertyType.Range)
                        material.SetFloat(materialOverride.NameId, toGamma ? Mathf.LinearToGammaSpace(value.x) : value.x);
                    else
                        material.SetVector(materialOverride.NameId, toGamma ? (Vector4)((Color)value).gamma : value);
                }

                // The inspector derives EmissiveIsBlack from the material's own emission color, which an override can replace.
                var giFlags = material.globalIlluminationFlags;
                bool emissiveIsBlackDerived = (giFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) != 0 && giFlags != MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                if (emissiveIsBlackDerived && HasOverride(overrides, k_EmissionColorId) && material.GetColor(k_EmissionColorId).maxColorComponent > 0f)
                    material.globalIlluminationFlags = giFlags & ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }

            static bool HasOverride(SurfaceCacheMaterialOverride[] overrides, int nameId)
            {
                foreach (var materialOverride in overrides)
                {
                    if (materialOverride.NameId == nameId)
                        return true;
                }

                return false;
            }

#if UNITY_EDITOR
            MaterialPool.MaterialDescriptor GetFallbackMaterialDescriptor()
            {
                var oldAllowAsyncCompilation = UnityEditor.ShaderUtil.allowAsyncCompilation;
                UnityEditor.ShaderUtil.allowAsyncCompilation = false;
                var descriptor = CreateDescriptor(_fallbackMaterial);
                UnityEditor.ShaderUtil.allowAsyncCompilation = oldAllowAsyncCompilation;
                return descriptor;
            }
#endif

            void UpdateInWorld(in SharedMaterialKey matKey, in Entry entry, in MaterialPool.MaterialDescriptor descriptor, SurfaceCacheWorld world)
            {
                bool updated = world.UpdateMaterial(entry.WorldHandle, in descriptor, kUVChannel);
                DestroyDescriptorTextures(descriptor);
                if (!updated)
                    ReportRejection(matKey.Material, entry.Material);
            }

            void ReportRejection(EntityId matEntityId, Material mat)
            {
                if (_reportedRejections.Add(matEntityId))
                    Debug.LogError($"Surface Cache GI: material '{mat.name}' could not be added because the material texture atlas is full ({SystemInfo.maxTextureArraySlices} slices). Lower 'Material Resolution' on the Surface Cache GI renderer feature or share materials between objects.", mat);
            }

            void RemoveHandle(in SharedMaterialKey matKey, SurfaceCacheWorld world)
            {
                _pendingMetaPassEvals.Remove(matKey);
                var entry = _entries[matKey];
                world.RemoveMaterial(entry.WorldHandle);
                _entries.Remove(matKey);
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
