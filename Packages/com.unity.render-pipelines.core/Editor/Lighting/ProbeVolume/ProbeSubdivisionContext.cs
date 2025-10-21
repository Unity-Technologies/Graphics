using System.Collections.Generic;
using Unity.Collections;
using System;
using System.Collections;
using System.Linq;
using UnityEditor;

using Brick = UnityEngine.Rendering.ProbeBrickIndex.Brick;
using UnityEngine.SceneManagement;

namespace UnityEngine.Rendering
{
    class ProbeSubdivisionContext
    {
        [InitializeOnLoad]
        class RealtimeProbeSubdivisionDebug
        {
            static double s_LastSubdivisionTime;
            static double s_LastRefreshTime;
            static IEnumerator s_CurrentSubdivision;

            static RealtimeProbeSubdivisionDebug()
            {
                EditorApplication.update -= UpdateRealtimeSubdivisionDebug;
                EditorApplication.update += UpdateRealtimeSubdivisionDebug;
            }

            static void UpdateRealtimeSubdivisionDebug()
            {
                var debugDisplay = ProbeReferenceVolume.instance.probeVolumeDebug;
                if (!debugDisplay.realtimeSubdivision)
                    return;

                // Avoid killing the GPU when Unity is in background and runInBackground is disabled
                if (!Application.runInBackground && !UnityEditorInternal.InternalEditorUtility.isApplicationActive)
                    return;

                // update is called 200 times per second so we bring down the update rate to 60hz to avoid overloading the GPU
                if (Time.realtimeSinceStartupAsDouble - s_LastRefreshTime < 1.0f / 60.0f)
                    return;
                s_LastRefreshTime = Time.realtimeSinceStartupAsDouble;

                if (Time.realtimeSinceStartupAsDouble - s_LastSubdivisionTime > debugDisplay.subdivisionDelayInSeconds)
                {
                    var probeVolume = GameObject.FindFirstObjectByType<ProbeVolume>();
                    if (probeVolume == null || !probeVolume.isActiveAndEnabled)
                        return;

                    var profile = ProbeVolumeBakingSet.GetBakingSetForScene(probeVolume.gameObject.scene);
                    if (profile == null)
                        return;

                    if (s_CurrentSubdivision == null)
                    {
                        // Start a new Subdivision
                        s_CurrentSubdivision = Subdivide(showProgress: false);
                    }

                    // Step the subdivision with the amount of cell per frame in debug menu
                    int updatePerFrame = debugDisplay.subdivisionCellUpdatePerFrame;
                    // From simplification level 5 and higher, the cost of calculating one cell is very high, so we adjust that number.
                    if (profile.simplificationLevels > 4)
                        updatePerFrame = (int)Mathf.Max(1, updatePerFrame / Mathf.Pow(9, profile.simplificationLevels - 4));
                    for (int i = 0; i < debugDisplay.subdivisionCellUpdatePerFrame; i++)
                    {
                        if (!s_CurrentSubdivision.MoveNext())
                        {
                            s_LastSubdivisionTime = Time.realtimeSinceStartupAsDouble;
                            s_CurrentSubdivision = null;
                            break;
                        }
                    }

                    IEnumerator Subdivide(bool showProgress)
                    {
                        var perSceneDataList = AdaptiveProbeVolumes.GetPerSceneDataList();
                        var ctx = AdaptiveProbeVolumes.PrepareProbeSubdivisionContext(perSceneDataList, true);
                        var contributors = GIContributors.Find(GIContributors.ContributorFilter.All);

                        var cullCtx = new ProbeVolume.CellCullingContext
                        {
                            ActiveCamera = null,
                            FrustumPlanes = stackalloc Plane[6]
                        };
                        ProbeVolume.PrepareCellCulling(ref cullCtx);

                        var sceneToBakingSetMap = ProbeVolumeBakingSet.SceneToBakingSet.Instance;
                        var probeRefVol = ProbeReferenceVolume.instance;

                        // Cull all the cells that are not visible (we don't need them for realtime debug)
                        for (int i = ctx.cells.Count - 1; i >= 0; i--)
                        {
                            var cell = ctx.cells[i];
                            bool shouldRemove = probeVolume.ShouldCullCell(cullCtx, sceneToBakingSetMap, probeRefVol, cell.position);
                            if (shouldRemove)
                                ctx.cells.RemoveAt(i);
                        }

                        Camera activeCamera = Camera.current ?? SceneView.lastActiveSceneView.camera;

                        // Sort cells by camera distance to compute the closest cells first
                        if (activeCamera != null)
                        {
                            var cameraPos = activeCamera.transform.position;
                            ctx.cells.Sort((c1, c2) =>
                            {
                                float c1Distance = Vector3.Distance(cameraPos, c1.bounds.center);
                                float c2Distance = Vector3.Distance(cameraPos, c2.bounds.center);

                                return c1Distance.CompareTo(c2Distance);
                            });
                        }

                        // Progressively update cells:
                        var cells = ctx.cells.ToList();

                        // Remove all the cells that was not updated to prevent ghosting
                        foreach (var cellBounds in ProbeReferenceVolume.instance.realtimeSubdivisionInfo.Keys.ToList())
                        {
                            if (!cells.Any(c => c.bounds.Equals(cellBounds)))
                                ProbeReferenceVolume.instance.realtimeSubdivisionInfo.Remove(cellBounds);
                        }

                        // Subdivide visible cells
                        foreach (var cell in cells)
                        {
                            // Override the cell list to only compute one cell
                            ctx.cells.Clear();
                            ctx.cells.Add(cell);

                            bool canceledByUser = false;
                            var result = AdaptiveProbeVolumes.BakeBricks(ctx, contributors, showProgress, ref canceledByUser);

                            if (result.cells.Count != 0)
                                ProbeReferenceVolume.instance.realtimeSubdivisionInfo[cell.bounds] = result.cells[0].bricks;
                            else
                                ProbeReferenceVolume.instance.realtimeSubdivisionInfo.Remove(cell.bounds);

                            yield return null;
                        }

                        yield break;
                    }
                }
            }
        }

        public List<(ProbeVolume component, ProbeReferenceVolume.Volume volume, Bounds bounds)> probeVolumes = new ();
        public List<(Vector3Int position, Bounds bounds)> cells = new ();
        public ProbeVolumeBakingSet bakingSet;
        public ProbeVolumeProfileInfo profile;

        public void Initialize(ProbeVolumeBakingSet bakingSet, ProbeVolumeProfileInfo profileInfo, Vector3 refVolOrigin)
        {
            Profiling.Profiler.BeginSample("ProbeSubdivisionContext.Initialize");

            this.bakingSet = bakingSet;
            profile = profileInfo;
            float cellSize = profileInfo.cellSizeInMeters;
            Vector3 cellDimensions = new Vector3(cellSize, cellSize, cellSize);

            var pvList = AdaptiveProbeVolumes.GetProbeVolumeList();
            foreach (var pv in pvList)
            {
                if (!pv.isActiveAndEnabled)
                    continue;

                var volume = new ProbeReferenceVolume.Volume(pv.GetVolume(), 1, -1);
                probeVolumes.Add((pv, volume, volume.CalculateAABB()));
            }

            // Generate all the unique cell positions from probe volumes:
            HashSet<Vector3Int> cellPositions = new HashSet<Vector3Int>();
            foreach (var pv in probeVolumes)
            {
                // This method generates many cells outside of the probe volumes but it's ok because next step will do obb collision tests between each cell and each probe volumes so we will eliminate them.
                var min = profileInfo.PositionToCell(pv.bounds.min);
                var max = profileInfo.PositionToCell(pv.bounds.max);

                for (int x = min.x; x <= max.x; x++)
                {
                    for (int y = min.y; y <= max.y; y++)
                    {
                        for (int z = min.z; z <= max.z; z++)
                        {
                            var cellPos = new Vector3Int(x, y, z);
                            if (cellPositions.Add(cellPos))
                            {
                                var center = profileInfo.probeOffset + new Vector3((cellPos.x + 0.5f) * cellSize, (cellPos.y + 0.5f) * cellSize, (cellPos.z + 0.5f) * cellSize);
                                cells.Add((cellPos, new Bounds(center, cellDimensions)));
                            }
                        }
                    }
                }
            }

            Profiling.Profiler.EndSample();
        }
    }
}
