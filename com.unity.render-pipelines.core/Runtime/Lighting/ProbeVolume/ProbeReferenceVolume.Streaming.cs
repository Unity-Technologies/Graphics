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
                    m_ToBeLoadedBlendingCells[i].blendingFactor = -1.0f;
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

        void ComputeStreamingScoreForBlending(DynamicArray<BlendingCellInfo> cells, bool areUploaded)
        {
            float factor = scenarioBlendingFactor;
            for (int i = 0; i < cells.size; ++i)
            {
                if (factor == cells[i].blendingFactor)
                {
                    // This cell is up to date - mark it as replaceable by any cell
                    cells[i].streamingScore = float.MaxValue;
                }
                else
                {
                    // TODO: consider lowering the priority for cells that stay in the buffer for a long time
                    // to leave room for other cells (only useful for very slow transitions)
                    cells[i].streamingScore = cells[i].cellInfo.streamingScore;
                    if (areUploaded)
                        cells[i].blendingFactor = factor;
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

        void UnloadBlendingCell(BlendingCellInfo blendingCell, ref int budget, DynamicArray<BlendingCellInfo> unloadedCells)
        {
            UnloadBlendingCell(blendingCell);

            unloadedCells.Add(blendingCell);
            budget += blendingCell.cellInfo.cell.shChunkCount;
        }

        bool TryLoadBlendingCell(BlendingCellInfo blendingCell, ref int budget, DynamicArray<BlendingCellInfo> loadedCells)
        {
            // Are we within budget?
            if (blendingCell.cellInfo.cell.shChunkCount > budget)
                return false;

            if (!AddBlendingBricks(blendingCell))
                return false;

            loadedCells.Add(blendingCell);
            budget -= blendingCell.chunkList.Count;

            return true;
        }

        /// <summary>
        /// Updates the cell streaming for a <see cref="Camera"/>
        /// </summary>
        /// <param name="camera">The <see cref="Camera"/></param>
        public void UpdateCellStreaming(Camera camera)
        {
            if (!isInitialized) return;

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

            if (m_SupportStreaming)
            {
                bool budgetReached = false;

                while (m_TempCellToLoadList.size < m_NumberOfCellsLoadedPerFrame && m_TempCellToLoadList.size < m_ToBeLoadedCells.size && !budgetReached)
                {
                    // Enough memory, we can safely load the cell.
                    var cellInfo = m_ToBeLoadedCells[m_TempCellToLoadList.size];
                    budgetReached = !TryLoadCell(cellInfo, ref shChunkBudget, ref indexChunkBudget, m_TempCellToLoadList);
                }

                // Budget reached. We need to figure out if we can safely unload other cells to make room.
                if (budgetReached)
                {
                    int pendingUnloadCount = 0;
                    bool canUnloadCell = true;
                    while (canUnloadCell && m_TempCellToLoadList.size < m_NumberOfCellsLoadedPerFrame && m_TempCellToLoadList.size < m_ToBeLoadedCells.size)
                    {
                        if (m_LoadedCells.size - pendingUnloadCount == 0)
                        {
                            canUnloadCell = false;
                            break;
                        }

                        var furthestLoadedCell = m_LoadedCells[m_LoadedCells.size - pendingUnloadCount - 1];
                        var closestUnloadedCell = m_ToBeLoadedCells[m_TempCellToLoadList.size];

                        // Redundant work. Maybe store during first sort pass?
                        float furthestLoadedCellDistance = Vector3.Distance(furthestLoadedCell.cell.position, cameraPositionCellSpace);
                        float closestUnloadedCellDistance = Vector3.Distance(closestUnloadedCell.cell.position, cameraPositionCellSpace);

                        // The most distant loaded cell is further than the closest unloaded cell, we can unload it.
                        if (furthestLoadedCellDistance > closestUnloadedCellDistance)
                        {
                            pendingUnloadCount++;
                            UnloadCell(furthestLoadedCell);
                            shChunkBudget += furthestLoadedCell.cell.shChunkCount;
                            indexChunkBudget += furthestLoadedCell.cell.indexChunkCount;

                            m_TempCellToUnloadList.Add(furthestLoadedCell);

                            TryLoadCell(closestUnloadedCell, ref shChunkBudget, ref indexChunkBudget, m_TempCellToLoadList);
                        }
                        else
                        {
                            // We are in a "stable" state, all the closest cells are loaded within the budget.
                            canUnloadCell = false;
                        }
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
                int cellCountToLoad = Mathf.Min(m_NumberOfCellsLoadedPerFrame, m_ToBeLoadedCells.size);
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

            // Handle cell streaming for blending
            if (ProbeBrickBlendingPool.isInitialized && UpdateBlendingCellStreaming())
                m_BlendingPool.PerformBlending(scenarioBlendingFactor, m_Pool);
        }

        bool UpdateBlendingCellStreaming()
        {
            // If all cells fit into blending pool
            if (m_ToBeLoadedBlendingCells.size == 0)
            {
                if (!m_HasRemainingCellsToBlend)
                    return false;

                // Trigger one last dispatch to make all cells up to date with blending factor
                // If blending factor changes later on, m_HasRemainingCellsToBlend will be
                // forced to true to resume process.
                for (int i = 0; i < m_LoadedBlendingCells.size; ++i)
                    m_LoadedBlendingCells[i].blendingFactor = scenarioBlendingFactor;
                m_HasRemainingCellsToBlend = false;
                return true;
            }

            ComputeStreamingScoreForBlending(m_ToBeLoadedBlendingCells, false);
            ComputeStreamingScoreForBlending(m_LoadedBlendingCells, true);

            m_ToBeLoadedBlendingCells.QuickSort();
            m_LoadedBlendingCells.QuickSort();

            int budget = m_BlendingPool.GetRemainingChunkCount();
            int numberOfCellsToLoad = Mathf.Min(m_ToBeLoadedBlendingCells.size, m_NumberOfCellsLoadedPerFrame);
            while (m_TempBlendingCellToLoadList.size < numberOfCellsToLoad)
            {
                var blendingCell = m_ToBeLoadedBlendingCells[m_TempBlendingCellToLoadList.size];
                if (!TryLoadBlendingCell(blendingCell, ref budget, m_TempBlendingCellToLoadList))
                    break;
            }

            // Budget reached
            if (m_TempBlendingCellToLoadList.size != numberOfCellsToLoad)
            {
                while (m_TempBlendingCellToLoadList.size < numberOfCellsToLoad)
                {
                    if (m_LoadedBlendingCells.size - m_TempBlendingCellToUnloadList.size == 0) // We unloaded everything
                        break;

                    var worstCellLoaded = m_LoadedBlendingCells[m_LoadedBlendingCells.size - m_TempBlendingCellToUnloadList.size - 1];
                    var bestCellToBeLoaded = m_ToBeLoadedBlendingCells[m_TempBlendingCellToLoadList.size];

                    if (bestCellToBeLoaded.streamingScore >= worstCellLoaded.streamingScore) // We are in a "stable" state
                        break;

                    UnloadBlendingCell(worstCellLoaded, ref budget, m_TempBlendingCellToUnloadList);
                    TryLoadBlendingCell(bestCellToBeLoaded, ref budget, m_TempBlendingCellToLoadList);
                }

                m_LoadedBlendingCells.RemoveRange(m_LoadedBlendingCells.size - m_TempBlendingCellToUnloadList.size, m_TempBlendingCellToUnloadList.size);
            }

            // Register newly uploaded cells for blending
            for (int i = 0; i < m_TempBlendingCellToLoadList.size; i++)
            {
                var blendingCell = m_TempBlendingCellToLoadList[i];
                Debug.Assert(blendingCell.blending);
                Debug.Assert(blendingCell.cellInfo.loaded);
                Debug.Assert(blendingCell.chunkList.Count <= blendingCell.cellInfo.chunkList.Count);

                for (int c = 0; c < blendingCell.chunkList.Count; c++)
                {
                    int dstIndex = blendingCell.cellInfo.chunkList[c].flattenIndex(m_Pool.GetPoolWidth(), m_Pool.GetPoolHeight());
                    m_BlendingPool.MapChunk(blendingCell.chunkList[c], dstIndex);
                }
            }

            m_ToBeLoadedBlendingCells.RemoveRange(0, m_TempBlendingCellToLoadList.size);
            m_LoadedBlendingCells.AddRange(m_TempBlendingCellToLoadList);
            m_TempBlendingCellToLoadList.Clear();
            m_ToBeLoadedBlendingCells.AddRange(m_TempBlendingCellToUnloadList);
            m_TempBlendingCellToUnloadList.Clear();

            m_HasRemainingCellsToBlend = true;
            return true;
        }
    }
}
