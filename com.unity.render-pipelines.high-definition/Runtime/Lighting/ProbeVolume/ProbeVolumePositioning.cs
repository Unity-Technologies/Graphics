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
