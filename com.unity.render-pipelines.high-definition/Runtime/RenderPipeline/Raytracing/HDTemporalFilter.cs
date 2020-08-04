using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    struct TemporalFilterParameters
    {
        // Camera parameters
        public int texWidth;
        public int texHeight;
        public int viewCount;

        // Denoising parameters
        public float historyValidity;
        public float pixelSpreadTangent;

        // Kernels
        public int validateHistoryKernel;
        public int temporalAccKernel;
        public int copyHistoryKernel;

        // Other parameters
        public ComputeShader temporalFilterCS;
    }

    struct TemporalFilterResources
    {
        // Input buffers
        public RTHandle depthStencilBuffer;
        public RTHandle normalBuffer;
        public RTHandle velocityBuffer;
        public RTHandle historyDepthTexture;
        public RTHandle historyNormalTexture;
        public RTHandle noisyBuffer;

        // Temporary buffers
        public RTHandle validationBuffer;

        // Output buffers
        public RTHandle historyBuffer;
        public RTHandle outputBuffer;
    }

    class HDTemporalFilter
    {
        // Resources used for the denoiser
        ComputeShader m_TemporalFilterCS;

        // Required for fetching depth and normal buffers
        SharedRTManager m_SharedRTManager;
        HDRenderPipeline m_RenderPipeline;

        // The set of required kernels
        int m_ValidateHistoryKernel;
        int m_TemporalAccumulationSingleKernel;
        int m_TemporalAccumulationColorKernel;
        int m_CopyHistorySingleKernel;
        int m_CopyHistoryColorKernel;

        public HDTemporalFilter()
        {
        }

        public void Init(HDRenderPipelineRayTracingResources rpRTResources, SharedRTManager sharedRTManager, HDRenderPipeline renderPipeline)
        {
            // Keep track of the resources
            m_TemporalFilterCS = rpRTResources.temporalFilterCS;

            // Keep track of the shared rt manager
            m_SharedRTManager = sharedRTManager;
            m_RenderPipeline = renderPipeline;

            // Grab all the kernels we'll eventually need
            m_ValidateHistoryKernel = m_TemporalFilterCS.FindKernel("ValidateHistory");
            m_TemporalAccumulationSingleKernel = m_TemporalFilterCS.FindKernel("TemporalAccumulationSingle");
            m_TemporalAccumulationColorKernel = m_TemporalFilterCS.FindKernel("TemporalAccumulationColor");
            m_CopyHistorySingleKernel = m_TemporalFilterCS.FindKernel("CopyHistorySingle");
            m_CopyHistoryColorKernel = m_TemporalFilterCS.FindKernel("CopyHistoryColor");
        }

        public void Release()
        {
        }

        public TemporalFilterParameters PrepareTemporalFilterParameters(HDCamera hdCamera, bool singleChannel, float historyValidity)
        {
            TemporalFilterParameters temporalFilterParameters = new TemporalFilterParameters();
            // Camera parameters
            temporalFilterParameters.texWidth = hdCamera.actualWidth;
            temporalFilterParameters.texHeight = hdCamera.actualHeight;
            temporalFilterParameters.viewCount = hdCamera.viewCount;

            // Denoising parameters
            temporalFilterParameters.pixelSpreadTangent = HDRenderPipeline.GetPixelSpreadTangent(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight);
            temporalFilterParameters.historyValidity = historyValidity;

            // Kernels
            temporalFilterParameters.validateHistoryKernel = m_ValidateHistoryKernel;
            temporalFilterParameters.temporalAccKernel = singleChannel ? m_TemporalAccumulationSingleKernel : m_TemporalAccumulationColorKernel;
            temporalFilterParameters.copyHistoryKernel = singleChannel ? m_CopyHistorySingleKernel : m_CopyHistoryColorKernel;

            // Other parameters
            temporalFilterParameters.temporalFilterCS = m_TemporalFilterCS;

            return temporalFilterParameters;
        }

        public TemporalFilterResources PrepareTemporalFilterResources(HDCamera hdCamera, RTHandle validationBuffer, RTHandle noisyBuffer, RTHandle historyBuffer, RTHandle outputBuffer)
        {
            TemporalFilterResources tfResources = new TemporalFilterResources();
            tfResources.depthStencilBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            tfResources.normalBuffer = m_SharedRTManager.GetNormalBuffer();
            tfResources.velocityBuffer = TextureXR.GetBlackTexture();
            tfResources.historyDepthTexture = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
            tfResources.historyNormalTexture = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Normal);
            tfResources.noisyBuffer = noisyBuffer;

            // Temporary buffers
            tfResources.validationBuffer = validationBuffer;

            // Output buffers
            tfResources.historyBuffer = historyBuffer;
            tfResources.outputBuffer = outputBuffer;

            return tfResources;
        }

        // Denoiser variant for non history array
        static public void DenoiseBuffer(CommandBuffer cmd, TemporalFilterParameters tfParameters, TemporalFilterResources tfResources)
        {
            // If we do not have a depth and normal history buffers, we can skip right away
            if (tfResources.historyDepthTexture == null || tfResources.historyNormalTexture == null)
            {
                HDUtils.BlitCameraTexture(cmd, tfResources.noisyBuffer, tfResources.historyBuffer);
                HDUtils.BlitCameraTexture(cmd, tfResources.noisyBuffer, tfResources.outputBuffer);
                return;
            }

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesX = (tfParameters.texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesY = (tfParameters.texHeight + (areaTileSize - 1)) / areaTileSize;

            // First of all we need to validate the history to know where we can or cannot use the history signal
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.validateHistoryKernel, HDShaderIDs._DepthTexture, tfResources.depthStencilBuffer);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.validateHistoryKernel, HDShaderIDs._HistoryDepthTexture, tfResources.historyDepthTexture);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.validateHistoryKernel, HDShaderIDs._NormalBufferTexture, tfResources.normalBuffer);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.validateHistoryKernel, HDShaderIDs._HistoryNormalTexture, tfResources.historyNormalTexture);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.validateHistoryKernel, HDShaderIDs._ValidationBufferRW, tfResources.validationBuffer);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.validateHistoryKernel, HDShaderIDs._VelocityBuffer, tfResources.velocityBuffer);
            cmd.SetComputeFloatParam(tfParameters.temporalFilterCS, HDShaderIDs._HistoryValidity, tfParameters.historyValidity);
            cmd.SetComputeFloatParam(tfParameters.temporalFilterCS, HDShaderIDs._PixelSpreadAngleTangent, tfParameters.pixelSpreadTangent);
            cmd.DispatchCompute(tfParameters.temporalFilterCS, tfParameters.validateHistoryKernel, numTilesX, numTilesY, tfParameters.viewCount);

            // Now that we have validated our history, let's accumulate
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.temporalAccKernel, HDShaderIDs._DenoiseInputTexture, tfResources.noisyBuffer);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.temporalAccKernel, HDShaderIDs._HistoryBuffer, tfResources.historyBuffer);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.temporalAccKernel, HDShaderIDs._DepthTexture, tfResources.depthStencilBuffer);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.temporalAccKernel, HDShaderIDs._DenoiseOutputTextureRW, tfResources.outputBuffer);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.temporalAccKernel, HDShaderIDs._ValidationBuffer, tfResources.validationBuffer);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.temporalAccKernel, HDShaderIDs._VelocityBuffer, tfResources.velocityBuffer);
            cmd.DispatchCompute(tfParameters.temporalFilterCS, tfParameters.temporalAccKernel, numTilesX, numTilesY, tfParameters.viewCount);

            // Make sure to copy the new-accumulated signal in our history buffer
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.copyHistoryKernel, HDShaderIDs._DenoiseInputTexture, tfResources.outputBuffer);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.copyHistoryKernel, HDShaderIDs._DenoiseOutputTextureRW, tfResources.historyBuffer);
            cmd.DispatchCompute(tfParameters.temporalFilterCS, tfParameters.copyHistoryKernel, numTilesX, numTilesY, tfParameters.viewCount);
        }

        // Denoiser variant when history is stored in an array and the validation buffer is seperate
        public void DenoiseBuffer(CommandBuffer cmd, HDCamera hdCamera,
            RTHandle noisySignal, RTHandle historySignal,
            RTHandle validationHistory,
            RTHandle velocityBuffer,
            RTHandle outputSignal,
            int sliceIndex, Vector4 channelMask,
            RTHandle distanceSignal, RTHandle distanceHistorySignal, RTHandle outputDistanceSignal, Vector4 distanceChannelMask,
            bool singleChannel = true, float historyValidity = 1.0f)
        {
            // If we do not have a depth and normal history buffers, we can skip right away
            var historyDepthBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
            var historyNormalBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Normal);
            if (historyDepthBuffer == null || historyNormalBuffer == null)
            {
                HDUtils.BlitCameraTexture(cmd, noisySignal, historySignal);
                HDUtils.BlitCameraTexture(cmd, noisySignal, outputSignal);
                if (distanceSignal != null && distanceHistorySignal != null && outputDistanceSignal != null)
                {
                    HDUtils.BlitCameraTexture(cmd, distanceSignal, distanceHistorySignal);
                    HDUtils.BlitCameraTexture(cmd, distanceSignal, outputDistanceSignal);
                }
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

            // First of all we need to validate the history to know where we can or cannot use the history signal
            int m_KernelFilter = m_TemporalFilterCS.FindKernel("ValidateHistory");
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._HistoryDepthTexture, historyDepthBuffer);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._HistoryNormalTexture, historyNormalBuffer);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._ValidationBufferRW, validationBuffer);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._VelocityBuffer, velocityBuffer);
            cmd.SetComputeFloatParam(m_TemporalFilterCS, HDShaderIDs._HistoryValidity, historyValidity);
            cmd.SetComputeFloatParam(m_TemporalFilterCS, HDShaderIDs._PixelSpreadAngleTangent, HDRenderPipeline.GetPixelSpreadTangent(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight));
            cmd.DispatchCompute(m_TemporalFilterCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            // Now that we have validated our history, let's accumulate
            m_KernelFilter = m_TemporalFilterCS.FindKernel(singleChannel ? "TemporalAccumulationSingleArray" : "TemporalAccumulationColorArray");
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, noisySignal);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._HistoryBuffer, historySignal);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._HistoryValidityBuffer, validationHistory);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, outputSignal);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._ValidationBuffer, validationBuffer);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._VelocityBuffer, velocityBuffer);
            cmd.SetComputeIntParam(m_TemporalFilterCS, HDShaderIDs._DenoisingHistorySlice, sliceIndex);
            cmd.SetComputeVectorParam(m_TemporalFilterCS, HDShaderIDs._DenoisingHistoryMask, channelMask);
            cmd.DispatchCompute(m_TemporalFilterCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            // Make sure to copy the new-accumulated signal in our history buffer
            m_KernelFilter = m_TemporalFilterCS.FindKernel(singleChannel ? "CopyHistorySingleArray" : "CopyHistoryColorArray");
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, outputSignal);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, historySignal);
            cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._ValidityOutputTextureRW, validationHistory);
            cmd.SetComputeIntParam(m_TemporalFilterCS, HDShaderIDs._DenoisingHistorySlice, sliceIndex);
            cmd.SetComputeVectorParam(m_TemporalFilterCS, HDShaderIDs._DenoisingHistoryMask, channelMask);
            cmd.DispatchCompute(m_TemporalFilterCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

            if (distanceSignal != null && distanceHistorySignal != null && outputDistanceSignal != null)
            {
                // Now that we have validated our history, let's accumulate
                m_KernelFilter = m_TemporalFilterCS.FindKernel("TemporalAccumulationSingleArray");

                // Bind the intput buffers
                cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, distanceSignal);
                cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._HistoryBuffer, distanceHistorySignal);
                cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._HistoryValidityBuffer, validationHistory);
                cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._ValidationBuffer, validationBuffer);
                cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._VelocityBuffer, velocityBuffer);

                // Bind the constant inputs
                cmd.SetComputeIntParam(m_TemporalFilterCS, HDShaderIDs._DenoisingHistorySlice, sliceIndex);
                cmd.SetComputeVectorParam(m_TemporalFilterCS, HDShaderIDs._DenoisingHistoryMask, distanceChannelMask);

                // Bind the output buffers
                cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, outputDistanceSignal);

                // Dispatch the temporal accumulation
                cmd.DispatchCompute(m_TemporalFilterCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);

                // Make sure to copy the new-accumulated signal in our history buffer
                m_KernelFilter = m_TemporalFilterCS.FindKernel("CopyHistorySingleArrayNoValidity");
                cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DenoiseInputTexture, outputDistanceSignal);
                cmd.SetComputeTextureParam(m_TemporalFilterCS, m_KernelFilter, HDShaderIDs._DenoiseOutputTextureRW, distanceHistorySignal);
                cmd.SetComputeIntParam(m_TemporalFilterCS, HDShaderIDs._DenoisingHistorySlice, sliceIndex);
                cmd.SetComputeVectorParam(m_TemporalFilterCS, HDShaderIDs._DenoisingHistoryMask, distanceChannelMask);
                cmd.DispatchCompute(m_TemporalFilterCS, m_KernelFilter, numTilesX, numTilesY, hdCamera.viewCount);
            }
        }
    }
}
