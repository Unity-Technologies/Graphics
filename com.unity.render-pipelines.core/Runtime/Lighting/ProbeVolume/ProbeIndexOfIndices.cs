using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Chunk = UnityEngine.Experimental.Rendering.ProbeBrickPool.BrickChunkAlloc;
using RegId = UnityEngine.Experimental.Rendering.ProbeReferenceVolume.RegId;
using Cell = UnityEngine.Experimental.Rendering.ProbeReferenceVolume.Cell;

namespace UnityEngine.Experimental.Rendering
{
    internal class ProbeCellIndices
    {
        const int kUintPerEntry = 3;
        internal int estimatedVMemCost { get; private set; }

        internal struct IndexMetaData
        {
            internal Vector3Int minLocalIdx;
            internal Vector3Int maxLocalIdx;
            internal int firstChunkIndex;
            internal int minSubdiv;

            internal void Pack(out uint[] vals)
            {
                vals = new uint[kUintPerEntry];
                for (int i = 0; i < kUintPerEntry; ++i)
                {
                    vals[i] = 0;
                }

                //  Note this packing is really really generous, I really think we can get rid of 1 uint at least if we assume we don't go extreme.
                //  but this is encompassing all scenarios.
                //
                // UINT 0:
                //  FirstChunkIndex 29 bit
                //  MinSubdiv       3  bit
                // UINT 1:
                //  minLocalIdx.x   10 bit
                //  minLocalIdx.y   10 bit
                //  minLocalIdx.z   10 bit
                // UINT 2:
                //  maxLocalIdx.x   10 bit
                //  maxLocalIdx.y   10 bit
                //  maxLocalIdx.z   10 bit

                vals[0] = (uint)firstChunkIndex & 0x1FFFFFFF;
                vals[0] |= ((uint)minSubdiv & 0x7) << 29;

                vals[1] = (uint)minLocalIdx.x & 0x3FF;
                vals[1] |= ((uint)minLocalIdx.y & 0x3FF) << 10;
                vals[1] |= ((uint)minLocalIdx.z & 0x3FF) << 20;

                vals[2] = (uint)maxLocalIdx.x & 0x3FF;
                vals[2] |= ((uint)maxLocalIdx.y & 0x3FF) << 10;
                vals[2] |= ((uint)maxLocalIdx.z & 0x3FF) << 20;
            }
        }

        ComputeBuffer m_IndexOfIndicesBuffer;
        uint[] m_IndexOfIndicesData;

        Vector3Int m_CellCount;
        Vector3Int m_CellMin;
        int m_CellSizeInMinBricks;

        bool m_NeedUpdateComputeBuffer;

        internal Vector3Int GetCellIndexDimension() => m_CellCount;
        internal Vector3Int GetCellMinPosition() => m_CellMin;

        int GetFlatIndex(Vector3Int normalizedPos)
        {
            return normalizedPos.z * (m_CellCount.x * m_CellCount.y) + normalizedPos.y * m_CellCount.x + normalizedPos.x;
        }

        internal ProbeCellIndices(Vector3Int cellMin, Vector3Int cellMax, int cellSizeInMinBricks)
        {
            Vector3Int cellCount = new Vector3Int(Mathf.Abs(cellMax.x - cellMin.x), Mathf.Abs(cellMax.y - cellMin.y), Mathf.Abs(cellMax.z - cellMin.z));
            m_CellCount = cellCount;
            m_CellMin = cellMin;
            m_CellSizeInMinBricks = cellSizeInMinBricks;
            int flatCellCount = cellCount.x * cellCount.y * cellCount.z;
            flatCellCount = flatCellCount == 0 ? 1 : flatCellCount;
            int bufferSize = kUintPerEntry * flatCellCount;
            m_IndexOfIndicesBuffer = new ComputeBuffer(flatCellCount, kUintPerEntry * sizeof(uint));
            m_IndexOfIndicesData = new uint[bufferSize];
            m_NeedUpdateComputeBuffer = false;
            estimatedVMemCost = flatCellCount * kUintPerEntry * sizeof(uint);
        }

        internal int GetFlatIdxForCell(Vector3Int cellPosition)
        {
            Vector3Int normalizedPos = cellPosition - m_CellMin;
            Debug.Assert(normalizedPos.x >= 0 && normalizedPos.y >= 0 && normalizedPos.z >= 0);

            return GetFlatIndex(normalizedPos);
        }

        internal void AddCell(int cellFlatIdx, ProbeBrickIndex.CellIndexUpdateInfo cellUpdateInfo)
        {
            int minSubdivCellSize = ProbeReferenceVolume.CellSize(cellUpdateInfo.minSubdivInCell);
            IndexMetaData metaData = new IndexMetaData();
            metaData.minSubdiv = cellUpdateInfo.minSubdivInCell;
            metaData.minLocalIdx = cellUpdateInfo.minValidBrickIndexForCellAtMaxRes / minSubdivCellSize;
            metaData.maxLocalIdx = cellUpdateInfo.maxValidBrickIndexForCellAtMaxResPlusOne / minSubdivCellSize;
            metaData.firstChunkIndex = cellUpdateInfo.firstChunkIndex;

            metaData.Pack(out uint[] packedVals);

            for (int i = 0; i < kUintPerEntry; ++i)
            {
                m_IndexOfIndicesData[cellFlatIdx * kUintPerEntry + i] = packedVals[i];
            }

            m_NeedUpdateComputeBuffer = true;
        }

        internal void MarkCellAsUnloaded(int cellFlatIdx)
        {
            for (int i = 0; i < kUintPerEntry; ++i)
            {
                m_IndexOfIndicesData[cellFlatIdx * kUintPerEntry + i] = 0xFFFFFFFF;
            }

            m_NeedUpdateComputeBuffer = true;
        }

        internal void PushComputeData()
        {
            m_IndexOfIndicesBuffer.SetData(m_IndexOfIndicesData);
            m_NeedUpdateComputeBuffer = false;
        }

        internal void GetRuntimeResources(ref ProbeReferenceVolume.RuntimeResources rr)
        {
            // If we are pending an update of the actual compute buffer we do it here
            if (m_NeedUpdateComputeBuffer)
            {
                PushComputeData();
            }
            rr.cellIndices = m_IndexOfIndicesBuffer;
        }

        internal void Cleanup()
        {
            CoreUtils.SafeRelease(m_IndexOfIndicesBuffer);
            m_IndexOfIndicesBuffer = null;
        }
    }
}
