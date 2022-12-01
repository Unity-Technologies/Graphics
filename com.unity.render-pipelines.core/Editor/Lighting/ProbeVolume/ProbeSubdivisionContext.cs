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
                    if (probeVolume == null || !probeVolume.isActiveAndEnabled || ProbeReferenceVolume.instance.sceneData == null)
                        return;

                    var profile = ProbeReferenceVolume.instance.sceneData.GetBakingSetForScene(probeVolume.gameObject.scene);
                    if (profile == null)
                        return;

                    if (s_CurrentSubdivision == null)
                    {
                        // Start a new Subdivision
                        s_CurrentSubdivision = Subdivide();
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

                    IEnumerator Subdivide()
                    {
                        var ctx = ProbeGIBaking.PrepareProbeSubdivisionContext();

                        // Cull all the cells that are not visible (we don't need them for realtime debug)
                        ctx.cells.RemoveAll(c =>
                        {
                            return probeVolume.ShouldCullCell(c.position);
                        });

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

                            var result = ProbeGIBaking.BakeBricks(ctx);

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
        public GIContributors contributors;
        public ProbeVolumeBakingSet profile;

        public void Initialize(ProbeVolumeBakingSet profile, Vector3 refVolOrigin)
        {
            Profiling.Profiler.BeginSample("ProbeSubdivisionContext.Initialize");

            this.profile = profile;
            float cellSize = profile.cellSizeInMeters;
            Vector3 cellDimensions = new Vector3(cellSize, cellSize, cellSize);

            var pvList = ProbeGIBaking.GetProbeVolumeList();
            foreach (var pv in pvList)
            {
                if (!pv.isActiveAndEnabled)
                    continue;

                ProbeReferenceVolume.Volume volume = new ProbeReferenceVolume.Volume(Matrix4x4.TRS(pv.transform.position, pv.transform.rotation, pv.GetExtents()), pv.GetMaxSubdivMultiplier(), pv.GetMinSubdivMultiplier());
                probeVolumes.Add((pv, volume, volume.CalculateAABB()));
            }

            contributors = GIContributors.Find(GIContributors.ContributorFilter.All);

            // Generate all the unique cell positions from probe volumes:
            HashSet<Vector3Int> cellPositions = new HashSet<Vector3Int>();
            foreach (var pv in probeVolumes)
            {
                // This method generates many cells outside of the probe volumes but it's ok because next step will do obb collision tests between each cell and each probe volumes so we will eliminate them.
                var minCellPosition = pv.bounds.min / cellSize;
                var maxCellPosition = pv.bounds.max / cellSize;

                Vector3Int min = new Vector3Int(Mathf.FloorToInt(minCellPosition.x), Mathf.FloorToInt(minCellPosition.y), Mathf.FloorToInt(minCellPosition.z));
                Vector3Int max = new Vector3Int(Mathf.CeilToInt(maxCellPosition.x), Mathf.CeilToInt(maxCellPosition.y), Mathf.CeilToInt(maxCellPosition.z));

                for (int x = min.x; x < max.x; x++)
                {
                    for (int y = min.y; y < max.y; y++)
                        for (int z = min.z; z < max.z; z++)
                        {
                            var cellPos = new Vector3Int(x, y, z);
                            if (cellPositions.Add(cellPos))
                            {
                                var center = new Vector3((cellPos.x + 0.5f) * cellSize, (cellPos.y + 0.5f) * cellSize, (cellPos.z + 0.5f) * cellSize);
                                cells.Add((cellPos, new Bounds(center, cellDimensions)));
                            }
                        }
                }
            }

            Profiling.Profiler.EndSample();
        }
    }
}
