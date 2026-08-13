using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEditor;
using UnityEngine.Rendering.UnifiedRayTracing;

namespace UnityEngine.Rendering
{
    partial class AdaptiveProbeVolumes
    {
        /// <summary>
        /// Virtual offset baker. This API allows implementing custom virtual offset baking strategies. Virtual offsets are used to offset probe positions away from geometry to avoid light leaking.
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
            static readonly int k_MaxProbeCountPerBatch = 65535;

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

            int m_BatchPosIdx;
            NativeArray<Vector3> m_Positions;
            NativeArray<Vector3> m_Results;
            Dictionary<int, TouchupsPerCell> m_CellToVolumes;
            ProbeData[] m_ProbeData;
            Vector3[] m_BatchResult;

            float m_ScaleForSearchDist;
            float m_RayOriginBias;
            float m_GeometryBias;
            float m_ValidityThreshold;

            // Output buffer
            public override NativeArray<Vector3> offsets => m_Results;

            AccelStructAdapter m_AccelerationStructure;
            GraphicsBuffer m_ProbeBuffer;
            GraphicsBuffer m_OffsetBuffer;
            GraphicsBuffer m_ScratchBuffer;
            bool m_HasTerrains;

            public override ulong currentStep => (ulong)m_BatchPosIdx;
            public override ulong stepCount => m_BatchResult == null ? 0 : (ulong)m_Positions.Length;

            public override void Initialize(ProbeVolumeBakingSet bakingSet, NativeArray<Vector3> probePositions)
            {
                var voSettings = bakingSet.settings.virtualOffsetSettings;
                if (!voSettings.useVirtualOffset)
                    return;

                m_BatchPosIdx = 0;
                m_ScaleForSearchDist = voSettings.searchMultiplier;
                m_RayOriginBias = voSettings.rayOriginBias;
                m_GeometryBias = voSettings.outOfGeoOffset;
                m_ValidityThreshold = voSettings.validityThreshold;

                m_Results = new NativeArray<Vector3>(probePositions.Length, Allocator.Persistent);
                m_CellToVolumes = GetTouchupsPerCell(out bool hasAppliers);

                if (m_ScaleForSearchDist == 0.0f)
                {
                    if (hasAppliers)
                        DoApplyVirtualOffsetsFromAdjustmentVolumes(probePositions, m_Results, m_CellToVolumes);
                    return;
                }

                m_Positions = probePositions;
                m_ProbeData = new ProbeData[k_MaxProbeCountPerBatch];
                m_BatchResult = new Vector3[k_MaxProbeCountPerBatch];

                var computeBufferTarget = GraphicsBuffer.Target.CopyDestination | GraphicsBuffer.Target.CopySource
                    | GraphicsBuffer.Target.Structured;

                // Create acceletation structure
                m_AccelerationStructure = BuildAccelerationStructure(voSettings.collisionMask, out m_HasTerrains);
                var virtualOffsetShader = s_TracingContext.shaderVO;

                m_ProbeBuffer = new GraphicsBuffer(computeBufferTarget, k_MaxProbeCountPerBatch, Marshal.SizeOf<ProbeData>());
                m_OffsetBuffer = new GraphicsBuffer(computeBufferTarget, k_MaxProbeCountPerBatch, Marshal.SizeOf<Vector3>());
                m_ScratchBuffer = RayTracingHelper.CreateScratchBufferForBuildAndDispatch(m_AccelerationStructure.GetAccelerationStructure(), virtualOffsetShader,
                    (uint)k_MaxProbeCountPerBatch, 1, 1);

                var cmd = new CommandBuffer();
                m_AccelerationStructure.Build(cmd, ref m_ScratchBuffer);
                Graphics.ExecuteCommandBuffer(cmd);
                cmd.Dispose();
            }

            static AccelStructAdapter BuildAccelerationStructure(int mask, out bool hasTerrains)
            {
                var accelStruct = s_TracingContext.CreateAccelerationStructure();
                var contributors = s_BakingBatch.contributors;

                foreach (var renderer in contributors.renderers)
                {
                    int layerMask = 1 << renderer.component.gameObject.layer;
                    if ((layerMask & mask) == 0)
                        continue;

                    if (!s_TracingContext.TryGetMeshForAccelerationStructure(renderer.component, out var mesh))
                        continue;

                    if (renderer.component is SkinnedMeshRenderer)
                        continue;

                    int subMeshCount = mesh.subMeshCount;
                    var maskAndMatDummy = new uint[subMeshCount];
                    System.Array.Fill(maskAndMatDummy, 0xFFFFFFFF);
                    Span<bool> perSubMeshOpaqueness = stackalloc bool[subMeshCount];
                    perSubMeshOpaqueness.Fill(true);

                    accelStruct.AddInstance(EntityId.ToULong(renderer.component.GetEntityId()), renderer.component, maskAndMatDummy, maskAndMatDummy, perSubMeshOpaqueness, 1);
                }

                hasTerrains = false;
#if ENABLE_TERRAIN_MODULE
                foreach (var terrain in contributors.terrains)
                {
                    int layerMask = 1 << terrain.component.gameObject.layer;
                    if ((layerMask & mask) == 0)
                        continue;

                    hasTerrains = true;

                    ExtractTerrainData(terrain.component, out var heightData, out var heightmapResolution,
                        out var heightmapScale, out var holeData, out var holeResolution);

                    accelStruct.AddTerrainInstance(
                        EntityId.ToULong(terrain.component.GetEntityId()),
                        heightData,
                        heightmapResolution,
                        heightmapScale,
                        holeData,
                        holeResolution,
                        terrain.component.transform.localToWorldMatrix,
                        0xFFFFFFFF,  // materialID
                        1); // renderingLayerMask
                }
#endif

                return accelStruct;
            }

            public override bool Step()
            {
                if (currentStep >= stepCount)
                    return true;

                float minBrickSize = s_ProfileInfo.minBrickSize;

                // Prepare batch
                int probeCountInBatch = 0;
                do
                {
                    int subdivLevel = s_BakingBatch.GetSubdivLevelAt(m_Positions[m_BatchPosIdx]);
                    var brickSize = ProbeReferenceVolume.CellSize(subdivLevel);
                    var searchDistance = (brickSize * minBrickSize) / ProbeBrickPool.k_BrickCellCount;
                    var distanceSearch = m_ScaleForSearchDist * searchDistance;

                    int cellIndex = PosToIndex(s_ProfileInfo.PositionToCell(m_Positions[m_BatchPosIdx]));
                    if (m_CellToVolumes.TryGetValue(cellIndex, out var volumes))
                    {
                        bool adjusted = false;
                        foreach (var (touchup, obb, center, offset) in volumes.appliers)
                        {
                            if (touchup.ContainsPoint(obb, center, m_Positions[m_BatchPosIdx]))
                            {
                                m_Results[m_BatchPosIdx] = offset;
                                adjusted = true;
                                break;
                            }
                        }

                        if (adjusted)
                            continue;

                        foreach (var (touchup, obb, center) in volumes.overriders)
                        {
                            if (touchup.ContainsPoint(obb, center, m_Positions[m_BatchPosIdx]))
                            {
                                m_RayOriginBias = touchup.rayOriginBias;
                                m_GeometryBias = touchup.geometryBias;
                                m_ValidityThreshold = 1.0f - touchup.virtualOffsetThreshold;
                                break;
                            }
                        }
                    }

                    m_ProbeData[probeCountInBatch++] = new ProbeData
                    {
                        position = m_Positions[m_BatchPosIdx],
                        originBias = m_RayOriginBias,
                        tMax = distanceSearch,
                        geometryBias = m_GeometryBias,
                        validityThreshold = m_ValidityThreshold,
                        probeIndex = m_BatchPosIdx,
                    };
                }
                while (++m_BatchPosIdx < m_Positions.Length && probeCountInBatch < k_MaxProbeCountPerBatch);

                if (probeCountInBatch == 0)
                    return true;

                // Execute job
                var cmd = new CommandBuffer();
                var virtualOffsetShader = s_TracingContext.shaderVO;
                virtualOffsetShader.SetKeyword(cmd, virtualOffsetShader.CreateLocalKeyword("TERRAIN_RAY_MARCHING_ENABLED"), m_HasTerrains);

                m_AccelerationStructure.Bind(cmd, "_AccelStruct", virtualOffsetShader);
                if (m_HasTerrains)
                    m_AccelerationStructure.BindTerrainResources(cmd, virtualOffsetShader);
                virtualOffsetShader.SetBufferParam(cmd, _Probes, m_ProbeBuffer);
                virtualOffsetShader.SetBufferParam(cmd, _Offsets, m_OffsetBuffer);

                cmd.SetBufferData(m_ProbeBuffer, m_ProbeData);
                virtualOffsetShader.Dispatch(cmd, m_ScratchBuffer, (uint)probeCountInBatch, 1, 1);

                Graphics.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                m_OffsetBuffer.GetData(m_BatchResult);
                for (int i = 0; i < probeCountInBatch; i++)
                {
                    m_Results[m_ProbeData[i].probeIndex] = m_BatchResult[i];
                }

                cmd.Dispose();
                return true;
            }

            public override void Dispose()
            {
                if (m_Results.IsCreated)
                    m_Results.Dispose();

                if (m_BatchResult == null)
                    return;

                m_AccelerationStructure.Dispose();
                m_ProbeBuffer.Dispose();
                m_OffsetBuffer.Dispose();
                m_ScratchBuffer?.Dispose();
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

            s_GlobalBounds = prv.globalBounds;
            CellCountInDirections(out s_MinCellPosition, out s_MaxCellPosition, prv.MaxBrickSize(), prv.ProbeOffset());
            s_CellCount = s_MaxCellPosition + Vector3Int.one - s_MinCellPosition;

            s_BakingBatch = new BakingBatch(s_CellCount, ProbeReferenceVolume.instance);
            s_ProfileInfo = new ProbeVolumeProfileInfo();
            ModifyProfileFromLoadedData(m_BakingSet);

            var positionList = new NativeList<Vector3>(Allocator.Persistent);
            Dictionary<int, int> positionToIndex = new();
            foreach (var cell in ProbeReferenceVolume.instance.m_Cells.Values)
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
                    int probeHash = s_BakingBatch.GetProbePositionHash(pos);

                    if (positionToIndex.TryGetValue(probeHash, out var index))
                    {
                        indices[i] = index;
                        int oldBrickLevel = s_BakingBatch.uniqueBrickSubdiv[probeHash];
                        if (brickSubdiv < oldBrickLevel)
                            s_BakingBatch.uniqueBrickSubdiv[probeHash] = brickSubdiv;
                    }
                    else
                    {
                        positionToIndex[probeHash] = uniqueIndex;
                        indices[i] = uniqueIndex;
                        s_BakingBatch.uniqueBrickSubdiv[probeHash] = brickSubdiv;
                        positionList.Add(pos);
                        uniqueIndex++;
                    }
                }

                bakingCell.probeIndices = indices;
                s_BakingBatch.cells.Add(bakingCell);

                // We need to force rebuild debug stuff.
                cell.debugProbes = null;
            }

            VirtualOffsetBaker job = s_VirtualOffsetOverride ?? new DefaultVirtualOffset();
            job.Initialize(m_BakingSet, positionList.AsArray());

            while (job.currentStep < job.stepCount)
            {
                if (!job.Step())
                    break;
            }

            for (int c = 0; c < s_BakingBatch.cells.Count; ++c)
            {
                job.Step();
            }

            for (int c = 0; c < s_BakingBatch.cells.Count; ++c)
            {
                var cell = s_BakingBatch.cells[c];
                int numProbes = cell.probePositions.Length;

                // Might have no offset vectors, if the previous bake wasn't using virtual offsets.
                // If so, initialize them so the user can still preview what the offsets will do.
                if (cell.offsetVectors == null)
                {
                    cell.offsetVectors = new Vector3[numProbes];
                    s_BakingBatch.cells[c] = cell;
                }

                for (int i = 0; i < numProbes; ++i)
                {
                    int j = cell.probeIndices[i];
                    cell.offsetVectors[i] = job.offsets[j];
                }
            }

            job.Dispose();

            // Unload it all as we are gonna load back with newly written cells.
            foreach (var sceneData in prv.perSceneDataList)
            {
                prv.AddPendingSceneRemoval(sceneData.sceneGUID);
            }

            // Make sure unloading happens.
            prv.PerformPendingOperations();

            // Validate baking cells size before writing
            var bakingCellsArray = s_BakingBatch.cells.ToArray();
            var chunkSizeInProbes = ProbeBrickPool.GetChunkSizeInProbeCount();
            GetOptionalFeaturesFromCells(bakingCellsArray, out var hasVirtualOffsets, out var hasRenderingLayers);

            if (ValidateBakingCellsSize(bakingCellsArray, chunkSizeInProbes, hasVirtualOffsets, hasRenderingLayers))
            {
                // Write back the assets.
                WriteBakingCells(m_BakingSet, bakingCellsArray, m_BakingSet.bakedLayerMasks);
            }

            s_BakingBatch?.Dispose();
            s_BakingBatch = null;

            foreach (var data in prv.perSceneDataList)
            {
                data.ResolveCellData();
            }

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

                Vector3Int min = Vector3Int.Max(s_ProfileInfo.PositionToCell(adjustment.aabb.min), s_MinCellPosition);
                Vector3Int max = Vector3Int.Min(s_ProfileInfo.PositionToCell(adjustment.aabb.max), s_MaxCellPosition);

                for (int x = min.x; x <= max.x; x++)
                {
                    for (int y = min.y; y <= max.y; y++)
                    {
                        for (int z = min.z; z <= max.z; z++)
                        {
                            var cell = PosToIndex(new Vector3Int(x, y, z));
                            if (!cellToVolumes.TryGetValue(cell, out var volumes))
                                cellToVolumes[cell] = volumes = new TouchupsPerCell { appliers = new(), overriders = new() };

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
                var cellPos = s_ProfileInfo.PositionToCell(positions[i]);
                cellPos.Clamp(s_MinCellPosition, s_MaxCellPosition);
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
            DirectRayVisMask = 1 << 0,
            IndirectRayVisMask = 1 << 1,
            ShadowRayVisMask = 1 << 2,
        }

        static uint GetInstanceMask(ShadowCastingMode shadowMode)
        {
            uint instanceMask = 0u;

            if (shadowMode != ShadowCastingMode.Off)
                instanceMask |= (uint)InstanceFlags.ShadowRayVisMask;

            if (shadowMode != ShadowCastingMode.ShadowsOnly)
            {
                instanceMask |= (uint)InstanceFlags.DirectRayVisMask;
                instanceMask |= (uint)InstanceFlags.IndirectRayVisMask;
            }

            return instanceMask;
        }

        static void ExtractTerrainData(
            Terrain terrain,
            out short[] heightData,
            out int heightmapResolution,
            out Unity.Mathematics.float3 heightmapScale,
            out byte[] holeData,
            out int holeResolution)
        {
            var terrainData = terrain.terrainData;
            heightmapResolution = terrainData.heightmapResolution;
            heightmapScale = new Unity.Mathematics.float3(
                terrainData.heightmapScale.x,
                terrainData.heightmapScale.y,
                terrainData.heightmapScale.z);

            var heights = terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);
            heightData = new short[heightmapResolution * heightmapResolution];
            for (int y = 0; y < heightmapResolution; y++)
            {
                for (int x = 0; x < heightmapResolution; x++)
                {
                    int idx = y * heightmapResolution + x;
                    heightData[idx] = (short)(heights[y, x] * 32766.0f);
                }
            }

            // Extract holes if present
            holeData = null;
            holeResolution = 0;
            if (terrainData.holesResolution > 0)
            {
                holeResolution = terrainData.holesResolution;
                var holes = terrainData.GetHoles(0, 0, holeResolution, holeResolution);
                holeData = new byte[holeResolution * holeResolution];
                for (int y = 0; y < holeResolution; y++)
                {
                    for (int x = 0; x < holeResolution; x++)
                    {
                        holeData[y * holeResolution + x] = holes[y, x] ? (byte)1 : (byte)0;
                    }
                }
            }
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
                    matIndices[i] = (uint)EntityId.ToULong(renderer.sharedMaterials[i].GetEntityId());
                else
                    matIndices[i] = 0;
            }

            return matIndices;
        }
    }

    // This class is used to access the internal class UnityEditor.LightBaking.VirtualOffsets.
    class VirtualOffsets : IDisposable
    {
        readonly object m_VirtualOffsets;
        readonly Type m_VirtualOffsetsType;

        public VirtualOffsets(IntPtr ptr)
        {
            m_VirtualOffsetsType = Type.GetType("UnityEditor.LightBaking.VirtualOffsets, UnityEditor");
            bool newed = m_VirtualOffsetsType != null;
            Debug.Assert(newed, "Unexpected, could not find the type UnityEditor.LightBaking.VirtualOffsets");
            m_VirtualOffsets = newed ? Activator.CreateInstance(m_VirtualOffsetsType, ptr) : null;
            Debug.Assert(m_VirtualOffsets != null, "Unexpected, could not new up a VirtualOffsets");
        }
        internal void SetVirtualOffsets(Vector3[] offsets) =>
            InvokeMethod(new object[] { offsets }, out _);

        public void Dispose() =>
            InvokeMethod(new object[] { }, out _);

        void InvokeMethod(object[] parameters, out object result, [CallerMemberName] string methodName = "")
        {
            MethodInfo methodInfo = m_VirtualOffsetsType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            bool gotMethod = methodInfo != null;
            Debug.Assert(gotMethod, $"Unexpected, could not find {methodName} on VirtualOffsets");
            result = !gotMethod ? null : methodInfo.Invoke(m_VirtualOffsets, parameters);
        }
    }
}
