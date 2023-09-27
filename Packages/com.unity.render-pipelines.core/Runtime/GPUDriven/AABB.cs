using System;
using UnityEngine;
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

        static float3 RotateExtents(float3 extents, float3 m0, float3 m1, float3 m2)
        {
            return math.abs(m0 * extents.x) + math.abs(m1 * extents.y) + math.abs(m2 * extents.z);
        }

        public static AABB Transform(float4x4 transform, AABB localBounds)
        {
            AABB transformed;
            transformed.extents = RotateExtents(localBounds.extents, transform.c0.xyz, transform.c1.xyz, transform.c2.xyz);
            transformed.center = math.transform(transform, localBounds.center);
            return transformed;
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
