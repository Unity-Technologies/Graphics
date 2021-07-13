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
        internal struct IndexMetaData
        {
            internal Vector3Int indexStart; // can be flatted into a single uint
            internal int indexDimension; // Same on all 3 axis. If the index are all uniform this is not needed (will be different when using per-index minBrickSize)
            internal int minSubdiv;

            internal void Pack(out uint val1, out uint val2)
            {
                val1 = 0;
                val2 = 0;


                // This is a bit wasteful, we could easily be in one uint, but we need to have the signs too as the physical index might not be rounded by cell
                // so cell index start might need to be negative
                // UINT 1:
                // Index start  sign X :  1 bit
                // Index start       X : 15 bit
                // Index start  sign Y :  1 bit
                // Index start       Y : 15 bit

                // UINT 2:
                // Index start  sign Z :  1 bit
                // Index start       Z : 15 bit
                // pow(3, minSubdiv)   : 16 bit


                // If sign was not an issue, we could've probably packed in a single uint as follow.
                // IndexStart x: 10 bits
                //            y: 9 bits
                //            z: 10 bits
                // minSubdiv   : 3 bits
                // minSubdiv   : 3 bits

                val1 = indexStart.x >= 0 ? 1u : 0u;
                val1 |= ((uint)Mathf.Abs(indexStart.x) & 0x7FFF) << 1;
                val1 |= (indexStart.y >= 0 ? 1u : 0u) << 16;
                val1 |= ((uint)Mathf.Abs(indexStart.y) & 0x7FFF) << 17;

                val2 = indexStart.z >= 0 ? 1u : 0u;
                val2 |= ((uint)Mathf.Abs(indexStart.z) & 0x7FFF) << 1;
                val2 |= ((uint)Mathf.Pow(3, minSubdiv)) << 16;
            }
        }

        // To  sample in shader:
        // - index position is in minBrickSpace
        // - Divide by number of bricks per cell, this will be index in cells starting from 0
        // - use index computed above to get the metadata.
        // - Get position relative to this index ( TODO: How? do we need extra data considering  we'll have streaming? Probably we will need a start in a virtual infinite index
        //   which is like the old-index which is the whole world to get a relative index + then a physical index into the physical index into the index table.)
        // - Sample index
        // - ???
        // - Profit


        ComputeBuffer m_IndexOfIndicesBuffer;
        uint[] m_IndexOfIndicesData;

        Vector3Int m_CellCount;
        Vector3Int m_CellMin;
        int m_CellSizeInMinBricks;

        bool m_NeedUpdateComputeBuffer;

        internal Vector3Int GetCellIndexDimension() =>  m_CellCount;
        internal Vector3Int GetCellMinPosition() => m_CellMin;

        int GetFlatIndex(Vector3Int  normalizedPos)
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
            int entryPerCell = 2;
            int bufferSize = entryPerCell * flatCellCount;
            m_IndexOfIndicesBuffer = new ComputeBuffer(flatCellCount, 2 * sizeof(uint));
            m_IndexOfIndicesData = new uint[bufferSize];
            m_NeedUpdateComputeBuffer = false;
        }

        internal void AddCell(Vector3Int cellPosition, Vector3Int indexStart, int minSubdiv)
        {
            Vector3Int normalizedPos = cellPosition - m_CellMin;
            Debug.Log($"Cell {cellPosition} normalized as {normalizedPos} via a min of {m_CellMin} starts at {indexStart} ?");

            Debug.Assert(normalizedPos.x >= 0 && normalizedPos.y >= 0 && normalizedPos.z >= 0);

            int flatIdx = GetFlatIndex(normalizedPos);

            int indexDimension = m_CellSizeInMinBricks / (int)Mathf.Pow(3, minSubdiv);
            IndexMetaData metaData = new IndexMetaData();
            metaData.indexStart = indexStart;
            metaData.indexDimension = indexDimension;
            metaData.minSubdiv = minSubdiv;

            uint packed1, packed2;
            metaData.Pack(out packed1, out packed2);
            m_IndexOfIndicesData[flatIdx * 2 + 0] = packed1;
            m_IndexOfIndicesData[flatIdx * 2 + 1] = packed2;
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
