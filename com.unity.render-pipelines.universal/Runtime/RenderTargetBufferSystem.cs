using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace UnityEngine.Rendering.Universal.Internal
{
    //NOTE: This class is meant to be removed when RTHandles get implemented in urp
    internal sealed class RenderTargetBufferSystem
    {
        struct SwapBuffer
        {
            public RTHandle rt;
            public string name;
            public int msaa;
        }
        SwapBuffer m_A, m_B;
        static bool m_AisBackBuffer = true;

        static RenderTextureDescriptor m_Desc;
        FilterMode m_FilterMode;
        bool m_AllowMSAA = true;
        bool m_RTisAllocated = false;

        ref SwapBuffer backBuffer { get { return ref m_AisBackBuffer ? ref m_A : ref m_B; } }
        ref SwapBuffer frontBuffer { get { return ref m_AisBackBuffer ? ref m_B : ref m_A; } }

        public RenderTargetBufferSystem(string name)
        {
            m_A.name = name + "A";
            m_B.name = name + "B";
        }

        public void Dispose()
        {
            m_A.rt?.Release();
            m_B.rt?.Release();
        }

        public RTHandle PeekBackBuffer()
        {
            return backBuffer.rt;
        }

        public RTHandle GetBackBuffer(CommandBuffer cmd)
        {
            if (!m_RTisAllocated)
                ReAllocate(cmd);
            return backBuffer.rt;
        }

        public RTHandle GetFrontBuffer(CommandBuffer cmd)
        {
            int bufferMSAA = frontBuffer.msaa;

            if (!m_AllowMSAA && frontBuffer.msaa > 1)
                frontBuffer.msaa = 1;

            if (!m_RTisAllocated)
                ReAllocate(cmd);

            return frontBuffer.rt;
        }

        public void Swap()
        {
            m_AisBackBuffer = !m_AisBackBuffer;
        }

        void ReAllocate(CommandBuffer cmd)
        {
            var desc = m_Desc;
            desc.msaaSamples = m_A.msaa;
            if (RenderingUtils.RTHandleNeedsReAlloc(m_A.rt, desc, false))
            {
                m_A.rt?.Release();
                m_A.rt = RTHandles.Alloc(desc, filterMode: m_FilterMode, wrapMode: TextureWrapMode.Clamp, name: m_A.name);
                cmd.SetGlobalTexture(m_A.rt.name, m_A.rt);
            }
            desc.msaaSamples = m_B.msaa;
            if (RenderingUtils.RTHandleNeedsReAlloc(m_B.rt, desc, false))
            {
                m_B.rt?.Release();
                m_B.rt = RTHandles.Alloc(desc, filterMode: m_FilterMode, wrapMode: TextureWrapMode.Clamp, name: m_B.name);
                cmd.SetGlobalTexture(m_B.rt.name, m_B.rt);
            }
            m_RTisAllocated = true;
        }

        public void Clear(CommandBuffer cmd)
        {
            m_AisBackBuffer = true;
            m_AllowMSAA = true;
        }

        public void SetCameraSettings(CommandBuffer cmd, RenderTextureDescriptor desc, FilterMode filterMode)
        {
            Clear(cmd); //SetCameraSettings is called when new stack starts rendering. Make sure the targets are updated to use the new descriptor.

            desc.depthBufferBits = 0;
            m_Desc = desc;
            m_FilterMode = filterMode;

            m_A.msaa = m_Desc.msaaSamples;
            m_B.msaa = m_Desc.msaaSamples;

            ReAllocate(cmd);
        }

        public RTHandle GetBufferA()
        {
            return m_A.rt;
        }

        public void EnableMSAA(bool enable)
        {
            m_AllowMSAA = enable;
        }
    }
}
