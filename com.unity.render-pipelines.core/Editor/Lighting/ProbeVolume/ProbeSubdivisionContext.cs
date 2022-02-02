using System.Collections.Generic;
using Unity.Collections;
using System;
using System.Collections;
using System.Linq;
using UnityEditor;

using Brick = UnityEngine.Experimental.Rendering.ProbeBrickIndex.Brick;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
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
                var debugDisplay = ProbeReferenceVolume.instance.debugDisplay;
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
                    var probeVolume = GameObject.FindObjectOfType<ProbeVolume>();
                    if (probeVolume == null || !probeVolume.isActiveAndEnabled || ProbeReferenceVolume.instance.sceneData == null)
                        return;

                    var profile = ProbeReferenceVolume.instance.sceneData.GetProfileForScene(probeVolume.gameObject.scene);
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
                                c1.volume.CalculateCenterAndSize(out var c1Center, out var _);
                                float c1Distance = Vector3.Distance(cameraPos, c1Center);

                                c2.volume.CalculateCenterAndSize(out var c2Center, out var _);
                                float c2Distance = Vector3.Distance(cameraPos, c2Center);

                                return c1Distance.CompareTo(c2Distance);
                            });
                        }

                        // Progressively update cells:
                        var cells = ctx.cells.ToList();

                        // Remove all the cells that was not updated to prevent ghosting
                        foreach (var cellVolume in ProbeReferenceVolume.instance.realtimeSubdivisionInfo.Keys.ToList())
                        {
                            if (!cells.Any(c => c.volume.Equals(cellVolume)))
                                ProbeReferenceVolume.instance.realtimeSubdivisionInfo.Remove(cellVolume);
                        }

                        // Subdivide visible cells
                        foreach (var cell in cells)
                        {
                            // Override the cell list to only compute one cell
                            ctx.cells.Clear();
                            ctx.cells.Add(cell);

                            var result = ProbeGIBaking.BakeBricks(ctx);
                            ProbeReferenceVolume.instance.realtimeSubdivisionInfo[cell.volume] = result.bricksPerCells[cell.position];

                            yield return null;
                        }

                        yield break;
                    }
                }
            }
        }

        public List<(ProbeVolume component, ProbeReferenceVolume.Volume volume)> probeVolumes = new List<(ProbeVolume, ProbeReferenceVolume.Volume)>();
        public List<(Renderer component, ProbeReferenceVolume.Volume volume)> renderers = new List<(Renderer, ProbeReferenceVolume.Volume)>();
        public List<(Vector3Int position, ProbeReferenceVolume.Volume volume)> cells = new List<(Vector3Int, ProbeReferenceVolume.Volume)>();
        public List<(Terrain, ProbeReferenceVolume.Volume volume)> terrains = new List<(Terrain, ProbeReferenceVolume.Volume)>();
        public ProbeReferenceVolumeProfile profile;

        public void Initialize(ProbeReferenceVolumeProfile profile, Vector3 refVolOrigin)
        {
            this.profile = profile;
            float cellSize = profile.cellSizeInMeters;

            foreach (var pv in UnityEngine.Object.FindObjectsOfType<ProbeVolume>())
            {
                if (!pv.isActiveAndEnabled)
                    continue;

                ProbeReferenceVolume.Volume volume = new ProbeReferenceVolume.Volume(Matrix4x4.TRS(pv.transform.position, pv.transform.rotation, pv.GetExtents()), pv.GetMaxSubdivMultiplier(), pv.GetMinSubdivMultiplier());
                probeVolumes.Add((pv, volume));
            }

            // Find all renderers in the scene
            foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>())
            {
                if (!r.enabled || !r.gameObject.activeSelf)
                    continue;

                var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
                if ((flags & StaticEditorFlags.ContributeGI) == 0)
                    continue;

                // Inflate a bit the volume in case it's too small (plane case)
                var volume = ProbePlacement.ToVolume(new Bounds(r.bounds.center, r.bounds.size + Vector3.one * 0.01f));

                renderers.Add((r, volume));
            }

            foreach (var terrain in UnityEngine.Object.FindObjectsOfType<Terrain>())
            {
                if (!terrain.isActiveAndEnabled)
                    continue;

                var volume = ProbePlacement.ToVolume(terrain.terrainData.bounds);

                terrains.Add((terrain, volume));
            }

            // Generate all the unique cell positions from probe volumes:
            var cellTrans = Matrix4x4.TRS(refVolOrigin, Quaternion.identity, Vector3.one);
            HashSet<Vector3Int> cellPositions = new HashSet<Vector3Int>();
            foreach (var pv in probeVolumes)
            {
                var probeVolume = pv.component;
                var halfSize = probeVolume.size / 2.0f;
                var minCellPosition = (probeVolume.transform.position - halfSize) / cellSize;
                var maxCellPosition = (probeVolume.transform.position + halfSize) / cellSize;

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
                                // Calculate the cell volume:
                                ProbeReferenceVolume.Volume cellVolume = new ProbeReferenceVolume.Volume();
                                cellVolume.corner = new Vector3(cellPos.x * cellSize, cellPos.y * cellSize, cellPos.z * cellSize);
                                cellVolume.X = new Vector3(cellSize, 0, 0);
                                cellVolume.Y = new Vector3(0, cellSize, 0);
                                cellVolume.Z = new Vector3(0, 0, cellSize);
                                cells.Add((cellPos, cellVolume));
                            }
                        }
                }
            }
        }
    }
}
