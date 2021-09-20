using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    public partial class ProbeReferenceVolume
    {
        DynamicArray<CellInfo> m_LoadedCells = new DynamicArray<CellInfo>();
        DynamicArray<CellInfo> m_UnloadedCells = new DynamicArray<CellInfo>();
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

        public void UpdateCellStreaming(Camera camera)
        {
            var cameraPosition = camera.transform.position;
            if (!debugDisplay.freezeStreaming)
            {
                m_FrozenCameraPosition = cameraPosition;
            }

            // Cell position in cell space is the top left corner. So we need to shift the camera position by half a cell to make things comparable.
            var cameraPositionCellSpace = (m_FrozenCameraPosition - m_Transform.posWS) / MaxBrickSize() - Vector3.one * 0.5f;

            ComputeCellCameraDistance(cameraPositionCellSpace, m_UnloadedCells);
            ComputeCellCameraDistance(cameraPositionCellSpace, m_LoadedCells);

            m_UnloadedCells.QuickSort();
            m_LoadedCells.QuickSort();

            bool budgetReached = false;
            int pendingLoadCount = 0;

            // This is only a rough budget estimate at first.
            // It doesn't account for fragmentation.
            int indexChunkBudget = m_Index.GetRemainingChunkCount();
            int shChunkBudget = m_Pool.GetRemainingChunkCount();

            while (pendingLoadCount < m_NumberOfCellsLoadedPerFrame && pendingLoadCount < m_UnloadedCells.size && !budgetReached)
            {
                // Enough memory, we can safely load the cell.
                var cellInfo = m_UnloadedCells[pendingLoadCount];
                if (cellInfo.cell.shChunkCount <= shChunkBudget && cellInfo.cell.indexChunkCount <= indexChunkBudget)
                {
                    // Do the actual upload to GPU memory and update indirection buffer.
                    // This can fail if there are not enough consecutive chunks for the cell.
                    // TODO: Defrag?
                    if (LoadCell(cellInfo))
                    {
                        m_TempCellToLoadList.Add(cellInfo);

                        shChunkBudget -= cellInfo.cell.shChunkCount;
                        indexChunkBudget -= cellInfo.cell.indexChunkCount;
                        pendingLoadCount++;
                    }
                    else
                    {
                        budgetReached = true;
                    }
                }
                else
                {
                    budgetReached = true;
                }
            }

            // Budget reached. We need to figure out if we can safely unload other cells to make room.
            if (budgetReached)
            {
                int pendingUnloadCount = 0;
                bool canUnloadCell = true;
                while (canUnloadCell && pendingLoadCount < m_NumberOfCellsLoadedPerFrame && pendingLoadCount < m_UnloadedCells.size)
                {
                    if (m_LoadedCells.size - pendingUnloadCount == 0)
                    {
                        canUnloadCell = false;
                        break;
                    }

                    var furthestLoadedCell = m_LoadedCells[m_LoadedCells.size - pendingUnloadCount - 1];
                    var closestUnloadedCell = m_UnloadedCells[pendingLoadCount];

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

                        // Are we within budget?
                        if (closestUnloadedCell.cell.shChunkCount <= shChunkBudget && closestUnloadedCell.cell.indexChunkCount <= indexChunkBudget)
                        {
                            if (LoadCell(closestUnloadedCell))
                            {
                                m_TempCellToLoadList.Add(closestUnloadedCell);

                                shChunkBudget -= closestUnloadedCell.cell.shChunkCount;
                                indexChunkBudget -= closestUnloadedCell.cell.indexChunkCount;
                                pendingLoadCount++;
                            }
                        }
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

            // Remove the cells we successfully loaded.
            m_UnloadedCells.RemoveRange(0, pendingLoadCount);
            m_LoadedCells.AddRange(m_TempCellToLoadList);
            m_UnloadedCells.AddRange(m_TempCellToUnloadList);
            m_TempCellToLoadList.Clear();
            m_TempCellToUnloadList.Clear();
        }

        internal int cellCount => cells.Count;

        internal void ToggleCellLoading(int cellIndex)
        {
            if (cells.TryGetValue(cellIndex, out var cell))
            {
                if (cell.loaded)
                {
                    UnloadCell(cell);
                }
                else
                {
                    LoadCell(cell);
                }
            }

        }
    }
}
