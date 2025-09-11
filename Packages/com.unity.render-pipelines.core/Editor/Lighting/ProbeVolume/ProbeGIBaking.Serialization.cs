using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;

using Brick = UnityEngine.Rendering.ProbeBrickIndex.Brick;
using Cell = UnityEngine.Rendering.ProbeReferenceVolume.Cell;
using CellDesc = UnityEngine.Rendering.ProbeReferenceVolume.CellDesc;
using CellData = UnityEngine.Rendering.ProbeReferenceVolume.CellData;
using IndirectionEntryInfo = UnityEngine.Rendering.ProbeReferenceVolume.IndirectionEntryInfo;
using StreamableCellDesc = UnityEngine.Rendering.ProbeVolumeStreamableAsset.StreamableCellDesc;

namespace UnityEngine.Rendering
{
    public partial class AdaptiveProbeVolumes
    {
        struct CellCounts
        {
            public int bricksCount;
            public int chunksCount;

            public void Add(CellCounts o)
            {
                bricksCount += o.bricksCount;
                chunksCount += o.chunksCount;
            }
        }

        struct CellChunkData
        {
            public bool scenarioValid;

            public NativeArray<ushort> shL0L1RxData;
            public NativeArray<byte> shL1GL1RyData;
            public NativeArray<byte> shL1BL1RzData;

            // Optional L2 Data
            public NativeArray<byte> shL2Data_0;
            public NativeArray<byte> shL2Data_1;
            public NativeArray<byte> shL2Data_2;
            public NativeArray<byte> shL2Data_3;

            public NativeArray<byte> validityNeighMaskData;
            public NativeArray<ushort> skyOcclusionDataL0L1;
            public NativeArray<byte> skyShadingDirectionIndices;

            public NativeArray<byte> probeOcclusion;
        }

        internal const string kAPVStreamingAssetsPath = "APVStreamingAssets";

        static CellCounts m_TotalCellCounts;

        static CellChunkData GetCellChunkData(CellData cellData, int chunkIndex)
        {
            var result = new CellChunkData();

            int chunkSizeInProbes = ProbeBrickPool.GetChunkSizeInProbeCount();
            int chunkOffset = chunkSizeInProbes * chunkIndex;

            if (m_BakingSet != null)
            {
                result.scenarioValid = cellData.scenarios.TryGetValue(m_BakingSet.lightingScenario, out var scenarioData);

                if (result.scenarioValid)
                {
                    result.shL0L1RxData = scenarioData.shL0L1RxData.GetSubArray(chunkOffset * 4, chunkSizeInProbes * 4);
                    result.shL1GL1RyData = scenarioData.shL1GL1RyData.GetSubArray(chunkOffset * 4, chunkSizeInProbes * 4);
                    result.shL1BL1RzData = scenarioData.shL1BL1RzData.GetSubArray(chunkOffset * 4, chunkSizeInProbes * 4);

                    if (scenarioData.shL2Data_0.Length > 0) // we might have no L2 if we are not during baking but during touchup interaction
                    {
                        result.shL2Data_0 = scenarioData.shL2Data_0.GetSubArray(chunkOffset * 4, chunkSizeInProbes * 4);
                        result.shL2Data_1 = scenarioData.shL2Data_1.GetSubArray(chunkOffset * 4, chunkSizeInProbes * 4);
                        result.shL2Data_2 = scenarioData.shL2Data_2.GetSubArray(chunkOffset * 4, chunkSizeInProbes * 4);
                        result.shL2Data_3 = scenarioData.shL2Data_3.GetSubArray(chunkOffset * 4, chunkSizeInProbes * 4);
                    }

                    if (scenarioData.probeOcclusion.Length > 0)
                    {
                        result.probeOcclusion = scenarioData.probeOcclusion.GetSubArray(chunkOffset * 4, chunkSizeInProbes * 4);
                    }
                }
            }

            if (cellData.skyOcclusionDataL0L1.Length > 0)
            {
                result.skyOcclusionDataL0L1 = cellData.skyOcclusionDataL0L1.GetSubArray(chunkOffset * 4, chunkSizeInProbes * 4);
                if (cellData.skyShadingDirectionIndices.Length > 0)
                {
                    result.skyShadingDirectionIndices = cellData.skyShadingDirectionIndices.GetSubArray(chunkOffset, chunkSizeInProbes);
                }
            }

            result.validityNeighMaskData = cellData.validityNeighMaskData.GetSubArray(chunkOffset, chunkSizeInProbes);

            return result;
        }

        static Dictionary<int, int> RemapBakedCells(bool isBakingSubset)
        {
            // When baking a baking set. It is possible that cells layout has changed (min and max position of cells in the set).
            // If this is the case then the cell index for a given position will change.
            // Because of this, when doing partial bakes, we need to generate a remapping table of the old cells to the new layout in order to be able to update existing data.
            Dictionary<int, int> oldToNewCellRemapping = new Dictionary<int, int>();

            if (isBakingSubset)
            {
                // Layout has changed but is still compatible. Remap all cells that are not part of the bake.
                if (minCellPosition != m_BakingSet.minCellPosition || maxCellPosition != m_BakingSet.maxCellPosition)
                {
                    var alreadyBakedCells = m_BakingSet.cellDescs;
                    var newCells = new SerializedDictionary<int, CellDesc>();

                    // Generate remapping for all cells baked the last time.
                    foreach (var cellKvP in alreadyBakedCells)
                    {
                        var cell = cellKvP.Value;
                        int oldIndex = cell.index;
                        int remappedIndex = PosToIndex(cell.position);
                        oldToNewCellRemapping.Add(oldIndex, remappedIndex);

                        cell.index = remappedIndex;
                        newCells.Add(oldIndex, cell);
                    }
                }
            }

            return oldToNewCellRemapping;
        }

        static void GenerateScenesCellLists(List<ProbeVolumePerSceneData> bakedSceneDataList, Dictionary<int, int> cellRemapTable)
        {
            bool needRemap = cellRemapTable.Count != 0;

            // Build lists of scene GUIDs and assign baking set to the PerSceneData.
            var bakedSceneGUIDList = new List<string>();
            foreach (var data in bakedSceneDataList)
            {
                Debug.Assert(ProbeVolumeBakingSet.SceneHasProbeVolumes(data.sceneGUID));
                bakedSceneGUIDList.Add(data.sceneGUID);

                if (m_BakingSet != data.serializedBakingSet)
                {
                    data.serializedBakingSet = m_BakingSet;
                    EditorUtility.SetDirty(data);
                }
            }

            var currentPerSceneCellList = m_BakingSet.perSceneCellLists; // Cell lists from last baking.
            m_BakingSet.perSceneCellLists = new SerializedDictionary<string, List<int>>();

            // Partial baking: Copy over scene cell lists for scenes not being baked.
            // Layout change: Remap indices.
            foreach (var scene in currentPerSceneCellList)
            {
                // Scene is not baked. Remap if needed or add it back to the baking set.
                if (!bakedSceneGUIDList.Contains(scene.Key))
                {
                    if (needRemap)
                    {
                        var newCellList = new List<int>();
                        foreach (var cell in scene.Value)
                            newCellList.Add(cellRemapTable[cell]);

                        m_BakingSet.perSceneCellLists.Add(scene.Key, newCellList);
                    }
                    else
                    {
                        m_BakingSet.perSceneCellLists.Add(scene.Key, scene.Value);
                    }
                }
            }

            // Allocate baked cells to the relevant scenes cell list.
            foreach (var cell in m_BakedCells.Values)
            {
                foreach (var scene in m_BakingBatch.cellIndex2SceneReferences[cell.index])
                {
                    // This scene has a probe volume in it?
                    if (bakedSceneGUIDList.Contains(scene))
                    {
                        List<int> indexList;
                        if (!m_BakingSet.perSceneCellLists.TryGetValue(scene, out indexList))
                        {
                            indexList = new List<int>();
                            m_BakingSet.perSceneCellLists.Add(scene, indexList);
                        }

                        indexList.Add(cell.index);
                    }
                }
            }

            EditorUtility.SetDirty(m_BakingSet);
        }

        static void PrepareCellsForWriting(bool isBakingSubset)
        {
            // Remap if needed existing Cell descriptors in the baking set.
            var cellRemapTable = RemapBakedCells(isBakingSubset);

            // Generate list of cells for all scenes being baked and remap untouched existing scenes if needed.
            GenerateScenesCellLists(GetPerSceneDataList(), cellRemapTable);

            if (isBakingSubset)
            {
                // Resolve all unloaded scene cells in CPU memory. This will allow us to extract them into BakingCells in order to have the full list for writing.
                // Other cells should already be in the baked cells list.
                var loadedSceneDataList = ProbeReferenceVolume.instance.perSceneDataList;
                foreach(var sceneGUID in m_BakingSet.sceneGUIDs)
                {
                    // If a scene was baked
                    if (m_BakingSet.perSceneCellLists.TryGetValue(sceneGUID, out var cellList))
                    {
                        // And the scene is not in the baked subset
                        if (cellList.Count != 0 && !partialBakeSceneList.Contains(sceneGUID))
                        {
                            // Resolve its data in CPU memory.
                            bool resolved = m_BakingSet.ResolveCellData(cellList);
                            Debug.Assert(resolved, "Could not resolve unloaded scene data");
                        }
                    }
                }

                // Extract all cells that weren't baked into baking cells.
                // Merge existing data of cells belonging both to the baking scene list and to scenes not being baked (prevents losing placement data for those).
                // This way we have a full cell list to provide to WriteBakingCells
                ExtractBakingCells();
            }
        }

        static void FinalizeCell(int c, NativeArray<int> positionRemap,
            NativeArray<SphericalHarmonicsL2> sh, NativeArray<float> validity,
            NativeArray<uint> renderingLayerMasks,
            NativeArray<Vector3> virtualOffsets,
            NativeArray<Vector4> skyOcclusion,
            NativeArray<uint> skyDirection,
            NativeArray<Vector4> probeOcclusion)
        {
            if (c == 0)
            {
                m_BakedCells.Clear();
                m_CellPosToIndex.Clear();
                m_CellsToDilate.Clear();
            }

            bool hasRenderingLayers = renderingLayerMasks.IsCreated;
            bool hasVirtualOffset = virtualOffsets.IsCreated;
            bool hasSkyOcclusion = skyOcclusion.IsCreated;
            bool hasSkyDirection = skyDirection.IsCreated;
            bool hasProbeOcclusion = probeOcclusion.IsCreated;

            var cell = m_BakingBatch.cells[c];
            int numProbes = cell.probePositions.Length;
            Debug.Assert(numProbes > 0);

            var probeRefVolume = ProbeReferenceVolume.instance;
            var localTouchupVolumes = cell.SelectIntersectingAdjustmentVolumes(s_AdjustmentVolumes);

            cell.sh = new SphericalHarmonicsL2[numProbes];
            cell.layerValidity = hasRenderingLayers ? new byte[numProbes] : null;
            cell.validity = new float[numProbes];
            cell.validityNeighbourMask = new byte[APVDefinitions.probeMaxRegionCount, numProbes];
            cell.probeOcclusion = new Vector4[hasProbeOcclusion ? numProbes : 0];
            cell.skyOcclusionDataL0L1 = new Vector4[hasSkyOcclusion ? numProbes : 0];
            cell.skyShadingDirectionIndices = new byte[hasSkyDirection ? numProbes : 0];
            cell.offsetVectors = new Vector3[hasVirtualOffset ? numProbes : 0];
            cell.touchupVolumeInteraction = new float[numProbes];
            cell.minSubdiv = probeRefVolume.GetMaxSubdivision();
            cell.shChunkCount = ProbeBrickPool.GetChunkCount(cell.bricks.Length);

            for (int i = 0; i < numProbes; ++i)
            {
                int brickIdx = i / 64;
                int subdivLevel = cell.bricks[brickIdx].subdivisionLevel;
                cell.minSubdiv = Mathf.Min(cell.minSubdiv, subdivLevel);

                int uniqueProbeIndex = positionRemap[cell.probeIndices[i]];
                cell.SetBakedData(m_BakingSet, m_BakingBatch, localTouchupVolumes, i, uniqueProbeIndex,
                    sh[uniqueProbeIndex], validity[uniqueProbeIndex], renderingLayerMasks, virtualOffsets, skyOcclusion, skyDirection, probeOcclusion);
            }

            ComputeValidityMasks(cell);

            m_BakedCells[cell.index] = cell;
            m_CellsToDilate[cell.index] = cell;
            m_CellPosToIndex.Add(cell.position, cell.index);
        }

        static void AnalyzeBrickForIndirectionEntries(ref BakingCell cell)
        {
            var prv = ProbeReferenceVolume.instance;
            int cellSizeInBricks = m_ProfileInfo.cellSizeInBricks;
            int entrySubdivLevel = Mathf.Min(m_ProfileInfo.simplificationLevels, prv.GetGlobalIndirectionEntryMaxSubdiv());
            int indirectionEntrySizeInBricks = ProbeReferenceVolume.CellSize(entrySubdivLevel);
            int numOfIndirectionEntriesPerCellDim = cellSizeInBricks / indirectionEntrySizeInBricks;

            int numOfEntries = numOfIndirectionEntriesPerCellDim * numOfIndirectionEntriesPerCellDim * numOfIndirectionEntriesPerCellDim;
            cell.indirectionEntryInfo = new IndirectionEntryInfo[numOfEntries];

            // This is fairly naive now, if we need optimization this is the place to be.

            Vector3Int cellPosInEntries = cell.position * numOfIndirectionEntriesPerCellDim;
            Vector3Int cellPosInBricks = cell.position * cellSizeInBricks;

            int totalIndexChunks = 0;
            int i = 0;
            for (int x = 0; x < numOfIndirectionEntriesPerCellDim; ++x)
            {
                for (int y = 0; y < numOfIndirectionEntriesPerCellDim; ++y)
                {
                    for (int z = 0; z < numOfIndirectionEntriesPerCellDim; ++z)
                    {
                        Vector3Int entryPositionInBricks = cellPosInBricks + new Vector3Int(x, y, z) * indirectionEntrySizeInBricks;
                        Bounds entryBoundsInBricks = new Bounds();
                        entryBoundsInBricks.min = entryPositionInBricks;
                        entryBoundsInBricks.max = entryPositionInBricks + new Vector3Int(indirectionEntrySizeInBricks, indirectionEntrySizeInBricks, indirectionEntrySizeInBricks);

                        int minSubdiv = m_ProfileInfo.maxSubdivision;
                        bool touchedBrick = false;
                        foreach (Brick b in cell.bricks)
                        {
                            if (b.subdivisionLevel < minSubdiv)
                            {
                                if (b.IntersectArea(entryBoundsInBricks))
                                {
                                    touchedBrick = true;
                                    minSubdiv = b.subdivisionLevel;
                                    if (minSubdiv == 0) break;
                                }
                            }
                        }

                        cell.indirectionEntryInfo[i].minSubdiv = minSubdiv;
                        cell.indirectionEntryInfo[i].positionInBricks = cellPosInBricks + new Vector3Int(x, y, z) * indirectionEntrySizeInBricks;
                        cell.indirectionEntryInfo[i].hasOnlyBiggerBricks = minSubdiv > entrySubdivLevel && touchedBrick;

                        prv.ComputeEntryMinMax(ref cell.indirectionEntryInfo[i], cell.bricks);
                        int brickCount = ProbeReferenceVolume.GetNumberOfBricksAtSubdiv(cell.indirectionEntryInfo[i]);

                        totalIndexChunks += Mathf.CeilToInt((float)brickCount / ProbeBrickIndex.kIndexChunkSize);

                        i++;
                    }
                }
            }

            // Chunk count.
            cell.indexChunkCount = totalIndexChunks;
        }

        // Mathf.HalfToFloat(Mathf.FloatToHalf(float.MaxValue)) returns +inf, so clamp manually to avoid that
        static float s_MaxSHValue = 65504; // IEEE max half

        static ushort SHFloatToHalf(float value)
        {
            return Mathf.FloatToHalf(Mathf.Min(value, s_MaxSHValue));
        }

        static float SHHalfToFloat(ushort value)
        {
            return Mathf.HalfToFloat(value);
        }

        static byte SHFloatToByte(float value)
        {
            return (byte)(Mathf.Clamp(value, 0.0f, 1.0f) * 255.0f);
        }

        static float SHByteToFloat(byte value)
        {
            return value / 255.0f;
        }

        static void WriteToShaderCoeffsL0L1(in SphericalHarmonicsL2 sh, NativeArray<ushort> shaderCoeffsL0L1Rx, NativeArray<byte> shaderCoeffsL1GL1Ry, NativeArray<byte> shaderCoeffsL1BL1Rz, int offset)
        {
            shaderCoeffsL0L1Rx[offset + 0] = SHFloatToHalf(sh[0, 0]); shaderCoeffsL0L1Rx[offset + 1] = SHFloatToHalf(sh[1, 0]); shaderCoeffsL0L1Rx[offset + 2] = SHFloatToHalf(sh[2, 0]); shaderCoeffsL0L1Rx[offset + 3] = SHFloatToHalf(sh[0, 1]);
            shaderCoeffsL1GL1Ry[offset + 0] = SHFloatToByte(sh[1, 1]); shaderCoeffsL1GL1Ry[offset + 1] = SHFloatToByte(sh[1, 2]); shaderCoeffsL1GL1Ry[offset + 2] = SHFloatToByte(sh[1, 3]); shaderCoeffsL1GL1Ry[offset + 3] = SHFloatToByte(sh[0, 2]);
            shaderCoeffsL1BL1Rz[offset + 0] = SHFloatToByte(sh[2, 1]); shaderCoeffsL1BL1Rz[offset + 1] = SHFloatToByte(sh[2, 2]); shaderCoeffsL1BL1Rz[offset + 2] = SHFloatToByte(sh[2, 3]); shaderCoeffsL1BL1Rz[offset + 3] = SHFloatToByte(sh[0, 3]);
        }

        static void WriteToShaderCoeffsL2(in SphericalHarmonicsL2 sh, NativeArray<byte> shaderCoeffsL2_0, NativeArray<byte> shaderCoeffsL2_1, NativeArray<byte> shaderCoeffsL2_2, NativeArray<byte> shaderCoeffsL2_3, int offset)
        {
            shaderCoeffsL2_0[offset + 0] = SHFloatToByte(sh[0, 4]); shaderCoeffsL2_0[offset + 1] = SHFloatToByte(sh[0, 5]); shaderCoeffsL2_0[offset + 2] = SHFloatToByte(sh[0, 6]); shaderCoeffsL2_0[offset + 3] = SHFloatToByte(sh[0, 7]);
            shaderCoeffsL2_1[offset + 0] = SHFloatToByte(sh[1, 4]); shaderCoeffsL2_1[offset + 1] = SHFloatToByte(sh[1, 5]); shaderCoeffsL2_1[offset + 2] = SHFloatToByte(sh[1, 6]); shaderCoeffsL2_1[offset + 3] = SHFloatToByte(sh[1, 7]);
            shaderCoeffsL2_2[offset + 0] = SHFloatToByte(sh[2, 4]); shaderCoeffsL2_2[offset + 1] = SHFloatToByte(sh[2, 5]); shaderCoeffsL2_2[offset + 2] = SHFloatToByte(sh[2, 6]); shaderCoeffsL2_2[offset + 3] = SHFloatToByte(sh[2, 7]);
            shaderCoeffsL2_3[offset + 0] = SHFloatToByte(sh[0, 8]); shaderCoeffsL2_3[offset + 1] = SHFloatToByte(sh[1, 8]); shaderCoeffsL2_3[offset + 2] = SHFloatToByte(sh[2, 8]);
        }

        static void ReadFromShaderCoeffsL0L1(ref SphericalHarmonicsL2 sh, NativeArray<ushort> shaderCoeffsL0L1Rx, NativeArray<byte> shaderCoeffsL1GL1Ry, NativeArray<byte> shaderCoeffsL1BL1Rz, int offset)
        {
            sh[0, 0] = SHHalfToFloat(shaderCoeffsL0L1Rx[offset + 0]); sh[1, 0] = SHHalfToFloat(shaderCoeffsL0L1Rx[offset + 1]); sh[2, 0] = SHHalfToFloat(shaderCoeffsL0L1Rx[offset + 2]); sh[0, 1] = SHHalfToFloat(shaderCoeffsL0L1Rx[offset + 3]);
            sh[1, 1] = SHByteToFloat(shaderCoeffsL1GL1Ry[offset + 0]); sh[1, 2] = SHByteToFloat(shaderCoeffsL1GL1Ry[offset + 1]); sh[1, 3] = SHByteToFloat(shaderCoeffsL1GL1Ry[offset + 2]); sh[0, 2] = SHByteToFloat(shaderCoeffsL1GL1Ry[offset + 3]);
            sh[2, 1] = SHByteToFloat(shaderCoeffsL1BL1Rz[offset + 0]); sh[2, 2] = SHByteToFloat(shaderCoeffsL1BL1Rz[offset + 1]); sh[2, 3] = SHByteToFloat(shaderCoeffsL1BL1Rz[offset + 2]); sh[0, 3] = SHByteToFloat(shaderCoeffsL1BL1Rz[offset + 3]);
        }

        static void ReadFromShaderCoeffsL2(ref SphericalHarmonicsL2 sh, NativeArray<byte> shaderCoeffsL2_0, NativeArray<byte> shaderCoeffsL2_1, NativeArray<byte> shaderCoeffsL2_2, NativeArray<byte> shaderCoeffsL2_3, int offset)
        {
            sh[0, 4] = SHByteToFloat(shaderCoeffsL2_0[offset + 0]); sh[0, 5] = SHByteToFloat(shaderCoeffsL2_0[offset + 1]); sh[0, 6] = SHByteToFloat(shaderCoeffsL2_0[offset + 2]); sh[0, 7] = SHByteToFloat(shaderCoeffsL2_0[offset + 3]);
            sh[1, 4] = SHByteToFloat(shaderCoeffsL2_1[offset + 0]); sh[1, 5] = SHByteToFloat(shaderCoeffsL2_1[offset + 1]); sh[1, 6] = SHByteToFloat(shaderCoeffsL2_1[offset + 2]); sh[1, 7] = SHByteToFloat(shaderCoeffsL2_1[offset + 3]);
            sh[2, 4] = SHByteToFloat(shaderCoeffsL2_2[offset + 0]); sh[2, 5] = SHByteToFloat(shaderCoeffsL2_2[offset + 1]); sh[2, 6] = SHByteToFloat(shaderCoeffsL2_2[offset + 2]); sh[2, 7] = SHByteToFloat(shaderCoeffsL2_2[offset + 3]);
            sh[0, 8] = SHByteToFloat(shaderCoeffsL2_3[offset + 0]); sh[1, 8] = SHByteToFloat(shaderCoeffsL2_3[offset + 1]); sh[2, 8] = SHByteToFloat(shaderCoeffsL2_3[offset + 2]);
        }

        static void ReadFullFromShaderCoeffsL0L1L2(ref SphericalHarmonicsL2 sh,
            NativeArray<ushort> shaderCoeffsL0L1Rx, NativeArray<byte> shaderCoeffsL1GL1Ry, NativeArray<byte> shaderCoeffsL1BL1Rz,
            NativeArray<byte> shaderCoeffsL2_0, NativeArray<byte> shaderCoeffsL2_1, NativeArray<byte> shaderCoeffsL2_2, NativeArray<byte> shaderCoeffsL2_3,
            int probeIdx)
        {
            ReadFromShaderCoeffsL0L1(ref sh, shaderCoeffsL0L1Rx, shaderCoeffsL1GL1Ry, shaderCoeffsL1BL1Rz, probeIdx * 4);
            if (shaderCoeffsL2_0.Length > 0)
                ReadFromShaderCoeffsL2(ref sh, shaderCoeffsL2_0, shaderCoeffsL2_1, shaderCoeffsL2_2, shaderCoeffsL2_3, probeIdx * 4);

        }

        static void WriteToShaderSkyOcclusion(in Vector4 occlusionL0L1, NativeArray<ushort> shaderCoeffsSkyOcclusionL0L1, int offset)
        {
            shaderCoeffsSkyOcclusionL0L1[offset + 0] = SHFloatToHalf(occlusionL0L1.x);
            shaderCoeffsSkyOcclusionL0L1[offset + 1] = SHFloatToHalf(occlusionL0L1.y);
            shaderCoeffsSkyOcclusionL0L1[offset + 2] = SHFloatToHalf(occlusionL0L1.z);
            shaderCoeffsSkyOcclusionL0L1[offset + 3] = SHFloatToHalf(occlusionL0L1.w);
        }

        static void ReadFromShaderCoeffsSkyOcclusion(ref Vector4 skyOcclusionL0L1, NativeArray<ushort> skyOcclusionDataL0L1, int probeIdx)
        {
            int offset = probeIdx * 4;
            skyOcclusionL0L1.x = SHHalfToFloat(skyOcclusionDataL0L1[offset + 0]);
            skyOcclusionL0L1.y = SHHalfToFloat(skyOcclusionDataL0L1[offset + 1]);
            skyOcclusionL0L1.z = SHHalfToFloat(skyOcclusionDataL0L1[offset + 2]);
            skyOcclusionL0L1.w = SHHalfToFloat(skyOcclusionDataL0L1[offset + 3]);
        }

        static void WriteToShaderProbeOcclusion(in Vector4 probeOcclusion, NativeArray<byte> shaderCoeffsProbeOcclusion, int offset)
        {
            shaderCoeffsProbeOcclusion[offset + 0] = SHFloatToByte(probeOcclusion.x);
            shaderCoeffsProbeOcclusion[offset + 1] = SHFloatToByte(probeOcclusion.y);
            shaderCoeffsProbeOcclusion[offset + 2] = SHFloatToByte(probeOcclusion.z);
            shaderCoeffsProbeOcclusion[offset + 3] = SHFloatToByte(probeOcclusion.w);
        }

        static void ReadFromShaderCoeffsProbeOcclusion(ref Vector4 probeOcclusion, NativeArray<byte> probeOcclusionData, int probeIdx)
        {
            int offset = probeIdx * 4;
            probeOcclusion.x = SHByteToFloat(probeOcclusionData[offset + 0]);
            probeOcclusion.y = SHByteToFloat(probeOcclusionData[offset + 1]);
            probeOcclusion.z = SHByteToFloat(probeOcclusionData[offset + 2]);
            probeOcclusion.w = SHByteToFloat(probeOcclusionData[offset + 3]);
        }

        // Returns index in the GPU layout of probe of coordinate (x, y, z) in the brick at brickIndex for a DataLocation of size locSize
        static int GetProbeGPUIndex(int brickIndex, int x, int y, int z, Vector3Int locSize)
        {
            Vector3Int locSizeInBrick = locSize / ProbeBrickPool.kBrickProbeCountPerDim;

            int bx = brickIndex % locSizeInBrick.x;
            int by = (brickIndex / locSizeInBrick.x) % locSizeInBrick.y;
            int bz = ((brickIndex / locSizeInBrick.x) / locSizeInBrick.y) % locSizeInBrick.z;

            // In probes
            int ix = bx * ProbeBrickPool.kBrickProbeCountPerDim + x;
            int iy = by * ProbeBrickPool.kBrickProbeCountPerDim + y;
            int iz = bz * ProbeBrickPool.kBrickProbeCountPerDim + z;

            return ix + locSize.x * (iy + locSize.y * iz);
        }

        static BakingCell ConvertCellToBakingCell(CellDesc cellDesc, CellData cellData)
        {
            BakingCell bc = new BakingCell
            {
                position = cellDesc.position,
                index = cellDesc.index,
                bricks = cellData.bricks.ToArray(),
                minSubdiv = cellDesc.minSubdiv,
                indexChunkCount = cellDesc.indexChunkCount,
                shChunkCount = cellDesc.shChunkCount,
                probeIndices = null, // Not needed for this conversion.
                indirectionEntryInfo = cellDesc.indirectionEntryInfo,
            };

            bool hasRenderingLayers = cellData.layer.Length > 0;
            bool hasVirtualOffsets = cellData.offsetVectors.Length > 0;
            bool hasSkyOcclusion = cellData.skyOcclusionDataL0L1.Length > 0;
            bool hasSkyShadingDirection = cellData.skyShadingDirectionIndices.Length > 0;
            bool hasProbeOcclusion = cellData.scenarios.TryGetValue(m_BakingSet.lightingScenario, out var scenarioData) && scenarioData.probeOcclusion.Length > 0;


            // Runtime Cell arrays may contain padding to match chunk size
            // so we use the actual probe count for these arrays.
            int probeCount = cellDesc.probeCount;
            bc.probePositions = new Vector3[probeCount];
            bc.layerValidity = hasRenderingLayers ? new byte[probeCount] : null;
            bc.validity = new float[probeCount];
            bc.touchupVolumeInteraction = new float[probeCount];
            bc.validityNeighbourMask = new byte[APVDefinitions.probeMaxRegionCount, probeCount];
            bc.probeOcclusion = hasProbeOcclusion ? new Vector4[probeCount] : null;
            bc.skyOcclusionDataL0L1 = hasSkyOcclusion ? new Vector4[probeCount] : null;
            bc.skyShadingDirectionIndices = hasSkyShadingDirection ? new byte[probeCount] : null;
            bc.offsetVectors = hasVirtualOffsets ? new Vector3[probeCount] : null;
            bc.sh = new SphericalHarmonicsL2[probeCount];

            // Runtime data layout is for GPU consumption.
            // We need to convert it back to a linear layout for the baking cell.
            int probeIndex = 0;
            int chunkOffsetInProbes = 0;
            var chunksCount = cellDesc.shChunkCount;
            var chunkSizeInProbes = ProbeBrickPool.GetChunkSizeInProbeCount();
            Vector3Int locSize = ProbeBrickPool.ProbeCountToDataLocSize(chunkSizeInProbes);

            var blackSH = GetBlackSH();

            for (int chunkIndex = 0; chunkIndex < chunksCount; ++chunkIndex)
            {
                var cellChunkData = GetCellChunkData(cellData, chunkIndex);

                for (int brickIndex = 0; brickIndex < m_BakingSet.chunkSizeInBricks; ++brickIndex)
                {
                    if (probeIndex >= probeCount)
                        break;

                    for (int z = 0; z < ProbeBrickPool.kBrickProbeCountPerDim; z++)
                    {
                        for (int y = 0; y < ProbeBrickPool.kBrickProbeCountPerDim; y++)
                        {
                            for (int x = 0; x < ProbeBrickPool.kBrickProbeCountPerDim; x++)
                            {
                                var remappedIndex = GetProbeGPUIndex(brickIndex, x, y, z, locSize);

                                // Scenario data can be invalid due to partially baking the set.
                                if (cellChunkData.scenarioValid)
                                {
                                    ReadFullFromShaderCoeffsL0L1L2(ref bc.sh[probeIndex], cellChunkData.shL0L1RxData, cellChunkData.shL1GL1RyData, cellChunkData.shL1BL1RzData,
                                        cellChunkData.shL2Data_0, cellChunkData.shL2Data_1, cellChunkData.shL2Data_2, cellChunkData.shL2Data_3, remappedIndex);

                                    if (hasProbeOcclusion)
                                        ReadFromShaderCoeffsProbeOcclusion(ref bc.probeOcclusion[probeIndex], cellChunkData.probeOcclusion, remappedIndex);
                                }
                                else
                                {
                                    bc.sh[probeIndex] = blackSH;

                                    if (hasProbeOcclusion)
                                        bc.probeOcclusion[probeIndex] = Vector4.one;
                                }

                                for (int l = 0; l < APVDefinitions.probeMaxRegionCount; l++)
                                    bc.validityNeighbourMask[l, probeIndex] = cellChunkData.validityNeighMaskData[remappedIndex];
                                if (hasSkyOcclusion)
                                    ReadFromShaderCoeffsSkyOcclusion(ref bc.skyOcclusionDataL0L1[probeIndex], cellChunkData.skyOcclusionDataL0L1, remappedIndex);
                                if (hasSkyShadingDirection)
                                {
                                    bc.skyShadingDirectionIndices[probeIndex] = cellChunkData.skyShadingDirectionIndices[remappedIndex];
                                }

                                remappedIndex += chunkOffsetInProbes;
                                bc.probePositions[probeIndex] = cellData.probePositions[remappedIndex];
                                bc.validity[probeIndex] = cellData.validity[remappedIndex];
                                bc.touchupVolumeInteraction[probeIndex] = cellData.touchupVolumeInteraction[remappedIndex];
                                if (hasRenderingLayers)
                                    bc.layerValidity[probeIndex] = cellData.layer[remappedIndex];
                                if (hasVirtualOffsets)
                                    bc.offsetVectors[probeIndex] = cellData.offsetVectors[remappedIndex];

                                probeIndex++;
                            }
                        }
                    }
                }

                chunkOffsetInProbes += chunkSizeInProbes;
            }

            return bc;
        }

        // This is slow, but artists wanted this... This can be optimized later.
        static BakingCell MergeCells(BakingCell dst, BakingCell srcCell)
        {
            int maxSubdiv = Math.Max(dst.bricks[0].subdivisionLevel, srcCell.bricks[0].subdivisionLevel);
            bool hasRenderingLayers = m_BakingSet.useRenderingLayers;
            bool hasVirtualOffsets = s_BakeData.virtualOffsetJob.offsets.IsCreated;
            bool hasSkyOcclusion = s_BakeData.skyOcclusionJob.occlusion.IsCreated;
            bool hasSkyShadingDirection = s_BakeData.skyOcclusionJob.shadingDirections.IsCreated;
            bool hasProbeOcclusion = s_BakeData.lightingJob.occlusion.IsCreated;

            List<(Brick, int, int)> consolidatedBricks = new List<(Brick, int, int)>();
            HashSet<(Vector3Int, int)> addedBricks = new HashSet<(Vector3Int, int)>();

            for (int b = 0; b < dst.bricks.Length; ++b)
            {
                var brick = dst.bricks[b];
                addedBricks.Add((brick.position, brick.subdivisionLevel));
                consolidatedBricks.Add((brick, b, 0));
            }

            // Now with lower priority we grab from src.
            for (int b = 0; b < srcCell.bricks.Length; ++b)
            {
                var brick = srcCell.bricks[b];

                if (!addedBricks.Contains((brick.position, brick.subdivisionLevel)))
                {
                    consolidatedBricks.Add((brick, b, 1));
                }
            }

            // And finally we sort. We don't need to check for anything but brick as we don't have duplicates.
            consolidatedBricks.Sort(((Brick, int, int) lhs, (Brick, int, int) rhs) =>
            {
                if (lhs.Item1.subdivisionLevel != rhs.Item1.subdivisionLevel)
                    return lhs.Item1.subdivisionLevel > rhs.Item1.subdivisionLevel ? -1 : 1;
                if (lhs.Item1.position.z != rhs.Item1.position.z)
                    return lhs.Item1.position.z < rhs.Item1.position.z ? -1 : 1;
                if (lhs.Item1.position.y != rhs.Item1.position.y)
                    return lhs.Item1.position.y < rhs.Item1.position.y ? -1 : 1;
                if (lhs.Item1.position.x != rhs.Item1.position.x)
                    return lhs.Item1.position.x < rhs.Item1.position.x ? -1 : 1;

                return 0;
            });

            BakingCell outCell = new BakingCell();

            int numberOfProbes = consolidatedBricks.Count * ProbeBrickPool.kBrickProbeCountTotal;
            outCell.index = dst.index;
            outCell.position = dst.position;
            outCell.bricks = new Brick[consolidatedBricks.Count];
            outCell.probePositions = new Vector3[numberOfProbes];
            outCell.minSubdiv = Math.Min(dst.minSubdiv, srcCell.minSubdiv);
            outCell.sh = new SphericalHarmonicsL2[numberOfProbes];
            outCell.layerValidity = hasRenderingLayers ? new byte[numberOfProbes] : null;
            outCell.validity = new float[numberOfProbes];
            outCell.validityNeighbourMask = new byte[APVDefinitions.probeMaxRegionCount, numberOfProbes];
            outCell.probeOcclusion = hasProbeOcclusion ? new Vector4[numberOfProbes] : null;
            outCell.skyOcclusionDataL0L1 = hasSkyOcclusion ? new Vector4[numberOfProbes] : null;
            outCell.skyShadingDirectionIndices = hasSkyShadingDirection ? new byte[numberOfProbes] : null;
            outCell.offsetVectors = hasVirtualOffsets ? new Vector3[numberOfProbes] : null;
            outCell.touchupVolumeInteraction = new float[numberOfProbes];
            outCell.shChunkCount = ProbeBrickPool.GetChunkCount(outCell.bricks.Length);
            // We don't need to analyse here, it will be done upon writing back.
            outCell.indirectionEntryInfo = new IndirectionEntryInfo[srcCell.indirectionEntryInfo.Length];

            BakingCell[] consideredCells = { dst, srcCell };

            for (int i = 0; i < consolidatedBricks.Count; ++i)
            {
                var b = consolidatedBricks[i];
                int brickIndexInSource = b.Item2;

                outCell.bricks[i] = consideredCells[b.Item3].bricks[brickIndexInSource];

                for (int p = 0; p < ProbeBrickPool.kBrickProbeCountTotal; ++p)
                {
                    int outIdx = i * ProbeBrickPool.kBrickProbeCountTotal + p;
                    int srcIdx = brickIndexInSource * ProbeBrickPool.kBrickProbeCountTotal + p;
                    outCell.probePositions[outIdx] = consideredCells[b.Item3].probePositions[srcIdx];
                    outCell.sh[outIdx] = consideredCells[b.Item3].sh[srcIdx];
                    outCell.validity[outIdx] = consideredCells[b.Item3].validity[srcIdx];
                    for (int l = 0; l < APVDefinitions.probeMaxRegionCount; l++)
                        outCell.validityNeighbourMask[l, outIdx] = consideredCells[b.Item3].validityNeighbourMask[l, srcIdx];
                    if (hasProbeOcclusion)
                        outCell.probeOcclusion[outIdx] = consideredCells[b.Item3].probeOcclusion[srcIdx];
                    if (hasRenderingLayers)
                        outCell.layerValidity[outIdx] = consideredCells[b.Item3].layerValidity[srcIdx];
                    if (hasSkyOcclusion)
                        outCell.skyOcclusionDataL0L1[outIdx] = consideredCells[b.Item3].skyOcclusionDataL0L1[srcIdx];
                    if (hasSkyShadingDirection)
                        outCell.skyShadingDirectionIndices[outIdx] = consideredCells[b.Item3].skyShadingDirectionIndices[srcIdx];
                    if (hasVirtualOffsets)
                        outCell.offsetVectors[outIdx] = consideredCells[b.Item3].offsetVectors[srcIdx];
                    outCell.touchupVolumeInteraction[outIdx] = consideredCells[b.Item3].touchupVolumeInteraction[srcIdx];
                }
            }
            return outCell;
        }

        static void ExtractBakingCells()
        {
            // For cells that are being baked, this loop will merge existing baked data with newly baked data to not lose data.
            var loadedSceneDataList = ProbeReferenceVolume.instance.perSceneDataList;
            foreach (var data in loadedSceneDataList)
            {
                var cells = m_BakingSet.GetSceneCellIndexList(data.sceneGUID);

                var numberOfCells = cells.Count;

                for (int i = 0; i < numberOfCells; ++i)
                {
                    if (m_BakedCells.ContainsKey(cells[i]))
                    {
                        var cell = m_BakingSet.GetCellDesc(cells[i]);

                        // This can happen if doing a partial bake before ever doing a full bake.
                        if (cell == null || !m_BakedCells.ContainsKey(cell.index))
                            continue;

                        var cellData = m_BakingSet.GetCellData(cells[i]);

                        // When doing partial baking some cells might not have any already baked data.
                        if (cellData == null || !cellData.scenarios.ContainsKey(m_BakingSet.lightingScenario))
                            continue;

                        BakingCell bc = ConvertCellToBakingCell(cell, cellData);
                        bc = MergeCells(m_BakedCells[cell.index], bc);
                        m_BakedCells[cell.index] = bc;
                    }
                }
            }

            // Here we convert to baking cells all cells that were not already baked.
            // This allows us to have the full set of cells ready for writing all at once.
            foreach (var cell in m_BakingSet.cellDescs.Values)
            {
                if (!m_BakedCells.ContainsKey(cell.index))
                {
                    var cellData = m_BakingSet.GetCellData(cell.index);
                    if (cellData == null)
                        continue;

                    m_BakedCells.Add(cell.index, ConvertCellToBakingCell(cell, cellData));
                }
            }
        }

        static long AlignRemainder16(long count) => count % 16L;

        /// <summary>
        /// Calculates support data chunk size based on provided configuration.
        /// </summary>
        /// <param name="chunkSizeInProbes">Number of probes per chunk</param>
        /// <param name="hasVirtualOffsets">Whether virtual offsets are enabled</param>
        /// <param name="hasRenderingLayers">Whether rendering layers are enabled</param>
        /// <returns>The size in bytes of a single support data chunk</returns>
        static int CalculateSupportDataChunkSize(int chunkSizeInProbes, bool hasVirtualOffsets, bool hasRenderingLayers)
        {
            int supportPositionChunkSize = UnsafeUtility.SizeOf<Vector3>() * chunkSizeInProbes;
            int supportValidityChunkSize = UnsafeUtility.SizeOf<float>() * chunkSizeInProbes;
            int supportTouchupChunkSize = UnsafeUtility.SizeOf<float>() * chunkSizeInProbes;
            int supportLayerMaskChunkSize = hasRenderingLayers ? UnsafeUtility.SizeOf<byte>() * chunkSizeInProbes : 0;
            int supportOffsetsChunkSize = hasVirtualOffsets ? UnsafeUtility.SizeOf<Vector3>() * chunkSizeInProbes : 0;
            
            return supportPositionChunkSize + supportValidityChunkSize + 
                   supportOffsetsChunkSize + supportLayerMaskChunkSize + supportTouchupChunkSize;
        }

        /// <summary>
        /// Validates that the baking cells can be written without exceeding system limits.
        /// This method performs size calculations without accessing any global state.
        /// </summary>
        /// <param name="bakingCells">Array of baking cells to validate</param>
        /// <param name="chunkSizeInProbes">Number of probes per chunk</param>
        /// <param name="hasVirtualOffsets">Whether virtual offsets are enabled</param>
        /// <param name="hasRenderingLayers">Whether rendering layers are enabled</param>
        /// <returns>True if cells can be written safely, false if they exceed limits</returns>
        static bool ValidateBakingCellsSize(BakingCell[] bakingCells, int chunkSizeInProbes, bool hasVirtualOffsets, bool hasRenderingLayers)
        {
            if (bakingCells == null || bakingCells.Length == 0)
                return true;

            int supportDataChunkSize = CalculateSupportDataChunkSize(chunkSizeInProbes, hasVirtualOffsets, hasRenderingLayers);
            
            // Calculate total chunks count - need to call AnalyzeBrickForIndirectionEntries to get shChunkCount
            // Create a copy to avoid modifying the original cells during validation
            var tempCells = new BakingCell[bakingCells.Length];
            int totalChunksCount = 0;
            for (var i = 0; i < bakingCells.Length; ++i)
            {
                tempCells[i] = bakingCells[i]; // Shallow copy is sufficient for this validation
                AnalyzeBrickForIndirectionEntries(ref tempCells[i]);
                totalChunksCount += tempCells[i].shChunkCount;
            }
            
            // Perform the critical size check
            long supportDataTotalSize = (long)totalChunksCount * supportDataChunkSize;
            if (supportDataTotalSize > int.MaxValue)
            {
                Debug.LogError($"The size of the Adaptive Probe Volume (APV) baking set chunks exceed the current system limit of {int.MaxValue}, unable to save the baked cell assets. Reduce density either by adjusting the general Probe Spacing in the Lighting window, or by modifying the Adaptive Probe Volumes in the scene to limit where the denser subdivision levels are used.");
                return false;
            }

            return true;
        }

        static void WriteNativeArray<T>(System.IO.FileStream fs, NativeArray<T> array) where T : struct
        {
            unsafe
            {
                fs.Write(new ReadOnlySpan<byte>(array.GetUnsafeReadOnlyPtr(), array.Length * UnsafeUtility.SizeOf<T>()));
                fs.Write(new byte[AlignRemainder16(fs.Position)]);
            }
        }

        /// <summary>
        /// This method attempts to convert a list of baking cells into 5 separate assets:
        ///  2 assets per baking state:
        ///   CellData: a binary flat file containing L0L1 probes data
        ///   CellOptionalData: a binary flat file containing L2 probe data (when present)
        ///  3 assets shared between states:
        ///   ProbeVolumeAsset: a Scriptable Object which currently contains book-keeping data, runtime cells, and references to flattened data
        ///   CellSharedData: a binary flat file containing bricks data
        ///   CellSupportData: a binary flat file containing debug data (stripped from player builds if building without debug shaders)
        /// </summary>
        static unsafe bool WriteBakingCells(BakingCell[] bakingCells)
        {
            m_BakingSet.GetBlobFileNames(m_BakingSet.lightingScenario, out var cellDataFilename, out var cellBricksDataFilename, out var cellOptionalDataFilename, out var cellProbeOcclusionDataFilename, out var cellSharedDataFilename, out var cellSupportDataFilename);

            m_BakingSet.cellDescs = new SerializedDictionary<int, CellDesc>();
            m_BakingSet.bakedMinDistanceBetweenProbes = m_ProfileInfo.minDistanceBetweenProbes;
            m_BakingSet.bakedSimplificationLevels = m_ProfileInfo.simplificationLevels;
            m_BakingSet.bakedProbeOffset = m_ProfileInfo.probeOffset;
            m_BakingSet.bakedProbeOcclusion = false;
            m_BakingSet.bakedSkyOcclusion = m_BakingSet.skyOcclusion;
            m_BakingSet.bakedSkyShadingDirection = m_BakingSet.bakedSkyOcclusion && m_BakingSet.skyOcclusionShadingDirection;
            m_BakingSet.bakedMaskCount = m_BakingSet.useRenderingLayers ? APVDefinitions.probeMaxRegionCount : 1;
            m_BakingSet.bakedLayerMasks = m_BakingSet.ComputeRegionMasks();

            var cellSharedDataDescs = new SerializedDictionary<int, StreamableCellDesc>();
            var cellL0L1DataDescs = new SerializedDictionary<int, StreamableCellDesc>();
            var cellL2DataDescs = new SerializedDictionary<int, StreamableCellDesc>();
            var cellProbeOcclusionDataDescs = new SerializedDictionary<int, StreamableCellDesc>();
            var cellBricksDescs = new SerializedDictionary<int, StreamableCellDesc>();
            var cellSupportDescs = new SerializedDictionary<int, StreamableCellDesc>();

            var voSettings = m_BakingSet.settings.virtualOffsetSettings;
            bool hasVirtualOffsets = voSettings.useVirtualOffset;
            bool handlesSkyOcclusion = m_BakingSet.bakedSkyOcclusion;
            bool handlesSkyShading = m_BakingSet.bakedSkyShadingDirection && m_BakingSet.bakedSkyShadingDirection;
            bool hasRenderingLayers = m_BakingSet.useRenderingLayers;
            int validityRegionCount = m_BakingSet.bakedMaskCount;

            for (var i = 0; i < bakingCells.Length; ++i)
            {
                AnalyzeBrickForIndirectionEntries(ref bakingCells[i]);
                var bakingCell = bakingCells[i];

                // If any cell had probe occlusion, the baking set has probe occlusion.
                m_BakingSet.bakedProbeOcclusion |= bakingCell.probeOcclusion?.Length > 0;

                m_BakingSet.cellDescs.Add(bakingCell.index, new CellDesc
                {
                    position = bakingCell.position,
                    index = bakingCell.index,
                    probeCount = bakingCell.probePositions.Length,
                    minSubdiv = bakingCell.minSubdiv,
                    indexChunkCount = bakingCell.indexChunkCount,
                    shChunkCount = bakingCell.shChunkCount,
                    indirectionEntryInfo = bakingCell.indirectionEntryInfo,
                    bricksCount = bakingCell.bricks.Length,
                });

                m_BakingSet.maxSHChunkCount = Mathf.Max(m_BakingSet.maxSHChunkCount, bakingCell.shChunkCount);

                m_TotalCellCounts.Add(new CellCounts
                {
                    bricksCount = bakingCell.bricks.Length,
                    chunksCount = bakingCell.shChunkCount
                });
            }

            // All per probe data is stored per chunk and contiguously for each cell.
            // This is done so that we can stream from disk one cell at a time by group of chunks.

            var chunkSizeInProbes = ProbeBrickPool.GetChunkSizeInProbeCount();

            // CellData
            // L0 and L1 Data: 12 Coeffs stored in 3 textures. L0 (rgb) and R1x as ushort in one texture, the rest as byte in two 4 component textures.
            var L0L1R1xChunkSize = sizeof(ushort) * 4 * chunkSizeInProbes; // 4 ushort components per probe
            var L1ChunkSize = sizeof(byte) * 4 * chunkSizeInProbes; // 4 components per probe
            var L0L1ChunkSize = L0L1R1xChunkSize + 2 * L1ChunkSize;
            var L0L1TotalSize = m_TotalCellCounts.chunksCount * L0L1ChunkSize;
            using var probesL0L1 = new NativeArray<byte>(L0L1TotalSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            m_BakingSet.L0ChunkSize = L0L1R1xChunkSize;
            m_BakingSet.L1ChunkSize = L1ChunkSize;

            // CellOptionalData
            // L2 Data: 15 Coeffs stored in 4 byte4 textures.
            var L2TextureChunkSize = 4 * sizeof(byte) * chunkSizeInProbes; // 4 byte component per probe
            var L2ChunkSize = L2TextureChunkSize * 4; // 4 Textures for all L2 data.
            var L2TotalSize = m_TotalCellCounts.chunksCount * L2ChunkSize; // 4 textures
            using var probesL2 = new NativeArray<byte>(L2TotalSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            m_BakingSet.L2TextureChunkSize = L2TextureChunkSize;

            // Probe occlusion data
            int probeOcclusionChunkSize = m_BakingSet.bakedProbeOcclusion ? sizeof(byte) * 4 * chunkSizeInProbes : 0; // 4 unorm per probe
            int probeOcclusionTotalSize = m_TotalCellCounts.chunksCount * probeOcclusionChunkSize;
            using var probeOcclusion = new NativeArray<byte>(probeOcclusionTotalSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            m_BakingSet.ProbeOcclusionChunkSize = probeOcclusionChunkSize;

            // CellSharedData
            m_BakingSet.sharedValidityMaskChunkSize = sizeof(byte) * validityRegionCount * chunkSizeInProbes;
            m_BakingSet.sharedSkyOcclusionL0L1ChunkSize = handlesSkyOcclusion ? sizeof(ushort) * 4 * chunkSizeInProbes : 0;
            m_BakingSet.sharedSkyShadingDirectionIndicesChunkSize = handlesSkyShading ? sizeof(byte) * chunkSizeInProbes : 0;
            m_BakingSet.sharedDataChunkSize = m_BakingSet.sharedValidityMaskChunkSize + m_BakingSet.sharedSkyOcclusionL0L1ChunkSize + m_BakingSet.sharedSkyShadingDirectionIndicesChunkSize;

            var sharedDataTotalSize = m_TotalCellCounts.chunksCount * m_BakingSet.sharedDataChunkSize;
            using var sharedData = new NativeArray<byte>(sharedDataTotalSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // Brick data
            using var bricks = new NativeArray<Brick>(m_TotalCellCounts.bricksCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // CellSupportData - use pure helper function for calculation
            m_BakingSet.supportPositionChunkSize = UnsafeUtility.SizeOf<Vector3>() * chunkSizeInProbes;
            m_BakingSet.supportValidityChunkSize = UnsafeUtility.SizeOf<float>() * chunkSizeInProbes;
            m_BakingSet.supportOffsetsChunkSize = hasVirtualOffsets ? UnsafeUtility.SizeOf<Vector3>() * chunkSizeInProbes : 0;
            m_BakingSet.supportTouchupChunkSize = UnsafeUtility.SizeOf<float>() * chunkSizeInProbes;
            m_BakingSet.supportLayerMaskChunkSize = hasRenderingLayers ? UnsafeUtility.SizeOf<byte>() * chunkSizeInProbes : 0;

            m_BakingSet.supportDataChunkSize = CalculateSupportDataChunkSize(chunkSizeInProbes, hasVirtualOffsets, hasRenderingLayers);
            long supportDataTotalSize = (long)m_TotalCellCounts.chunksCount * m_BakingSet.supportDataChunkSize;
            using var supportData = new NativeArray<byte>((int)supportDataTotalSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            var sceneStateHash = m_BakingSet.GetBakingHashCode();
            var startCounts = new CellCounts();

            int sharedChunkOffset = 0;

            int shL0L1ChunkOffset = 0;
            int shL2ChunkOffset = 0;
            int probeOcclusionChunkOffset = 0;
            int supportChunkOffset = 0;

            var blackSH = GetBlackSH();

            // Size of the DataLocation used to do the copy texture at runtime. Used to generate the right layout for the 3D texture.
            Vector3Int locSize = ProbeBrickPool.ProbeCountToDataLocSize(ProbeBrickPool.GetChunkSizeInProbeCount());

            for (var i = 0; i < bakingCells.Length; ++i)
            {
                var bakingCell = bakingCells[i];
                var cellDesc = m_BakingSet.cellDescs[bakingCell.index];
                var chunksCount = cellDesc.shChunkCount;

                cellSharedDataDescs.Add(bakingCell.index, new StreamableCellDesc() { offset = startCounts.chunksCount * m_BakingSet.sharedDataChunkSize, elementCount = chunksCount });
                cellL0L1DataDescs.Add(bakingCell.index, new StreamableCellDesc() { offset = startCounts.chunksCount * L0L1ChunkSize, elementCount = chunksCount });
                cellL2DataDescs.Add(bakingCell.index, new StreamableCellDesc() { offset = startCounts.chunksCount * L2ChunkSize, elementCount = chunksCount });
                cellProbeOcclusionDataDescs.Add(bakingCell.index, new StreamableCellDesc() { offset = startCounts.chunksCount * probeOcclusionChunkSize, elementCount = chunksCount });
                cellBricksDescs.Add(bakingCell.index, new StreamableCellDesc() { offset = startCounts.bricksCount * sizeof(Brick), elementCount = cellDesc.bricksCount });
                cellSupportDescs.Add(bakingCell.index, new StreamableCellDesc() { offset = startCounts.chunksCount * m_BakingSet.supportDataChunkSize, elementCount = chunksCount });

                sceneStateHash = sceneStateHash * 23 + bakingCell.GetBakingHashCode();

                var inputProbesCount = bakingCell.probePositions.Length;

                int shidx = 0;

                // Cell base offsets for each data streams
                int cellL0R1xOffset = shL0L1ChunkOffset;
                int cellL1GL1RyOffset = cellL0R1xOffset + chunksCount * L0L1R1xChunkSize;
                int cellL1BL1RzOffset = cellL1GL1RyOffset + chunksCount * L1ChunkSize;

                int validityMaskOffset = sharedChunkOffset;
                int skyOcclusionL0L1Offset = validityMaskOffset + chunksCount * m_BakingSet.sharedValidityMaskChunkSize;
                int skyShadingIndicesOffset = skyOcclusionL0L1Offset + chunksCount * m_BakingSet.sharedSkyOcclusionL0L1ChunkSize;

                int positionOffset = supportChunkOffset;
                int validityOffset = positionOffset + chunksCount * m_BakingSet.supportPositionChunkSize;
                int touchupOffset = validityOffset + chunksCount * m_BakingSet.supportValidityChunkSize;
                int layerOffset = touchupOffset + chunksCount * m_BakingSet.supportTouchupChunkSize; // This is optional
                int offsetsOffset = layerOffset + chunksCount * m_BakingSet.supportLayerMaskChunkSize; // Keep last as it's optional.

                // Here we directly map each chunk to the layout of the 3D textures in order to be able to copy the data directly to the GPU.
                // The granularity at runtime is one chunk at a time currently so the temporary data loc used is sized accordingly.
                for (int chunkIndex = 0; chunkIndex < chunksCount; ++chunkIndex)
                {
                    NativeArray<ushort> probesTargetL0L1Rx = probesL0L1.GetSubArray(cellL0R1xOffset + chunkIndex * L0L1R1xChunkSize, L0L1R1xChunkSize).Reinterpret<ushort>(1);
                    NativeArray<byte> probesTargetL1GL1Ry = probesL0L1.GetSubArray(cellL1GL1RyOffset + chunkIndex * L1ChunkSize, L1ChunkSize);
                    NativeArray<byte> probesTargetL1BL1Rz = probesL0L1.GetSubArray(cellL1BL1RzOffset + chunkIndex * L1ChunkSize, L1ChunkSize);

                    NativeArray<byte> validityNeighboorMaskChunkTarget = sharedData.GetSubArray(validityMaskOffset + chunkIndex * m_BakingSet.sharedValidityMaskChunkSize, m_BakingSet.sharedValidityMaskChunkSize);
                    NativeArray<ushort> skyOcclusionL0L1ChunkTarget = sharedData.GetSubArray(skyOcclusionL0L1Offset + chunkIndex * m_BakingSet.sharedSkyOcclusionL0L1ChunkSize, m_BakingSet.sharedSkyOcclusionL0L1ChunkSize).Reinterpret<ushort>(1);
                    NativeArray<byte> skyShadingIndicesChunkTarget = sharedData.GetSubArray(skyShadingIndicesOffset + chunkIndex * m_BakingSet.sharedSkyShadingDirectionIndicesChunkSize, m_BakingSet.sharedSkyShadingDirectionIndicesChunkSize);

                    NativeArray<Vector3> positionsChunkTarget = supportData.GetSubArray(positionOffset + chunkIndex * m_BakingSet.supportPositionChunkSize, m_BakingSet.supportPositionChunkSize).Reinterpret<Vector3>(1);
                    NativeArray<float> validityChunkTarget = supportData.GetSubArray(validityOffset + chunkIndex * m_BakingSet.supportValidityChunkSize, m_BakingSet.supportValidityChunkSize).Reinterpret<float>(1);
                    NativeArray<float> touchupVolumeInteractionChunkTarget = supportData.GetSubArray(touchupOffset + chunkIndex * m_BakingSet.supportTouchupChunkSize, m_BakingSet.supportTouchupChunkSize).Reinterpret<float>(1);
                    NativeArray<byte> regionChunkTarget = supportData.GetSubArray(layerOffset + chunkIndex * m_BakingSet.supportLayerMaskChunkSize, m_BakingSet.supportLayerMaskChunkSize).Reinterpret<byte>(1);
                    NativeArray<Vector3> offsetChunkTarget = supportData.GetSubArray(offsetsOffset + chunkIndex * m_BakingSet.supportOffsetsChunkSize, m_BakingSet.supportOffsetsChunkSize).Reinterpret<Vector3>(1);

                    NativeArray<byte> probesTargetL2_0 = probesL2.GetSubArray(shL2ChunkOffset + chunksCount * L2TextureChunkSize * 0 + chunkIndex * L2TextureChunkSize, L2TextureChunkSize);
                    NativeArray<byte> probesTargetL2_1 = probesL2.GetSubArray(shL2ChunkOffset + chunksCount * L2TextureChunkSize * 1 + chunkIndex * L2TextureChunkSize, L2TextureChunkSize);
                    NativeArray<byte> probesTargetL2_2 = probesL2.GetSubArray(shL2ChunkOffset + chunksCount * L2TextureChunkSize * 2 + chunkIndex * L2TextureChunkSize, L2TextureChunkSize);
                    NativeArray<byte> probesTargetL2_3 = probesL2.GetSubArray(shL2ChunkOffset + chunksCount * L2TextureChunkSize * 3 + chunkIndex * L2TextureChunkSize, L2TextureChunkSize);

                    NativeArray<byte> probeOcclusionTarget = probeOcclusion.GetSubArray(probeOcclusionChunkOffset + chunkIndex * m_BakingSet.ProbeOcclusionChunkSize, m_BakingSet.ProbeOcclusionChunkSize);

                    for (int brickIndex = 0; brickIndex < m_BakingSet.chunkSizeInBricks; brickIndex++)
                    {
                        for (int z = 0; z < ProbeBrickPool.kBrickProbeCountPerDim; z++)
                        {
                            for (int y = 0; y < ProbeBrickPool.kBrickProbeCountPerDim; y++)
                            {
                                for (int x = 0; x < ProbeBrickPool.kBrickProbeCountPerDim; x++)
                                {
                                    int index = GetProbeGPUIndex(brickIndex, x, y, z, locSize);

                                    // We are processing chunks at a time.
                                    // So in practice we can go over the number of SH we have in the input list.
                                    // We fill with encoded black to avoid copying garbage in the final atlas.
                                    if (shidx >= inputProbesCount)
                                    {
                                        WriteToShaderCoeffsL0L1(blackSH, probesTargetL0L1Rx, probesTargetL1GL1Ry, probesTargetL1BL1Rz, index * 4);
                                        WriteToShaderCoeffsL2(blackSH, probesTargetL2_0, probesTargetL2_1, probesTargetL2_2, probesTargetL2_3, index * 4);

                                        for (int l = 0; l < validityRegionCount; l++)
                                            validityNeighboorMaskChunkTarget[index * validityRegionCount + l] = 0;
                                        if (m_BakingSet.bakedSkyOcclusion)
                                        {
                                            WriteToShaderSkyOcclusion(Vector4.zero, skyOcclusionL0L1ChunkTarget, index * 4);
                                            if (m_BakingSet.bakedSkyShadingDirection)
                                            {
                                                skyShadingIndicesChunkTarget[index] = 255;
                                            }
                                        }

                                        if (m_BakingSet.bakedProbeOcclusion)
                                        {
                                            WriteToShaderProbeOcclusion(Vector4.one, probeOcclusionTarget, index * 4);
                                        }

                                        validityChunkTarget[index] = 0.0f;
                                        positionsChunkTarget[index] = Vector3.zero;
                                        touchupVolumeInteractionChunkTarget[index] = 0.0f;
                                        if (hasRenderingLayers)
                                            regionChunkTarget[index] = 0xFF;
                                        if (hasVirtualOffsets)
                                            offsetChunkTarget[index] = Vector3.zero;
                                    }
                                    else
                                    {
                                        ref var sh = ref bakingCell.sh[shidx];

                                        WriteToShaderCoeffsL0L1(sh, probesTargetL0L1Rx, probesTargetL1GL1Ry, probesTargetL1BL1Rz, index * 4);
                                        WriteToShaderCoeffsL2(sh, probesTargetL2_0, probesTargetL2_1, probesTargetL2_2, probesTargetL2_3, index * 4);

                                        for (int l = 0; l < validityRegionCount; l++)
                                            validityNeighboorMaskChunkTarget[index * validityRegionCount + l] = bakingCell.validityNeighbourMask[l, shidx];
                                        if (m_BakingSet.bakedSkyOcclusion)
                                        {
                                            WriteToShaderSkyOcclusion(bakingCell.skyOcclusionDataL0L1[shidx], skyOcclusionL0L1ChunkTarget, index * 4);
                                            if (m_BakingSet.bakedSkyShadingDirection)
                                            {
                                                skyShadingIndicesChunkTarget[index] = bakingCell.skyShadingDirectionIndices[shidx];
                                            }
                                        }

                                        if (m_BakingSet.bakedProbeOcclusion)
                                        {
                                            WriteToShaderProbeOcclusion(bakingCell.probeOcclusion[shidx], probeOcclusionTarget, index * 4);
                                        }

                                        validityChunkTarget[index] = bakingCell.validity[shidx];
                                        positionsChunkTarget[index] = bakingCell.probePositions[shidx];
                                        touchupVolumeInteractionChunkTarget[index] = bakingCell.touchupVolumeInteraction[shidx];
                                        if (hasRenderingLayers)
                                            regionChunkTarget[index] = bakingCell.layerValidity[shidx];
                                        if (hasVirtualOffsets)
                                            offsetChunkTarget[index] = bakingCell.offsetVectors[shidx];
                                    }
                                    shidx++;
                                }
                            }
                        }
                    }
                }

                shL0L1ChunkOffset += (chunksCount * L0L1ChunkSize);
                shL2ChunkOffset += (chunksCount * L2ChunkSize);
                probeOcclusionChunkOffset += (chunksCount * probeOcclusionChunkSize);
                supportChunkOffset += (chunksCount * m_BakingSet.supportDataChunkSize);
                sharedChunkOffset += (chunksCount * m_BakingSet.sharedDataChunkSize);

                bricks.GetSubArray(startCounts.bricksCount, cellDesc.bricksCount).CopyFrom(bakingCell.bricks);

                startCounts.Add(new CellCounts()
                {
                    bricksCount = cellDesc.bricksCount,
                    chunksCount = cellDesc.shChunkCount
                });
            }

            // Need to save here because the forced import below discards the changes.
            EditorUtility.SetDirty(m_BakingSet);
            AssetDatabase.SaveAssets();

            // Explicitly make sure the binary output files are writable since we write them using the C# file API (i.e. check out Perforce files if applicable)
            var outputPaths = new List<string>(new[] { cellDataFilename, cellBricksDataFilename, cellSharedDataFilename, cellSupportDataFilename, cellOptionalDataFilename, cellProbeOcclusionDataFilename });

            if (!AssetDatabase.MakeEditable(outputPaths.ToArray()))
                Debug.LogWarning($"Failed to make one or more probe volume output file(s) writable. This could result in baked data not being properly written to disk. {string.Join(",", outputPaths)}");

            unsafe
            {
                using (var fs = new System.IO.FileStream(cellDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite))
                {
                    WriteNativeArray(fs, probesL0L1);
                }
                using (var fs = new System.IO.FileStream(cellOptionalDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite))
                {
                    WriteNativeArray(fs, probesL2);
                }
                if (probeOcclusion.Length > 0)
                {
                    // Write the probe occlusion data file, only if this data was baked (shadowmask mode) - UUM-85411
                    using (var fs = new System.IO.FileStream(cellProbeOcclusionDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite))
                    {
                        WriteNativeArray(fs, probeOcclusion);
                    }
                }
                using (var fs = new System.IO.FileStream(cellSharedDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite))
                {
                    WriteNativeArray(fs, sharedData);
                }
                using (var fs = new System.IO.FileStream(cellBricksDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite))
                {
                    WriteNativeArray(fs, bricks);
                }
                using (var fs = new System.IO.FileStream(cellSupportDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite))
                {
                    WriteNativeArray(fs, supportData);
                }
            }

            AssetDatabase.ImportAsset(cellDataFilename);
            AssetDatabase.ImportAsset(cellOptionalDataFilename);
            // If we did not write a probe occlusion file (because it was zero bytes), don't try to load it (UUM-101480)
            if (probeOcclusion.Length > 0)
            {
                AssetDatabase.ImportAsset(cellProbeOcclusionDataFilename);
            }
            AssetDatabase.ImportAsset(cellBricksDataFilename);
            AssetDatabase.ImportAsset(cellSharedDataFilename);
            AssetDatabase.ImportAsset(cellSupportDataFilename);

            var bakingSetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m_BakingSet));

            m_BakingSet.scenarios[ProbeReferenceVolume.instance.lightingScenario] = new ProbeVolumeBakingSet.PerScenarioDataInfo
            {
                sceneHash = sceneStateHash,
                cellDataAsset = new ProbeVolumeStreamableAsset(kAPVStreamingAssetsPath, cellL0L1DataDescs, L0L1ChunkSize, bakingSetGUID, AssetDatabase.AssetPathToGUID(cellDataFilename)),
                cellOptionalDataAsset = new ProbeVolumeStreamableAsset(kAPVStreamingAssetsPath, cellL2DataDescs, L2ChunkSize, bakingSetGUID, AssetDatabase.AssetPathToGUID(cellOptionalDataFilename)),
                cellProbeOcclusionDataAsset = new ProbeVolumeStreamableAsset(kAPVStreamingAssetsPath, cellProbeOcclusionDataDescs, probeOcclusionChunkSize, bakingSetGUID, AssetDatabase.AssetPathToGUID(cellProbeOcclusionDataFilename)),
            };
            m_BakingSet.cellSharedDataAsset = new ProbeVolumeStreamableAsset(kAPVStreamingAssetsPath, cellSharedDataDescs, m_BakingSet.sharedDataChunkSize, bakingSetGUID, AssetDatabase.AssetPathToGUID(cellSharedDataFilename));
            m_BakingSet.cellBricksDataAsset = new ProbeVolumeStreamableAsset(kAPVStreamingAssetsPath, cellBricksDescs, sizeof(Brick), bakingSetGUID, AssetDatabase.AssetPathToGUID(cellBricksDataFilename));
            m_BakingSet.cellSupportDataAsset = new ProbeVolumeStreamableAsset(kAPVStreamingAssetsPath, cellSupportDescs, m_BakingSet.supportDataChunkSize, bakingSetGUID, AssetDatabase.AssetPathToGUID(cellSupportDataFilename));

            EditorUtility.SetDirty(m_BakingSet);

            return true;
        }

        unsafe static void WriteDilatedCells(List<Cell> cells)
        {
            m_BakingSet.GetBlobFileNames(m_BakingSet.lightingScenario, out var cellDataFilename, out var _, out var cellOptionalDataFilename, out var cellProbeOcclusionDataFilename, out var cellSharedDataFilename, out var _);

            var chunkSizeInProbes = ProbeBrickPool.GetChunkSizeInProbeCount();

            // CellData
            // L0 and L1 Data: 12 Coeffs stored in 3 textures. L0 (rgb) and R1x as ushort in one texture, the rest as byte in two 4 component textures.
            var L0L1R1xChunkSize = sizeof(ushort) * 4 * chunkSizeInProbes; // 4 ushort components per probe
            var L1ChunkSize = sizeof(byte) * 4 * chunkSizeInProbes; // 4 components per probe
            var L0L1ChunkSize = L0L1R1xChunkSize + 2 * L1ChunkSize;
            var L0L1TotalSize = m_TotalCellCounts.chunksCount * L0L1ChunkSize;
            using var probesL0L1 = new NativeArray<byte>(L0L1TotalSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // CellOptionalData
            // L2 Data: 15 Coeffs stored in 4 byte4 textures.
            var L2ChunkSize = 4 * sizeof(byte) * chunkSizeInProbes; // 4 byte component per probe
            var L2TotalSize = m_TotalCellCounts.chunksCount * L2ChunkSize * 4; // 4 textures
            using var probesL2 = new NativeArray<byte>(L2TotalSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // Probe occlusion data
            var probeOcclusionChunkSize = m_BakingSet.ProbeOcclusionChunkSize;
            var probeOcclusionTotalSize = m_TotalCellCounts.chunksCount * probeOcclusionChunkSize;
            using var probeOcclusion = new NativeArray<byte>(probeOcclusionTotalSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // CellSharedData
            var sharedValidityMaskChunkSize = m_BakingSet.sharedValidityMaskChunkSize;
            var sharedSkyOcclusionL0L1ChunkSize = m_BakingSet.sharedSkyOcclusionL0L1ChunkSize;
            var sharedSkyShadingDirectionIndicesChunkSize = m_BakingSet.sharedSkyShadingDirectionIndicesChunkSize;
            var sharedDataTotalSize = m_TotalCellCounts.chunksCount * m_BakingSet.sharedDataChunkSize;
            using var sharedData = new NativeArray<byte>(sharedDataTotalSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // We don't want to overwrite validity data
            sharedData.CopyFrom(System.IO.File.ReadAllBytes(cellSharedDataFilename));

            // When baking with partially loaded scenes, the list of cells being dilated might be smaller than the full list of cells in the bake.
            // In this case, in order not to destroy the rest of the data, we need to load it back before writing.
            if (cells.Count != m_BakingSet.cellDescs.Count)
            {
                probesL0L1.CopyFrom(System.IO.File.ReadAllBytes(cellDataFilename));
                probesL2.CopyFrom(System.IO.File.ReadAllBytes(cellOptionalDataFilename));
                probeOcclusion.CopyFrom(System.IO.File.ReadAllBytes(cellProbeOcclusionDataFilename));
            }

            var lightingScenario = ProbeReferenceVolume.instance.lightingScenario;
            Debug.Assert(m_BakingSet.scenarios.ContainsKey(lightingScenario));
            var scenarioDataInfo = m_BakingSet.scenarios[lightingScenario];

            for (var i = 0; i < cells.Count; ++i)
            {
                var srcCell = cells[i];

                var srcCellDesc = srcCell.desc;
                var scenarioData = srcCell.data.scenarios[lightingScenario];

                var L0L1chunkBaseOffset = scenarioDataInfo.cellDataAsset.streamableCellDescs[srcCellDesc.index].offset;
                var L2chunkBaseOffset = scenarioDataInfo.cellOptionalDataAsset.streamableCellDescs[srcCellDesc.index].offset;
                var probeOcclusionChunkBaseOffset = scenarioDataInfo.cellProbeOcclusionDataAsset.streamableCellDescs[srcCellDesc.index].offset;
                var sharedchunkBaseOffset = m_BakingSet.cellSharedDataAsset.streamableCellDescs[srcCellDesc.index].offset;
                var shChunksCount = srcCellDesc.shChunkCount;

                NativeArray<ushort> probesTargetL0L1Rx = probesL0L1.GetSubArray(L0L1chunkBaseOffset, L0L1R1xChunkSize * shChunksCount).Reinterpret<ushort>(1);
                NativeArray<byte> probesTargetL1GL1Ry = probesL0L1.GetSubArray(L0L1chunkBaseOffset + shChunksCount * L0L1R1xChunkSize, L1ChunkSize * shChunksCount);
                NativeArray<byte> probesTargetL1BL1Rz = probesL0L1.GetSubArray(L0L1chunkBaseOffset + shChunksCount * (L0L1R1xChunkSize + L1ChunkSize), L1ChunkSize * shChunksCount);

                probesTargetL0L1Rx.CopyFrom(scenarioData.shL0L1RxData);
                probesTargetL1GL1Ry.CopyFrom(scenarioData.shL1GL1RyData);
                probesTargetL1BL1Rz.CopyFrom(scenarioData.shL1BL1RzData);

                NativeArray<byte> probesTargetL2_0 = probesL2.GetSubArray(L2chunkBaseOffset + shChunksCount * L2ChunkSize * 0, L2ChunkSize * shChunksCount);
                NativeArray<byte> probesTargetL2_1 = probesL2.GetSubArray(L2chunkBaseOffset + shChunksCount * L2ChunkSize * 1, L2ChunkSize * shChunksCount);
                NativeArray<byte> probesTargetL2_2 = probesL2.GetSubArray(L2chunkBaseOffset + shChunksCount * L2ChunkSize * 2, L2ChunkSize * shChunksCount);
                NativeArray<byte> probesTargetL2_3 = probesL2.GetSubArray(L2chunkBaseOffset + shChunksCount * L2ChunkSize * 3, L2ChunkSize * shChunksCount);

                probesTargetL2_0.CopyFrom(scenarioData.shL2Data_0);
                probesTargetL2_1.CopyFrom(scenarioData.shL2Data_1);
                probesTargetL2_2.CopyFrom(scenarioData.shL2Data_2);
                probesTargetL2_3.CopyFrom(scenarioData.shL2Data_3);

                if (probeOcclusionChunkSize != 0)
                {
                    NativeArray<byte> probeOcclusionTarget = probeOcclusion.GetSubArray(probeOcclusionChunkBaseOffset, probeOcclusionChunkSize * shChunksCount);
                    probeOcclusionTarget.CopyFrom(scenarioData.probeOcclusion);
                }

                if (sharedSkyOcclusionL0L1ChunkSize != 0)
                {
                    NativeArray<ushort> skyOcclusionL0L1ChunkTarget = sharedData.GetSubArray(sharedchunkBaseOffset + shChunksCount * sharedValidityMaskChunkSize, sharedSkyOcclusionL0L1ChunkSize * shChunksCount).Reinterpret<ushort>(1);
                    skyOcclusionL0L1ChunkTarget.CopyFrom(srcCell.data.skyOcclusionDataL0L1);

                    if (sharedSkyShadingDirectionIndicesChunkSize != 0)
                    {
                        NativeArray<byte> skyShadingIndicesChunkTarget = sharedData.GetSubArray(sharedchunkBaseOffset + shChunksCount * (sharedValidityMaskChunkSize + sharedSkyOcclusionL0L1ChunkSize), sharedSkyShadingDirectionIndicesChunkSize * shChunksCount);
                        skyShadingIndicesChunkTarget.CopyFrom(srcCell.data.skyShadingDirectionIndices);
                    }
                }
            }

            // Explicitly make sure the binary output files are writable since we write them using the C# file API (i.e. check out Perforce files if applicable)
            var outputPaths = new List<string>(new[] { cellDataFilename, cellSharedDataFilename, cellOptionalDataFilename, cellProbeOcclusionDataFilename });

            if (!AssetDatabase.MakeEditable(outputPaths.ToArray()))
                Debug.LogWarning($"Failed to make one or more probe volume output file(s) writable. This could result in baked data not being properly written to disk. {string.Join(",", outputPaths)}");

            unsafe
            {
                using (var fs = new System.IO.FileStream(cellDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite))
                {
                    WriteNativeArray(fs, probesL0L1);
                }
                using (var fs = new System.IO.FileStream(cellOptionalDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite))
                {
                    WriteNativeArray(fs, probesL2);
                }
                using (var fs = new System.IO.FileStream(cellProbeOcclusionDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite))
                {
                    WriteNativeArray(fs, probeOcclusion);
                }
                using (var fs = new System.IO.FileStream(cellSharedDataFilename, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.ReadWrite))
                {
                    WriteNativeArray(fs, sharedData);
                }
            }
        }
    }
}
