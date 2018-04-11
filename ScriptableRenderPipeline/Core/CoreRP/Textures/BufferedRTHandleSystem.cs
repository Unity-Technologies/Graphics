using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    public class BufferedRTHandleSystem : IDisposable
    {
        Dictionary<int, RTHandleSystem.RTHandle[]> m_RTHandles = new Dictionary<int, RTHandleSystem.RTHandle[]>();

        RTHandleSystem m_RTHandleSystem = new RTHandleSystem();
        bool m_DisposedValue = false;

        public RTHandleSystem.RTHandle GetFrameRT(int id, int index)
        {
            if (!m_RTHandles.ContainsKey(id))
                return null;

            Assert.IsTrue(index >= 0 && index < m_RTHandles[id].Length);

            return m_RTHandles[id][index];
        }

        public void AllocBuffer(
            int id, 
            Func<RTHandleSystem, RTHandleSystem.RTHandle> allocator,
            int bufferSize
        )
        {
            m_RTHandles.Add(id, new RTHandleSystem.RTHandle[bufferSize]);
        }

        void Dispose(bool disposing)
        {
            if (!m_DisposedValue)
            {
                if (disposing)
                {
                    m_RTHandleSystem.Dispose();
                    m_RTHandleSystem = null;
                }

                m_DisposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
