
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    internal class ProbeGlobalIndirection
    {
        const int kUintPerEntry = 3;
        internal int estimatedVMemCost { get; private set; }

        // IMPORTANT! IF THIS VALUE CHANGES DATA NEEDS TO BE REBAKED.
        internal const int kEntryMaxSubdivLevel = 3;

        internal struct IndexMetaData
        {
            static uint[] s_PackedValues = new uint[kUintPerEntry];

            internal Vector3Int minLocalIdx;
            internal Vector3Int maxLocalIdx;
            internal int firstChunkIndex;
            internal int minSubdiv;

            internal void Pack(out uint[] vals)
            {
                vals = s_PackedValues;
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

        int m_CellSizeInMinBricks;

        Vector3Int m_EntriesCount;
        Vector3Int m_EntryMin;
        Vector3Int m_EntryMax;

        internal void GetMinMaxEntry(out Vector3Int minEntry, out Vector3Int maxEntry)
        {
            minEntry = m_EntryMin;
            maxEntry = m_EntryMax;
        }

        bool m_NeedUpdateComputeBuffer;

        internal Vector3Int GetGlobalIndirectionDimension() => m_EntriesCount;
        internal Vector3Int GetGlobalIndirectionMinEntry() => m_EntryMin;

        int entrySizeInBricks => Mathf.Min((int)Mathf.Pow(ProbeBrickPool.kBrickCellCount, kEntryMaxSubdivLevel), m_CellSizeInMinBricks);
        internal int entriesPerCellDimension => m_CellSizeInMinBricks / Mathf.Max(1, entrySizeInBricks);

        int GetFlatIndex(Vector3Int normalizedPos)
        {
            return normalizedPos.z * (m_EntriesCount.x * m_EntriesCount.y) + normalizedPos.y * m_EntriesCount.x + normalizedPos.x;
        }

        internal ProbeGlobalIndirection(Vector3Int cellMin, Vector3Int cellMax, int cellSizeInMinBricks)
        {
            m_CellSizeInMinBricks = cellSizeInMinBricks;

            Vector3Int cellCount = cellMax + Vector3Int.one - cellMin;
            m_EntriesCount = cellCount * entriesPerCellDimension;
            m_EntryMin = cellMin * entriesPerCellDimension;

            m_EntryMax = (cellMax + Vector3Int.one) * entriesPerCellDimension - Vector3Int.one;

            int flatEntryCount = m_EntriesCount.x * m_EntriesCount.y * m_EntriesCount.z;
            int bufferSize = kUintPerEntry * flatEntryCount;
            m_IndexOfIndicesBuffer = new ComputeBuffer(flatEntryCount, kUintPerEntry * sizeof(uint));
            m_IndexOfIndicesData = new uint[bufferSize];
            m_NeedUpdateComputeBuffer = false;
            estimatedVMemCost = flatEntryCount * kUintPerEntry * sizeof(uint);
        }


        internal int GetFlatIdxForEntry(Vector3Int entryPosition)
        {
            Vector3Int normalizedPos = entryPosition - m_EntryMin;
            Debug.Assert(normalizedPos.x >= 0 && normalizedPos.y >= 0 && normalizedPos.z >= 0);

            return GetFlatIndex(normalizedPos);
        }

        internal int[] GetFlatIndicesForCell(Vector3Int cellPosition)
        {
            Vector3Int firstEntryPosition = cellPosition * entriesPerCellDimension;
            int entriesPerCellDim = m_CellSizeInMinBricks / entrySizeInBricks;

            int[] outListOfIndices = new int[entriesPerCellDimension * entriesPerCellDimension * entriesPerCellDimension];

            int i = 0;
            for (int x = 0; x < entriesPerCellDim; ++x)
            {
                for (int y = 0; y < entriesPerCellDim; ++y)
                {
                    for (int z = 0; z < entriesPerCellDim; ++z)
                    {
                        outListOfIndices[i++] = GetFlatIdxForEntry(firstEntryPosition + new Vector3Int(x, y, z));
                    }
                }
            }

            return outListOfIndices;
        }

        internal void UpdateCell(int[] cellEntriesIndices, ProbeBrickIndex.CellIndexUpdateInfo cellUpdateInfo)
        {
            for (int entry = 0; entry < cellEntriesIndices.Length; ++entry)
            {
                int entryIndex = cellEntriesIndices[entry];
                ProbeBrickIndex.IndirectionEntryUpdateInfo entryUpdateInfo = cellUpdateInfo.entriesInfo[entry];

                int minSubdivCellSize = ProbeReferenceVolume.CellSize(entryUpdateInfo.minSubdivInCell);
                IndexMetaData metaData = new IndexMetaData();
                metaData.minSubdiv = entryUpdateInfo.minSubdivInCell;
                metaData.minLocalIdx = entryUpdateInfo.hasOnlyBiggerBricks ? Vector3Int.zero : entryUpdateInfo.minValidBrickIndexForCellAtMaxRes / minSubdivCellSize;
                metaData.maxLocalIdx = entryUpdateInfo.hasOnlyBiggerBricks ? Vector3Int.one : entryUpdateInfo.maxValidBrickIndexForCellAtMaxResPlusOne / minSubdivCellSize;
                metaData.firstChunkIndex = entryUpdateInfo.firstChunkIndex;

                metaData.Pack(out uint[] packedVals);

                for (int i = 0; i < kUintPerEntry; ++i)
                {
                    m_IndexOfIndicesData[entryIndex * kUintPerEntry + i] = packedVals[i];
                }
            }

            m_NeedUpdateComputeBuffer = true;
        }

        internal void MarkEntriesAsUnloaded(int[] entriesFlatIndices)
        {
            for (int entry = 0; entry < entriesFlatIndices.Length; ++entry)
            {
                for (int i = 0; i < kUintPerEntry; ++i)
                {
                    m_IndexOfIndicesData[entriesFlatIndices[entry] * kUintPerEntry + i] = 0xFFFFFFFF;
                }
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
