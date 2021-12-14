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
    public GraphicsBuffer[] m_Buffers;
    public NativeArray<GraphicsBufferHandle> m_BufferHandles;
    public int m_bufferSizeBytes;

    public UploadBufferPool(int numBuffers, int bufferSizeBytes)
    {
        m_bufferSizeBytes = bufferSizeBytes;
        m_BufferHandles = new NativeArray<GraphicsBufferHandle>(numBuffers, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        m_Buffers = new GraphicsBuffer[numBuffers];
        for (int i = 0; i < m_Buffers.Length; ++i)
        {
            // TODO: Use vertex buffer instead of raw buffer!
            m_Buffers[i] = new GraphicsBuffer(GraphicsBuffer.Target.Raw, bufferSizeBytes / 4, 4);
        }
    }

    public void Dispose()
    {
        m_BufferHandles.Dispose();
        for (int i = 0; i < m_Buffers.Length; ++i)
        {
            m_Buffers[i].Dispose();
        }
    }
}
