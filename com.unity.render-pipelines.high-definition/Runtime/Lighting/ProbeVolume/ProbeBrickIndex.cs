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


        private ComputeBuffer m_IndexBuffer;
        private Vector3Int    m_IndexDim;
        private Vector3Int    m_CenterRS;   // the anchor in ref space, around which the index is defined
        private Vector3Int    m_CenterIS;   // the position in index space that the anchor maps to
#if !USE_NATIVE_ARRAY
        private int[]         m_TmpUpdater = new int[Mathf.Max( kAPVConstantsSize, ProbeReferenceVolume.cellSize(15) )];
#endif

        internal ProbeBrickIndex( Vector3Int indexDimensions )
        {
            Profiler.BeginSample("Create ProbeBrickIndex");
            int index_size = kAPVConstantsSize + indexDimensions.x * indexDimensions.y * indexDimensions.z;
            m_CenterRS     = new Vector3Int(0, 0, 0);
            m_IndexDim     = indexDimensions;
            m_CenterIS     = indexDimensions / 2;
#if USE_INDEX_NATIVE_ARRAY
            m_IndexBuffer = new ComputeBuffer(index_size, sizeof(int), ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
#else
            m_IndexBuffer = new ComputeBuffer(index_size, sizeof(int), ComputeBufferType.Structured);
#endif
            // Should be done by a compute shader
            Clear();
            Profiler.EndSample();
        }

        internal void Clear()
        {
            Profiler.BeginSample("Clear Index");
            int index_size = kAPVConstantsSize + m_IndexDim.x * m_IndexDim.y * m_IndexDim.z;
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
            Profiler.EndSample();
        }

        internal void AddBricks( List<Brick> bricks, List<Chunk> allocations, int allocationSize, int poolWidth, int poolHeight )
        {
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
                        for( int y = 0; y < bsize.y; y++ )
                        {
                            if (bsize.x <= 0)
                                continue;

                            int mz = (posIS.z + z) % m_IndexDim.z;
                            int my = (posIS.y + y) % m_IndexDim.y;
                            int mx = (posIS.x + 0) % m_IndexDim.x;

                            // x wraps around
                            int xmax = Mathf.Min(mx + bsize.x, m_IndexDim.x);

#if USE_INDEX_NATIVE_ARRAY
                            NativeArray<int> dst = m_IndexBuffer.BeginWrite<int>(kAPVConstantsSize + TranslateIndex(new Vector3Int( mx, my, mz) ), xmax - mx);
                            for (int idx = 0; idx < xmax - mx; idx++)
                                dst[idx] = poolIdx;
                            m_IndexBuffer.EndWrite<int>(xmax - mx);
                            int remainder = bsize.x - (xmax - mx);
                            if( remainder > 0 )
                            {
                                NativeArray<int> dst2 = m_IndexBuffer.BeginWrite<int>(kAPVConstantsSize + TranslateIndex(new Vector3Int(0, my, mz)), remainder);
                                for (int idx = 0; idx < remainder; idx++)
                                    dst2[idx] = poolIdx;
                                m_IndexBuffer.EndWrite<int>(remainder);
                            }                            
#else
                            for (int idx = 0; idx < bsize.x; idx++)
                                m_TmpUpdater[idx] = poolIdx;

                            m_IndexBuffer.SetData(m_TmpUpdater, 0, kAPVConstantsSize + TranslateIndex(new Vector3Int(mx, my, mz)), xmax - mx);
                            int remainder = bsize.x - (xmax - mx);
                            if (remainder > 0)
                                m_IndexBuffer.SetData(m_TmpUpdater, 0, kAPVConstantsSize + TranslateIndex(new Vector3Int(0, my, mz)), remainder);
#endif
                        }
                    }
                }
            }
        }

        private int TranslateIndex( Vector3Int pos )
        {
            return pos.z * (m_IndexDim.x * m_IndexDim.y) + pos.y * m_IndexDim.x + pos.x;
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
