using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Experimental.Rendering.HighDefinition
{
    class HDTemporalFilter
    {
        // Resources used for the denoiser
        ComputeShader m_TemporalFilterCS;

        // Required for fetching depth and normal buffers
        SharedRTManager m_SharedRTManager;
        HDRenderPipeline m_RenderPipeline;

        // The set of kernels that can be requested by the component
        int m_ValidateHistoryKernel;
        int m_ValidateHistoryKernelNoVelocityKernel;
        int m_ValidateHistoryHalfResKernel;
        int m_ValidateHistoryKernelNoVelocityHalfResKernel;
        int m_TemporalAccumulationSingleKernel;
        int m_TemporalAccumulationColorKernel;
        int m_TemporalAccumulationSingleHalfResKernel;
        int m_TemporalAccumulationColorHalfResKernel;
        int m_CopyHistorySingleKernel;
        int m_CopyHistoryColorKernel;

        public HDTemporalFilter()
        {
        }

        public void Init(RenderPipelineResources rpResources, HDRenderPipelineRayTracingResources rpRTResources, SharedRTManager sharedRTManager, HDRenderPipeline renderPipeline)
        {   
            // Keep track of the resources
            m_TemporalFilterCS = rpResources.shaders.temporalFilterCS;

            // Keep track of the shared rt manager
            m_SharedRTManager = sharedRTManager;
            m_RenderPipeline = renderPipeline;

            // Fetch all the kernels we are interested in
            m_ValidateHistoryKernel = m_TemporalFilterCS.FindKernel("ValidateHistory");
            m_ValidateHistoryKernelNoVelocityKernel = m_TemporalFilterCS.FindKernel("ValidateHistoryNoVelocity");
            m_ValidateHistoryHalfResKernel = m_TemporalFilterCS.FindKernel("ValidateHistoryHalfRes"); ;
            m_ValidateHistoryKernelNoVelocityHalfResKernel = m_TemporalFilterCS.FindKernel("ValidateHistoryNoVelocityHalfRes");
            m_TemporalAccumulationSingleKernel = m_TemporalFilterCS.FindKernel("TemporalAccumulationSingle");
            m_TemporalAccumulationColorKernel = m_TemporalFilterCS.FindKernel("TemporalAccumulationColor");
            m_TemporalAccumulationSingleHalfResKernel = m_TemporalFilterCS.FindKernel("TemporalAccumulationSingleHalfRes");
            m_TemporalAccumulationColorHalfResKernel = m_TemporalFilterCS.FindKernel("TemporalAccumulationColorHalfRes");
            m_CopyHistorySingleKernel = m_TemporalFilterCS.FindKernel("CopyHistorySingle");
            m_CopyHistoryColorKernel = m_TemporalFilterCS.FindKernel("CopyHistoryColor");
        }

        public void Release()
        {
        }

        public void ValidateHistory(CommandBuffer cmd, HDCamera hdCamera,
            float historyValidity = 1.0f, bool velocityCriterion = true, bool halfResolution = false)
        {
            // Grab the history validity buffer
            RTHandle historyValidityBuffer = m_RenderPipeline.GetRayTracingBuffer(InternalRayTracingBuffers.HistoryValidity);

            // If we do not have a depth and normal history buffers, the history buffer is not usable
            var historyDepthBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
            var historyNormalBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Normal);
            if (historyDepthBuffer == null || historyNormalBuffer == null)
            {
                CoreUtils.SetRenderTarget(cmd, historyValidityBuffer, m_SharedRTManager.GetDepthStencilBuffer(), ClearFlag.Color, clearColor: Color.black);
                return;
            }

            int areaTileSize = 8;
            int numTilesX, numTilesY;
            if (halfResolution)
            {
                // Fetch texture dimensions
                int texWidth = hdCamera.actualWidth / 2;
                int texHeight = hdCamera.actualHeight / 2;
                numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
                numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;
            }
            else
            {
                // Fetch texture dimensions
                int texWidth = hdCamera.actualWidth;
                int texHeight = hdCamera.actualHeight;
                numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
                numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;
            }

            // Pick the right kernel to use
            int m_KernelFilter;
            if (halfResolution)
                m_KernelFilter = velocityCriterion ? m_ValidateHistoryHalfResKernel : m_ValidateHistoryKernelNoVelocityHalfResKernel;
            else
                m_KernelFilter = velocityCriterion ? m_ValidateHistoryKernel : m_ValidateHistoryKernelNoVelocityKernel;

            // Bind constants required for the process
            var historyScale = new Vector2(hdCamera.actualWidth / (float)historyDepthBuffer.rt.width, hdCamera.actualHeight / (float)historyDepthBuffer.rt.height);
            cmd.SetComputeVectorParam(m_TemporalFilterCS, HDShaderIDs._RTHandleScaleHistory, historyScale);
            cmd.SetComputeFloatParam(m_TemporalFilterCS, HDShaderIDs._HistoryValidity, historyValidity);
            cmd.SetComputeFloatParam(m_TemporalFilterCS, HDShaderIDs._PixelSpreadAngleTangent, HDRenderPipeline.GetPixelSpreadTangent(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight));

            // Bind the input buffers
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthTexture());
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._HistoryDepthTexture, historyDepthBuffer);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._HistoryNormalBufferTexture, historyNormalBuffer);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._VelocityBuffer, TextureXR.GetBlackTexture());
            var info = m_SharedRTManager.GetDepthBufferMipChainInfo();
            cmd.SetComputeBufferParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DepthPyramidMipLevelOffsets, info.GetOffsetBufferData(m_RenderPipeline.depthPyramidMipLevelOffsetsBuffer));

            // Bind the output buffer
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._ValidationBufferRW, historyValidityBuffer);

            // Evaluate the validation
            cmd.DispatchCompute(m_TemporalFilterCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);
        }

        // Denoiser variant for non history array
        public void DenoiseBuffer(CommandBuffer cmd, HDCamera hdCamera,
            RTHandle noisySignal, RTHandle historySignal,
            RTHandle outputSignal, 
            bool singleChannel = true, float historyValidity = 1.0f, bool halfResolution = false)
        {
            int areaTileSize = 8;
            int numTilesX, numTilesY;
            if (halfResolution)
            {
                // Fetch texture dimensions
                int texWidth = hdCamera.actualWidth / 2;
                int texHeight = hdCamera.actualHeight / 2;
                numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
                numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;
            }
            else
            {
                // Fetch texture dimensions
                int texWidth = hdCamera.actualWidth;
                int texHeight = hdCamera.actualHeight;
                numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
                numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;
            }

            // Grab the history validity buffer
            RTHandle historyValidityBuffer = m_RenderPipeline.GetRayTracingBuffer(InternalRayTracingBuffers.HistoryValidity);

            int m_KernelFilter;
            if (halfResolution)
                m_KernelFilter = singleChannel ? m_TemporalAccumulationSingleHalfResKernel : m_TemporalAccumulationColorHalfResKernel;
            else
                m_KernelFilter = singleChannel ? m_TemporalAccumulationSingleKernel : m_TemporalAccumulationColorKernel;

            // Bind all the input buffers
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, noisySignal);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._HistoryBuffer, historySignal);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthTexture());
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._ValidationBuffer, historyValidityBuffer);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._VelocityBuffer, TextureXR.GetBlackTexture());
            var info = m_SharedRTManager.GetDepthBufferMipChainInfo();
            cmd.SetComputeBufferParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DepthPyramidMipLevelOffsets, info.GetOffsetBufferData(m_RenderPipeline.depthPyramidMipLevelOffsetsBuffer));

            // Bind the output buffer
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, outputSignal);

            // Do the temporal accumulation
            cmd.DispatchCompute(m_TemporalFilterCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            // Make sure to copy the new-accumulated signal in our history buffer
            m_KernelFilter = singleChannel ? m_CopyHistorySingleKernel : m_CopyHistoryColorKernel;
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, outputSignal);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DenoiseSharedOutputTextureRW, historySignal);
            cmd.DispatchCompute(m_TemporalFilterCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);
        }

        // Denoiser variant when history is stored in an array and the validation buffer is seperate
        public void DenoiseBuffer(CommandBuffer cmd, HDCamera hdCamera,
            RTHandle noisySignal, RTHandle historySignal, RTHandle validationHistory, RTHandle velocityBuffer,
            RTHandle outputSignal,
            int sliceIndex, Vector4 channelMask, bool singleChannel = true, float historyValidity = 1.0f)
        {
            // If we do not have a depth and normal history buffers, we can skip right away
            var historyDepthBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
            var historyNormalBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Normal);
            if (historyDepthBuffer == null || historyNormalBuffer == null)
            {
                HDUtils.BlitCameraTexture(cmd, noisySignal, historySignal);
                HDUtils.BlitCameraTexture(cmd, noisySignal, outputSignal);
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
            RTHandle historyValidityBuffer = m_RenderPipeline.GetRayTracingBuffer(InternalRayTracingBuffers.HistoryValidity);

            // Now that we have validated our history, let's accumulate
            int m_KernelFilter = m_TemporalFilterCS.FindKernel(singleChannel ? "TemporalAccumulationSingleArray" : "TemporalAccumulationColorArray");
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, noisySignal);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._HistoryBuffer, historySignal);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._HistoryValidityBuffer, validationHistory);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, outputSignal);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._ValidationBuffer, historyValidityBuffer);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._VelocityBuffer, velocityBuffer);
            cmd.SetComputeIntParam(m_TemporalFilterCS, HDShaderIDs._DenoisingHistorySlice, sliceIndex);
            cmd.SetComputeVectorParam(m_TemporalFilterCS, HDShaderIDs._DenoisingHistoryMask, channelMask);
            cmd.DispatchCompute(m_TemporalFilterCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            // Make sure to copy the new-accumulated signal in our history buffer
            m_KernelFilter = m_TemporalFilterCS.FindKernel(singleChannel ? "CopyHistorySingleArray" : "CopyHistoryColorArray");
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, outputSignal);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DenoiseSharedOutputTextureRW, historySignal);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._ValidityOutputTextureRW, validationHistory);
            cmd.SetComputeIntParam(m_TemporalFilterCS, HDShaderIDs._DenoisingHistorySlice, sliceIndex);
            cmd.SetComputeVectorParam(m_TemporalFilterCS, HDShaderIDs._DenoisingHistoryMask, channelMask);
            cmd.DispatchCompute(m_TemporalFilterCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);
        }
    }
}
