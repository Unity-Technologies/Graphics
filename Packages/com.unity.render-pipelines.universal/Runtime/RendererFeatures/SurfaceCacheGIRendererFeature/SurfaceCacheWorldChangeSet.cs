#if SURFACE_CACHE_SUPPORTED || UNITY_EDITOR

using System;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal
{
    sealed class SurfaceCacheWorldChangeSet
    {
        // Ids and matrices as the dispatcher produced them; parallel arrays, same length.
        public NativeArray<EntityId> MeshRendererTransformChangedIds { get; set; }
        public NativeArray<Matrix4x4> MeshRendererTransformChangedLocalToWorlds { get; set; }
        public IReadOnlyList<Object> MeshRendererChangedList { get; set; } = Array.Empty<Object>();
        public NativeArray<EntityId> MeshRendererDestroyedList { get; set; }

        public IReadOnlyList<Component> LightTransformChangedList { get; set; } = Array.Empty<Component>();
        public IReadOnlyList<Object> LightChangedList { get; set; } = Array.Empty<Object>();
        public NativeArray<EntityId> LightDestroyedList { get; set; }

        public IReadOnlyList<Object> MaterialChangedList { get; set; } = Array.Empty<Object>();

        public NativeArray<EntityId> MeshDestroyedList { get; set; }

        public NativeArray<SurfaceCacheEntityInstanceRecord> EntityInstanceChangedList { get; set; }
        public NativeArray<SurfaceCacheSubMeshMaterial> EntityInstanceMaterialList { get; set; }
        public NativeArray<SurfaceCacheEntityTransformRecord> EntityInstanceTransformChangedList { get; set; }
        public NativeArray<EntityId> EntityInstanceDestroyedList { get; set; }

#if ENABLE_TERRAIN_MODULE
        public IReadOnlyList<Component> TerrainTransformChangedList { get; set; } = Array.Empty<Component>();
        public IReadOnlyList<Object> TerrainChangedList { get; set; } = Array.Empty<Object>();
        public NativeArray<EntityId> TerrainDestroyedList { get; set; }
        public IReadOnlyList<Object> TerrainDataChangedList { get; set; } = Array.Empty<Object>();
#endif
    }
}

#endif
