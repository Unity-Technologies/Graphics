using System;
using System.Diagnostics;
using System.Collections.Generic;
using Unity.Collections;

using CellStreamingScratchBuffer = UnityEngine.Rendering.ProbeReferenceVolume.CellStreamingScratchBuffer;
using CellStreamingScratchBufferLayout = UnityEngine.Rendering.ProbeReferenceVolume.CellStreamingScratchBufferLayout;

namespace UnityEngine.Rendering
{
    class ProbeVolumeScratchBufferPool
    {
        [DebuggerDisplay("ChunkCount = {chunkCount} ElementCount = {pool.Count}")]
        class ScratchBufferPool
            : IComparable<ScratchBufferPool>
        {
            public int chunkCount = -1;
            public Stack<CellStreamingScratchBuffer> pool = new Stack<CellStreamingScratchBuffer>();

            public ScratchBufferPool(int chunkCount)
            {
                this.chunkCount = chunkCount;
            }

            private ScratchBufferPool()
            {

            }

            public int CompareTo(ScratchBufferPool other)
            {
                if (chunkCount < other.chunkCount)
                    return -1;
                else if (chunkCount > other.chunkCount)
                    return 1;
                else
                    return 0;
            }
        }

        // Size in bytes of a full SH chunk (L0+L1+L2+ValidityMask)
        public int chunkSize { get; private set; }
        // Maximum number of chunks that should allocated in total. This is a hint, size may differ a bit to ensure at least one scratch buffer for each required size.
        public int maxChunkCount { get; private set; }
        public int allocatedMemory => chunkSize * m_CurrentlyAllocatedChunkCount;

        int m_L0Size;
        int m_L1Size;
        int m_ValiditySize;
        int m_ValidityLayerCount;
        int m_L2Size;
        int m_SkyOcclusionSize;
        int m_SkyShadingDirectionSize;

        int m_CurrentlyAllocatedChunkCount = 0;
        // List and not a Dictionary because we need the list sorted.
        List<ScratchBufferPool> m_Pools = new List<ScratchBufferPool>();
        // We store layouts separately because we might use a bigger buffer than required but we still want the layout to match the exact chunk count.
        Dictionary<int, CellStreamingScratchBufferLayout> m_Layouts = new Dictionary<int, CellStreamingScratchBufferLayout>();

        public ProbeVolumeScratchBufferPool(ProbeVolumeBakingSet bakingSet, ProbeVolumeSHBands shBands)
        {
            chunkSize = bakingSet.GetChunkGPUMemory(shBands);
            maxChunkCount = bakingSet.maxSHChunkCount;

            m_L0Size = bakingSet.L0ChunkSize;
            m_L1Size = bakingSet.L1ChunkSize;
            m_ValiditySize = bakingSet.sharedValidityMaskChunkSize;
            m_ValidityLayerCount = bakingSet.bakedMaskCount;
            m_SkyOcclusionSize = bakingSet.sharedSkyOcclusionL0L1ChunkSize;
            m_SkyShadingDirectionSize = bakingSet.sharedSkyShadingDirectionIndicesChunkSize;
            m_L2Size = bakingSet.L2TextureChunkSize;
        }

        CellStreamingScratchBufferLayout GetOrCreateScratchBufferLayout(int chunkCount)
        {
            if (m_Layouts.TryGetValue(chunkCount, out var layout))
            {
                return layout;
            }
            else
            {
                var bufferLayout = new CellStreamingScratchBufferLayout();
                bufferLayout._L0Size = m_L0Size;
                bufferLayout._L1Size = m_L1Size;
                bufferLayout._ValiditySize = m_ValiditySize;
                bufferLayout._ValidityProbeSize = m_ValidityLayerCount; // 1layer => 1xbyte => 4 probes at a time.
                if (m_SkyOcclusionSize != 0)
                {
                    bufferLayout._SkyOcclusionSize = m_SkyOcclusionSize;
                    bufferLayout._SkyOcclusionProbeSize = 8; // 4xFP16

                    if (m_SkyShadingDirectionSize != 0)
                    {
                        bufferLayout._SkyShadingDirectionSize = m_SkyShadingDirectionSize;
                        bufferLayout._SkyShadingDirectionProbeSize = 1; // 1xbyte => 4 probes at a time.
                    }
                    else
                    {
                        bufferLayout._SkyShadingDirectionSize = 0;
                        bufferLayout._SkyShadingDirectionProbeSize = 0;
                    }
                }
                else
                {
                    bufferLayout._SkyOcclusionSize = 0;
                    bufferLayout._SkyOcclusionProbeSize = 0;

                    bufferLayout._SkyShadingDirectionSize = 0;
                    bufferLayout._SkyShadingDirectionProbeSize = 0;
                }
                bufferLayout._L2Size = m_L2Size;

                // TODO: Find a generic way to pass this down? (depends on the gfx format we use).
                bufferLayout._L0ProbeSize = 8; // 4xFP16
                bufferLayout._L1ProbeSize = 4; // 4xbyte
                bufferLayout._L2ProbeSize = 4; // 4xbyte

                int destChunksSize = chunkCount * sizeof(uint) * 4; // 1 Chunk == Vector4Int
                                                                    // First destination chunks at offset 0 (no explicit member for this).
                                                                    // Then, shared data destination chunks. Can be different from SH data destination in case of blending
                                                                    // (one pool for blending and one other pool for shared data and blending destination).
                bufferLayout._SharedDestChunksOffset = destChunksSize;
                bufferLayout._L0L1rxOffset = bufferLayout._SharedDestChunksOffset + destChunksSize;
                bufferLayout._L1GryOffset = bufferLayout._L0L1rxOffset + m_L0Size * chunkCount;
                bufferLayout._L1BrzOffset = bufferLayout._L1GryOffset + m_L1Size * chunkCount;
                bufferLayout._ValidityOffset = bufferLayout._L1BrzOffset + m_L1Size * chunkCount;
                bufferLayout._SkyOcclusionOffset = bufferLayout._ValidityOffset + m_ValiditySize * chunkCount;
                bufferLayout._SkyShadingDirectionOffset = bufferLayout._SkyOcclusionOffset + m_SkyOcclusionSize * chunkCount;
                bufferLayout._L2_0Offset = bufferLayout._SkyShadingDirectionOffset + m_SkyShadingDirectionSize * chunkCount;
                bufferLayout._L2_1Offset = bufferLayout._L2_0Offset + m_L2Size * chunkCount;
                bufferLayout._L2_2Offset = bufferLayout._L2_1Offset + m_L2Size * chunkCount;
                bufferLayout._L2_3Offset = bufferLayout._L2_2Offset + m_L2Size * chunkCount;

                bufferLayout._ProbeCountInChunkLine = ProbeBrickPool.kChunkProbeCountPerDim;
                bufferLayout._ProbeCountInChunkSlice = ProbeBrickPool.kChunkProbeCountPerDim * ProbeBrickPool.kBrickProbeCountPerDim;

                m_Layouts.Add(chunkCount, bufferLayout);
                return bufferLayout;
            }
        }

        CellStreamingScratchBuffer CreateScratchBuffer(int chunkCount, bool allocateGraphicsBuffers)
        {
            var scratchBuffer = new CellStreamingScratchBuffer(chunkCount, chunkSize, allocateGraphicsBuffers);
            m_CurrentlyAllocatedChunkCount += chunkCount;

            return scratchBuffer;
        }

        // Used to avoid gcalloc in m_Pools.FindIndex
        static int s_ChunkCount;

        // Will return a the smallest GraphicsBuffer with at least enough space for chunkCount chunks.
        public bool AllocateScratchBuffer(int chunkCount, out CellStreamingScratchBuffer scratchBuffer, out CellStreamingScratchBufferLayout layout, bool allocateGraphicsBuffers)
        {
            s_ChunkCount = chunkCount;
            int index = m_Pools.FindIndex(0, (o) => o.chunkCount == s_ChunkCount);
            // The size of buffer we return may not be the exact requested size (can be bigger).
            // So we need to make sure the layout is the right one for the number of requested chunks.
            layout = GetOrCreateScratchBufferLayout(chunkCount);

            if (index != -1)
            {
                var pool = m_Pools[index].pool;
                // If one is available, return it.
                if (pool.Count > 0)
                {
                    scratchBuffer = pool.Pop();
                    scratchBuffer.Swap();
                    return true;
                }
                else
                {
                    // If none are available of the exact size, try to find a bigger buffer instead.
                    // We won't go above twice the size to avoid "wasting" bigger buffers for requests that are too small.
                    for (int i = index; i < m_Pools.Count; ++i)
                    {
                        var biggerPool = m_Pools[i];
                        // Next existing pools scratch size are too big. Break out of the loop.
                        // We don't want to hog a big buffer for a small request.
                        if (biggerPool.chunkCount >= (chunkCount * 2))
                            break;
                        else if (biggerPool.pool.Count > 0)
                        {
                            scratchBuffer = biggerPool.pool.Pop();
                            scratchBuffer.Swap();
                            return true;
                        }
                    }

                    // We did not find any suitable buffer. Create a new one with the exact requested size.
                    if ((m_CurrentlyAllocatedChunkCount + chunkCount) < maxChunkCount)
                    {
                        scratchBuffer = CreateScratchBuffer(chunkCount, allocateGraphicsBuffers);
                        return true;
                    }
                    // If we are above the maximum, we don't allocate more buffers.
                    // It's ok since we know at least one buffer will exist for any given chunkCount so we won't get impossible to fulfill requests.
                    else
                    {
                        scratchBuffer = null;
                        return false;
                    }
                }
            }
            else
            {
                // No pool of this size exists. Create a new pool of that size and return a new buffer;
                ScratchBufferPool newPool = new ScratchBufferPool(chunkCount);
                m_Pools.Add(newPool);
                m_Pools.Sort();

                scratchBuffer = CreateScratchBuffer(chunkCount, allocateGraphicsBuffers);
                return true;
            }
        }

        public void ReleaseScratchBuffer(CellStreamingScratchBuffer scratchBuffer)
        {
            s_ChunkCount = scratchBuffer.chunkCount;
            var pool = m_Pools.Find((o) => o.chunkCount == s_ChunkCount);
            Debug.Assert(pool != null);
            pool.pool.Push(scratchBuffer);
        }

        public void Cleanup()
        {
            foreach (var pool in m_Pools)
            {
                while(pool.pool.Count > 0)
                {
                    var scratchBuffer = pool.pool.Pop();
                    scratchBuffer.Dispose();
                }
            }

            m_Pools.Clear();
            m_CurrentlyAllocatedChunkCount = 0;
            chunkSize = 0;
            maxChunkCount = 0;
        }
    }
}
