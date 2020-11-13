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
        public RTHandle motionVectorBuffer;
        public RTHandle historyDepthTexture;
        public RTHandle historyNormalTexture;
        public RTHandle noisyBuffer;

        // Temporary buffers
        public RTHandle validationBuffer;

        // Output buffers
        public RTHandle historyBuffer;
        public RTHandle outputBuffer;
    }

    partial class HDTemporalFilter
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
        int m_TemporalAccumulationSingleArrayKernel;
        int m_TemporalAccumulationColorArrayKernel;
        int m_CopyHistorySingleArrayKernel;
        int m_CopyHistoryColorArrayKernel;
        int m_CopyHistorySingleArrayNoValidityKernel;

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

            m_TemporalAccumulationSingleArrayKernel = m_TemporalFilterCS.FindKernel("TemporalAccumulationSingleArray");
            m_TemporalAccumulationColorArrayKernel = m_TemporalFilterCS.FindKernel("TemporalAccumulationColorArray");

            m_CopyHistorySingleArrayKernel = m_TemporalFilterCS.FindKernel("CopyHistorySingleArray");
            m_CopyHistoryColorArrayKernel = m_TemporalFilterCS.FindKernel("CopyHistoryColorArray");

            m_CopyHistorySingleArrayNoValidityKernel = m_TemporalFilterCS.FindKernel("CopyHistorySingleArrayNoValidity");
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
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.MotionVectors))
                tfResources.motionVectorBuffer = m_SharedRTManager.GetMotionVectorsBuffer();
            else
                tfResources.motionVectorBuffer = TextureXR.GetBlackTexture();

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
            // Bind the input buffers
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.validateHistoryKernel, HDShaderIDs._DepthTexture, tfResources.depthStencilBuffer);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.validateHistoryKernel, HDShaderIDs._HistoryDepthTexture, tfResources.historyDepthTexture);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.validateHistoryKernel, HDShaderIDs._NormalBufferTexture, tfResources.normalBuffer);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.validateHistoryKernel, HDShaderIDs._HistoryNormalTexture, tfResources.historyNormalTexture);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.validateHistoryKernel, HDShaderIDs._VelocityBuffer, tfResources.velocityBuffer);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.validateHistoryKernel, HDShaderIDs._CameraMotionVectorsTexture, tfResources.motionVectorBuffer);

            // Bind the constants
            cmd.SetComputeFloatParam(tfParameters.temporalFilterCS, HDShaderIDs._HistoryValidity, tfParameters.historyValidity);
            cmd.SetComputeFloatParam(tfParameters.temporalFilterCS, HDShaderIDs._PixelSpreadAngleTangent, tfParameters.pixelSpreadTangent);

            // Bind the output buffer
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.validateHistoryKernel, HDShaderIDs._ValidationBufferRW, tfResources.validationBuffer);

            // Evaluate the validity
            cmd.DispatchCompute(tfParameters.temporalFilterCS, tfParameters.validateHistoryKernel, numTilesX, numTilesY, tfParameters.viewCount);

            // Now that we have validated our history, let's accumulate
            // Bind the input buffers
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.temporalAccKernel, HDShaderIDs._DenoiseInputTexture, tfResources.noisyBuffer);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.temporalAccKernel, HDShaderIDs._HistoryBuffer, tfResources.historyBuffer);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.temporalAccKernel, HDShaderIDs._DepthTexture, tfResources.depthStencilBuffer);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.temporalAccKernel, HDShaderIDs._ValidationBuffer, tfResources.validationBuffer);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.temporalAccKernel, HDShaderIDs._VelocityBuffer, tfResources.velocityBuffer);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.temporalAccKernel, HDShaderIDs._CameraMotionVectorsTexture, tfResources.motionVectorBuffer);

            // Bind the output buffer
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.temporalAccKernel, HDShaderIDs._DenoiseOutputTextureRW, tfResources.outputBuffer);

            // Combine signal with history
            cmd.DispatchCompute(tfParameters.temporalFilterCS, tfParameters.temporalAccKernel, numTilesX, numTilesY, tfParameters.viewCount);

            // Make sure to copy the new-accumulated signal in our history buffer
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.copyHistoryKernel, HDShaderIDs._DenoiseInputTexture, tfResources.outputBuffer);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.copyHistoryKernel, HDShaderIDs._DenoiseOutputTextureRW, tfResources.historyBuffer);
            cmd.DispatchCompute(tfParameters.temporalFilterCS, tfParameters.copyHistoryKernel, numTilesX, numTilesY, tfParameters.viewCount);
        }

        struct TemporalFilterArrayParameters
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Denoising parameters
            public bool distanceBasedDenoiser;
            public float historyValidity;
            public float pixelSpreadTangent;
            public int sliceIndex;
            public Vector4 channelMask;
            public Vector4 distanceChannelMask;

            // Kernels
            public int validateHistoryKernel;
            public int temporalAccKernel;
            public int copyHistoryKernel;
            public int temporalAccSingleKernel;
            public int copyHistoryNoValidityKernel;

            // Other parameters
            public ComputeShader temporalFilterCS;
        }

        TemporalFilterArrayParameters PrepareTemporalFilterArrayParameters(HDCamera hdCamera, bool distanceBased, bool singleChannel, float historyValidity, int sliceIndex, Vector4 channelMask, Vector4 distanceChannelMask)
        {
            TemporalFilterArrayParameters tfaParams = new TemporalFilterArrayParameters();
        
            // Set the camera parameters
            tfaParams.texWidth = hdCamera.actualWidth;
            tfaParams.texHeight = hdCamera.actualHeight;
            tfaParams.viewCount = hdCamera.viewCount;

            // Denoising parameters
            tfaParams.distanceBasedDenoiser = distanceBased;
            tfaParams.historyValidity = historyValidity;
            tfaParams.pixelSpreadTangent = HDRenderPipeline.GetPixelSpreadTangent(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight);
            tfaParams.sliceIndex = sliceIndex;
            tfaParams.channelMask = channelMask;
            tfaParams.distanceChannelMask = distanceChannelMask;

            // Kernels
            tfaParams.validateHistoryKernel = m_ValidateHistoryKernel;
            tfaParams.temporalAccKernel = singleChannel ? m_TemporalAccumulationSingleArrayKernel : m_TemporalAccumulationColorArrayKernel;
            tfaParams.copyHistoryKernel = singleChannel ? m_CopyHistorySingleArrayKernel : m_CopyHistoryColorArrayKernel;
            tfaParams.temporalAccSingleKernel = m_TemporalAccumulationSingleArrayKernel;
            tfaParams.copyHistoryNoValidityKernel = m_CopyHistorySingleArrayNoValidityKernel;

            // Other parameters
            tfaParams.temporalFilterCS = m_TemporalFilterCS;

            return tfaParams;
        }

        struct TemporalFilterArrayResources
        {
            // Input buffers
            public RTHandle depthStencilBuffer;
            public RTHandle normalBuffer;
            public RTHandle velocityBuffer;
            public RTHandle historyDepthTexture;
            public RTHandle historyNormalTexture;
            public RTHandle noisyBuffer;
            public RTHandle distanceBuffer;
            public RTHandle motionVectorBuffer;

            // Temporary buffers
            public RTHandle validationBuffer;

            // InOutput buffers
            public RTHandle historyBuffer;
            public RTHandle validationHistoryBuffer;
            public RTHandle distanceHistorySignal;

            // Output buffers
            public RTHandle outputBuffer;
            public RTHandle outputDistanceSignal;
        }

        TemporalFilterArrayResources PrepareTemporalFilterArrayResources(HDCamera hdCamera, RTHandle noisyBuffer, RTHandle distanceBuffer, RTHandle validationBuffer,
                                                                        RTHandle historyBuffer, RTHandle validationHistoryBuffer, RTHandle distanceHistorySignal,
                                                                        RTHandle outputBuffer, RTHandle outputDistanceSignal)
        {
            TemporalFilterArrayResources tfaResources = new TemporalFilterArrayResources();

            // Input buffers
            tfaResources.depthStencilBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            tfaResources.normalBuffer = m_SharedRTManager.GetNormalBuffer();
            tfaResources.velocityBuffer = TextureXR.GetBlackTexture();
            tfaResources.historyDepthTexture = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
            tfaResources.historyNormalTexture = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Normal);
            tfaResources.noisyBuffer = noisyBuffer;
            tfaResources.distanceBuffer = distanceBuffer;
            tfaResources.motionVectorBuffer = m_SharedRTManager.GetMotionVectorsBuffer();

            // Temporary buffers
            tfaResources.validationBuffer = validationBuffer;

            // InOut buffers
            tfaResources.historyBuffer = historyBuffer;
            tfaResources.validationHistoryBuffer = validationHistoryBuffer;
            tfaResources.distanceHistorySignal = distanceHistorySignal;

            // Output buffers
            tfaResources.outputBuffer = outputBuffer;
            tfaResources.outputDistanceSignal = outputDistanceSignal;

            return tfaResources;
        }

        static void ExecuteTemporalFilterArray(CommandBuffer cmd, TemporalFilterArrayParameters tfaParams, TemporalFilterArrayResources tfaResources)
        {
            if (tfaResources.historyDepthTexture == null || tfaResources.historyNormalTexture == null)
            {
                HDUtils.BlitCameraTexture(cmd, tfaResources.noisyBuffer, tfaResources.historyBuffer);
                HDUtils.BlitCameraTexture(cmd, tfaResources.noisyBuffer, tfaResources.outputBuffer);
                if (tfaParams.distanceBasedDenoiser)
                {
                    HDUtils.BlitCameraTexture(cmd, tfaResources.distanceBuffer, tfaResources.distanceHistorySignal);
                    HDUtils.BlitCameraTexture(cmd, tfaResources.distanceBuffer, tfaResources.outputDistanceSignal);
                }
                return;
            }

            // Evaluate the dispatch parameters
            int tfTileSize = 8;
            int numTilesX = (tfaParams.texWidth + (tfTileSize - 1)) / tfTileSize;
            int numTilesY = (tfaParams.texHeight + (tfTileSize - 1)) / tfTileSize;

            // First of all we need to validate the history to know where we can or cannot use the history signal
            // Bind all the input buffers
            cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.validateHistoryKernel, HDShaderIDs._DepthTexture, tfaResources.depthStencilBuffer);
            cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.validateHistoryKernel, HDShaderIDs._HistoryDepthTexture, tfaResources.historyDepthTexture);
            cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.validateHistoryKernel, HDShaderIDs._NormalBufferTexture, tfaResources.normalBuffer);
            cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.validateHistoryKernel, HDShaderIDs._HistoryNormalTexture, tfaResources.historyNormalTexture);
            cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.validateHistoryKernel, HDShaderIDs._CameraMotionVectorsTexture, tfaResources.motionVectorBuffer);
            cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.validateHistoryKernel, HDShaderIDs._VelocityBuffer, tfaResources.velocityBuffer);

            // Bind the constants
            cmd.SetComputeFloatParam(tfaParams.temporalFilterCS, HDShaderIDs._HistoryValidity, tfaParams.historyValidity);
            cmd.SetComputeFloatParam(tfaParams.temporalFilterCS, HDShaderIDs._PixelSpreadAngleTangent, tfaParams.pixelSpreadTangent);

            // Bind the output buffer
            cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.validateHistoryKernel, HDShaderIDs._ValidationBufferRW, tfaResources.validationBuffer);

            // Evaluate the validity
            cmd.DispatchCompute(tfaParams.temporalFilterCS, tfaParams.validateHistoryKernel, numTilesX, numTilesY, tfaParams.viewCount);

            // Now that we have validated our history, let's accumulate
            cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.temporalAccKernel, HDShaderIDs._DenoiseInputTexture, tfaResources.noisyBuffer);
            cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.temporalAccKernel, HDShaderIDs._HistoryBuffer, tfaResources.historyBuffer);
            cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.temporalAccKernel, HDShaderIDs._HistoryValidityBuffer, tfaResources.validationHistoryBuffer);
            cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.temporalAccKernel, HDShaderIDs._DepthTexture, tfaResources.depthStencilBuffer);
            cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.temporalAccKernel, HDShaderIDs._CameraMotionVectorsTexture, tfaResources.motionVectorBuffer);
            cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.temporalAccKernel, HDShaderIDs._ValidationBuffer, tfaResources.validationBuffer);
            cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.temporalAccKernel, HDShaderIDs._VelocityBuffer, tfaResources.velocityBuffer);

            // Bind the constants
            cmd.SetComputeIntParam(tfaParams.temporalFilterCS, HDShaderIDs._DenoisingHistorySlice, tfaParams.sliceIndex);
            cmd.SetComputeVectorParam(tfaParams.temporalFilterCS, HDShaderIDs._DenoisingHistoryMask, tfaParams.channelMask);

            // Bind the output buffer
            cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.temporalAccKernel, HDShaderIDs._DenoiseOutputTextureRW, tfaResources.outputBuffer);

            // Combine with the history
            cmd.DispatchCompute(tfaParams.temporalFilterCS, tfaParams.temporalAccKernel, numTilesX, numTilesY, tfaParams.viewCount);

            // Make sure to copy the new-accumulated signal in our history buffer
            cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.copyHistoryKernel, HDShaderIDs._DenoiseInputTexture, tfaResources.outputBuffer);
            cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.copyHistoryKernel, HDShaderIDs._DenoiseOutputTextureRW, tfaResources.historyBuffer);
            cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.copyHistoryKernel, HDShaderIDs._ValidityOutputTextureRW, tfaResources.validationHistoryBuffer);
            cmd.SetComputeIntParam(tfaParams.temporalFilterCS, HDShaderIDs._DenoisingHistorySlice, tfaParams.sliceIndex);
            cmd.SetComputeVectorParam(tfaParams.temporalFilterCS, HDShaderIDs._DenoisingHistoryMask, tfaParams.channelMask);
            cmd.DispatchCompute(tfaParams.temporalFilterCS, tfaParams.copyHistoryKernel, numTilesX, numTilesY, tfaParams.viewCount);

            if (tfaParams.distanceBasedDenoiser)
            {
                // Bind the input buffers
                cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.temporalAccSingleKernel, HDShaderIDs._DenoiseInputTexture, tfaResources.distanceBuffer);
                cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.temporalAccSingleKernel, HDShaderIDs._HistoryBuffer, tfaResources.distanceHistorySignal);
                cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.temporalAccSingleKernel, HDShaderIDs._HistoryValidityBuffer, tfaResources.validationHistoryBuffer);
                cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.temporalAccSingleKernel, HDShaderIDs._DepthTexture, tfaResources.depthStencilBuffer);
                cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.temporalAccSingleKernel, HDShaderIDs._ValidationBuffer, tfaResources.validationBuffer);
                cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.temporalAccSingleKernel, HDShaderIDs._VelocityBuffer, tfaResources.velocityBuffer);
                cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.temporalAccSingleKernel, HDShaderIDs._CameraMotionVectorsTexture, tfaResources.motionVectorBuffer);

                // Bind the constant inputs
                cmd.SetComputeIntParam(tfaParams.temporalFilterCS, HDShaderIDs._DenoisingHistorySlice, tfaParams.sliceIndex);
                cmd.SetComputeVectorParam(tfaParams.temporalFilterCS, HDShaderIDs._DenoisingHistoryMask, tfaParams.distanceChannelMask);

                // Bind the output buffers
                cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.temporalAccSingleKernel, HDShaderIDs._DenoiseOutputTextureRW, tfaResources.outputDistanceSignal);

                // Dispatch the temporal accumulation
                cmd.DispatchCompute(tfaParams.temporalFilterCS, tfaParams.temporalAccSingleKernel, numTilesX, numTilesY, tfaParams.viewCount);

                // Make sure to copy the new-accumulated signal in our history buffer
                cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.copyHistoryNoValidityKernel, HDShaderIDs._DenoiseInputTexture, tfaResources.outputDistanceSignal);
                cmd.SetComputeTextureParam(tfaParams.temporalFilterCS, tfaParams.copyHistoryNoValidityKernel, HDShaderIDs._DenoiseOutputTextureRW, tfaResources.distanceHistorySignal);
                cmd.SetComputeIntParam(tfaParams.temporalFilterCS, HDShaderIDs._DenoisingHistorySlice, tfaParams.sliceIndex);
                cmd.SetComputeVectorParam(tfaParams.temporalFilterCS, HDShaderIDs._DenoisingHistoryMask, tfaParams.distanceChannelMask);
                cmd.DispatchCompute(tfaParams.temporalFilterCS, tfaParams.copyHistoryNoValidityKernel, numTilesX, numTilesY, tfaParams.viewCount);
            }
        }

        // Denoiser variant when history is stored in an array and the validation buffer is separate
        public void DenoiseBuffer(CommandBuffer cmd, HDCamera hdCamera,
            RTHandle noisyBuffer, RTHandle historyBuffer,
            RTHandle validationHistoryBuffer,
            RTHandle velocityBuffer,
            RTHandle outputBuffer,
            int sliceIndex, Vector4 channelMask,
            RTHandle distanceBuffer, RTHandle distanceHistorySignal, RTHandle outputDistanceSignal, Vector4 distanceChannelMask,
            bool distanceBased, bool singleChannel = true, float historyValidity = 1.0f)
        {
            // If we do not have a depth and normal history buffers, we can skip right away
            var historyDepthBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
            var historyNormalBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Normal);

            // Request the intermediate buffer we need
            RTHandle validationBuffer = m_RenderPipeline.GetRayTracingBuffer(InternalRayTracingBuffers.R0);

            TemporalFilterArrayParameters tfaParams = PrepareTemporalFilterArrayParameters(hdCamera, distanceBased, singleChannel, historyValidity, sliceIndex, channelMask, distanceChannelMask);
            TemporalFilterArrayResources tfaResources = PrepareTemporalFilterArrayResources(hdCamera, noisyBuffer, distanceBuffer, validationBuffer,
                                                                                            historyBuffer, validationHistoryBuffer, distanceHistorySignal,
                                                                                            outputBuffer, outputDistanceSignal);
            ExecuteTemporalFilterArray(cmd, tfaParams, tfaResources);
        }
    }
}
