#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace UnityEngine.Rendering
{
    using Brick = ProbeBrickIndex.Brick;
    using Flags = ProbeReferenceVolume.BrickFlags;
    using ReferenceVolume = ProbeReferenceVolume.Volume;
    using RefTrans = ProbeReferenceVolume.RefVolTransform;

    internal class ProbePlacement
    {
        static protected ReferenceVolume ToVolume(Bounds bounds)
        {
            ReferenceVolume v = new ReferenceVolume();
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

        static protected int RenderersToVolumes(ref Renderer[] renderers, ref ReferenceVolume cellVolume, ref List<ReferenceVolume> volumes, ref Dictionary<Scene, int> sceneRefs)
        {
            int num = 0;

            foreach (Renderer r in renderers)
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI;
                bool contributeGI = (flags & StaticEditorFlags.ContributeGI) != 0;

                if (!r.enabled || !r.gameObject.activeSelf || !contributeGI)
                    continue;

                ReferenceVolume v = ToVolume(r.bounds);

                if (ProbeVolumePositioning.OBBIntersect(ref cellVolume, ref v))
                {
                    volumes.Add(v);

                    TrackSceneRefs(r.gameObject.scene, ref sceneRefs);

                    num++;
                }
            }

            return num;
        }

        static protected int NavPathsToVolumes(ref ReferenceVolume cellVolume, ref List<ReferenceVolume> volumes, ref Dictionary<Scene, int> sceneRef)
        {
            // TODO
            return 0;
        }

        static protected int ImportanceVolumesToVolumes(ref ReferenceVolume cellVolume, ref List<ReferenceVolume> volumes, ref Dictionary<Scene, int> sceneRef)
        {
            // TODO
            return 0;
        }

        static protected int LightsToVolumes(ref ReferenceVolume cellVolume, ref List<ReferenceVolume> volumes, ref Dictionary<Scene, int> sceneRef)
        {
            // TODO
            return 0;
        }

        static protected int ProbeVolumesToVolumes(ref ProbeVolume[] probeVolumes, ref ReferenceVolume cellVolume, ref List<ReferenceVolume> volumes, ref Dictionary<Scene, int> sceneRefs)
        {
            int num = 0;

            foreach (ProbeVolume pv in probeVolumes)
            {
                if (!pv.enabled || !pv.gameObject.activeSelf)
                    continue;

                ReferenceVolume indicatorVolume = new ReferenceVolume(Matrix4x4.TRS(pv.transform.position, pv.transform.rotation, pv.GetExtents()));

                if (ProbeVolumePositioning.OBBIntersect(ref cellVolume, ref indicatorVolume))
                {
                    volumes.Add(indicatorVolume);
                    TrackSceneRefs(pv.gameObject.scene, ref sceneRefs);
                    num++;
                }
            }

            return num;
        }

        static protected void CullVolumes(ref List<ReferenceVolume> cullees, ref List<ReferenceVolume> cullers, ref List<ReferenceVolume> result)
        {
            foreach (ReferenceVolume v in cullers)
            {
                ReferenceVolume lv = v;

                foreach (ReferenceVolume c in cullees)
                {
                    if (result.Contains(c))
                        continue;

                    ReferenceVolume lc = c;

                    if (ProbeVolumePositioning.OBBIntersect(ref lv, ref lc))
                        result.Add(c);
                }
            }
        }

        static public void CreateInfluenceVolumes(Vector3Int cellPos,
            Renderer[] renderers, ProbeVolume[] probeVolumes,
            ProbeReferenceVolumeAuthoring settings, Matrix4x4 cellTrans, out List<ReferenceVolume> culledVolumes, out Dictionary<Scene, int> sceneRefs)
        {
            ReferenceVolume cellVolume = new ReferenceVolume();
            cellVolume.corner = new Vector3(cellPos.x * settings.cellSize, cellPos.y * settings.cellSize, cellPos.z * settings.cellSize);
            cellVolume.X = new Vector3(settings.cellSize, 0, 0);
            cellVolume.Y = new Vector3(0, settings.cellSize, 0);
            cellVolume.Z = new Vector3(0, 0, settings.cellSize);
            cellVolume.Transform(cellTrans);

            // Keep track of volumes and which scene they originated from
            sceneRefs = new Dictionary<Scene, int>();

            // Extract all influencers inside the cell
            List<ReferenceVolume> influenceVolumes = new List<ReferenceVolume>();
            RenderersToVolumes(ref renderers, ref cellVolume, ref influenceVolumes, ref sceneRefs);
            NavPathsToVolumes(ref cellVolume, ref influenceVolumes, ref sceneRefs);
            ImportanceVolumesToVolumes(ref cellVolume, ref influenceVolumes, ref sceneRefs);
            LightsToVolumes(ref cellVolume, ref influenceVolumes, ref sceneRefs);

            // Extract all ProbeVolumes inside the cell
            List<ReferenceVolume> indicatorVolumes = new List<ReferenceVolume>();
            ProbeVolumesToVolumes(ref probeVolumes, ref cellVolume, ref indicatorVolumes, ref sceneRefs);

            // Cull all influencers against ProbeVolumes
            culledVolumes = new List<ReferenceVolume>();
            CullVolumes(ref influenceVolumes, ref indicatorVolumes, ref culledVolumes);
        }

        public static void SubdivisionAlgorithm(ReferenceVolume cellVolume, List<ReferenceVolume> probeVolumes, List<ReferenceVolume> influenceVolumes, RefTrans refTrans, List<Brick> inBricks, List<Flags> outFlags)
        {
            Flags f = new Flags();
            for (int i = 0; i < inBricks.Count; i++)
            {
                ReferenceVolume brickVolume = ProbeVolumePositioning.CalculateBrickVolume(ref refTrans, inBricks[i]);

                // Keep bricks that overlap at least one probe volume, and at least one influencer (mesh)
                if (ShouldKeepBrick(probeVolumes, brickVolume) && ShouldKeepBrick(influenceVolumes, brickVolume))
                {
                    f.subdivide = true;

                    // Transform into refvol space
                    brickVolume.Transform(refTrans.refSpaceToWS.inverse);
                    ReferenceVolume cellVolumeTrans = new ReferenceVolume(cellVolume);
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

        internal static bool ShouldKeepBrick(List<ReferenceVolume> volumes, ReferenceVolume brick)
        {
            foreach (ReferenceVolume v in volumes)
            {
                ReferenceVolume vol = v;
                if (ProbeVolumePositioning.OBBIntersect(ref vol, ref brick))
                    return true;
            }

            return false;
        }

        public static void Subdivide(Vector3Int cellPosGridSpace, ProbeReferenceVolume refVol, float cellSize, Vector3 translation, Quaternion rotation, List<ReferenceVolume> influencerVolumes,
            ref Vector3[] positions, ref List<ProbeBrickIndex.Brick> bricks)
        {
            //TODO: This per-cell volume is calculated 2 times during probe placement. We should calculate it once and reuse it.
            ReferenceVolume cellVolume = new ReferenceVolume();
            cellVolume.corner = new Vector3(cellPosGridSpace.x * cellSize, cellPosGridSpace.y * cellSize, cellPosGridSpace.z * cellSize);
            cellVolume.X = new Vector3(cellSize, 0, 0);
            cellVolume.Y = new Vector3(0, cellSize, 0);
            cellVolume.Z = new Vector3(0, 0, cellSize);
            cellVolume.Transform(Matrix4x4.TRS(translation, rotation, Vector3.one));

            // TODO move out
            var indicatorVolumes = new List<ReferenceVolume>();
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
            refVol.CreateBricks(new List<ReferenceVolume>() { cellVolume }, subdivDel, bricks, out numProbes);

            positions = new Vector3[numProbes];
            refVol.ConvertBricks(bricks, positions);
        }
    }
}

#endif
