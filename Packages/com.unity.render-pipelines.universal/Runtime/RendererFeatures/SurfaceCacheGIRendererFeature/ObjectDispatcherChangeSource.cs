#if SURFACE_CACHE_SUPPORTED || UNITY_EDITOR

using System;
using Unity.Collections;
using ObjectDispatcher = UnityEngine.InternalBridge.ObjectDispatcher;
using TypeDispatchData = UnityEngine.InternalBridge.TypeDispatchData;

namespace UnityEngine.Rendering.Universal
{
    sealed class ObjectDispatcherChangeSource : IDisposable
    {
        readonly ObjectDispatcher _dispatcher = new();

        public ObjectDispatcherChangeSource()
        {
#if UNITY_EDITOR
            _dispatcher.maxDispatchHistoryFramesCount = int.MaxValue;
#endif
            _dispatcher.EnableTypeTracking<MeshRenderer>(ObjectDispatcher.TypeTrackingFlags.SceneObjects);
            _dispatcher.EnableTransformTracking<MeshRenderer>(ObjectDispatcher.TransformTrackingType.GlobalTRS);
            _dispatcher.EnableTypeTracking<Light>(ObjectDispatcher.TypeTrackingFlags.SceneObjects);
            _dispatcher.EnableTransformTracking<Light>(ObjectDispatcher.TransformTrackingType.GlobalTRS);
            _dispatcher.EnableTypeTracking<Material>(ObjectDispatcher.TypeTrackingFlags.SceneObjects | ObjectDispatcher.TypeTrackingFlags.Assets);
#if ENABLE_TERRAIN_MODULE
            _dispatcher.EnableTypeTracking<Terrain>(ObjectDispatcher.TypeTrackingFlags.SceneObjects);
            _dispatcher.EnableTransformTracking<Terrain>(ObjectDispatcher.TransformTrackingType.GlobalTRS);
            _dispatcher.EnableTypeTracking<TerrainData>(ObjectDispatcher.TypeTrackingFlags.SceneObjects | ObjectDispatcher.TypeTrackingFlags.Assets);
#endif
        }

        static readonly Unity.Profiling.ProfilerMarker k_CollectMarker = new("SurfaceCache.GameObjectChangeCollection");

        public ObjectDispatcherChangeSet CollectChanges()
        {
            using var _ = k_CollectMarker.Auto();

            var meshRendererTransforms = _dispatcher.GetTransformChangesAndClear<MeshRenderer>(ObjectDispatcher.TransformTrackingType.GlobalTRS, false);
            var meshRendererTypeChanges = _dispatcher.GetTypeChangesAndClear<MeshRenderer>(Allocator.Temp);
            var lightTransforms = _dispatcher.GetTransformChangesAndClear<Light>(ObjectDispatcher.TransformTrackingType.GlobalTRS, false);
            var lightTypeChanges = _dispatcher.GetTypeChangesAndClear<Light>(Allocator.Temp);
            var materialTypeChanges = _dispatcher.GetTypeChangesAndClear<Material>(Allocator.Temp);
#if ENABLE_TERRAIN_MODULE
            var terrainTransforms = _dispatcher.GetTransformChangesAndClear<Terrain>(ObjectDispatcher.TransformTrackingType.GlobalTRS, false);
            var terrainTypeChanges = _dispatcher.GetTypeChangesAndClear<Terrain>(Allocator.Temp);
            var terrainDataTypeChanges = _dispatcher.GetTypeChangesAndClear<TerrainData>(Allocator.Temp);
#endif

            var changeSet = new SurfaceCacheWorldChangeSet
            {
                MeshRendererTransformChangedList = meshRendererTransforms,
                MeshRendererChangedList = meshRendererTypeChanges.changed,
                MeshRendererDestroyedList = meshRendererTypeChanges.destroyedID,
                LightTransformChangedList = lightTransforms,
                LightChangedList = lightTypeChanges.changed,
                LightDestroyedList = lightTypeChanges.destroyedID,
                MaterialChangedList = materialTypeChanges.changed,
#if ENABLE_TERRAIN_MODULE
                TerrainTransformChangedList = terrainTransforms,
                TerrainChangedList = terrainTypeChanges.changed,
                TerrainDestroyedList = terrainTypeChanges.destroyedID,
                TerrainDataChangedList = terrainDataTypeChanges.changed,
#endif
            };

            return new ObjectDispatcherChangeSet(
                changeSet,
                meshRendererTypeChanges,
                lightTypeChanges,
                materialTypeChanges
#if ENABLE_TERRAIN_MODULE
                , terrainTypeChanges
                , terrainDataTypeChanges
#endif
            );
        }

        public void Dispose()
        {
            _dispatcher.Dispose();
        }
    }

    readonly struct ObjectDispatcherChangeSet : IDisposable
    {
        readonly TypeDispatchData _meshRendererTypeChanges;
        readonly TypeDispatchData _lightTypeChanges;
        readonly TypeDispatchData _materialTypeChanges;
#if ENABLE_TERRAIN_MODULE
        readonly TypeDispatchData _terrainTypeChanges;
        readonly TypeDispatchData _terrainDataTypeChanges;
#endif

        public SurfaceCacheWorldChangeSet WorldChangeSet { get; }

        internal ObjectDispatcherChangeSet(
            SurfaceCacheWorldChangeSet changeSet,
            TypeDispatchData meshRendererTypeChanges,
            TypeDispatchData lightTypeChanges,
            TypeDispatchData materialTypeChanges
#if ENABLE_TERRAIN_MODULE
            , TypeDispatchData terrainTypeChanges
            , TypeDispatchData terrainDataTypeChanges
#endif
        )
        {
            WorldChangeSet = changeSet;
            _meshRendererTypeChanges = meshRendererTypeChanges;
            _lightTypeChanges = lightTypeChanges;
            _materialTypeChanges = materialTypeChanges;
#if ENABLE_TERRAIN_MODULE
            _terrainTypeChanges = terrainTypeChanges;
            _terrainDataTypeChanges = terrainDataTypeChanges;
#endif
        }

        public void Dispose()
        {
            _meshRendererTypeChanges.Dispose();
            _lightTypeChanges.Dispose();
            _materialTypeChanges.Dispose();
#if ENABLE_TERRAIN_MODULE
            _terrainTypeChanges.Dispose();
            _terrainDataTypeChanges.Dispose();
#endif
        }
    }
}

#endif
