using UnityEngine;

namespace Unity.Mathematics
{
    public static class AABBExtensions
    {
        public static AABB ToAABB(this Bounds bounds)
        {
            return new AABB { Center = bounds.center, Extents = bounds.extents};
        }

        public static Bounds ToBounds(this AABB aabb)
        {
            return new Bounds { center = aabb.Center, extents = aabb.Extents};
        }
    }
}
