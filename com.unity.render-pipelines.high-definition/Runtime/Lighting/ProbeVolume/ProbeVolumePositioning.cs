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
        internal static void SubdivisionAlgorithm(RefTrans refTrans, List<Brick> inBricks, List<Brick> outBricks)
        {
            List<Brick> level1 = new List<Brick>(inBricks);
            List<Brick> level2 = new List<Brick>();

            while (level1.Count != 0)
            {           
                ProbeReferenceVolume.SubdivideBricks(level1, level2);

                DiscardBricks(ref refTrans, level2);

                outBricks.AddRange(level2);

                var tmp = level1;
                level1 = level2;
                level2 = tmp;

                level2.Clear();
            }          
        }

        private static void DiscardBricks(ref RefTrans refTrans, List<Brick> level)
        {
            for(int i = level.Count - 1; i >= 0; i--)
            {
                if (!ShouldKeepBrick(ref refTrans, level[i]))
                {
                    level.RemoveAt(i);
                }
            }
        }

        // TODO: Add subdivision criteria here,
        // currently just keeps subdividing inside probe volumes
        internal static bool ShouldKeepBrick(ref RefTrans refTrans, Brick brick)
        {
            return IntersectsProbeVolume(ref refTrans, brick);
        }

        // TODO: Full OBB-OBB collision, perhaps using SAT. Take refvol translation and rotation into account
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
    }
}
