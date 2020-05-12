using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition
{
    using Brick = ProbeReferenceVolume.Brick;
    using Volume = ProbeReferenceVolume.Volume;

    internal static class ProbeVolumePositioning
    {
        internal static void SubDivideBricks(Matrix4x4 refSpaceToWS, List<Brick> inBricks, List<Brick> outBricks)
        {
            List<Brick> level = new List<Brick>(inBricks);

            while (level.Count != 0)
            {
                level = SubDivideLevel(level);
                outBricks.AddRange(level);
            }
        }

        private static List<Brick> SubDivideLevel(List<Brick> level)
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

                        var child = new Brick(brick.Position + offset, thirdSubDivLevel);
                        if (ShouldKeepBrick(child))
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
        internal static bool ShouldKeepBrick(Brick brick)
        {
            return IntersectsProbeVolume(brick);
        }

        // TODO: Full OBB-OBB collision, perhaps using SAT
        // TODO: Take refvol translation and rotation into account
        internal static bool IntersectsProbeVolume(Brick brick)
        {
            float minCellSize = ProbeVolumeManager.manager.refVol.minBrickSize();
            Vector3 scaledSize = Vector3.one * Mathf.Pow(3, brick.size) * minCellSize;
            Vector3 scaledPos = (Vector3)brick.Position * minCellSize + scaledSize / 2;
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
