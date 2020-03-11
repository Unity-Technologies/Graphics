using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HighDefinition;

namespace UnityEngine.Experimental.Rendering.HighDefinition
{
    class SSGIDenoiser
    {
        // Resources used for the denoiser
        ComputeShader m_SSGIDenoiserCS;

        // Required for fetching depth and normal buffers
        SharedRTManager m_SharedRTManager;
        HDRenderPipeline m_RenderPipeline;

        int m_SpatialFilterHalfKernel;
        int m_SpatialFilterKernel;
        int m_TemporalFilterHalfKernel;
        int m_TemporalFilterKernel;
        int m_CopyHistory;

        // Temporary variables to avoid memory alloc
        Vector2 firstMipOffset = new Vector2(0.0f, 0.0f);

        public SSGIDenoiser()
        {
        }

        public void Init(RenderPipelineResources rpResources, SharedRTManager sharedRTManager, HDRenderPipeline renderPipeline)
        {   
            // Keep track of the resources
            m_SSGIDenoiserCS = rpResources.shaders.ssGIDenoiserCS;

            // Keep track of the shared rt manager
            m_SharedRTManager = sharedRTManager;
            m_RenderPipeline = renderPipeline;

            // Fetch the kernels we are going to require
            m_SpatialFilterHalfKernel = m_SSGIDenoiserCS.FindKernel("SpatialFilterHalf");
            m_SpatialFilterKernel = m_SSGIDenoiserCS.FindKernel("SpatialFilter");

            // Fetch the kernels we are going to require
            m_TemporalFilterHalfKernel = m_SSGIDenoiserCS.FindKernel("TemporalFilterHalf");
            m_TemporalFilterKernel = m_SSGIDenoiserCS.FindKernel("TemporalFilter");

            m_CopyHistory = m_SSGIDenoiserCS.FindKernel("CopyHistory");
        }

        public void Release()
        {
        }

        void EvaluateDispatchParameters(HDCamera hdCamera, bool halfResolution, int tileSize, 
                                        out int numTilesX, out int numTilesY, out Vector4 halfScreenSize)
        {
            if (halfResolution)
            {
                // Fetch texture dimensions
                int texWidth = hdCamera.actualWidth / 2;
                int texHeight = hdCamera.actualHeight / 2;
                numTilesX = (texWidth + (tileSize - 1)) / tileSize;
                numTilesY = (texHeight + (tileSize - 1)) / tileSize;
                halfScreenSize = new Vector4(texWidth, texHeight, 1.0f / texWidth, 1.0f / texHeight);

            }
            else
            {
                // Fetch texture dimensions
                int texWidth = hdCamera.actualWidth;
                int texHeight = hdCamera.actualHeight;
                numTilesX = (texWidth + (tileSize - 1)) / tileSize;
                numTilesY = (texHeight + (tileSize - 1)) / tileSize;
                halfScreenSize = new Vector4(texWidth / 2, texHeight / 2, 0.5f / texWidth, 0.5f / texHeight);
            }
        }

        RTHandle IndirectDiffuseHistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension,
                                        enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                                        name: string.Format("IndirectDiffuseHistoryBuffer{0}", frameIndex));
        }

        public void Denoise(CommandBuffer cmd, HDCamera hdCamera, 
            RTHandle noisyBuffer, RTHandle outputBuffer,
            bool halfResolution = false)
        {
            // Grab the global illumination volume component
            var giSettings = hdCamera.volumeStack.GetComponent<UnityEngine.Rendering.HighDefinition.GlobalIllumination>();

            var historyDepthBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
            var historyDepthBuffer1 = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth1);
            if (historyDepthBuffer == null || historyDepthBuffer1 == null)
            {
                HDUtils.BlitCameraTexture(cmd, noisyBuffer, outputBuffer);
                return;
            }
            // Compute the dispatch parameters based on if we are half res or not
            int tileSize = 8;
            int numTilesX, numTilesY;
            Vector4 halfScreenSize;
            EvaluateDispatchParameters(hdCamera, halfResolution, tileSize, out numTilesX, out numTilesY, out halfScreenSize);

            // Pick the right kernel to use
            int m_KernelFilter = halfResolution ? m_SpatialFilterHalfKernel : m_SpatialFilterKernel;

            // Inject half screen size if required
            if (halfResolution)
                cmd.SetComputeVectorParam(m_SSGIDenoiserCS, HDShaderIDs._HalfScreenSize, halfScreenSize);

            // Bind the input buffers
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthTexture());
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._InputNoisyBuffer, noisyBuffer);

            // Bind the input scalars
            var info = m_SharedRTManager.GetDepthBufferMipChainInfo();
            firstMipOffset.Set(HDShadowUtils.Asfloat((uint)info.mipLevelOffsets[1].x), HDShadowUtils.Asfloat((uint)info.mipLevelOffsets[1].y));
            cmd.SetComputeVectorParam(m_SSGIDenoiserCS, HDShaderIDs._DepthPyramidFirstMipLevelOffset, firstMipOffset);
            cmd.SetComputeIntParam(m_SSGIDenoiserCS, HDShaderIDs._IndirectDiffuseSpatialFilter, giSettings.filterRadius);

            // Bind the output buffer
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._OutputFilteredBuffer, outputBuffer);

            // Evaluate the validation
            cmd.DispatchCompute(m_SSGIDenoiserCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            RTHandle indirectDiffuseHistory0 = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF);
            if(indirectDiffuseHistory0 == null)
            {
                indirectDiffuseHistory0 = hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF, IndirectDiffuseHistoryBufferAllocatorFunction, 1);
                CoreUtils.SetRenderTarget(cmd, indirectDiffuseHistory0, m_SharedRTManager.GetDepthStencilBuffer(), ClearFlag.Color, clearColor: Color.black);
            }

            // Pick the right kernel to use
            m_KernelFilter = halfResolution ? m_TemporalFilterHalfKernel : m_TemporalFilterKernel;

            // Bind the input buffers
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthTexture());
            if (halfResolution)
            {   
                cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._HistoryDepthTexture, historyDepthBuffer1);
                cmd.SetComputeVectorParam(m_SSGIDenoiserCS, HDShaderIDs._DepthPyramidFirstMipLevelOffset, firstMipOffset);
            }
            else
            {
                cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._HistoryDepthTexture, historyDepthBuffer);
            }
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._HistoryBuffer, indirectDiffuseHistory0);
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._InputNoisyBuffer, outputBuffer);

            // Bind the output buffer
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._OutputFilteredBuffer, noisyBuffer);

            // Evaluate the validation
            cmd.DispatchCompute(m_SSGIDenoiserCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            // Pick the right kernel to use
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_CopyHistory, HDShaderIDs._InputNoisyBuffer, noisyBuffer);
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_CopyHistory, HDShaderIDs._OutputFilteredBuffer, indirectDiffuseHistory0);
            cmd.DispatchCompute(m_SSGIDenoiserCS, m_CopyHistory, numTilesX, numTilesY, hdCamera.viewCount);
        }
    }
}
