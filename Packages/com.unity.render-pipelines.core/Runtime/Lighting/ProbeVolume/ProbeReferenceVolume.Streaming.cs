namespace UnityEngine.Rendering
{
    public partial class ProbeReferenceVolume
    {
        DynamicArray<CellInfo> m_LoadedCells = new DynamicArray<CellInfo>();
        DynamicArray<CellInfo> m_ToBeLoadedCells = new DynamicArray<CellInfo>();
        DynamicArray<CellInfo> m_TempCellToLoadList = new DynamicArray<CellInfo>();
        DynamicArray<CellInfo> m_TempCellToUnloadList = new DynamicArray<CellInfo>();

        DynamicArray<BlendingCellInfo> m_LoadedBlendingCells = new();
        DynamicArray<BlendingCellInfo> m_ToBeLoadedBlendingCells = new();
        DynamicArray<BlendingCellInfo> m_TempBlendingCellToLoadList = new();
        DynamicArray<BlendingCellInfo> m_TempBlendingCellToUnloadList = new();

        Vector3 m_FrozenCameraPosition;

        bool m_HasRemainingCellsToBlend = false;
        internal void ScenarioBlendingChanged(bool scenarioChanged)
        {
            m_HasRemainingCellsToBlend = true;
            if (scenarioChanged)
            {
                UnloadAllBlendingCells();
                for (int i = 0; i < m_ToBeLoadedBlendingCells.size; ++i)
                    m_ToBeLoadedBlendingCells[i].ForceReupload();
            }
        }

        /// <summary>
        /// Set the number of cells that are loaded per frame when needed.
        /// </summary>
        /// <param name="numberOfCells"></param>
        public void SetNumberOfCellsLoadedPerFrame(int numberOfCells)
        {
            m_NumberOfCellsLoadedPerFrame = Mathf.Max(1, numberOfCells);
        }

        void ComputeCellCameraDistance(Vector3 cameraPosition, DynamicArray<CellInfo> cells)
        {
            for (int i = 0; i < cells.size; ++i)
            {
                var cellInfo = cells[i];
                // For now streaming score is only distance based.
                cellInfo.streamingScore = Vector3.Distance(cameraPosition, cellInfo.cell.position);
            }
        }

        void ComputeStreamingScoreForBlending(DynamicArray<BlendingCellInfo> cells, float worstScore)
        {
            float factor = scenarioBlendingFactor;
            for (int i = 0; i < cells.size; ++i)
            {
                var blendingCell = cells[i];
                if (factor == blendingCell.blendingFactor)
                    blendingCell.MarkUpToDate();
                else
                {
                    blendingCell.streamingScore = blendingCell.cellInfo.streamingScore;
                    if (blendingCell.ShouldPrioritize())
                        blendingCell.streamingScore -= worstScore;
                }
            }
        }

        bool TryLoadCell(CellInfo cellInfo, ref int shBudget, ref int indexBudget, DynamicArray<CellInfo> loadedCells)
        {
            // Are we within budget?
            if (cellInfo.cell.shChunkCount <= shBudget && cellInfo.cell.indexChunkCount <= indexBudget)
            {
                // This can still fail because of fragmentation.
                // TODO: Handle defrag
                if (LoadCell(cellInfo))
                {
                    loadedCells.Add(cellInfo);

                    shBudget -= cellInfo.cell.shChunkCount;
                    indexBudget -= cellInfo.cell.indexChunkCount;
                    return true;
                }
            }
            return false;
        }

        void UnloadBlendingCell(BlendingCellInfo blendingCell, DynamicArray<BlendingCellInfo> unloadedCells)
        {
            UnloadBlendingCell(blendingCell);

            unloadedCells.Add(blendingCell);
        }

        bool TryLoadBlendingCell(BlendingCellInfo blendingCell, DynamicArray<BlendingCellInfo> loadedCells)
        {
            if (!AddBlendingBricks(blendingCell))
                return false;

            loadedCells.Add(blendingCell);

            return true;
        }

        /// <summary>
        /// Updates the cell streaming for a <see cref="Camera"/>
        /// </summary>
        /// <param name="cmd">The <see cref="CommandBuffer"/></param>
        /// <param name="camera">The <see cref="Camera"/></param>
        public void UpdateCellStreaming(CommandBuffer cmd, Camera camera)
        {
            if (!isInitialized) return;

            using (new ProfilingScope(null, ProfilingSampler.Get(CoreProfileId.APVCellStreamingUpdate)))
            {
                var cameraPosition = camera.transform.position;
                if (!probeVolumeDebug.freezeStreaming)
                {
                    m_FrozenCameraPosition = cameraPosition;
                }

                // Cell position in cell space is the top left corner. So we need to shift the camera position by half a cell to make things comparable.
                var cameraPositionCellSpace = (m_FrozenCameraPosition - m_Transform.posWS) / MaxBrickSize() - Vector3.one * 0.5f;

                ComputeCellCameraDistance(cameraPositionCellSpace, m_ToBeLoadedCells);
                ComputeCellCameraDistance(cameraPositionCellSpace, m_LoadedCells);

                m_ToBeLoadedCells.QuickSort();
                m_LoadedCells.QuickSort();

                // This is only a rough budget estimate at first.
                // It doesn't account for fragmentation.
                int indexChunkBudget = m_Index.GetRemainingChunkCount();
                int shChunkBudget = m_Pool.GetRemainingChunkCount();
                int cellCountToLoad = Mathf.Min(m_NumberOfCellsLoadedPerFrame, m_ToBeLoadedCells.size);

                if (m_SupportStreaming)
                {
                    while (m_TempCellToLoadList.size < cellCountToLoad)
                    {
                        // Enough memory, we can safely load the cell.
                        var cellInfo = m_ToBeLoadedCells[m_TempCellToLoadList.size];
                        if (!TryLoadCell(cellInfo, ref shChunkBudget, ref indexChunkBudget, m_TempCellToLoadList))
                            break;
                    }

                    // Budget reached. We need to figure out if we can safely unload other cells to make room.
                    if (m_TempCellToLoadList.size != cellCountToLoad)
                    {
                        int pendingUnloadCount = 0;
                        while (m_TempCellToLoadList.size < cellCountToLoad)
                        {
                            if (m_LoadedCells.size - pendingUnloadCount == 0)
                                break;

                            var furthestLoadedCell = m_LoadedCells[m_LoadedCells.size - pendingUnloadCount - 1];
                            var closestUnloadedCell = m_ToBeLoadedCells[m_TempCellToLoadList.size];

                            // The most distant loaded cell is further than the closest unloaded cell, we can unload it.
                            if (furthestLoadedCell.streamingScore > closestUnloadedCell.streamingScore)
                            {
                                pendingUnloadCount++;
                                UnloadCell(furthestLoadedCell);
                                shChunkBudget += furthestLoadedCell.cell.shChunkCount;
                                indexChunkBudget += furthestLoadedCell.cell.indexChunkCount;

                                m_TempCellToUnloadList.Add(furthestLoadedCell);

                                TryLoadCell(closestUnloadedCell, ref shChunkBudget, ref indexChunkBudget, m_TempCellToLoadList);
                            }
                            else // We are in a "stable" state, all the closest cells are loaded within the budget.
                                break;
                        }

                        if (pendingUnloadCount > 0)
                        {
                            m_LoadedCells.RemoveRange(m_LoadedCells.size - pendingUnloadCount, pendingUnloadCount);
                            RecomputeMinMaxLoadedCellPos();
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < cellCountToLoad; ++i)
                    {
                        var cellInfo = m_ToBeLoadedCells[m_TempCellToLoadList.size]; // m_TempCellToLoadList.size get incremented in TryLoadCell
                        TryLoadCell(cellInfo, ref shChunkBudget, ref indexChunkBudget, m_TempCellToLoadList);
                    }
                }

                // Remove the cells we successfully loaded.
                m_ToBeLoadedCells.RemoveRange(0, m_TempCellToLoadList.size);
                m_LoadedCells.AddRange(m_TempCellToLoadList);
                m_ToBeLoadedCells.AddRange(m_TempCellToUnloadList);
                m_TempCellToLoadList.Clear();
                m_TempCellToUnloadList.Clear();
            }

            // Handle cell streaming for blending
            if (enableScenarioBlending)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(CoreProfileId.APVScenarioBlendingUpdate)))
                    UpdateBlendingCellStreaming(cmd);
            }
        }

        int FindWorstBlendingCellToBeLoaded()
        {
            int idx = -1;
            float worstBlending = -1;
            float factor = scenarioBlendingFactor;
            for (int i = m_TempBlendingCellToLoadList.size; i < m_ToBeLoadedBlendingCells.size; ++i)
            {
                float score = Mathf.Abs(m_ToBeLoadedBlendingCells[i].blendingFactor - factor);
                if (score > worstBlending)
                {
                    idx = i;
                    if (m_ToBeLoadedBlendingCells[i].ShouldReupload()) // We are not gonna find worse than that
                        break;
                    worstBlending = score;
                }
            }
            return idx;
        }

        void UpdateBlendingCellStreaming(CommandBuffer cmd)
        {
            if (!m_HasRemainingCellsToBlend)
                return;

            // Compute the worst score to offset score of cells to prioritize
            float worstLoaded = m_LoadedCells.size != 0 ? m_LoadedCells[m_LoadedCells.size - 1].streamingScore : 0.0f;
            float worstToBeLoaded = m_ToBeLoadedCells.size != 0 ? m_ToBeLoadedCells[m_ToBeLoadedCells.size - 1].streamingScore : 0.0f;
            float worstScore = Mathf.Max(worstLoaded, worstToBeLoaded);

            ComputeStreamingScoreForBlending(m_ToBeLoadedBlendingCells, worstScore);
            ComputeStreamingScoreForBlending(m_LoadedBlendingCells, worstScore);

            m_ToBeLoadedBlendingCells.QuickSort();
            m_LoadedBlendingCells.QuickSort();

            int cellCountToLoad = Mathf.Min(m_NumberOfCellsLoadedPerFrame, m_ToBeLoadedBlendingCells.size);
            while (m_TempBlendingCellToLoadList.size < cellCountToLoad)
            {
                var blendingCell = m_ToBeLoadedBlendingCells[m_TempBlendingCellToLoadList.size];
                if (!TryLoadBlendingCell(blendingCell, m_TempBlendingCellToLoadList))
                    break;
            }

            // Budget reached
            if (m_TempBlendingCellToLoadList.size != cellCountToLoad)
            {
                // Turnover allows a percentage of the pool to be replaced by cells with a lower streaming score
                // once the system is in a stable state. This ensures all cells get updated regularly.
                int turnoverOffset = -1;
                int idx = (int)(m_LoadedBlendingCells.size * (1.0f - turnoverRate));
                var worstNoTurnover = idx < m_LoadedBlendingCells.size ? m_LoadedBlendingCells[idx] : null;

                while (m_TempBlendingCellToLoadList.size < cellCountToLoad)
                {
                    if (m_LoadedBlendingCells.size - m_TempBlendingCellToUnloadList.size == 0) // We unloaded everything
                        break;

                    var worstCellLoaded = m_LoadedBlendingCells[m_LoadedBlendingCells.size - m_TempBlendingCellToUnloadList.size - 1];
                    var bestCellToBeLoaded = m_ToBeLoadedBlendingCells[m_TempBlendingCellToLoadList.size];

                    if (bestCellToBeLoaded.streamingScore >= (worstNoTurnover ?? worstCellLoaded).streamingScore) // We are in a "stable" state
                    {
                        if (worstNoTurnover == null) // Disable turnover
                            break;

                        // Find worst cell and assume contiguous cells have roughly the same blending factor
                        // (contiguous cells are spatially close by, so it's good anyway to update them together)
                        if (turnoverOffset == -1)
                            turnoverOffset = FindWorstBlendingCellToBeLoaded();

                        bestCellToBeLoaded = m_ToBeLoadedBlendingCells[turnoverOffset];
                        if (bestCellToBeLoaded.IsUpToDate()) // Every single cell is blended :)
                            break;
                    }

                    UnloadBlendingCell(worstCellLoaded, m_TempBlendingCellToUnloadList);
                    // Loading can still fail cause all cells don't have the same chunk count
                    if (TryLoadBlendingCell(bestCellToBeLoaded, m_TempBlendingCellToLoadList) && turnoverOffset != -1)
                    {
                        // swap to ensure loaded cells are at the start of m_ToBeLoadedBlendingCells
                        m_ToBeLoadedBlendingCells[turnoverOffset] = m_ToBeLoadedBlendingCells[m_TempBlendingCellToLoadList.size-1];
                        m_ToBeLoadedBlendingCells[m_TempBlendingCellToLoadList.size-1] = bestCellToBeLoaded;
                        if (++turnoverOffset >= m_ToBeLoadedBlendingCells.size)
                            turnoverOffset = m_TempBlendingCellToLoadList.size;
                    }
                }

                m_LoadedBlendingCells.RemoveRange(m_LoadedBlendingCells.size - m_TempBlendingCellToUnloadList.size, m_TempBlendingCellToUnloadList.size);
            }

            m_ToBeLoadedBlendingCells.RemoveRange(0, m_TempBlendingCellToLoadList.size);
            m_LoadedBlendingCells.AddRange(m_TempBlendingCellToLoadList);
            m_TempBlendingCellToLoadList.Clear();
            m_ToBeLoadedBlendingCells.AddRange(m_TempBlendingCellToUnloadList);
            m_TempBlendingCellToUnloadList.Clear();

            if (m_LoadedBlendingCells.size != 0)
            {
                float factor = scenarioBlendingFactor;
                int cellCountToBlend = Mathf.Min(numberOfCellsBlendedPerFrame, m_LoadedBlendingCells.size);

                for (int i = 0; i < cellCountToBlend; ++i)
                {
                    m_LoadedBlendingCells[i].blendingFactor = factor;
                    m_BlendingPool.BlendChunks(m_LoadedBlendingCells[i], m_Pool);
                }

                m_BlendingPool.PerformBlending(cmd, factor, m_Pool);
            }

            if (m_ToBeLoadedBlendingCells.size == 0)
                m_HasRemainingCellsToBlend = false;
        }
    }
}
