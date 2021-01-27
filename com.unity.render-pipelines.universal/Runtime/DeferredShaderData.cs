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
            m_PreTiles = new NativeArray<PreTile>[DeferredConfig.kTilerDepth];
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

            for (int i = 0; i < m_Buffers.Length; ++i)
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

        internal ComputeBuffer ReserveBuffer<T>(int count, bool asCBuffer) where T : struct
        {
            int stride = Marshal.SizeOf<T>();
            int paddedCount = asCBuffer ? Align(stride * count, 16) / stride : count;
            return GetOrUpdateBuffer(paddedCount, stride, asCBuffer);
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

        ComputeBuffer GetOrUpdateBuffer(int count, int stride, bool isConstantBuffer)
        {
            ComputeBufferType type = isConstantBuffer ? ComputeBufferType.Constant : ComputeBufferType.Structured;
#if UNITY_SWITCH // maxQueuedFrames returns -1 on Switch!
            int maxQueuedFrames = 3;
#else
            int maxQueuedFrames = QualitySettings.maxQueuedFrames;
            Assertions.Assert.IsTrue(maxQueuedFrames >= 1, "invalid QualitySettings.maxQueuedFrames");
#endif

            for (int i = 0; i < m_BufferCount; ++i)
            {
                int bufferIndex = (m_CachedBufferIndex + i + 1) % m_BufferCount;

                if (IsLessCircular(m_BufferInfos[bufferIndex].frameUsed + (uint)maxQueuedFrames, m_FrameIndex)
                    && m_BufferInfos[bufferIndex].type == type && m_Buffers[bufferIndex].count == count && m_Buffers[bufferIndex].stride == stride)
                {
                    m_BufferInfos[bufferIndex].frameUsed = m_FrameIndex;
                    m_CachedBufferIndex = bufferIndex;
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
            return m_Buffers[m_BufferCount++];
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
