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
        private Vector3Int    m_AnchorRS;   // the anchor in ref space, around which the index is defined
        private Vector3Int    m_AnchorIS;   // the position in index space that the anchor maps to

        internal ProbeBrickIndex( Vector3Int indexDimensions )
        {
            Profiler.BeginSample("Create ProbeBrickIndex");
            int index_size = indexDimensions.x * indexDimensions.y * indexDimensions.z;
            m_IndexDim    = indexDimensions;
            m_AnchorRS    = new Vector3Int(0, 0, 0);
            m_AnchorIS    = indexDimensions / 2;
            m_IndexBuffer = new ComputeBuffer(index_size, sizeof(int), ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
            // Should be done by a compute shader
            Profiler.BeginSample("Clear Index");
            NativeArray<int> arr = m_IndexBuffer.BeginWrite<int>(0, index_size);
            for (int i = 0; i < index_size; i++)
                arr[i] = -1;
            m_IndexBuffer.EndWrite<int>(index_size);
            Profiler.EndSample();
            Profiler.EndSample();
        }

        internal void AddBricks( List<Brick> bricks, List<Chunk> allocations, int allocationSize, int poolWidth, int poolHeight )
        {
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
                    Vector3Int minpos   = b.position - m_AnchorRS;
                    Vector3Int maxpos   = minpos + new Vector3Int(cellSize, cellSize, cellSize);

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

                    Vector3Int posIS = m_AnchorIS + minpos;
                    for( int z = 0; z < bsize.z; z++ )
                    {
                        for( int y = 0; y < bsize.y; y++ )
                        {
                            int mz = (posIS.z + z) % m_IndexDim.z;
                            int my = (posIS.y + y) % m_IndexDim.y;
                            int mx = (posIS.x + 0) % m_IndexDim.x;

                            // x wraps around
                            int xmax = Mathf.Min(mx + bsize.z, m_IndexDim.x);

                            NativeArray<int> dst = m_IndexBuffer.BeginWrite<int>(TranslateIndex(new Vector3Int( mx, my, mz) ), xmax - mx);
                            for (int idx = 0; idx < xmax - mx; idx++)
                                dst[idx] = poolIdx;
                            m_IndexBuffer.EndWrite<int>(xmax - mx);

                            int remainder = bsize.x - (xmax - mx);
                            if( remainder > 0 )
                            {
                                NativeArray<int> dst2 = m_IndexBuffer.BeginWrite<int>(TranslateIndex(new Vector3Int(0, my, mz)), remainder);
                                for (int idx = 0; idx < remainder; idx++)
                                    dst2[idx] = poolIdx;
                                m_IndexBuffer.EndWrite<int>(remainder);
                            }
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
    }
}
