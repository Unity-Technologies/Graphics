using UnityEngine.Rendering;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class NormalBufferManager
    {
        // Currently we only support NormalBuffer with one buffer. If the shader code change, it may require to update the shader manager
        public const int k_MaxNormalBuffer = 1;

        public int normalBufferCount { get { return k_MaxNormalBuffer; } }

        RTHandleSystem.RTHandle[] m_ColorMRTs = new RTHandleSystem.RTHandle[k_MaxNormalBuffer];
        protected RenderTargetIdentifier[] m_RTIDs = new RenderTargetIdentifier[k_MaxNormalBuffer];
        bool[] m_ExternalBuffer = new bool[k_MaxNormalBuffer];

        RTHandleSystem.RTHandle m_HTile;

        public NormalBufferManager()
        {
        }

        public void InitNormalBuffers(GBufferManager gbufferManager, RenderPipelineSettings settings)
        {
            if (settings.supportOnlyForward)
            {
                // In case of full forward we must allocate the render target for normal buffer (or reuse one already existing)
                // TODO: Provide a way to reuse a render target
                m_ColorMRTs[0] = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.ARGB32, sRGB: false, name: "NormalBuffer");
                m_ExternalBuffer[0] = false;
            }
            else
            {
                // In case of deferred, we must be in sync with NormalBuffer.hlsl and lit.hlsl files and setup the correct buffers
                m_ColorMRTs[0] = gbufferManager.GetBuffer(1); // Normal + Roughness is GBuffer(1)
                m_ExternalBuffer[0] = true;
            }
        }

        public RenderTargetIdentifier[] GetBuffersRTI()
        {
            // nameID can change from one frame to another depending on the msaa flag so so we need to update this array to be sure it's up to date.
            for (int i = 0; i < normalBufferCount; ++i)
            {
                m_RTIDs[i] = m_ColorMRTs[i].nameID;
            }

            return m_RTIDs;
        }

        public RTHandleSystem.RTHandle GetNormalBuffer(int index)
        {
            Debug.Assert(index < normalBufferCount);
            return m_ColorMRTs[index];
        }

        public void Build(HDRenderPipelineAsset hdAsset)
        {
        }

        public void Cleanup()
        {
            for (int i = 0; i < k_MaxNormalBuffer; ++i)
            {
                if (!m_ExternalBuffer[i])
                {
                    RTHandles.Release(m_ColorMRTs[i]);
                }
            }
        }

        public void BindNormalBuffers(CommandBuffer cmd)
        {
            // NormalBuffer can be access in forward shader, so need to set global texture
            for (int i = 0; i < normalBufferCount; ++i)
            {
                cmd.SetGlobalTexture(HDShaderIDs._NormalBufferTexture[i], GetNormalBuffer(i));
            }
        }
    }
}
