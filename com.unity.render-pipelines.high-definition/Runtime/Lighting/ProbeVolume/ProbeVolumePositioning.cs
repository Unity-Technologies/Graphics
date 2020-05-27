using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition
{
    using Brick = ProbeReferenceVolume.Brick;
    using Flags = ProbeReferenceVolume.BrickFlags;
    using Volume = ProbeReferenceVolume.Volume;
    using RefTrans = ProbeReferenceVolume.RefVolTransform;

    internal static class ProbeVolumePositioning
    {
        internal static void SubdivisionAlgorithm(RefTrans refTrans, List<Brick> inBricks, List<Flags> outFlags)
        {
            Flags f = new Flags();
            for( int i = 0; i < inBricks.Count; i++ )
            {
                if( ShouldKeepBrick( ref refTrans, inBricks[i] ) )
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
            return true;
        }

        // TODO: Take refvol translation and rotation into account
        internal static bool OBBIntersect(ref RefTrans refTrans, Brick brick, ref Volume volume)
        {
            float scaledSize = Mathf.Pow(3, brick.size);
            Vector3 scaledPos = refTrans.refSpaceToWS.MultiplyPoint(brick.position);

            Volume bounds;
            bounds.Corner = scaledPos;
            bounds.X = refTrans.refSpaceToWS.GetColumn(0) * scaledSize;
            bounds.Y = refTrans.refSpaceToWS.GetColumn(1) * scaledSize;
            bounds.Z = refTrans.refSpaceToWS.GetColumn(2) * scaledSize;

            return OBBIntersect(ref bounds, ref volume);
        }

        internal static bool OBBIntersect(ref Volume a, ref Volume b)
        {
            Vector3[] axises =
            {
                a.X.normalized,
                a.Y.normalized,
                a.Z.normalized,
                b.X.normalized,
                b.Y.normalized,
                b.Z.normalized
            };

            foreach (Vector3 axis in axises)
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

        private static Vector2 ProjectOBB(ref Volume a, Vector3 axis)
        {
            float min = Vector3.Dot(axis, a.Corner);
            float max = min;

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 vert = a.Corner + a.X * x + a.Y * y + a.Z * z;

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
