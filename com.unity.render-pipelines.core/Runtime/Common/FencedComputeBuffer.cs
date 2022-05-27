using System;
using Unity.Collections;
using UnityEngine;

namespace Unity.Rendering
{
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

        public NativeArray<DataType> BeginWrite(int count)
        {
            m_BufferPool.BeginFrame();
            return m_BufferPool.GetCurrentFrameBuffer().BeginWrite<DataType>(0, count);
        }

        public ComputeBuffer EndWrite(int countWritten)
        {
            var buffer = m_BufferPool.GetCurrentFrameBuffer();
            buffer.EndWrite<DataType>(countWritten);
            m_BufferPool.EndFrame();
            return buffer;
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

        public NativeArray<DataType> BeginWrite(int count)
        {
            m_BufferPool.BeginFrame();
            return m_BufferPool.GetCurrentFrameBuffer().BeginWrite<DataType>(0, count);
        }

        public ComputeBuffer EndWrite(int countWritten)
        {
            var buffer = m_BufferPool.GetCurrentFrameBuffer();
            buffer.EndWrite<DataType>(countWritten);
            m_BufferPool.EndFrame();
            return buffer;
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

        public NativeArray<DataType> BeginWrite<DataType>(int count) where DataType : struct
        {
            m_BufferPool.BeginFrame();
            return m_BufferPool.GetCurrentFrameBuffer().BeginWrite<DataType>(0, count);
        }

        public ComputeBuffer EndWrite<DataType>(int countWritten) where DataType : struct
        {
            var buffer = m_BufferPool.GetCurrentFrameBuffer();
            buffer.EndWrite<DataType>(countWritten);
            m_BufferPool.EndFrame();
            return buffer;
        }
    }
}
