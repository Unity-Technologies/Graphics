#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace UnityEngine.Rendering
{
    using Brick = ProbeBrickIndex.Brick;
    using Flags = ProbeReferenceVolume.BrickFlags;
    using RefTrans = ProbeReferenceVolume.RefVolTransform;

    internal class ProbePlacement
    {
        static protected ProbeReferenceVolume.Volume ToVolume(Bounds bounds)
        {
            ProbeReferenceVolume.Volume v = new ProbeReferenceVolume.Volume();
            v.corner = bounds.center - bounds.size * 0.5f;
            v.X = new Vector3(bounds.size.x, 0, 0);
            v.Y = new Vector3(0, bounds.size.y, 0);
            v.Z = new Vector3(0, 0, bounds.size.z);
            return v;
        }

        static void TrackSceneRefs(Scene origin, ref Dictionary<Scene, int> sceneRefs)
        {
            if (!sceneRefs.ContainsKey(origin))
                sceneRefs[origin] = 0;
            else
                sceneRefs[origin] += 1;
        }

        static protected int RenderersToVolumes(ref Renderer[] renderers, ref ProbeReferenceVolume.Volume cellVolume, ref List<ProbeReferenceVolume.Volume> volumes, ref Dictionary<Scene, int> sceneRefs)
        {
            int num = 0;

            foreach (Renderer r in renderers)
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI;
                bool contributeGI = (flags & StaticEditorFlags.ContributeGI) != 0;

                if (!r.enabled || !r.gameObject.activeSelf || !contributeGI)
                    continue;

                ProbeReferenceVolume.Volume v = ToVolume(r.bounds);

                if (ProbeVolumePositioning.OBBIntersect(ref cellVolume, ref v))
                {
                    volumes.Add(v);

                    TrackSceneRefs(r.gameObject.scene, ref sceneRefs);

                    num++;
                }
            }

            return num;
        }

        static protected int NavPathsToVolumes(ref ProbeReferenceVolume.Volume cellVolume, ref List<ProbeReferenceVolume.Volume> volumes, ref Dictionary<Scene, int> sceneRef)
        {
            // TODO
            return 0;
        }

        static protected int ImportanceVolumesToVolumes(ref ProbeReferenceVolume.Volume cellVolume, ref List<ProbeReferenceVolume.Volume> volumes, ref Dictionary<Scene, int> sceneRef)
        {
            // TODO
            return 0;
        }

        static protected int LightsToVolumes(ref ProbeReferenceVolume.Volume cellVolume, ref List<ProbeReferenceVolume.Volume> volumes, ref Dictionary<Scene, int> sceneRef)
        {
            // TODO
            return 0;
        }

        static protected int ProbeVolumesToVolumes(ref ProbeVolume[] probeVolumes, ref ProbeReferenceVolume.Volume cellVolume, ref List<ProbeReferenceVolume.Volume> volumes, ref Dictionary<Scene, int> sceneRefs)
        {
            int num = 0;

            foreach (ProbeVolume pv in probeVolumes)
            {
                if (!pv.enabled || !pv.gameObject.activeSelf)
                    continue;

                ProbeReferenceVolume.Volume indicatorVolume = new ProbeReferenceVolume.Volume(Matrix4x4.TRS(pv.transform.position, pv.transform.rotation, pv.GetExtents()));

                if (ProbeVolumePositioning.OBBIntersect(ref cellVolume, ref indicatorVolume))
                {
                    volumes.Add(indicatorVolume);
                    TrackSceneRefs(pv.gameObject.scene, ref sceneRefs);
                    num++;
                }
            }

            return num;
        }

        static protected void CullVolumes(ref List<ProbeReferenceVolume.Volume> cullees, ref List<ProbeReferenceVolume.Volume> cullers, ref List<ProbeReferenceVolume.Volume> result)
        {
            foreach (ProbeReferenceVolume.Volume v in cullers)
            {
                ProbeReferenceVolume.Volume lv = v;

                foreach (ProbeReferenceVolume.Volume c in cullees)
                {
                    if (result.Contains(c))
                        continue;

                    ProbeReferenceVolume.Volume lc = c;

                    if (ProbeVolumePositioning.OBBIntersect(ref lv, ref lc))
                        result.Add(c);
                }
            }
        }

        static public void CreateInfluenceVolumes(Vector3Int cellPos,
            Renderer[] renderers, ProbeVolume[] probeVolumes,
            ProbeReferenceVolumeAuthoring settings, Matrix4x4 cellTrans, out List<ProbeReferenceVolume.Volume> culledVolumes, out Dictionary<Scene, int> sceneRefs)
        {
            ProbeReferenceVolume.Volume cellVolume = new ProbeReferenceVolume.Volume();
            cellVolume.corner = new Vector3(cellPos.x * settings.cellSize, cellPos.y * settings.cellSize, cellPos.z * settings.cellSize);
            cellVolume.X = new Vector3(settings.cellSize, 0, 0);
            cellVolume.Y = new Vector3(0, settings.cellSize, 0);
            cellVolume.Z = new Vector3(0, 0, settings.cellSize);
            cellVolume.Transform(cellTrans);

            // Keep track of volumes and which scene they originated from
            sceneRefs = new Dictionary<Scene, int>();

            // Extract all influencers inside the cell
            List<ProbeReferenceVolume.Volume> influenceVolumes = new List<ProbeReferenceVolume.Volume>();
            RenderersToVolumes(ref renderers, ref cellVolume, ref influenceVolumes, ref sceneRefs);
            NavPathsToVolumes(ref cellVolume, ref influenceVolumes, ref sceneRefs);
            ImportanceVolumesToVolumes(ref cellVolume, ref influenceVolumes, ref sceneRefs);
            LightsToVolumes(ref cellVolume, ref influenceVolumes, ref sceneRefs);

            // Extract all ProbeVolumes inside the cell
            List<ProbeReferenceVolume.Volume> indicatorVolumes = new List<ProbeReferenceVolume.Volume>();
            ProbeVolumesToVolumes(ref probeVolumes, ref cellVolume, ref indicatorVolumes, ref sceneRefs);

            // Cull all influencers against ProbeVolumes
            culledVolumes = new List<ProbeReferenceVolume.Volume>();
            CullVolumes(ref influenceVolumes, ref indicatorVolumes, ref culledVolumes);
        }

        public static void SubdivisionAlgorithm(ProbeReferenceVolume.Volume cellVolume, List<ProbeReferenceVolume.Volume> probeVolumes, List<ProbeReferenceVolume.Volume> influenceVolumes, RefTrans refTrans, List<Brick> inBricks, List<Flags> outFlags)
        {
            Flags f = new Flags();
            for (int i = 0; i < inBricks.Count; i++)
            {
                ProbeReferenceVolume.Volume brickVolume = ProbeVolumePositioning.CalculateBrickVolume(ref refTrans, inBricks[i]);

                // Keep bricks that overlap at least one probe volume, and at least one influencer (mesh)
                if (ShouldKeepBrick(probeVolumes, brickVolume) && ShouldKeepBrick(influenceVolumes, brickVolume))
                {
                    f.subdivide = true;

                    // Transform into refvol space
                    brickVolume.Transform(refTrans.refSpaceToWS.inverse);
                    ProbeReferenceVolume.Volume cellVolumeTrans = new ProbeReferenceVolume.Volume(cellVolume);
                    cellVolumeTrans.Transform(refTrans.refSpaceToWS.inverse);

                    // Discard parent brick if it extends outside of the cell, to prevent duplicates
                    var brickVolumeMax = brickVolume.corner + brickVolume.X + brickVolume.Y + brickVolume.Z;
                    var cellVolumeMax = cellVolumeTrans.corner + cellVolumeTrans.X + cellVolumeTrans.Y + cellVolumeTrans.Z;

                    f.discard = brickVolumeMax.x > cellVolumeMax.x ||
                        brickVolumeMax.y > cellVolumeMax.y ||
                        brickVolumeMax.z > cellVolumeMax.z ||
                        brickVolume.corner.x < cellVolumeTrans.corner.x ||
                        brickVolume.corner.y < cellVolumeTrans.corner.y ||
                        brickVolume.corner.z < cellVolumeTrans.corner.z;
                }
                else
                {
                    f.discard = true;
                    f.subdivide = false;
                }
                outFlags.Add(f);
            }
        }

        internal static bool ShouldKeepBrick(List<ProbeReferenceVolume.Volume> volumes, ProbeReferenceVolume.Volume brick)
        {
            foreach (ProbeReferenceVolume.Volume v in volumes)
            {
                ProbeReferenceVolume.Volume vol = v;
                if (ProbeVolumePositioning.OBBIntersect(ref vol, ref brick))
                    return true;
            }

            return false;
        }

        public static void Subdivide(Vector3Int cellPosGridSpace, ProbeReferenceVolume refVol, float cellSize, Vector3 translation, Quaternion rotation, List<ProbeReferenceVolume.Volume> influencerVolumes,
            ref Vector3[] positions, ref List<ProbeBrickIndex.Brick> bricks)
        {
            //TODO: This per-cell volume is calculated 2 times during probe placement. We should calculate it once and reuse it.
            ProbeReferenceVolume.Volume cellVolume = new ProbeReferenceVolume.Volume();
            cellVolume.corner = new Vector3(cellPosGridSpace.x * cellSize, cellPosGridSpace.y * cellSize, cellPosGridSpace.z * cellSize);
            cellVolume.X = new Vector3(cellSize, 0, 0);
            cellVolume.Y = new Vector3(0, cellSize, 0);
            cellVolume.Z = new Vector3(0, 0, cellSize);
            cellVolume.Transform(Matrix4x4.TRS(translation, rotation, Vector3.one));

            // TODO move out
            var indicatorVolumes = new List<ProbeReferenceVolume.Volume>();
            foreach (ProbeVolume pv in UnityEngine.Object.FindObjectsOfType<ProbeVolume>())
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
            refVol.CreateBricks(new List<ProbeReferenceVolume.Volume>() { cellVolume }, subdivDel, bricks, out numProbes);

            positions = new Vector3[numProbes];
            refVol.ConvertBricks(bricks, positions);
        }
    }
}

#endif
