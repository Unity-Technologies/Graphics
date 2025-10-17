using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Rendering
{
    internal static class ProbeVolumeGizmos
    {
        static MeshGizmo _brickMeshGizmo;
        static MeshGizmo _cellMeshGizmo;
        static double _lastDrawAt = 0;

        static readonly string _gizmoPath = "Packages/com.unity.render-pipelines.core/Editor/Resources/Gizmos";
        static readonly string _probeAdjustmentVolumeIconPath = _gizmoPath + "/ProbeTouchupVolume.png";
        static readonly string _probeVolumeIconPath = _gizmoPath + "/ProbeVolume.png";

        static ProbeVolumeGizmos()
        {
            EditorApplication.update += Update;
        }

        static void Update()
        {
            bool resourcesAllocated = _brickMeshGizmo != null || _cellMeshGizmo != null;

            if (resourcesAllocated)
            {
                bool shouldCleanUp = EditorApplication.timeSinceStartup - _lastDrawAt > 1.0;
                if (shouldCleanUp)
                {
                    _brickMeshGizmo?.Dispose();
                    _brickMeshGizmo = null;
                    _cellMeshGizmo?.Dispose();
                    _cellMeshGizmo = null;
                }
            }
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected | GizmoType.NonSelected)]
        static void DrawProbeAdjustmentVolumes(ProbeAdjustmentVolume volume, GizmoType gizmoType)
        {
            Gizmos.DrawIcon(volume.transform.position, _probeAdjustmentVolumeIconPath, true);
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected | GizmoType.NonSelected)]
        static void DrawProbeVolumeGizmos(ProbeVolume volume, GizmoType gizmoType)
        {
            _lastDrawAt = EditorApplication.timeSinceStartup;

            Gizmos.DrawIcon(volume.transform.position, _probeVolumeIconPath, true);

            var probeRefVolume = ProbeReferenceVolume.instance;
            var sceneToBakingSetMap = ProbeVolumeBakingSet.SceneToBakingSet.Instance;
            var allVolumes = ProbeVolume.instances;

            if (!probeRefVolume.isInitialized || allVolumes.Count == 0)
                return;

            // Only the first PV of the available ones will draw gizmos.
            if (allVolumes[0] != volume)
                return;

            var debugDisplay = probeRefVolume.probeVolumeDebug;

            float minBrickSize = probeRefVolume.MinBrickSize();
            var cellSizeInMeters = probeRefVolume.MaxBrickSize();
            var probeOffset = probeRefVolume.ProbeOffset() + ProbeVolumeDebug.currentOffset;
            if (debugDisplay.realtimeSubdivision)
            {
                var bakingSet = ProbeVolumeBakingSet.GetBakingSetForScene(volume.gameObject.scene);
                if (bakingSet == null)
                    return;

                // Overwrite settings with data from profile
                minBrickSize = ProbeVolumeBakingSet.GetMinBrickSize(bakingSet.minDistanceBetweenProbes);
                cellSizeInMeters = ProbeVolumeBakingSet.GetCellSizeInBricks(bakingSet.simplificationLevels) * minBrickSize;
                probeOffset = bakingSet.probeOffset;
            }

            if (debugDisplay.drawBricks)
            {
                var subDivColors = probeRefVolume.subdivisionDebugColors;

                if (_brickMeshGizmo == null)
                    _brickMeshGizmo = new MeshGizmo((int)(Mathf.Pow(3, ProbeBrickIndex.kMaxSubdivisionLevels) * MeshGizmo.vertexCountPerCube));
                _brickMeshGizmo.Clear();

                if (debugDisplay.realtimeSubdivision)
                {
                    // realtime subdiv cells are already culled
                    foreach (var kp in probeRefVolume.realtimeSubdivisionInfo)
                    {
                        var cellVolume = kp.Key;

                        foreach (var brick in kp.Value)
                            DrawAndAddBrick(_brickMeshGizmo, brick, minBrickSize, probeOffset, subDivColors);
                    }
                }
                else
                {
                    var cullCtx = new ProbeVolume.CellCullingContext
                    {
                        ActiveCamera = null,
                        FrustumPlanes = stackalloc Plane[6]
                    };
                    ProbeVolume.PrepareCellCulling(ref cullCtx);

                    foreach (var cell in probeRefVolume.cells.Values)
                    {
                        if (!cell.loaded)
                            continue;

                        if (volume.ShouldCullCell(cullCtx, sceneToBakingSetMap, probeRefVolume, cell.desc.position))
                            continue;

                        if (cell.data.bricks == null)
                            continue;

                        foreach (var brick in cell.data.bricks)
                            DrawAndAddBrick(_brickMeshGizmo, brick, minBrickSize, probeOffset, subDivColors);
                    }
                }

                _brickMeshGizmo.RenderWireframe(Matrix4x4.identity, gizmoName: "Brick Gizmo Rendering");
            }

            if (debugDisplay.drawCells)
            {
                Color s_LoadedColor = new Color(0, 1, 0.5f, 0.2f);
                Color s_UnloadedColor = new Color(1, 0.0f, 0.0f, 0.2f);
                Color s_StreamingColor = new Color(0.0f, 0.0f, 1.0f, 0.2f);
                Color s_LowScoreColor = new Color(0, 0, 0, 0.2f);
                Color s_HighScoreColor = new Color(1, 1, 0, 0.2f);

                var oldGizmoMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.identity;

                if (_cellMeshGizmo == null)
                    _cellMeshGizmo = new MeshGizmo();
                _cellMeshGizmo.Clear();

                float minStreamingScore = probeRefVolume.minStreamingScore;
                float streamingScoreRange = probeRefVolume.maxStreamingScore - probeRefVolume.minStreamingScore;

                if (debugDisplay.realtimeSubdivision)
                {
                    foreach (var kp in probeRefVolume.realtimeSubdivisionInfo)
                    {
                        DrawAndAddCell(_cellMeshGizmo, kp.Key.center, s_LoadedColor, cellSizeInMeters);
                    }
                }
                else
                {
                    var cullCtx = new ProbeVolume.CellCullingContext
                    {
                        ActiveCamera = null,
                        FrustumPlanes = stackalloc Plane[6]
                    };
                    ProbeVolume.PrepareCellCulling(ref cullCtx);

                    foreach (var cell in probeRefVolume.cells.Values)
                    {
                        if (volume.ShouldCullCell(cullCtx, sceneToBakingSetMap, probeRefVolume, cell.desc.position))
                            continue;

                        Color color;
                        if (debugDisplay.displayCellStreamingScore)
                        {
                            float lerpFactor = (cell.streamingInfo.streamingScore - minStreamingScore) / streamingScoreRange;
                            color = Color.Lerp(s_HighScoreColor, s_LowScoreColor, lerpFactor);
                        }
                        else
                        {
                            if (cell.streamingInfo.IsStreaming())
                                color = s_StreamingColor;
                            else
                                color = cell.loaded ? s_LoadedColor : s_UnloadedColor;
                        }

                        var positionF = new Vector4(cell.desc.position.x, cell.desc.position.y, cell.desc.position.z, 0.0f);
                        var center = (Vector4)probeOffset + positionF * cellSizeInMeters + cellSizeInMeters * 0.5f * Vector4.one;
                        DrawAndAddCell(_cellMeshGizmo, center, color, cellSizeInMeters);
                    }
                }

                _cellMeshGizmo.RenderWireframe(Gizmos.matrix, gizmoName: "Brick Gizmo Rendering");
                Gizmos.matrix = oldGizmoMatrix;
            }
        }

        static void DrawAndAddCell(MeshGizmo meshGizmo, Vector4 center, Color color, float cellSizeInMeters)
        {
            Gizmos.color = color;
            Gizmos.DrawCube(center, Vector3.one * cellSizeInMeters);
            var wireColor = color;
            wireColor.a = 1.0f;
            meshGizmo.AddWireCube(center, Vector3.one * cellSizeInMeters, wireColor);
        }

        static void DrawAndAddBrick(MeshGizmo meshGizmo, ProbeBrickIndex.Brick brick, float minBrickSize, Vector3 probeOffset, Color[] subDivColors)
        {
            if (brick.subdivisionLevel < 0)
                return;

            float brickSize = minBrickSize * ProbeReferenceVolume.CellSize(brick.subdivisionLevel);
            Vector3 scaledSize = new Vector3(brickSize, brickSize, brickSize);
            Vector3 scaledPos = probeOffset + new Vector3(brick.position.x * minBrickSize, brick.position.y * minBrickSize, brick.position.z * minBrickSize) + scaledSize / 2;
            meshGizmo.AddWireCube(scaledPos, scaledSize, subDivColors[brick.subdivisionLevel]);
        }
    }
}
