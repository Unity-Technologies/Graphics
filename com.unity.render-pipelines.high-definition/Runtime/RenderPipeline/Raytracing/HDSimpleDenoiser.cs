using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    class HDSimpleDenoiser
    {
        ComputeShader m_SimpleDenoiserCS;

        int m_BilateralFilterHSingleKernel;
        int m_BilateralFilterVSingleKernel;
        int m_BilateralFilterHColorKernel;
        int m_BilateralFilterVColorKernel;

        public HDSimpleDenoiser()
        {
        }

        public void Init(HDRenderPipelineRayTracingResources rpRTResources)
        {
            m_SimpleDenoiserCS = rpRTResources.simpleDenoiserCS;

            m_BilateralFilterHSingleKernel = m_SimpleDenoiserCS.FindKernel("BilateralFilterHSingle");
            m_BilateralFilterVSingleKernel = m_SimpleDenoiserCS.FindKernel("BilateralFilterVSingle");
            m_BilateralFilterHColorKernel = m_SimpleDenoiserCS.FindKernel("BilateralFilterHColor");
            m_BilateralFilterVColorKernel = m_SimpleDenoiserCS.FindKernel("BilateralFilterVColor");
        }

        public void Release()
        {
        }

        class SimpleDenoiserPassData
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

                // Camera parameters
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Evaluation parameters
                passData.kernelSize = kernelSize;

                // Kernels
                passData.bilateralHKernel = singleChannel ? m_BilateralFilterHSingleKernel : m_BilateralFilterHColorKernel;
                passData.bilateralVKernel = singleChannel ? m_BilateralFilterVSingleKernel : m_BilateralFilterVColorKernel;

                // Other parameters
                passData.simpleDenoiserCS = m_SimpleDenoiserCS;

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
                        // Evaluate the dispatch parameters
                        int sdTileSize = 8;
                        int numTilesX = (data.texWidth + (sdTileSize - 1)) / sdTileSize;
                        int numTilesY = (data.texHeight + (sdTileSize - 1)) / sdTileSize;

                        // Horizontal pass of the bilateral filter
                        ctx.cmd.SetComputeIntParam(data.simpleDenoiserCS, HDShaderIDs._DenoiserFilterRadius, data.kernelSize);
                        ctx.cmd.SetComputeTextureParam(data.simpleDenoiserCS, data.bilateralHKernel, HDShaderIDs._DenoiseInputTexture, data.noisyBuffer);
                        ctx.cmd.SetComputeTextureParam(data.simpleDenoiserCS, data.bilateralHKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                        ctx.cmd.SetComputeTextureParam(data.simpleDenoiserCS, data.bilateralHKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.simpleDenoiserCS, data.bilateralHKernel, HDShaderIDs._DenoiseOutputTextureRW, data.intermediateBuffer);
                        ctx.cmd.DispatchCompute(data.simpleDenoiserCS, data.bilateralHKernel, numTilesX, numTilesY, data.viewCount);

                        // Horizontal pass of the bilateral filter
                        ctx.cmd.SetComputeIntParam(data.simpleDenoiserCS, HDShaderIDs._DenoiserFilterRadius, data.kernelSize);
                        ctx.cmd.SetComputeTextureParam(data.simpleDenoiserCS, data.bilateralVKernel, HDShaderIDs._DenoiseInputTexture, data.intermediateBuffer);
                        ctx.cmd.SetComputeTextureParam(data.simpleDenoiserCS, data.bilateralVKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                        ctx.cmd.SetComputeTextureParam(data.simpleDenoiserCS, data.bilateralVKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.simpleDenoiserCS, data.bilateralVKernel, HDShaderIDs._DenoiseOutputTextureRW, data.outputBuffer);
                        ctx.cmd.DispatchCompute(data.simpleDenoiserCS, data.bilateralVKernel, numTilesX, numTilesY, data.viewCount);
                    });
                return passData.outputBuffer;
            }
        }
    }
}
