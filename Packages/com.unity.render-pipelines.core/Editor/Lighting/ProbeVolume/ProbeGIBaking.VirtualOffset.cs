using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEditor;
using UnityEngine.Rendering.UnifiedRayTracing;

namespace UnityEngine.Rendering
{
    partial class AdaptiveProbeVolumes
    {
        /// <summary>
        /// Virtual offset baker
        /// </summary>
        public abstract class VirtualOffsetBaker : IDisposable
        {
            /// <summary>The current baking step.</summary>
            public abstract ulong currentStep { get; }
            /// <summary>The total amount of step.</summary>
            public abstract ulong stepCount { get; }

            /// <summary>Array storing the resulting virtual offsets to be applied to probe positions.</summary>
            public abstract NativeArray<Vector3> offsets { get; }

            /// <summary>
            /// This is called before the start of baking to allow allocating necessary resources.
            /// </summary>
            /// <param name="bakingSet">The baking set that is currently baked.</param>
            /// <param name="probePositions">The probe positions.</param>
            public abstract void Initialize(ProbeVolumeBakingSet bakingSet, NativeArray<Vector3> probePositions);

            /// <summary>
            /// Run a step of virtual offset baking. Baking is considered done when currentStep property equals stepCount.
            /// </summary>
            /// <returns>Return false if bake failed and should be stopped.</returns>
            public abstract bool Step();

            /// <summary>
            /// Performs necessary tasks to free allocated resources.
            /// </summary>
            public abstract void Dispose();
        }

        class DefaultVirtualOffset : VirtualOffsetBaker
        {
            static int k_MaxProbeCountPerBatch = 65535;

            static readonly int _Probes = Shader.PropertyToID("_Probes");
            static readonly int _Offsets = Shader.PropertyToID("_Offsets");

            // Duplicated in HLSL
            struct ProbeData
            {
                public Vector3 position;
                public float originBias;
                public float tMax;
                public float geometryBias;
                public int probeIndex;
                public float validityThreshold;
            };

            int batchPosIdx;
            NativeArray<Vector3> positions;
            NativeArray<Vector3> results;
            Dictionary<int, TouchupsPerCell> cellToVolumes;
            ProbeData[] probeData;
            Vector3[] batchResult;

            float scaleForSearchDist;
            float rayOriginBias;
            float geometryBias;
            float validityThreshold;

            // Output buffer
            public override NativeArray<Vector3> offsets => results;

            private AccelStructAdapter m_AccelerationStructure;
            private GraphicsBuffer probeBuffer;
            private GraphicsBuffer offsetBuffer;
            private GraphicsBuffer scratchBuffer;

            public override ulong currentStep => (ulong)batchPosIdx;
            public override ulong stepCount => batchResult == null ? 0 : (ulong)positions.Length;

            public override void Initialize(ProbeVolumeBakingSet bakingSet, NativeArray<Vector3> probePositions)
            {
                var voSettings = bakingSet.settings.virtualOffsetSettings;
                if (!voSettings.useVirtualOffset)
                    return;

                batchPosIdx = 0;
                scaleForSearchDist = voSettings.searchMultiplier;
                rayOriginBias = voSettings.rayOriginBias;
                geometryBias = voSettings.outOfGeoOffset;
                validityThreshold = voSettings.validityThreshold;

                results = new NativeArray<Vector3>(probePositions.Length, Allocator.Persistent);
                cellToVolumes = GetTouchupsPerCell(out bool hasAppliers);

                if (scaleForSearchDist == 0.0f)
                {
                    if (hasAppliers)
                        DoApplyVirtualOffsetsFromAdjustmentVolumes(probePositions, results, cellToVolumes);
                    return;
                }

                positions = probePositions;
                probeData = new ProbeData[k_MaxProbeCountPerBatch];
                batchResult = new Vector3[k_MaxProbeCountPerBatch];

                var computeBufferTarget = GraphicsBuffer.Target.CopyDestination | GraphicsBuffer.Target.CopySource
                    | GraphicsBuffer.Target.Structured;

                // Create acceletation structure
                m_AccelerationStructure = BuildAccelerationStructure(voSettings.collisionMask);
                var virtualOffsetShader = s_TracingContext.shaderVO;

                probeBuffer = new GraphicsBuffer(computeBufferTarget, k_MaxProbeCountPerBatch, Marshal.SizeOf<ProbeData>());
                offsetBuffer = new GraphicsBuffer(computeBufferTarget, k_MaxProbeCountPerBatch, Marshal.SizeOf<Vector3>());
                scratchBuffer = RayTracingHelper.CreateScratchBufferForBuildAndDispatch(m_AccelerationStructure.GetAccelerationStructure(), virtualOffsetShader,
                    (uint)k_MaxProbeCountPerBatch, 1, 1);

                var cmd = new CommandBuffer();
                m_AccelerationStructure.Build(cmd, ref scratchBuffer);
                Graphics.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }

            static AccelStructAdapter BuildAccelerationStructure(int mask)
            {
                var accelStruct = s_TracingContext.CreateAccelerationStructure();
                var contributors = m_BakingBatch.contributors;

                foreach (var renderer in contributors.renderers)
                {
                    int layerMask = 1 << renderer.component.gameObject.layer;
                    if ((layerMask & mask) == 0)
                        continue;

                    if (!s_TracingContext.TryGetMeshForAccelerationStructure(renderer.component, out var mesh))
                        continue;

                    int subMeshCount = mesh.subMeshCount;
                    var maskAndMatDummy = new uint[subMeshCount];
                    System.Array.Fill(maskAndMatDummy, 0xFFFFFFFF);

                    accelStruct.AddInstance(renderer.component.GetInstanceID(), renderer.component, maskAndMatDummy, maskAndMatDummy, 1);
                }

                foreach (var terrain in contributors.terrains)
                {
                    int layerMask = 1 << terrain.component.gameObject.layer;
                    if ((layerMask & mask) == 0)
                        continue;

                    accelStruct.AddInstance(terrain.component.GetInstanceID(), terrain.component, new uint[1] { 0xFFFFFFFF }, new uint[1] { 0xFFFFFFFF }, 1);
                }

                return accelStruct;
            }

            public override bool Step()
            {
                if (currentStep >= stepCount)
                    return true;

                float minBrickSize = m_ProfileInfo.minBrickSize;

                // Prepare batch
                int probeCountInBatch = 0;
                do
                {
                    int subdivLevel = m_BakingBatch.GetSubdivLevelAt(positions[batchPosIdx]);
                    var brickSize = ProbeReferenceVolume.CellSize(subdivLevel);
                    var searchDistance = (brickSize * minBrickSize) / ProbeBrickPool.kBrickCellCount;
                    var distanceSearch = scaleForSearchDist * searchDistance;

                    int cellIndex = PosToIndex(m_ProfileInfo.PositionToCell(positions[batchPosIdx]));
                    if (cellToVolumes.TryGetValue(cellIndex, out var volumes))
                    {
                        bool adjusted = false;
                        foreach (var (touchup, obb, center, offset) in volumes.appliers)
                        {
                            if (touchup.ContainsPoint(obb, center, positions[batchPosIdx]))
                            {
                                results[batchPosIdx] = offset;
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
                                validityThreshold = 1.0f - touchup.virtualOffsetThreshold;
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
                        validityThreshold = validityThreshold,
                        probeIndex = batchPosIdx,
                    };
                }
                while (++batchPosIdx < positions.Length && probeCountInBatch < k_MaxProbeCountPerBatch);

                if (probeCountInBatch == 0)
                    return true;

                // Execute job
                var cmd = new CommandBuffer();
                var virtualOffsetShader = s_TracingContext.shaderVO;
                m_AccelerationStructure.Bind(cmd, "_AccelStruct", virtualOffsetShader);
                virtualOffsetShader.SetBufferParam(cmd, _Probes, probeBuffer);
                virtualOffsetShader.SetBufferParam(cmd, _Offsets, offsetBuffer);

                cmd.SetBufferData(probeBuffer, probeData);
                virtualOffsetShader.Dispatch(cmd, scratchBuffer, (uint)probeCountInBatch, 1, 1);

                Graphics.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                offsetBuffer.GetData(batchResult);
                for (int i = 0; i < probeCountInBatch; i++)
                    results[probeData[i].probeIndex] = batchResult[i];

                cmd.Dispose();
                return true;
            }

            public override void Dispose()
            {
                if (results.IsCreated)
                    results.Dispose();

                if (batchResult == null)
                    return;

                m_AccelerationStructure.Dispose();
                probeBuffer.Dispose();
                offsetBuffer.Dispose();
                scratchBuffer?.Dispose();
            }
        }

        static internal void RecomputeVOForDebugOnly()
        {
            var prv = ProbeReferenceVolume.instance;
            if (prv.perSceneDataList.Count == 0)
                return;

            SetBakingContext(prv.perSceneDataList);

            if (!m_BakingSet.HasBeenBaked())
                return;

            globalBounds = prv.globalBounds;
            CellCountInDirections(out minCellPosition, out maxCellPosition, prv.MaxBrickSize(), prv.ProbeOffset());
            cellCount = maxCellPosition + Vector3Int.one - minCellPosition;

            m_BakingBatch = new BakingBatch(cellCount);
            m_ProfileInfo = new ProbeVolumeProfileInfo();
            ModifyProfileFromLoadedData(m_BakingSet);

            var positionList = new NativeList<Vector3>(Allocator.Persistent);
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

            VirtualOffsetBaker job = virtualOffsetOverride ?? new DefaultVirtualOffset();
            job.Initialize(m_BakingSet, positionList.AsArray());

            while (job.currentStep < job.stepCount)
                job.Step();

            foreach (var cell in m_BakingBatch.cells)
            {
                int numProbes = cell.probePositions.Length;
                for (int i = 0; i < numProbes; ++i)
                {
                    int j = cell.probeIndices[i];
                    cell.offsetVectors[i] = job.offsets[j];
                }
            }

            job.Dispose();

            // Unload it all as we are gonna load back with newly written cells.
            foreach (var sceneData in prv.perSceneDataList)
                prv.AddPendingSceneRemoval(sceneData.sceneGUID);

            // Make sure unloading happens.
            prv.PerformPendingOperations();

            // Validate baking cells size before writing
            var bakingCellsArray = m_BakingBatch.cells.ToArray();
            var chunkSizeInProbes = ProbeBrickPool.GetChunkSizeInProbeCount();
            var hasVirtualOffsets = m_BakingSet.settings.virtualOffsetSettings.useVirtualOffset;
            var hasRenderingLayers = m_BakingSet.useRenderingLayers;
            
            if (ValidateBakingCellsSize(bakingCellsArray, chunkSizeInProbes, hasVirtualOffsets, hasRenderingLayers))
            {
                // Write back the assets.
                WriteBakingCells(bakingCellsArray);
            }

            m_BakingBatch?.Dispose();
            m_BakingBatch = null;

            foreach (var data in prv.perSceneDataList)
                data.ResolveCellData();

            // We can now finally reload.
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            foreach (var sceneData in prv.perSceneDataList)
            {
                prv.AddPendingSceneLoading(sceneData.sceneGUID, sceneData.serializedBakingSet);
            }

            prv.PerformPendingOperations();
        }
    }

    partial class AdaptiveProbeVolumes
    {
        struct TouchupsPerCell
        {
            public List<(ProbeAdjustmentVolume touchup, ProbeReferenceVolume.Volume obb, Vector3 center, Vector3 offset)> appliers;
            public List<(ProbeAdjustmentVolume touchup, ProbeReferenceVolume.Volume obb, Vector3 center)> overriders;
        }

        static Dictionary<int, TouchupsPerCell> GetTouchupsPerCell(out bool hasAppliers)
        {
            hasAppliers = false;
            var adjustmentVolumes = s_AdjustmentVolumes != null ? s_AdjustmentVolumes : GetAdjustementVolumes();

            Dictionary<int, TouchupsPerCell> cellToVolumes = new();
            foreach (var adjustment in adjustmentVolumes)
            {
                var volume = adjustment.volume;
                var mode = volume.mode;
                if (mode != ProbeAdjustmentVolume.Mode.ApplyVirtualOffset && mode != ProbeAdjustmentVolume.Mode.OverrideVirtualOffsetSettings)
                    continue;

                hasAppliers |= mode == ProbeAdjustmentVolume.Mode.ApplyVirtualOffset;

                Vector3Int min = Vector3Int.Max(m_ProfileInfo.PositionToCell(adjustment.aabb.min), minCellPosition);
                Vector3Int max = Vector3Int.Min(m_ProfileInfo.PositionToCell(adjustment.aabb.max), maxCellPosition);

                for (int x = min.x; x <= max.x; x++)
                {
                    for (int y = min.y; y <= max.y; y++)
                    {
                        for (int z = min.z; z <= max.z; z++)
                        {
                            var cell = PosToIndex(new Vector3Int(x, y, z));
                            if (!cellToVolumes.TryGetValue(cell, out var volumes))
                                cellToVolumes[cell] = volumes = new TouchupsPerCell() { appliers = new(), overriders = new() };

                            if (mode == ProbeAdjustmentVolume.Mode.ApplyVirtualOffset)
                                volumes.appliers.Add((volume, adjustment.obb, volume.transform.position, volume.GetVirtualOffset()));
                            else
                                volumes.overriders.Add((volume, adjustment.obb, volume.transform.position));
                        }

                    }
                }
            }

            return cellToVolumes;
        }

        static void DoApplyVirtualOffsetsFromAdjustmentVolumes(NativeArray<Vector3> positions, NativeArray<Vector3> offsets, Dictionary<int, TouchupsPerCell> cellToVolumes)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                var cellPos = m_ProfileInfo.PositionToCell(positions[i]);
                cellPos.Clamp(minCellPosition, maxCellPosition);
                int cellIndex = PosToIndex(cellPos);
                if (cellToVolumes.TryGetValue(cellIndex, out var volumes))
                {
                    foreach (var (touchup, obb, center, offset) in volumes.appliers)
                    {
                        if (touchup.ContainsPoint(obb, center, positions[i]))
                        {
                            offsets[i] = offset;
                            break;
                        }
                    }
                }
            }
        }

        enum InstanceFlags
        {
            DIRECT_RAY_VIS_MASK = 1,
            INDIRECT_RAY_VIS_MASK = 2,
            SHADOW_RAY_VIS_MASK = 4,
        }

        private static uint GetInstanceMask(ShadowCastingMode shadowMode)
        {
            uint instanceMask = 0u;

            if (shadowMode != ShadowCastingMode.Off)
                instanceMask |= (uint)InstanceFlags.SHADOW_RAY_VIS_MASK;

            if (shadowMode != ShadowCastingMode.ShadowsOnly)
            {
                instanceMask |= (uint)InstanceFlags.DIRECT_RAY_VIS_MASK;
                instanceMask |= (uint)InstanceFlags.INDIRECT_RAY_VIS_MASK;
            }

            return instanceMask;
        }

        static uint[] GetMaterialIndices(Renderer renderer)
        {
            int submeshCount = 1;
            var meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter)
                submeshCount = renderer.GetComponent<MeshFilter>().sharedMesh.subMeshCount;

            uint[] matIndices = new uint[submeshCount];
            for (int i = 0; i < matIndices.Length; ++i)
            {
                if (i < renderer.sharedMaterials.Length && renderer.sharedMaterials[i] != null)
                    matIndices[i] = (uint)renderer.sharedMaterials[i].GetInstanceID();
                else
                    matIndices[i] = 0;
            }

            return matIndices;
        }
    }
}
