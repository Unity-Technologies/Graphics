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

            // Shader
            public ComputeShader ssgiDenoiserCS;

            // Kernels
            public int spatialFilterKernel;
            public int temporalFilterKernel;
            public int copyHistory;
        }

        SSGIDenoiserParameters PrepareSSGIDenoiserParameters(HDCamera hdCamera, bool halfResolution, float historyValidity, bool historyNeedsClear)
        {
            var giSettings = hdCamera.volumeStack.GetComponent<UnityEngine.Rendering.HighDefinition.GlobalIllumination>();

            SSGIDenoiserParameters parameters = new SSGIDenoiserParameters();

            // Compute the dispatch parameters based on if we are half res or not
            int tileSize = 8;
            EvaluateDispatchParameters(hdCamera, halfResolution, tileSize, out parameters.numTilesX, out parameters.numTilesY, out parameters.halfScreenSize);
            var info = m_SharedRTManager.GetDepthBufferMipChainInfo();
            parameters.firstMipOffset.Set(HDShadowUtils.Asfloat((uint)info.mipLevelOffsets[1].x), HDShadowUtils.Asfloat((uint)info.mipLevelOffsets[1].y));
            parameters.historyValidity = historyValidity;
            parameters.viewCount = hdCamera.viewCount;

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
            public RTHandle indirectDiffuseHistory;
            public RTHandle historyDepthBuffer;

            // Intermediate buffer
            public RTHandle intermediateBuffer;

            // In-output Buffer
            public RTHandle inputOutputBuffer;
        }

        SSGIDenoiserResources PrepareSSGIDenoiserResources(RTHandle historyDepthBuffer, RTHandle indirectDiffuseHistory, RTHandle inputOutputBuffer, RTHandle intermediateBuffer)
        {
            SSGIDenoiserResources resources = new SSGIDenoiserResources();

            // Input Buffers
            resources.depthTexture = m_SharedRTManager.GetDepthTexture();
            resources.normalBuffer = m_SharedRTManager.GetNormalBuffer();
            resources.motionVectorsBuffer = m_SharedRTManager.GetMotionVectorsBuffer();

            // History Buffer
            resources.indirectDiffuseHistory = indirectDiffuseHistory;
            resources.historyDepthBuffer = historyDepthBuffer;

            // Intermediate buffer
            resources.intermediateBuffer = intermediateBuffer;

            // In-output Buffer
            resources.inputOutputBuffer = inputOutputBuffer;

            return resources;
        }

        static void Denoise(CommandBuffer cmd, SSGIDenoiserParameters parameters, SSGIDenoiserResources resources)
        {
            if (resources.historyDepthBuffer == null)
                return;

            // Bind the input scalars
            cmd.SetComputeVectorParam(parameters.ssgiDenoiserCS, HDShaderIDs._DepthPyramidFirstMipLevelOffset, parameters.firstMipOffset);
            cmd.SetComputeIntParam(parameters.ssgiDenoiserCS, HDShaderIDs._IndirectDiffuseSpatialFilter, parameters.filterRadius);
            // Inject half screen size if required
            if (parameters.halfResolution)
                cmd.SetComputeVectorParam(parameters.ssgiDenoiserCS, HDShaderIDs._HalfScreenSize, parameters.halfScreenSize);

            // Bind the input buffers
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.spatialFilterKernel, HDShaderIDs._DepthTexture, resources.depthTexture);
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.spatialFilterKernel, HDShaderIDs._NormalBufferTexture, resources.normalBuffer);
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.spatialFilterKernel, HDShaderIDs._InputNoisyBuffer, resources.inputOutputBuffer);

            // Bind the output buffer
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.spatialFilterKernel, HDShaderIDs._OutputFilteredBuffer, resources.intermediateBuffer);

            // Do the spatial pass
            cmd.DispatchCompute(parameters.ssgiDenoiserCS, parameters.spatialFilterKernel, parameters.numTilesX, parameters.numTilesY, parameters.viewCount);

            // Grab the history buffer
            if (parameters.historyNeedsClear)
            {
                // clear it to black if this is the first pass to avoid nans
                CoreUtils.SetRenderTarget(cmd, resources.indirectDiffuseHistory, ClearFlag.Color, clearColor: Color.black);
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
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.temporalFilterKernel, HDShaderIDs._HistoryBuffer, resources.indirectDiffuseHistory);
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.temporalFilterKernel, HDShaderIDs._InputNoisyBuffer, resources.intermediateBuffer);

            // Bind the output buffer
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.temporalFilterKernel, HDShaderIDs._OutputFilteredBuffer, resources.inputOutputBuffer);

            // Do the temporal pass
            cmd.DispatchCompute(parameters.ssgiDenoiserCS, parameters.temporalFilterKernel, parameters.numTilesX, parameters.numTilesY, parameters.viewCount);

            // Copy the new version into the history buffer
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.copyHistory, HDShaderIDs._InputNoisyBuffer, resources.inputOutputBuffer);
            cmd.SetComputeTextureParam(parameters.ssgiDenoiserCS, parameters.copyHistory, HDShaderIDs._OutputFilteredBuffer, resources.indirectDiffuseHistory);
            cmd.DispatchCompute(parameters.ssgiDenoiserCS, parameters.copyHistory, parameters.numTilesX, parameters.numTilesY, parameters.viewCount);
        }

        RTHandle RequestIndirectDiffuseHistory(HDCamera hdCamera, out bool historyRequireClear)
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

        public void Denoise(CommandBuffer cmd, HDCamera hdCamera, RTHandle inputOutputBuffer, RTHandle intermediateBuffer, bool halfResolution = false, float historyValidity = 1.0f)
        {
            var historyDepthBuffer = halfResolution ? hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth1) : hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
            bool historyRequireClear = false;
            RTHandle indirectDiffuseHistory = RequestIndirectDiffuseHistory(hdCamera, out historyRequireClear);

            SSGIDenoiserParameters parameters = PrepareSSGIDenoiserParameters(hdCamera, halfResolution, historyValidity, historyRequireClear);
            SSGIDenoiserResources resources = PrepareSSGIDenoiserResources(historyDepthBuffer, indirectDiffuseHistory, inputOutputBuffer, intermediateBuffer);
            Denoise(cmd, parameters, resources);
        }
        class TraceRTGIPassData
        {
            public SSGIDenoiserParameters parameters;
            public TextureHandle depthTexture;
            public TextureHandle normalBuffer;
            public TextureHandle motionVectorsBuffer;
            public TextureHandle indirectDiffuseHistory;
            public TextureHandle historyDepthBuffer;
            public TextureHandle intermediateBuffer;
            public TextureHandle inputOutputBuffer;
        }

        public TextureHandle Denoise(RenderGraph renderGraph, HDCamera hdCamera,
            TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle motionVectorsBuffer, TextureHandle inputOutputBuffer,
            bool halfResolution = false, float historyValidity = 1.0f)
        {
            using (var builder = renderGraph.AddRenderPass<TraceRTGIPassData>("Denoise SSGI", out var passData, ProfilingSampler.Get(HDProfileId.SSGIDenoise)))
            {
                builder.EnableAsyncCompute(false);

                // Input buffers
                passData.depthTexture = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.motionVectorsBuffer = builder.ReadTexture(motionVectorsBuffer);

                // History buffer
                bool historyRequireClear = false;
                RTHandle indirectDiffuseHistory = RequestIndirectDiffuseHistory(hdCamera, out historyRequireClear);
                passData.indirectDiffuseHistory = builder.ReadTexture(builder.WriteTexture(renderGraph.ImportTexture(indirectDiffuseHistory)));
                var historyDepthBuffer = halfResolution ? hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth1) : hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
                passData.historyDepthBuffer = historyDepthBuffer != null ? builder.ReadTexture(renderGraph.ImportTexture(historyDepthBuffer)) : renderGraph.defaultResources.blackTextureXR;
                passData.intermediateBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "SSGI Denoiser Intermediate" });
                passData.inputOutputBuffer = builder.WriteTexture(builder.ReadTexture(inputOutputBuffer));

                passData.parameters = PrepareSSGIDenoiserParameters(hdCamera, halfResolution, historyValidity, historyRequireClear);

                builder.SetRenderFunc(
                (TraceRTGIPassData data, RenderGraphContext ctx) =>
                {
                    // We need to fill the structure that holds the various resources
                    SSGIDenoiserResources resources = new SSGIDenoiserResources();
                    resources.depthTexture = data.depthTexture;
                    resources.normalBuffer = data.normalBuffer;
                    resources.motionVectorsBuffer = data.motionVectorsBuffer;
                    resources.indirectDiffuseHistory = data.indirectDiffuseHistory;
                    resources.historyDepthBuffer = data.historyDepthBuffer;
                    resources.intermediateBuffer = data.intermediateBuffer;
                    resources.inputOutputBuffer = data.inputOutputBuffer;
                    Denoise(ctx.cmd, data.parameters, resources);
                });
                return inputOutputBuffer;
            }
        }
    }
}
