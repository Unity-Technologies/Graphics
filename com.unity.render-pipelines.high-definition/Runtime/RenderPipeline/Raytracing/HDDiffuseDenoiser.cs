using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    struct DiffuseDenoiserParameters
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
    }

    struct DiffuseDenoiserResources
    {
        // Input buffers
        public RTHandle depthStencilBuffer;
        public RTHandle normalBuffer;
        public RTHandle noisyBuffer;

        // Temporary buffers
        public RTHandle intermediateBuffer;

        // Output buffers
        public RTHandle outputBuffer;
    }

    class HDDiffuseDenoiser
    {
        // Resources used for the denoiser
        ComputeShader m_DiffuseDenoiser;
        Texture m_OwenScrambleRGBA;

        // Required for fetching depth and normal buffers
        SharedRTManager m_SharedRTManager;
        HDRenderPipeline m_RenderPipeline;

        // Kernels that may be required
        int m_BilateralFilterSingleKernel;
        int m_BilateralFilterColorKernel;
        int m_GatherSingleKernel;
        int m_GatherColorKernel;

        public HDDiffuseDenoiser()
        {
        }

        public void Init(RenderPipelineResources rpResources, HDRenderPipelineRayTracingResources rpRTResources, SharedRTManager sharedRTManager, HDRenderPipeline renderPipeline)
        {
            // Keep track of the resources
            m_DiffuseDenoiser = rpRTResources.diffuseDenoiserCS;
            m_OwenScrambleRGBA = rpResources.textures.owenScrambledRGBATex;

            // Keep track of the shared rt manager
            m_SharedRTManager = sharedRTManager;
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

        public DiffuseDenoiserParameters PrepareDiffuseDenoiserParameters(HDCamera hdCamera, bool singleChannel, float kernelSize, bool halfResolutionFilter, bool jitterFilter)
        {
            DiffuseDenoiserParameters ddParams = new DiffuseDenoiserParameters();

            // Camera parameters
            ddParams.texWidth = hdCamera.actualWidth;
            ddParams.texHeight = hdCamera.actualHeight;
            ddParams.viewCount = hdCamera.viewCount;

            // Denoising parameters
            ddParams.pixelSpreadTangent = HDRenderPipeline.GetPixelSpreadTangent(hdCamera.camera.fieldOfView, hdCamera.actualWidth, hdCamera.actualHeight);
            ddParams.kernelSize = kernelSize;
            ddParams.halfResolutionFilter = halfResolutionFilter;
            ddParams.jitterFilter = jitterFilter;
            ddParams.frameIndex = m_RenderPipeline.RayTracingFrameIndex(hdCamera);

            // Kernels
            ddParams.bilateralFilterKernel = singleChannel ? m_BilateralFilterSingleKernel : m_BilateralFilterColorKernel;
            ddParams.gatherKernel = singleChannel ? m_GatherSingleKernel : m_GatherColorKernel;

            // Other parameters
            ddParams.owenScrambleRGBA = m_OwenScrambleRGBA;
            ddParams.diffuseDenoiserCS = m_DiffuseDenoiser;
            return ddParams;
        }

        public DiffuseDenoiserResources PrepareDiffuseDenoiserResources(RTHandle noisyBuffer, RTHandle intermediateBuffer, RTHandle outputBuffer)
        {
            DiffuseDenoiserResources ddResources = new DiffuseDenoiserResources();
            // Input buffers
            ddResources.depthStencilBuffer = m_SharedRTManager.GetDepthStencilBuffer();
            ddResources.normalBuffer = m_SharedRTManager.GetNormalBuffer();
            ddResources.noisyBuffer = noisyBuffer;

            // Temporary buffers
            ddResources.intermediateBuffer = intermediateBuffer;

            // Output buffers
            ddResources.outputBuffer = outputBuffer;

            return ddResources;
        }

        static public void DenoiseBuffer(CommandBuffer cmd, DiffuseDenoiserParameters ddParams, DiffuseDenoiserResources ddResources)
        {
            // Evaluate the dispatch parameters
            int areaTileSize = 8;
            int numTilesX = (ddParams.texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesY = (ddParams.texHeight + (areaTileSize - 1)) / areaTileSize;

            // Request the intermediate buffers that we need
            cmd.SetGlobalTexture(HDShaderIDs._OwenScrambledRGTexture, ddParams.owenScrambleRGBA);
            cmd.SetComputeFloatParam(ddParams.diffuseDenoiserCS, HDShaderIDs._DenoiserFilterRadius, ddParams.kernelSize);
            cmd.SetComputeTextureParam(ddParams.diffuseDenoiserCS, ddParams.bilateralFilterKernel, HDShaderIDs._DenoiseInputTexture, ddResources.noisyBuffer);
            cmd.SetComputeTextureParam(ddParams.diffuseDenoiserCS, ddParams.bilateralFilterKernel, HDShaderIDs._DepthTexture, ddResources.depthStencilBuffer);
            cmd.SetComputeTextureParam(ddParams.diffuseDenoiserCS, ddParams.bilateralFilterKernel, HDShaderIDs._NormalBufferTexture, ddResources.normalBuffer);
            cmd.SetComputeTextureParam(ddParams.diffuseDenoiserCS, ddParams.bilateralFilterKernel, HDShaderIDs._DenoiseOutputTextureRW, ddParams.halfResolutionFilter ? ddResources.intermediateBuffer : ddResources.outputBuffer);
            cmd.SetComputeIntParam(ddParams.diffuseDenoiserCS, HDShaderIDs._HalfResolutionFilter, ddParams.halfResolutionFilter ? 1 : 0);
            cmd.SetComputeFloatParam(ddParams.diffuseDenoiserCS, HDShaderIDs._PixelSpreadAngleTangent, ddParams.pixelSpreadTangent);
            if (ddParams.jitterFilter)
                cmd.SetComputeIntParam(ddParams.diffuseDenoiserCS, HDShaderIDs._JitterFramePeriod, (ddParams.frameIndex % 4));
            else
                cmd.SetComputeIntParam(ddParams.diffuseDenoiserCS, HDShaderIDs._JitterFramePeriod, -1);

            cmd.DispatchCompute(ddParams.diffuseDenoiserCS, ddParams.bilateralFilterKernel, numTilesX, numTilesY, ddParams.viewCount);

            if (ddParams.halfResolutionFilter)
            {
                cmd.SetComputeTextureParam(ddParams.diffuseDenoiserCS, ddParams.gatherKernel, HDShaderIDs._DenoiseInputTexture, ddResources.intermediateBuffer);
                cmd.SetComputeTextureParam(ddParams.diffuseDenoiserCS, ddParams.gatherKernel, HDShaderIDs._DenoiseOutputTextureRW, ddResources.outputBuffer);
                cmd.DispatchCompute(ddParams.diffuseDenoiserCS, ddParams.gatherKernel, numTilesX, numTilesY, ddParams.viewCount);
            }
        }

        class DiffuseDenoiserPassData
        {
            public DiffuseDenoiserParameters parameters;
            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle noisyBuffer;
            public TextureHandle intermediateBuffer;
            public TextureHandle outputBuffer;
        }

        public TextureHandle Denoise(RenderGraph renderGraph, HDCamera hdCamera, DiffuseDenoiserParameters tfParameters, TextureHandle noisyBuffer, TextureHandle depthPyramid, TextureHandle normalBuffer, TextureHandle outputBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<DiffuseDenoiserPassData>("DiffuseDenoiser", out var passData, ProfilingSampler.Get(HDProfileId.DiffuseFilter)))
            {
                // Cannot run in async
                builder.EnableAsyncCompute(false);

                // Fetch all the resources
                passData.parameters = tfParameters;
                passData.depthStencilBuffer = builder.ReadTexture(depthPyramid);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.noisyBuffer = builder.ReadTexture(noisyBuffer);
                passData.intermediateBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "DiffuseDenoiserIntermediate" });
                passData.outputBuffer = builder.WriteTexture(outputBuffer);

                builder.SetRenderFunc(
                (DiffuseDenoiserPassData data, RenderGraphContext ctx) =>
                {
                    DiffuseDenoiserResources ddResources = new DiffuseDenoiserResources();
                    ddResources.depthStencilBuffer = data.depthStencilBuffer;
                    ddResources.normalBuffer = data.normalBuffer;
                    ddResources.noisyBuffer = data.noisyBuffer;
                    ddResources.intermediateBuffer = data.intermediateBuffer;
                    ddResources.outputBuffer = data.outputBuffer;
                    DenoiseBuffer(ctx.cmd, data.parameters, ddResources);
                });
                return passData.outputBuffer;
            }
        }
    }
}
