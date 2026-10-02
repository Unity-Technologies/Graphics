#if SURFACE_CACHE_SUPPORTED || UNITY_EDITOR

namespace UnityEngine.Rendering.Universal
{
    struct SurfaceCacheSubMeshMaterial
    {
        public int SubMeshIndex;
        public EntityId Material;
    }

    struct SurfaceCacheEntityInstanceRecord
    {
        public EntityId Key;
        public EntityId Mesh;
        // Into the buffer this record was drained with, so valid only until that buffer is reused.
        public int MaterialsStart;
        public int MaterialsCount;
        public Matrix4x4 LocalToWorld;
        public uint RenderingLayerMask;
        public bool Visible;
    }

    struct SurfaceCacheEntityTransformRecord
    {
        public EntityId Key;
        public Matrix4x4 LocalToWorld;
    }
}

#endif
