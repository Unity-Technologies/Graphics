using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
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

        float m_TransitionTimeToLerpFactor;
        float m_BakingStateLerpFactor = 1.0f;
        bool hasRemainingCellsToBlend = false;
        internal float stateTransitionTime
        {
            set
            {
                m_TransitionTimeToLerpFactor = value > 0.0f ? 1.0f / value : 0.0f;
                m_BakingStateLerpFactor = value > 0.0f ? 0.0f : 1.0f;
                hasRemainingCellsToBlend = true;
                // Abort any blending operation in progress
                UnloadAllBlendingCells();
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

        bool ComputeStreamingScoreForBlending(DynamicArray<BlendingCellInfo> cells, bool areUploaded)
        {
            bool hasRemaining = !areUploaded && cells.size != 0;
            for (int i = 0; i < cells.size; ++i)
            {
                var cellInfo = cells[i].cellInfo;
                if (m_BakingStateLerpFactor >= 1.0f && cells[i].blendingFactor >= 1.0f) // Finished blending
                {
                    cells[i].streamingScore = int.MaxValue;
                    hasRemaining |= cells[i].blendingFactor < 1.0f;
                }
                else
                {
                    // TODO: consider lowering the priority for cells that stay in the buffer for a long time
                    // to leave room for other cells (only useful for very slow transitions)
                    cells[i].streamingScore = cellInfo.streamingScore;
                    if (areUploaded)
                        cells[i].blendingFactor = m_BakingStateLerpFactor;
                }
            }
            return m_BakingStateLerpFactor < 1.0f || hasRemaining;
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

        bool TryLoadBlendingCell(BlendingCellInfo blendingCell, ref int budget, DynamicArray<BlendingCellInfo> loadedCells)
        {
            // Are we within budget?
            if (blendingCell.cellInfo.cell.shChunkCount > budget)
                return false;

            if (!AddBlendingBricks(blendingCell))
                return false;

            m_TempBlendingCellToLoadList.Add(blendingCell);
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
            if (!debugDisplay.freezeStreaming)
            {
                m_FrozenCameraPosition = cameraPosition;
            }

            // Cell position in cell space is the top left corner. So we need to shift the camera position by half a cell to make things comparable.
            var cameraPositionCellSpace = (m_FrozenCameraPosition - m_Transform.posWS) / MaxBrickSize() - Vector3.one * 0.5f;

            // Compute lerping factor between states
            float newStateLerpFactor = m_BakingStateLerpFactor;
            if (m_BakingStateLerpFactor < 1.0f)
            {
                float deltaTime = Time.deltaTime;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    deltaTime = 0.033f;
#endif

                newStateLerpFactor = Mathf.Min(newStateLerpFactor + deltaTime * m_TransitionTimeToLerpFactor, 1.0f);
            }

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
            if (hasRemainingCellsToBlend)
            {
                //UnityEditorInternal.RenderDoc.BeginCaptureRenderDoc(UnityEditor.SceneView.lastActiveSceneView);
                m_BakingStateLerpFactor = newStateLerpFactor; // TODO: evaluate consequences of setting that after streaming
                UpdateBlendingCellStreaming();
                //UnityEditorInternal.RenderDoc.EndCaptureRenderDoc(UnityEditor.SceneView.lastActiveSceneView);
            }
        }

        void UpdateBlendingCellStreaming()
        {
            hasRemainingCellsToBlend = ComputeStreamingScoreForBlending(m_ToBeLoadedBlendingCells, false);
            hasRemainingCellsToBlend |= ComputeStreamingScoreForBlending(m_LoadedBlendingCells, true);

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
                int pendingUnloadCount = 0;
                while (m_TempBlendingCellToLoadList.size < numberOfCellsToLoad)
                {
                    if (m_LoadedBlendingCells.size - pendingUnloadCount == 0) // We unloaded everything
                        break;

                    var worstCellLoaded = m_LoadedBlendingCells[m_LoadedBlendingCells.size - pendingUnloadCount - 1];
                    var bestCellToBeLoaded = m_ToBeLoadedBlendingCells[m_TempBlendingCellToLoadList.size];

                    if (bestCellToBeLoaded.streamingScore < worstCellLoaded.streamingScore)
                    {
                        pendingUnloadCount++;
                        UnloadBlendingCell(worstCellLoaded);
                        budget += worstCellLoaded.cellInfo.cell.shChunkCount;
                        m_TempBlendingCellToUnloadList.Add(worstCellLoaded);

                        TryLoadBlendingCell(bestCellToBeLoaded, ref budget, m_TempBlendingCellToLoadList);
                    }
                    else // We are in a "stable" state, all the closest cells are loaded within the budget.
                        break;
                }

                if (pendingUnloadCount > 0)
                    m_LoadedBlendingCells.RemoveRange(m_LoadedBlendingCells.size - pendingUnloadCount, pendingUnloadCount);
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

            // Trigger blending compute shader
            m_BlendingPool.PerformBlending(m_BakingStateLerpFactor, m_Pool);
        }
    }
}
