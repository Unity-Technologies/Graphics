//#define USE_INDEX_NATIVE_ARRAY
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Profiling;
using Chunk = UnityEngine.Rendering.HighDefinition.ProbeBrickPool.BrickChunkAlloc;

namespace UnityEngine.Rendering.HighDefinition
{
    public class ProbeBrickIndex
    {
        // a few constants
        internal const int kMaxSubdivisionLevels = 15; // 4 bits
        private  const int kAPVConstantsSize = 12 + 1 + 3 + 3 + 3 + 3;

        public struct Brick
        {
            public Vector3Int position;   // refspace index, indices are cell coordinates at max resolution
            public int size;              // size as factor covered elementary cells

            internal Brick(Vector3Int position, int size)
            {
                this.position = position;
                this.size = size;
            }
        }

        private struct HeightRange
        {
            public int min;
            public int cnt;
        }

        private ComputeBuffer m_IndexBuffer;
        private Vector3Int    m_IndexDim;
        private Vector3Int    m_CenterRS;   // the anchor in ref space, around which the index is defined
        private Vector3Int    m_CenterIS;   // the position in index space that the anchor maps to
        private HeightRange[] m_HeightRanges;
#if !USE_NATIVE_ARRAY
        private int[]         m_TmpUpdater = new int[Mathf.Max( kAPVConstantsSize, ProbeReferenceVolume.cellSize(15) + 1)];
#endif

        internal ProbeBrickIndex( Vector3Int indexDimensions )
        {
            Profiler.BeginSample("Create ProbeBrickIndex");
            int index_size = kAPVConstantsSize + indexDimensions.x * (indexDimensions.y + 1) * indexDimensions.z;
            m_CenterRS     = new Vector3Int(0, 0, 0);
            m_IndexDim     = indexDimensions;
            m_CenterIS     = indexDimensions / 2;
#if USE_INDEX_NATIVE_ARRAY
            m_IndexBuffer = new ComputeBuffer(index_size, sizeof(int), ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
#else
            m_IndexBuffer = new ComputeBuffer(index_size, sizeof(int), ComputeBufferType.Structured);
#endif
            m_HeightRanges = new HeightRange[indexDimensions.x * indexDimensions.z];
            // Should be done by a compute shader
            Clear();
            Profiler.EndSample();
        }

        internal void Clear()
        {
            Profiler.BeginSample("Clear Index");
            int index_size = kAPVConstantsSize + m_IndexDim.x * (m_IndexDim.y + 1) * m_IndexDim.z;
#if USE_INDEX_NATIVE_ARRAY
            NativeArray<int> arr = m_IndexBuffer.BeginWrite<int>(0, index_size);
            for (int i = 0; i < index_size; i++)
                arr[i] = -1;
            m_IndexBuffer.EndWrite<int>(index_size);
#else
            for (int i = 0; i < m_TmpUpdater.Length; i++)
                m_TmpUpdater[i] = -1;

            for (int i = 0; i < m_IndexBuffer.count; i += m_TmpUpdater.Length)
                m_IndexBuffer.SetData(m_TmpUpdater, 0, i, Mathf.Min(m_TmpUpdater.Length, m_IndexBuffer.count - i));
#endif

            HeightRange hr = new HeightRange() { min = -1, cnt = 0 };
            for (int i = 0; i < m_HeightRanges.Length; i++)
                m_HeightRanges[i] = hr;

            Profiler.EndSample();
        }

        internal void AddBricks2( List<Brick> bricks, List<Chunk> allocations, int allocationSize, int poolWidth, int poolHeight )
        {
            int base_offset = kAPVConstantsSize + m_IndexDim.x * m_IndexDim.z;
            int largest_cell = ProbeReferenceVolume.cellSize( 15 );
            int brickIdx = 0;
            for( int j = 0; j < allocations.Count; j++ )
            {
                Chunk alloc = allocations[j];
                int count = Mathf.Min(allocationSize, bricks.Count - brickIdx);

                for (int i = 0; i < count; i++, brickIdx++, alloc.x += ProbeBrickPool.kBrickProbeCountPerDim)
                {
                    // brick data
                    Brick      b        = bricks[brickIdx];
                    int        cellSize = ProbeReferenceVolume.cellSize(b.size);
                    Vector3Int minpos   = b.position - m_CenterRS;
                    Vector3Int maxpos   = minpos + new Vector3Int(cellSize, cellSize, cellSize);

                    Debug.Assert(cellSize <= largest_cell, "Cell sizes are not correctly sorted.");
                    largest_cell = Mathf.Min(largest_cell, cellSize);

                    // clip to index region
                    minpos.x = Mathf.Max(minpos.x, -m_IndexDim.x / 2);
                    minpos.y = Mathf.Max(minpos.y, -m_IndexDim.y / 2);
                    minpos.z = Mathf.Max(minpos.z, -m_IndexDim.z / 2);
                    maxpos.x = Mathf.Min(maxpos.x,  m_IndexDim.x / 2);
                    maxpos.y = Mathf.Min(maxpos.y,  m_IndexDim.y / 2);
                    maxpos.z = Mathf.Min(maxpos.z,  m_IndexDim.z / 2);
                    Vector3Int bsize = maxpos - minpos;

                    if( bsize.x <= 0 || bsize.y <= 0 || bsize.z <= 0 )
                    {
                        Debug.Log("APV: Tried to add a brick that lies outside the range covered by the brick index. Ignoring brick.");
                        continue;
                    }

                    // chunk data
                    int poolIdx = MergeIndex(alloc.flattenIndex(poolWidth, poolHeight), b.size);

                    Vector3Int posIS = m_CenterIS + minpos;
                    for( int z = 0; z < bsize.z; z++ )
                    {
                        for( int x = 0; x < bsize.x; x++ )
                        {
                            if (bsize.y <= 0)
                                continue;


                            int mz = (posIS.z + z) % m_IndexDim.z;
                            int my = (posIS.y + 0) % m_IndexDim.y;
                            int mx = (posIS.x + x) % m_IndexDim.x;

                            // y wraps around
                            int ymax = Mathf.Min(my + bsize.y, m_IndexDim.y);

#if USE_INDEX_NATIVE_ARRAY
                            NativeArray<int> dst = m_IndexBuffer.BeginWrite<int>(base_offset + TranslateIndex(new Vector3Int( mx, my, mz) ), ymax - my);
                            for (int idx = 0; idx < ymax - my; idx++)
                                dst[idx] = poolIdx;
                            m_IndexBuffer.EndWrite<int>(ymax - my);
                            int remainder = bsize.y - (ymax - my);
                            if( remainder > 0 )
                            {
                                NativeArray<int> dst2 = m_IndexBuffer.BeginWrite<int>(base_offset + TranslateIndex(new Vector3Int(mx, 0, mz)), remainder);
                                for (int idx = 0; idx < remainder; idx++)
                                    dst2[idx] = poolIdx;
                                m_IndexBuffer.EndWrite<int>(remainder);
                            }                            
#else
                            for (int idx = 0; idx < bsize.y; idx++)
                                m_TmpUpdater[idx] = poolIdx;

                            m_IndexBuffer.SetData(m_TmpUpdater, 0, base_offset + TranslateIndex(new Vector3Int(mx, my, mz)), ymax - my);
                            int remainder = bsize.y - (ymax - my);
                            if (remainder > 0)
                                m_IndexBuffer.SetData(m_TmpUpdater, 0, base_offset + TranslateIndex(new Vector3Int(mx, 0, mz)), remainder);
#endif
                        }
                    }
                }
            }
        }

        internal void AddBricks(List<Brick> bricks, List<Chunk> allocations, int allocationSize, int poolWidth, int poolHeight)
        {
            int base_offset = kAPVConstantsSize + m_IndexDim.x * m_IndexDim.z;
            int largest_cell = ProbeReferenceVolume.cellSize(15);
            int brickIdx = 0;
            for (int j = 0; j < allocations.Count; j++)
            {
                Chunk alloc = allocations[j];
                int count = Mathf.Min(allocationSize, bricks.Count - brickIdx);

                for (int i = 0; i < count; i++, brickIdx++, alloc.x += ProbeBrickPool.kBrickProbeCountPerDim)
                {
                    // brick data
                    Brick b = bricks[brickIdx];
                    int cellSize = ProbeReferenceVolume.cellSize(b.size);

                    Debug.Assert(cellSize <= largest_cell, "Cell sizes are not correctly sorted.");
                    largest_cell = Mathf.Min(largest_cell, cellSize);

                    int minpos_x = b.position.x - m_CenterRS.x;
                    int minpos_y = b.position.y;
                    int minpos_z = b.position.z - m_CenterRS.z;
                    int maxpos_x = minpos_x + cellSize;
                    int maxpos_y = minpos_y + cellSize;
                    int maxpos_z = minpos_z + cellSize;
                    // clip to index region
                    minpos_x = Mathf.Max(minpos_x, -m_IndexDim.x / 2);
                    minpos_z = Mathf.Max(minpos_z, -m_IndexDim.z / 2);
                    maxpos_x = Mathf.Min(maxpos_x, m_IndexDim.x / 2);
                    maxpos_z = Mathf.Min(maxpos_z, m_IndexDim.z / 2);

                    int bsize_x = maxpos_x - minpos_x;
                    int bsize_z = maxpos_z - minpos_z;

                    if (bsize_x <= 0 || bsize_z <= 0)
                        continue;

                    // chunk data
                    int poolIdx = MergeIndex(alloc.flattenIndex(poolWidth, poolHeight), b.size);
                    for (int idx = 0; idx < cellSize; idx++)
                        m_TmpUpdater[idx] = poolIdx;

                    int posIS_x = m_CenterIS.x + minpos_x;
                    int posIS_z = m_CenterIS.z + minpos_z;
                    for (int z = 0; z < bsize_z; z++)
                    {
                        for (int x = 0; x < bsize_x; x++)
                        {
                            int mx = (posIS_x + x) % m_IndexDim.x;
                            int mz = (posIS_z + z) % m_IndexDim.z;

                            int hoff_idx = mz * m_IndexDim.x + mx;
                            HeightRange hr = m_HeightRanges[hoff_idx];

                            if (hr.min == -1) // untouched column
                            {
                                hr.min = minpos_y;
                                hr.cnt = Mathf.Min( cellSize, m_IndexDim.y );
                                m_IndexBuffer.SetData(m_TmpUpdater, 0, base_offset + TranslateIndex(new Vector3Int(mx, 0, mz)), hr.cnt);
                            }
                            else
                            {
                                // shift entire column upwards, but without pushing out existing indices
                                int lowest_limit = hr.min - (m_IndexDim.y - hr.cnt);
                                    lowest_limit = Mathf.Max(minpos_y, lowest_limit);
                                int shift_cnt    = Mathf.Max( 0, hr.min - lowest_limit );
                                int highest_limit = hr.min + m_IndexDim.y;

                                if( shift_cnt == 0 )
                                {
                                    hr.cnt = Mathf.Min(m_IndexDim.y, minpos_y + cellSize - hr.min);
                                    m_IndexBuffer.SetData(m_TmpUpdater, 0, base_offset + TranslateIndex(new Vector3Int(mx, minpos_y - hr.min, mz)), Mathf.Min( cellSize, highest_limit - minpos_y ));
                                }
                                else
                                {
                                    m_IndexBuffer.GetData(m_TmpUpdater, shift_cnt, base_offset + TranslateIndex(new Vector3Int(mx, 0, mz)), hr.cnt);

                                    hr.min  = lowest_limit;
                                    hr.cnt += shift_cnt;

                                    m_IndexBuffer.SetData(m_TmpUpdater, 0, base_offset + TranslateIndex(new Vector3Int(mx, 0, mz)), hr.cnt);

                                    // restore pool idx array
                                    for (int cidx = shift_cnt; cidx < cellSize; cidx++)
                                        m_TmpUpdater[cidx] = poolIdx;
                                }
                            }

                            // update the column offset
                            m_HeightRanges[hoff_idx] = hr;
                            m_TmpUpdater[m_TmpUpdater.Length - 1] = hr.min;
                            m_IndexBuffer.SetData(m_TmpUpdater, m_TmpUpdater.Length - 1, kAPVConstantsSize + hoff_idx, 1);
                        }
                    }
                }
            }
        }


        private int TranslateIndex( Vector3Int pos )
        {
            return pos.z * (m_IndexDim.x * m_IndexDim.y) + pos.x * m_IndexDim.y + pos.y;
        }

        private int MergeIndex( int index, int size )
        {
            const int mask = kMaxSubdivisionLevels;
            const int shift = 28;
            return (index & ~(mask << shift)) | ((size & mask) << shift);
        }

        private struct APVConstants
        {
            float WStoAPV;
        }

        private static int Asint(float val) { unsafe { return *((int*)&val); } }

        internal void WriteConstants(ref ProbeReferenceVolume.RefVolTransform refTrans, Vector3Int poolDim)
        {
#if USE_INDEX_NATIVE_ARRAY
            NativeArray<int> dst = m_IndexBuffer.BeginWrite<int>(0, kAPVConstantsSize);
#else
            int[] dst = m_TmpUpdater;
#endif
            Matrix4x4 WStoRS = Matrix4x4.Inverse(refTrans.refSpaceToWS);

            dst[ 0] = Asint(WStoRS[0,0]);
            dst[ 1] = Asint(WStoRS[1,0]);
            dst[ 2] = Asint(WStoRS[2,0]);
            dst[ 3] = Asint(WStoRS[0,1]);
            dst[ 4] = Asint(WStoRS[1,1]);
            dst[ 5] = Asint(WStoRS[2,1]);
            dst[ 6] = Asint(WStoRS[0,2]);
            dst[ 7] = Asint(WStoRS[1,2]);
            dst[ 8] = Asint(WStoRS[2,2]);
            dst[ 9] = Asint(WStoRS[0,3]);
            dst[10] = Asint(WStoRS[1,3]);
            dst[11] = Asint(WStoRS[2,3]);
            dst[12] = Asint(0.0f);
            dst[13] = m_CenterRS.x;
            dst[14] = m_CenterRS.y;
            dst[15] = m_CenterRS.z;
            dst[16] = m_CenterIS.x;
            dst[17] = m_CenterIS.y;
            dst[18] = m_CenterIS.z;
            dst[19] = m_IndexDim.x;
            dst[20] = m_IndexDim.y;
            dst[21] = m_IndexDim.z;
            dst[22] = poolDim.x;
            dst[23] = poolDim.y;
            dst[24] = poolDim.z;

#if USE_INDEX_NATIVE_ARRAY
            m_IndexBuffer.EndWrite<int>(kAPVConstantsSize);
#else
            m_IndexBuffer.SetData(dst, 0, 0, kAPVConstantsSize);
#endif
        }

        internal void GetRuntimeResources(ref ProbeReferenceVolume.RuntimeResources rr) { rr.index = m_IndexBuffer; }
    }
}
