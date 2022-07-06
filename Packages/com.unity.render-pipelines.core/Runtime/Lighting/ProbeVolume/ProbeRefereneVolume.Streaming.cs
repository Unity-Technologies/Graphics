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

        Vector3 m_FrozenCameraPosition;

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

        /// <summary>
        /// Updates the cell streaming for a <see cref="Camera"/>
        /// </summary>
        /// <param name="camera">The camera to be updated</param>
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
                        m_LoadedCells.RemoveRange(m_LoadedCells.size - pendingUnloadCount, pendingUnloadCount);
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
        }
    }
}
