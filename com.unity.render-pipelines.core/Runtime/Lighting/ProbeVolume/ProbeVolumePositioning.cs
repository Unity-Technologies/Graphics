#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Rendering
{
    using Brick = ProbeBrickIndex.Brick;
    using Flags = ProbeReferenceVolume.BrickFlags;
    using RefTrans = ProbeReferenceVolume.RefVolTransform;

    internal static class ProbeVolumePositioning
    {
        internal static Vector3[] m_Axes = new Vector3[6];

        public static void SubdivisionAlgorithm(RefTrans refTrans, List<Brick> inBricks, List<Flags> outFlags)
        {
            Flags f = new Flags();
            for (int i = 0; i < inBricks.Count; i++)
            {
                if (ShouldKeepBrick(ref refTrans, inBricks[i]))
                {
                    f.discard = false;
                    f.subdivide = true;
                }
                else
                {
                    f.discard = true;
                    f.subdivide = false;
                }
                outFlags.Add(f);
            }
        }

        // TODO: Add subdivision criteria here,
        // currently just keeps subdividing inside probe volumes
        internal static bool ShouldKeepBrick(ref RefTrans refTrans, Brick brick)
        {
            Renderer[] renderers = Object.FindObjectsOfType<Renderer>();
            foreach (Renderer r in renderers)
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI;
                bool contributeGI = (flags & StaticEditorFlags.ContributeGI) != 0;

                if (!r.enabled || !contributeGI)
                    continue;

                ProbeReferenceVolume.Volume v = new ProbeReferenceVolume.Volume();
                v.corner = r.bounds.center - r.bounds.size * 0.5f;
                v.X = new Vector3(r.bounds.size.x, 0, 0);
                v.Y = new Vector3(0, r.bounds.size.y, 0);
                v.Z = new Vector3(0, 0, r.bounds.size.z);

                if (OBBIntersect(ref refTrans, brick, ref v))
                    return true;
            }

            return false;
        }

        // TODO: Take refvol translation and rotation into account
        public static ProbeReferenceVolume.Volume CalculateBrickVolume(ref RefTrans refTrans, Brick brick)
        {
            float scaledSize = Mathf.Pow(3, brick.size);
            Vector3 scaledPos = refTrans.refSpaceToWS.MultiplyPoint(brick.position);

            ProbeReferenceVolume.Volume bounds;
            bounds.corner = scaledPos;
            bounds.X = refTrans.refSpaceToWS.GetColumn(0) * scaledSize;
            bounds.Y = refTrans.refSpaceToWS.GetColumn(1) * scaledSize;
            bounds.Z = refTrans.refSpaceToWS.GetColumn(2) * scaledSize;

            return bounds;
        }

        public static bool OBBIntersect(ref RefTrans refTrans, Brick brick, ref ProbeReferenceVolume.Volume volume)
        {
            var transformed = CalculateBrickVolume(ref refTrans, brick);
            return OBBIntersect(ref transformed, ref volume);
        }

        public static bool OBBIntersect(ref ProbeReferenceVolume.Volume a, ref ProbeReferenceVolume.Volume b)
        {
            m_Axes[0] = a.X.normalized;
            m_Axes[1] = a.Y.normalized;
            m_Axes[2] = a.Z.normalized;
            m_Axes[3] = b.X.normalized;
            m_Axes[4] = b.Y.normalized;
            m_Axes[5] = b.Z.normalized;

            foreach (Vector3 axis in m_Axes)
            {
                Vector2 aProj = ProjectOBB(ref a, axis);
                Vector2 bProj = ProjectOBB(ref b, axis);

                if (aProj.y < bProj.x || bProj.y < aProj.x)
                {
                    return false;
                }
            }

            return true;
        }

        private static Vector2 ProjectOBB(ref ProbeReferenceVolume.Volume a, Vector3 axis)
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
