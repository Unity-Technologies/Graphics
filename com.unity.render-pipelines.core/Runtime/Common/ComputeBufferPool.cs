using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Rendering
{
    internal class ComputeBufferPool : IDisposable
    {
        private List<ComputeBuffer> m_Buffers;
        private Stack<int> m_FreeBufferIds;

        private int m_Count;
        private int m_Stride;
        private ComputeBufferType m_Type;
        private ComputeBufferMode m_Mode;

        public ComputeBufferPool(int count, int stride, ComputeBufferType type = ComputeBufferType.Default, ComputeBufferMode mode = ComputeBufferMode.Immutable)
        {
            m_Buffers = new List<ComputeBuffer>();
            m_FreeBufferIds = new Stack<int>();

            m_Count = count;
            m_Stride = stride;
            m_Type = type;
            m_Mode = mode;
        }
        public void Dispose()
        {
            for (int i = 0; i < m_Buffers.Count; ++i)
            {
                m_Buffers[i].Dispose();
            }
        }
        private int AllocateBuffer()
        {
            var id = m_Buffers.Count;
            var cb = new ComputeBuffer(m_Count, m_Stride, m_Type, m_Mode);
            m_Buffers.Add(cb);
            return id;
        }

        public int GetBufferId()
        {
            if (m_FreeBufferIds.Count == 0)
                return AllocateBuffer();

            return m_FreeBufferIds.Pop();
        }
        public ComputeBuffer GetBufferFromId(int id)
        {
            return m_Buffers[id];
        }

        public void PutBufferId(int id)
        {
            m_FreeBufferIds.Push(id);
        }

        public int TotalBufferCount => m_Buffers.Count;
        public int TotalBufferSize => TotalBufferCount * m_Count * m_Stride;
    }
}
