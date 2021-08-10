using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class HDDiffuseDenoiser
    {
        // Resources used for the denoiser
        ComputeShader m_DiffuseDenoiser;
        Texture m_OwenScrambleRGBA;

        HDRenderPipeline m_RenderPipeline;

        // Kernels that may be required
        int m_BilateralFilterSingleKernel;
        int m_BilateralFilterColorKernel;
        int m_GatherSingleKernel;
        int m_GatherColorKernel;

        public void Init(HDRenderPipelineRuntimeResources rpResources, HDRenderPipeline renderPipeline)
        {
            // Keep track of the resources
            m_DiffuseDenoiser = rpResources.shaders.diffuseDenoiserCS;
            m_OwenScrambleRGBA = rpResources.textures.owenScrambledRGBATex;

            m_RenderPipeline = renderPipeline;

            // Grab all the kernels we'll eventually need
            m_BilateralFilterSingleKernel = m_DiffuseDenoiser.FindKernel("BilateralFilterSingle");
            m_BilateralFilterColorKernel = m_DiffuseDenoiser.FindKernel("BilateralFilterColor");
            m_GatherSingleKernel = m_DiffuseDenoiser.FindKernel("GatherSingle");
            m_GatherColorKernel = m_DiffuseDenoiser.FindKernel("GatherColor");
        }

        public void Release()
        {
        }

        class DiffuseDenoiserPassData
        {
            // Camera parameters
            public int texWidth;
            public int texHeight;
            public int viewCount;

            // Denoising parameters
            public float pixelSpreadTangent;
            public float kernelSize;
            public bool halfResolutionFilter;
            public bool jitterFilter;
            public int frameIndex;

            // Kernels
            public int bilateralFilterKernel;
            public int gatherKernel;

            // Other parameters
            public Texture owenScrambleRGBA;
            public ComputeShader diffuseDenoiserCS;

            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle noisyBuffer;
            public TextureHandle intermediateBuffer;
            public TextureHandle outputBuffer;
        }

        public TextureHandle Denoise(RenderGraph renderGraph, HDCamera hdCamera, bool singleChannel, float kernelSize, bool halfResolutionFilter, bool jitterFilter,
            TextureHandle noisyBuffer, TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle outputBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<DiffuseDenoiserPassData>("DiffuseDenoiser", out var passData, ProfilingSampler.Get(HDProfileId.DiffuseFilter)))
            {
                // Cannot run in async
                builder.EnableAsyncCompute(false);

                // Fetch all the resources
                // Camera parameters
                passData.texWidth = hdCamera.actualWidth;
                passData.texHeight = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Denoising parameters
                passData.pixelSpreadTangent = HDRenderPipeline.GetPixelSpreadTangent(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight);
                passData.kernelSize = kernelSize;
                passData.halfResolutionFilter = halfResolutionFilter;
                passData.jitterFilter = jitterFilter;
                passData.frameIndex = m_RenderPipeline.RayTracingFrameIndex(hdCamera);

                // Kernels
                passData.bilateralFilterKernel = singleChannel ? m_BilateralFilterSingleKernel : m_BilateralFilterColorKernel;
                passData.gatherKernel = singleChannel ? m_GatherSingleKernel : m_GatherColorKernel;

                // Other parameters
                passData.owenScrambleRGBA = m_OwenScrambleRGBA;
                passData.diffuseDenoiserCS = m_DiffuseDenoiser;

                passData.depthStencilBuffer = builder.ReadTexture(depthBuffer);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.noisyBuffer = builder.ReadTexture(noisyBuffer);
                passData.intermediateBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "DiffuseDenoiserIntermediate" });
                passData.outputBuffer = builder.WriteTexture(outputBuffer);

                builder.SetRenderFunc(
                    (DiffuseDenoiserPassData data, RenderGraphContext ctx) =>
                    {
                        // Evaluate the dispatch parameters
                        int areaTileSize = 8;
                        int numTilesX = (data.texWidth + (areaTileSize - 1)) / areaTileSize;
                        int numTilesY = (data.texHeight + (areaTileSize - 1)) / areaTileSize;

                        // Request the intermediate buffers that we need
                        ctx.cmd.SetGlobalTexture(HDShaderIDs._OwenScrambledRGTexture, data.owenScrambleRGBA);
                        ctx.cmd.SetComputeFloatParam(data.diffuseDenoiserCS, HDShaderIDs._DenoiserFilterRadius, data.kernelSize);
                        ctx.cmd.SetComputeTextureParam(data.diffuseDenoiserCS, data.bilateralFilterKernel, HDShaderIDs._DenoiseInputTexture, data.noisyBuffer);
                        ctx.cmd.SetComputeTextureParam(data.diffuseDenoiserCS, data.bilateralFilterKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                        ctx.cmd.SetComputeTextureParam(data.diffuseDenoiserCS, data.bilateralFilterKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.SetComputeTextureParam(data.diffuseDenoiserCS, data.bilateralFilterKernel, HDShaderIDs._DenoiseOutputTextureRW, data.halfResolutionFilter ? data.intermediateBuffer : data.outputBuffer);
                        ctx.cmd.SetComputeIntParam(data.diffuseDenoiserCS, HDShaderIDs._HalfResolutionFilter, data.halfResolutionFilter ? 1 : 0);
                        ctx.cmd.SetComputeFloatParam(data.diffuseDenoiserCS, HDShaderIDs._PixelSpreadAngleTangent, data.pixelSpreadTangent);
                        if (data.jitterFilter)
                            ctx.cmd.SetComputeIntParam(data.diffuseDenoiserCS, HDShaderIDs._JitterFramePeriod, (data.frameIndex % 4));
                        else
                            ctx.cmd.SetComputeIntParam(data.diffuseDenoiserCS, HDShaderIDs._JitterFramePeriod, -1);

                        ctx.cmd.DispatchCompute(data.diffuseDenoiserCS, data.bilateralFilterKernel, numTilesX, numTilesY, data.viewCount);

                        if (data.halfResolutionFilter)
                        {
                            ctx.cmd.SetComputeTextureParam(data.diffuseDenoiserCS, data.gatherKernel, HDShaderIDs._DenoiseInputTexture, data.intermediateBuffer);
                            ctx.cmd.SetComputeTextureParam(data.diffuseDenoiserCS, data.gatherKernel, HDShaderIDs._DepthTexture, data.depthStencilBuffer);
                            ctx.cmd.SetComputeTextureParam(data.diffuseDenoiserCS, data.gatherKernel, HDShaderIDs._DenoiseOutputTextureRW, data.outputBuffer);
                            ctx.cmd.DispatchCompute(data.diffuseDenoiserCS, data.gatherKernel, numTilesX, numTilesY, data.viewCount);
                        }
                    });
                return passData.outputBuffer;
            }
        }
    }
}
