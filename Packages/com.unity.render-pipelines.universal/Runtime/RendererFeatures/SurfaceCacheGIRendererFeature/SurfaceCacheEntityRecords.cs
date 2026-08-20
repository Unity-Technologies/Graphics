#if SURFACE_CACHE_SUPPORTED || UNITY_EDITOR

namespace UnityEngine.Rendering.Universal
{
    struct SurfaceCacheEntityInstanceRecord
    {
        public EntityId Key;
        // Null when the entity has no resolvable mesh; the instance is then kept out of the world.
        public Mesh Mesh;
        // Per-submesh materials, length equal to Mesh.subMeshCount. Null entries mask out the submesh.
        public Material[] Materials;
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
