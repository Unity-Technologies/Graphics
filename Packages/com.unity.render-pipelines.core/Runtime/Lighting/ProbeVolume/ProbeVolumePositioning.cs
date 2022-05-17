#if UNITY_EDITOR

namespace UnityEngine.Rendering
{
    internal static class ProbeVolumePositioning
    {
        internal static Vector3[] m_Axes = new Vector3[6];
        internal static Vector3[] m_AABBCorners = new Vector3[8];

        public static bool OBBIntersect(in ProbeReferenceVolume.Volume a, in ProbeReferenceVolume.Volume b)
        {
            // First we test if the bounding spheres intersects, in which case we case do the more complex OBB test
            a.CalculateCenterAndSize(out var aCenter, out var aSize);
            b.CalculateCenterAndSize(out var bCenter, out var bSize);

            var aRadius = aSize.sqrMagnitude / 2.0f;
            var bRadius = bSize.sqrMagnitude / 2.0f;
            if (Vector3.SqrMagnitude(aCenter - bCenter) > aRadius + bRadius)
                return false;

            m_Axes[0] = a.X.normalized;
            m_Axes[1] = a.Y.normalized;
            m_Axes[2] = a.Z.normalized;
            m_Axes[3] = b.X.normalized;
            m_Axes[4] = b.Y.normalized;
            m_Axes[5] = b.Z.normalized;

            for (int i = 0; i < 6; i++)
            {
                Vector2 aProj = ProjectOBB(in a, m_Axes[i]);
                Vector2 bProj = ProjectOBB(in b, m_Axes[i]);

                if (aProj.y < bProj.x || bProj.y < aProj.x)
                {
                    return false;
                }
            }

            return true;
        }

        // Test between a OBB and an AABB. The AABB of the OBB is requested to avoid recalculating it
        public static bool OBBAABBIntersect(in ProbeReferenceVolume.Volume a, in Bounds b, in Bounds aAABB)
        {
            // First perform fast AABB test
            if (!aAABB.Intersects(b))
                return false;

            // Perform complex OBB test
            Vector3 boundsMin = b.min, boundsMax = b.max;
            m_AABBCorners[0] = new Vector3(boundsMin.x, boundsMin.y, boundsMin.z);
            m_AABBCorners[1] = new Vector3(boundsMax.x, boundsMin.y, boundsMin.z);
            m_AABBCorners[2] = new Vector3(boundsMax.x, boundsMax.y, boundsMin.z);
            m_AABBCorners[3] = new Vector3(boundsMin.x, boundsMax.y, boundsMin.z);
            m_AABBCorners[4] = new Vector3(boundsMin.x, boundsMin.y, boundsMax.z);
            m_AABBCorners[5] = new Vector3(boundsMax.x, boundsMin.y, boundsMax.z);
            m_AABBCorners[6] = new Vector3(boundsMax.x, boundsMax.y, boundsMax.z);
            m_AABBCorners[7] = new Vector3(boundsMin.x, boundsMax.y, boundsMax.z);

            m_Axes[0] = a.X.normalized;
            m_Axes[1] = a.Y.normalized;
            m_Axes[2] = a.Z.normalized;

            for (int i = 0; i < 3; i++)
            {
                Vector2 aProj = ProjectOBB(in a, m_Axes[i]);
                Vector2 bProj = ProjectAABB(m_AABBCorners, m_Axes[i]);

                if (aProj.y < bProj.x || bProj.y < aProj.x)
                {
                    return false;
                }
            }

            return true;
        }

        static Vector2 ProjectOBB(in ProbeReferenceVolume.Volume a, Vector3 axis)
        {
            float min = Vector3.Dot(axis, a.corner);
            float max = min;

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 vert = a.corner + a.X * x + a.Y * y + a.Z * z;

                        float proj = Vector3.Dot(axis, vert);

                        if (proj < min)
                        {
                            min = proj;
                        }
                        else if (proj > max)
                        {
                            max = proj;
                        }
                    }
                }
            }

            return new Vector2(min, max);
        }

        static Vector2 ProjectAABB(in Vector3[] corners, Vector3 axis)
        {
            float min = Vector3.Dot(axis, corners[0]);
            float max = min;
            for (int i = 1; i < 8; i++)
            {
                float proj = Vector3.Dot(axis, corners[i]);
                if (proj < min) min = proj;
                else if (proj > max)  max = proj;
            }

            return new Vector2(min, max);
        }
    }
}

#endif
