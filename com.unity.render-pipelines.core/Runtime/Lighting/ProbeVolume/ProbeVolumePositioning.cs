#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Experimental.Rendering
{
    using Brick = ProbeBrickIndex.Brick;
    using RefTrans = ProbeReferenceVolume.RefVolTransform;

    internal static class ProbeVolumePositioning
    {
        internal static Vector3[] m_Axes = new Vector3[6];

        // TODO: Take refvol translation and rotation into account
        public static ProbeReferenceVolume.Volume CalculateBrickVolume(in RefTrans refTrans, Brick brick)
        {
            float scaledSize = Mathf.Pow(3, brick.subdivisionLevel);
            Vector3 scaledPos = refTrans.refSpaceToWS.MultiplyPoint(brick.position);

            var bounds = new ProbeReferenceVolume.Volume(
                scaledPos,
                refTrans.refSpaceToWS.GetColumn(0) * scaledSize,
                refTrans.refSpaceToWS.GetColumn(1) * scaledSize,
                refTrans.refSpaceToWS.GetColumn(2) * scaledSize
            );

            return bounds;
        }

        public static bool OBBIntersect(in RefTrans refTrans, Brick brick, in ProbeReferenceVolume.Volume volume)
        {
            var transformed = CalculateBrickVolume(in refTrans, brick);
            return OBBIntersect(in transformed, in volume);
        }

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

            foreach (Vector3 axis in m_Axes)
            {
                Vector2 aProj = ProjectOBB(in a, axis);
                Vector2 bProj = ProjectOBB(in b, axis);

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
    }
}

#endif
