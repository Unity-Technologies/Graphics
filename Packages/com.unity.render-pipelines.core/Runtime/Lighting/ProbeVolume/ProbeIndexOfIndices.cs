using System;
using CellIndexInfo = UnityEngine.Rendering.ProbeReferenceVolume.CellIndexInfo;

namespace UnityEngine.Rendering
{
    internal class ProbeGlobalIndirection
    {
        const int k_UintPerEntry = 3;
        internal int estimatedVMemCost { get; private set; }

        // IMPORTANT! IF THIS VALUE CHANGES DATA NEEDS TO BE REBAKED.
        internal const int k_EntryMaxSubdivLevel = 3;

        internal struct IndexMetaData
        {
            static readonly uint[] s_PackedValues = new uint[k_UintPerEntry];

#if UNITY_EDITOR
            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
            static void ResetStaticsOnLoad()
            {
                Array.Clear(s_PackedValues, 0, s_PackedValues.Length);
            }
#endif

            internal Vector3Int m_MinLocalIdx;
            internal Vector3Int m_MaxLocalIdxPlusOne;
            internal int m_FirstChunkIndex;
            internal int m_MinSubdiv;

            internal void Pack(out uint[] vals)
            {
                vals = s_PackedValues;
                for (int i = 0; i < k_UintPerEntry; ++i)
                {
                    vals[i] = 0;
                }

                //  TODO: Note this packing is too generous, we can get rid of 1 uint
                //  minLocalIndex is in cell space so it has an upper bound
                //  first chunk index is also on 16bits max when using max memory budget
                // see comment below about size of valid
                //
                // UINT 0:
                //  FirstChunkIndex        29 bit
                //  MinSubdiv              3  bit
                // UINT 1:
                //  minLocalIdx.x          10 bit
                //  minLocalIdx.y          10 bit
                //  minLocalIdx.z          10 bit
                // UINT 2:
                //  sizeOfValid.x          10 bit
                //  sizeOfValid.y          10 bit
                //  sizeOfValid.z          10 bit

                // This is always less than CellSize(k_EntryMaxSubdivLevel)+1 == 28. See GetEntrySubdivLevel()
                var sizeOfValid = m_MaxLocalIdxPlusOne - m_MinLocalIdx;

                vals[0] = (uint)m_FirstChunkIndex & 0x1FFFFFFF;
                vals[0] |= ((uint)m_MinSubdiv & 0x7) << 29;

                vals[1] = (uint)m_MinLocalIdx.x & 0x3FF;
                vals[1] |= ((uint)m_MinLocalIdx.y & 0x3FF) << 10;
                vals[1] |= ((uint)m_MinLocalIdx.z & 0x3FF) << 20;

                vals[2] = (uint)sizeOfValid.x & 0x3FF;
                vals[2] |= ((uint)sizeOfValid.y & 0x3FF) << 10;
                vals[2] |= ((uint)sizeOfValid.z & 0x3FF) << 20;
            }
        }

        ComputeBuffer m_IndexOfIndicesBuffer;
        readonly uint[] m_IndexOfIndicesData;

        readonly int m_CellSizeInMinBricks;

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

        int entrySizeInBricks => Mathf.Min((int)Mathf.Pow(ProbeBrickPool.k_BrickCellCount, k_EntryMaxSubdivLevel), m_CellSizeInMinBricks);
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
            int bufferSize = k_UintPerEntry * flatEntryCount;
            m_IndexOfIndicesBuffer = new ComputeBuffer(flatEntryCount, k_UintPerEntry * sizeof(uint));
            m_IndexOfIndicesData = new uint[bufferSize];
            m_NeedUpdateComputeBuffer = false;
            estimatedVMemCost = flatEntryCount * k_UintPerEntry * sizeof(uint);
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

        internal void UpdateCell(CellIndexInfo cellInfo)
        {
            for (int entry = 0; entry < cellInfo.flatIndicesInGlobalIndirection.Length; ++entry)
            {
                int entryIndex = cellInfo.flatIndicesInGlobalIndirection[entry];
                ProbeBrickIndex.IndirectionEntryUpdateInfo entryUpdateInfo = cellInfo.updateInfo.entriesInfo[entry];

                int minSubdivCellSize = ProbeReferenceVolume.CellSize(entryUpdateInfo.minSubdivInCell);
                var metaData = new IndexMetaData();
                metaData.m_MinSubdiv = entryUpdateInfo.minSubdivInCell;
                metaData.m_MinLocalIdx = entryUpdateInfo.hasOnlyBiggerBricks ? Vector3Int.zero : entryUpdateInfo.minValidBrickIndexForCellAtMaxRes / minSubdivCellSize;
                metaData.m_MaxLocalIdxPlusOne = entryUpdateInfo.hasOnlyBiggerBricks ? Vector3Int.one : entryUpdateInfo.maxValidBrickIndexForCellAtMaxResPlusOne / minSubdivCellSize;
                metaData.m_FirstChunkIndex = entryUpdateInfo.firstChunkIndex;

                metaData.Pack(out uint[] packedVals);

                for (int i = 0; i < k_UintPerEntry; ++i)
                {
                    m_IndexOfIndicesData[entryIndex * k_UintPerEntry + i] = packedVals[i];
                }
            }

            m_NeedUpdateComputeBuffer = true;
        }

        internal void MarkEntriesAsUnloaded(int[] entriesFlatIndices)
        {
            for (int entry = 0; entry < entriesFlatIndices.Length; ++entry)
            {
                for (int i = 0; i < k_UintPerEntry; ++i)
                {
                    m_IndexOfIndicesData[entriesFlatIndices[entry] * k_UintPerEntry + i] = 0xFFFFFFFF;
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
