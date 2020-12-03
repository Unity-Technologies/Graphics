using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    class DeferredShaderData : IDisposable
    {
        static DeferredShaderData m_Instance = null;

        struct ComputeBufferInfo
        {
            public uint frameUsed;
            public ComputeBufferType type; // There is no interface to retrieve the type of a ComputeBuffer, so we must save it on our side
        }

        // Precomputed tiles (for each tiler).
        NativeArray<PreTile>[] m_PreTiles = null;
        // Tile headers (constant data per tile).
        ComputeBuffer[] m_GpuTilerTileHeaders;
        // Tile data (light list indices)
        ComputeBuffer[] m_GpuTilerTileData;
        // Structured buffers and constant buffers are all allocated from this array.
        ComputeBuffer[] m_Buffers = null;
        // We need to store extra info per ComputeBuffer.
        ComputeBufferInfo[] m_BufferInfos;

        // How many buffers have been created so far. This is <= than m_Buffers.Length.
        int m_BufferCount = 0;
        // Remember index of last buffer used. This optimizes the search for available buffer.
        int m_CachedBufferIndex = 0;
        // This counter is allowed to cycle back to 0.
        uint m_FrameIndex = 0;

        DeferredShaderData()
        {
            m_PreTiles = new NativeArray<PreTile>[DeferredConfig.kCPUTilerDepth];
            m_GpuTilerTileHeaders = new ComputeBuffer[DeferredConfig.kGPUTilerDepth];
            m_GpuTilerTileData = new ComputeBuffer[DeferredConfig.kGPUTilerDepth];
            m_Buffers = new ComputeBuffer[64];
            m_BufferInfos = new ComputeBufferInfo[64];
        }

        internal static DeferredShaderData instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new DeferredShaderData();

                return m_Instance;
            }
        }

        public void Dispose()
        {
            DisposeNativeArrays(ref m_PreTiles);

            for (int i = 0; i < m_GpuTilerTileHeaders.Length; ++i)
            {
                if (m_GpuTilerTileHeaders[i] != null)
                {
                    m_GpuTilerTileHeaders[i].Dispose();
                    m_GpuTilerTileHeaders[i] = null;
                }

                if (m_GpuTilerTileData[i] != null)
                {
                    m_GpuTilerTileData[i].Dispose();
                    m_GpuTilerTileData[i] = null;
                }
            }

            for (int i = 0; i < m_BufferCount; ++i)
            {
                if (m_Buffers[i] != null)
                {
                    m_Buffers[i].Dispose();
                    m_Buffers[i] = null;
                }
            }
            m_BufferCount = 0;
        }

        internal void ResetBuffers()
        {
            ++m_FrameIndex; // Allowed to cycle back to 0.
        }

        internal NativeArray<PreTile> GetPreTiles(int level, int count)
        {
            return GetOrUpdateNativeArray<PreTile>(ref m_PreTiles, level, count);
        }

        internal ComputeBuffer GetGPUTilerTileHeaders(int level, int count)
        {
            if (m_GpuTilerTileHeaders[level] == null || m_GpuTilerTileHeaders[level].count != count)
            {
                if (m_GpuTilerTileHeaders[level] != null)
                    m_GpuTilerTileHeaders[level].Dispose();
                m_GpuTilerTileHeaders[level] = new ComputeBuffer(count, 4);
            }
            return m_GpuTilerTileHeaders[level];
        }

        internal ComputeBuffer GetGPUTilerTileData(int level, int count)
        {
            if (m_GpuTilerTileData[level] == null || m_GpuTilerTileData[level].count != count)
            {
                if (m_GpuTilerTileData[level] != null)
                    m_GpuTilerTileData[level].Dispose();
                m_GpuTilerTileData[level] = new ComputeBuffer(count, 4);
            }
            return m_GpuTilerTileData[level];
        }

        internal ComputeBuffer ReserveBuffer<T>(int count, bool asCBuffer) where T : struct
        {
            int bufferId;
            return ReserveBuffer<T>(count, asCBuffer ? ComputeBufferType.Constant : ComputeBufferType.Structured, out bufferId);
        }

        internal ComputeBuffer ReserveBuffer<T>(int count, bool asCBuffer, out int bufferId) where T : struct
        {
            return ReserveBuffer<T>(count, asCBuffer ? ComputeBufferType.Constant : ComputeBufferType.Structured, out bufferId);
        }

        internal ComputeBuffer ReserveBuffer<T>(int count, ComputeBufferType type) where T : struct
        {
            int bufferId;
            return ReserveBuffer<T>(count, type, out bufferId);
        }

        internal ComputeBuffer ReserveBuffer<T>(int count, ComputeBufferType type, out int bufferId) where T : struct
        {
            int stride = Marshal.SizeOf<T>();
            int paddedCount = type == ComputeBufferType.Constant ? Align(stride * count, 16) / stride : count;
            return GetOrUpdateBuffer(paddedCount, stride, type, out bufferId);
        }

        internal ComputeBuffer GetBuffer(int bufferId)
        {
            return m_Buffers[bufferId];
        }

        NativeArray<T> GetOrUpdateNativeArray<T>(ref NativeArray<T>[] nativeArrays, int level, int count) where T : struct
        {
            if (!nativeArrays[level].IsCreated)
            {
                nativeArrays[level] = new NativeArray<T>(count, Allocator.Persistent);
            }
            else if (count > nativeArrays[level].Length)
            {
                nativeArrays[level].Dispose();
                nativeArrays[level] = new NativeArray<T>(count, Allocator.Persistent);
            }

            return nativeArrays[level];
        }

        void DisposeNativeArrays<T>(ref NativeArray<T>[] nativeArrays) where T : struct
        {
            for (int i = 0; i < nativeArrays.Length; ++i)
            {
                if (nativeArrays[i].IsCreated)
                    nativeArrays[i].Dispose();
            }
        }

        ComputeBuffer GetOrUpdateBuffer(int count, int stride, ComputeBufferType type, out int bufferId)
        {
            /*
            #if !UNITY_EDITOR && UNITY_SWITCH // maxQueuedFrames returns -1 on Switch!
            int maxQueuedFrames = 3;
            #else
            int maxQueuedFrames = QualitySettings.maxQueuedFrames;
            Assertions.Assert.IsTrue(maxQueuedFrames >= 1, "invalid QualitySettings.maxQueuedFrames");
            #endif
            */
            int maxQueuedFrames = 3;

            for (int i = 0; i < m_BufferCount; ++i)
            {
                int bufferIndex = (m_CachedBufferIndex + i + 1) % m_BufferCount;

                if (IsLessCircular(m_BufferInfos[bufferIndex].frameUsed + (uint)maxQueuedFrames, m_FrameIndex)
                    && m_BufferInfos[bufferIndex].type == type && m_Buffers[bufferIndex].count == count && m_Buffers[bufferIndex].stride == stride)
                {
                    m_BufferInfos[bufferIndex].frameUsed = m_FrameIndex;
                    m_CachedBufferIndex = bufferIndex;
                    bufferId = bufferIndex;
                    return m_Buffers[bufferIndex];
                }
            }

            if (m_BufferCount == m_Buffers.Length) // If all buffers used: allocate more space.
            {
                ComputeBuffer[] newBuffers = new ComputeBuffer[m_BufferCount * 2];
                for (int i = 0; i < m_BufferCount; ++i)
                    newBuffers[i] = m_Buffers[i];
                m_Buffers = newBuffers;

                ComputeBufferInfo[] newBufferInfos = new ComputeBufferInfo[m_BufferCount * 2];
                for (int i = 0; i < m_BufferCount; ++i)
                    newBufferInfos[i] = m_BufferInfos[i];
                m_BufferInfos = newBufferInfos;
            }

            // Create new buffer.
            m_Buffers[m_BufferCount] = new ComputeBuffer(count, stride, type, ComputeBufferMode.Immutable);
            m_BufferInfos[m_BufferCount].frameUsed = m_FrameIndex;
            m_BufferInfos[m_BufferCount].type = type;
            m_CachedBufferIndex = m_BufferCount;
            bufferId = m_BufferCount++;
            return m_Buffers[bufferId++];
        }

        void DisposeBuffers(ComputeBuffer[,] buffers)
        {
            for (int i = 0; i < buffers.GetLength(0); ++i)
            {
                for (int j = 0; j < buffers.GetLength(1); ++j)
                {
                    if (buffers[i, j] != null)
                    {
                        buffers[i, j].Dispose();
                        buffers[i, j] = null;
                    }
                }
            }
        }

        static bool IsLessCircular(uint a, uint b)
        {
            return a != b ? (b - a) < 0x80000000 : false;
        }

        static int Align(int s, int alignment)
        {
            return ((s + alignment - 1) / alignment) * alignment;
        }
    }
}
