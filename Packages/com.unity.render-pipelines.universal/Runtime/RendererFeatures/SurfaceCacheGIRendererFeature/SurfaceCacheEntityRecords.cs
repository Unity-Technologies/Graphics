#if SURFACE_CACHE_SUPPORTED || UNITY_EDITOR

using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    struct SurfaceCacheSubMeshMaterial
    {
        public int SubMeshIndex;
        public EntityId Material;
    }

    // Value as the shader receives it, so colors are in the active color space.
    struct SurfaceCacheMaterialOverride
    {
        public int NameId;
        public Vector4 Value;
    }

    struct SurfaceCacheEntityInstanceRecord
    {
        public EntityId Key;
        public EntityId Mesh;
        public int MeshLod;
        // Into the buffer this record was drained with, so valid only until that buffer is reused.
        public int MaterialsStart;
        public int MaterialsCount;
        // Into the buffer this record was drained with, like the materials. Applies to every submesh.
        public int OverridesStart;
        public int OverridesCount;
        // Pending transforms below this index were pushed before this record.
        public int TransformsBefore;
        public Matrix4x4 LocalToWorld;
        public uint RenderingLayerMask;
        public bool Visible;
    }

    struct SurfaceCacheEntityTransformRecord
    {
        public EntityId Key;
        public Matrix4x4 LocalToWorld;
    }

    struct SurfaceCacheCullingObserver
    {
        public float3 Position;
        public float MaxDistance;
        public float MinAngle;
    }
}

#endif
