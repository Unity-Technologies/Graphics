namespace UnityEngine.Rendering
{
    internal static class ProbeVolumePositioning
    {
        // Scratch buffers reused across calls to avoid per-call allocation.
        // Every code path fully writes all elements before reading — no reset needed on domain reload.
        static readonly Vector3[] s_Axes = new Vector3[6];
        static readonly Vector3[] s_AABBCorners = new Vector3[8];

        public static bool OBBIntersect(in ProbeReferenceVolume.Volume a, in ProbeReferenceVolume.Volume b)
        {
            // First we test if the bounding spheres intersects, in which case we case do the more complex OBB test
            a.CalculateCenterAndSize(out var aCenter, out var aSize);
            b.CalculateCenterAndSize(out var bCenter, out var bSize);

            var aRadius = aSize.sqrMagnitude / 2.0f;
            var bRadius = bSize.sqrMagnitude / 2.0f;
            if (Vector3.SqrMagnitude(aCenter - bCenter) > aRadius + bRadius)
                return false;

            s_Axes[0] = a.m_X.normalized;
            s_Axes[1] = a.m_Y.normalized;
            s_Axes[2] = a.m_Z.normalized;
            s_Axes[3] = b.m_X.normalized;
            s_Axes[4] = b.m_Y.normalized;
            s_Axes[5] = b.m_Z.normalized;

            for (int i = 0; i < 6; i++)
            {
                Vector2 aProj = ProjectOBB(in a, s_Axes[i]);
                Vector2 bProj = ProjectOBB(in b, s_Axes[i]);

                if (aProj.y < bProj.x || bProj.y < aProj.x)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool OBBContains(in ProbeReferenceVolume.Volume obb, Vector3 point)
        {
            float lenX2 = obb.m_X.sqrMagnitude;
            float lenY2 = obb.m_Y.sqrMagnitude;
            float lenZ2 = obb.m_Z.sqrMagnitude;

            // Project in OBB space
            point -= obb.m_Corner;
            point = new Vector3(Vector3.Dot(point, obb.m_X), Vector3.Dot(point, obb.m_Y), Vector3.Dot(point, obb.m_Z));

            return (0.0f < point.x && point.x < lenX2) && (0.0f < point.y && point.y < lenY2) && (0.0f < point.z && point.z < lenZ2);
        }

        // Test between a OBB and an AABB. The AABB of the OBB is requested to avoid recalculating it
        public static bool OBBAABBIntersect(in ProbeReferenceVolume.Volume a, in Bounds b, in Bounds aAABB)
        {
            // First perform fast AABB test
            if (!aAABB.Intersects(b))
                return false;

            // Perform complex OBB test
            Vector3 boundsMin = b.min, boundsMax = b.max;
            s_AABBCorners[0] = new Vector3(boundsMin.x, boundsMin.y, boundsMin.z);
            s_AABBCorners[1] = new Vector3(boundsMax.x, boundsMin.y, boundsMin.z);
            s_AABBCorners[2] = new Vector3(boundsMax.x, boundsMax.y, boundsMin.z);
            s_AABBCorners[3] = new Vector3(boundsMin.x, boundsMax.y, boundsMin.z);
            s_AABBCorners[4] = new Vector3(boundsMin.x, boundsMin.y, boundsMax.z);
            s_AABBCorners[5] = new Vector3(boundsMax.x, boundsMin.y, boundsMax.z);
            s_AABBCorners[6] = new Vector3(boundsMax.x, boundsMax.y, boundsMax.z);
            s_AABBCorners[7] = new Vector3(boundsMin.x, boundsMax.y, boundsMax.z);

            s_Axes[0] = a.m_X.normalized;
            s_Axes[1] = a.m_Y.normalized;
            s_Axes[2] = a.m_Z.normalized;

            for (int i = 0; i < 3; i++)
            {
                Vector2 aProj = ProjectOBB(in a, s_Axes[i]);
                Vector2 bProj = ProjectAABB(s_AABBCorners, s_Axes[i]);

                if (aProj.y < bProj.x || bProj.y < aProj.x)
                {
                    return false;
                }
            }

            return true;
        }

        static Vector2 ProjectOBB(in ProbeReferenceVolume.Volume a, Vector3 axis)
        {
            float min = Vector3.Dot(axis, a.m_Corner);
            float max = min;

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 vert = a.m_Corner + a.m_X * x + a.m_Y * y + a.m_Z * z;

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
                else if (proj > max) max = proj;
            }

            return new Vector2(min, max);
        }
    }
}
