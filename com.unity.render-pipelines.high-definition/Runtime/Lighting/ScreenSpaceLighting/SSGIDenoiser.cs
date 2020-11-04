using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

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
        static Color s_CoCgAccClearColor = new Color(0.501960784f, 0.501960784f, 0.0f, 0.0f);

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

        struct SSGIDenoiserParameters
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

            // Shader
            public ComputeShader ssgiDenoiserCS;

            // Kernels
            public int spatialFilterKernel;
            public int temporalFilterKernel;
            public int copyHistory;
        }

        SSGIDenoiserParameters PrepareSSGIDenoiserParameters(HDCamera hdCamera, bool halfResolution, float historyValidity, bool historyNeedsClear, HDUtils.PackedMipChainInfo depthMipInfo)
        {
            var giSettings = hdCamera.volumeStack.GetComponent<UnityEngine.Rendering.HighDefinition.GlobalIllumination>();

            SSGIDenoiserParameters parameters = new SSGIDenoiserParameters();

            // Compute the dispatch parameters based on if we are half res or not
            int tileSize = 8;
            EvaluateDispatchParameters(hdCamera, halfResolution, tileSize, out parameters.numTilesX, out parameters.numTilesY, out parameters.halfScreenSize);
            parameters.firstMipOffset.Set(HDShadowUtils.Asfloat((uint)depthMipInfo.mipLevelOffsets[1].x), HDShadowUtils.Asfloat((uint)depthMipInfo.mipLevelOffsets[1].y));
            parameters.historyValidity = historyValidity;
            parameters.viewCount = hdCamera.viewCount;
            parameters.pixelSpreadTangent = HDRenderPipeline.GetPixelSpreadTangent(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight);

            // Denoising parameters
            parameters.filterRadius = giSettings.filterRadius;
            parameters.halfResolution = halfResolution;
            parameters.historyValidity = historyValidity;
            parameters.historyNeedsClear = historyNeedsClear;

            // Compute shader
            parameters.ssgiDenoiserCS = m_SSGIDenoiserCS;

            // Kernels
            parameters.spatialFilterKernel = halfResolution ? m_SpatialFilterHalfKernel : m_SpatialFilterKernel;
            parameters.temporalFilterKernel = halfResolution ? m_TemporalFilterHalfKernel : m_TemporalFilterKernel;
            parameters.copyHistory = m_CopyHistory;

            return parameters;
        }

        struct SSGIDenoiserResources
        {
            // Input Buffers
            public RTHandle depthTexture;
            public RTHandle normalBuffer;
            public RTHandle motionVectorsBuffer;

            // History Buffer
            public RTHandle indirectDiffuseHistory0;
            public RTHandle indirectDiffuseHistory1;
            public RTHandle historyDepthBuffer;

            // Intermediate buffer
            public RTHandle intermediateBuffer0;
            public RTHandle intermediateBuffer1;

            // In-output Buffer
            public RTHandle inputOutputBuffer0;
            public RTHandle inputOutputBuffer1;
        }

        SSGIDenoiserResources PrepareSSGIDenoiserResources(RTHandle historyDepthBuffer, RTHandle indirectDiffuseHistory0, RTHandle indirectDiffuseHistory1, RTHandle inputOutputBuffer0, RTHandle inputOutputBuffer1, RTHandle intermediateBuffer0, RTHandle intermediateBuffer1)
        {
            SSGIDenoiserResources resources = new SSGIDenoiserResources();

            // Input Buffers
            resources.depthTexture = m_SharedRTManager.GetDepthTexture();
            resources.normalBuffer = m_SharedRTManager.GetNormalBuffer();
            resources.motionVectorsBuffer = m_SharedRTManager.GetMotionVectorsBuffer();

            // History Buffer
            resources.indirectDiffuseHistory0 = indirectDiffuseHistory0;
            resources.indirectDiffuseHistory1 = indirectDiffuseHistory1;
            resources.historyDepthBuffer = historyDepthBuffer;

            // Intermediate buffer
            resources.intermediateBuffer0 = intermediateBuffer0;
            resources.intermediateBuffer1 = intermediateBuffer1;

            // In-output Buffer
            resources.inputOutputBuffer0 = inputOutputBuffer0;
            resources.inputOutputBuffer1 = inputOutputBuffer1;

            return resources;
        }

        static void Denoise(CommandBuffer cmd, SSGIDenoiserParameters parameters, SSGIDenoiserResources resources)
        {
            if (resources.historyDepthBuffer == null)
                return;

            // Bind the input scalars
            cmd.SetComputeVectorParam(parameters.ssgiDenoiserCS, HDShaderIDs._DepthPyramidFirstMipLevelOffset, parameters.firstMipOffset);
            cmd.SetComputeIntParam(parameters.ssgiDenoiserCS, HDShaderIDs._IndirectDiffuseSpatialFilter, parameters.filterRadius);
            cmd.SetComputeFloatParam(parameters.ssgiDenoiserCS, HDShaderIDs._PixelSpreadAngleTangent, parameters.pixelSpreadTangent);
            // Inject half screen size if required
            if (parameters.halfResolution)
                cmd.SetComputeVectorParam(parameters.ssgiDenoiserCS, HDShaderIDs._HalfScreenSize, parameters.halfScreenSize);

            // Bind the input buffers
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.spatialFilterKernel, HDShaderIDs._DepthTexture, resources.depthTexture);
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.spatialFilterKernel, HDShaderIDs._NormalBufferTexture, resources.normalBuffer);
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.spatialFilterKernel, HDShaderIDs._InputNoisyBuffer0, resources.inputOutputBuffer0);
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.spatialFilterKernel, HDShaderIDs._InputNoisyBuffer1, resources.inputOutputBuffer1);

            // Bind the output buffer
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.spatialFilterKernel, HDShaderIDs._OutputFilteredBuffer0, resources.intermediateBuffer0);
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.spatialFilterKernel, HDShaderIDs._OutputFilteredBuffer1, resources.intermediateBuffer1);

            // Do the spatial pass
            cmd.DispatchCompute(parameters.ssgiDenoiserCS, parameters.spatialFilterKernel, parameters.numTilesX, parameters.numTilesY, parameters.viewCount);

            // Grab the history buffer
            if (parameters.historyNeedsClear)
            {
                // clear it to black if this is the first pass to avoid nans
                CoreUtils.SetRenderTarget(cmd, resources.indirectDiffuseHistory0, ClearFlag.Color, Color.black);
                CoreUtils.SetRenderTarget(cmd, resources.indirectDiffuseHistory1, ClearFlag.Color, s_CoCgAccClearColor);
            }

            // Bind the input buffers
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.temporalFilterKernel, HDShaderIDs._DepthTexture, resources.depthTexture);
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.temporalFilterKernel, HDShaderIDs._NormalBufferTexture, resources.normalBuffer);
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.temporalFilterKernel, HDShaderIDs._CameraMotionVectorsTexture, resources.motionVectorsBuffer);
            cmd.SetComputeFloatParam(parameters.ssgiDenoiserCS, HDShaderIDs._HistoryValidity, parameters.historyValidity);
            if (parameters.halfResolution)
            {
                cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.temporalFilterKernel, HDShaderIDs._HistoryDepthTexture, resources.historyDepthBuffer);
                cmd.SetComputeVectorParam(parameters.ssgiDenoiserCS, HDShaderIDs._DepthPyramidFirstMipLevelOffset, parameters.firstMipOffset);
            }
            else
            {
                cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.temporalFilterKernel, HDShaderIDs._HistoryDepthTexture, resources.historyDepthBuffer);
            }
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.temporalFilterKernel, HDShaderIDs._HistoryBuffer0, resources.indirectDiffuseHistory0);
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.temporalFilterKernel, HDShaderIDs._HistoryBuffer1, resources.indirectDiffuseHistory1);
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.temporalFilterKernel, HDShaderIDs._InputNoisyBuffer0, resources.intermediateBuffer0);
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.temporalFilterKernel, HDShaderIDs._InputNoisyBuffer1, resources.intermediateBuffer1);

            // Bind the output buffer
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.temporalFilterKernel, HDShaderIDs._OutputFilteredBuffer0, resources.inputOutputBuffer0);
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.temporalFilterKernel, HDShaderIDs._OutputFilteredBuffer1, resources.inputOutputBuffer1);

            // Do the temporal pass
            cmd.DispatchCompute(parameters.ssgiDenoiserCS, parameters.temporalFilterKernel, parameters.numTilesX, parameters.numTilesY, parameters.viewCount);

            // Copy the new version into the history buffer
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.copyHistory, HDShaderIDs._InputNoisyBuffer0, resources.inputOutputBuffer0);
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.copyHistory, HDShaderIDs._InputNoisyBuffer1, resources.inputOutputBuffer1);
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.copyHistory, HDShaderIDs._OutputFilteredBuffer0, resources.indirectDiffuseHistory0);
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.copyHistory, HDShaderIDs._OutputFilteredBuffer1, resources.indirectDiffuseHistory1);
            cmd.DispatchCompute(parameters.ssgiDenoiserCS, parameters.copyHistory, parameters.numTilesX, parameters.numTilesY, parameters.viewCount);
        }

        RTHandle RequestIndirectDiffuseHistory0(HDCamera hdCamera, out bool historyRequireClear)
        {
            historyRequireClear = false;
            RTHandle indirectDiffuseHistory = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF);
            if (indirectDiffuseHistory == null)
            {
                indirectDiffuseHistory = hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseHF, IndirectDiffuseHistoryBufferAllocatorFunction, 1);
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
                indirectDiffuseHistory = hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedIndirectDiffuseLF, IndirectDiffuseHistoryBufferAllocatorFunction, 1);
                historyRequireClear = true;
            }
            return indirectDiffuseHistory;
        }

        public void Denoise(CommandBuffer cmd, HDCamera hdCamera, RTHandle inputOutputBuffer0, RTHandle inputOutputBuffer1, RTHandle intermediateBuffer0, RTHandle intermediateBuffer1, bool halfResolution = false, float historyValidity = 1.0f)
        {
            var historyDepthBuffer = halfResolution ? hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth1) : hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
            bool historyRequireClear = false;
            RTHandle indirectDiffuseHistory0 = RequestIndirectDiffuseHistory0(hdCamera, out historyRequireClear);
            RTHandle indirectDiffuseHistory1 = RequestIndirectDiffuseHistory1(hdCamera, out historyRequireClear);

            SSGIDenoiserParameters parameters = PrepareSSGIDenoiserParameters(hdCamera, halfResolution, historyValidity, historyRequireClear, m_SharedRTManager.GetDepthBufferMipChainInfo());
            SSGIDenoiserResources resources = PrepareSSGIDenoiserResources(historyDepthBuffer, indirectDiffuseHistory0, indirectDiffuseHistory1, inputOutputBuffer0, inputOutputBuffer1, intermediateBuffer0, intermediateBuffer1);
            Denoise(cmd, parameters, resources);
        }

        class DenoiseSSGIPassData
        {
            public SSGIDenoiserParameters parameters;
            public TextureHandle depthTexture;
            public TextureHandle normalBuffer;
            public TextureHandle motionVectorsBuffer;
            public TextureHandle indirectDiffuseHistory0;
            public TextureHandle indirectDiffuseHistory1;
            public TextureHandle historyDepthBuffer;
            public TextureHandle intermediateBuffer0;
            public TextureHandle intermediateBuffer1;
            public TextureHandle inputOutputBuffer0;
            public TextureHandle inputOutputBuffer1;
        }

        public struct SSGIDenoiserOutput
        {
            public TextureHandle outputBuffer0;
            public TextureHandle outputBuffer1;
        }
        public SSGIDenoiserOutput Denoise(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle motionVectorsBuffer, TextureHandle inputOutputBuffer0, TextureHandle inputOutputBuffer1,
            HDUtils.PackedMipChainInfo depthMipInfo, bool halfResolution = false, float historyValidity = 1.0f)
        {
            using (var builder = renderGraph.AddRenderPass<DenoiseSSGIPassData>("Denoise SSGI", out var passData, ProfilingSampler.Get(HDProfileId.SSGIDenoise)))
            {
                builder.EnableAsyncCompute(false);

                // Input buffers
                passData.depthTexture = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.motionVectorsBuffer = builder.ReadTexture(motionVectorsBuffer);


                // History buffer
                bool historyRequireClear = false;
                RTHandle indirectDiffuseHistory0 = RequestIndirectDiffuseHistory0(hdCamera, out historyRequireClear);
                passData.indirectDiffuseHistory0 = builder.ReadTexture(builder.WriteTexture(renderGraph.ImportTexture(indirectDiffuseHistory0)));
                RTHandle indirectDiffuseHistory1 = RequestIndirectDiffuseHistory1(hdCamera, out historyRequireClear);
                passData.indirectDiffuseHistory1 = builder.ReadTexture(builder.WriteTexture(renderGraph.ImportTexture(indirectDiffuseHistory1)));
                var historyDepthBuffer = halfResolution ? hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth1) : hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
                passData.historyDepthBuffer = historyDepthBuffer != null ? builder.ReadTexture(renderGraph.ImportTexture(historyDepthBuffer)) : renderGraph.defaultResources.blackTextureXR;
                passData.intermediateBuffer0 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "SSGI Denoiser Intermediate0" });
                passData.intermediateBuffer1 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "SSGI Denoiser Intermediate1" });
                passData.inputOutputBuffer0 = builder.WriteTexture(builder.ReadTexture(inputOutputBuffer0));
                passData.inputOutputBuffer1 = builder.WriteTexture(builder.ReadTexture(inputOutputBuffer1));

                passData.parameters = PrepareSSGIDenoiserParameters(hdCamera, halfResolution, historyValidity, historyRequireClear, depthMipInfo);

                builder.SetRenderFunc(
                (DenoiseSSGIPassData data, RenderGraphContext ctx) =>
                {
                    // We need to fill the structure that holds the various resources
                    SSGIDenoiserResources resources = new SSGIDenoiserResources();
                    resources.depthTexture = data.depthTexture;
                    resources.normalBuffer = data.normalBuffer;
                    resources.motionVectorsBuffer = data.motionVectorsBuffer;
                    resources.indirectDiffuseHistory0 = data.indirectDiffuseHistory0;
                    resources.indirectDiffuseHistory1 = data.indirectDiffuseHistory1;
                    resources.historyDepthBuffer = data.historyDepthBuffer;
                    resources.intermediateBuffer0 = data.intermediateBuffer0;
                    resources.intermediateBuffer1 = data.intermediateBuffer1;
                    resources.inputOutputBuffer0 = data.inputOutputBuffer0;
                    resources.inputOutputBuffer1 = data.inputOutputBuffer1;
                    Denoise(ctx.cmd, data.parameters, resources);
                });

                SSGIDenoiserOutput denoiserOutput = new SSGIDenoiserOutput();
                denoiserOutput.outputBuffer0 = inputOutputBuffer0;
                denoiserOutput.outputBuffer1 = inputOutputBuffer1;
                return denoiserOutput;
            }
        }
    }
}
