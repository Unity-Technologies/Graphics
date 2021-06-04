//#define USE_INDEX_NATIVE_ARRAY
using System;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Chunk = UnityEngine.Experimental.Rendering.ProbeBrickPool.BrickChunkAlloc;
using RegId = UnityEngine.Experimental.Rendering.ProbeReferenceVolume.RegId;

namespace UnityEngine.Experimental.Rendering
{
    internal class ProbeBrickIndex
    {
        // a few constants
        internal const int kMaxSubdivisionLevels = 7; // 3 bits

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
            public int   flattenedIdx;
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

        ComputeBuffer   m_IndexBuffer;
        int[]           m_IndexBufferData;
        Vector3Int      m_IndexDim;
        Vector3Int      m_CenterRS;   // the anchor in ref space, around which the index is defined. [IMPORTANT NOTE! For now we always have it at 0, so is not passed to the shader, but is kept here until development is active in case we find it useful]
        Vector3Int      m_CenterIS;   // the position in index space that the anchor maps to [IMPORTANT NOTE! For now we always have it at indexDimensions / 2, so is not passed to the shader, but is kept here until development is active in case we find it useful]

#if !USE_NATIVE_ARRAY
        int[] m_TmpUpdater = new int[ProbeReferenceVolume.CellSize(kMaxSubdivisionLevels)];
#endif
        Dictionary<Vector3Int, List<VoxelMeta>> m_VoxelToBricks;
        Dictionary<RegId, BrickMeta>            m_BricksToVoxels;
        int                                     m_VoxelSubdivLevel = 3;

        bool m_NeedUpdateIndexComputeBuffer;

        internal Vector3Int GetIndexDimension() { return m_IndexDim; }

        internal ProbeBrickIndex(Vector3Int indexDimensions)
        {
            Profiler.BeginSample("Create ProbeBrickIndex");
            int index_size = indexDimensions.x * indexDimensions.y * indexDimensions.z;
            m_CenterRS     = new Vector3Int(0, 0, 0);
            m_IndexDim     = indexDimensions;
            m_CenterIS     = indexDimensions / 2;

            m_VoxelToBricks = new Dictionary<Vector3Int, List<VoxelMeta>>();
            m_BricksToVoxels = new Dictionary<RegId, BrickMeta>();

#if USE_INDEX_NATIVE_ARRAY
            m_IndexBuffer = new ComputeBuffer(index_size, sizeof(int), ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
#else
            m_IndexBuffer = new ComputeBuffer(index_size, sizeof(int), ComputeBufferType.Structured);
#endif
            m_IndexBufferData = new int[index_size];
            m_NeedUpdateIndexComputeBuffer = false;
            // Should be done by a compute shader
            Clear();
            Profiler.EndSample();
        }

        void UpdateIndexData(int[] data, int dataStartIndex, int dstStartIndex, int count)
        {
            Debug.Assert(count <= data.Length);
            Debug.Assert(m_IndexBufferData.Length >= dstStartIndex + count);
            Array.Copy(data, dataStartIndex, m_IndexBufferData, dstStartIndex, count);

            // We made some modifications, we need to update the compute buffer before is used.
            m_NeedUpdateIndexComputeBuffer = true;
        }

        internal void UploadIndexData()
        {
            m_IndexBuffer.SetData(m_IndexBufferData);
            m_NeedUpdateIndexComputeBuffer = false;
        }

        internal void Clear()
        {
            Profiler.BeginSample("Clear Index");
            int index_size = m_IndexDim.x * m_IndexDim.y * m_IndexDim.z;
#if USE_INDEX_NATIVE_ARRAY
            NativeArray<int> arr = m_IndexBuffer.BeginWrite<int>(0, index_size);
            for (int i = 0; i < index_size; i++)
                arr[i] = -1;
            m_IndexBuffer.EndWrite<int>(index_size);
#else
            for (int i = 0; i < m_TmpUpdater.Length; i++)
                m_TmpUpdater[i] = -1;

            // TODO: Replace with Array.Fill when available.
            for (int i = 0; i < m_IndexBuffer.count; i += m_TmpUpdater.Length)
                UpdateIndexData(m_TmpUpdater, 0, i, Mathf.Min(m_TmpUpdater.Length, m_IndexBuffer.count - i));
#endif
            m_VoxelToBricks.Clear();
            m_BricksToVoxels.Clear();

            Profiler.EndSample();
        }

        public void AddBricks(RegId id, List<Brick> bricks, List<Chunk> allocations, int allocationSize, int poolWidth, int poolHeight)
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
                UpdateIndexForVoxel(voxel);
            }
        }

        public void RemoveBricks(RegId id)
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
                        UpdateIndexForVoxel(v);
                    }
                    else
                    {
                        ClearVoxel(v);
                        m_VoxelToBricks.Remove(v);
                    }
                }
            }
            m_BricksToVoxels.Remove(id);
        }

        void MapBrickToVoxels(ProbeBrickIndex.Brick brick, HashSet<Vector3Int> voxels)
        {
            // create a list of all voxels this brick will touch
            int brick_subdiv = brick.subdivisionLevel;
            int voxels_touched_cnt = (int)Mathf.Pow(3, Mathf.Max(0, brick_subdiv - m_VoxelSubdivLevel));

            Vector3Int ipos = brick.position;
            int        brick_size = ProbeReferenceVolume.CellSize(brick.subdivisionLevel);
            int        voxel_size = ProbeReferenceVolume.CellSize(m_VoxelSubdivLevel);

            if (voxels_touched_cnt <= 1)
            {
                Vector3 pos  = brick.position;
                pos  = pos * (1.0f / voxel_size);
                ipos = new Vector3Int(Mathf.FloorToInt(pos.x) * voxel_size, Mathf.FloorToInt(pos.y) * voxel_size, Mathf.FloorToInt(pos.z) * voxel_size);
            }

            for (int z = ipos.z; z < ipos.z + brick_size; z += voxel_size)
                for (int y = ipos.y; y < ipos.y + brick_size; y += voxel_size)
                    for (int x = ipos.x; x < ipos.x + brick_size; x += voxel_size)
                    {
                        voxels.Add(new Vector3Int(x, y, z));
                    }
        }

        void UpdateIndexForVoxel(Vector3Int voxel)
        {
            ClearVoxel(voxel);
            List<VoxelMeta> vm_list = m_VoxelToBricks[voxel];
            foreach (var vm in vm_list)
            {
                // get the list of bricks and indices
                List<ReservedBrick> bricks = m_BricksToVoxels[vm.id].bricks;
                List<ushort>        indcs = vm.brickIndices;
                UpdateIndexForVoxel(voxel, bricks, indcs);
            }
        }

        void ClearVoxel(Vector3Int pos)
        {
            // clip voxel to index space
            Vector3Int volMin, volMax;
            ClipToIndexSpace(pos, m_VoxelSubdivLevel, out volMin, out volMax);

            Vector3Int bSize = volMax - volMin;
            Vector3Int posIS = m_CenterIS + volMin;

            UpdateIndexData(posIS, bSize, -1);
        }

        void UpdateIndexForVoxel(Vector3Int voxel, List<ReservedBrick> bricks, List<ushort> indices)
        {
            // clip voxel to index space
            Vector3Int vx_min, vx_max;
            ClipToIndexSpace(voxel, m_VoxelSubdivLevel, out vx_min, out vx_max);

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

                Vector3Int bSize = brick_max - brick_min;
                Vector3Int posIS = m_CenterIS + brick_min;

                UpdateIndexData(posIS, bSize, rbrick.flattenedIdx);
            }
        }

        void UpdateIndexData(in Vector3Int pos, in Vector3Int size, int value)
        {
            if (size.x <= 0 || size.y <= 0 || size.z <= 0)
                return;

            for (int z = 0; z < size.z; z++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int x = 0; x < size.x; x++)
                    {
                        int mx = (pos.x + x) % m_IndexDim.x;
                        int my = (pos.y + y) % m_IndexDim.y;
                        int mz = (pos.z + z) % m_IndexDim.z;
                        int indexTrans = TranslateIndex(mx, my, mz);
                        m_IndexBufferData[indexTrans] = value;
                    }
                }
            }

            m_NeedUpdateIndexComputeBuffer = true;
        }

        void ClipToIndexSpace(Vector3Int pos, int subdiv, out Vector3Int outMinpos, out Vector3Int outMaxpos)
        {
            // to relative coordinates
            int cellSize = ProbeReferenceVolume.CellSize(subdiv);

            int minpos_x = pos.x - m_CenterRS.x;
            int minpos_y = pos.y;
            int minpos_z = pos.z - m_CenterRS.z;
            int maxpos_x = minpos_x + cellSize;
            int maxpos_y = minpos_y + cellSize;
            int maxpos_z = minpos_z + cellSize;
            // clip to index region
            minpos_x = Mathf.Max(minpos_x, -m_IndexDim.x / 2);
            minpos_y = Mathf.Max(minpos_y, -m_IndexDim.y / 2);
            minpos_z = Mathf.Max(minpos_z, -m_IndexDim.z / 2);
            maxpos_x = Mathf.Min(maxpos_x,  m_IndexDim.x / 2);
            maxpos_y = Mathf.Min(maxpos_y,  m_IndexDim.y / 2);
            maxpos_z = Mathf.Min(maxpos_z,  m_IndexDim.z / 2);

            outMinpos = new Vector3Int(minpos_x, minpos_y, minpos_z);
            outMaxpos = new Vector3Int(maxpos_x, maxpos_y, maxpos_z);
        }

        int TranslateIndex(int posX, int posY, int posZ)
        {
            return posZ * (m_IndexDim.x * m_IndexDim.y) + posX * m_IndexDim.y + posY;
        }

        int MergeIndex(int index, int size)
        {
            const int mask = kMaxSubdivisionLevels;
            const int shift = 28;
            return (index & ~(mask << shift)) | ((size & mask) << shift);
        }

        internal void GetRuntimeResources(ref ProbeReferenceVolume.RuntimeResources rr)
        {
            // If we are pending an update of the actual compute buffer we do it here
            if (m_NeedUpdateIndexComputeBuffer)
            {
                UploadIndexData();
            }
            rr.index = m_IndexBuffer;
        }

        internal void Cleanup()
        {
            CoreUtils.SafeRelease(m_IndexBuffer);
            m_IndexBuffer = null;
        }
    }
}
