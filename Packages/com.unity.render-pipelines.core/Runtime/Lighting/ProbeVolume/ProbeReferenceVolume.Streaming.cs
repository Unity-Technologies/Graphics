namespace UnityEngine.Rendering
{
    public partial class ProbeReferenceVolume
    {
#if UNITY_EDITOR
        // By default on editor we load a lot of cells in one go to avoid having to mess with scene view
        // to see results, this value can still be changed via API.
        bool m_LoadMaxCellsPerFrame = true;
#else
        bool m_LoadMaxCellsPerFrame = false;
#endif

        /// <summary>
        /// Enable streaming as many cells per frame as possible.
        /// </summary>
        /// <param name="value">True to enable streaming as many cells per frame as possible.</param>
        public void EnableMaxCellStreaming(bool value)
        {
            m_LoadMaxCellsPerFrame = value;
        }

        const int kMaxCellLoadedPerFrame = 10;
        int m_NumberOfCellsLoadedPerFrame = 1;

        /// <summary>
        /// Set the number of cells that are loaded per frame when needed. This number is capped at 10.
        /// </summary>
        /// <param name="numberOfCells">Number of cells to be loaded per frame.</param>
        public void SetNumberOfCellsLoadedPerFrame(int numberOfCells)
        {
            m_NumberOfCellsLoadedPerFrame = Mathf.Min(kMaxCellLoadedPerFrame, Mathf.Max(1, numberOfCells));
        }

        /// <summary>Set to true to stream as many cells as possible every frame.</summary>
        public bool loadMaxCellsPerFrame
        {
            get => m_LoadMaxCellsPerFrame;
            set => m_LoadMaxCellsPerFrame = value;
        }

        int numberOfCellsLoadedPerFrame => m_LoadMaxCellsPerFrame ? cells.Count : m_NumberOfCellsLoadedPerFrame;

        int m_NumberOfCellsBlendedPerFrame = 10000;
        /// <summary>Maximum number of cells that are blended per frame.</summary>
        public int numberOfCellsBlendedPerFrame
        {
            get => m_NumberOfCellsBlendedPerFrame;
            set => m_NumberOfCellsBlendedPerFrame = Mathf.Max(1, value);
        }

        float m_TurnoverRate = 0.1f;
        /// <summary>Percentage of cells loaded in the blending pool that can be replaced by out of date cells.</summary>
        public float turnoverRate
        {
            get => m_TurnoverRate;
            set => m_TurnoverRate = Mathf.Clamp01(value);
        }

        DynamicArray<Cell> m_LoadedCells = new(); // List of currently loaded cells.
        DynamicArray<Cell> m_ToBeLoadedCells = new(); // List of currently unloaded cells.
        DynamicArray<Cell> m_WorseLoadedCells = new(); // Reduced list (N cells are processed per frame) of worse loaded cells.
        DynamicArray<Cell> m_BestToBeLoadedCells = new(); // Reduced list (N cells are processed per frame) of best unloaded cells.
        DynamicArray<Cell> m_TempCellToLoadList = new(); // Temp list of cells loaded during this frame.
        DynamicArray<Cell> m_TempCellToUnloadList = new(); // Temp list of cells unloaded during this frame.

        DynamicArray<Cell> m_LoadedBlendingCells = new();
        DynamicArray<Cell> m_ToBeLoadedBlendingCells = new();
        DynamicArray<Cell> m_TempBlendingCellToLoadList = new();
        DynamicArray<Cell> m_TempBlendingCellToUnloadList = new();

        Vector3 m_FrozenCameraPosition;
        Vector3 m_FrozenCameraDirection;

        const float kIndexFragmentationThreshold = 0.2f;
        bool m_IndexDefragmentationInProgress;
        ProbeBrickIndex m_DefragIndex;
        ProbeGlobalIndirection m_DefragCellIndices;
        DynamicArray<Cell> m_IndexDefragCells = new DynamicArray<Cell>();

        internal float minStreamingScore;
        internal float maxStreamingScore;

        bool m_HasRemainingCellsToBlend = false;

        internal void ScenarioBlendingChanged(bool scenarioChanged)
        {
            m_HasRemainingCellsToBlend = true;
            if (scenarioChanged)
            {
                UnloadAllBlendingCells();
                for (int i = 0; i < m_ToBeLoadedBlendingCells.size; ++i)
                    m_ToBeLoadedBlendingCells[i].blendingInfo.ForceReupload();
            }
        }

        static void ComputeCellStreamingScore(Cell cell, Vector3 cameraPosition, Vector3 cameraDirection)
        {
            var cellPosition = cell.desc.position;
            var cameraToCell = (cellPosition - cameraPosition).normalized;
            cell.streamingScore = Vector3.Distance(cameraPosition, cell.desc.position);
            // This should give more weight to cells in front of the camera.
            cell.streamingScore *= (2.0f - Vector3.Dot(cameraDirection, cameraToCell));
        }

        void ComputeStreamingScore(Vector3 cameraPosition, Vector3 cameraDirection, DynamicArray<Cell> cells)
        {
            for (int i = 0; i < cells.size; ++i)
            {
                ComputeCellStreamingScore(cells[i], cameraPosition, cameraDirection);
            }
        }

        void ComputeBestToBeLoadedCells(Vector3 cameraPosition, Vector3 cameraDirection)
        {
            m_BestToBeLoadedCells.Clear();
            m_BestToBeLoadedCells.Reserve(m_ToBeLoadedCells.size); // Pre-reserve to avoid Insert allocating every time.

            foreach (var cell in m_ToBeLoadedCells)
            {
                ComputeCellStreamingScore(cell, cameraPosition, cameraDirection);

                // We need to compute min/max streaming scores here since we don't have the full sorted list anymore (which is used in ComputeMinMaxStreamingScore)
                minStreamingScore = Mathf.Min(minStreamingScore, cell.streamingScore);
                maxStreamingScore = Mathf.Max(maxStreamingScore, cell.streamingScore);

                int currentBestCellsSize = System.Math.Min(m_BestToBeLoadedCells.size, numberOfCellsLoadedPerFrame);
                int index;
                for (index = 0; index < currentBestCellsSize; ++index)
                {
                    if (cell.streamingScore < m_BestToBeLoadedCells[index].streamingScore)
                        break;
                }

                if (index < numberOfCellsLoadedPerFrame)
                    m_BestToBeLoadedCells.Insert(index, cell);

                // Avoids too many copies when Inserting new elements.
                if (m_BestToBeLoadedCells.size > numberOfCellsLoadedPerFrame)
                    m_BestToBeLoadedCells.Resize(numberOfCellsLoadedPerFrame);
            }
        }

        void ComputeWorseLoadedCells(Vector3 cameraPosition, Vector3 cameraDirection)
        {
            m_WorseLoadedCells.Clear();
            m_WorseLoadedCells.Reserve(m_LoadedCells.size); // Pre-reserve to avoid Insert allocating every time.

            int requiredSHChunks = 0;
            int requiredIndexChunks = 0;
            foreach(var cell in m_BestToBeLoadedCells)
            {
                requiredSHChunks += cell.desc.shChunkCount;
                requiredIndexChunks += cell.desc.indexChunkCount;
            }

            foreach (var cell in m_LoadedCells) 
            {
                ComputeCellStreamingScore(cell, cameraPosition, cameraDirection);

                // We need to compute min/max streaming scores here since we don't have the full sorted list anymore (which is used in ComputeMinMaxStreamingScore)
                minStreamingScore = Mathf.Min(minStreamingScore, cell.streamingScore);
                maxStreamingScore = Mathf.Max(maxStreamingScore, cell.streamingScore);

                int currentWorseSize = m_WorseLoadedCells.size;
                int index;
                for (index = 0; index < currentWorseSize; ++index)
                {
                    if (cell.streamingScore > m_WorseLoadedCells[index].streamingScore)
                        break;
                }

                m_WorseLoadedCells.Insert(index, cell);

                // Compute the chunk counts of the current worse cells.
                int currentSHChunks = 0;
                int currentIndexChunks = 0;
                int newSize = 0;
                for (int i = 0; i < m_WorseLoadedCells.size; ++i)
                {
                    var worseCell = m_WorseLoadedCells[i];
                    currentSHChunks += worseCell.desc.shChunkCount;
                    currentIndexChunks += worseCell.desc.indexChunkCount;

                    if (currentSHChunks >= requiredSHChunks && currentIndexChunks >= requiredIndexChunks)
                    {
                        newSize = i + 1;
                        break;
                    }
                }

                // Now we resize to keep just enough worse cells that represent enough room to load the required cell.
                // This allows insertions to be cheaper.
                if (newSize != 0)
                    m_WorseLoadedCells.Resize(newSize);
            }
        }

        void ComputeBlendingScore(DynamicArray<Cell> cells, float worstScore)
        {
            float factor = scenarioBlendingFactor;
            for (int i = 0; i < cells.size; ++i)
            {
                var cell = cells[i];
                var blendingInfo = cell.blendingInfo;
                if (factor == blendingInfo.blendingFactor)
                    blendingInfo.MarkUpToDate();
                else
                {
                    blendingInfo.blendingScore = cell.streamingScore;
                    if (blendingInfo.ShouldPrioritize())
                        blendingInfo.blendingScore -= worstScore;
                }
            }
        }

        bool TryLoadCell(Cell cell, ref int shBudget, ref int indexBudget, DynamicArray<Cell> loadedCells)
        {
            // Are we within budget?
            if (cell.poolInfo.shChunkCount <= shBudget && cell.indexInfo.indexChunkCount <= indexBudget)
            {
                // This can still fail because of fragmentation.
                if (LoadCell(cell, ignoreErrorLog: true))
                {
                    loadedCells.Add(cell);

                    shBudget -= cell.poolInfo.shChunkCount;
                    indexBudget -= cell.indexInfo.indexChunkCount;
                    return true;
                }
            }
            return false;
        }

        void UnloadBlendingCell(Cell cell, DynamicArray<Cell> unloadedCells)
        {
            UnloadBlendingCell(cell.blendingInfo);

            unloadedCells.Add(cell);
        }

        bool TryLoadBlendingCell(Cell cell, DynamicArray<Cell> loadedCells)
        {
            if (!cell.UpdateCellScenarioData(lightingScenario, m_CurrentBakingSet.otherScenario))
                return false;

            if (!AddBlendingBricks(cell))
                return false;

            loadedCells.Add(cell);

            return true;
        }

        void ComputeMinMaxStreamingScore()
        {
            minStreamingScore = float.MaxValue;
            maxStreamingScore = float.MinValue;

            if (m_ToBeLoadedCells.size != 0)
            {
                minStreamingScore = Mathf.Min(minStreamingScore, m_ToBeLoadedCells[0].streamingScore);
                maxStreamingScore = Mathf.Max(maxStreamingScore, m_ToBeLoadedCells[m_ToBeLoadedCells.size - 1].streamingScore);
            }

            if (m_LoadedCells.size != 0)
            {
                minStreamingScore = Mathf.Min(minStreamingScore, m_LoadedCells[0].streamingScore);
                maxStreamingScore = Mathf.Max(maxStreamingScore, m_LoadedCells[m_LoadedCells.size - 1].streamingScore);
            }
        }

        /// <summary>
        /// Updates the cell streaming for a <see cref="Camera"/>
        /// </summary>
        /// <param name="cmd">The <see cref="CommandBuffer"/></param>
        /// <param name="camera">The <see cref="Camera"/></param>
        public void UpdateCellStreaming(CommandBuffer cmd, Camera camera)
        {
            if (!isInitialized) return;

            using (new ProfilingScope(ProfilingSampler.Get(CoreProfileId.APVCellStreamingUpdate)))
            {
                var cameraPosition = camera.transform.position;
                if (!probeVolumeDebug.freezeStreaming)
                {
                    m_FrozenCameraPosition = cameraPosition;
                    m_FrozenCameraDirection = camera.transform.forward;
                }

                // Cell position in cell space is the top left corner. So we need to shift the camera position by half a cell to make things comparable.
                var cameraPositionCellSpace = (m_FrozenCameraPosition - m_Transform.posWS) / MaxBrickSize() - Vector3.one * 0.5f;

                DynamicArray<Cell> bestUnloadedCells;
                DynamicArray<Cell> worseLoadedCells;

                // When in this mode, we just sort through all loaded/ToBeLoaded cells in order to figure out worse/best cells to process.
                // This is slow so only recommended in the editor.
                if (m_LoadMaxCellsPerFrame)
                {
                    ComputeStreamingScore(cameraPositionCellSpace, m_FrozenCameraDirection, m_ToBeLoadedCells);
                    m_ToBeLoadedCells.QuickSort();
                    bestUnloadedCells = m_ToBeLoadedCells;
                }
                // Otherwise, when we only process a handful of cells per frame, we'll linearly go through the lists to determine two things:
                // - The list of best cells to load.
                // - The list of worse cells to load. This list can be bigger than the previous one since cells have different sizes so we may need to evict more to make room.
                // This allows us to not sort through all the cells every frame which is very slow. Instead we just output very small lists that we then process.
                else
                {
                    minStreamingScore = float.MaxValue;
                    maxStreamingScore = float.MinValue;

                    ComputeBestToBeLoadedCells(cameraPositionCellSpace, m_FrozenCameraDirection);
                    bestUnloadedCells = m_BestToBeLoadedCells;
                }

                // This is only a rough budget estimate at first.
                // It doesn't account for fragmentation.
                int indexChunkBudget = m_Index.GetRemainingChunkCount();
                int shChunkBudget = m_Pool.GetRemainingChunkCount();
                int cellCountToLoad = Mathf.Min(numberOfCellsLoadedPerFrame, bestUnloadedCells.size);

                if (m_SupportGPUStreaming)
                {
                    if (m_IndexDefragmentationInProgress)
                    {
                        UpdateIndexDefragmentation();
                    }
                    else
                    {
                        bool needComputeFragmentation = false;

                        while (m_TempCellToLoadList.size < cellCountToLoad)
                        {
                            // Enough memory, we can safely load the cell.
                            var cellInfo = bestUnloadedCells[m_TempCellToLoadList.size];
                            if (!TryLoadCell(cellInfo, ref shChunkBudget, ref indexChunkBudget, m_TempCellToLoadList))
                                break;
                        }

                        // Budget reached. We need to figure out if we can safely unload other cells to make room.
                        // If defrag was triggered by TryLoadCell we should not try to load further cells either.
                        if (m_TempCellToLoadList.size != cellCountToLoad && !m_IndexDefragmentationInProgress)
                        {
                            // We need to unload cells so we have to compute the worse loaded cells now (not earlier as it would be useless)
                            if (m_LoadMaxCellsPerFrame)
                            {
                                ComputeStreamingScore(cameraPositionCellSpace, m_FrozenCameraDirection, m_LoadedCells);
                                m_LoadedCells.QuickSort();
                                worseLoadedCells = m_LoadedCells;
                            }
                            else
                            {
                                ComputeWorseLoadedCells(cameraPositionCellSpace, m_FrozenCameraDirection);
                                worseLoadedCells = m_WorseLoadedCells;
                            }

                            int pendingUnloadCount = 0;
                            while (m_TempCellToLoadList.size < cellCountToLoad)
                            {
                                // No more cells to unload.
                                if (worseLoadedCells.size - pendingUnloadCount == 0)
                                    break;

                                // List are stored in reverse order depending on the mode.
                                // TODO make the full List be sorted the same way as partial list.
                                int worseCellIndex = m_LoadMaxCellsPerFrame ? worseLoadedCells.size - pendingUnloadCount - 1 : pendingUnloadCount;
                                var worseLoadedCell = worseLoadedCells[worseCellIndex];
                                var bestUnloadedCell = bestUnloadedCells[m_TempCellToLoadList.size];

                                // We are in a "stable" state, all the closest cells are loaded within the budget.
                                if (worseLoadedCell.streamingScore <= bestUnloadedCell.streamingScore)
                                    break;

                                // The worse loaded cell is further than the best unloaded cell, we can unload it.
                                while (pendingUnloadCount < worseLoadedCells.size && worseLoadedCell.streamingScore > bestUnloadedCell.streamingScore && (shChunkBudget < bestUnloadedCell.desc.shChunkCount || indexChunkBudget < bestUnloadedCell.desc.indexChunkCount))
                                {
                                    pendingUnloadCount++;
                                    UnloadCell(worseLoadedCell);
                                    shChunkBudget += worseLoadedCell.desc.shChunkCount;
                                    indexChunkBudget += worseLoadedCell.desc.indexChunkCount;

                                    m_TempCellToUnloadList.Add(worseLoadedCell);

                                    worseCellIndex = m_LoadMaxCellsPerFrame ? worseLoadedCells.size - pendingUnloadCount - 1 : pendingUnloadCount;
                                    if (pendingUnloadCount < worseLoadedCells.size)
                                        worseLoadedCell = worseLoadedCells[worseCellIndex];
                                }

                                // We unloaded enough space (not taking fragmentation into account)
                                if (shChunkBudget >= bestUnloadedCell.desc.shChunkCount && indexChunkBudget >= bestUnloadedCell.desc.indexChunkCount)
                                {
                                    if (!TryLoadCell(bestUnloadedCell, ref shChunkBudget, ref indexChunkBudget, m_TempCellToLoadList))
                                    {
                                        needComputeFragmentation = true;
                                        break; // Alloc failed because of fragmentation, stop trying to load cells.
                                    }
                                }
                            }
                        }

                        if (needComputeFragmentation)
                            m_Index.ComputeFragmentationRate();

                        if (m_Index.fragmentationRate >= kIndexFragmentationThreshold)
                            StartIndexDefragmentation();
                    }
                }
                else
                {
                    for (int i = 0; i < cellCountToLoad; ++i)
                    {
                        var cellInfo = m_ToBeLoadedCells[m_TempCellToLoadList.size]; // m_TempCellToLoadList.size get incremented in TryLoadCell
                        if (!TryLoadCell(cellInfo, ref shChunkBudget, ref indexChunkBudget, m_TempCellToLoadList))
                            break;
                    }
                }

                if (m_LoadMaxCellsPerFrame)
                    ComputeMinMaxStreamingScore();

                // Update internal load/toBeLoaded lists.

                // Move the successfully loaded cells to the "loaded cells" list.
                foreach (var cell in m_TempCellToLoadList)
                    m_ToBeLoadedCells.Remove(cell);
                m_LoadedCells.AddRange(m_TempCellToLoadList);
                // Move the unloaded cells to the list of cells to be loaded.
                if (m_TempCellToUnloadList.size > 0)
                {
                    foreach (var cell in m_TempCellToUnloadList)
                        m_LoadedCells.Remove(cell);

                    RecomputeMinMaxLoadedCellPos();
                }
                m_ToBeLoadedCells.AddRange(m_TempCellToUnloadList);
                // Clear temp lists.
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
                float score = Mathf.Abs(m_ToBeLoadedBlendingCells[i].blendingInfo.blendingFactor - factor);
                if (score > worstBlending)
                {
                    idx = i;
                    if (m_ToBeLoadedBlendingCells[i].blendingInfo.ShouldReupload()) // We are not gonna find worse than that
                        break;
                    worstBlending = score;
                }
            }
            return idx;
        }

        static int BlendingComparer(Cell a, Cell b)
        {
            if (a.blendingInfo.blendingScore < b.blendingInfo.blendingScore)
                return -1;
            else if (a.blendingInfo.blendingScore > b.blendingInfo.blendingScore)
                return 1;
            else
                return 0;
        }

        void UpdateBlendingCellStreaming(CommandBuffer cmd)
        {
            if (!m_HasRemainingCellsToBlend)
                return;

            // Compute the worst score to offset score of cells to prioritize
            float worstLoaded = m_LoadedCells.size != 0 ? m_LoadedCells[m_LoadedCells.size - 1].streamingScore : 0.0f;
            float worstToBeLoaded = m_ToBeLoadedCells.size != 0 ? m_ToBeLoadedCells[m_ToBeLoadedCells.size - 1].streamingScore : 0.0f;
            float worstScore = Mathf.Max(worstLoaded, worstToBeLoaded);

            ComputeBlendingScore(m_ToBeLoadedBlendingCells, worstScore);
            ComputeBlendingScore(m_LoadedBlendingCells, worstScore);

            m_ToBeLoadedBlendingCells.QuickSort(BlendingComparer);
            m_LoadedBlendingCells.QuickSort(BlendingComparer);

            int cellCountToLoad = Mathf.Min(numberOfCellsLoadedPerFrame, m_ToBeLoadedBlendingCells.size);
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

                    if (bestCellToBeLoaded.blendingInfo.blendingScore >= (worstNoTurnover ?? worstCellLoaded).blendingInfo.blendingScore) // We are in a "stable" state
                    {
                        if (worstNoTurnover == null) // Disable turnover
                            break;

                        // Find worst cell and assume contiguous cells have roughly the same blending factor
                        // (contiguous cells are spatially close by, so it's good anyway to update them together)
                        if (turnoverOffset == -1)
                            turnoverOffset = FindWorstBlendingCellToBeLoaded();

                        bestCellToBeLoaded = m_ToBeLoadedBlendingCells[turnoverOffset];
                        if (bestCellToBeLoaded.blendingInfo.IsUpToDate()) // Every single cell is blended :)
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
                    m_LoadedBlendingCells[i].blendingInfo.blendingFactor = factor;
                    m_BlendingPool.BlendChunks(m_LoadedBlendingCells[i], m_Pool);
                }

                m_BlendingPool.PerformBlending(cmd, factor, m_Pool);
            }

            if (m_ToBeLoadedBlendingCells.size == 0)
                m_HasRemainingCellsToBlend = false;
        }

        static int DefragComparer(Cell a, Cell b)
        {
            if (a.indexInfo.updateInfo.GetNumberOfChunks() > b.indexInfo.updateInfo.GetNumberOfChunks())
                return 1;
            else if (a.indexInfo.updateInfo.GetNumberOfChunks() < b.indexInfo.updateInfo.GetNumberOfChunks())
                return -1;
            else return 0;
        }

        void StartIndexDefragmentation()
        {
            m_IndexDefragmentationInProgress = true;

            // Prepare the list of cells.
            // We want to relocate cells with more indices first.
            m_IndexDefragCells.Clear();
            m_IndexDefragCells.AddRange(m_LoadedCells);
            m_IndexDefragCells.QuickSort(DefragComparer);

            m_DefragIndex.Clear();
        }

        void UpdateIndexDefragmentation()
        {
            using (new ProfilingScope(ProfilingSampler.Get(CoreProfileId.APVIndexDefragUpdate)))
            {
                int numberOfCellsToProcess = Mathf.Min(m_IndexDefragCells.size, numberOfCellsLoadedPerFrame);
                for (int i = 0; i < numberOfCellsToProcess; ++i)
                {
                    var cell = m_IndexDefragCells[m_IndexDefragCells.size - i - 1];

                    m_DefragIndex.FindSlotsForEntries(ref cell.indexInfo.updateInfo.entriesInfo);
                    m_DefragIndex.ReserveChunks(cell.indexInfo.updateInfo.entriesInfo, false);

                    // Update index and indirection
                    m_DefragIndex.AddBricks(cell.indexInfo, cell.data.bricks, cell.poolInfo.chunkList, ProbeBrickPool.GetChunkSizeInBrickCount(), m_Pool.GetPoolWidth(), m_Pool.GetPoolHeight());
                    m_DefragCellIndices.UpdateCell(cell.indexInfo);
                }

                // Remove processed cells from the list.
                m_IndexDefragCells.Resize(m_IndexDefragCells.size - numberOfCellsToProcess);

                if (m_IndexDefragCells.size == 0)
                {
                    // Swap index buffers
                    var oldDefragIndex = m_DefragIndex;
                    m_DefragIndex = m_Index;
                    m_Index = oldDefragIndex;

                    var oldDefragCellIndices = m_DefragCellIndices;
                    m_DefragCellIndices = m_CellIndices;
                    m_CellIndices = oldDefragCellIndices;

                    // Resume streaming
                    m_IndexDefragmentationInProgress = false;
                }
            }
        }

        void PushDiskStreamingRequest(Cell cell)
        {

        }
    }
}
