using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class HDIndirectDiffuseDenoiser
    {
        // Resources used for the denoiser
        ComputeShader m_IndirectDiffuseDenoiseCS;
        Texture m_OwenScrambleRGBA;

        // Required for fetching depth and normal buffers
        SharedRTManager m_SharedRTManager;
        HDRenderPipeline m_RenderPipeline;

        public HDIndirectDiffuseDenoiser()
        {
        }

        public void Init(RenderPipelineResources rpResources, HDRenderPipelineRayTracingResources rpRTResources, SharedRTManager sharedRTManager, HDRenderPipeline renderPipeline)
        {
            // Keep track of the resources
            m_IndirectDiffuseDenoiseCS = rpRTResources.indirectDiffuseDenoiserCS;
            m_OwenScrambleRGBA = rpResources.textures.owenScrambledRGBATex;

            // Keep track of the shared rt manager
            m_SharedRTManager = sharedRTManager;
            m_RenderPipeline = renderPipeline;
        }

        public void Release()
        {
        }

        public void DenoiseBuffer(CommandBuffer cmd, HDCamera hdCamera,
            RTHandle noisySignal, RTHandle directionBuffer,
            RTHandle historySignal0_0,  RTHandle historySignal1_0,
            RTHandle historySignal0_1, RTHandle historySignal1_1,
            RTHandle outputSignal, 
            float historyValidity, float kernelSize, float kernelSize2)
        {
            // If we do not have a depth and normal history buffers, we can skip right away
            var historyDepthBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
            var historyNormalBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Normal);
            if (historyDepthBuffer == null || historyNormalBuffer == null)
            {
                HDUtils.BlitCameraTexture(cmd, noisySignal, historySignal0_0);
                HDUtils.BlitCameraTexture(cmd, noisySignal, historySignal0_1);
                HDUtils.BlitCameraTexture(cmd, noisySignal, historySignal1_0);
                HDUtils.BlitCameraTexture(cmd, noisySignal, historySignal1_1);
                return;
            }

            // Fetch texture dimensions
            int texWidth = hdCamera.actualWidth;
            int texHeight = hdCamera.actualHeight;

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

            // Request the intermediate buffer we need
            RTHandle validationBuffer = m_RenderPipeline.GetRayTracingBuffer(InternalRayTracingBuffers.R0);
            RTHandle outputBuffer0 = m_RenderPipeline.GetRayTracingBuffer(InternalRayTracingBuffers.RGBA0);
            RTHandle outputBuffer1 = m_RenderPipeline.GetRayTracingBuffer(InternalRayTracingBuffers.RGBA1);
            RTHandle outputBuffer2 = m_RenderPipeline.GetRayTracingBuffer(InternalRayTracingBuffers.RGBA2);
            RTHandle outputBuffer3 = m_RenderPipeline.GetRayTracingBuffer(InternalRayTracingBuffers.RGBA3);

            // First of all we need to validate the history to know where we can or cannot use the history signal
            int m_KernelFilter = m_IndirectDiffuseDenoiseCS.FindKernel("ValidateHistory");
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._HistoryDepthTexture, historyDepthBuffer);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._HistoryNormalBufferTexture, historyNormalBuffer);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._ValidationBufferRW, validationBuffer);
            cmd.SetComputeFloatParam(m_IndirectDiffuseDenoiseCS, HDShaderIDs._HistoryValidity, historyValidity);
            cmd.SetComputeFloatParam(m_IndirectDiffuseDenoiseCS, HDShaderIDs._PixelSpreadAngleTangent, HDRenderPipeline.GetPixelSpreadTangent(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight));
            cmd.DispatchCompute(m_IndirectDiffuseDenoiseCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            // Convert the signal
            m_KernelFilter = m_IndirectDiffuseDenoiseCS.FindKernel("ConvertRGBToYCoCg");
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture0, noisySignal);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTexture0RW, outputBuffer0);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTexture1RW, outputBuffer1);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DirectionTexture, directionBuffer);
            cmd.DispatchCompute(m_IndirectDiffuseDenoiseCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            // Now that we have validated our history, let's accumulate
            m_KernelFilter = m_IndirectDiffuseDenoiseCS.FindKernel("TemporalAccumulation");
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture0, outputBuffer0);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture1, outputBuffer1);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._HistoryBuffer0, historySignal0_0);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._HistoryBuffer1, historySignal1_0);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTexture0RW, outputBuffer2);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTexture1RW, outputBuffer3);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._ValidationBuffer, validationBuffer);
            cmd.DispatchCompute(m_IndirectDiffuseDenoiseCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            // Make sure to copy the new-accumulated signal in our history buffer
            m_KernelFilter = m_IndirectDiffuseDenoiseCS.FindKernel("CopyHistory");
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture0, outputBuffer2);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture1, outputBuffer3);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTexture0RW, historySignal0_0);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTexture1RW, historySignal1_0);
            cmd.DispatchCompute(m_IndirectDiffuseDenoiseCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            m_KernelFilter = m_IndirectDiffuseDenoiseCS.FindKernel("BilateralFilterNoNormal");
            cmd.SetGlobalTexture(HDShaderIDs._OwenScrambledRGTexture, m_OwenScrambleRGBA);
            cmd.SetComputeFloatParam(m_IndirectDiffuseDenoiseCS, HDShaderIDs._DenoiserFilterRadius, kernelSize);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture0, outputBuffer2);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture1, outputBuffer3);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTexture0RW, outputBuffer0);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTexture1RW, outputBuffer1);
            cmd.SetComputeFloatParam(m_IndirectDiffuseDenoiseCS, HDShaderIDs._PixelSpreadAngleTangent, HDRenderPipeline.GetPixelSpreadTangent(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight));
            cmd.DispatchCompute(m_IndirectDiffuseDenoiseCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            // Now that we have validated our history, let's accumulate
            m_KernelFilter = m_IndirectDiffuseDenoiseCS.FindKernel("TemporalAccumulation");
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture0, outputBuffer0);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture1, outputBuffer1);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._HistoryBuffer0, historySignal0_1);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._HistoryBuffer1, historySignal1_1);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTexture0RW, outputBuffer2);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTexture1RW, outputBuffer3);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._ValidationBuffer, validationBuffer);
            cmd.DispatchCompute(m_IndirectDiffuseDenoiseCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            // Make sure to copy the new-accumulated signal in our history buffer
            m_KernelFilter = m_IndirectDiffuseDenoiseCS.FindKernel("CopyHistory");
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture0, outputBuffer2);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture1, outputBuffer3);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTexture0RW, historySignal0_1);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTexture1RW, historySignal1_1);
            cmd.DispatchCompute(m_IndirectDiffuseDenoiseCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            m_KernelFilter = m_IndirectDiffuseDenoiseCS.FindKernel("BilateralFilter");
            cmd.SetGlobalTexture(HDShaderIDs._OwenScrambledRGTexture, m_OwenScrambleRGBA);
            cmd.SetComputeFloatParam(m_IndirectDiffuseDenoiseCS, HDShaderIDs._DenoiserFilterRadius, kernelSize2);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture0, outputBuffer2);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture1, outputBuffer3);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTexture0RW, outputBuffer0);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTexture1RW, outputBuffer1);
            cmd.SetComputeFloatParam(m_IndirectDiffuseDenoiseCS, HDShaderIDs._PixelSpreadAngleTangent, HDRenderPipeline.GetPixelSpreadTangent(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight));
            cmd.DispatchCompute(m_IndirectDiffuseDenoiseCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            m_KernelFilter = m_IndirectDiffuseDenoiseCS.FindKernel("ConvertYCoCgToRGB");
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture0, outputBuffer0);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture1, outputBuffer1);
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_IndirectDiffuseDenoiseCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTexture0RW, outputSignal);
            cmd.DispatchCompute(m_IndirectDiffuseDenoiseCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);
        }
    }
}
