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
            public RenderTargetHandle rt;
            public int name;
            public int msaa;
        }
        SwapBuffer m_A, m_B;
        static bool m_AisBackBuffer = true;

        static RenderTextureDescriptor m_Desc;
        FilterMode m_FilterMode;
        bool m_AllowMSAA = true;
        bool m_RTisAllocated = false;

        SwapBuffer backBuffer { get { return m_AisBackBuffer ? m_A : m_B; } }
        SwapBuffer frontBuffer { get { return m_AisBackBuffer ? m_B : m_A; } }

        public RenderTargetBufferSystem(string name)
        {
            m_A.name = Shader.PropertyToID(name + "A");
            m_B.name = Shader.PropertyToID(name + "B");
            m_A.rt.Init(name + "A");
            m_B.rt.Init(name + "B");
        }

        public RenderTargetHandle GetBackBuffer()
        {
            return backBuffer.rt;
        }

        public RenderTargetHandle GetBackBuffer(CommandBuffer cmd)
        {
            if (!m_RTisAllocated)
                Initialize(cmd);
            return backBuffer.rt;
        }

        public RenderTargetHandle GetFrontBuffer(CommandBuffer cmd)
        {
            if (!m_RTisAllocated)
                Initialize(cmd);

            int pipelineMSAA = m_Desc.msaaSamples;
            int bufferMSAA = frontBuffer.msaa;

            if (m_AllowMSAA && bufferMSAA != pipelineMSAA)
            {
                //We don't want a depth buffer on B buffer
                var desc = m_Desc;
                if (m_AisBackBuffer)
                    desc.depthBufferBits = 0;

                cmd.ReleaseTemporaryRT(frontBuffer.name);
                cmd.GetTemporaryRT(frontBuffer.name, desc, m_FilterMode);

                if (m_AisBackBuffer)
                    m_B.msaa = desc.msaaSamples;
                else m_A.msaa = desc.msaaSamples;
            }
            else if (!m_AllowMSAA && bufferMSAA > 1)
            {
                //We don't want a depth buffer on B buffer
                var desc = m_Desc;
                desc.msaaSamples = 1;
                if (m_AisBackBuffer)
                    desc.depthBufferBits = 0;

                cmd.ReleaseTemporaryRT(frontBuffer.name);
                cmd.GetTemporaryRT(frontBuffer.name, desc, m_FilterMode);

                if (m_AisBackBuffer)
                    m_B.msaa = desc.msaaSamples;
                else m_A.msaa = desc.msaaSamples;
            }

            return frontBuffer.rt;
        }

        public void Swap()
        {
            m_AisBackBuffer = !m_AisBackBuffer;
        }

        void Initialize(CommandBuffer cmd)
        {
            m_A.msaa = m_Desc.msaaSamples;
            m_B.msaa = m_Desc.msaaSamples;

            cmd.GetTemporaryRT(m_A.name, m_Desc, m_FilterMode);
            var descB = m_Desc;
            //descB.depthBufferBits = 0;
            cmd.GetTemporaryRT(m_B.name, descB, m_FilterMode);

            m_RTisAllocated = true;
        }

        public void Clear(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(m_A.name);
            cmd.ReleaseTemporaryRT(m_B.name);

            m_AisBackBuffer = true;
            m_AllowMSAA = true;
        }

        public void SetCameraSettings(CommandBuffer cmd, RenderTextureDescriptor desc, FilterMode filterMode)
        {
            Clear(cmd); //SetCameraSettings is called when new stack starts rendering. Make sure the targets are updated to use the new descriptor.

            m_Desc = desc;
            m_FilterMode = filterMode;

            Initialize(cmd);
        }

        public void SetCameraSettings(RenderTextureDescriptor desc, FilterMode filterMode)
        {
            m_Desc = desc;
            m_FilterMode = filterMode;
        }

        public RenderTargetHandle GetBufferA()
        {
            return m_A.rt;
        }

        public void EnableMSAA(bool enable)
        {
            m_AllowMSAA = enable;
        }
    }
}
