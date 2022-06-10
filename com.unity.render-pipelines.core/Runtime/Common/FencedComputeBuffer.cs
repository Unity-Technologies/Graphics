using System;
using Unity.Collections;
using UnityEngine;

namespace Unity.Rendering
{
    public class FencedComputeBufferHandle<DataType> : IDisposable where DataType : struct
    {
        private FencedComputeBufferPool m_BufferPool;
        private int m_InitialCount;
        private ComputeBuffer m_Buffer;

        public NativeArray<DataType> Data { get; private set; }

        internal FencedComputeBufferHandle(FencedComputeBufferPool bufferPool, int count)
        {
            m_BufferPool = bufferPool;
            m_InitialCount = count;

            m_BufferPool.BeginFrame();
            m_Buffer = m_BufferPool.GetCurrentFrameBuffer();
            Data = m_Buffer.BeginWrite<DataType>(0, count);
        }

        public void Dispose()
        {
            if (m_Buffer != null)
            {
                m_Buffer.EndWrite<DataType>(m_InitialCount);
                m_BufferPool.EndFrame();

                m_Buffer = null;
            }
        }

        public ComputeBuffer EndWrite(int writtenCount)
        {
            var buffer = m_Buffer;

            if (m_Buffer != null)
            {
                m_Buffer.EndWrite<DataType>(writtenCount);
                m_BufferPool.EndFrame();

                m_Buffer = null;
            }

            return buffer;
        }
    }

    public class ConstantFencedComputeBuffer<DataType> : IDisposable where DataType : struct
    {
        private ConstantFencedComputeBufferPool<DataType> m_BufferPool;

        public ConstantFencedComputeBuffer()
        {
            m_BufferPool = new ConstantFencedComputeBufferPool<DataType>();
        }

        public void Dispose()
        {
            m_BufferPool?. Dispose();
        }

        public FencedComputeBufferHandle<DataType> BeginWrite(int count)
        {
            return new FencedComputeBufferHandle<DataType>(m_BufferPool, count);
        }
    }
    public class StructuredFencedComputeBuffer<DataType> : IDisposable where DataType : struct
    {
        private StructuredFencedComputeBufferPool<DataType> m_BufferPool;

        public StructuredFencedComputeBuffer(int size)
        {
            m_BufferPool = new StructuredFencedComputeBufferPool<DataType>(size);
        }

        public void Dispose()
        {
            m_BufferPool?.Dispose();
        }

        public FencedComputeBufferHandle<DataType> BeginWrite(int count)
        {
            return new FencedComputeBufferHandle<DataType>(m_BufferPool, count);
        }
    }

    public class ByteAddressFencedComputeBuffer : IDisposable
    {
        private ByteAddressFencedComputeBufferPool m_BufferPool;

        public ByteAddressFencedComputeBuffer(int size, int stride)
        {
            m_BufferPool = new ByteAddressFencedComputeBufferPool(size, stride);
        }

        public void Dispose()
        {
            m_BufferPool?.Dispose();
        }

        public FencedComputeBufferHandle<DataType> BeginWrite<DataType>(int count) where DataType : struct
        {
            return new FencedComputeBufferHandle<DataType>(m_BufferPool, count);
        }
    }
}
