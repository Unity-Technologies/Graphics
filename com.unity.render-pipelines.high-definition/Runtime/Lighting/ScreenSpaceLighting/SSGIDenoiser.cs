using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    class SSGIDenoiser
    {
        // Resources used for the denoiser
        ComputeShader m_SSGIDenoiserCS;

        int m_SpatialFilterHalfKernel;
        int m_SpatialFilterKernel;
        int m_TemporalFilterHalfKernel;
        int m_TemporalFilterKernel;
        int m_CopyHistory;

        public SSGIDenoiser()
        {
        }

        public void Init(HDRenderPipelineRuntimeResources rpResources)
        {
            // Keep track of the resources
            m_SSGIDenoiserCS = rpResources.shaders.ssGIDenoiserCS;

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

        RTHandle IndirectDiffuseHistoryBufferAllocatorFunction0(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, dimension: TextureXR.dimension,
                enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                name: string.Format("IndirectDiffuseHistoryBuffer0{0}", frameIndex));
        }

        RTHandle IndirectDiffuseHistoryBufferAllocatorFunction1(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16_SFloat, dimension: TextureXR.dimension,
                enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                name: string.Format("IndirectDiffuseHistoryBuffer1{0}", frameIndex));
        }

        static void SpatialFilter(CommandBuffer cmd, DenoiseSSGIPassData data, int filterRadius, Vector2 filterDirection,
            RTHandle input0, RTHandle input1, RTHandle output0, RTHandle output1)
        {
            // Bind the input scalars
            cmd.SetComputeVectorParam(data.ssgiDenoiserCS, HDShaderIDs._DepthPyramidFirstMipLevelOffset, data.firstMipOffset);
            cmd.SetComputeIntParam(data.ssgiDenoiserCS, HDShaderIDs._IndirectDiffuseSpatialFilter, filterRadius);
            cmd.SetComputeFloatParam(data.ssgiDenoiserCS, HDShaderIDs._PixelSpreadAngleTangent, data.pixelSpreadTangent);
            cmd.SetComputeVectorParam(data.ssgiDenoiserCS, HDShaderIDs._SpatialFilterDirection, filterDirection);

            // Inject half screen size if required
            if (data.halfResolution)
                cmd.SetComputeVectorParam(data.ssgiDenoiserCS, HDShaderIDs._HalfScreenSize, data.halfScreenSize);

            // Bind the input buffers
            cmd.SetComputeTextureParam(data.ssgiDenoiserCS, data.spatialFilterKernel, HDShaderIDs._DepthTexture, data.depthTexture);
            cmd.SetComputeTextureParam(data.ssgiDenoiserCS, data.spatialFilterKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
            cmd.SetComputeTextureParam(data.ssgiDenoiserCS, data.spatialFilterKernel, HDShaderIDs._InputNoisyBuffer0, input0);
            cmd.SetComputeTextureParam(data.ssgiDenoiserCS, data.spatialFilterKernel, HDShaderIDs._InputNoisyBuffer1, input1);

            // Bind the output buffer
            cmd.SetComputeTextureParam(data.ssgiDenoiserCS, data.spatialFilterKernel, HDShaderIDs._OutputFilteredBuffer0, output0);
            cmd.SetComputeTextureParam(data.ssgiDenoiserCS, data.spatialFilterKernel, HDShaderIDs._OutputFilteredBuffer1, output1);

            // Do the spatial pass
            cmd.DispatchCompute(data.ssgiDenoiserCS, data.spatialFilterKernel, data.numTilesX, data.numTilesY, data.viewCount);
        }

        RTHandle RequestIndirectDiffuseHistory0(HDCamera hdCamera, out bool historyRequireClear)
        {
            historyRequireClear = false;
            RTHandle indirectDiffuseHistory = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF);
            if (indirectDiffuseHistory == null)
            {
                indirectDiffuseHistory = hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF, IndirectDiffuseHistoryBufferAllocatorFunction0, 1);
                historyRequireClear = true;
            }
            return indirectDiffuseHistory;
        }

        RTHandle RequestIndirectDiffuseHistory1(HDCamera hdCamera, out bool historyRequireClear)
        {
            historyRequireClear = false;
            RTHandle indirectDiffuseHistory = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseLF);
            if (indirectDiffuseHistory == null)
            {
                indirectDiffuseHistory = hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseLF, IndirectDiffuseHistoryBufferAllocatorFunction1, 1);
                historyRequireClear = true;
            }
            return indirectDiffuseHistory;
        }

        class DenoiseSSGIPassData
        {
            // Camera Parameters
            public int numTilesX;
            public int numTilesY;
            public int viewCount;
            public Vector4 halfScreenSize;
            public Vector2 firstMipOffset;

            // Denoising parameters
            public int filterRadius;
            public bool halfResolution;
            public float historyValidity;
            public bool historyNeedsClear;
            public float pixelSpreadTangent;
            public bool exclusiveMode;

            // Shader
            public ComputeShader ssgiDenoiserCS;

            // Kernels
            public int spatialFilterKernel;
            public int temporalFilterKernel;
            public int copyHistory;

            // Prepass data
            public TextureHandle depthTexture;
            public TextureHandle normalBuffer;
            public TextureHandle motionVectorsBuffer;

            // History buffer
            public TextureHandle indirectDiffuseHistory0;
            public TextureHandle indirectDiffuseHistory1;
            public TextureHandle historyDepthBuffer;

            // input buffers
            public TextureHandle inputBuffer0;
            public TextureHandle inputBuffer1;

            // Output buffers
            public TextureHandle outputBuffer0;
            public TextureHandle outputBuffer1;
        }

        public struct SSGIDenoiserOutput
        {
            public TextureHandle outputBuffer0;
            public TextureHandle outputBuffer1;
        }

        public SSGIDenoiserOutput Denoise(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle motionVectorsBuffer, TextureHandle inputBuffer0, TextureHandle inputBuffer1,
            HDUtils.PackedMipChainInfo depthMipInfo, bool halfResolution = false, float historyValidity = 1.0f)
        {
            using (var builder = renderGraph.AddRenderPass<DenoiseSSGIPassData>("Denoise SSGI", out var passData, ProfilingSampler.Get(HDProfileId.SSGIDenoise)))
            {
                builder.EnableAsyncCompute(false);

                // Prepass buffers
                passData.depthTexture = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.motionVectorsBuffer = builder.ReadTexture(motionVectorsBuffer);

                // History buffers
                bool historyRequireClear = false;
                RTHandle indirectDiffuseHistory0 = RequestIndirectDiffuseHistory0(hdCamera, out historyRequireClear);
                passData.indirectDiffuseHistory0 = builder.ReadWriteTexture(renderGraph.ImportTexture(indirectDiffuseHistory0));
                RTHandle indirectDiffuseHistory1 = RequestIndirectDiffuseHistory1(hdCamera, out historyRequireClear);
                passData.indirectDiffuseHistory1 = builder.ReadWriteTexture(renderGraph.ImportTexture(indirectDiffuseHistory1));
                var historyDepthBuffer = halfResolution ? hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth1) : hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
                passData.historyDepthBuffer = historyDepthBuffer != null ? builder.ReadTexture(renderGraph.ImportTexture(historyDepthBuffer)) : renderGraph.defaultResources.blackTextureXR;

                // Input buffers
                passData.inputBuffer0 = builder.ReadTexture(inputBuffer0);
                passData.inputBuffer1 = builder.ReadTexture(inputBuffer1);

                // Output buffers
                passData.outputBuffer0 = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true, name = "SSGI Denoised 0" }));
                passData.outputBuffer1 = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16_SFloat, enableRandomWrite = true, name = "SSGI Denoised 1" }));

                // Parameters
                var giSettings = hdCamera.volumeStack.GetComponent<UnityEngine.Rendering.HighDefinition.GlobalIllumination>();

                // Compute the dispatch parameters based on if we are half res or not
                int tileSize = 8;
                EvaluateDispatchParameters(hdCamera, halfResolution, tileSize, out passData.numTilesX, out passData.numTilesY, out passData.halfScreenSize);
                passData.firstMipOffset.Set(HDShadowUtils.Asfloat((uint)depthMipInfo.mipLevelOffsets[1].x), HDShadowUtils.Asfloat((uint)depthMipInfo.mipLevelOffsets[1].y));
                passData.historyValidity = historyValidity;
                passData.viewCount = hdCamera.viewCount;
                passData.pixelSpreadTangent = HDRenderPipeline.GetPixelSpreadTangent(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight);

                // Denoising parameters
                passData.filterRadius = giSettings.filterRadius;
                passData.halfResolution = halfResolution;
                passData.historyValidity = historyValidity;
                passData.historyNeedsClear = historyRequireClear;
                passData.exclusiveMode = !hdCamera.frameSettings.IsEnabled(FrameSettingsField.ProbeVolume);

                // Compute shader
                passData.ssgiDenoiserCS = m_SSGIDenoiserCS;

                // Kernels
                passData.spatialFilterKernel = halfResolution ? m_SpatialFilterHalfKernel : m_SpatialFilterKernel;
                passData.temporalFilterKernel = halfResolution ? m_TemporalFilterHalfKernel : m_TemporalFilterKernel;
                passData.copyHistory = m_CopyHistory;

                builder.SetRenderFunc(
                    (DenoiseSSGIPassData data, RenderGraphContext ctx) =>
                    {
                        int effectiveRadius = data.exclusiveMode ? data.filterRadius / 2 : data.filterRadius;
                        if (data.exclusiveMode)
                        {
                            // Horizontal Filter
                            SpatialFilter(ctx.cmd, data, effectiveRadius, new Vector2(1.0f, 0.0f), data.inputBuffer0, data.inputBuffer1, data.outputBuffer0, data.outputBuffer1);
                            // Vertical Filter
                            SpatialFilter(ctx.cmd, data, effectiveRadius, new Vector2(0.0f, 1.0f), data.outputBuffer0, data.outputBuffer1, data.inputBuffer0, data.inputBuffer1);
                        }

                        // Grab the history buffer
                        if (data.historyNeedsClear)
                        {
                            // clear it to black if this is the first pass to avoid nans
                            CoreUtils.SetRenderTarget(ctx.cmd, data.indirectDiffuseHistory0, ClearFlag.Color, Color.black);
                            CoreUtils.SetRenderTarget(ctx.cmd, data.indirectDiffuseHistory1, ClearFlag.Color, Color.black);
                        }

                        // Bind the input buffers
                        ctx.cmd.SetComputeTextureParam(data.ssgiDenoiserCS, data.temporalFilterKernel, HDShaderIDs._DepthTexture, data.depthTexture);
                        ctx.cmd.SetComputeTextureParam(data.ssgiDenoiserCS, data.temporalFilterKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.ssgiDenoiserCS, data.temporalFilterKernel, HDShaderIDs._CameraMotionVectorsTexture, data.motionVectorsBuffer);
                        ctx.cmd.SetComputeFloatParam(data.ssgiDenoiserCS, HDShaderIDs._HistoryValidity, data.historyValidity);
                        if (data.halfResolution)
                        {
                            ctx.cmd.SetComputeTextureParam(data.ssgiDenoiserCS, data.temporalFilterKernel, HDShaderIDs._HistoryDepthTexture, data.historyDepthBuffer);
                            ctx.cmd.SetComputeVectorParam(data.ssgiDenoiserCS, HDShaderIDs._DepthPyramidFirstMipLevelOffset, data.firstMipOffset);
                        }
                        else
                        {
                            ctx.cmd.SetComputeTextureParam(data.ssgiDenoiserCS, data.temporalFilterKernel, HDShaderIDs._HistoryDepthTexture, data.historyDepthBuffer);
                        }
                        ctx.cmd.SetComputeTextureParam(data.ssgiDenoiserCS, data.temporalFilterKernel, HDShaderIDs._HistoryBuffer0, data.indirectDiffuseHistory0);
                        ctx.cmd.SetComputeTextureParam(data.ssgiDenoiserCS, data.temporalFilterKernel, HDShaderIDs._HistoryBuffer1, data.indirectDiffuseHistory1);
                        ctx.cmd.SetComputeTextureParam(data.ssgiDenoiserCS, data.temporalFilterKernel, HDShaderIDs._InputNoisyBuffer0, data.inputBuffer0);
                        ctx.cmd.SetComputeTextureParam(data.ssgiDenoiserCS, data.temporalFilterKernel, HDShaderIDs._InputNoisyBuffer1, data.inputBuffer1);

                        // Bind the output buffer
                        ctx.cmd.SetComputeTextureParam(data.ssgiDenoiserCS, data.temporalFilterKernel, HDShaderIDs._OutputFilteredBuffer0, data.outputBuffer0);
                        ctx.cmd.SetComputeTextureParam(data.ssgiDenoiserCS, data.temporalFilterKernel, HDShaderIDs._OutputFilteredBuffer1, data.outputBuffer1);

                        // Do the temporal pass
                        ctx.cmd.DispatchCompute(data.ssgiDenoiserCS, data.temporalFilterKernel, data.numTilesX, data.numTilesY, data.viewCount);

                        // Copy the new version into the history buffer
                        ctx.cmd.SetComputeTextureParam(data.ssgiDenoiserCS, data.copyHistory, HDShaderIDs._InputNoisyBuffer0, data.outputBuffer0);
                        ctx.cmd.SetComputeTextureParam(data.ssgiDenoiserCS, data.copyHistory, HDShaderIDs._InputNoisyBuffer1, data.outputBuffer1);
                        ctx.cmd.SetComputeTextureParam(data.ssgiDenoiserCS, data.copyHistory, HDShaderIDs._OutputFilteredBuffer0, data.indirectDiffuseHistory0);
                        ctx.cmd.SetComputeTextureParam(data.ssgiDenoiserCS, data.copyHistory, HDShaderIDs._OutputFilteredBuffer1, data.indirectDiffuseHistory1);
                        ctx.cmd.DispatchCompute(data.ssgiDenoiserCS, data.copyHistory, data.numTilesX, data.numTilesY, data.viewCount);

                        // Horizontal Filter
                        SpatialFilter(ctx.cmd, data, effectiveRadius, new Vector2(1.0f, 0.0f), data.outputBuffer0, data.outputBuffer1, data.inputBuffer0, data.inputBuffer1);
                        // Vertical Filter
                        SpatialFilter(ctx.cmd, data, effectiveRadius, new Vector2(0.0f, 1.0f), data.inputBuffer0, data.inputBuffer1, data.outputBuffer0, data.outputBuffer1);
                    });

                SSGIDenoiserOutput denoiserOutput = new SSGIDenoiserOutput();
                denoiserOutput.outputBuffer0 = passData.outputBuffer0;
                denoiserOutput.outputBuffer1 = passData.outputBuffer1;
                return denoiserOutput;
            }
        }
    }
}
