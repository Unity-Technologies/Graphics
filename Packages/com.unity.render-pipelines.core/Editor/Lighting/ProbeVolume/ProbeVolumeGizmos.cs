using UnityEditor;

namespace UnityEngine.Rendering
{
    internal static class ProbeVolumeGizmos
    {
        static MeshGizmo s_BrickMeshGizmo;
        static MeshGizmo s_CellMeshGizmo;
        static double s_LastDrawAt;

        static readonly string k_GizmoPath = "Packages/com.unity.render-pipelines.core/Editor/Resources/Gizmos";
        static readonly string k_ProbeAdjustmentVolumeIconPath = k_GizmoPath + "/ProbeTouchupVolume.png";
        static readonly string k_ProbeVolumeIconPath = k_GizmoPath + "/ProbeVolume.png";

        static ProbeVolumeGizmos()
        {
            EditorApplication.update += Update;
        }

        static void Update()
        {
            bool resourcesAllocated = s_BrickMeshGizmo != null || s_CellMeshGizmo != null;

            if (resourcesAllocated)
            {
                bool shouldCleanUp = EditorApplication.timeSinceStartup - s_LastDrawAt > 1.0;
                if (shouldCleanUp)
                {
                    s_BrickMeshGizmo?.Dispose();
                    s_BrickMeshGizmo = null;
                    s_CellMeshGizmo?.Dispose();
                    s_CellMeshGizmo = null;
                }
            }
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected | GizmoType.NonSelected)]
        static void DrawProbeAdjustmentVolumes(ProbeAdjustmentVolume volume, GizmoType gizmoType)
        {
            Gizmos.DrawIcon(volume.transform.position, k_ProbeAdjustmentVolumeIconPath, true);
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected | GizmoType.NonSelected)]
        static void DrawProbeVolumeGizmos(ProbeVolume volume, GizmoType gizmoType)
        {
            s_LastDrawAt = EditorApplication.timeSinceStartup;

            Gizmos.DrawIcon(volume.transform.position, k_ProbeVolumeIconPath, true);

            var probeRefVolume = ProbeReferenceVolume.instance;
            var sceneToBakingSetMap = ProbeVolumeBakingSet.SceneToBakingSet.Instance;
            var allVolumes = ProbeVolume.s_Instances;

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

                if (s_BrickMeshGizmo == null)
                    s_BrickMeshGizmo = new MeshGizmo((int)(Mathf.Pow(3, ProbeBrickIndex.k_MaxSubdivisionLevels) * MeshGizmo.vertexCountPerCube));
                s_BrickMeshGizmo.Clear();

                if (debugDisplay.realtimeSubdivision)
                {
                    // realtime subdiv cells are already culled
                    foreach (var kp in probeRefVolume.m_RealtimeSubdivisionInfo)
                    {
                        var cellVolume = kp.Key;

                        foreach (var brick in kp.Value)
                        {
                            DrawAndAddBrick(s_BrickMeshGizmo, brick, minBrickSize, probeOffset, subDivColors);
                        }
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

                    foreach (var cell in probeRefVolume.m_Cells.Values)
                    {
                        if (!cell.loaded)
                            continue;

                        if (volume.ShouldCullCell(cullCtx, sceneToBakingSetMap, probeRefVolume, cell.desc.position))
                            continue;

                        if (cell.data.bricks == null)
                            continue;

                        foreach (var brick in cell.data.bricks)
                        {
                            DrawAndAddBrick(s_BrickMeshGizmo, brick, minBrickSize, probeOffset, subDivColors);
                        }
                    }
                }

                s_BrickMeshGizmo.RenderWireframe(Matrix4x4.identity, gizmoName: "Brick Gizmo Rendering");
            }

            if (debugDisplay.drawCells)
            {
                var loadedColor = new Color(0, 1, 0.5f, 0.2f);
                var unloadedColor = new Color(1, 0.0f, 0.0f, 0.2f);
                var streamingColor = new Color(0.0f, 0.0f, 1.0f, 0.2f);
                var lowScoreColor = new Color(0, 0, 0, 0.2f);
                var highScoreColor = new Color(1, 1, 0, 0.2f);

                var oldGizmoMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.identity;

                if (s_CellMeshGizmo == null)
                    s_CellMeshGizmo = new MeshGizmo();
                s_CellMeshGizmo.Clear();

                float minStreamingScore = probeRefVolume.m_MinStreamingScore;
                float streamingScoreRange = probeRefVolume.m_MaxStreamingScore - probeRefVolume.m_MinStreamingScore;

                if (debugDisplay.realtimeSubdivision)
                {
                    foreach (var kp in probeRefVolume.m_RealtimeSubdivisionInfo)
                    {
                        DrawAndAddCell(s_CellMeshGizmo, kp.Key.center, loadedColor, cellSizeInMeters);
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

                    foreach (var cell in probeRefVolume.m_Cells.Values)
                    {
                        if (volume.ShouldCullCell(cullCtx, sceneToBakingSetMap, probeRefVolume, cell.desc.position))
                            continue;

                        Color color;
                        if (debugDisplay.displayCellStreamingScore)
                        {
                            float lerpFactor = (cell.streamingInfo.streamingScore - minStreamingScore) / streamingScoreRange;
                            color = Color.Lerp(highScoreColor, lowScoreColor, lerpFactor);
                        }
                        else
                        {
                            if (cell.streamingInfo.IsStreaming())
                                color = streamingColor;
                            else
                                color = cell.loaded ? loadedColor : unloadedColor;
                        }

                        var positionF = new Vector4(cell.desc.position.x, cell.desc.position.y, cell.desc.position.z, 0.0f);
                        var center = (Vector4)probeOffset + positionF * cellSizeInMeters + cellSizeInMeters * 0.5f * Vector4.one;
                        DrawAndAddCell(s_CellMeshGizmo, center, color, cellSizeInMeters);
                    }
                }

                s_CellMeshGizmo.RenderWireframe(Gizmos.matrix, gizmoName: "Brick Gizmo Rendering");
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
            var scaledSize = new Vector3(brickSize, brickSize, brickSize);
            Vector3 scaledPos = probeOffset + new Vector3(brick.position.x * minBrickSize, brick.position.y * minBrickSize, brick.position.z * minBrickSize) + scaledSize / 2;
            meshGizmo.AddWireCube(scaledPos, scaledSize, subDivColors[brick.subdivisionLevel]);
        }
    }
}
