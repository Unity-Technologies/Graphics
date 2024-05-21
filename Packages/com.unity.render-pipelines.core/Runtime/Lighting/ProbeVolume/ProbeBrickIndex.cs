//#define USE_INDEX_NATIVE_ARRAY
using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Profiling;
using System.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Chunk = UnityEngine.Rendering.ProbeBrickPool.BrickChunkAlloc;
using CellIndexInfo = UnityEngine.Rendering.ProbeReferenceVolume.CellIndexInfo;

namespace UnityEngine.Rendering
{
    internal class ProbeBrickIndex
    {
        // a few constants
        internal const int kMaxSubdivisionLevels = 7; // 3 bits
        internal const int kIndexChunkSize = 243;

        internal const int kFailChunkIndex = -1;
        internal const int kEmptyIndex = -2; // This is a tag value used to say that we have a valid entry but with no data.


        BitArray m_IndexChunks;
        BitArray m_IndexChunksCopyForChecks;

        int m_ChunksCount;
        int m_AvailableChunkCount;

        ComputeBuffer m_PhysicalIndexBuffer;
        NativeArray<int> m_PhysicalIndexBufferData;
        ComputeBuffer m_DebugFragmentationBuffer;
        int[] m_DebugFragmentationData;
        
        bool m_NeedUpdateIndexComputeBuffer;
        int m_UpdateMinIndex = int.MaxValue;
        int m_UpdateMaxIndex = int.MinValue;

        internal int estimatedVMemCost { get; private set; }

        internal ComputeBuffer GetDebugFragmentationBuffer() => m_DebugFragmentationBuffer;
        internal float fragmentationRate { get; private set; }

        [DebuggerDisplay("Brick [{position}, {subdivisionLevel}]")]
        [Serializable]

        public struct Brick : IEquatable<Brick>
        {
            public Vector3Int position;   // refspace index, indices are brick coordinates at max resolution
            public int subdivisionLevel;              // size as factor covered elementary cells

            internal Brick(Vector3Int position, int subdivisionLevel)
            {
                this.position = position;
                this.subdivisionLevel = subdivisionLevel;
            }

            public bool Equals(Brick other) => position == other.position && subdivisionLevel == other.subdivisionLevel;

            // Important boundInBricks needs to be in min brick size so we can be agnostic of profile information.
            public bool IntersectArea(Bounds boundInBricksToCheck)
            {
                int sizeInMinBricks = ProbeReferenceVolume.CellSize(subdivisionLevel);
                Bounds brickBounds = new Bounds();
                brickBounds.min = position;
                brickBounds.max = position + new Vector3Int(sizeInMinBricks, sizeInMinBricks, sizeInMinBricks);

                brickBounds.extents *= 0.99f; // Extend a bit to avoid issues.

                bool intersectionHappens = boundInBricksToCheck.Intersects(brickBounds);

                return intersectionHappens;
            }
        }

        Vector3Int m_CenterRS;   // the anchor in ref space, around which the index is defined. [IMPORTANT NOTE! For now we always have it at 0, so is not passed to the shader, but is kept here until development is active in case we find it useful]

        int SizeOfPhysicalIndexFromBudget(ProbeVolumeTextureMemoryBudget memoryBudget)
        {
            switch (memoryBudget)
            {
                case ProbeVolumeTextureMemoryBudget.MemoryBudgetLow:
                    // 16 MB - 4 million of bricks worth of space. At full resolution and a distance of 1 meter between probes, this is roughly 474 * 474 * 474 meters worth of bricks. If 0.25x on Y axis, this is equivalent to 948 * 118 * 948 meters
                    return 4000000;
                case ProbeVolumeTextureMemoryBudget.MemoryBudgetMedium:
                    // 32 MB - 8 million of bricks worth of space. At full resolution and a distance of 1 meter between probes, this is roughly 600 * 600 * 600 meters worth of bricks. If 0.25x on Y axis, this is equivalent to 1200 * 150 * 1200 meters
                    return 8000000;
                case ProbeVolumeTextureMemoryBudget.MemoryBudgetHigh:
                    // 64 MB - 16 million of bricks worth of space. At full resolution and a distance of 1 meter between probes, this is roughly 756 * 756 * 756 meters worth of bricks. If 0.25x on Y axis, this is equivalent to 1512 * 184 * 1512 meters
                    return 16000000;
            }
            return 32000000;
        }

        internal ProbeBrickIndex(ProbeVolumeTextureMemoryBudget memoryBudget)
        {
            Profiler.BeginSample("Create ProbeBrickIndex");
            m_CenterRS = new Vector3Int(0, 0, 0);

            m_NeedUpdateIndexComputeBuffer = false;

            m_ChunksCount = Mathf.Max(1, Mathf.CeilToInt((float)SizeOfPhysicalIndexFromBudget(memoryBudget) / kIndexChunkSize));
            m_AvailableChunkCount = m_ChunksCount;
            m_IndexChunks = new BitArray(m_ChunksCount);
            m_IndexChunksCopyForChecks = new BitArray(m_ChunksCount);

            int physicalBufferSize = m_ChunksCount * kIndexChunkSize;
            m_PhysicalIndexBufferData = new NativeArray<int>(physicalBufferSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_PhysicalIndexBuffer = new ComputeBuffer(physicalBufferSize, sizeof(int), ComputeBufferType.Structured);

            estimatedVMemCost = physicalBufferSize * sizeof(int);

            Clear();
            Profiler.EndSample();
        }

        public int GetRemainingChunkCount()
        {
            return m_AvailableChunkCount;
        }

        internal void UploadIndexData()
        {
            var count = m_UpdateMaxIndex - m_UpdateMinIndex + 1;
            Debug.Assert(m_UpdateMinIndex >= 0 && m_UpdateMaxIndex < m_PhysicalIndexBufferData.Length, "Out of bounds");
            Debug.Assert(count >= 0, "Negative index");

            m_PhysicalIndexBuffer.SetData(m_PhysicalIndexBufferData, m_UpdateMinIndex, m_UpdateMinIndex, count);
            m_NeedUpdateIndexComputeBuffer = false;
            m_UpdateMaxIndex = int.MinValue;
            m_UpdateMinIndex = int.MaxValue;
        }

        void UpdateDebugData()
        {
            if (m_DebugFragmentationData == null || m_DebugFragmentationData.Length != m_IndexChunks.Length)
            {
                m_DebugFragmentationData = new int[m_IndexChunks.Length];
                CoreUtils.SafeRelease(m_DebugFragmentationBuffer);
                m_DebugFragmentationBuffer = new ComputeBuffer(m_IndexChunks.Length, 4);
            }

            for (int i = 0; i < m_IndexChunks.Length; ++i)
            {
                m_DebugFragmentationData[i] = m_IndexChunks[i] ? 1 : -1;
            }

            m_DebugFragmentationBuffer.SetData(m_DebugFragmentationData);
        }

        internal void Clear()
        {
            Profiler.BeginSample("Clear Index");

            m_IndexChunks.SetAll(false);
            m_AvailableChunkCount = m_ChunksCount;

            // Clear the physical buffer
            // This is not needed but cleaner i guess
            unsafe
            {
                uint* pBuffer = (uint*)NativeArrayUnsafeUtility.GetUnsafePtr(m_PhysicalIndexBufferData);
                UnsafeUtility.MemSet(pBuffer, 0xFF, m_PhysicalIndexBufferData.Length * 4);

                m_NeedUpdateIndexComputeBuffer = true;
                m_UpdateMinIndex = 0;
                m_UpdateMaxIndex = m_PhysicalIndexBufferData.Length - 1;
            }

            Profiler.EndSample();
        }

        internal void GetRuntimeResources(ref ProbeReferenceVolume.RuntimeResources rr)
        {
            bool displayFrag = ProbeReferenceVolume.instance.probeVolumeDebug.displayIndexFragmentation;
            // If we are pending an update of the actual compute buffer we do it here
            if (m_NeedUpdateIndexComputeBuffer)
            {
                UploadIndexData();
                if (displayFrag)
                    UpdateDebugData();
            }

            if (displayFrag && m_DebugFragmentationBuffer == null)
                UpdateDebugData();

            rr.index = m_PhysicalIndexBuffer;
        }

        internal void Cleanup()
        {
            m_PhysicalIndexBufferData.Dispose();
            CoreUtils.SafeRelease(m_PhysicalIndexBuffer);
            m_PhysicalIndexBuffer = null;
            CoreUtils.SafeRelease(m_DebugFragmentationBuffer);
            m_DebugFragmentationBuffer = null;
        }

        internal void ComputeFragmentationRate()
        {
            // Clearly suboptimal, will need to be profiled before trying to make it smarter.
            int highestAllocatedChunk = 0;
            for (int i = m_ChunksCount - 1; i >= 0; i--)
            {
                if (m_IndexChunks[i])
                {
                    highestAllocatedChunk = i + 1;
                    break;
                }
            }

            int tailFreeChunks = m_ChunksCount - highestAllocatedChunk;
            int holes = m_AvailableChunkCount - tailFreeChunks;
            fragmentationRate = (float)holes / highestAllocatedChunk;
        }

        public struct IndirectionEntryUpdateInfo
        {
            public int firstChunkIndex;
            public int numberOfChunks;
            public int minSubdivInCell;
            // IMPORTANT, These values should be at max resolution, independent of minSubdivInCell. This means that
            // The map to the lower possible resolution is done after.  However they are still in local space.
            public Vector3Int minValidBrickIndexForCellAtMaxRes;
            public Vector3Int maxValidBrickIndexForCellAtMaxResPlusOne;
            public Vector3Int entryPositionInBricksAtMaxRes;

            public bool hasOnlyBiggerBricks; // True if it has only bricks that are bigger than the entry itself
        }

        public struct CellIndexUpdateInfo
        {
            public IndirectionEntryUpdateInfo[] entriesInfo;

            public int GetNumberOfChunks()
            {
                int chunkCount = 0;
                foreach (var entry in entriesInfo)
                {
                    chunkCount += entry.numberOfChunks;
                }
                return chunkCount;
            }
        }

        int MergeIndex(int index, int size)
        {
            const int mask = kMaxSubdivisionLevels;
            const int shift = 28;
            return (index & ~(mask << shift)) | ((size & mask) << shift);
        }

        internal int GetNumberOfChunks(int brickCount)
        {
            return Mathf.CeilToInt((float)brickCount / kIndexChunkSize);
        }

        internal bool FindSlotsForEntries(ref IndirectionEntryUpdateInfo[] entriesInfo)
        {
            using var _ = new Unity.Profiling.ProfilerMarker("FindSlotsForEntries").Auto();

            // This is not great, but the alternative is making the always in memory m_IndexChunks bigger.
            // The other alternative is to temporary mark m_IndexChunks and unmark, but that is going to be slower.
            // The copy here is temporary and while it is created upon loading all the time, it is going to occupy mnemory
            // only when a load operation happens.

            m_IndexChunksCopyForChecks.SetAll(false);
            m_IndexChunksCopyForChecks.Or(m_IndexChunks);

            int numberOfEntries = entriesInfo.Length;
            for (int entry = 0; entry < numberOfEntries; ++entry)
            {
                entriesInfo[entry].firstChunkIndex = kEmptyIndex;

                int numberOfChunksForEntry = entriesInfo[entry].numberOfChunks;
                if (numberOfChunksForEntry == 0) continue;

                for (int i = 0; i < m_ChunksCount - numberOfChunksForEntry; ++i)
                {
                    if (!m_IndexChunksCopyForChecks[i])
                    {
                        int firstSlot = i, lastSlot = i + numberOfChunksForEntry;
                        while (i + 1 < lastSlot)
                        {
                            if (m_IndexChunksCopyForChecks[++i])
                                break;
                        }

                        if (!m_IndexChunksCopyForChecks[i])
                        {
                            entriesInfo[entry].firstChunkIndex = firstSlot;
                            break;
                        }
                    }
                }

                if (entriesInfo[entry].firstChunkIndex < 0)
                {
                    for (int e = 0; e < numberOfEntries; ++e)
                        entriesInfo[e].firstChunkIndex = kFailChunkIndex;
                    return false;
                }

                // Sucessful allocation - We need to markup the copy.
                for (int i = entriesInfo[entry].firstChunkIndex; i < (entriesInfo[entry].firstChunkIndex + numberOfChunksForEntry); ++i)
                {
                    Debug.Assert(!m_IndexChunksCopyForChecks[i]);
                    m_IndexChunksCopyForChecks[i] = true;
                }
            }

            return true;
        }

        internal bool ReserveChunks(IndirectionEntryUpdateInfo[] entriesInfo, bool ignoreErrorLog)
        {
            int entryCount = entriesInfo.Length;
            // Sanity check that shouldn't be needed.
            for (int entry = 0; entry < entryCount; ++entry)
            {
                int firstChunkForEntry = entriesInfo[entry].firstChunkIndex;
                int numberOfChunkForEntry = entriesInfo[entry].numberOfChunks;

                if (numberOfChunkForEntry == 0) continue;

                if (firstChunkForEntry < 0)
                {
                    if (!ignoreErrorLog)
                        Debug.LogError("APV Index Allocation failed.");

                    return false;
                }

                for (int i = firstChunkForEntry; i < (firstChunkForEntry + numberOfChunkForEntry); ++i)
                {
                    Debug.Assert(!m_IndexChunks[i]);
                    m_IndexChunks[i] = true;
                }

                m_AvailableChunkCount -= numberOfChunkForEntry;
            }

            return true;
        }

        static internal bool BrickOverlapEntry(Vector3Int brickMin, Vector3Int brickMax, Vector3Int entryMin, Vector3Int entryMax)
        {
            return brickMax.x > entryMin.x && entryMax.x > brickMin.x &&
                   brickMax.y > entryMin.y && entryMax.y > brickMin.y &&
                   brickMax.z > entryMin.z && entryMax.z > brickMin.z;
        }

        static int LocationToIndex(int x, int y, int z, Vector3Int sizeOfValid)
        {
            return z * (sizeOfValid.x * sizeOfValid.y) + x * sizeOfValid.y + y;
        }

        void MarkBrickInPhysicalBuffer(in IndirectionEntryUpdateInfo entry, Vector3Int brickMin, Vector3Int brickMax, int brickSubdivLevel, int entrySubdivLevel, int idx)
        {
            m_NeedUpdateIndexComputeBuffer = true;

            // An indirection entry might have only a single brick that is actually bigger than the entry itself. This is a bit of simpler use case
            // that doesn't fit what happens otherwise in this function where the assumption is that minSubdivInCell is equal or less the size of the entry.
            // In this special case, we can do things much simpler as we *know* by construction that we will always have only a single relevant brick
            // and so we do the update in a simpler fashion.
            if (entry.hasOnlyBiggerBricks)
            {
                int singleEntry = entry.firstChunkIndex * kIndexChunkSize;
                m_UpdateMinIndex = Math.Min(m_UpdateMinIndex, singleEntry);
                m_UpdateMaxIndex = Math.Max(m_UpdateMaxIndex, singleEntry);
                m_PhysicalIndexBufferData[singleEntry] = idx;
            }
            else
            {
                int minBrickSize = ProbeReferenceVolume.CellSize(entry.minSubdivInCell);

                // Map the brick to the voxel, essentially the inverse of GetIndexData in hlsl
                // The size of valid data in the entry might be smaller than the entry size
                var entryMinIndex = entry.minValidBrickIndexForCellAtMaxRes / minBrickSize;
                var entryMaxIndex = entry.maxValidBrickIndexForCellAtMaxResPlusOne / minBrickSize;
                var sizeOfValid = (entryMaxIndex - entryMinIndex);
                            
                if (brickSubdivLevel >= entrySubdivLevel)
                {
                    brickMin = Vector3Int.zero;
                    brickMax = sizeOfValid;
                }
                else
                {
                    // We need to do our calculations in local space to the cell, so we move the brick to local space as a first step.
                    // Reminder that at this point we are still operating at highest resolution possible, not necessarily the one that will be
                    // the final resolution for the chunk.
                    brickMin -= entry.entryPositionInBricksAtMaxRes;
                    brickMax -= entry.entryPositionInBricksAtMaxRes;

                    // Since the index is spurious (not same resolution, but varying per cell) we need to bring to the output resolution the brick coordinates
                    // Before finding the locations inside the Index for the current cell/chunk.
                    brickMin /= minBrickSize;
                    brickMax /= minBrickSize;

                    // Verify we are actually in local space now.
                    int maxCellSizeInOutputRes = ProbeReferenceVolume.CellSize(entrySubdivLevel - entry.minSubdivInCell);
                    Debug.Assert(brickMin.x >= 0 && brickMin.y >= 0 && brickMin.z >= 0, "Brick is out of bounds");
                    Debug.Assert(brickMax.x <= maxCellSizeInOutputRes && brickMax.y <= maxCellSizeInOutputRes && brickMax.z <= maxCellSizeInOutputRes, "Brick out of bounds");

                    // Then perform the rescale of the local indices for min and max.
                    brickMin -= entryMinIndex;
                    brickMax -= entryMinIndex;
                }
                            
                // Analytically compute min and max because doing it in the inner loop with Math.Min/Max is costly (not inlined)
                int chunkStart = entry.firstChunkIndex * kIndexChunkSize;
                int newMin = chunkStart + LocationToIndex(brickMin.x, brickMin.y, brickMin.z, sizeOfValid);
                int newMax = chunkStart + LocationToIndex(brickMax.x - 1, brickMax.y - 1, brickMax.z - 1, sizeOfValid);
                m_UpdateMinIndex = Math.Min(m_UpdateMinIndex, newMin);
                m_UpdateMaxIndex = Math.Max(m_UpdateMaxIndex, newMax);
                            
                // Loop through all touched voxels
                for (int x = brickMin.x; x < brickMax.x; ++x)
                {
                    for (int z = brickMin.z; z < brickMax.z; ++z)
                    {
                        for (int y = brickMin.y; y < brickMax.y; ++y)
                        {
                            int localFlatIdx = LocationToIndex(x, y, z, sizeOfValid);
                            m_PhysicalIndexBufferData[chunkStart + localFlatIdx] = idx;
                        }
                    }
                }
            }
        }

        public void AddBricks(CellIndexInfo cellInfo, NativeArray<Brick> bricks, List<Chunk> allocations, int allocationSize, int poolWidth, int poolHeight)
        {
            Debug.Assert(bricks.Length <= ushort.MaxValue, "Cannot add more than 65K bricks per RegId.");

            // Updates the physical buffer
            // Note that bricks in a cell are sorted with biggest subdivisions first in the list
            // They are placed in the same order inside the chunks of the brick pool

            // This can still be improved but is not the bottleneck currently

            // Compute some stuff
            var prv = ProbeReferenceVolume.instance;
            int entrySubdivLevel = prv.GetEntrySubdivLevel();

            // Iterate over all bricks, while tracking allocated coords in the pool
            int brick_idx = 0;
            for (int i = 0; i < allocations.Count; i++)
            {
                Chunk alloc = allocations[i];
                int last_brick = brick_idx + Mathf.Min(allocationSize, bricks.Length - brick_idx);
                while (brick_idx != last_brick)
                {
                    // Fetch brick and increment counters
                    Brick brick = bricks[brick_idx++];
                    int idx = MergeIndex(alloc.flattenIndex(poolWidth, poolHeight), brick.subdivisionLevel);
                    alloc.x += ProbeBrickPool.kBrickProbeCountPerDim;

                    // Brick bounds
                    int brickSize = ProbeReferenceVolume.CellSize(brick.subdivisionLevel);
                    Vector3Int brickMin = brick.position;
                    Vector3Int brickMax = brick.position + new Vector3Int(brickSize, brickSize, brickSize);
                    
                    // Find all entries that this brick touch (usually only one, but several in case of bigger bricks)
                    foreach (var entry in cellInfo.updateInfo.entriesInfo)
                    {
                        // We might have the brick completely out of the entry. In those case, we must skip.
                        var minEntryPosition = entry.entryPositionInBricksAtMaxRes + entry.minValidBrickIndexForCellAtMaxRes;
                        var maxEntryPosition = entry.entryPositionInBricksAtMaxRes + entry.maxValidBrickIndexForCellAtMaxResPlusOne - Vector3Int.one;
                        if (BrickOverlapEntry(brickMin, brickMax, minEntryPosition, maxEntryPosition))
                            MarkBrickInPhysicalBuffer(entry, brickMin, brickMax, brick.subdivisionLevel, entrySubdivLevel, idx);
                    }
                }
            }
        }

        public void RemoveBricks(CellIndexInfo cellInfo)
        {
            // Clear allocated chunks
            for (int e = 0; e < cellInfo.updateInfo.entriesInfo.Length; e++)
            {
                ref var entryInfo = ref cellInfo.updateInfo.entriesInfo[e];
                if (entryInfo.firstChunkIndex < 0) continue;

                for (int i = entryInfo.firstChunkIndex; i < (entryInfo.firstChunkIndex + entryInfo.numberOfChunks); ++i)
                {
                    m_IndexChunks[i] = false;
                }
                m_AvailableChunkCount += entryInfo.numberOfChunks;
                entryInfo.numberOfChunks = 0;
            }
        }
    }
}
