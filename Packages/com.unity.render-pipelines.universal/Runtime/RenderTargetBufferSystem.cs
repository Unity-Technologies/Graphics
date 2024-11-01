using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal.Internal
{
    internal sealed class RenderTargetBufferSystem
    {
        struct SwapBuffer
        {
            public RTHandle rtMSAA;
            public RTHandle rtResolve;
            public string name;
            public int msaa;
        }
        SwapBuffer m_A, m_B;
        static bool m_AisBackBuffer = true;

        static RenderTextureDescriptor m_Desc;
        FilterMode m_FilterMode;
        bool m_AllowMSAA = true;

        ref SwapBuffer backBuffer { get { return ref m_AisBackBuffer ? ref m_A : ref m_B; } }
        ref SwapBuffer frontBuffer { get { return ref m_AisBackBuffer ? ref m_B : ref m_A; } }

        public RenderTargetBufferSystem(string name)
        {
            m_A.name = name + "A";
            m_B.name = name + "B";
        }

        public void Dispose()
        {
            m_A.rtMSAA?.Release();
            m_B.rtMSAA?.Release();
            m_A.rtResolve?.Release();
            m_B.rtResolve?.Release();
        }

        public RTHandle PeekBackBuffer()
        {
            return (m_AllowMSAA && backBuffer.msaa > 1) ? backBuffer.rtMSAA : backBuffer.rtResolve;
        }

        public RTHandle GetBackBuffer(CommandBuffer cmd)
        {
            ReAllocate(cmd);
            return PeekBackBuffer();
        }

        public RTHandle GetFrontBuffer(CommandBuffer cmd)
        {
            if (!m_AllowMSAA && frontBuffer.msaa > 1)
                frontBuffer.msaa = 1;

            ReAllocate(cmd);

            return (m_AllowMSAA && frontBuffer.msaa > 1) ? frontBuffer.rtMSAA : frontBuffer.rtResolve;
        }

        public void Swap()
        {
            m_AisBackBuffer = !m_AisBackBuffer;
        }

        void ReAllocate(CommandBuffer cmd)
        {
            var desc = m_Desc;

            desc.msaaSamples = m_A.msaa;
            if (desc.msaaSamples > 1)
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_A.rtMSAA, desc, m_FilterMode, TextureWrapMode.Clamp, name: m_A.name);

            desc.msaaSamples = m_B.msaa;
            if (desc.msaaSamples > 1)
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_B.rtMSAA, desc, m_FilterMode, TextureWrapMode.Clamp, name: m_B.name);

            desc.msaaSamples = 1;
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_A.rtResolve, desc, m_FilterMode, TextureWrapMode.Clamp, name: m_A.name);
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_B.rtResolve, desc, m_FilterMode, TextureWrapMode.Clamp, name: m_B.name);
            cmd.SetGlobalTexture(m_A.name, m_A.rtResolve);
            cmd.SetGlobalTexture(m_B.name, m_B.rtResolve);
        }

        public void Clear()
        {
            m_AisBackBuffer = true;
            m_AllowMSAA = m_A.msaa > 1 || m_B.msaa > 1;
        }

        public void SetCameraSettings(RenderTextureDescriptor desc, FilterMode filterMode)
        {
            desc.depthStencilFormat = GraphicsFormat.None;
            m_Desc = desc;
            m_FilterMode = filterMode;

            m_A.msaa = m_Desc.msaaSamples;
            m_B.msaa = m_Desc.msaaSamples;

            if (m_Desc.msaaSamples > 1)
                EnableMSAA(true);
        }

        public RTHandle GetBufferA()
        {
            return (m_AllowMSAA && m_A.msaa > 1) ? m_A.rtMSAA : m_A.rtResolve;
        }

        public void EnableMSAA(bool enable)
        {
            m_AllowMSAA = enable;
            if (enable)
            {
                m_A.msaa = m_Desc.msaaSamples;
                m_B.msaa = m_Desc.msaaSamples;
            }
        }
    }
}
