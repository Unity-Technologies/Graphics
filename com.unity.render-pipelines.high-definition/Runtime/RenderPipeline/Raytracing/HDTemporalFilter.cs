using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    struct HistoryValidityParameters
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

        // Other parameters
        public ComputeShader temporalFilterCS;
    }

    struct HistoryValidityResources
    {
        // Input buffers
        public RTHandle depthStencilBuffer;
        public RTHandle normalBuffer;
        public RTHandle motionVectorBuffer;
        public RTHandle historyDepthTexture;
        public RTHandle historyNormalTexture;

        // Output buffer
        public RTHandle validationBuffer;
    }

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
        public int temporalAccKernel;
        public int copyHistoryKernel;

        // Other parameters
        public ComputeShader temporalFilterCS;
    }

    struct TemporalFilterResources
    {
        // Prepass buffers
        public RTHandle depthStencilBuffer;
        public RTHandle normalBuffer;
        public RTHandle motionVectorBuffer;

        // Effect buffers
        public RTHandle noisyBuffer;
        public RTHandle validationBuffer;
        public RTHandle velocityBuffer;

        // Output buffers
        public RTHandle historyBuffer;
        public RTHandle outputBuffer;
    }

    partial class HDTemporalFilter
    {
        // Resources used for the denoiser
        ComputeShader m_TemporalFilterCS;

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

        public void Init(HDRenderPipelineRayTracingResources rpRTResources)
        {
            // Keep track of the resources
            m_TemporalFilterCS = rpRTResources.temporalFilterCS;

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

        public HistoryValidityParameters PrepareHistoryValidityParameters(HDCamera hdCamera, float historyValidity)
        {
            HistoryValidityParameters parameters = new HistoryValidityParameters();
            // Camera parameters
            parameters.texWidth = hdCamera.actualWidth;
            parameters.texHeight = hdCamera.actualHeight;
            parameters.viewCount = hdCamera.viewCount;

            // Denoising parameters
            parameters.pixelSpreadTangent = HDRenderPipeline.GetPixelSpreadTangent(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight);
            parameters.historyValidity = historyValidity;

            // Kernels
            parameters.validateHistoryKernel = m_ValidateHistoryKernel;

            // Other parameters
            parameters.temporalFilterCS = m_TemporalFilterCS;

            return parameters;
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
            temporalFilterParameters.temporalAccKernel = singleChannel ? m_TemporalAccumulationSingleKernel : m_TemporalAccumulationColorKernel;
            temporalFilterParameters.copyHistoryKernel = singleChannel ? m_CopyHistorySingleKernel : m_CopyHistoryColorKernel;

            // Other parameters
            temporalFilterParameters.temporalFilterCS = m_TemporalFilterCS;

            return temporalFilterParameters;
        }

        // Function that evaluates the history validation Buffer
        static public void ExecuteHistoryValidity(CommandBuffer cmd, HistoryValidityParameters parameters, HistoryValidityResources resources)
        {
            // If we do not have a depth and normal history buffers, we can skip right away
            if (resources.historyDepthTexture == null || resources.historyNormalTexture == null)
            {
                CoreUtils.SetRenderTarget(cmd, resources.validationBuffer, clearFlag: ClearFlag.Color, Color.black);
                return;
            }

            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesX = (parameters.texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesY = (parameters.texHeight + (areaTileSize - 1)) / areaTileSize;

            // First of all we need to validate the history to know where we can or cannot use the history signal
            // Bind the input buffers
            cmd.SetComputeTextureParam(parameters.temporalFilterCS, parameters.validateHistoryKernel, HDShaderIDs._DepthTexture, resources.depthStencilBuffer);
            cmd.SetComputeTextureParam(parameters.temporalFilterCS, parameters.validateHistoryKernel, HDShaderIDs._HistoryDepthTexture, resources.historyDepthTexture);
            cmd.SetComputeTextureParam(parameters.temporalFilterCS, parameters.validateHistoryKernel, HDShaderIDs._NormalBufferTexture, resources.normalBuffer);
            cmd.SetComputeTextureParam(parameters.temporalFilterCS, parameters.validateHistoryKernel, HDShaderIDs._HistoryNormalTexture, resources.historyNormalTexture);
            cmd.SetComputeTextureParam(parameters.temporalFilterCS, parameters.validateHistoryKernel, HDShaderIDs._CameraMotionVectorsTexture, resources.motionVectorBuffer);
            cmd.SetComputeTextureParam(parameters.temporalFilterCS, parameters.validateHistoryKernel, HDShaderIDs._StencilTexture, resources.depthStencilBuffer, 0, RenderTextureSubElement.Stencil);

            // Bind the constants
            cmd.SetComputeFloatParam(parameters.temporalFilterCS, HDShaderIDs._HistoryValidity, parameters.historyValidity);
            cmd.SetComputeFloatParam(parameters.temporalFilterCS, HDShaderIDs._PixelSpreadAngleTangent, parameters.pixelSpreadTangent);
            cmd.SetComputeIntParam(parameters.temporalFilterCS, HDShaderIDs._ObjectMotionStencilBit, (int)StencilUsage.ObjectMotionVector);

            // Bind the output buffer
            cmd.SetComputeTextureParam(parameters.temporalFilterCS, parameters.validateHistoryKernel, HDShaderIDs._ValidationBufferRW, resources.validationBuffer);

            // Evaluate the validity
            cmd.DispatchCompute(parameters.temporalFilterCS, parameters.validateHistoryKernel, numTilesX, numTilesY, parameters.viewCount);
        }

        // Denoiser variant for non history array
        static public void DenoiseBuffer(CommandBuffer cmd, TemporalFilterParameters tfParameters, TemporalFilterResources tfResources)
        {
            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesX = (tfParameters.texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesY = (tfParameters.texHeight + (areaTileSize - 1)) / areaTileSize;

            // Now that we have validated our history, let's accumulate
            // Bind the input buffers
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.temporalAccKernel, HDShaderIDs._DenoiseInputTexture, tfResources.noisyBuffer);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.temporalAccKernel, HDShaderIDs._HistoryBuffer, tfResources.historyBuffer);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.temporalAccKernel, HDShaderIDs._DepthTexture, tfResources.depthStencilBuffer);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.temporalAccKernel, HDShaderIDs._ValidationBuffer, tfResources.validationBuffer);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.temporalAccKernel, HDShaderIDs._VelocityBuffer, tfResources.velocityBuffer);
            cmd.SetComputeTextureParam(tfParameters.temporalFilterCS, tfParameters.temporalAccKernel, HDShaderIDs._CameraMotionVectorsTexture, tfResources.motionVectorBuffer);
            cmd.SetComputeFloatParam(tfParameters.temporalFilterCS, HDShaderIDs._HistoryValidity, tfParameters.historyValidity);

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
            // Prepass buffers
            public RTHandle depthStencilBuffer;
            public RTHandle normalBuffer;
            public RTHandle motionVectorBuffer;

            // Effects buffer
            public RTHandle velocityBuffer;
            public RTHandle noisyBuffer;
            public RTHandle distanceBuffer;
            public RTHandle validationBuffer;

            // InOutput buffers
            public RTHandle historyBuffer;
            public RTHandle validationHistoryBuffer;
            public RTHandle distanceHistorySignal;

            // Output buffers
            public RTHandle outputBuffer;
            public RTHandle outputDistanceSignal;
        }

        static void ExecuteTemporalFilterArray(CommandBuffer cmd, TemporalFilterArrayParameters tfaParams, TemporalFilterArrayResources tfaResources)
        {
            // Evaluate the dispatch parameters
            int tfTileSize = 8;
            int numTilesX = (tfaParams.texWidth + (tfTileSize - 1)) / tfTileSize;
            int numTilesY = (tfaParams.texHeight + (tfTileSize - 1)) / tfTileSize;

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
            cmd.SetComputeFloatParam(tfaParams.temporalFilterCS, HDShaderIDs._HistoryValidity, tfaParams.historyValidity);

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
    }
}
