using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

public unsafe class UploadBufferPool
{
    public struct UploadBuffer
    {
        public NativeArray<uint> gpuData;
        public GraphicsBufferHandle bufferHandle;
        public int index;
    }

    public GraphicsBuffer[] m_buffers;
    public NativeArray<GraphicsBufferHandle> m_bufferHandles;
    public NativeArray<int> m_usedFrame;
    public NativeArray<int> m_countWrittenAsync;
    public int m_bufferSizeBytes;
    public int m_frame;
    public int m_reuseFrame;
    public int m_reuseCounter;

    public void SetFrame(int v) => m_frame = v;
    public void SetReuseFrame(int v) => m_reuseFrame = v;

    public UploadBufferPool(int numBuffers, int bufferSizeBytes)
    {
        m_frame = 0;
        m_reuseFrame = Int32.MinValue;
        m_reuseCounter = 0;

        m_bufferSizeBytes = bufferSizeBytes;
        m_bufferHandles = new NativeArray<GraphicsBufferHandle>(numBuffers, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        m_buffers = new GraphicsBuffer[numBuffers];
        for (int i = 0; i < m_buffers.Length; ++i)
        {
            m_buffers[i] = new GraphicsBuffer(GraphicsBuffer.Target.Vertex, GraphicsBuffer.UsageFlags.LockBufferForWrite, bufferSizeBytes / 4, 4);
            m_bufferHandles[i] = m_buffers[i].bufferHandle;
        }

        m_usedFrame = new NativeArray<int>(numBuffers, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        for (int i = 0; i < m_usedFrame.Length; ++i)
        {
            m_usedFrame[i] = Int32.MinValue;
        }

        m_countWrittenAsync = new NativeArray<int>(1, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        m_countWrittenAsync[0] = m_bufferSizeBytes / 4;
    }

    public UploadBuffer StartBufferWrite()
    {
        // Need to resize the pool?
        if (m_usedFrame[m_reuseCounter] > m_reuseFrame)
        {
            // TODO: Implement!
            Debug.Log("UploadBufferPool out of storage! TODO: Implement growing. Index=" + m_reuseCounter + " Used=" + m_usedFrame[m_reuseCounter] + " ReuseFrame=" + m_reuseFrame);
        }

        var current = m_reuseCounter;
        m_usedFrame[current] = m_frame;

        // Ring buffer
        m_reuseCounter++;
        if (m_reuseCounter >= m_usedFrame.Length)
            m_reuseCounter = 0;

        var gpuData = m_buffers[current].LockBufferForWrite<uint>(0, m_bufferSizeBytes / 4);

        return new UploadBuffer
        {
            gpuData = gpuData,
            bufferHandle = m_bufferHandles[current],
            index = current
        };
    }

    public void EndBufferWrite(UploadBuffer buffer)
    {
        m_buffers[buffer.index].UnlockBufferAfterWrite<uint>(m_bufferSizeBytes / 4);
    }

    public void EndBufferWriteAfterJob(UploadBuffer buffer, JobHandle dependency)
    {
        m_buffers[buffer.index].UnlockBufferAfterWriteOnCompletion<uint>(dependency, m_countWrittenAsync);
    }

    public void Dispose()
    {
        m_bufferHandles.Dispose();
        m_countWrittenAsync.Dispose();
        m_usedFrame.Dispose();
        for (int i = 0; i < m_buffers.Length; ++i)
        {
            m_buffers[i].Dispose();
        }
    }
}
