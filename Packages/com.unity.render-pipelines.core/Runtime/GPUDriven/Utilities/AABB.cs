using System;
using Unity.Mathematics;

namespace UnityEngine.Rendering
{
    [Serializable]
    internal partial struct AABB
    {
        public float3 center;
        public float3 extents; // half the size, must be 0 or greater

        public float3 min { get { return center - extents; } }
        public float3 max { get { return center + extents; } }

        /// <summary>Returns a string representation of the AABB.</summary>
        public override string ToString()
        {
            return $"AABB(Center:{center}, Extents:{extents}";
        }
    }

    internal static class AABBExtensions
    {
        public static AABB ToAABB(this Bounds bounds)
        {
            return new AABB { center = bounds.center, extents = bounds.extents };
        }

        public static Bounds ToBounds(this AABB aabb)
        {
            return new Bounds { center = aabb.center, extents = aabb.extents };
        }
    }
}
