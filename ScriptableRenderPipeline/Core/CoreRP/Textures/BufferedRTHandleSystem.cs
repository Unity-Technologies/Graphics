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

        public RTHandleSystem.RTHandle GetFrameRT(int historyId, int frameIndex)
        {
            if (!m_RTHandles.ContainsKey(historyId))
                return null;

            Assert.IsTrue(frameIndex >= 0 && frameIndex < m_RTHandles[historyId].Length);

            return m_RTHandles[historyId][frameIndex];
        }

        public void AllocBuffer(
            int id, 
            Func<RTHandleSystem, int, RTHandleSystem.RTHandle> allocator,
            int bufferCount
        )
        {
            var buffer = new RTHandleSystem.RTHandle[bufferCount];
            m_RTHandles.Add(id, buffer);

            // First is autoresized
            buffer[0] = allocator(m_RTHandleSystem, 0);

            // Other are resized on demand
            for (int i = 1, c = buffer.Length; i < c; ++i)
            {
                buffer[i] = allocator(m_RTHandleSystem, i);
                m_RTHandleSystem.SwitchResizeMode(buffer[i], RTHandleSystem.ResizeMode.OnDemand);
            }
        }

        public void SetReferenceSize(int width, int height, bool msaa, MSAASamples msaaSamples)
        {
            m_RTHandleSystem.SetReferenceSize(width, height, msaa, msaaSamples);
        }

        public void Swap()
        {
            foreach (var item in m_RTHandles)
            {
                var nextFirst = item.Value[item.Value.Length - 1];
                for (int i = 0, c = item.Value.Length - 1; i < c; ++i)
                    item.Value[i + 1] = item.Value[i];
                item.Value[0] = nextFirst;

                // First is autoresize, other are on demand
                m_RTHandleSystem.SwitchResizeMode(item.Value[0], RTHandleSystem.ResizeMode.Auto);
                m_RTHandleSystem.SwitchResizeMode(item.Value[1], RTHandleSystem.ResizeMode.OnDemand);
            }
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

        public void ReleaseAll()
        {
            foreach (var item in m_RTHandles)
            {
                for (int i = 0, c = item.Value.Length; i < c; ++i)
                {
                    m_RTHandleSystem.Release(item.Value[i]);
                }
            }
            m_RTHandles.Clear();
        }
    }
}
