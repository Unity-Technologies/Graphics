using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
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
            RTHandle noisyBuffer0, RTHandle noisyBuffer1,
            RTHandle outputBuffer0, RTHandle outputBuffer1,
            bool halfResolution = false, float historyValidity = 1.0f)
        {
            // Grab the global illumination volume component
            var giSettings = hdCamera.volumeStack.GetComponent<UnityEngine.Rendering.HighDefinition.GlobalIllumination>();

            var historyDepthBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
            var historyDepthBuffer1 = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth1);
            // If the depth textures are not available, we can't denoise
            if (historyDepthBuffer == null || historyDepthBuffer1 == null)
            {
                return;
            }

            // Compute the dispatch parameters based on if we are half res or not
            int tileSize = 8;
            int numTilesX, numTilesY;
            Vector4 halfScreenSize;
            EvaluateDispatchParameters(hdCamera, halfResolution, tileSize, out numTilesX, out numTilesY, out halfScreenSize);

            // Pick the right kernel to use
            int m_KernelFilter = halfResolution ? m_SpatialFilterHalfKernel : m_SpatialFilterKernel;

            // Bind the input scalars
            var info = m_SharedRTManager.GetDepthBufferMipChainInfo();
            firstMipOffset.Set(HDShadowUtils.Asfloat((uint)info.mipLevelOffsets[1].x), HDShadowUtils.Asfloat((uint)info.mipLevelOffsets[1].y));
            cmd.SetComputeVectorParam(m_SSGIDenoiserCS, HDShaderIDs._DepthPyramidFirstMipLevelOffset, firstMipOffset);
            cmd.SetComputeIntParam(m_SSGIDenoiserCS, HDShaderIDs._IndirectDiffuseSpatialFilter, giSettings.filterRadius);
            // Inject half screen size if required
            if (halfResolution)
                cmd.SetComputeVectorParam(m_SSGIDenoiserCS, HDShaderIDs._HalfScreenSize, halfScreenSize);

            // Bind the input buffers
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthTexture());
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._InputNoisyBuffer0, noisyBuffer0);
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._InputNoisyBuffer1, noisyBuffer1);

            // Bind the output buffer
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._OutputFilteredBuffer0, outputBuffer0);
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._OutputFilteredBuffer1, outputBuffer1);

            // Do the spatial pass
            cmd.DispatchCompute(m_SSGIDenoiserCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            // Grab the history buffer
            RTHandle indirectDiffuseHistory0 = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF);
            if(indirectDiffuseHistory0 == null)
            {
                indirectDiffuseHistory0 = hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF, IndirectDiffuseHistoryBufferAllocatorFunction, 1);
                CoreUtils.SetRenderTarget(cmd, indirectDiffuseHistory0, ClearFlag.Color, clearColor: Color.black);
            }
            RTHandle indirectDiffuseHistory1 = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseLF);
            if (indirectDiffuseHistory1 == null)
            {
                indirectDiffuseHistory1 = hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseLF, IndirectDiffuseHistoryBufferAllocatorFunction, 1);
                CoreUtils.SetRenderTarget(cmd, indirectDiffuseHistory1, ClearFlag.Color, clearColor: new Color(0.501960784f, 0.501960784f, 0.0f, 0.0f));
            }

            // Pick the right kernel to use
            m_KernelFilter = halfResolution ? m_TemporalFilterHalfKernel : m_TemporalFilterKernel;

            // Bind the input buffers
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthTexture());
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeFloatParam(m_SSGIDenoiserCS, HDShaderIDs._HistoryValidity, historyValidity);
            if (halfResolution)
            {   
                cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._HistoryDepthTexture, historyDepthBuffer1);
                cmd.SetComputeVectorParam(m_SSGIDenoiserCS, HDShaderIDs._DepthPyramidFirstMipLevelOffset, firstMipOffset);
            }
            else
            {
                cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._HistoryDepthTexture, historyDepthBuffer);
            }
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._HistoryBuffer0, indirectDiffuseHistory0);
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._HistoryBuffer1, indirectDiffuseHistory1);
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._InputNoisyBuffer0, outputBuffer0);
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._InputNoisyBuffer1, outputBuffer1);

            // Bind the output buffer
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._OutputFilteredBuffer0, noisyBuffer0);
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_KernelFilter, HDShaderIDs._OutputFilteredBuffer1, noisyBuffer1);

            // Do the temporal pass
            cmd.DispatchCompute(m_SSGIDenoiserCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            // Copy the new version into the history buffer
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_CopyHistory, HDShaderIDs._InputNoisyBuffer0, noisyBuffer0);
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_CopyHistory, HDShaderIDs._InputNoisyBuffer1, noisyBuffer1);
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_CopyHistory, HDShaderIDs._OutputFilteredBuffer0, indirectDiffuseHistory0);
            cmd.SetComputeTextureParam(m_SSGIDenoiserCS, m_CopyHistory, HDShaderIDs._OutputFilteredBuffer1, indirectDiffuseHistory1);
            cmd.DispatchCompute(m_SSGIDenoiserCS, m_CopyHistory, numTilesX, numTilesY, hdCamera.viewCount);
        }
    }
}
