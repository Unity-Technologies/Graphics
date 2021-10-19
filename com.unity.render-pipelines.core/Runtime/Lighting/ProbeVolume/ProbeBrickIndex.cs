//#define USE_INDEX_NATIVE_ARRAY
using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using System.Collections;
using Chunk = UnityEngine.Experimental.Rendering.ProbeBrickPool.BrickChunkAlloc;
using RegId = UnityEngine.Experimental.Rendering.ProbeReferenceVolume.RegId;

namespace UnityEngine.Experimental.Rendering
{
    internal class ProbeBrickIndex
    {
        // a few constants
        internal const int kMaxSubdivisionLevels = 7; // 3 bits
        internal const int kIndexChunkSize = 243;

        BitArray m_IndexChunks;
        int m_IndexInChunks;
        int m_NextFreeChunk;

        ComputeBuffer m_PhysicalIndexBuffer;
        int[] m_PhysicalIndexBufferData;

        internal int estimatedVMemCost { get; private set; }

        [DebuggerDisplay("Brick [{position}, {subdivisionLevel}]")]
        [Serializable]

        public struct Brick : IEquatable<Brick>
        {
            public Vector3Int position;   // refspace index, indices are cell coordinates at max resolution
            public int subdivisionLevel;              // size as factor covered elementary cells

            internal Brick(Vector3Int position, int subdivisionLevel)
            {
                this.position = position;
                this.subdivisionLevel = subdivisionLevel;
            }

            public bool Equals(Brick other) => position == other.position && subdivisionLevel == other.subdivisionLevel;
        }

        [DebuggerDisplay("Brick [{brick.position}, {brick.subdivisionLevel}], {flattenedIdx}")]
        struct ReservedBrick
        {
            public Brick brick;
            public int flattenedIdx;
        }

        struct VoxelMeta
        {
            public RegId id;
            public List<ushort> brickIndices;
        }

        struct BrickMeta
        {
            public HashSet<Vector3Int> voxels;
            public List<ReservedBrick> bricks;
        }

        Vector3Int m_CenterRS;   // the anchor in ref space, around which the index is defined. [IMPORTANT NOTE! For now we always have it at 0, so is not passed to the shader, but is kept here until development is active in case we find it useful]

        Dictionary<Vector3Int, List<VoxelMeta>> m_VoxelToBricks;
        Dictionary<RegId, BrickMeta> m_BricksToVoxels;

        int GetVoxelSubdivLevel()
        {
            int defaultVoxelSubdivLevel = 3;
            return Mathf.Min(defaultVoxelSubdivLevel, ProbeReferenceVolume.instance.GetMaxSubdivision() - 1);
        }

        bool m_NeedUpdateIndexComputeBuffer;

        int SizeOfPhysicalIndexFromBudget(ProbeVolumeTextureMemoryBudget memoryBudget)
        {
            switch (memoryBudget)
            {
                case ProbeVolumeTextureMemoryBudget.MemoryBudgetLow:
                    // 16 MB - 4 million of bricks worth of space. At full resolution and a distance of 1 meter between probes, this is roughly 474 * 474 * 474 meters worth of bricks. If 0.25x on Y axis, this is equivalent to 948 * 118 * 948 meters
                    return 16000000;
                case ProbeVolumeTextureMemoryBudget.MemoryBudgetMedium:
                    // 32 MB - 8 million of bricks worth of space. At full resolution and a distance of 1 meter between probes, this is roughly 600 * 600 * 600 meters worth of bricks. If 0.25x on Y axis, this is equivalent to 1200 * 150 * 1200 meters
                    return 32000000;
                case ProbeVolumeTextureMemoryBudget.MemoryBudgetHigh:
                    // 64 MB - 16 million of bricks worth of space. At full resolution and a distance of 1 meter between probes, this is roughly 756 * 756 * 756 meters worth of bricks. If 0.25x on Y axis, this is equivalent to 1512 * 184 * 1512 meters
                    return 64000000;
            }
            return 32000000;
        }

        internal ProbeBrickIndex(ProbeVolumeTextureMemoryBudget memoryBudget)
        {
            Profiler.BeginSample("Create ProbeBrickIndex");
            m_CenterRS = new Vector3Int(0, 0, 0);

            m_VoxelToBricks = new Dictionary<Vector3Int, List<VoxelMeta>>();
            m_BricksToVoxels = new Dictionary<RegId, BrickMeta>();

            m_NeedUpdateIndexComputeBuffer = false;

            m_IndexInChunks = Mathf.CeilToInt((float)SizeOfPhysicalIndexFromBudget(memoryBudget) / kIndexChunkSize);
            m_IndexChunks = new BitArray(Mathf.Max(1, m_IndexInChunks));
            int physicalBufferSize = m_IndexInChunks * kIndexChunkSize;
            m_PhysicalIndexBufferData = new int[physicalBufferSize];
            m_PhysicalIndexBuffer = new ComputeBuffer(physicalBufferSize, sizeof(int), ComputeBufferType.Structured);
            m_NextFreeChunk = 0;

            estimatedVMemCost = physicalBufferSize * sizeof(int);

            // Should be done by a compute shader
            Clear();
            Profiler.EndSample();
        }

        internal void UploadIndexData()
        {
            m_PhysicalIndexBuffer.SetData(m_PhysicalIndexBufferData);
            m_NeedUpdateIndexComputeBuffer = false;
        }

        internal void Clear()
        {
            Profiler.BeginSample("Clear Index");

            for (int i = 0; i < m_PhysicalIndexBufferData.Length; ++i)
                m_PhysicalIndexBufferData[i] = -1;

            m_NeedUpdateIndexComputeBuffer = true;
            m_NextFreeChunk = 0;
            m_IndexChunks.SetAll(false);

            m_VoxelToBricks.Clear();
            m_BricksToVoxels.Clear();
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

        void ClearVoxel(Vector3Int pos, CellIndexUpdateInfo cellInfo)
        {
            Vector3Int vx_min, vx_max;
            ClipToIndexSpace(pos, GetVoxelSubdivLevel(), out vx_min, out vx_max, cellInfo);
            UpdatePhysicalIndex(vx_min, vx_max, -1, cellInfo);
        }

        internal void GetRuntimeResources(ref ProbeReferenceVolume.RuntimeResources rr)
        {
            // If we are pending an update of the actual compute buffer we do it here
            if (m_NeedUpdateIndexComputeBuffer)
            {
                UploadIndexData();
            }
            rr.index = m_PhysicalIndexBuffer;
        }

        internal void Cleanup()
        {
            CoreUtils.SafeRelease(m_PhysicalIndexBuffer);
            m_PhysicalIndexBuffer = null;
        }

        public struct CellIndexUpdateInfo
        {
            public int firstChunkIndex;
            public int numberOfChunks;
            public int minSubdivInCell;
            // IMPORTANT, These values should be at max resolution. This means that
            // The map to the lower possible resolution is done after.  However they are still in local space.
            public Vector3Int minValidBrickIndexForCellAtMaxRes;
            public Vector3Int maxValidBrickIndexForCellAtMaxResPlusOne;
            public Vector3Int cellPositionInBricksAtMaxRes;
        }

        int MergeIndex(int index, int size)
        {
            const int mask = kMaxSubdivisionLevels;
            const int shift = 28;
            return (index & ~(mask << shift)) | ((size & mask) << shift);
        }

        internal bool AssignIndexChunksToCell(ProbeReferenceVolume.Cell cell, int bricksCount, ref CellIndexUpdateInfo cellUpdateInfo)
        {
            // We need to better handle the case where the chunks are full, this is where streaming will need to come into place swapping in/out
            // Also the current way to find an empty spot might be sub-optimal, when streaming is in place it'd be nice to have this more efficient
            // if it is meant to happen frequently.

            int numberOfChunks = Mathf.CeilToInt((float)bricksCount / kIndexChunkSize);

            // Search for the first empty element with enough space.
            int firstValidChunk = -1;
            for (int i = 0; i < m_IndexInChunks; ++i)
            {
                if (!m_IndexChunks[i] && (i + numberOfChunks) < m_IndexInChunks)
                {
                    int emptySlotsStartingHere = 0;
                    for (int k = i; k < (i + numberOfChunks); ++k)
                    {
                        if (!m_IndexChunks[k]) emptySlotsStartingHere++;
                        else break;
                    }

                    if (emptySlotsStartingHere == numberOfChunks)
                    {
                        firstValidChunk = i;
                        break;
                    }
                }
            }

            if (firstValidChunk < 0) return false;

            // This assert will need to go away or do something else when streaming is allowed (we need to find holes in available chunks or stream out stuff)
            cellUpdateInfo.firstChunkIndex = firstValidChunk;
            cellUpdateInfo.numberOfChunks = numberOfChunks;
            for (int i = firstValidChunk; i < (firstValidChunk + numberOfChunks); ++i)
            {
                Debug.Assert(!m_IndexChunks[i]);
                m_IndexChunks[i] = true;
            }

            m_NextFreeChunk += Mathf.Max(0, (firstValidChunk + numberOfChunks) - m_NextFreeChunk);

            return true;
        }

        public void AddBricks(RegId id, List<Brick> bricks, List<Chunk> allocations, int allocationSize, int poolWidth, int poolHeight, CellIndexUpdateInfo cellInfo)
        {
            Debug.Assert(bricks.Count <= ushort.MaxValue, "Cannot add more than 65K bricks per RegId.");
            int largest_cell = ProbeReferenceVolume.CellSize(kMaxSubdivisionLevels);

            // create a new copy
            BrickMeta bm = new BrickMeta();
            bm.voxels = new HashSet<Vector3Int>();
            bm.bricks = new List<ReservedBrick>(bricks.Count);
            m_BricksToVoxels.Add(id, bm);

            int brick_idx = 0;
            // find all voxels each brick will touch
            for (int i = 0; i < allocations.Count; i++)
            {
                Chunk alloc = allocations[i];
                int cnt = Mathf.Min(allocationSize, bricks.Count - brick_idx);
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
                            vm_list = new List<VoxelMeta>(1);
                            m_VoxelToBricks.Add(v, vm_list);
                        }

                        VoxelMeta vm;
                        int vm_idx = vm_list.FindIndex((VoxelMeta lhs) => lhs.id == id);
                        if (vm_idx == -1) // first time a brick from this id has touched this voxel
                        {
                            vm.id = id;
                            vm.brickIndices = new List<ushort>(4);
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
                UpdateIndexForVoxel(voxel, cellInfo);
            }
        }

        public void RemoveBricks(RegId id, CellIndexUpdateInfo cellInfo)
        {
            if (!m_BricksToVoxels.ContainsKey(id))
                return;

            BrickMeta bm = m_BricksToVoxels[id];
            foreach (var v in bm.voxels)
            {
                List<VoxelMeta> vm_list = m_VoxelToBricks[v];
                int idx = vm_list.FindIndex((VoxelMeta lhs) => lhs.id == id);
                if (idx >= 0)
                {
                    vm_list.RemoveAt(idx);
                    if (vm_list.Count > 0)
                    {
                        UpdateIndexForVoxel(v, cellInfo);
                    }
                    else
                    {
                        ClearVoxel(v, cellInfo);
                        m_VoxelToBricks.Remove(v);
                    }
                }
            }
            m_BricksToVoxels.Remove(id);

            // Clear allocated chunks
            for (int i = cellInfo.firstChunkIndex; i < (cellInfo.firstChunkIndex + cellInfo.numberOfChunks); ++i)
            {
                m_IndexChunks[i] = false;
            }
        }

        void UpdateIndexForVoxel(Vector3Int voxel, CellIndexUpdateInfo cellInfo)
        {
            ClearVoxel(voxel, cellInfo);
            List<VoxelMeta> vm_list = m_VoxelToBricks[voxel];
            foreach (var vm in vm_list)
            {
                // get the list of bricks and indices
                List<ReservedBrick> bricks = m_BricksToVoxels[vm.id].bricks;
                List<ushort> indcs = vm.brickIndices;
                UpdateIndexForVoxel(voxel, bricks, indcs, cellInfo);
            }
        }

        void UpdatePhysicalIndex(Vector3Int brickMin, Vector3Int brickMax, int value, CellIndexUpdateInfo cellInfo)
        {
            // We need to do our calculations in local space to the cell, so we move the brick to local space as a first step.
            // Reminder that at this point we are still operating at highest resolution possible, not necessarily the one that will be
            // the final resolution for the chunk.
            brickMin = brickMin - cellInfo.cellPositionInBricksAtMaxRes;
            brickMax = brickMax - cellInfo.cellPositionInBricksAtMaxRes;

            // Since the index is spurious (not same resolution, but varying per cell) we need to bring to the output resolution the brick coordinates
            // Before finding the locations inside the Index for the current cell/chunk.

            brickMin /= ProbeReferenceVolume.CellSize(cellInfo.minSubdivInCell);
            brickMax /= ProbeReferenceVolume.CellSize(cellInfo.minSubdivInCell);

            // Verify we are actually in local space now.
            int maxCellSizeInOutputRes = ProbeReferenceVolume.CellSize(ProbeReferenceVolume.instance.GetMaxSubdivision() - 1 - cellInfo.minSubdivInCell);
            Debug.Assert(brickMin.x >= 0 && brickMin.y >= 0 && brickMin.z >= 0 && brickMax.x >= 0 && brickMax.y >= 0 && brickMax.z >= 0);
            Debug.Assert(brickMin.x < maxCellSizeInOutputRes && brickMin.y < maxCellSizeInOutputRes && brickMin.z < maxCellSizeInOutputRes && brickMax.x <= maxCellSizeInOutputRes && brickMax.y <= maxCellSizeInOutputRes && brickMax.z <= maxCellSizeInOutputRes);

            // We are now in the right resolution, but still not considering the valid area, so we need to still normalize against that.
            // To do so first let's move back the limits to the desired resolution
            var cellMinIndex = cellInfo.minValidBrickIndexForCellAtMaxRes / ProbeReferenceVolume.CellSize(cellInfo.minSubdivInCell);
            var cellMaxIndex = cellInfo.maxValidBrickIndexForCellAtMaxResPlusOne / ProbeReferenceVolume.CellSize(cellInfo.minSubdivInCell);

            // Then perform the rescale of the local indices for min and max.
            brickMin -= cellMinIndex;
            brickMax -= cellMinIndex;

            // In theory now we are all positive since we clipped during the voxel stage. Keeping assert for debugging, but can go later.
            Debug.Assert(brickMin.x >= 0 && brickMin.y >= 0 && brickMin.z >= 0 && brickMax.x >= 0 && brickMax.y >= 0 && brickMax.z >= 0);


            // Compute the span of the valid part
            var size = (cellMaxIndex - cellMinIndex);

            // Loop through all touched indices
            int chunkStart = cellInfo.firstChunkIndex * kIndexChunkSize;
            for (int z = brickMin.z; z < brickMax.z; ++z)
            {
                for (int y = brickMin.y; y < brickMax.y; ++y)
                {
                    for (int x = brickMin.x; x < brickMax.x; ++x)
                    {
                        int localFlatIdx = z * (size.x * size.y) + x * size.y + y;
                        int actualIdx = chunkStart + localFlatIdx;
                        m_PhysicalIndexBufferData[actualIdx] = value;
                    }
                }
            }

            m_NeedUpdateIndexComputeBuffer = true;
        }

        void ClipToIndexSpace(Vector3Int pos, int subdiv, out Vector3Int outMinpos, out Vector3Int outMaxpos, CellIndexUpdateInfo cellInfo)
        {
            // to relative coordinates
            int cellSize = ProbeReferenceVolume.CellSize(subdiv);

            // The position here is in global space, however we want to constraint this voxel update to the valid cell area
            var minValidPosition = cellInfo.cellPositionInBricksAtMaxRes + cellInfo.minValidBrickIndexForCellAtMaxRes;
            var maxValidPosition = cellInfo.cellPositionInBricksAtMaxRes + cellInfo.maxValidBrickIndexForCellAtMaxResPlusOne - Vector3Int.one;

            int minpos_x = pos.x - m_CenterRS.x;
            int minpos_y = pos.y;
            int minpos_z = pos.z - m_CenterRS.z;
            int maxpos_x = minpos_x + cellSize;
            int maxpos_y = minpos_y + cellSize;
            int maxpos_z = minpos_z + cellSize;
            // clip to valid region
            minpos_x = Mathf.Max(minpos_x, minValidPosition.x);
            minpos_y = Mathf.Max(minpos_y, minValidPosition.y);
            minpos_z = Mathf.Max(minpos_z, minValidPosition.z);
            maxpos_x = Mathf.Min(maxpos_x, maxValidPosition.x);
            maxpos_y = Mathf.Min(maxpos_y, maxValidPosition.y);
            maxpos_z = Mathf.Min(maxpos_z, maxValidPosition.z);

            outMinpos = new Vector3Int(minpos_x, minpos_y, minpos_z);
            outMaxpos = new Vector3Int(maxpos_x, maxpos_y, maxpos_z);
        }

        void UpdateIndexForVoxel(Vector3Int voxel, List<ReservedBrick> bricks, List<ushort> indices, CellIndexUpdateInfo cellInfo)
        {
            // clip voxel to index space
            Vector3Int vx_min, vx_max;
            ClipToIndexSpace(voxel, GetVoxelSubdivLevel(), out vx_min, out vx_max, cellInfo);

            foreach (var rbrick in bricks)
            {
                // clip brick to clipped voxel
                int brick_cell_size = ProbeReferenceVolume.CellSize(rbrick.brick.subdivisionLevel);
                Vector3Int brick_min = rbrick.brick.position;
                Vector3Int brick_max = rbrick.brick.position + Vector3Int.one * brick_cell_size;
                brick_min.x = Mathf.Max(vx_min.x, brick_min.x - m_CenterRS.x);
                brick_min.y = Mathf.Max(vx_min.y, brick_min.y);
                brick_min.z = Mathf.Max(vx_min.z, brick_min.z - m_CenterRS.z);
                brick_max.x = Mathf.Min(vx_max.x, brick_max.x - m_CenterRS.x);
                brick_max.y = Mathf.Min(vx_max.y, brick_max.y);
                brick_max.z = Mathf.Min(vx_max.z, brick_max.z - m_CenterRS.z);

                UpdatePhysicalIndex(brick_min, brick_max, rbrick.flattenedIdx, cellInfo);
            }
        }
    }
}
