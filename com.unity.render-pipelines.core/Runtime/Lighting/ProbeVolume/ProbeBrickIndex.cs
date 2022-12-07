//#define USE_INDEX_NATIVE_ARRAY
using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Profiling;
using System.Collections;
using Unity.Collections;
using Chunk = UnityEngine.Rendering.ProbeBrickPool.BrickChunkAlloc;
using CellInfo = UnityEngine.Rendering.ProbeReferenceVolume.CellInfo;
using Cell = UnityEngine.Rendering.ProbeReferenceVolume.Cell;

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
        int[] m_PhysicalIndexBufferData;
        ComputeBuffer m_DebugFragmentationBuffer;
        int[] m_DebugFragmentationData;

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

        [DebuggerDisplay("Brick [{brick.position}, {brick.subdivisionLevel}], {flattenedIdx}")]
        struct ReservedBrick
        {
            public Brick brick;
            public int flattenedIdx;
        }

        class VoxelMeta
        {
            public Cell cell;
            public List<ushort> brickIndices = new List<ushort>();

            public void Clear()
            {
                cell = null;
                brickIndices.Clear();
            }
        }

        class BrickMeta
        {
            public HashSet<Vector3Int> voxels = new HashSet<Vector3Int>();
            public List<ReservedBrick> bricks = new List<ReservedBrick>();

            public void Clear()
            {
                voxels.Clear();
                bricks.Clear();
            }
        }

        Vector3Int m_CenterRS;   // the anchor in ref space, around which the index is defined. [IMPORTANT NOTE! For now we always have it at 0, so is not passed to the shader, but is kept here until development is active in case we find it useful]

        Dictionary<Vector3Int, List<VoxelMeta>> m_VoxelToBricks;
        Dictionary<Cell, BrickMeta> m_BricksToVoxels;

        // Various pools for data re-usage
        ObjectPool<BrickMeta> m_BrickMetaPool = new ObjectPool<BrickMeta>(x => x.Clear(), null, false);
        ObjectPool<List<VoxelMeta>> m_VoxelMetaListPool = new ObjectPool<List<VoxelMeta>>(x => x.Clear(), null, false);
        ObjectPool<VoxelMeta> m_VoxelMetaPool = new ObjectPool<VoxelMeta>(x => x.Clear(), null, false);

        int GetVoxelSubdivLevel()
        {
            int defaultVoxelSubdivLevel = 3;
            return Mathf.Min(defaultVoxelSubdivLevel, ProbeReferenceVolume.instance.GetMaxSubdivision() - 1);
        }

        bool m_NeedUpdateIndexComputeBuffer;
        int m_UpdateMinIndex = int.MaxValue;
        int m_UpdateMaxIndex = int.MinValue;

        // Static variable required to avoid allocations inside lambda functions
        static Cell g_Cell = null;

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

            m_VoxelToBricks = new Dictionary<Vector3Int, List<VoxelMeta>>();
            m_BricksToVoxels = new Dictionary<Cell, BrickMeta>();

            m_NeedUpdateIndexComputeBuffer = false;

            m_ChunksCount = Mathf.Max(1, Mathf.CeilToInt((float)SizeOfPhysicalIndexFromBudget(memoryBudget) / kIndexChunkSize));
            m_AvailableChunkCount = m_ChunksCount;
            m_IndexChunks = new BitArray(m_ChunksCount);
            m_IndexChunksCopyForChecks = new BitArray(m_ChunksCount);

            int physicalBufferSize = m_ChunksCount * kIndexChunkSize;
            m_PhysicalIndexBufferData = new int[physicalBufferSize];
            m_PhysicalIndexBuffer = new ComputeBuffer(physicalBufferSize, sizeof(int), ComputeBufferType.Structured);

            estimatedVMemCost = physicalBufferSize * sizeof(int);

            // Should be done by a compute shader
            Clear();
            Profiler.EndSample();
        }

        public int GetRemainingChunkCount()
        {
            return m_AvailableChunkCount;
        }

        internal void UploadIndexData()
        {
            Debug.Assert(m_UpdateMinIndex >= 0 && m_UpdateMaxIndex < m_PhysicalIndexBufferData.Length);

            var count = m_UpdateMaxIndex - m_UpdateMinIndex + 1;
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

            for (int i = 0; i < m_PhysicalIndexBufferData.Length; ++i)
                m_PhysicalIndexBufferData[i] = -1;

            m_NeedUpdateIndexComputeBuffer = true;
            m_UpdateMinIndex = 0;
            m_UpdateMaxIndex = m_PhysicalIndexBufferData.Length - 1;

            m_IndexChunks.SetAll(false);

            foreach (var value in m_VoxelToBricks.Values)
            {
                foreach (var voxel in value)
                    m_VoxelMetaPool.Release(voxel);
                m_VoxelMetaListPool.Release(value);
            }
            m_VoxelToBricks.Clear();

            foreach (var value in m_BricksToVoxels.Values)
                m_BrickMetaPool.Release(value);
            m_BricksToVoxels.Clear();

            m_AvailableChunkCount = m_ChunksCount;

            Profiler.EndSample();
        }

        void MapBrickToVoxels(ProbeBrickIndex.Brick brick, HashSet<Vector3Int> voxels)
        {
            // create a list of all voxels this brick will touch
            int brick_subdiv = brick.subdivisionLevel;
            int voxels_touched_cnt = (int)Mathf.Pow(3, Mathf.Max(0, brick_subdiv - GetVoxelSubdivLevel()));

            Vector3Int ipos = brick.position;
            int brick_size = ProbeReferenceVolume.CellSize(brick.subdivisionLevel);
            int voxel_size = ProbeReferenceVolume.CellSize(GetVoxelSubdivLevel());

            if (voxels_touched_cnt <= 1)
            {
                Vector3 pos = brick.position;
                pos = pos * (1.0f / voxel_size);
                ipos = new Vector3Int(Mathf.FloorToInt(pos.x) * voxel_size, Mathf.FloorToInt(pos.y) * voxel_size, Mathf.FloorToInt(pos.z) * voxel_size);
            }

            for (int z = ipos.z; z < ipos.z + brick_size; z += voxel_size)
                for (int y = ipos.y; y < ipos.y + brick_size; y += voxel_size)
                    for (int x = ipos.x; x < ipos.x + brick_size; x += voxel_size)
                    {
                        voxels.Add(new Vector3Int(x, y, z));
                    }
        }

        void ClearVoxel(Vector3Int pos, IndirectionEntryUpdateInfo entryInfo)
        {
            Vector3Int vx_min, vx_max;
            ClipToIndexSpace(pos, GetVoxelSubdivLevel(), out vx_min, out vx_max, entryInfo);

            var brickMin = vx_min - entryInfo.entryPositionInBricksAtMaxRes;
            var brickMax = vx_max - entryInfo.entryPositionInBricksAtMaxRes;
            brickMin /= ProbeReferenceVolume.CellSize(entryInfo.minSubdivInCell);
            brickMax /= ProbeReferenceVolume.CellSize(entryInfo.minSubdivInCell);

            Debug.Assert(brickMin.x >= 0 && brickMin.y >= 0 && brickMin.z >= 0 && brickMax.x >= 0 && brickMax.y >= 0 && brickMax.z >= 0);

            UpdatePhysicalIndex(vx_min, vx_max, -1, entryInfo);
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
            public int brickCount;
            public int firstChunkIndex;
            public int numberOfChunks;
            public int minSubdivInCell;
            // IMPORTANT, These values should be at max resolution. This means that
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
            int numberOfEntries = entriesInfo.Length;
            for (int entry = 0; entry < numberOfEntries; ++entry)
            {
                entriesInfo[entry].firstChunkIndex = (entriesInfo[entry].numberOfChunks > 0) ? kFailChunkIndex : kEmptyIndex;
            }

            // This is not great, but the alternative is making the always in memory m_IndexChunks bigger.
            // The other alternative is to temporary mark m_IndexChunks and unmark, but that is going to be slower.
            // The copy here is temporary and while it is created upon loading all the time, it is going to occupy mnemory
            // only when a load operation happens.

            m_IndexChunksCopyForChecks.SetAll(false);
            m_IndexChunksCopyForChecks.Or(m_IndexChunks);

            for (int entry = 0; entry < numberOfEntries; ++entry)
            {
                int numberOfChunksForEntry = entriesInfo[entry].numberOfChunks;
                if (numberOfChunksForEntry == 0) continue;


                for (int i = 0; i < m_ChunksCount; ++i)
                {
                    if (!m_IndexChunksCopyForChecks[i] && (i + numberOfChunksForEntry) < m_ChunksCount)
                    {
                        int emptySlotsStartingHere = 0;
                        for (int k = i; k < (i + numberOfChunksForEntry); ++k)
                        {
                            if (!m_IndexChunksCopyForChecks[k]) emptySlotsStartingHere++;
                            else break;
                        }

                        if (emptySlotsStartingHere == numberOfChunksForEntry)
                        {
                            entriesInfo[entry].firstChunkIndex = i;
                            break;
                        }
                    }
                }

                if (entriesInfo[entry].firstChunkIndex < 0)
                    return false;
                else // We need to markup the copy.
                {
                    for (int i = entriesInfo[entry].firstChunkIndex; i < (entriesInfo[entry].firstChunkIndex + numberOfChunksForEntry); ++i)
                    {
                        Debug.Assert(!m_IndexChunksCopyForChecks[i]);
                        m_IndexChunksCopyForChecks[i] = true;
                    }
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

        public void AddBricks(Cell cell, NativeArray<Brick> bricks, List<Chunk> allocations, int allocationSize, int poolWidth, int poolHeight, CellIndexUpdateInfo cellInfo)
        {
            Debug.Assert(bricks.Length <= ushort.MaxValue, "Cannot add more than 65K bricks per RegId.");
            int largest_cell = ProbeReferenceVolume.CellSize(kMaxSubdivisionLevels);

            g_Cell = cell;

            // create a new copy
            BrickMeta bm = m_BrickMetaPool.Get();
            //if (!m_BricksToVoxels.ContainsKey(cell))
                m_BricksToVoxels.Add(cell, bm);

            int brick_idx = 0;
            // find all voxels each brick will touch
            for (int i = 0; i < allocations.Count; i++)
            {
                Chunk alloc = allocations[i];
                int cnt = Mathf.Min(allocationSize, bricks.Length - brick_idx);
                for (int j = 0; j < cnt; j++, brick_idx++, alloc.x += ProbeBrickPool.kBrickProbeCountPerDim)
                {
                    Brick brick = bricks[brick_idx];

                    int cellSize = ProbeReferenceVolume.CellSize(brick.subdivisionLevel);
                    Debug.Assert(cellSize <= largest_cell, "Cell sizes are not correctly sorted.");
                    largest_cell = Mathf.Min(largest_cell, cellSize);

                    MapBrickToVoxels(brick, bm.voxels);

                    ReservedBrick rbrick = new ReservedBrick();
                    rbrick.brick = brick;
                    rbrick.flattenedIdx = MergeIndex(alloc.flattenIndex(poolWidth, poolHeight), brick.subdivisionLevel);
                    bm.bricks.Add(rbrick);

                    foreach (var v in bm.voxels)
                    {
                        List<VoxelMeta> vm_list;
                        if (!m_VoxelToBricks.TryGetValue(v, out vm_list)) // first time the voxel is touched
                        {
                            vm_list = m_VoxelMetaListPool.Get();
                            m_VoxelToBricks.Add(v, vm_list);
                        }

                        VoxelMeta vm = null;
                        int vm_idx = vm_list.FindIndex((VoxelMeta lhs) => lhs.cell == g_Cell);
                        if (vm_idx == -1) // first time a brick from this id has touched this voxel
                        {
                            vm = m_VoxelMetaPool.Get();
                            vm.cell = cell;
                            vm_list.Add(vm);
                        }
                        else
                        {
                            vm = vm_list[vm_idx];
                        }

                        // add this brick to the voxel under its regId
                        vm.brickIndices.Add((ushort)brick_idx);
                    }
                }
            }

            foreach (var voxel in bm.voxels)
            {
                foreach (var entryInfo in cellInfo.entriesInfo)
                {
                    UpdateIndexForVoxel(voxel, entryInfo);
                }
            }
        }

        public void RemoveBricks(CellInfo cellInfo)
        {
            if (!m_BricksToVoxels.ContainsKey(cellInfo.cell))
                return;

            var cellUpdateInfo = cellInfo.updateInfo;

            g_Cell = cellInfo.cell;

            BrickMeta bm = m_BricksToVoxels[cellInfo.cell];
            foreach (var v in bm.voxels)
            {
                List<VoxelMeta> vm_list = m_VoxelToBricks[v];
                int idx = vm_list.FindIndex((VoxelMeta lhs) => lhs.cell == g_Cell);
                if (idx >= 0)
                {
                    m_VoxelMetaPool.Release(vm_list[idx]);
                    vm_list.RemoveAt(idx);

                    // Loop through entries.
                    foreach (var entryInfo in cellInfo.updateInfo.entriesInfo)
                    {
                        if (entryInfo.brickCount == 0) continue;

                        if (vm_list.Count > 0)
                        {
                            UpdateIndexForVoxel(v, entryInfo);
                        }
                        else
                        {
                            ClearVoxel(v, entryInfo);
                            m_VoxelMetaListPool.Release(vm_list);
                            m_VoxelToBricks.Remove(v);
                        }
                    }
                }
            }
            m_BrickMetaPool.Release(bm);
            m_BricksToVoxels.Remove(cellInfo.cell);

            // Clear allocated chunks
            foreach (var entryInfo in cellInfo.updateInfo.entriesInfo)
            {
                for (int i = entryInfo.firstChunkIndex; i < (entryInfo.firstChunkIndex + entryInfo.numberOfChunks); ++i)
                {
                    m_IndexChunks[i] = false;
                }
                m_AvailableChunkCount += entryInfo.numberOfChunks;
            }
        }

        void UpdateIndexForVoxel(Vector3Int voxel, IndirectionEntryUpdateInfo entryInfo)
        {
            if (entryInfo.brickCount == 0) return;

            ClearVoxel(voxel, entryInfo);
            List<VoxelMeta> vm_list = m_VoxelToBricks[voxel];
            foreach (var vm in vm_list)
            {
                // get the list of bricks and indices
                List<ReservedBrick> bricks = m_BricksToVoxels[vm.cell].bricks;
                List<ushort> indcs = vm.brickIndices;
                UpdateIndexForVoxel(voxel, bricks, indcs, entryInfo);
            }
        }

        void UpdatePhysicalIndex(Vector3Int brickMin, Vector3Int brickMax, int value, IndirectionEntryUpdateInfo entryInfo)
        {
            // An indirection entry might have only a single brick that is actually bigger than the entry itself. This is a bit of simpler use case
            // that doesn't fit what happens otherwise in this function where the assumption is that minSubdivInCell is equal or less the size of the entry.
            // In this special case, we can do things much simpler as we *know* by construction that we will always have only a single relevant brick
            // and so we do the update in a simpler fashion.
            if (entryInfo.hasOnlyBiggerBricks)
            {
                int singleEntry = entryInfo.firstChunkIndex * kIndexChunkSize;
                m_UpdateMinIndex = Math.Min(m_UpdateMinIndex, singleEntry);
                m_UpdateMaxIndex = Math.Max(m_UpdateMaxIndex, singleEntry);
                m_PhysicalIndexBufferData[singleEntry] = value;
                m_NeedUpdateIndexComputeBuffer = true;

                return;
            }

            // We need to do our calculations in local space to the cell, so we move the brick to local space as a first step.
            // Reminder that at this point we are still operating at highest resolution possible, not necessarily the one that will be
            // the final resolution for the chunk.
            brickMin = brickMin - entryInfo.entryPositionInBricksAtMaxRes;
            brickMax = brickMax - entryInfo.entryPositionInBricksAtMaxRes;

            // Since the index is spurious (not same resolution, but varying per cell) we need to bring to the output resolution the brick coordinates
            // Before finding the locations inside the Index for the current cell/chunk.

            brickMin /= ProbeReferenceVolume.CellSize(entryInfo.minSubdivInCell);
            brickMax /= ProbeReferenceVolume.CellSize(entryInfo.minSubdivInCell);

            // Verify we are actually in local space now.
            int maxCellSizeInOutputRes = ProbeReferenceVolume.CellSize(ProbeReferenceVolume.instance.GetEntrySubdivLevel() - entryInfo.minSubdivInCell);
            Debug.Assert(brickMin.x >= 0 && brickMin.y >= 0 && brickMin.z >= 0 && brickMax.x >= 0 && brickMax.y >= 0 && brickMax.z >= 0);
            Debug.Assert(brickMin.x <= maxCellSizeInOutputRes && brickMin.y <= maxCellSizeInOutputRes && brickMin.z <= maxCellSizeInOutputRes && brickMax.x <= maxCellSizeInOutputRes && brickMax.y <= maxCellSizeInOutputRes && brickMax.z <= maxCellSizeInOutputRes);

            // We are now in the right resolution, but still not considering the valid area, so we need to still normalize against that.
            // To do so first let's move back the limits to the desired resolution
            var cellMinIndex = entryInfo.minValidBrickIndexForCellAtMaxRes / ProbeReferenceVolume.CellSize(entryInfo.minSubdivInCell);
            var cellMaxIndex = entryInfo.maxValidBrickIndexForCellAtMaxResPlusOne / ProbeReferenceVolume.CellSize(entryInfo.minSubdivInCell);

            // Then perform the rescale of the local indices for min and max.
            brickMin -= cellMinIndex;
            brickMax -= cellMinIndex;

            // In theory now we are all positive since we clipped during the voxel stage. Keeping assert for debugging, but can go later.
            Debug.Assert(brickMin.x >= 0 && brickMin.y >= 0 && brickMin.z >= 0 && brickMax.x >= 0 && brickMax.y >= 0 && brickMax.z >= 0);


            // Compute the span of the valid part
            var size = (cellMaxIndex - cellMinIndex);

            // Analytically compute min and max because doing it in the inner loop with Math.Min/Max is costly (not inlined)
            int chunkStart = entryInfo.firstChunkIndex * kIndexChunkSize;
            int newMin = chunkStart + brickMin.z * (size.x * size.y) + brickMin.x * size.y + brickMin.y;
            int newMax = chunkStart + Math.Max(0, (brickMax.z - 1)) * (size.x * size.y) + Math.Max(0, (brickMax.x - 1)) * size.y + Math.Max(0, (brickMax.y - 1));
            m_UpdateMinIndex = Math.Min(m_UpdateMinIndex, newMin);
            m_UpdateMaxIndex = Math.Max(m_UpdateMaxIndex, newMax);

            // Loop through all touched indices
            for (int x = brickMin.x; x < brickMax.x; ++x)
            {
                for (int z = brickMin.z; z < brickMax.z; ++z)
                {
                    for (int y = brickMin.y; y < brickMax.y; ++y)
                    {
                        int localFlatIdx = z * (size.x * size.y) + x * size.y + y;
                        int actualIdx = chunkStart + localFlatIdx;
                        m_PhysicalIndexBufferData[actualIdx] = value;
                    }
                }
            }

            m_NeedUpdateIndexComputeBuffer = true;
        }

        void ClipToIndexSpace(Vector3Int pos, int subdiv, out Vector3Int outMinpos, out Vector3Int outMaxpos, IndirectionEntryUpdateInfo entryInfo)
        {
            // to relative coordinates
            int cellSize = ProbeReferenceVolume.CellSize(subdiv);

            // The position here is in global space, however we want to constraint this voxel update to the valid cell area
            var minValidPosition = entryInfo.entryPositionInBricksAtMaxRes + entryInfo.minValidBrickIndexForCellAtMaxRes;
            var maxValidPosition = entryInfo.entryPositionInBricksAtMaxRes + entryInfo.maxValidBrickIndexForCellAtMaxResPlusOne - Vector3Int.one;

            int minpos_x = pos.x - m_CenterRS.x;
            int minpos_y = pos.y;
            int minpos_z = pos.z - m_CenterRS.z;
            int maxpos_x = minpos_x + cellSize;
            int maxpos_y = minpos_y + cellSize;
            int maxpos_z = minpos_z + cellSize;
            // clip to valid region
            minpos_x = Mathf.Clamp(minpos_x, minValidPosition.x, maxValidPosition.x);
            minpos_y = Mathf.Clamp(minpos_y, minValidPosition.y, maxValidPosition.y);
            minpos_z = Mathf.Clamp(minpos_z, minValidPosition.z, maxValidPosition.z);
            maxpos_x = Mathf.Clamp(maxpos_x, minValidPosition.x, maxValidPosition.x);
            maxpos_y = Mathf.Clamp(maxpos_y, minValidPosition.y, maxValidPosition.y);
            maxpos_z = Mathf.Clamp(maxpos_z, minValidPosition.z, maxValidPosition.z);

            outMinpos = new Vector3Int(minpos_x, minpos_y, minpos_z);
            outMaxpos = new Vector3Int(maxpos_x, maxpos_y, maxpos_z);
        }

        bool BrickOverlapEntry(Vector3Int brickMin, Vector3Int brickMax, Vector3Int entryMin, Vector3Int entryMax)
        {
            return brickMax.x >= entryMin.x && entryMax.x >= brickMin.x &&
                   brickMax.y >= entryMin.y && entryMax.y >= brickMin.y &&
                   brickMax.z >= entryMin.z && entryMax.z >= brickMin.z;
        }

        void UpdateIndexForVoxel(Vector3Int voxel, List<ReservedBrick> bricks, List<ushort> indices, IndirectionEntryUpdateInfo entryInfo)
        {
            // clip voxel to index space
            Vector3Int vx_min, vx_max;
            ClipToIndexSpace(voxel, GetVoxelSubdivLevel(), out vx_min, out vx_max, entryInfo);

            var minEntryPosition = entryInfo.entryPositionInBricksAtMaxRes + entryInfo.minValidBrickIndexForCellAtMaxRes;
            var maxEntryPosition = entryInfo.entryPositionInBricksAtMaxRes + entryInfo.maxValidBrickIndexForCellAtMaxResPlusOne - Vector3Int.one;

            foreach (var rbrick in bricks)
            {
                // clip brick to clipped voxel
                int brick_cell_size = ProbeReferenceVolume.CellSize(rbrick.brick.subdivisionLevel);
                Vector3Int brick_min = rbrick.brick.position;
                Vector3Int brick_max = rbrick.brick.position + Vector3Int.one * brick_cell_size;

                // We might have the brick completely out of the entry. In those case, we must skip.
                if (!BrickOverlapEntry(brick_min, brick_max, minEntryPosition, maxEntryPosition)) continue;

                brick_min.x = Mathf.Max(vx_min.x, brick_min.x - m_CenterRS.x);
                brick_min.y = Mathf.Max(vx_min.y, brick_min.y);
                brick_min.z = Mathf.Max(vx_min.z, brick_min.z - m_CenterRS.z);
                brick_max.x = Mathf.Min(vx_max.x, brick_max.x - m_CenterRS.x);
                brick_max.y = Mathf.Min(vx_max.y, brick_max.y);
                brick_max.z = Mathf.Min(vx_max.z, brick_max.z - m_CenterRS.z);

                UpdatePhysicalIndex(brick_min, brick_max, rbrick.flattenedIdx, entryInfo);
            }
        }
    }
}
