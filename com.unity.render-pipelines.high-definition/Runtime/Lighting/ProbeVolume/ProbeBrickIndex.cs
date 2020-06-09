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
        private Vector3Int    m_Anchor;

        internal ProbeBrickIndex( Vector3Int indexDimensions )
        {
            Profiler.BeginSample("Create ProbeBrickIndex");
            int index_size = indexDimensions.x * indexDimensions.y * indexDimensions.z;
            m_IndexDim    = indexDimensions;
            m_Anchor      = new Vector3Int(0, 0, 0);
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
                    Brick      b   = bricks[brickIdx];
                    Vector3Int pos = b.position;
                    if (pos.x < 0 || pos.y < 0 || pos.z < 0)
                        Debug.Log("brick position is negative");

                    // chunk data
                    int poolIdx = MergeIndex(alloc.flattenIndex(poolWidth, poolHeight), b.size);

                    for (pos.z = b.position.z; pos.z < b.position.z + b.size; pos.z++)
                        for (pos.y = b.position.y; pos.y < b.position.y + b.size; pos.y++)
                        {
                            pos.x = b.position.x;
                            NativeArray<int> dst = m_IndexBuffer.BeginWrite<int>(TranslateIndex(pos), b.size);
                            for(int idx = 0; idx < b.size; idx++)
                            {
                                dst[idx] = poolIdx;
                            }
                            m_IndexBuffer.EndWrite<int>(b.size);
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
