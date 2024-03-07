using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    class HDTemporalFilter
    {
        [GenerateHLSL]
        enum HistoryRejectionFlags
        {
            Depth = 0x1,
            Reprojection = 0x2,
            PreviousDepth = 0x4,
            Position = 0x8,
            Normal = 0x10,
            Motion = 0x20,
            Combined = Depth | Reprojection | PreviousDepth | Position | Normal | Motion,
            CombinedNoMotion = Depth | Reprojection | PreviousDepth | Position | Normal
        }

        // Resources used for the denoiser
        ComputeShader m_TemporalFilterCS;

        // The set of required kernels
        int m_ValidateHistoryKernel;

        int m_TemporalAccumulationSingleKernel;
        int m_TemporalAccumulationColorKernel;
        int m_CopyHistoryKernel;

        int m_TemporalAccumulationSingleArrayKernel;
        int m_TemporalAccumulationColorArrayKernel;
        int m_BlendHistorySingleArrayKernel;
        int m_BlendHistoryColorArrayKernel;
        int m_BlendHistorySingleArrayNoValidityKernel;
        int m_OutputHistoryArrayKernel;

        public HDTemporalFilter()
        {
        }

        public void Init(HDRenderPipelineRuntimeResources rpResources)
        {
            // Keep track of the resources
            m_TemporalFilterCS = rpResources.shaders.temporalFilterCS;

            m_ValidateHistoryKernel = m_TemporalFilterCS.FindKernel("ValidateHistory");

            m_TemporalAccumulationSingleKernel = m_TemporalFilterCS.FindKernel("TemporalAccumulationSingle");
            m_TemporalAccumulationColorKernel = m_TemporalFilterCS.FindKernel("TemporalAccumulationColor");
            m_CopyHistoryKernel = m_TemporalFilterCS.FindKernel("CopyHistory");

            m_TemporalAccumulationSingleArrayKernel = m_TemporalFilterCS.FindKernel("TemporalAccumulationSingleArray");
            m_TemporalAccumulationColorArrayKernel = m_TemporalFilterCS.FindKernel("TemporalAccumulationColorArray");
            m_BlendHistorySingleArrayKernel = m_TemporalFilterCS.FindKernel("BlendHistorySingleArray");
            m_BlendHistoryColorArrayKernel = m_TemporalFilterCS.FindKernel("BlendHistoryColorArray");
            m_BlendHistorySingleArrayNoValidityKernel = m_TemporalFilterCS.FindKernel("BlendHistorySingleArrayNoValidity");
            m_OutputHistoryArrayKernel = m_TemporalFilterCS.FindKernel("OutputHistoryArray");
        }

        public void Release()
        {
        }

        internal struct TemporalFilterParameters
        {
            public bool singleChannel;
            public float historyValidity;
            public bool occluderMotionRejection;
            public bool receiverMotionRejection;
            public bool exposureControl;
            public float resolutionMultiplier;
            public float historyResolutionMultiplier;
        }

        class HistoryValidityPassData
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;
            public Vector4 historySizeAndScale;

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
                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.MotionVectors))
                    passData.motionVectorBuffer = builder.ReadTexture(motionVectorBuffer);
                else
                    passData.motionVectorBuffer = builder.ReadTexture(renderGraph.defaultResources.blackTextureXR);

                // Grab and import the history buffers
                var historyDepth = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
                var historyNormal = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Normal);
                passData.historyDepthTexture = builder.ReadTexture(renderGraph.ImportTexture(historyDepth));
                passData.historyNormalTexture = builder.ReadTexture(renderGraph.ImportTexture(historyNormal));
                passData.historySizeAndScale = (historyDepth != null && historyNormal != null) ? HDRenderPipeline.EvaluateRayTracingHistorySizeAndScale(hdCamera, historyDepth) : Vector4.one;

                // Output buffers
                passData.validationBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R8_UInt, enableRandomWrite = true, name = "ValidationTexture" }));

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
                        ctx.cmd.SetComputeVectorParam(data.temporalFilterCS, HDShaderIDs._HistorySizeAndScale, data.historySizeAndScale);

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
            public bool occluderMotionRejection;
            public bool receiverMotionRejection;
            public int exposureControl;
            public float resolutionMultiplier;
            public float historyResolutionMultiplier;

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
        internal TextureHandle Denoise(RenderGraph renderGraph, HDCamera hdCamera, TemporalFilterParameters filterParams,
            TextureHandle noisyBuffer, TextureHandle velocityBuffer,
            TextureHandle historyBuffer,
            TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectorBuffer, TextureHandle historyValidationBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<TemporalFilterPassData>("TemporalDenoiser", out var passData, ProfilingSampler.Get(HDProfileId.TemporalFilter)))
            {
                // Cannot run in async
                builder.EnableAsyncCompute(false);

                // Camera parameters
                passData.texWidth = (int)Mathf.Floor((float)hdCamera.actualWidth * filterParams.resolutionMultiplier);
                passData.texHeight = (int)Mathf.Floor((float)hdCamera.actualHeight * filterParams.resolutionMultiplier);
                passData.viewCount = hdCamera.viewCount;

                // Denoising parameters
                passData.pixelSpreadTangent = HDRenderPipeline.GetPixelSpreadTangent(hdCamera.camera.fieldOfView, passData.texWidth, passData.texHeight);
                passData.historyValidity = filterParams.historyValidity;
                passData.receiverMotionRejection = filterParams.receiverMotionRejection;
                passData.occluderMotionRejection = filterParams.occluderMotionRejection;
                passData.exposureControl = filterParams.exposureControl ? 1 : 0;
                passData.resolutionMultiplier = filterParams.resolutionMultiplier;
                passData.historyResolutionMultiplier = filterParams.historyResolutionMultiplier;

                // Kernels
                passData.temporalAccKernel = filterParams.singleChannel ? m_TemporalAccumulationSingleKernel : m_TemporalAccumulationColorKernel;
                passData.copyHistoryKernel = m_CopyHistoryKernel;

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
                        ctx.cmd.SetComputeIntParam(data.temporalFilterCS, HDShaderIDs._ReceiverMotionRejection, data.receiverMotionRejection ? 1 : 0);
                        ctx.cmd.SetComputeIntParam(data.temporalFilterCS, HDShaderIDs._OccluderMotionRejection, data.occluderMotionRejection ? 1 : 0);
                        ctx.cmd.SetComputeFloatParam(data.temporalFilterCS, HDShaderIDs._PixelSpreadAngleTangent, data.pixelSpreadTangent);
                        ctx.cmd.SetComputeVectorParam(data.temporalFilterCS, HDShaderIDs._DenoiserResolutionMultiplierVals, new Vector4(data.resolutionMultiplier, 1.0f / data.resolutionMultiplier, data.historyResolutionMultiplier, 1.0f / data.historyResolutionMultiplier));
                        ctx.cmd.SetComputeIntParam(data.temporalFilterCS, HDShaderIDs._EnableExposureControl, data.exposureControl);

                        // Bind the output buffer
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccKernel, HDShaderIDs._AccumulationOutputTextureRW, data.outputBuffer);

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
            public int blendHistoryKernel;
            public int temporalAccSingleKernel;
            public int blendHistoryNoValidityKernel;
            public int outputHistoryKernel;

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
            public TextureHandle inputHistoryBuffer;
            public TextureHandle outputHistoryBuffer;
            public TextureHandle validationHistoryBuffer;
            public TextureHandle distanceHistorySignal;

            // Intermediate buffers
            public TextureHandle intermediateSignalOutput;
            public TextureHandle intermediateValidityOutput;

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
                passData.blendHistoryKernel = singleChannel ? m_BlendHistorySingleArrayKernel : m_BlendHistoryColorArrayKernel;
                passData.temporalAccSingleKernel = m_TemporalAccumulationSingleArrayKernel;
                passData.blendHistoryNoValidityKernel = m_BlendHistorySingleArrayNoValidityKernel;
                passData.outputHistoryKernel = m_OutputHistoryArrayKernel;

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
                passData.outputHistoryBuffer = builder.ReadWriteTexture(renderGraph.ImportTexture(historyBuffer));
                passData.inputHistoryBuffer = passData.outputHistoryBuffer;
                passData.validationHistoryBuffer = builder.ReadWriteTexture(renderGraph.ImportTexture(validationHistoryBuffer));
                passData.distanceHistorySignal = distanceBased ? builder.ReadWriteTexture(renderGraph.ImportTexture(distanceHistorySignal)) : renderGraph.defaultResources.blackTextureXR;

                // Intermediate buffers
                passData.intermediateSignalOutput = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Intermediate Filter Output" });
                passData.intermediateValidityOutput = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Intermediate Validity output" });

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
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccKernel, HDShaderIDs._HistoryBuffer, data.inputHistoryBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccKernel, HDShaderIDs._HistoryValidityBuffer, data.validationHistoryBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccKernel, HDShaderIDs._CameraMotionVectorsTexture, data.motionVectorBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccKernel, HDShaderIDs._ValidationBuffer, data.validationBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccKernel, HDShaderIDs._VelocityBuffer, data.velocityBuffer);

                        // Bind the constants
                        ctx.cmd.SetComputeIntParam(data.temporalFilterCS, HDShaderIDs._DenoisingHistorySlice, data.sliceIndex);
                        ctx.cmd.SetComputeVectorParam(data.temporalFilterCS, HDShaderIDs._DenoisingHistoryMask, data.channelMask);
                        ctx.cmd.SetComputeFloatParam(data.temporalFilterCS, HDShaderIDs._HistoryValidity, data.historyValidity);
                        ctx.cmd.SetComputeIntParam(data.temporalFilterCS, HDShaderIDs._ReceiverMotionRejection, 1);
                        ctx.cmd.SetComputeIntParam(data.temporalFilterCS, HDShaderIDs._OccluderMotionRejection, 1);
                        ctx.cmd.SetComputeVectorParam(data.temporalFilterCS, HDShaderIDs._DenoiserResolutionMultiplierVals, Vector4.one);

                        // Bind the output buffer
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccKernel, HDShaderIDs._AccumulationOutputTextureRW, data.outputBuffer);

                        // Combine with the history
                        ctx.cmd.DispatchCompute(data.temporalFilterCS, data.temporalAccKernel, numTilesX, numTilesY, data.viewCount);

                        // Combine with the history buffer
                        ctx.cmd.SetComputeIntParam(data.temporalFilterCS, HDShaderIDs._DenoisingHistorySlice, data.sliceIndex);
                        ctx.cmd.SetComputeVectorParam(data.temporalFilterCS, HDShaderIDs._DenoisingHistoryMask, data.channelMask);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.blendHistoryKernel, HDShaderIDs._DenoiseInputTexture, data.outputBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.blendHistoryKernel, HDShaderIDs._DenoiseInputArrayTexture, data.inputHistoryBuffer );
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.blendHistoryKernel, HDShaderIDs._ValidityInputArrayTexture, data.validationHistoryBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.blendHistoryKernel, HDShaderIDs._IntermediateDenoiseOutputTextureRW, data.intermediateSignalOutput);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.blendHistoryKernel, HDShaderIDs._IntermediateValidityOutputTextureRW, data.intermediateValidityOutput);
                        ctx.cmd.DispatchCompute(data.temporalFilterCS, data.blendHistoryKernel, numTilesX, numTilesY, data.viewCount);

                        // Output the combination to the history buffer
                        ctx.cmd.SetComputeIntParam(data.temporalFilterCS, HDShaderIDs._DenoisingHistorySlice, data.sliceIndex);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.outputHistoryKernel, HDShaderIDs._IntermediateDenoiseOutputTexture, data.intermediateSignalOutput);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.outputHistoryKernel, HDShaderIDs._IntermediateValidityOutputTexture, data.intermediateValidityOutput);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.outputHistoryKernel, HDShaderIDs._DenoiseOutputArrayTextureRW, data.outputHistoryBuffer);
                        ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.outputHistoryKernel, HDShaderIDs._ValidityOutputTextureRW, data.validationHistoryBuffer);
                        ctx.cmd.DispatchCompute(data.temporalFilterCS, data.outputHistoryKernel, numTilesX, numTilesY, data.viewCount);

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
                            ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.temporalAccSingleKernel, HDShaderIDs._AccumulationOutputTextureRW, data.outputDistanceSignal);

                            // Dispatch the temporal accumulation
                            ctx.cmd.DispatchCompute(data.temporalFilterCS, data.temporalAccSingleKernel, numTilesX, numTilesY, data.viewCount);

                            // Make sure to copy the new-accumulated signal in our history buffer
                            ctx.cmd.SetComputeIntParam(data.temporalFilterCS, HDShaderIDs._DenoisingHistorySlice, data.sliceIndex);
                            ctx.cmd.SetComputeVectorParam(data.temporalFilterCS, HDShaderIDs._DenoisingHistoryMask, data.distanceChannelMask);
                            ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.blendHistoryNoValidityKernel, HDShaderIDs._DenoiseInputTexture, data.outputDistanceSignal);
                            ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.blendHistoryNoValidityKernel, HDShaderIDs._DenoiseInputArrayTexture, data.distanceHistorySignal);
                            ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.blendHistoryNoValidityKernel, HDShaderIDs._IntermediateDenoiseOutputTextureRW, data.intermediateSignalOutput);
                            ctx.cmd.DispatchCompute(data.temporalFilterCS, data.blendHistoryNoValidityKernel, numTilesX, numTilesY, data.viewCount);

                            // Output the combination to the history buffer
                            ctx.cmd.SetComputeIntParam(data.temporalFilterCS, HDShaderIDs._DenoisingHistorySlice, data.sliceIndex);
                            ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.outputHistoryKernel, HDShaderIDs._IntermediateDenoiseOutputTexture, data.intermediateSignalOutput);
                            ctx.cmd.SetComputeTextureParam(data.temporalFilterCS, data.outputHistoryKernel, HDShaderIDs._DenoiseOutputArrayTextureRW, data.distanceHistorySignal);
                            ctx.cmd.DispatchCompute(data.temporalFilterCS, data.outputHistoryKernel, numTilesX, numTilesY, data.viewCount);
                        }
                    });

                resultData.outputSignal = passData.outputBuffer;
                resultData.outputSignalDistance = passData.outputDistanceSignal;
            }
            return resultData;
        }
    }
}
