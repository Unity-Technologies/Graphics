using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine.Rendering.UnifiedRayTracing;

namespace UnityEngine.Rendering
{
    partial class ProbeGIBaking
    {
        static int k_MaxProbeCountPerBatch = 65535;

        static RayTracingContext m_RayTracingContext;
        static IRayTracingAccelStruct m_RayTracingAccelerationStructure;
        static IRayTracingShader m_RayTracingShader;

        static int _Probes, _Offsets;

        static void BuildAccelerationStructure(GIContributors? contribs, int mask)
        {
            var contributors = contribs.HasValue ? contribs.Value : GIContributors.Find(GIContributors.ContributorFilter.All);

            foreach (var renderer in contributors.renderers)
            {
                int layerMask = 1 << renderer.component.gameObject.layer;
                if ((layerMask & mask) == 0)
                    continue;

                var mesh = renderer.component.GetComponent<MeshFilter>().sharedMesh;
                if (mesh == null)
                    continue;

                int subMeshCount = mesh.subMeshCount;
                for (int i = 0; i < subMeshCount; ++i)
                {
                    m_RayTracingAccelerationStructure.AddInstance(new MeshInstanceDesc(mesh, i)
                    {
                        localToWorldMatrix = renderer.component.transform.localToWorldMatrix,
                        enableTriangleCulling = false
                    });
                }
            }

            foreach (var terrain in contributors.terrains)
            {
                int layerMask = 1 << terrain.component.gameObject.layer;
                if ((layerMask & mask) == 0)
                    continue;

                m_RayTracingAccelerationStructure.AddTerrain(new TerrainDesc(terrain.component)
                {
                    localToWorldMatrix = terrain.component.transform.localToWorldMatrix,
                    enableTriangleCulling = false
                });
            }
        }

        static void CreateRayTracingResources()
        {
            if (m_RayTracingContext != null)
                return;

            var backend = RayTracingContext.IsBackendSupported(RayTracingBackend.Hardware) ? RayTracingBackend.Compute : RayTracingBackend.Compute;

            var resources = ScriptableObject.CreateInstance<RayTracingResources>();
            ResourceReloader.ReloadAllNullIn(resources, "Packages/com.unity.rendering.light-transport");
            Func<string, Type, Object> fileLoader = (filename, type) => AssetDatabase.LoadAssetAtPath("Packages/com.unity.render-pipelines.core/" + filename, type);

            m_RayTracingContext = new RayTracingContext(backend, resources);
            m_RayTracingShader = m_RayTracingContext.CreateRayTracingShader("Editor/Lighting/ProbeVolume/VirtualOffset/TraceVirtualOffset", fileLoader);
            m_RayTracingAccelerationStructure = m_RayTracingContext.CreateAccelerationStructure(new AccelerationStructureOptions{buildFlags = BuildFlags.PreferFastBuild}); // Use PreferFastBuild to avoid bug triggered with big meshes (UUM-52552)

            _Probes = Shader.PropertyToID("_Probes");
            _Offsets = Shader.PropertyToID("_Offsets");
        }

        // Duplicated in HLSL
        struct ProbeData
        {
            public Vector3 position;
            public float originBias;
            public float tMax;
            public float geometryBias;
            public int probeIndex;
            internal float _;
        };

        static void ApplyVirtualOffsets(GIContributors? contributors, Vector3[] positions, out Vector3[] offsets)
        {
            var voSettings = m_BakingSet.settings.virtualOffsetSettings;
            if (!voSettings.useVirtualOffset)
            {
                offsets = null;
                return;
            }

            var cellToVolumes = GetTouchupsPerCell(out bool hasAppliers);
            offsets = new Vector3[positions.Length];

            var scaleForSearchDist = voSettings.searchMultiplier;
            if (scaleForSearchDist == 0.0f)
            {
                if (hasAppliers)
                    DoApplyVirtualOffsetsFromAdjustmentVolumes(positions, offsets, cellToVolumes);
                return;
            }

            var computeBufferTarget = GraphicsBuffer.Target.CopyDestination | GraphicsBuffer.Target.CopySource
                | GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.Raw;

            // Allocate shared data
            CreateRayTracingResources();
            BuildAccelerationStructure(contributors, voSettings.collisionMask);

            var cmd = new CommandBuffer();
            var probeData = new ProbeData[k_MaxProbeCountPerBatch];
            var batchResult = new Vector3[k_MaxProbeCountPerBatch];
            using var probeBuffer = new GraphicsBuffer(computeBufferTarget, k_MaxProbeCountPerBatch, Marshal.SizeOf<ProbeData>());
            using var offsetBuffer = new GraphicsBuffer(computeBufferTarget, k_MaxProbeCountPerBatch, Marshal.SizeOf<Vector3>());
            using var scratchBuffer = RayTracingHelper.CreateScratchBufferForBuildAndDispatch(m_RayTracingAccelerationStructure, m_RayTracingShader,
                (uint)k_MaxProbeCountPerBatch, 1, 1);

            // Setup RT structure
            m_RayTracingAccelerationStructure.Build(cmd, scratchBuffer);
            m_RayTracingShader.SetAccelerationStructure(cmd, "_AccelStruct", m_RayTracingAccelerationStructure);
            m_RayTracingShader.SetBufferParam(cmd, _Probes, probeBuffer);
            m_RayTracingShader.SetBufferParam(cmd, _Offsets, offsetBuffer);

            // Run virtual offset in batches
            int batchPosIdx = 0;
            float cellSize = m_ProfileInfo.cellSizeInMeters;
            while (batchPosIdx < positions.Length)
            {
                // Prepare batch
                int probeCountInBatch = 0;
                var batchPosStart = batchPosIdx;
                do
                {
                    float rayOriginBias = voSettings.rayOriginBias;
                    float geometryBias = voSettings.outOfGeoOffset;

                    int subdivLevel = m_BakingBatch.GetSubdivLevelAt(positions[batchPosIdx]);
                    var brickSize = ProbeReferenceVolume.CellSize(subdivLevel);
                    var searchDistance = (brickSize * m_ProfileInfo.minBrickSize) / ProbeBrickPool.kBrickCellCount;
                    var distanceSearch = scaleForSearchDist * searchDistance;

                    int cellIndex = PosToIndex(Vector3Int.FloorToInt(positions[batchPosIdx] / cellSize));
                    if (cellToVolumes.TryGetValue(cellIndex, out var volumes))
                    {
                        bool adjusted = false;
                        foreach (var (touchup, obb, center, offset) in volumes.appliers)
                        {
                            if (touchup.ContainsPoint(obb, center, positions[batchPosIdx]))
                            {
                                positions[batchPosIdx] += offset;
                                offsets[batchPosIdx] = offset;
                                adjusted = true;
                                break;
                            }
                        }

                        if (adjusted)
                            continue;

                        foreach (var (touchup, obb, center) in volumes.overriders)
                        {
                            if (touchup.ContainsPoint(obb, center, positions[batchPosIdx]))
                            {
                                rayOriginBias = touchup.rayOriginBias;
                                geometryBias = touchup.geometryBias;
                                break;
                            }
                        }
                    }

                    probeData[probeCountInBatch++] = new ProbeData
                    {
                        position = positions[batchPosIdx],
                        originBias = rayOriginBias,
                        tMax = distanceSearch,
                        geometryBias = geometryBias,
                        probeIndex = batchPosIdx,
                    };
                }
                while (++batchPosIdx < positions.Length && probeCountInBatch < k_MaxProbeCountPerBatch);

                // Execute job
                cmd.SetBufferData(probeBuffer, probeData);
                m_RayTracingShader.Dispatch(cmd, scratchBuffer, (uint)probeCountInBatch, 1, 1);

                Graphics.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                offsetBuffer.GetData(batchResult);
                for (int i = 0; i < probeCountInBatch; i++)
                {
                    positions[probeData[i].probeIndex] += batchResult[i];
                    offsets[probeData[i].probeIndex] = batchResult[i];
                }
            }

            m_RayTracingAccelerationStructure.ClearInstances();
            cmd.Dispose();

            // If bake is triggered from the lighting pannel, we don't Dispose buffers now
            if (ProbeVolumeLightingTab.instance == null)
                Dispose();
        }

        static internal void Dispose()
        {
            m_RayTracingAccelerationStructure?.Dispose();
            m_RayTracingAccelerationStructure = null;
            m_RayTracingContext?.Dispose();
            m_RayTracingContext = null;
        }

        static internal void RecomputeVOForDebugOnly()
        {
            var prv = ProbeReferenceVolume.instance;
            if (prv.perSceneDataList.Count == 0)
                return;

            var contributors = GIContributors.Find(GIContributors.ContributorFilter.All);
            SetBakingContext(prv.perSceneDataList);

            if (!m_BakingSet.HasBeenBaked())
                return;

            globalBounds = prv.globalBounds;
            CellCountInDirections(out minCellPosition, out maxCellPosition, prv.MaxBrickSize());
            cellCount = maxCellPosition + Vector3Int.one - minCellPosition;

            m_BakingBatch = new BakingBatch(128, cellCount);
            m_ProfileInfo = new ProbeVolumeProfileInfo();
            ModifyProfileFromLoadedData(m_BakingSet);

            List <Vector3> positionList = new();
            Dictionary<int, int> positionToIndex = new();
            foreach (var cell in ProbeReferenceVolume.instance.cells.Values)
            {
                var bakingCell = ConvertCellToBakingCell(cell.desc, cell.data);

                int numProbes = bakingCell.probePositions.Length;
                int uniqueIndex = positionToIndex.Count;
                var indices = new int[numProbes];

                // DeduplicateProbePositions
                for (int i = 0; i < numProbes; i++)
                {
                    var pos = bakingCell.probePositions[i];
                    int brickSubdiv = bakingCell.bricks[i / 64].subdivisionLevel;
                    int probeHash = m_BakingBatch.GetProbePositionHash(pos);

                    if (positionToIndex.TryGetValue(probeHash, out var index))
                    {
                        indices[i] = index;
                        int oldBrickLevel = m_BakingBatch.uniqueBrickSubdiv[probeHash];
                        if (brickSubdiv < oldBrickLevel)
                            m_BakingBatch.uniqueBrickSubdiv[probeHash] = brickSubdiv;
                    }
                    else
                    {
                        positionToIndex[probeHash] = uniqueIndex;
                        indices[i] = uniqueIndex;
                        m_BakingBatch.uniqueBrickSubdiv[probeHash] = brickSubdiv;
                        positionList.Add(pos);
                        uniqueIndex++;
                    }
                }

                bakingCell.probeIndices = indices;
                m_BakingBatch.cells.Add(bakingCell);

                // We need to force rebuild debug stuff.
                cell.debugProbes = null;
            }

            ApplyVirtualOffsets(contributors, positionList.ToArray(), out m_BakingBatch.virtualOffsets);

            foreach (var cell in m_BakingBatch.cells)
            {
                int numProbes = cell.probePositions.Length;
                for (int i = 0; i < numProbes; ++i)
                {
                    int j = cell.probeIndices[i];
                    cell.offsetVectors[i] = m_BakingBatch.virtualOffsets[j];
                }
            }

            // Unload it all as we are gonna load back with newly written cells.
            foreach (var sceneData in prv.perSceneDataList)
                prv.AddPendingSceneRemoval(sceneData.sceneGUID);

            // Make sure unloading happens.
            prv.PerformPendingOperations();

            // Write back the assets.
            WriteBakingCells(m_BakingBatch.cells.ToArray());

            foreach (var data in prv.perSceneDataList)
                data.ResolveCellData();

            // We can now finally reload.
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            m_BakingBatch = null;

            foreach (var sceneData in prv.perSceneDataList)
            {
                prv.AddPendingSceneLoading(sceneData.sceneGUID);
            }

            prv.PerformPendingOperations();
        }
    }

    partial class ProbeGIBaking
    {
        struct TouchupsPerCell
        {
            public List<(ProbeTouchupVolume touchup, ProbeReferenceVolume.Volume obb, Vector3 center, Vector3 offset)> appliers;
            public List<(ProbeTouchupVolume touchup, ProbeReferenceVolume.Volume obb, Vector3 center)> overriders;
        }

        static Dictionary<int, TouchupsPerCell> GetTouchupsPerCell(out bool hasAppliers)
        {
            float cellSize = m_ProfileInfo.cellSizeInMeters;
            hasAppliers = false;

            Dictionary<int, TouchupsPerCell> cellToVolumes = new();
            foreach (var touchup in Object.FindObjectsByType<ProbeTouchupVolume>(FindObjectsSortMode.InstanceID))
            {
                if (!touchup.isActiveAndEnabled || (touchup.mode != ProbeTouchupVolume.Mode.ApplyVirtualOffset && touchup.mode != ProbeTouchupVolume.Mode.OverrideVirtualOffsetSettings))
                    continue;

                hasAppliers |= touchup.mode == ProbeTouchupVolume.Mode.ApplyVirtualOffset;
                touchup.GetOBBandAABB(out var obb, out var aabb);

                Vector3Int min = Vector3Int.FloorToInt(aabb.min / cellSize);
                Vector3Int max = Vector3Int.FloorToInt(aabb.max / cellSize);

                for (int x = min.x; x <= max.x; x++)
                {
                    for (int y = min.y; y <= max.y; y++)
                    {
                        for (int z = min.z; z <= max.z; z++)
                        {
                            var cell = PosToIndex(new Vector3Int(x, y, z));
                            if (!cellToVolumes.TryGetValue(cell, out var volumes))
                                cellToVolumes[cell] = volumes = new TouchupsPerCell() { appliers = new(), overriders = new() };

                            if (touchup.mode == ProbeTouchupVolume.Mode.ApplyVirtualOffset)
                                volumes.appliers.Add((touchup, obb, touchup.transform.position, touchup.GetVirtualOffset()));
                            else
                                volumes.overriders.Add((touchup, obb, touchup.transform.position));
                        }

                    }
                }
            }

            return cellToVolumes;
        }

        static Vector3[] DoApplyVirtualOffsetsFromAdjustmentVolumes(Vector3[] positions, Vector3[] offsets, Dictionary<int, TouchupsPerCell> cellToVolumes)
        {
            float cellSize = m_ProfileInfo.cellSizeInMeters;
            for (int i = 0; i < positions.Length; i++)
            {
                int cellIndex = PosToIndex(Vector3Int.FloorToInt(positions[i] / cellSize));
                if (cellToVolumes.TryGetValue(cellIndex, out var volumes))
                {
                    foreach (var (touchup, obb, center, offset) in volumes.appliers)
                    {
                        if (touchup.ContainsPoint(obb, center, positions[i]))
                        {
                            positions[i] += offset;
                            offsets[i] = offset;
                            break;
                        }
                    }
                }
            }
            return offsets;
        }
    }
}
