using UnityEngine.Rendering;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class SubsurfaceScatteringManager
    {
        // Currently we only support SSSBuffer with one buffer. If the shader code change, it may require to update the shader manager
        public const int k_MaxSSSBuffer = 1;

        readonly int m_SSSBuffer0;
        readonly RenderTargetIdentifier m_SSSBuffer0RT;

        public int sssBufferCount { get { return k_MaxSSSBuffer; } }

        RenderTargetIdentifier[] m_ColorMRTs;
        RenderTargetIdentifier[] m_RTIDs = new RenderTargetIdentifier[k_MaxSSSBuffer];

        public SubsurfaceScatteringManager()
        {
            m_SSSBuffer0RT = new RenderTargetIdentifier(m_SSSBuffer0);
        }

        // In case of deferred, we must be in sync with SubsurfaceScattering.hlsl and lit.hlsl files and setup the correct buffers
        // for SSS
        public void InitGBuffers(int width, int height, GBufferManager gbufferManager, CommandBuffer cmd)
        {
            m_RTIDs[0] = gbufferManager.GetGBuffers()[0];
        }

        // In case of full forward we must allocate the render target for forward SSS (or reuse one already existing)
        // TODO: Provide a way to reuse a render target
        public void InitGBuffers(int width, int height, CommandBuffer cmd)
        {
            m_RTIDs[0] = m_SSSBuffer0RT;

            cmd.ReleaseTemporaryRT(m_SSSBuffer0);
            cmd.GetTemporaryRT(m_SSSBuffer0, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);            
        }

        public RenderTargetIdentifier GetSSSBuffers(int index)
        {
            Debug.Assert(index < sssBufferCount);
            return m_RTIDs[index];
        }
    }
}
