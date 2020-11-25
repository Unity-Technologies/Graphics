#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition
{
    using Brick = ProbeBrickIndex.Brick;
    using Flags = ProbeReferenceVolume.BrickFlags;
    using Volume = ProbeReferenceVolume.Volume;
    using RefTrans = ProbeReferenceVolume.RefVolTransform;

    public class ProbePlacement
    {
        static protected Volume ToVolume(Bounds bounds)
        {
            Volume v = new Volume();
            v.Corner = bounds.center - bounds.size * 0.5f;
            v.X = new Vector3(bounds.size.x, 0, 0);
            v.Y = new Vector3(0, bounds.size.y, 0);
            v.Z = new Vector3(0, 0, bounds.size.z);
            return v;
        }

        static protected int RenderersToVolumes(ref Renderer[] renderers, ref Volume cellVolume, ref List<Volume> volumes)
        {
            int num = 0;
            
            foreach (Renderer r in renderers)
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI;
                bool contributeGI = (flags & StaticEditorFlags.ContributeGI) != 0;

                if (!r.enabled || !contributeGI)
                    continue;

                Volume v = ToVolume(r.bounds);

                if (ProbeVolumePositioning.OBBIntersect(ref cellVolume, ref v))
                { 
                    volumes.Add(v);
                    num++;
                }
            }

            return num;
        }

        static protected int NavPathsToVolumes(ref Volume cellVolume, ref List<Volume> volumes)
        {
            // TODO
            return 0;
        }

        static protected int ImportanceVolumesToVolumes(ref Volume cellVolume, ref List<Volume> volumes)
        {
            // TODO
            return 0;
        }

        static protected int LightsToVolumes(ref Volume cellVolume, ref List<Volume> volumes)
        {
            // TODO
            return 0;
        }

        static protected int ProbeVolumesToVolumes(ref ProbeVolume[] probeVolumes, ref Volume cellVolume, ref List<Volume> volumes)
        {
            int num = 0;

            foreach (ProbeVolume pv in probeVolumes)
            {
                if (!pv.enabled)
                    continue;

                Volume indicatorVolume = new Volume(Matrix4x4.TRS(pv.transform.position, pv.transform.rotation, pv.GetExtents()));

                if (ProbeVolumePositioning.OBBIntersect(ref cellVolume, ref indicatorVolume))
                { 
                    volumes.Add(indicatorVolume);
                    num++;
                }
            }

            return num;
        }

        static protected void CullVolumes(ref List<Volume> cullees, ref List<Volume> cullers, ref List<Volume> result)
        {
            foreach (Volume v in cullers)
            {
                Volume lv = v;

                foreach (Volume c in cullees)
                {
                    if (result.Contains(c))
                        continue;

                    Volume lc = c;

                    if (ProbeVolumePositioning.OBBIntersect(ref lv, ref lc))
                        result.Add(c);
                }
            }
        }

        static public void CreateInfluenceVolumes(Vector3Int cellPos,
            Renderer[] renderers, ProbeVolume[] probeVolumes,
            ProbeReferenceVolumeAuthoring settings, Matrix4x4 cellTrans, out List<Volume> culledVolumes)
        {
            Volume cellVolume = new Volume();
            cellVolume.Corner = new Vector3(cellPos.x * settings.CellSize, cellPos.y * settings.CellSize, cellPos.z * settings.CellSize);
            cellVolume.X = new Vector3(settings.CellSize, 0, 0);
            cellVolume.Y = new Vector3(0, settings.CellSize, 0);
            cellVolume.Z = new Vector3(0, 0, settings.CellSize);
            cellVolume.Transform(cellTrans);

            // Extract all influencers inside the cell
            List<Volume> influenceVolumes = new List<Volume>();
            RenderersToVolumes(ref renderers, ref cellVolume, ref influenceVolumes);
            NavPathsToVolumes(ref cellVolume, ref influenceVolumes);
            ImportanceVolumesToVolumes(ref cellVolume, ref influenceVolumes);
            LightsToVolumes(ref cellVolume, ref influenceVolumes);

            // Extract all ProbeVolumes inside the cell
            List<Volume> indicatorVolumes = new List<Volume>();
            ProbeVolumesToVolumes(ref probeVolumes, ref cellVolume, ref indicatorVolumes);

            // Cull all influencers against ProbeVolumes
            culledVolumes = new List<Volume>();
            CullVolumes(ref influenceVolumes, ref indicatorVolumes, ref culledVolumes);
        }

        public static void SubdivisionAlgorithm(Volume cellVolume, List<Volume> probeVolumes, List<Volume> influenceVolumes, RefTrans refTrans, List<Brick> inBricks, List<Flags> outFlags)
        {
            Flags f = new Flags();
            for (int i = 0; i < inBricks.Count; i++)
            {
                Volume brickVolume = ProbeVolumePositioning.CalculateBrickVolume(ref refTrans, inBricks[i]);

                // Keep bricks that overlap at least one probe volume, and at least one influencer (mesh)
                if (ShouldKeepBrick(probeVolumes, brickVolume) && ShouldKeepBrick(influenceVolumes, brickVolume))
                {
                    f.subdivide = true;

                    // Transform into refvol space
                    brickVolume.Transform(refTrans.refSpaceToWS.inverse);
                    Volume cellVolumeTrans = new Volume(cellVolume);
                    cellVolumeTrans.Transform(refTrans.refSpaceToWS.inverse);

                    // Discard parent brick if it extends outside of the cell, to prevent duplicates
                    var brickVolumeMax = brickVolume.Corner + brickVolume.X + brickVolume.Y + brickVolume.Z;
                    var cellVolumeMax = cellVolumeTrans.Corner + cellVolumeTrans.X + cellVolumeTrans.Y + cellVolumeTrans.Z;

                    f.discard = brickVolume.Corner.x < cellVolumeTrans.Corner.x ||
                                brickVolume.Corner.y < cellVolumeTrans.Corner.y ||
                                brickVolume.Corner.z < cellVolumeTrans.Corner.z ||
                                brickVolumeMax.x > cellVolumeMax.x ||
                                brickVolumeMax.y > cellVolumeMax.y ||
                                brickVolumeMax.z > cellVolumeMax.z;
                }
                else
                {
                    f.discard = true;
                    f.subdivide = false;
                }
                outFlags.Add(f);
            }
        }

        internal static bool ShouldKeepBrick(List<Volume> volumes, Volume brick)
        {
            foreach (Volume v in volumes)
            {
                Volume vol = v;
                if (ProbeVolumePositioning.OBBIntersect(ref vol, ref brick))
                    return true;
            }

            return false;
        }

        public static void Subdivide(Vector3Int cellPosGridSpace, ProbeReferenceVolume refVol, float cellSize, Vector3 translation, Quaternion rotation, List<Volume> influencerVolumes, 
            ref Vector3[] positions, ref List<ProbeBrickIndex.Brick> bricks)
        {
            //TODO: This per-cell volume is calculated 2 times during probe placement. We should calculate it once and reuse it.
            Volume cellVolume = new Volume();
            cellVolume.Corner = new Vector3(cellPosGridSpace.x * cellSize, cellPosGridSpace.y * cellSize, cellPosGridSpace.z * cellSize);
            cellVolume.X = new Vector3(cellSize, 0, 0);
            cellVolume.Y = new Vector3(0, cellSize, 0);
            cellVolume.Z = new Vector3(0, 0, cellSize);
            cellVolume.Transform(Matrix4x4.TRS(translation, rotation, Vector3.one));

            // TODO move out
            var indicatorVolumes = new List<ProbeReferenceVolume.Volume>();
            foreach(ProbeVolume pv in UnityEngine.Object.FindObjectsOfType<ProbeVolume>())
            {
                if (!pv.enabled)
                    continue;

                indicatorVolumes.Add(new ProbeReferenceVolume.Volume(Matrix4x4.TRS(pv.transform.position, pv.transform.rotation, pv.GetExtents())));
            }

            ProbeReferenceVolume.SubdivisionDel subdivDel =
                (RefTrans refTrans, List<Brick> inBricks, List<Flags> outFlags) =>
                { SubdivisionAlgorithm(cellVolume, indicatorVolumes, influencerVolumes, refTrans, inBricks, outFlags); };

            bricks = new List<ProbeBrickIndex.Brick>();

            // get a list of bricks for this volume
            int numProbes;
            refVol.CreateBricks(new List<Volume>() { cellVolume }, subdivDel, bricks, out numProbes);

            positions = new Vector3[numProbes];
            refVol.ConvertBricks(bricks, positions);
        }
    }
}

#endif
