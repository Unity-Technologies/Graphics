using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    class HDSimpleDenoiser
    {
        ComputeShader m_SimpleDenoiserCS;
        SharedRTManager m_SharedRTManager;
        HDRenderPipeline m_RenderPipeline;

        int m_BilateralFilterHSingleKernel;
        int m_BilateralFilterVSingleKernel;
        int m_BilateralFilterHColorKernel;
        int m_BilateralFilterVColorKernel;

        public HDSimpleDenoiser()
        {
        }

        public void Init(HDRenderPipelineRayTracingResources rpRTResources, SharedRTManager sharedRTManager, HDRenderPipeline renderPipeline)
        {
            m_SimpleDenoiserCS = rpRTResources.simpleDenoiserCS;
            m_SharedRTManager = sharedRTManager;
            m_RenderPipeline = renderPipeline;

            m_BilateralFilterHSingleKernel = m_SimpleDenoiserCS.FindKernel("BilateralFilterHSingle");
            m_BilateralFilterVSingleKernel = m_SimpleDenoiserCS.FindKernel("BilateralFilterVSingle");
            m_BilateralFilterHColorKernel = m_SimpleDenoiserCS.FindKernel("BilateralFilterHColor");
            m_BilateralFilterVColorKernel = m_SimpleDenoiserCS.FindKernel("BilateralFilterVColor");
        }

        public void Release()
        {
        }

        struct SimpleDenoiserParameters
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Evaluation parameters
            public int kernelSize;

            // Kernels
            public int bilateralHKernel;
            public int bilateralVKernel;

            // Other parameters
            public ComputeShader simpleDenoiserCS;
        }

        SimpleDenoiserParameters PrepareSimpleDenoiserParameters(HDCamera hdCamera, bool singleChannel, int kernelSize)
        {
            SimpleDenoiserParameters sdParams = new SimpleDenoiserParameters();

            // Camera parameters
            sdParams.texWidth = hdCamera.actualWidth;
            sdParams.texHeight = hdCamera.actualHeight;
            sdParams.viewCount = hdCamera.viewCount;

            // Evaluation parameters
            sdParams.kernelSize = kernelSize;

            // Kernels
            sdParams.bilateralHKernel = singleChannel ? m_BilateralFilterHSingleKernel : m_BilateralFilterHColorKernel;
            sdParams.bilateralVKernel = singleChannel ? m_BilateralFilterVSingleKernel : m_BilateralFilterVColorKernel;

            // Other parameters
            sdParams.simpleDenoiserCS = m_SimpleDenoiserCS;
            return sdParams;
        }

        struct SimpleDenoiserResources
        {
            // Input buffers
            public RTHandle noisyBuffer;
            public RTHandle depthStencilBuffer;
            public RTHandle normalBuffer;

            // Temporary buffers
            public RTHandle intermediateBuffer;

            // Output buffers
            public RTHandle outputBuffer;
        }

        SimpleDenoiserResources PrepareSimpleDenoiserResources(RTHandle noisyBuffer, RTHandle intermediateBuffer, RTHandle outputBuffer)
        {
            SimpleDenoiserResources sdResources = new SimpleDenoiserResources();

            // Input buffers
            sdResources.noisyBuffer = noisyBuffer;
            sdResources.depthStencilBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            sdResources.normalBuffer = m_SharedRTManager.GetNormalBuffer();

            // Temporary buffers
            sdResources.intermediateBuffer = intermediateBuffer;

            // Output buffers
            sdResources.outputBuffer = outputBuffer;

            return sdResources;
        }


        static void ExecuteSimpleDenoiser(CommandBuffer cmd, SimpleDenoiserParameters sdParams, SimpleDenoiserResources sdResources)
        {
            // Evaluate the dispatch parameters
            int sdTileSize = 8;
            int numTilesX = (sdParams.texWidth + (sdTileSize - 1)) / sdTileSize;
            int numTilesY = (sdParams.texHeight + (sdTileSize - 1)) / sdTileSize;

            // Horizontal pass of the bilateral filter
            cmd.SetComputeIntParam(sdParams.simpleDenoiserCS, HDShaderIDs._DenoiserFilterRadius, sdParams.kernelSize);
            cmd.SetComputeTextureParam(sdParams.simpleDenoiserCS, sdParams.bilateralHKernel, HDShaderIDs._DenoiseInputTexture, sdResources.noisyBuffer);
            cmd.SetComputeTextureParam(sdParams.simpleDenoiserCS, sdParams.bilateralHKernel, HDShaderIDs._DepthTexture, sdResources.depthStencilBuffer);
            cmd.SetComputeTextureParam(sdParams.simpleDenoiserCS, sdParams.bilateralHKernel, HDShaderIDs._NormalBufferTexture, sdResources.normalBuffer);
            cmd.SetComputeTextureParam(sdParams.simpleDenoiserCS, sdParams.bilateralHKernel, HDShaderIDs._DenoiseOutputTextureRW, sdResources.intermediateBuffer);
            cmd.DispatchCompute(sdParams.simpleDenoiserCS, sdParams.bilateralHKernel, numTilesX, numTilesY, sdParams.viewCount);

            // Horizontal pass of the bilateral filter
            cmd.SetComputeIntParam(sdParams.simpleDenoiserCS, HDShaderIDs._DenoiserFilterRadius, sdParams.kernelSize);
            cmd.SetComputeTextureParam(sdParams.simpleDenoiserCS, sdParams.bilateralVKernel, HDShaderIDs._DenoiseInputTexture, sdResources.intermediateBuffer);
            cmd.SetComputeTextureParam(sdParams.simpleDenoiserCS, sdParams.bilateralVKernel, HDShaderIDs._DepthTexture, sdResources.depthStencilBuffer);
            cmd.SetComputeTextureParam(sdParams.simpleDenoiserCS, sdParams.bilateralVKernel, HDShaderIDs._NormalBufferTexture, sdResources.normalBuffer);
            cmd.SetComputeTextureParam(sdParams.simpleDenoiserCS, sdParams.bilateralVKernel, HDShaderIDs._DenoiseOutputTextureRW, sdResources.outputBuffer);
            cmd.DispatchCompute(sdParams.simpleDenoiserCS, sdParams.bilateralVKernel, numTilesX, numTilesY, sdParams.viewCount);
        }

        public void DenoiseBufferNoHistory(CommandBuffer cmd, HDCamera hdCamera, RTHandle noisyBuffer, RTHandle outputBuffer, int kernelSize, bool singleChannel = true)
        {
            // Request the intermediate buffers that we need
            RTHandle intermediateBuffer = m_RenderPipeline.GetRayTracingBuffer(InternalRayTracingBuffers.RGBA3);
            SimpleDenoiserParameters sdParams = PrepareSimpleDenoiserParameters(hdCamera, singleChannel, kernelSize);
            SimpleDenoiserResources sdResources = PrepareSimpleDenoiserResources(noisyBuffer, intermediateBuffer, outputBuffer);
            ExecuteSimpleDenoiser(cmd, sdParams, sdResources);
        }

        class SimpleDenoiserPassData
        {
            public SimpleDenoiserParameters parameters;
            // Input buffers
            public TextureHandle noisyBuffer;
            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;

            // Temporary buffers
            public TextureHandle intermediateBuffer;

            // Output buffers
            public TextureHandle outputBuffer;
        }

        public TextureHandle DenoiseBufferNoHistory(RenderGraph renderGraph, HDCamera hdCamera,
                            TextureHandle depthBuffer, TextureHandle normalBuffer,
                            TextureHandle noisyBuffer,
                            int kernelSize, bool singleChannel)
        {
            using (var builder = renderGraph.AddRenderPass<SimpleDenoiserPassData>("DiffuseDenoiser", out var passData, ProfilingSampler.Get(HDProfileId.DiffuseFilter)))
            {
                // Cannot run in async
                builder.EnableAsyncCompute(false);

                // Fetch all the resources
                passData.parameters = PrepareSimpleDenoiserParameters(hdCamera, singleChannel, kernelSize);

                // Input buffers
                passData.depthStencilBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.Read);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.noisyBuffer = builder.ReadTexture(noisyBuffer);

                // Temporary buffers
                passData.intermediateBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Intermediate buffer" });

                // Output buffer
                passData.outputBuffer = builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
				{ colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Denoised Buffer" }));


                builder.SetRenderFunc(
                (SimpleDenoiserPassData data, RenderGraphContext ctx) =>
                {
                    SimpleDenoiserResources resources = new SimpleDenoiserResources();
                    resources.depthStencilBuffer = data.depthStencilBuffer;
                    resources.normalBuffer = data.normalBuffer;
                    resources.noisyBuffer = data.noisyBuffer;

                    resources.intermediateBuffer = data.intermediateBuffer;

                    resources.outputBuffer = data.outputBuffer;
                    ExecuteSimpleDenoiser(ctx.cmd, data.parameters, resources);
                });
                return passData.outputBuffer;
            }
        }
    }
}
