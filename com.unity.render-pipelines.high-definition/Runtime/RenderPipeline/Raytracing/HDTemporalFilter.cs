using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    class HDTemporalFilter
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

        class HistoryValidityPassData
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

            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle motionVectorBuffer;
            public TextureHandle historyDepthTexture;
            public TextureHandle historyNormalTexture;
            public TextureHandle validationBuffer;
        }

        // Function that evaluates the history validation Buffer
        public TextureHandle HistoryValidity(RenderGraph renderGraph, HDCamera hdCamera, float historyValidity,
            TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectorBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<HistoryValidityPassData>("History Validity Evaluation", out var passData, ProfilingSampler.Get(HDProfileId.HistoryValidity)))
            {
                // Cannot run in async
                builder.EnableAsyncCompute(false);

                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Denoising parameters
                passData.pixelSpreadTangent = HDRenderPipeline.GetPixelSpreadTangent(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight);
                passData.historyValidity = historyValidity;

                // Kernels
                passData.validateHistoryKernel = m_ValidateHistoryKernel;

                // Other parameters
                passData.temporalFilterCS = m_TemporalFilterCS;

                // Input Buffers
                passData.depthStencilBuffer = builder.ReadTexture(depthBuffer);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.motionVectorBuffer = builder.ReadTexture(motionVectorBuffer);

                // History buffers
                passData.historyDepthTexture = builder.ReadTexture(renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth)));
                passData.historyNormalTexture = builder.ReadTexture(renderGraph.ImportTexture(hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Normal)));

                // Output buffers
                passData.validationBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R8_UNorm, enableRandomWrite = true, name = "ValidationTexture" }));

                builder.SetRenderFunc(
                    (HistoryValidityPassData data, RenderGraphContext ctx) =>
                    {
                        RTHandle historyDepthTexture = data.historyDepthTexture;
                        RTHandle historyNormalTexture = data.historyNormalTexture;
                        // If we do not have a depth and normal history buffers, we can skip right away
                        if (historyDepthTexture == null || historyNormalTexture == null)
                        {
                            CoreUtils.SetRenderTarget(ctx.cmd, data.validationBuffer, clearFlag: ClearFlag.Color, Color.black);
                            return;
                        }

                        // Evaluate the dispatch parameters
                        int areaTileSize = 8;
                        int numTilesX = (data.texWidth + (areaTileSize - 1)) / areaTileSize;
                        int numTilesY = (data.texHeight + (areaTileSize - 1)) / areaTileSize;

                        // First of all we need to validate the history to know where we can or cannot use the history signal
                        // Bind the input buffers
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.validateHistoryKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.validateHistoryKernel, HDShaderIDs._HistoryDepthTexture, data.historyDepthTexture);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.validateHistoryKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.validateHistoryKernel, HDShaderIDs._HistoryNormalTexture, data.historyNormalTexture);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.validateHistoryKernel, HDShaderIDs._CameraMotionVectorsTexture, data.motionVectorBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.validateHistoryKernel, HDShaderIDs._StencilTexture, data.depthStencilBuffer, 0, RenderTextureSubElement.Stencil);

                        // Bind the constants
                        ctx.cmd.SetComputeFloatParam(data.temporalFilterCS, HDShaderIDs._HistoryValidity, data.historyValidity);
                        ctx.cmd.SetComputeFloatParam(data.temporalFilterCS, HDShaderIDs._PixelSpreadAngleTangent, data.pixelSpreadTangent);
                        ctx.cmd.SetComputeIntParam(data.temporalFilterCS, HDShaderIDs._ObjectMotionStencilBit, (int)StencilUsage.ObjectMotionVector);

                        // Bind the output buffer
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.validateHistoryKernel, HDShaderIDs._ValidationBufferRW, data.validationBuffer);

                        // Evaluate the validity
                        ctx.cmd.DispatchCompute(data.temporalFilterCS, data.validateHistoryKernel, numTilesX, numTilesY, data.viewCount);
                    });
                return passData.validationBuffer;
            }
        }

        class TemporalFilterPassData
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

            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle motionVectorBuffer;
            public TextureHandle velocityBuffer;
            public TextureHandle noisyBuffer;
            public TextureHandle validationBuffer;
            public TextureHandle historyBuffer;
            public TextureHandle outputBuffer;
        }

        // Denoiser variant for non history array
        public TextureHandle Denoise(RenderGraph renderGraph, HDCamera hdCamera, bool singleChannel, float historyValidity,
            TextureHandle noisyBuffer, TextureHandle velocityBuffer,
            TextureHandle historyBuffer,
            TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectorBuffer, TextureHandle historyValidationBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<TemporalFilterPassData>("TemporalDenoiser", out var passData, ProfilingSampler.Get(HDProfileId.TemporalFilter)))
            {
                // Cannot run in async
                builder.EnableAsyncCompute(false);

                // Camera parameters
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Denoising parameters
                passData.pixelSpreadTangent = HDRenderPipeline.GetPixelSpreadTangent(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight);
                passData.historyValidity = historyValidity;

                // Kernels
                passData.temporalAccKernel = singleChannel ? m_TemporalAccumulationSingleKernel : m_TemporalAccumulationColorKernel;
                passData.copyHistoryKernel = singleChannel ? m_CopyHistorySingleKernel : m_CopyHistoryColorKernel;

                // Other parameters
                passData.temporalFilterCS = m_TemporalFilterCS;

                // Prepass Buffers
                passData.depthStencilBuffer = builder.ReadTexture(depthBuffer);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.motionVectorBuffer = builder.ReadTexture(motionVectorBuffer);

                // Effect buffers
                passData.velocityBuffer = builder.ReadTexture(velocityBuffer);
                passData.noisyBuffer = builder.ReadTexture(noisyBuffer);
                passData.validationBuffer = builder.ReadTexture(historyValidationBuffer);

                // History buffer
                passData.historyBuffer = builder.ReadWriteTexture(historyBuffer);

                // Output buffers
                passData.outputBuffer = builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Temporal Filter Output" }));

                builder.SetRenderFunc(
                    (TemporalFilterPassData data, RenderGraphContext ctx) =>
                    {
                        // Evaluate the dispatch parameters
                        int areaTileSize = 8;
                        int numTilesX = (data.texWidth + (areaTileSize - 1)) / areaTileSize;
                        int numTilesY = (data.texHeight + (areaTileSize - 1)) / areaTileSize;

                        // Now that we have validated our history, let's accumulate
                        // Bind the input buffers
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccKernel, HDShaderIDs._DenoiseInputTexture, data.noisyBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccKernel, HDShaderIDs._HistoryBuffer, data.historyBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccKernel, HDShaderIDs._ValidationBuffer, data.validationBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccKernel, HDShaderIDs._VelocityBuffer, data.velocityBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccKernel, HDShaderIDs._CameraMotionVectorsTexture, data.motionVectorBuffer);
                        ctx.cmd.SetComputeFloatParam(data.temporalFilterCS, HDShaderIDs._HistoryValidity, data.historyValidity);

                        // Bind the output buffer
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccKernel, HDShaderIDs._DenoiseOutputTextureRW, data.outputBuffer);

                        // Combine signal with history
                        ctx.cmd.DispatchCompute(data.temporalFilterCS, data.temporalAccKernel, numTilesX, numTilesY, data.viewCount);

                        // Make sure to copy the new-accumulated signal in our history buffer
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.copyHistoryKernel, HDShaderIDs._DenoiseInputTexture, data.outputBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.copyHistoryKernel, HDShaderIDs._DenoiseOutputTextureRW, data.historyBuffer);
                        ctx.cmd.DispatchCompute(data.temporalFilterCS, data.copyHistoryKernel, numTilesX, numTilesY, data.viewCount);
                    });
                return passData.outputBuffer;
            }
        }

        internal struct TemporalDenoiserArrayOutputData
        {
            public TextureHandle outputSignal;
            public TextureHandle outputSignalDistance;
        }

        class TemporalFilterArrayPassData
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

            // Prepass buffers
            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle motionVectorBuffer;

            // Effect Buffers
            public TextureHandle noisyBuffer;
            public TextureHandle distanceBuffer;
            public TextureHandle validationBuffer;
            public TextureHandle velocityBuffer;

            // History buffers
            public TextureHandle historyBuffer;
            public TextureHandle validationHistoryBuffer;
            public TextureHandle distanceHistorySignal;

            // Output buffers
            public TextureHandle outputBuffer;
            public TextureHandle outputDistanceSignal;
        }

        public TemporalDenoiserArrayOutputData DenoiseBuffer(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectorBuffer, TextureHandle historyValidationBuffer,
            TextureHandle noisyBuffer, RTHandle historyBuffer,
            TextureHandle distanceBuffer, RTHandle distanceHistorySignal,
            TextureHandle velocityBuffer,
            RTHandle validationHistoryBuffer,
            int sliceIndex, Vector4 channelMask, Vector4 distanceChannelMask,
            bool distanceBased, bool singleChannel, float historyValidity)
        {
            TemporalDenoiserArrayOutputData resultData = new TemporalDenoiserArrayOutputData();
            using (var builder = renderGraph.AddRenderPass<TemporalFilterArrayPassData>("TemporalDenoiser", out var passData, ProfilingSampler.Get(HDProfileId.TemporalFilter)))
            {
                // Cannot run in async
                builder.EnableAsyncCompute(false);

                // Set the camera parameters
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Denoising parameters
                passData.distanceBasedDenoiser = distanceBased;
                passData.historyValidity = historyValidity;
                passData.pixelSpreadTangent = HDRenderPipeline.GetPixelSpreadTangent(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight);
                passData.sliceIndex = sliceIndex;
                passData.channelMask = channelMask;
                passData.distanceChannelMask = distanceChannelMask;

                // Kernels
                passData.temporalAccKernel = singleChannel ? m_TemporalAccumulationSingleArrayKernel : m_TemporalAccumulationColorArrayKernel;
                passData.copyHistoryKernel = singleChannel ? m_CopyHistorySingleArrayKernel : m_CopyHistoryColorArrayKernel;
                passData.temporalAccSingleKernel = m_TemporalAccumulationSingleArrayKernel;
                passData.copyHistoryNoValidityKernel = m_CopyHistorySingleArrayNoValidityKernel;

                // Other parameters
                passData.temporalFilterCS = m_TemporalFilterCS;

                // Input buffers
                passData.depthStencilBuffer = builder.ReadTexture(depthBuffer);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.motionVectorBuffer = builder.ReadTexture(motionVectorBuffer);

                passData.velocityBuffer = builder.ReadTexture(velocityBuffer);
                passData.noisyBuffer = builder.ReadTexture(noisyBuffer);
                passData.distanceBuffer = distanceBased ? builder.ReadTexture(distanceBuffer) : renderGraph.defaultResources.blackTextureXR;
                passData.validationBuffer = builder.ReadTexture(historyValidationBuffer);

                // History buffers
                passData.historyBuffer = builder.ReadWriteTexture(renderGraph.ImportTexture(historyBuffer));
                passData.validationHistoryBuffer = builder.ReadWriteTexture(renderGraph.ImportTexture(validationHistoryBuffer));
                passData.distanceHistorySignal = distanceBased ? builder.ReadWriteTexture(renderGraph.ImportTexture(distanceHistorySignal)) : renderGraph.defaultResources.blackTextureXR;

                // Output textures
                passData.outputBuffer = builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Temporal Filter Output" }));
                passData.outputDistanceSignal = distanceBased ? builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Temporal Filter Distance output" })) : new TextureHandle();

                builder.SetRenderFunc(
                    (TemporalFilterArrayPassData data, RenderGraphContext ctx) =>
                    {
                        // Evaluate the dispatch parameters
                        int tfTileSize = 8;
                        int numTilesX = (data.texWidth + (tfTileSize - 1)) / tfTileSize;
                        int numTilesY = (data.texHeight + (tfTileSize - 1)) / tfTileSize;

                        // Now that we have validated our history, let's accumulate
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccKernel, HDShaderIDs._DenoiseInputTexture, data.noisyBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccKernel, HDShaderIDs._HistoryBuffer, data.historyBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccKernel, HDShaderIDs._HistoryValidityBuffer, data.validationHistoryBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccKernel, HDShaderIDs._CameraMotionVectorsTexture, data.motionVectorBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccKernel, HDShaderIDs._ValidationBuffer, data.validationBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccKernel, HDShaderIDs._VelocityBuffer, data.velocityBuffer);

                        // Bind the constants
                        ctx.cmd.SetComputeIntParam(data.temporalFilterCS, HDShaderIDs._DenoisingHistorySlice, data.sliceIndex);
                        ctx.cmd.SetComputeVectorParam(data.temporalFilterCS, HDShaderIDs._DenoisingHistoryMask, data.channelMask);
                        ctx.cmd.SetComputeFloatParam(data.temporalFilterCS, HDShaderIDs._HistoryValidity, data.historyValidity);

                        // Bind the output buffer
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccKernel, HDShaderIDs._DenoiseOutputTextureRW, data.outputBuffer);

                        // Combine with the history
                        ctx.cmd.DispatchCompute(data.temporalFilterCS, data.temporalAccKernel, numTilesX, numTilesY, data.viewCount);

                        // Make sure to copy the new-accumulated signal in our history buffer
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.copyHistoryKernel, HDShaderIDs._DenoiseInputTexture, data.outputBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.copyHistoryKernel, HDShaderIDs._DenoiseOutputTextureRW, data.historyBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.copyHistoryKernel, HDShaderIDs._ValidityOutputTextureRW, data.validationHistoryBuffer);
                        ctx.cmd.SetComputeIntParam(data.temporalFilterCS, HDShaderIDs._DenoisingHistorySlice, data.sliceIndex);
                        ctx.cmd.SetComputeVectorParam(data.temporalFilterCS, HDShaderIDs._DenoisingHistoryMask, data.channelMask);
                        ctx.cmd.DispatchCompute(data.temporalFilterCS, data.copyHistoryKernel, numTilesX, numTilesY, data.viewCount);

                        if (data.distanceBasedDenoiser)
                        {
                            // Bind the input buffers
                            ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccSingleKernel, HDShaderIDs._DenoiseInputTexture, data.distanceBuffer);
                            ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccSingleKernel, HDShaderIDs._HistoryBuffer, data.distanceHistorySignal);
                            ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccSingleKernel, HDShaderIDs._HistoryValidityBuffer, data.validationHistoryBuffer);
                            ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccSingleKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                            ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccSingleKernel, HDShaderIDs._ValidationBuffer, data.validationBuffer);
                            ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccSingleKernel, HDShaderIDs._VelocityBuffer, data.velocityBuffer);
                            ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccSingleKernel, HDShaderIDs._CameraMotionVectorsTexture, data.motionVectorBuffer);

                            // Bind the constant inputs
                            ctx.cmd.SetComputeIntParam(data.temporalFilterCS, HDShaderIDs._DenoisingHistorySlice, data.sliceIndex);
                            ctx.cmd.SetComputeVectorParam(data.temporalFilterCS, HDShaderIDs._DenoisingHistoryMask, data.distanceChannelMask);

                            // Bind the output buffers
                            ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccSingleKernel, HDShaderIDs._DenoiseOutputTextureRW, data.outputDistanceSignal);

                            // Dispatch the temporal accumulation
                            ctx.cmd.DispatchCompute(data.temporalFilterCS, data.temporalAccSingleKernel, numTilesX, numTilesY, data.viewCount);

                            // Make sure to copy the new-accumulated signal in our history buffer
                            ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.copyHistoryNoValidityKernel, HDShaderIDs._DenoiseInputTexture, data.outputDistanceSignal);
                            ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.copyHistoryNoValidityKernel, HDShaderIDs._DenoiseOutputTextureRW, data.distanceHistorySignal);
                            ctx.cmd.SetComputeIntParam(data.temporalFilterCS, HDShaderIDs._DenoisingHistorySlice, data.sliceIndex);
                            ctx.cmd.SetComputeVectorParam(data.temporalFilterCS, HDShaderIDs._DenoisingHistoryMask, data.distanceChannelMask);
                            ctx.cmd.DispatchCompute(data.temporalFilterCS, data.copyHistoryNoValidityKernel, numTilesX, numTilesY, data.viewCount);
                        }
                    });

                resultData.outputSignal = passData.outputBuffer;
                resultData.outputSignalDistance = passData.outputDistanceSignal;
            }
            return resultData;
        }
    }
}
