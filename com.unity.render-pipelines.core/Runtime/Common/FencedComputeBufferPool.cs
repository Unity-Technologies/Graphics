using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace Unity.Rendering
{
    internal abstract class FencedComputeBufferPool : IDisposable
    {
        struct FrameData
        {
            public int DataBufferID;
            public int FenceBufferID;
            public AsyncGPUReadbackRequest Fence;
        }

        public int BufferSize { get; private set; }

        const int k_maxFrames = 16;
        Queue<FrameData> m_FrameData = new Queue<FrameData>(k_maxFrames);

        ComputeBufferPool m_FenceBufferPool;
        internal ComputeBufferPool m_DataBufferPool;

        int m_CurrentFrameBufferID;

        public FencedComputeBufferPool()
        {
            m_CurrentFrameBufferID = -1;
        }

        public void Dispose()
        {
            m_FrameData.Clear();

            m_FenceBufferPool?.Dispose();
            m_DataBufferPool?.Dispose();

            m_CurrentFrameBufferID = -1;
        }

        public void BeginFrame()
        {
            Assert.IsTrue(m_CurrentFrameBufferID == -1);

            RecoverBuffers();
            m_CurrentFrameBufferID = m_DataBufferPool.GetBufferId();
        }

        public void EndFrame()
        {
            Assert.IsFalse(m_CurrentFrameBufferID == -1);

            var fenceBufferID = m_FenceBufferPool.GetBufferId();
            var frameData = new FrameData
            {
                DataBufferID = m_CurrentFrameBufferID,
                FenceBufferID = fenceBufferID,
            };

            if (SystemInfo.supportsAsyncGPUReadback)
            {
                frameData.Fence = AsyncGPUReadback.Request(m_FenceBufferPool.GetBufferFromId(fenceBufferID));
            }

            m_FrameData.Enqueue(frameData);

            m_CurrentFrameBufferID = -1;
        }

        public ComputeBuffer GetCurrentFrameBuffer()
        {
            Assert.IsFalse(m_CurrentFrameBufferID == -1);
            return m_DataBufferPool.GetBufferFromId(m_CurrentFrameBufferID);
        }

        // todo: improve the behavior here (GFXMESH-62).
        // GG: What would happen if any computebuffer is still in-flight?
        protected void ResizeBuffer(int size)
        {
            m_FrameData.Clear();

            m_FenceBufferPool?.Dispose();
            m_DataBufferPool?.Dispose();

            m_FenceBufferPool = new ComputeBufferPool(1, 4, ComputeBufferType.Raw, ComputeBufferMode.Dynamic);

            BufferSize = size;
        }

        private void RecoverBuffers()
        {
            while (CanFreeNextBuffer())
            {
                var data = m_FrameData.Dequeue();

                Assert.IsFalse(data.FenceBufferID == -1);

                m_FenceBufferPool.PutBufferId(data.FenceBufferID);
                m_DataBufferPool.PutBufferId(data.DataBufferID);
            }

            // Something is probably leaking if any of these fail.
            Assert.IsFalse(m_FrameData.Count > k_maxFrames - 1);

            bool CanFreeNextBuffer()
            {
                // Assume 3 frames in flight if the platform does not support async readbacks.
                if (SystemInfo.supportsAsyncGPUReadback)
                {
                    // Keep buffers around for another frame on Metal (GFXMESH-65).
                    // hasError is set to true when the Fence is disposed.
                    if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
                    {
                        return m_FrameData.Count > 0 && m_FrameData.Peek().Fence.hasError;
                    }
                    else
                    {
                        return m_FrameData.Count > 0 && m_FrameData.Peek().Fence.done;
                    }
                }
                else
                {
                    return m_FrameData.Count > 3;
                }
            }
        }
    }

    internal class ConstantFencedComputeBufferPool<DataType> : FencedComputeBufferPool where DataType : struct
    {
        public ConstantFencedComputeBufferPool() : base()
        {
            ResizeBuffer(1);
            m_DataBufferPool = new ComputeBufferPool(1, UnsafeUtility.SizeOf<DataType>(), ComputeBufferType.Constant, ComputeBufferMode.SubUpdates);
        }
    }

    internal class StructuredFencedComputeBufferPool<DataType> : FencedComputeBufferPool where DataType : struct
    {
        public StructuredFencedComputeBufferPool(int size) : base()
        {
            ResizeBuffer(size);
        }

        public new void ResizeBuffer(int size)
        {
            base.ResizeBuffer(size);
            m_DataBufferPool = new ComputeBufferPool(size, UnsafeUtility.SizeOf<DataType>(), ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
        }
    }

    internal class ByteAddressFencedComputeBufferPool : FencedComputeBufferPool
    {
        public ByteAddressFencedComputeBufferPool(int size, int stride) : base()
        {
            ResizeBuffer(size, stride);
        }

        public void ResizeBuffer(int size, int stride)
        {
            ResizeBuffer(size);
            m_DataBufferPool = new ComputeBufferPool(size, stride, ComputeBufferType.Raw, ComputeBufferMode.SubUpdates);
        }
    }
}
