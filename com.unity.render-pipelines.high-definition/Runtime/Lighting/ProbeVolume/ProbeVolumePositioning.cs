using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition
{
    using Brick = ProbeReferenceVolume.Brick;
    using Volume = ProbeReferenceVolume.Volume;
    using RefTrans = ProbeReferenceVolume.RefVolTransform;

    internal static class ProbeVolumePositioning
    {
        internal static void SubDivideBricks(RefTrans refTrans, List<Brick> inBricks, List<Brick> outBricks)
        {
            List<Brick> level = new List<Brick>(inBricks);

            while (level.Count != 0)
            {
                level = SubDivideLevel(ref refTrans, level);
                outBricks.AddRange(level);
            }
        }

        private static List<Brick> SubDivideLevel(ref RefTrans refTrans, List<Brick> level)
        {
            List<Brick> result = new List<Brick>();

            // Subdivide into new level
            for (int i = 0; i < level.Count; i++)
            {
                Brick brick = level[i];
                if (brick.size > 0)
                {
                    int thirdSubDivLevel = brick.size - 1;
                    int thirdSize = (int)Mathf.Pow(3, thirdSubDivLevel);

                    for (int b = 0; b < 27; b++)
                    {
                        Vector3Int offset = Position3D(3, 3, b) * thirdSize;

                        var child = new Brick(brick.position + offset, thirdSubDivLevel);
                        if (ShouldKeepBrick(ref refTrans, child))
                        {
                            result.Add(child);
                        }
                    }
                }
            }

            return result;
        }

        // TODO: Add subdivision criteria here,
        // currently just keeps subdividing inside probe volumes
        internal static bool ShouldKeepBrick(ref RefTrans refTrans, Brick brick)
        {
            return IntersectsProbeVolume(ref refTrans, brick);
        }

        // TODO: Full OBB-OBB collision, perhaps using SAT
        // TODO: Take refvol translation and rotation into account
        internal static bool IntersectsProbeVolume(ref RefTrans refTrans, Brick brick)
        {
            Vector3 scaledSize = refTrans.scale * Mathf.Pow(3, brick.size) * Vector3.one;
            Vector3 scaledPos = refTrans.refSpaceToWS.MultiplyPoint(brick.position) + scaledSize / 2;
            Bounds bounds = new Bounds(scaledPos, scaledSize);

            bool result = false;
            foreach (ProbeVolume v in ProbeVolumeManager.manager.volumes)
            {
                var OBB = new Volume(Matrix4x4.TRS(v.transform.position, v.transform.rotation, v.parameters.size));
                if (bounds.Intersects(OBB.CalculateAABB()))
                {
                    result = true;
                }
            }
            return result;
        }

        private static Vector3Int Position3D(int width, int height, int idx)
        {
            int x = idx % width;
            int y = (idx / width) % height;
            int z = idx / (width * height);

            return new Vector3Int(x, y, z);
        }

        private static int Index3D(int width, int height, Vector3Int pos)
        {
            return pos.x + width * (pos.y + height * pos.z);
        }
    }
}
