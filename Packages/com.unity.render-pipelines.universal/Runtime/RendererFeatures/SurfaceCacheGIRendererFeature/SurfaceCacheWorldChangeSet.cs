#if SURFACE_CACHE_SUPPORTED || UNITY_EDITOR

using System;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal
{
    sealed class SurfaceCacheWorldChangeSet
    {
        public IEnumerable<Component> MeshRendererTransformChangedList { get; set; } = Array.Empty<Component>();
        public IEnumerable<Object> MeshRendererChangedList { get; set; } = Array.Empty<Object>();
        public IEnumerable<EntityId> MeshRendererDestroyedList { get; set; } = Array.Empty<EntityId>();

        public IEnumerable<Component> LightTransformChangedList { get; set; } = Array.Empty<Component>();
        public IEnumerable<Object> LightChangedList { get; set; } = Array.Empty<Object>();
        public IEnumerable<EntityId> LightDestroyedList { get; set; } = Array.Empty<EntityId>();

        public IEnumerable<Object> MaterialChangedList { get; set; } = Array.Empty<Object>();

        public IEnumerable<SurfaceCacheEntityInstanceRecord> EntityInstanceChangedList { get; set; } = Array.Empty<SurfaceCacheEntityInstanceRecord>();
        public NativeArray<SurfaceCacheEntityTransformRecord> EntityInstanceTransformChangedList { get; set; }
        public IEnumerable<EntityId> EntityInstanceDestroyedList { get; set; } = Array.Empty<EntityId>();

#if ENABLE_TERRAIN_MODULE
        public IEnumerable<Component> TerrainTransformChangedList { get; set; } = Array.Empty<Component>();
        public IEnumerable<Object> TerrainChangedList { get; set; } = Array.Empty<Object>();
        public IEnumerable<EntityId> TerrainDestroyedList { get; set; } = Array.Empty<EntityId>();
        public IEnumerable<Object> TerrainDataChangedList { get; set; } = Array.Empty<Object>();
#endif
    }
}

#endif
